using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Prepara el proyecto para el Universal Render Pipeline con el Renderer 2D (luces 2D, normal maps y
    /// post-procesado): crea los assets si faltan, los activa en Graphics y en todos los niveles de Quality,
    /// y crea el perfil de post-procesado (bloom, viñeta, grano, color...).
    /// </summary>
    public static class PipelineSetup
    {
        public const string SettingsFolder = "Assets/_Abismo/Settings";
        const string RendererPath = SettingsFolder + "/Abismo_Renderer2D.asset";
        const string PipelinePath = SettingsFolder + "/Abismo_URP2D.asset";
        const string ProfilePath = SettingsFolder + "/Abismo_PostProcesado.asset";

        static string PackagePath => UniversalRenderPipelineAsset.packagePath;

        public static Material SpriteLit => AssetDatabase.LoadAssetAtPath<Material>(PackagePath + "/Runtime/Materials/Sprite-Lit-Default.mat");
        public static Material SpriteUnlit => AssetDatabase.LoadAssetAtPath<Material>(PackagePath + "/Runtime/Materials/Sprite-Unlit-Default.mat");

        public static UniversalRenderPipelineAsset EnsurePipeline()
        {
            AbismoBuilder.EnsureFolder(SettingsFolder);

            // 1) Datos del Renderer 2D (lo mismo que el menú "URP Asset (with 2D Renderer)").
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                ResourceReloader.ReloadAllNullIn(renderer, PackagePath);
            }
            // postProcessData es interno: sin él, el Renderer 2D apaga TODO el post-procesado.
            var so = new SerializedObject(renderer);
            var postData = so.FindProperty("m_PostProcessData");
            if (postData != null && postData.objectReferenceValue == null)
                postData.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PostProcessData>(PackagePath + "/Runtime/Data/PostProcessData.asset");
            var lightScale = so.FindProperty("m_LightRenderTextureScale");
            if (lightScale != null) lightScale.floatValue = 1f; // a 640×360 cuesta poco y las luces quedan nítidas
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);

            // 2) Asset del pipeline.
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            pipeline.supportsHDR = true;       // las partes emisivas (> 1) alimentan el bloom
            pipeline.msaaSampleCount = 1;      // pixel art: sin antialiasing
            pipeline.renderScale = 1f;
            pipeline.colorGradingMode = ColorGradingMode.LowDynamicRange;
            EditorUtility.SetDirty(pipeline);

            // 3) Activarlo por defecto y en cada nivel de calidad.
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
            return pipeline;
        }

        /// <summary>Perfil de post-procesado: bloom para lo que brilla, viñeta, contraste frío y grano de película.</summary>
        public static VolumeProfile EnsurePostProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.1f);
            bloom.scatter.Override(0.62f);
            bloom.tint.Override(new Color(0.85f, 1f, 0.92f));
            bloom.highQualityFiltering.Override(false);

            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.02f, 0.02f, 0.04f));

            var color = profile.Add<ColorAdjustments>();
            color.contrast.Override(12f);
            color.saturation.Override(-12f);
            color.colorFilter.Override(new Color(0.92f, 0.97f, 1f));

            var grain = profile.Add<FilmGrain>();
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.16f);
            grain.response.Override(0.8f);

            profile.Add<ChromaticAberration>().intensity.Override(0f); // los anima CameraFX
            profile.Add<LensDistortion>().intensity.Override(0f);

            // Cada efecto es un sub-asset del perfil: hay que guardarlos dentro.
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>Activa el post-procesado en la cámara (por defecto viene apagado en URP).</summary>
        public static void ConfigureCamera(Camera cam)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;
            data.stopNaN = false;
            data.dithering = false;
            data.renderShadows = false;
            data.volumeLayerMask = 1; // capa "Default", donde está el Volume global
            data.volumeTrigger = cam.transform;
        }

        public static PixelPerfectCamera AddPixelPerfect(Camera cam, int ppu, int width, int height)
        {
            var ppc = cam.GetComponent<PixelPerfectCamera>();
            if (ppc == null) ppc = cam.gameObject.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = ppu;
            ppc.refResolutionX = width;
            ppc.refResolutionY = height;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
            return ppc;
        }

        /// <summary>Las luces 2D no tienen setter público para usar normal maps: se ajusta el campo serializado.</summary>
        public static void UseNormalMaps(Light2D light, float distance)
        {
            var so = new SerializedObject(light);
            var quality = so.FindProperty("m_NormalMapQuality");
            if (quality != null) quality.intValue = (int)Light2D.NormalMapQuality.Accurate;
            var dist = so.FindProperty("m_NormalMapDistance");
            if (dist != null) dist.floatValue = distance;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Avisa si el proyecto no usa el Input System nuevo (no hay API pública para cambiarlo).</summary>
        public static string InputHandlingWarning()
        {
            var settings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            if (settings == null) return null;
            var prop = new SerializedObject(settings).FindProperty("activeInputHandler");
            if (prop == null || prop.intValue != 0) return null;
            return "El proyecto usa el Input Manager antiguo. El juego funciona igual, pero para usar mando con el " +
                   "Input System nuevo ve a Project Settings > Player > Active Input Handling y elige \"Input System Package (New)\" o \"Both\".";
        }

        public static void ConfigurePlayer()
        {
            PlayerSettings.productName = "ABISMO";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
        }
    }
}
