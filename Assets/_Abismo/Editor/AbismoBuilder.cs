using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Menú "Abismo" de la barra superior de Unity. Con un clic:
    ///   1. Configura el proyecto para URP 2D (luces 2D, normal maps, bloom y post-procesado) y la cámara pixel-perfect.
    ///   2. Pinta TODO el arte por código (personajes animados, terreno, decorado, fondos, interfaz) y lo importa.
    ///   3. Crea los prefabs (jugador, enemigos, jefe, proyectiles, altar...) con sus animaciones.
    ///   4. Monta la escena Assets/_Abismo/Scenes/Nivel_01 a partir del mapa de texto
    ///      Assets/_Abismo/Levels/nivel_01.txt (¡edítalo para diseñar tu propio nivel!).
    /// </summary>
    public static class AbismoBuilder
    {
        const string Root = "Assets/_Abismo";
        const string MaterialFolder = Root + "/Materials";
        const string PrefabFolder = Root + "/Prefabs";
        const string SceneFolder = Root + "/Scenes";
        const string FontFolder = Root + "/Fonts";
        const string LevelFile = Root + "/Levels/nivel_01.txt";
        const string ScenePath = SceneFolder + "/Nivel_01.unity";
        const string PrefabLabel = "AbismoV2";

        // Resolución de referencia: 640×360 píxeles de arte a 32 px por unidad (20 × 11,25 casillas).
        const int RefWidth = 640, RefHeight = 360;
        const float HalfViewW = RefWidth / 2f / ArtBaker.PixelsPerUnit, HalfViewH = RefHeight / 2f / ArtBaker.PixelsPerUnit;

        // Orden de dibujado (todo en la capa de ordenación "Default").
        const int OrderBackWall = -15, OrderPropsBack = -8, OrderPropsHanging = -7, OrderShafts = -5, OrderTerrain = 0,
                  OrderPlatforms = 1, OrderHazards = 2, OrderInteractables = 3, OrderBoss = 4, OrderEnemies = 5, OrderPlayer = 10,
                  OrderPickups = 15, OrderWater = 20, OrderForeground = 60;

        [MenuItem("Abismo/Construir demo jugable", false, 0)]
        public static void BuildDemo() => Build(false, false);

        [MenuItem("Abismo/Restablecer prefabs y construir", false, 20)]
        public static void ResetPrefabsAndBuild()
        {
            bool ok = EditorUtility.DisplayDialog("Abismo",
                "Esto vuelve a crear desde cero los prefabs de " + PrefabFolder + ".\n" +
                "Los valores que hayas ajustado en ellos (velocidad, vida, daño...) se perderán.", "Restablecer", "Cancelar");
            if (ok) Build(false, true);
        }

        [MenuItem("Abismo/Volver a importar todo el arte y construir", false, 21)]
        public static void ReimportArtAndBuild() => Build(true, false);

        // ------------------------------------------------------------------
        // Proceso completo
        // ------------------------------------------------------------------

        sealed class Context
        {
            public LevelData Level;
            public Materials Mat;
            public Art Art;
            public Prefabs Prefabs;
            public int GroundLayer, PlayerLayer, EnemyLayer;
        }

        static void Build(bool forceArt, bool overwritePrefabs)
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Abismo", "Sal del modo Play antes de construir.", "Vale");
                return;
            }
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Abismo",
                    "Se volverá a crear la escena Nivel_01 a partir de " + LevelFile + ".\n" +
                    "Los cambios hechos A MANO en la escena se perderán (tus scripts y el mapa no se tocan).\n\n¿Continuar?",
                    "Construir", "Cancelar"))
            {
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var ctx = new Context();
                Progress("Leyendo el mapa", 0.01f);
                ctx.Level = LevelData.Load(LevelFile);

                Progress("Capas y física", 0.02f);
                ctx.GroundLayer = EnsureLayer(GameLayers.Ground);
                ctx.PlayerLayer = EnsureLayer(GameLayers.Player);
                ctx.EnemyLayer = EnsureLayer(GameLayers.Enemy);
                Physics2D.IgnoreLayerCollision(ctx.PlayerLayer, ctx.EnemyLayer, true);
                Physics2D.IgnoreLayerCollision(ctx.EnemyLayer, ctx.EnemyLayer, true);
                EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
                foreach (var folder in new[] { MaterialFolder, PrefabFolder, SceneFolder, ArtBaker.ArtRoot, ArtBaker.AnimRoot }) EnsureFolder(folder);

                Progress("Configurando URP 2D", 0.04f);
                PipelineSetup.EnsurePipeline();
                PipelineSetup.ConfigurePlayer();
                ctx.Mat = CreateMaterials();

                ctx.Art = BakeArt(ctx.Level, forceArt);

                Progress("Creando prefabs", 0.7f);
                ctx.Prefabs = CreatePrefabs(ctx, overwritePrefabs);

                Progress("Construyendo el nivel", 0.8f);
                BuildScene(ctx);
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();

                string warning = PipelineSetup.InputHandlingWarning();
                EditorUtility.DisplayDialog("Abismo",
                    $"¡Listo en {watch.Elapsed.TotalSeconds:0} s! Se ha creado y abierto la escena Nivel_01.\n\nPulsa ▶ (Play) para jugar.\n" +
                    "Controles: A/D mover · Espacio saltar · J atacar · L esquivar · I parar · F curarse · U conjuro · E interactuar · Esc pausa" +
                    (warning != null ? "\n\nAviso: " + warning : ""),
                    "¡A jugar!");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Abismo", "Algo ha fallado al construir:\n" + e.Message + "\n\nMira la ventana Console para más detalles.", "Vale");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static void Progress(string message, float value) => EditorUtility.DisplayProgressBar("Abismo", message, value);

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Busca la capa por nombre y, si no existe, la crea en el primer hueco libre (8-31).</summary>
        static int EnsureLayer(string name)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == name) return i;
            }
            for (int i = 8; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = name;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }
            throw new System.Exception("No quedan capas libres para crear '" + name + "'.");
        }

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        sealed class Materials
        {
            public Material Lit, Unlit, Additive, Silhouette, Emissive;
        }

        static Materials CreateMaterials()
        {
            var m = new Materials
            {
                Lit = PipelineSetup.SpriteLit,
                Unlit = PipelineSetup.SpriteUnlit,
                Additive = MaterialAsset("SpriteAdditive", "Abismo/SpriteAdditive"),
                Silhouette = MaterialAsset("SpriteSilhouette", "Abismo/SpriteSilhouette"),
                Emissive = MaterialAsset("SpriteEmissive", "Abismo/SpriteEmissive"),
            };
            m.Emissive.SetFloat("_Intensity", 1.6f);
            if (m.Lit == null || m.Unlit == null)
                throw new System.Exception("No se encontraron los materiales de sprites de URP. ¿Está instalado el paquete Universal RP (Window > Package Manager)?");
            return m;
        }

        static Material MaterialAsset(string name, string shaderName)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new System.Exception("No se encontró el shader " + shaderName + " (Assets/_Abismo/Shaders).");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------
        // Arte
        // ------------------------------------------------------------------

        sealed class Art
        {
            public readonly Dictionary<string, BakedCharacter> Characters = new Dictionary<string, BakedCharacter>();
            public readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
            public readonly Dictionary<string, Sprite> Glows = new Dictionary<string, Sprite>();
            public readonly Dictionary<string, PropSprite> PropInfo = new Dictionary<string, PropSprite>();
            public readonly Dictionary<string, Sprite[]> Animations = new Dictionary<string, Sprite[]>();
            public readonly Dictionary<string, float> AnimationFps = new Dictionary<string, float>();
            public readonly List<(TerrainChunk chunk, Sprite sprite, bool back)> Terrain = new List<(TerrainChunk, Sprite, bool)>();
            public readonly Dictionary<Zone, List<(BackgroundLayer layer, Sprite sprite)>> Backgrounds = new Dictionary<Zone, List<(BackgroundLayer, Sprite)>>();
            public Font TitleFont, TextFont;

            public Sprite this[string name]
            {
                get
                {
                    if (Sprites.TryGetValue(name, out var sprite) && sprite != null) return sprite;
                    throw new System.Exception("No se encontró el sprite '" + name + "'.");
                }
            }

            public Sprite Glow(string name) => Glows.TryGetValue(name, out var g) ? g : null;
        }

        static Art BakeArt(LevelData level, bool force)
        {
            var art = new Art();
            var baker = new ArtBaker(Progress);

            // Personajes.
            var characters = new CharacterArt[] { new AhogadoArt(), new ProfundoArt(), new SectarioArt(), new OjoArt(), new ArcipresteArt() };
            var pending = new List<ArtBaker.PendingCharacter>();
            for (int i = 0; i < characters.Length; i++)
            {
                Progress("Animando a " + characters[i].Id, 0.06f + 0.12f * i / characters.Length);
                pending.Add(baker.AddCharacter(characters[i]));
            }

            // Decorado y objetos.
            Progress("Pintando el decorado", 0.18f);
            var propPaths = new Dictionary<string, (string color, string glow)>();
            foreach (var prop in PropArt.Statics().Concat(PropArt.Gameplay()))
            {
                string path = baker.AddSprite("Decorado", prop.Name, prop.Color, prop.Normal, prop.Pivot01, prop.Border);
                string glow = prop.Emission != null ? baker.AddSprite("Decorado", prop.Name + "_brillo", prop.Emission, null, prop.Pivot01) : null;
                propPaths[prop.Name] = (path, glow);
                art.PropInfo[prop.Name] = prop;
            }
            var animPaths = new Dictionary<string, (string path, List<string> names)>();
            foreach (var anim in PropArt.Animated())
            {
                var names = Enumerable.Range(0, anim.Frames.Count).Select(i => $"{anim.Name}_{i:00}").ToList();
                animPaths[anim.Name] = (baker.AddSheet("Decorado", anim.Name, anim.Frames, names, anim.Normals, anim.Pivot01), names);
                art.AnimationFps[anim.Name] = anim.Fps;
            }
            var pixel = new PixelCanvas(4, 4);
            for (int i = 0; i < pixel.Pixels.Length; i++) pixel.Pixels[i] = new Color32(255, 255, 255, 255);
            string pixelPath = baker.AddSprite("Comun", "pixel", pixel, null, new Vector2(0.5f, 0.5f), default, 4f);

            // Terreno pintado a partir del mapa.
            Progress("Pintando el terreno", 0.24f);
            System.Func<int, Zone> zoneAt = level.ZoneAt;
            var solid = TerrainPainter.PaintSolid((x, y) => level.At(x, y) == '#', level.Width, level.Height, zoneAt);
            var back = TerrainPainter.PaintBackWall(level.IsBackWall, level.Width, level.Height, zoneAt);
            var terrainPaths = new List<(TerrainChunk chunk, string path, bool back)>();
            foreach (var chunk in solid) terrainPaths.Add((chunk, baker.AddSprite("Terreno", $"solido_{chunk.TileX}_{chunk.TileY}", chunk.Color, chunk.Normal, Vector2.zero), false));
            foreach (var chunk in back) terrainPaths.Add((chunk, baker.AddSprite("Terreno", $"fondo_{chunk.TileX}_{chunk.TileY}", chunk.Color, chunk.Normal, Vector2.zero), true));
            RemoveStale(ArtBaker.ArtRoot + "/Terreno", terrainPaths.Select(t => t.path));

            // Fondos con paralaje.
            Progress("Pintando los fondos", 0.28f);
            var bgPaths = new Dictionary<Zone, List<(BackgroundLayer layer, string path)>>();
            foreach (Zone zone in System.Enum.GetValues(typeof(Zone)))
            {
                bgPaths[zone] = new List<(BackgroundLayer, string)>();
                foreach (var layer in BackgroundArt.For(zone))
                    bgPaths[zone].Add((layer, baker.AddSprite("Fondos", $"{zone.ToString().ToLowerInvariant()}_{layer.Name}", layer.Canvas, null, Vector2.zero)));
            }

            // Interfaz (100 px por unidad = 1 píxel de arte por unidad del Canvas de 640×360).
            var uiPaths = new Dictionary<string, string>();
            foreach (var ui in UIArt.All()) uiPaths[ui.Name] = baker.AddSprite("Interfaz", ui.Name, ui.Canvas, null, new Vector2(0.5f, 0.5f), ui.Border, 100f);

            baker.ImportAll(force);

            Progress("Creando animaciones", 0.5f);
            foreach (var p in pending) art.Characters[p.Art.Id] = ArtBaker.FinishCharacter(p);
            foreach (var kv in propPaths)
            {
                art.Sprites[kv.Key] = ArtBaker.LoadSprite(kv.Value.color);
                if (kv.Value.glow != null) art.Glows[kv.Key] = ArtBaker.LoadSprite(kv.Value.glow);
            }
            foreach (var kv in animPaths)
            {
                var sheet = ArtBaker.LoadSheet(kv.Value.path);
                art.Animations[kv.Key] = kv.Value.names.Select(n => sheet.TryGetValue(n, out var s) ? s : null).Where(s => s != null).ToArray();
            }
            art.Sprites["pixel"] = ArtBaker.LoadSprite(pixelPath);
            foreach (var t in terrainPaths) art.Terrain.Add((t.chunk, ArtBaker.LoadSprite(t.path), t.back));
            foreach (var kv in bgPaths) art.Backgrounds[kv.Key] = kv.Value.Select(b => (b.layer, ArtBaker.LoadSprite(b.path))).ToList();
            foreach (var kv in uiPaths) art.Sprites[kv.Key] = ArtBaker.LoadSprite(kv.Value);

            art.TitleFont = ImportFont(FontFolder + "/Jacquard24-Regular.ttf", 24);
            art.TextFont = ImportFont(FontFolder + "/Jersey10-Regular.ttf", 10);
            AssetDatabase.SaveAssets();
            return art;
        }

        /// <summary>Borra trozos de terreno de versiones anteriores del mapa que ya no existen.</summary>
        static void RemoveStale(string folder, IEnumerable<string> keep)
        {
            if (!Directory.Exists(folder)) return;
            var wanted = new HashSet<string>(keep.SelectMany(p => new[] { p, p.Replace(".png", "_n.png") }));
            foreach (var file in Directory.GetFiles(folder, "*.png"))
            {
                string path = file.Replace('\\', '/');
                if (!wanted.Contains(path)) AssetDatabase.DeleteAsset(path);
            }
        }

        /// <summary>Fuente pixel: se rasteriza sin suavizado a su tamaño de diseño (o múltiplos) para que quede nítida.</summary>
        static Font ImportFont(string path, int designSize)
        {
            var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Abismo] No se encontró la fuente " + path + "; se usará la fuente por defecto.");
                return null;
            }
            if (importer.fontSize != designSize || importer.fontRenderingMode != FontRenderingMode.HintedRaster)
            {
                importer.fontSize = designSize;
                importer.fontRenderingMode = FontRenderingMode.HintedRaster;
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.includeFontData = true;
                importer.characterPadding = 1;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Font>(path);
        }

        // ------------------------------------------------------------------
        // Piezas comunes de los prefabs
        // ------------------------------------------------------------------

        static GameObject Child(string name, GameObject parent, Vector2 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        static SpriteRenderer AddSprite(GameObject go, Sprite sprite, Material material, int order, Color? color = null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = material;
            sr.sortingOrder = order;
            if (color.HasValue) sr.color = color.Value;
            return sr;
        }

        static SpriteRenderer AddGlow(GameObject parent, Context ctx, Vector2 position, float scale, Color color, int order)
        {
            var glow = AddSprite(Child("Brillo", parent, position), ctx.Art["brillo"], ctx.Mat.Additive, order, color);
            glow.transform.localScale = new Vector3(scale, scale, 1f);
            return glow;
        }

        static Light2D AddLight(GameObject parent, Vector2 position, Color color, float intensity, float outerRadius, float falloff = 0.6f,
                                bool normals = true, float flicker = 0f)
        {
            var go = Child("Luz", parent, position);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.blendStyleIndex = 0;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = outerRadius * 0.15f;
            light.pointLightOuterRadius = outerRadius;
            light.pointLightInnerAngle = 360f;
            light.pointLightOuterAngle = 360f;
            light.falloffIntensity = falloff;
            light.shadowsEnabled = false;
            light.volumetricEnabled = false;
            light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
            if (normals) PipelineSetup.UseNormalMaps(light, 1.6f);
            if (flicker > 0f) go.AddComponent<LightFlicker>().Configure(flicker, 7f);
            return light;
        }

        static void AddFlame(GameObject parent, Context ctx, Vector2 position, bool big, int order)
        {
            string name = big ? "llama_grande" : "llama";
            var flame = AddSprite(Child("Llama", parent, position), ctx.Art.Animations[name][0], ctx.Mat.Emissive, order);
            flame.gameObject.AddComponent<LoopingSprite>().Configure(ctx.Art.Animations[name], ctx.Art.AnimationFps[name]);
        }

        /// <summary>
        /// Cuerpo animado de un personaje: sprite iluminado + Animator, su capa de brillo (ojos, brasas) y la
        /// silueta para los destellos (golpes, avisos de ataque). Devuelve el objeto "Visual" (se voltea con la escala).
        /// </summary>
        static (GameObject visual, SpriteRenderer body, CharacterAnimator anim, FlashEffect flash) Body(GameObject root, Context ctx, BakedCharacter character, int order)
        {
            var visual = Child("Visual", root, Vector2.zero);
            var body = AddSprite(visual, character.FirstFrame, ctx.Mat.Lit, order);
            var animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = character.Controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var anim = visual.AddComponent<CharacterAnimator>();

            if (character.GlowFrames.Any(g => g != null))
            {
                var glowRenderer = AddSprite(Child("Brillo", visual, Vector2.zero), null, ctx.Mat.Emissive, order + 1);
                var glow = glowRenderer.gameObject.AddComponent<SpriteGlow>();
                glow.source = body;
                glow.overlay = glowRenderer;
                glow.from = character.Frames;
                glow.to = character.GlowFrames;
            }

            var overlay = AddSprite(Child("Destello", visual, Vector2.zero), null, ctx.Mat.Silhouette, order + 2);
            overlay.enabled = false;
            var flash = root.AddComponent<FlashEffect>();
            flash.source = body;
            flash.overlay = overlay;
            return (visual, body, anim, flash);
        }

        static GameObject SavePrefab(GameObject go, string fileName)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/" + fileName + ".prefab");
            Object.DestroyImmediate(go);
            AssetDatabase.SetLabels(prefab, new[] { PrefabLabel });
            return prefab;
        }

        /// <summary>
        /// Si el prefab ya existe (y es de esta versión) se reutiliza tal cual, con los ajustes que le hayas hecho;
        /// solo se crea si falta, si es de la versión anterior o si se pide restablecerlo.
        /// </summary>
        static GameObject GetOrBuild(string fileName, bool overwrite, System.Func<GameObject> build)
        {
            string path = PrefabFolder + "/" + fileName + ".prefab";
            if (!overwrite)
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null && AssetDatabase.GetLabels(existing).Contains(PrefabLabel)) return existing;
            }
            return build();
        }

        // ------------------------------------------------------------------
        // Prefabs
        // ------------------------------------------------------------------

        sealed class Prefabs
        {
            public GameObject Player, DeepOne, Cultist, Eye, Boss;
            public GameObject Orb, Spell, Coin, Fragment, Tentacle, Altar, Inscription;
        }

        static Prefabs CreatePrefabs(Context ctx, bool overwrite)
        {
            var p = new Prefabs();
            p.Orb = GetOrBuild("Orbe", overwrite, () =>
                BuildProjectile(ctx, "Orbe", ctx.Art["orbe"], new Color(0.85f, 0.45f, 1f), 6f, 12, 0.3f, 6f, false, 0f, 1f));
            p.Spell = GetOrBuild("SignoArcano", overwrite, () =>
                BuildProjectile(ctx, "SignoArcano", ctx.Art["signo_arcano"], new Color(0.45f, 1f, 0.82f), 11f, 25, 0.5f, 1.2f, true, -540f, 1.1f));
            p.Coin = GetOrBuild("Moneda", overwrite, () => BuildCoin(ctx));
            p.Fragment = GetOrBuild("FragmentoDeMente", overwrite, () => BuildFragment(ctx));
            p.Tentacle = GetOrBuild("Tentaculo", overwrite, () => BuildTentacle(ctx));
            p.Altar = GetOrBuild("AltarDelSignoAntiguo", overwrite, () => BuildAltar(ctx));
            p.Inscription = GetOrBuild("Inscripcion", overwrite, () => BuildInscription(ctx));
            p.Player = GetOrBuild("Jugador", overwrite, () => BuildPlayer(ctx, p));

            var gold = p.Coin.GetComponent<GoldPickup>();
            p.DeepOne = GetOrBuild("Profundo", overwrite, () => BuildEnemy<DeepOneEnemy>(ctx, "Profundo", "profundo", new Vector2(0.9f, 1.6f), false, gold,
                e => { e.ConfigureStats("Profundo", 45, 20, 0f, true); e.ConfigureDeath(0.95f); }, null));
            p.Cultist = GetOrBuild("Sectario", overwrite, () => BuildEnemy<CultistEnemy>(ctx, "Sectario", "sectario", new Vector2(0.8f, 1.85f), false, gold,
                e => { e.ConfigureStats("Sectario", 30, 25, 0f, true); e.ConfigureDeath(1.05f); e.projectilePrefab = p.Orb.GetComponent<Projectile>(); },
                v => AddLight(v, new Vector2(0.55f, 2.6f), new Color(1f, 0.55f, 0.25f), 0.9f, 2.4f, 0.7f, true, 0.3f)));
            p.Eye = GetOrBuild("OjoDelVacio", overwrite, () => BuildEnemy<FlyingEyeEnemy>(ctx, "OjoDelVacio", "ojo", new Vector2(0.9f, 0.9f), true, gold,
                e => { e.ConfigureStats("Ojo del Vacío", 22, 15, 0f, true); e.ConfigureDeath(0.6f); },
                v => AddLight(v, new Vector2(0.2f, 0.2f), new Color(1f, 0.3f, 0.3f), 0.7f, 2f, 0.7f)));
            p.Boss = GetOrBuild("Arcipreste", overwrite, () => BuildEnemy<BossArchpriest>(ctx, "Arcipreste", "arcipreste", new Vector2(1.9f, 3.9f), false, gold,
                e =>
                {
                    e.ConfigureStats("El Arcipreste de las Mareas", 420, 300, 1f, false);
                    e.ConfigureDeath(2.2f);
                    e.tentaclePrefab = p.Tentacle.GetComponent<TentacleStrike>();
                    e.orbPrefab = p.Orb.GetComponent<Projectile>();
                },
                v => AddLight(v, new Vector2(0.4f, 4.6f), new Color(0.45f, 1f, 0.8f), 1.1f, 4.5f, 0.6f, true, 0.15f)));
            return p;
        }

        static GameObject BuildPlayer(Context ctx, Prefabs prefabs)
        {
            var root = new GameObject("Jugador") { layer = ctx.PlayerLayer };
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.2f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var capsule = root.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.7f, 1.8f);
            capsule.offset = new Vector2(0f, 0.9f);
            root.AddComponent<PlayerStats>();
            var controller = root.AddComponent<PlayerController>();

            var (visual, body, anim, flash) = Body(root, ctx, ctx.Art.Characters["ahogado"], OrderPlayer);
            // La mirilla de la escafandra ilumina un poco alrededor (como un farol de buzo).
            AddLight(visual, new Vector2(0.35f, 1.55f), new Color(0.45f, 1f, 0.85f), 0.55f, 3.6f, 0.75f, true, 0.06f);

            controller.visual = visual.transform;
            controller.bodyRenderer = body;
            controller.anim = anim;
            controller.flash = flash;
            controller.spellPrefab = prefabs.Spell.GetComponent<Projectile>();
            return SavePrefab(root, "Jugador");
        }

        static GameObject BuildEnemy<T>(Context ctx, string name, string characterId, Vector2 size, bool flying, GoldPickup gold,
                                        System.Action<T> configure, System.Action<GameObject> decorate) where T : Enemy
        {
            var root = new GameObject(name) { layer = ctx.EnemyLayer };
            var rb = root.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (flying)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                var circle = root.AddComponent<CircleCollider2D>();
                circle.radius = size.x * 0.5f;
                circle.offset = Vector2.zero;
            }
            else
            {
                rb.gravityScale = 3f;
                var capsule = root.AddComponent<CapsuleCollider2D>();
                capsule.size = size;
                capsule.offset = new Vector2(0f, size.y * 0.5f);
            }

            int order = characterId == "arcipreste" ? OrderBoss : OrderEnemies;
            var (visual, _, anim, flash) = Body(root, ctx, ctx.Art.Characters[characterId], order);
            decorate?.Invoke(visual);

            var enemy = root.AddComponent<T>();
            enemy.visual = visual.transform;
            enemy.anim = anim;
            enemy.flash = flash;
            enemy.goldPrefab = gold;
            configure(enemy);
            return SavePrefab(root, name);
        }

        static GameObject BuildProjectile(Context ctx, string name, Sprite sprite, Color color, float speed, int damage, float radius, float life,
                                          bool piercing, float spin, float scale)
        {
            var root = new GameObject(name);
            AddGlow(root, ctx, Vector2.zero, 0.55f * scale, new Color(color.r, color.g, color.b, 0.45f), 29);
            var sr = AddSprite(Child("Sprite", root, Vector2.zero), sprite, ctx.Mat.Additive, 30, color);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            AddLight(root, Vector2.zero, color, 0.9f, 2.2f, 0.7f, true);
            var projectile = root.AddComponent<Projectile>();
            projectile.spriteRenderer = sr;
            projectile.Configure(speed, damage, radius, life, piercing, spin);
            return SavePrefab(root, name);
        }

        static GameObject BuildCoin(Context ctx)
        {
            var root = new GameObject("Moneda");
            var visual = Child("Visual", root, Vector2.zero);
            AddSprite(visual, ctx.Art["moneda"], ctx.Mat.Lit, OrderPickups);
            AddGlow(visual, ctx, Vector2.zero, 0.3f, new Color(1f, 0.8f, 0.3f, 0.35f), OrderPickups - 1);
            root.AddComponent<GoldPickup>().visual = visual.transform;
            return SavePrefab(root, "Moneda");
        }

        static GameObject BuildFragment(Context ctx)
        {
            var root = new GameObject("FragmentoDeMente");
            var visual = Child("Visual", root, Vector2.zero);
            AddSprite(visual, ctx.Art["fragmento"], ctx.Mat.Emissive, OrderPickups);
            AddGlow(visual, ctx, Vector2.zero, 0.8f, new Color(0.65f, 0.45f, 1f, 0.5f), OrderPickups - 1);
            AddLight(root, Vector2.zero, new Color(0.7f, 0.5f, 1f), 1f, 3f, 0.7f, true, 0.2f);
            root.AddComponent<MindFragment>().visual = visual.transform;
            return SavePrefab(root, "FragmentoDeMente");
        }

        static GameObject BuildTentacle(Context ctx)
        {
            var root = new GameObject("Tentaculo");
            var warning = AddSprite(Child("Aviso", root, Vector2.zero), ctx.Art["brillo"], ctx.Mat.Additive, OrderHazards, new Color(1f, 0.15f, 0.1f, 0.5f));
            warning.transform.localScale = new Vector3(1.1f, 0.3f, 1f);
            var body = Child("Cuerpo", root, Vector2.zero);
            AddSprite(body, ctx.Art["tentaculo"], ctx.Mat.Lit, OrderEnemies + 3);
            var strike = root.AddComponent<TentacleStrike>();
            strike.tentacle = body.transform;
            strike.warning = warning;
            return SavePrefab(root, "Tentaculo");
        }

        static GameObject BuildAltar(Context ctx)
        {
            var root = new GameObject("AltarDelSignoAntiguo");
            var sprite = Child("Sprite", root, Vector2.zero);
            AddSprite(sprite, ctx.Art["altar"], ctx.Mat.Lit, OrderInteractables);
            var runeGlow = ctx.Art.Glow("altar");
            if (runeGlow != null) AddSprite(Child("Runa", sprite, Vector2.zero), runeGlow, ctx.Mat.Emissive, OrderInteractables + 1);
            foreach (var anchor in ctx.Art.PropInfo["altar"].FlameAnchors) AddFlame(sprite, ctx, anchor / ArtBaker.PixelsPerUnit, false, OrderInteractables + 2);
            var glow = AddGlow(root, ctx, new Vector2(0f, 0.85f), 1.6f, new Color(0.45f, 1f, 0.8f, 0.12f), OrderInteractables - 1);
            AddLight(root, new Vector2(0f, 1.2f), new Color(0.45f, 1f, 0.8f), 1.2f, 5f, 0.6f, true, 0.12f);
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2.4f, 2.6f);
            box.offset = new Vector2(0f, 1.3f);
            root.AddComponent<ElderSignAltar>().glow = glow;
            return SavePrefab(root, "AltarDelSignoAntiguo");
        }

        static GameObject BuildInscription(Context ctx)
        {
            var root = new GameObject("Inscripcion");
            AddSprite(Child("Sprite", root, Vector2.zero), ctx.Art["inscripcion"], ctx.Mat.Lit, OrderInteractables);
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.8f, 2.2f);
            box.offset = new Vector2(0f, 1.1f);
            root.AddComponent<LoreInscription>();
            return SavePrefab(root, "Inscripcion");
        }

        // ------------------------------------------------------------------
        // Escena
        // ------------------------------------------------------------------

        static void BuildScene(Context ctx)
        {
            var level = ctx.Level;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Colisiones invisibles (una casilla = una unidad) y el terreno pintado encima.
            var grid = new GameObject("Nivel");
            grid.AddComponent<Grid>();
            var solidTile = CollisionTile();
            var ground = CreateTilemap("Colision", grid.transform, ctx.GroundLayer, false);
            var platforms = CreateTilemap("Plataformas", grid.transform, ctx.GroundLayer, true);
            BuildTerrain(ctx);

            var entities = new GameObject("Entidades").transform;
            var hazards = new GameObject("Peligros").transform;
            var zones = new GameObject("Zonas").transform;
            var props = new GameObject("Decorado").transform;

            PlayerController player = null;
            BossArchpriest boss = null;
            var occupied = new HashSet<Vector2Int>();

            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    char ch = level.At(x, y);
                    var cell = new Vector3Int(x, y, 0);
                    var feet = new Vector3(x + 0.5f, y, 0f);
                    if (ch != '#' && ch != '.' && ch != ':') occupied.Add(new Vector2Int(x, y));

                    switch (ch)
                    {
                        case '#': ground.SetTile(cell, solidTile); break;
                        case '=': platforms.SetTile(cell, solidTile); break;
                        case 'P': player = Spawn(ctx.Prefabs.Player, feet, entities).GetComponent<PlayerController>(); break;
                        case 'A': Spawn(ctx.Prefabs.Altar, feet, entities); break;
                        case 'D': Spawn(ctx.Prefabs.DeepOne, feet, entities); break;
                        case 'C': Spawn(ctx.Prefabs.Cultist, feet, entities); break;
                        case 'V': Spawn(ctx.Prefabs.Eye, feet + Vector3.up * 0.5f, entities); break;
                        case 'B': boss = Spawn(ctx.Prefabs.Boss, feet, entities).GetComponent<BossArchpriest>(); break;
                        case 'S': PlaceProp(ctx, "estatua_madre", feet, props, OrderPropsBack - 1); break;
                        case 'I': PlaceProp(ctx, "idolo", feet, props, OrderPropsBack); break;
                        case 'L': PlaceProp(ctx, "candelabro", feet, props, OrderPropsBack); break;
                        case 'F': PlaceProp(ctx, "farol", feet, props, OrderPropsBack); break;
                        case '$':
                            var coin = Spawn(ctx.Prefabs.Coin, feet + Vector3.up * 0.4f, entities);
                            coin.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                            Record(coin.transform);
                            var gold = coin.GetComponent<GoldPickup>();
                            gold.Configure(30);
                            Record(gold);
                            break;
                        default:
                            if (ch >= '1' && ch <= '9')
                            {
                                var stone = Spawn(ctx.Prefabs.Inscription, feet, entities).GetComponent<LoreInscription>();
                                stone.Configure(level.Text(ch, "Las palabras están demasiado erosionadas para leerlas."));
                                Record(stone);
                            }
                            else if (ch >= 'a' && ch <= 'z')
                            {
                                CreateAreaTrigger(level.Text(ch, "Zona sin nombre"), feet, zones);
                            }
                            break;
                    }
                }
            }
            if (player == null) throw new System.Exception("El mapa " + LevelFile + " no tiene jugador (letra P).");

            BuildPlatforms(ctx, platforms.transform);
            CreateHazards(ctx, hazards);
            var gates = CreateGates(ctx);
            if (boss != null) CreateBossArena(boss, gates);
            Decorate(ctx, props, occupied);

            // Cámara pixel-perfect (640×360 píxeles de arte, escalados a la pantalla).
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = cameraObject.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = HalfViewH;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.02f, 0.02f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            cameraObject.AddComponent<AudioListener>();
            PipelineSetup.ConfigureCamera(cam);
            PipelineSetup.AddPixelPerfect(cam, (int)ArtBaker.PixelsPerUnit, RefWidth, RefHeight);
            var follow = cameraObject.AddComponent<CameraFollow>();
            follow.Configure(player.transform, new Rect(0f, 0f, level.Width, level.Height));
            Vector3 cameraStart = ClampCamera(player.transform.position + new Vector3(1.6f, 1.3f, 0f), level);
            cameraStart.z = -10f;
            cameraObject.transform.position = cameraStart;

            // Post-procesado global (bloom, viñeta, grano) y efectos reactivos.
            var volumeObject = new GameObject("Volumen Global");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.sharedProfile = PipelineSetup.EnsurePostProfile();
            volumeObject.AddComponent<CameraFX>();

            // Luz global (la ambientación por zonas cambia su color e intensidad).
            var lightObject = new GameObject("Luz Global 2D");
            var globalLight = lightObject.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.blendStyleIndex = 0;
            globalLight.color = new Color(0.62f, 0.78f, 0.8f);
            globalLight.intensity = 0.75f;
            globalLight.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();

            // Sistemas.
            var systems = new GameObject("Sistemas");
            systems.AddComponent<InputReader>();
            systems.AddComponent<Sfx>();
            var manager = systems.AddComponent<GameManager>();
            var hud = BuildHud(ctx);

            manager.player = player;
            manager.cameraFollow = follow;
            manager.hud = hud;
            manager.mindFragmentPrefab = ctx.Prefabs.Fragment.GetComponent<MindFragment>();
            manager.additiveMaterial = ctx.Mat.Additive;
            manager.unlitMaterial = ctx.Mat.Unlit;
            manager.Configure(-3f, level.Text('a', "Costa de Innsmouth"));

            var ambience = new GameObject("Ambientacion").AddComponent<ZoneAmbience>();
            ambience.globalLight = globalLight;
            ambience.zones = BuildBackgrounds(ctx, cameraStart);

            var motes = new GameObject("Motas").AddComponent<AmbientMotes>();
            motes.sprite = ctx.Art["mota"];
            motes.material = ctx.Mat.Additive;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Selection.activeGameObject = player.gameObject;
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.in2DMode = true;
                sceneView.FrameSelected();
            }
        }

        static void Record(Object target) => PrefabUtility.RecordPrefabInstancePropertyModifications(target);

        static GameObject Spawn(GameObject prefab, Vector3 position, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Record(go.transform);
            return go;
        }

        static Tile CollisionTile()
        {
            string path = Root + "/Art/ColisionCasilla.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = null;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>Tilemap solo de colisión (sin dibujo: lo que se ve es el terreno pintado).</summary>
        static Tilemap CreateTilemap(string name, Transform parent, int layer, bool oneWay)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            var collider = go.AddComponent<TilemapCollider2D>();
            if (oneWay)
            {
                // Plataformas que se atraviesan desde abajo (y hacia abajo con Abajo + Saltar).
                collider.usedByEffector = true;
                var effector = go.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 170f;
                effector.useSideFriction = false;
                effector.useSideBounce = false;
            }
            return tilemap;
        }

        static void BuildTerrain(Context ctx)
        {
            var solid = new GameObject("Terreno").transform;
            var back = new GameObject("Pared de fondo").transform;
            foreach (var (chunk, sprite, isBack) in ctx.Art.Terrain)
            {
                var go = new GameObject($"Trozo {chunk.TileX},{chunk.TileY}");
                go.transform.SetParent(isBack ? back : solid, false);
                go.transform.position = new Vector3(chunk.TileX, chunk.TileY, 0f);
                AddSprite(go, sprite, ctx.Mat.Lit, isBack ? OrderBackWall : OrderTerrain);
            }
        }

        /// <summary>Tablones de madera sobre cada tramo de plataforma.</summary>
        static void BuildPlatforms(Context ctx, Transform parent)
        {
            var level = ctx.Level;
            foreach (var (start, length, y) in level.Runs('='))
            {
                var go = new GameObject("Tablon");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(start + length * 0.5f, y + 1f, 0f);
                var sr = AddSprite(go, ctx.Art["plataforma"], ctx.Mat.Lit, OrderPlatforms);
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(length, 0.5f);
            }
        }

        static void CreateAreaTrigger(string title, Vector3 position, Transform parent)
        {
            var go = new GameObject("Zona: " + title);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1f, 12f);
            box.offset = new Vector2(0f, 5f);
            go.AddComponent<AreaTitleTrigger>().Configure(title);
        }

        /// <summary>Coral espinoso (^) y agua abisal (~): una pieza por cada tramo horizontal.</summary>
        static void CreateHazards(Context ctx, Transform parent)
        {
            var level = ctx.Level;
            foreach (var (start, length, y) in level.Runs('^'))
            {
                var go = new GameObject("CoralEspinoso");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(start + length * 0.5f, y, 0f);
                var sr = AddSprite(go, ctx.Art["coral_espinas"], ctx.Mat.Lit, OrderHazards);
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(length, 1f);
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(length - 0.2f, 0.5f);
                box.offset = new Vector2(0f, 0.3f);
                go.AddComponent<Hazard>().Configure(20, false);
            }
            foreach (var (start, length, y) in level.Runs('~'))
            {
                bool surface = level.At(start, y + 1) != '~';
                var go = new GameObject(surface ? "AguaAbisal" : "AguaProfunda");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(start + length * 0.5f, y + 0.5f, 0f);
                string anim = surface ? "agua" : "agua_profunda";
                var frames = ctx.Art.Animations[anim];
                var sr = AddSprite(go, frames[0], surface ? ctx.Mat.Lit : ctx.Mat.Unlit, OrderWater);
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(length, 1f);
                go.AddComponent<LoopingSprite>().Configure(frames, ctx.Art.AnimationFps[anim]);
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(length, 0.8f);
                box.offset = new Vector2(0f, -0.1f);
                go.AddComponent<Hazard>().Configure(30, false);
            }
        }

        /// <summary>Rejas (|) de la arena del jefe: una por cada columna vertical.</summary>
        static List<GameObject> CreateGates(Context ctx)
        {
            var level = ctx.Level;
            var gates = new List<GameObject>();
            Transform parent = null;
            foreach (var (x, start, length) in level.VerticalRuns('|'))
            {
                if (parent == null) parent = new GameObject("Rejas").transform;
                var gate = new GameObject("Reja") { layer = ctx.GroundLayer };
                gate.transform.SetParent(parent, false);
                gate.transform.position = new Vector3(x + 0.5f, start + length * 0.5f, 0f);
                var sr = AddSprite(gate, ctx.Art["reja"], ctx.Mat.Lit, OrderHazards);
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(1f, length);
                gate.AddComponent<BoxCollider2D>().size = new Vector2(1f, length);
                gates.Add(gate);
            }
            return gates;
        }

        static void CreateBossArena(BossArchpriest boss, List<GameObject> gates)
        {
            var arena = new GameObject("ArenaDelJefe");
            var box = arena.AddComponent<BoxCollider2D>();
            box.isTrigger = true;

            if (gates.Count >= 2)
            {
                float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
                foreach (var gate in gates)
                {
                    var b = gate.GetComponent<BoxCollider2D>();
                    Vector3 p = gate.transform.position;
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y - b.size.y * 0.5f);
                    maxY = Mathf.Max(maxY, p.y + b.size.y * 0.5f);
                }
                float x0 = minX + 2f, x1 = maxX - 1.5f, y0 = minY, y1 = maxY + 3f;
                arena.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
                box.size = new Vector2(Mathf.Max(1f, x1 - x0), y1 - y0);
            }
            else
            {
                Debug.LogWarning("[Abismo] El jefe no tiene rejas (|) a ambos lados: el combate empezará al acercarte.");
                arena.transform.position = boss.transform.position + Vector3.up * 2f;
                box.size = new Vector2(16f, 6f);
            }

            var component = arena.AddComponent<BossArena>();
            component.boss = boss;
            component.gates = gates.ToArray();
        }

        static Vector3 ClampCamera(Vector3 p, LevelData level)
        {
            p.x = level.Width < HalfViewW * 2f ? level.Width * 0.5f : Mathf.Clamp(p.x, HalfViewW, level.Width - HalfViewW);
            p.y = level.Height < HalfViewH * 2f ? level.Height * 0.5f : Mathf.Clamp(p.y, HalfViewH, level.Height - HalfViewH);
            return p;
        }

        // ------------------------------------------------------------------
        // Decorado
        // ------------------------------------------------------------------

        /// <summary>Coloca un objeto de decorado con sus llamas (y luz cálida) y su brillo.</summary>
        static GameObject PlaceProp(Context ctx, string name, Vector3 position, Transform parent, int order)
        {
            var info = ctx.Art.PropInfo[name];
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            AddSprite(go, ctx.Art[name], info.Unlit ? ctx.Mat.Unlit : ctx.Mat.Lit, order);
            var glow = ctx.Art.Glow(name);
            if (glow != null) AddSprite(Child("Brillo", go, Vector2.zero), glow, ctx.Mat.Emissive, order + 1);
            if (info.FlameAnchors.Count > 0)
            {
                Vector2 center = Vector2.zero;
                bool big = name == "farol";
                foreach (var anchor in info.FlameAnchors)
                {
                    Vector2 local = anchor / ArtBaker.PixelsPerUnit;
                    if (!big) AddFlame(go, ctx, local, false, order + 2);
                    center += local;
                }
                center /= info.FlameAnchors.Count;
                float radius = name == "estatua_madre" ? 6f : big ? 5.5f : 4.2f;
                AddLight(go, center + Vector2.up * 0.2f, new Color(1f, 0.64f, 0.32f), big ? 1.2f : 1.05f, radius, 0.6f, true, 0.22f);
            }
            return go;
        }

        /// <summary>
        /// Decorado automático según la zona: escombros, velas, columnas rotas, cadenas y estandartes que cuelgan
        /// del techo, corales, huesos... (determinista: el mismo mapa da siempre el mismo decorado).
        /// </summary>
        static void Decorate(Context ctx, Transform parent, HashSet<Vector2Int> occupied)
        {
            var level = ctx.Level;
            int lastFloorX = -10, lastCeilX = -10;
            for (int x = 1; x < level.Width - 1; x++)
            {
                var zone = level.ZoneAt(x);
                for (int y = 1; y < level.Height - 1; y++)
                {
                    char here = level.At(x, y);
                    bool air = here == '.' || here == ':';
                    if (!air) continue;
                    float h = PixelCanvas.Hash(x, y, 4242);

                    // Suelo: casilla libre con roca debajo y aire encima.
                    if (level.At(x, y - 1) == '#' && x - lastFloorX >= 3 && !NearOccupied(occupied, x, y, 2) && level.At(x, y + 1) != '#')
                    {
                        string prop = FloorProp(zone, h, level.IsBackWall(x, y));
                        if (prop != null)
                        {
                            PlaceProp(ctx, prop, new Vector3(x + 0.5f, y, 0f), parent, OrderPropsBack);
                            lastFloorX = x;
                        }
                    }
                    // Techo: casilla libre con roca encima (cuelgan cosas).
                    if (level.At(x, y + 1) == '#' && x - lastCeilX >= 4 && level.At(x, y - 1) != '#' && level.At(x, y - 2) != '#')
                    {
                        string prop = CeilingProp(zone, PixelCanvas.Hash(x, y, 777));
                        if (prop != null)
                        {
                            PlaceProp(ctx, prop, new Vector3(x + 0.5f, y + 1f, 0f), parent, OrderPropsHanging);
                            lastCeilX = x;
                        }
                    }
                }
            }

            // Rayos de luz en el santuario y siluetas en primer plano (dan profundidad).
            for (int x = 6; x < level.Width - 6; x += 1)
            {
                var zone = level.ZoneAt(x);
                int floor = level.FloorBelowTop(x);
                if (floor < 0) continue;
                if (zone == Zone.Sanctuary && x % 9 == 4)
                {
                    var shaft = AddSprite(new GameObject("RayoDeLuz"), ctx.Art.Animations["rayo_luz"][0], ctx.Mat.Additive, OrderShafts, new Color(1f, 0.9f, 0.7f, 0.5f));
                    shaft.transform.SetParent(parent, false);
                    shaft.transform.position = new Vector3(x + 0.5f, floor + 7f, 0f);
                    shaft.gameObject.AddComponent<LoopingSprite>().Configure(ctx.Art.Animations["rayo_luz"], ctx.Art.AnimationFps["rayo_luz"]);
                }
                if (x % 37 == 18)
                {
                    string name = level.IsBackWall(x, floor) ? "primer_plano_columna" : "primer_plano_escombros";
                    var fg = PlaceProp(ctx, name, new Vector3(x + 0.5f, floor - 1.5f, 0f), parent, OrderForeground);
                    var parallax = fg.AddComponent<ParallaxLayer>();
                    parallax.Configure(new Vector2(-0.18f, -0.06f), Vector2.zero, 0f);
                    parallax.SetReference(new Vector2(x + 0.5f, floor + 3f)); // en su sitio cuando la cámara lo tiene delante
                }
            }
        }

        static bool NearOccupied(HashSet<Vector2Int> occupied, int x, int y, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
                if (occupied.Contains(new Vector2Int(x + dx, y))) return true;
            return false;
        }

        static string FloorProp(Zone zone, float h, bool indoors)
        {
            switch (zone)
            {
                case Zone.Coast:
                    if (h < 0.05f) return "escombros_a";
                    if (h < 0.075f) return "ancla";
                    if (h < 0.1f) return "red";
                    if (h < 0.12f) return "huesos";
                    if (h < 0.14f) return "coral_a";
                    return null;
                case Zone.Ruins:
                    if (h < 0.06f) return "escombros_b";
                    if (h < 0.1f) return "escombros_c";
                    if (h < 0.14f) return indoors ? "columna_rota" : "escombros_a";
                    if (h < 0.17f) return "velas_suelo";
                    return null;
                case Zone.Sanctuary:
                    if (h < 0.08f) return "velas_suelo";
                    if (h < 0.11f) return "escombros_a";
                    if (h < 0.13f) return "columna_rota";
                    return null;
                default:
                    if (h < 0.07f) return "coral_a";
                    if (h < 0.13f) return "coral_b";
                    if (h < 0.16f) return "huesos";
                    if (h < 0.18f) return "escombros_b";
                    return null;
            }
        }

        static string CeilingProp(Zone zone, float h)
        {
            switch (zone)
            {
                case Zone.Ruins: return h < 0.1f ? "cadenas" : h < 0.13f ? "jaula" : null;
                case Zone.Sanctuary: return h < 0.1f ? "estandarte" : h < 0.15f ? "cadenas" : h < 0.18f ? "jaula" : null;
                default: return null;
            }
        }

        // ------------------------------------------------------------------
        // Fondos con paralaje por zona
        // ------------------------------------------------------------------

        static ZoneAmbience.Zone[] BuildBackgrounds(Context ctx, Vector3 cameraStart)
        {
            var level = ctx.Level;
            var root = new GameObject("Fondos").transform;
            float minCamera = HalfViewW, maxCamera = Mathf.Max(HalfViewW, level.Width - HalfViewW);
            float dMin = minCamera - cameraStart.x, dMax = maxCamera - cameraStart.x;
            float viewBottom = cameraStart.y - HalfViewH;

            var lighting = new Dictionary<Zone, (Color color, float intensity)>
            {
                { Zone.Coast, (new Color(0.62f, 0.8f, 0.82f), 0.78f) },
                { Zone.Ruins, (new Color(0.55f, 0.75f, 0.68f), 0.62f) },
                { Zone.Sanctuary, (new Color(0.9f, 0.76f, 0.62f), 0.66f) },
                { Zone.Reef, (new Color(0.62f, 0.62f, 0.9f), 0.72f) },
            };

            var result = new List<ZoneAmbience.Zone>();
            var starts = level.ZoneStarts;
            for (int z = 0; z < starts.Count; z++)
            {
                var zone = level.ZoneAt(Mathf.Min(level.Width - 1, starts[z]));
                var zoneRoot = new GameObject("Fondo " + zone).transform;
                zoneRoot.SetParent(root, false);
                foreach (var (layer, sprite) in ctx.Art.Backgrounds[zone])
                {
                    var go = new GameObject(layer.Name);
                    go.transform.SetParent(zoneRoot, false);
                    float width = sprite.bounds.size.x, height = sprite.bounds.size.y;
                    float travel = Mathf.Abs((1f - layer.Parallax.x) * (dMax - dMin));
                    float needed = 2f * HalfViewW + travel + 2f * width + 4f;
                    var sr = AddSprite(go, sprite, layer.Lit ? ctx.Mat.Lit : ctx.Mat.Unlit, layer.Order, (Color)layer.Tint);
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.tileMode = SpriteTileMode.Continuous;
                    float tiledWidth = Mathf.Ceil(needed / width) * width;
                    sr.size = new Vector2(tiledWidth, height);
                    float x = cameraStart.x + (1f - layer.Parallax.x) * (dMin + dMax) * 0.5f - tiledWidth * 0.5f;
                    float y = viewBottom + layer.BottomOffset;
                    go.transform.position = new Vector3(x, y, 0f);
                    var parallax = go.AddComponent<ParallaxLayer>();
                    parallax.Configure(layer.Parallax, layer.Scroll, layer.Scroll != Vector2.zero ? width : 0f);
                    parallax.SetReference(cameraStart);

                    // Rellenos para que nunca se vea el final de la capa al subir o bajar la cámara.
                    if (layer.FillBelow.a > 0) Fill(ctx, go, new Vector2(tiledWidth * 0.5f, -20f), new Vector2(tiledWidth, 40f), layer.FillBelow, layer.Order, layer.Lit);
                    if (layer.FillAbove.a > 0) Fill(ctx, go, new Vector2(tiledWidth * 0.5f, height + 20f), new Vector2(tiledWidth, 40f), layer.FillAbove, layer.Order, layer.Lit);
                }
                var light = lighting[zone];
                result.Add(new ZoneAmbience.Zone
                {
                    name = zone.ToString(), startX = starts[z], lightColor = light.color, lightIntensity = light.intensity, backdrop = zoneRoot.gameObject,
                });
            }
            return result.ToArray();
        }

        static void Fill(Context ctx, GameObject parent, Vector2 center, Vector2 size, Color32 color, int order, bool lit)
        {
            var go = Child("Relleno", parent, center);
            var sr = AddSprite(go, ctx.Art["pixel"], lit ? ctx.Mat.Lit : ctx.Mat.Unlit, order, (Color)color);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
        }

        // ------------------------------------------------------------------
        // Interfaz
        // ------------------------------------------------------------------

        static HUD BuildHud(Context ctx)
        {
            var hud = new GameObject("HUD").AddComponent<HUD>();
            var a = ctx.Art;
            hud.portraitSprite = a["ui_retrato"];
            hud.barFrameSprite = a["ui_barra_marco"];
            hud.healthFillSprite = a["ui_barra_vida"];
            hud.revelationFillSprite = a["ui_barra_revelacion"];
            hud.barBackSprite = a["ui_barra_fondo"];
            hud.barTrailSprite = a["ui_barra_rastro"];
            hud.flaskFullSprite = a["ui_frasco_lleno"];
            hud.flaskEmptySprite = a["ui_frasco_vacio"];
            hud.goldSprite = a["ui_oro"];
            hud.goldFrameSprite = a["ui_marco_oro"];
            hud.panelSprite = a["ui_panel"];
            hud.separatorSprite = a["ui_separador"];
            hud.bossFrameSprite = a["ui_jefe_marco"];
            hud.promptSprite = a["ui_aviso"];
            hud.vignetteSprite = a["ui_vineta"];
            hud.signSprite = a["ui_signo"];
            hud.titleFont = a.TitleFont;
            hud.textFont = a.TextFont;
            return hud;
        }

        // ------------------------------------------------------------------
        // Lectura del mapa de texto
        // ------------------------------------------------------------------

        sealed class LevelData
        {
            public int Width, Height;
            char[,] cells;
            readonly Dictionary<char, string> texts = new Dictionary<char, string>();
            public List<int> ZoneStarts = new List<int> { 0 };

            /// <summary>Fuera del mapa todo cuenta como roca sólida.</summary>
            public char At(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? '#' : cells[x, y];

            public string Text(char key, string fallback) => texts.TryGetValue(key, out var t) ? t : fallback;

            /// <summary>Zona (Costa, Ruinas, Santuario, Arrecife) de una columna, según la línea "zonas:" del mapa.</summary>
            public Zone ZoneAt(int x)
            {
                int index = 0;
                for (int i = 0; i < ZoneStarts.Count; i++) if (x >= ZoneStarts[i]) index = i;
                return (Zone)Mathf.Min(index, 3);
            }

            /// <summary>Pared de fondo: ':' y lo que hay dentro de los interiores (enemigos, objetos...).</summary>
            public bool IsBackWall(int x, int y)
            {
                char c = At(x, y);
                if (c == ':') return true;
                if (c == '#' || c == '.') return false;
                return At(x - 1, y) == ':' || At(x + 1, y) == ':' || At(x, y + 1) == ':' || At(x, y - 1) == ':';
            }

            /// <summary>La primera casilla de aire sobre el suelo más bajo de una columna (o -1).</summary>
            public int FloorBelowTop(int x)
            {
                for (int y = 1; y < Height; y++)
                    if (At(x, y - 1) == '#' && At(x, y) != '#') return y;
                return -1;
            }

            /// <summary>Tramos horizontales de un carácter: (inicio, longitud, fila).</summary>
            public IEnumerable<(int start, int length, int y)> Runs(char ch)
            {
                for (int y = 0; y < Height; y++)
                {
                    int x = 0;
                    while (x < Width)
                    {
                        if (At(x, y) != ch) { x++; continue; }
                        int start = x;
                        while (x < Width && At(x, y) == ch) x++;
                        yield return (start, x - start, y);
                    }
                }
            }

            /// <summary>Tramos verticales de un carácter: (columna, inicio, longitud).</summary>
            public IEnumerable<(int x, int start, int length)> VerticalRuns(char ch)
            {
                for (int x = 0; x < Width; x++)
                {
                    int y = 0;
                    while (y < Height)
                    {
                        if (At(x, y) != ch) { y++; continue; }
                        int start = y;
                        while (y < Height && At(x, y) == ch) y++;
                        yield return (x, start, y - start);
                    }
                }
            }

            public static LevelData Load(string path)
            {
                if (!File.Exists(path)) throw new System.Exception("No existe el mapa " + path);
                var data = new LevelData();
                var rows = new List<string>();
                bool definitions = false;
                foreach (var raw in File.ReadAllLines(path))
                {
                    string line = raw.TrimEnd('\r');
                    if (line.StartsWith("//")) continue;
                    if (line.StartsWith("---"))
                    {
                        definitions = true;
                        continue;
                    }
                    if (definitions)
                    {
                        if (line.StartsWith("zonas:"))
                        {
                            data.ZoneStarts = line.Substring(6).Split(new[] { ' ', ',', '\t' }, System.StringSplitOptions.RemoveEmptyEntries)
                                .Select(v => int.TryParse(v, out int n) ? n : 0).OrderBy(n => n).ToList();
                            if (data.ZoneStarts.Count == 0) data.ZoneStarts.Add(0);
                        }
                        else if (line.Length >= 2 && line[1] == ':')
                        {
                            data.texts[line[0]] = line.Substring(2).Trim().Replace("\\n", "\n");
                        }
                        continue;
                    }
                    if (rows.Count == 0 && line.Trim().Length == 0) continue;
                    rows.Add(line);
                }
                while (rows.Count > 0 && rows[rows.Count - 1].Trim().Length == 0) rows.RemoveAt(rows.Count - 1);
                if (rows.Count == 0) throw new System.Exception("El mapa " + path + " está vacío.");

                data.Height = rows.Count;
                foreach (var r in rows) data.Width = Mathf.Max(data.Width, r.Length);
                data.cells = new char[data.Width, data.Height];
                for (int row = 0; row < rows.Count; row++)
                {
                    for (int x = 0; x < data.Width; x++)
                    {
                        char ch = x < rows[row].Length ? rows[row][x] : '.';
                        data.cells[x, data.Height - 1 - row] = ch == ' ' ? '.' : ch;
                    }
                }
                return data;
            }
        }
    }
}
