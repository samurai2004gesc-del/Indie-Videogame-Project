using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>Lo que el constructor necesita de un personaje ya importado.</summary>
    public sealed class BakedCharacter
    {
        public string Id;
        public AnimatorController Controller;
        public Sprite FirstFrame;
        public Sprite[] Frames;      // todos los fotogramas...
        public Sprite[] GlowFrames;  // ...y su máscara de brillo (null si ese fotograma no brilla)
        public Dictionary<string, float> Durations = new Dictionary<string, float>();
    }

    /// <summary>
    /// "Hornea" el arte procedural a assets de Unity: escribe los PNG (color, normal map y máscara de brillo),
    /// configura su importación (pixel art: sin filtrado, sin compresión, 32 px por unidad), trocea las hojas de
    /// sprites, asigna los normal maps como textura secundaria "_NormalMap" (luces 2D de URP) y crea los
    /// AnimationClips y AnimatorControllers de cada personaje.
    /// Solo reimporta lo que ha cambiado, así que reconstruir es rápido.
    /// </summary>
    public sealed class ArtBaker
    {
        public const float PixelsPerUnit = 32f;
        public const string ArtRoot = "Assets/_Abismo/Art/Generated";
        public const string AnimRoot = "Assets/_Abismo/Animations";

        /// <summary>Importación pendiente de un PNG.</summary>
        sealed class Job
        {
            public string Path;
            public bool Changed;
            public bool IsNormal;
            public Vector2 Pivot;
            public Vector4 Border;
            public float PixelsPerUnit = ArtBaker.PixelsPerUnit;
            public string NormalPath;
            public int FrameW, FrameH, Columns;
            public List<string> FrameNames; // != null → hoja con varios sprites
            public List<bool> FrameUsed;
        }

        readonly List<Job> jobs = new List<Job>();
        readonly System.Action<string, float> progress;

        public ArtBaker(System.Action<string, float> progress) => this.progress = progress;

        // ------------------------------------------------------------------
        // 1) Escribir PNG
        // ------------------------------------------------------------------

        /// <summary>Escribe el PNG solo si cambió. Devuelve true si se escribió.</summary>
        static bool WritePng(string path, PixelCanvas canvas)
        {
            var texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false);
            texture.SetPixels32(canvas.Pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(png)) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, png);
            return true;
        }

        /// <summary>Un sprite suelto (con normal map y máscara de brillo opcionales).</summary>
        public string AddSprite(string folder, string name, PixelCanvas color, PixelCanvas normal, Vector2 pivot01, Vector4 border = default,
                                float pixelsPerUnit = PixelsPerUnit)
        {
            string path = $"{ArtRoot}/{folder}/{name}.png";
            string normalPath = null;
            if (normal != null)
            {
                normalPath = $"{ArtRoot}/{folder}/{name}_n.png";
                jobs.Add(new Job { Path = normalPath, Changed = WritePng(normalPath, normal), IsNormal = true });
            }
            jobs.Add(new Job { Path = path, Changed = WritePng(path, color), Pivot = pivot01, Border = border, NormalPath = normalPath, PixelsPerUnit = pixelsPerUnit });
            return path;
        }

        /// <summary>Varios fotogramas del mismo tamaño en una hoja (fila a fila). Devuelve la ruta.</summary>
        public string AddSheet(string folder, string name, IList<PixelCanvas> frames, IList<string> frameNames, IList<PixelCanvas> normals, Vector2 pivot01)
        {
            int fw = frames[0].Width, fh = frames[0].Height;
            int cols = Mathf.Clamp(4096 / fw, 1, frames.Count);
            int rows = Mathf.CeilToInt(frames.Count / (float)cols);
            var sheet = new PixelCanvas(cols * fw, rows * fh);
            var normalSheet = normals != null ? new PixelCanvas(cols * fw, rows * fh) : null;
            var used = new List<bool>();
            for (int i = 0; i < frames.Count; i++)
            {
                int ox = (i % cols) * fw, oy = sheet.Height - (i / cols + 1) * fh;
                bool any = Blit(frames[i], sheet, ox, oy);
                used.Add(any);
                if (normalSheet != null) Blit(normals[i], normalSheet, ox, oy);
            }
            string path = $"{ArtRoot}/{folder}/{name}.png";
            string normalPath = null;
            if (normalSheet != null)
            {
                normalPath = $"{ArtRoot}/{folder}/{name}_n.png";
                jobs.Add(new Job { Path = normalPath, Changed = WritePng(normalPath, normalSheet), IsNormal = true });
            }
            jobs.Add(new Job
            {
                Path = path, Changed = WritePng(path, sheet), Pivot = pivot01, NormalPath = normalPath,
                FrameW = fw, FrameH = fh, Columns = cols, FrameNames = new List<string>(frameNames), FrameUsed = used,
            });
            return path;
        }

        static bool Blit(PixelCanvas src, PixelCanvas dst, int ox, int oy)
        {
            bool any = false;
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    var p = src.Pixels[y * src.Width + x];
                    if (p.a > 0) any = true;
                    dst.Pixels[(oy + y) * dst.Width + ox + x] = p;
                }
            return any;
        }

        // ------------------------------------------------------------------
        // 2) Importar
        // ------------------------------------------------------------------

        /// <summary>Importa y configura todo lo pendiente. Primero los normal maps (los sprites los referencian).</summary>
        public void ImportAll(bool force)
        {
            progress("Importando texturas", 0.3f);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var todo = jobs.Where(j => j.Changed || force || NeedsSetup(j)).ToList();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var job in todo.Where(j => j.IsNormal)) ConfigureNormal(job.Path);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            int n = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var job in todo.Where(j => !j.IsNormal))
                {
                    if (++n % 8 == 0) progress("Configurando " + Path.GetFileName(job.Path), 0.3f + 0.15f * n / Mathf.Max(1, todo.Count));
                    if (job.FrameNames != null) ConfigureSheet(job);
                    else ConfigureSprite(job);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            jobs.Clear();
        }

        /// <summary>Si el PNG no cambió pero su importador no está preparado (p. ej. un .meta borrado), hay que configurarlo.</summary>
        static bool NeedsSetup(Job job)
        {
            var importer = AssetImporter.GetAtPath(job.Path) as TextureImporter;
            if (importer == null) return true;
            if (job.IsNormal) return importer.textureType != TextureImporterType.NormalMap;
            if (importer.textureType != TextureImporterType.Sprite) return true;
            return importer.spriteImportMode != (job.FrameNames != null ? SpriteImportMode.Multiple : SpriteImportMode.Single);
        }

        static void BaseSettings(TextureImporter importer, int maxSize)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(maxSize), 32, 8192);
            importer.mipmapEnabled = false;
            importer.isReadable = false;
        }

        static int SourceSize(string path)
        {
            var bytes = File.ReadAllBytes(path);
            // Cabecera PNG: ancho y alto en big-endian en los bytes 16..23.
            int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            return Mathf.Max(w, h);
        }

        static void ConfigureNormal(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.NormalMap;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.convertToNormalMap = false;
            settings.filterMode = FilterMode.Point;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.mipmapEnabled = false;
            importer.SetTextureSettings(settings);
            BaseSettings(importer, SourceSize(path));
            importer.SaveAndReimport();
        }

        static void SpriteSettings(TextureImporter importer, Job job, SpriteImportMode mode)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = mode;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;   // necesario para el modo Tiled y barato de generar
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = job.Pivot;
            settings.spriteBorder = job.Border;
            settings.spritePixelsPerUnit = job.PixelsPerUnit;
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.filterMode = FilterMode.Point;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            importer.SetTextureSettings(settings);
            BaseSettings(importer, SourceSize(job.Path));
        }

        static void ConfigureSprite(Job job)
        {
            var importer = AssetImporter.GetAtPath(job.Path) as TextureImporter;
            if (importer == null) return;
            SpriteSettings(importer, job, SpriteImportMode.Single);
            var normal = job.NormalPath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(job.NormalPath) : null;
            importer.secondarySpriteTextures = normal != null
                ? new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = normal } }
                : new SecondarySpriteTexture[0];
            importer.SaveAndReimport();
        }

        /// <summary>Trocea una hoja con el proveedor de datos del Sprite Editor (paquete 2D Sprite).</summary>
        static void ConfigureSheet(Job job)
        {
            var importer = AssetImporter.GetAtPath(job.Path) as TextureImporter;
            if (importer == null) return;
            SpriteSettings(importer, job, SpriteImportMode.Multiple); // ANTES de pedir el proveedor

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // Reutilizar los SpriteRect existentes por nombre mantiene sus IDs: clips y prefabs no pierden referencias.
            var existing = new Dictionary<string, SpriteRect>();
            foreach (var r in provider.GetSpriteRects())
                if (!string.IsNullOrEmpty(r.name)) existing[r.name] = r;

            int sheetH = Mathf.CeilToInt(job.FrameNames.Count / (float)job.Columns) * job.FrameH;
            var rects = new List<SpriteRect>();
            for (int i = 0; i < job.FrameNames.Count; i++)
            {
                if (!job.FrameUsed[i]) continue;
                string name = job.FrameNames[i];
                if (!existing.TryGetValue(name, out var rect)) rect = new SpriteRect { name = name, spriteID = GUID.Generate() };
                rect.rect = new Rect((i % job.Columns) * job.FrameW, sheetH - (i / job.Columns + 1) * job.FrameH, job.FrameW, job.FrameH);
                rect.alignment = SpriteAlignment.Custom;
                rect.pivot = job.Pivot;
                rect.border = Vector4.zero;
                rects.Add(rect);
            }
            provider.SetSpriteRects(rects.ToArray());
            var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (ids != null) ids.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());

            var normal = job.NormalPath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(job.NormalPath) : null;
            var secondary = provider.GetDataProvider<ISecondaryTextureDataProvider>();
            if (secondary != null)
            {
                secondary.textures = normal != null
                    ? new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = normal } }
                    : new SecondarySpriteTexture[0];
            }
            provider.Apply();
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------
        // 3) Cargar
        // ------------------------------------------------------------------

        public static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        public static Dictionary<string, Sprite> LoadSheet(string path) =>
            AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First());

        // ------------------------------------------------------------------
        // Personajes: hojas + clips + animator
        // ------------------------------------------------------------------

        public sealed class PendingCharacter
        {
            public CharacterArt Art;
            public string SheetPath, GlowPath;
            public List<AnimSpec> Animations;
            public List<string> FrameNames;
            public List<bool> HasGlow;
        }

        /// <summary>Dibuja todos los fotogramas de un personaje y prepara sus hojas (color, normal, brillo).</summary>
        public PendingCharacter AddCharacter(CharacterArt art)
        {
            var anims = art.Animations();
            var colors = new List<PixelCanvas>();
            var normals = new List<PixelCanvas>();
            var glows = new List<PixelCanvas>();
            var names = new List<string>();
            var hasGlow = new List<bool>();
            foreach (var anim in anims)
            {
                for (int f = 0; f < anim.Frames; f++)
                {
                    var color = anim.Draw(f).Render(out var normal, out var emission);
                    colors.Add(color);
                    normals.Add(normal);
                    glows.Add(emission);
                    names.Add($"{art.Id}_{anim.Name}_{f:00}");
                    hasGlow.Add(emission.Pixels.Any(p => p.a > 0));
                }
            }
            var pivot = new Vector2(art.Pivot.x / art.FrameWidth, art.Pivot.y / art.FrameHeight);
            var pending = new PendingCharacter
            {
                Art = art, Animations = anims, FrameNames = names, HasGlow = hasGlow,
                SheetPath = AddSheet("Personajes", art.Id, colors, names, normals, pivot),
            };
            if (hasGlow.Any(g => g))
                pending.GlowPath = AddSheet("Personajes", art.Id + "_brillo", glows, names.Select(n => n + "_brillo").ToList(), null, pivot);
            return pending;
        }

        /// <summary>Después de importar: crea los clips y el AnimatorController.</summary>
        public static BakedCharacter FinishCharacter(PendingCharacter pending)
        {
            var art = pending.Art;
            var sprites = LoadSheet(pending.SheetPath);
            var glowSprites = pending.GlowPath != null ? LoadSheet(pending.GlowPath) : new Dictionary<string, Sprite>();
            string folder = $"{AnimRoot}/{art.Id}";
            AbismoBuilder.EnsureFolder(folder);

            var baked = new BakedCharacter { Id = art.Id };
            var frames = new List<Sprite>();
            var glowFrames = new List<Sprite>();
            var states = new List<KeyValuePair<string, AnimationClip>>();
            int index = 0;
            foreach (var anim in pending.Animations)
            {
                var animFrames = new Sprite[anim.Frames];
                for (int f = 0; f < anim.Frames; f++, index++)
                {
                    string name = pending.FrameNames[index];
                    sprites.TryGetValue(name, out var sprite);
                    animFrames[f] = sprite;
                    frames.Add(sprite);
                    glowSprites.TryGetValue(name + "_brillo", out var glow);
                    glowFrames.Add(glow);
                }
                // Si un fotograma salió vacío (no debería), repite el anterior.
                for (int f = 0; f < animFrames.Length; f++)
                    if (animFrames[f] == null && f > 0) animFrames[f] = animFrames[f - 1];
                var clip = SpriteClip($"{folder}/{anim.Name}.anim", animFrames, anim.Fps, anim.Loop);
                states.Add(new KeyValuePair<string, AnimationClip>(anim.Name, clip));
                baked.Durations[anim.Name] = anim.Duration;
            }
            baked.Frames = frames.ToArray();
            baked.GlowFrames = glowFrames.ToArray();
            baked.FirstFrame = frames.FirstOrDefault(s => s != null);
            baked.Controller = Controller($"{folder}/{art.Id}.controller", states, pending.Animations[0].Name);
            return baked;
        }

        public static AnimationClip SpriteClip(string path, Sprite[] frames, float fps, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path); // reutilizar el asset mantiene su GUID al reconstruir
            }
            clip.frameRate = fps;
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            // Clave final repetida: el último fotograma dura lo mismo que los demás (duración = fotogramas / fps).
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        public static AnimatorController Controller(string path, List<KeyValuePair<string, AnimationClip>> states, string defaultState)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states) machine.RemoveState(child.state);
            foreach (var pair in states)
            {
                var state = machine.AddState(pair.Key);
                state.motion = pair.Value;
                state.writeDefaultValues = false;
                if (pair.Key == defaultState) machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }
    }
}
