using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Menú "Abismo" de la barra superior de Unity. Con un clic crea:
    ///   1. Las capas (Layers) que usa el juego.
    ///   2. Los sprites provisionales (PNG) en Assets/_Abismo/Art/Generated.
    ///   3. Materiales, tiles y prefabs (jugador, enemigos, jefe, altar...).
    ///   4. La escena Assets/_Abismo/Scenes/Nivel_01 a partir del mapa de texto
    ///      Assets/_Abismo/Levels/nivel_01.txt (¡edítalo para diseñar tu propio nivel!).
    /// </summary>
    public static class AbismoBuilder
    {
        const string Root = "Assets/_Abismo";
        const string ArtFolder = Root + "/Art/Generated";
        const string TileFolder = Root + "/Art/Tiles";
        const string MaterialFolder = Root + "/Materials";
        const string PrefabFolder = Root + "/Prefabs";
        const string SceneFolder = Root + "/Scenes";
        const string LevelFile = Root + "/Levels/nivel_01.txt";
        const string ScenePath = SceneFolder + "/Nivel_01.unity";

        const float CameraSize = 7f;
        const float MaxHalfWidth = 16f; // cubre pantallas de hasta 21:9

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

        [MenuItem("Abismo/Restablecer sprites provisionales y construir", false, 21)]
        public static void ResetArtAndBuild()
        {
            bool ok = EditorUtility.DisplayDialog("Abismo",
                "Esto SOBRESCRIBE los PNG de " + ArtFolder + " con el arte provisional.\n" +
                "Si has pintado tu propio arte encima, se perderá.", "Sobrescribir", "Cancelar");
            if (ok) Build(true, false);
        }

        // ------------------------------------------------------------------
        // Proceso completo
        // ------------------------------------------------------------------

        static void Build(bool overwriteArt, bool overwritePrefabs)
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Abismo", "Sal del modo Play antes de construir.", "Vale");
                return;
            }
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Abismo",
                    "Se volverá a crear la escena Nivel_01 a partir de " + LevelFile + ".\n" +
                    "Los cambios hechos A MANO en la escena se perderán (tus prefabs, scripts y PNG no se tocan).\n\n¿Continuar?",
                    "Construir", "Cancelar"))
            {
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                Progress("Creando capas", 0.02f);
                int groundLayer = EnsureLayer(GameLayers.Ground);
                int playerLayer = EnsureLayer(GameLayers.Player);
                int enemyLayer = EnsureLayer(GameLayers.Enemy);
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
                Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
                EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

                foreach (var folder in new[] { ArtFolder, TileFolder, MaterialFolder, PrefabFolder, SceneFolder }) EnsureFolder(folder);

                var art = GenerateArt(overwriteArt);
                Progress("Creando materiales y tiles", 0.45f);
                var materials = CreateMaterials();
                var tiles = CreateTiles(art);
                Progress("Creando prefabs", 0.55f);
                var prefabs = CreatePrefabs(art, materials, playerLayer, enemyLayer, overwritePrefabs);
                Progress("Construyendo el nivel", 0.75f);
                BuildScene(art, materials, tiles, prefabs, groundLayer);
                AssetDatabase.SaveAssets();

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Abismo",
                    "¡Listo! Se ha creado y abierto la escena Nivel_01.\n\nPulsa el botón ▶ (Play) para jugar.\n" +
                    "Controles: A/D mover · Espacio saltar · J atacar · L esquivar · I parar · F curarse · U conjuro · E interactuar · Esc pausa",
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

        static void EnsureFolder(string path)
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
        // Arte
        // ------------------------------------------------------------------

        class Art
        {
            readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

            public void Add(string name, Sprite sprite) => sprites[name] = sprite;

            public Sprite this[string name]
            {
                get
                {
                    if (sprites.TryGetValue(name, out var sprite) && sprite != null) return sprite;
                    throw new System.Exception("No se encontró el sprite '" + name + "' en " + ArtFolder + ".");
                }
            }
        }

        static Art GenerateArt(bool overwrite)
        {
            var specs = AbismoArt.All();
            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                string path = ArtFolder + "/" + spec.Name + ".png";
                if (!overwrite && File.Exists(path)) continue;
                Progress("Dibujando " + spec.Name, 0.05f + 0.3f * i / specs.Count);
                File.WriteAllBytes(path, EncodePng(spec.Draw()));
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var art = new Art();
            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                string path = ArtFolder + "/" + spec.Name + ".png";
                Progress("Importando " + spec.Name, 0.35f + 0.1f * i / specs.Count);
                ConfigureImporter(path, spec);
                art.Add(spec.Name, AssetDatabase.LoadAssetAtPath<Sprite>(path));
            }
            return art;
        }

        static byte[] EncodePng(PixelCanvas canvas)
        {
            var texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false);
            texture.SetPixels32(canvas.Pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }

        static void ConfigureImporter(string path, SpriteSpec spec)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)spec.Alignment;
            settings.spritePivot = spec.Pivot;
            settings.spritePixelsPerUnit = spec.PixelsPerUnit;
            settings.filterMode = spec.Smooth ? FilterMode.Bilinear : FilterMode.Point;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------
        // Materiales y tiles
        // ------------------------------------------------------------------

        class Materials
        {
            public Material Flash;
            public Material Additive;
        }

        static Materials CreateMaterials() => new Materials
        {
            Flash = MaterialAsset("SpriteFlash", "Abismo/SpriteFlash"),
            Additive = MaterialAsset("SpriteAdditive", "Abismo/SpriteAdditive"),
        };

        static Material MaterialAsset(string name, string shaderName)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("[Abismo] No se encontró el shader " + shaderName + "; se usa Sprites/Default.");
                shader = Shader.Find("Sprites/Default");
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        class Tiles
        {
            public TileBase Top, InnerA, InnerB, Back, Platform;
        }

        static Tiles CreateTiles(Art art) => new Tiles
        {
            Top = TileAsset("SueloSuperior", art[AbismoArt.GroundTop], Tile.ColliderType.Grid),
            InnerA = TileAsset("SueloInteriorA", art[AbismoArt.GroundA], Tile.ColliderType.Grid),
            InnerB = TileAsset("SueloInteriorB", art[AbismoArt.GroundB], Tile.ColliderType.Grid),
            Back = TileAsset("ParedDeFondo", art[AbismoArt.BackWall], Tile.ColliderType.None),
            Platform = TileAsset("Plataforma", art[AbismoArt.Platform], Tile.ColliderType.Grid),
        };

        static Tile TileAsset(string name, Sprite sprite, Tile.ColliderType collider)
        {
            string path = TileFolder + "/" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprite;
            tile.colliderType = collider;
            tile.color = Color.white;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        // ------------------------------------------------------------------
        // Prefabs
        // ------------------------------------------------------------------

        class Prefabs
        {
            public GameObject Player, DeepOne, Cultist, Eye, Boss;
            public GameObject Orb, Spell, Coin, Fragment, Tentacle, Altar, Inscription;
        }

        static GameObject Child(string name, GameObject parent, Vector2 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        static Material DefaultSpriteMaterial => AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

        static SpriteRenderer AddSprite(GameObject go, Sprite sprite, Material material, int order, Color? color = null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (material == null) material = DefaultSpriteMaterial;
            if (material != null) sr.sharedMaterial = material;
            sr.sortingOrder = order;
            if (color.HasValue) sr.color = color.Value;
            return sr;
        }

        static SpriteRenderer AddGlow(GameObject parent, Art art, Materials materials, Vector2 position, float scale, Color color, int order)
        {
            var glow = AddSprite(Child("Brillo", parent, position), art[AbismoArt.Glow], materials.Additive, order, color);
            glow.transform.localScale = new Vector3(scale, scale, 1f);
            return glow;
        }

        static GameObject SavePrefab(GameObject go, string fileName)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/" + fileName + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>
        /// Si el prefab ya existe se reutiliza tal cual (con los ajustes que le hayas hecho);
        /// solo se crea si falta o si se pide restablecerlo.
        /// </summary>
        static GameObject GetOrBuild(string fileName, bool overwrite, System.Func<GameObject> build)
        {
            string path = PrefabFolder + "/" + fileName + ".prefab";
            if (!overwrite)
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) return existing;
            }
            return build();
        }

        static Prefabs CreatePrefabs(Art art, Materials materials, int playerLayer, int enemyLayer, bool overwrite)
        {
            var p = new Prefabs();
            p.Orb = GetOrBuild("Orbe", overwrite, () =>
                BuildProjectile("Orbe", art[AbismoArt.Orb], art, materials, new Color(0.85f, 0.45f, 1f), 6f, 12, 0.3f, 6f, false, 0f, 1f));
            p.Spell = GetOrBuild("SignoArcano", overwrite, () =>
                BuildProjectile("SignoArcano", art[AbismoArt.Spell], art, materials, new Color(0.5f, 1f, 0.85f), 11f, 25, 0.5f, 1.2f, true, -540f, 1.2f));
            p.Coin = GetOrBuild("Moneda", overwrite, () => BuildCoin(art, materials));
            p.Fragment = GetOrBuild("FragmentoDeMente", overwrite, () => BuildFragment(art, materials));
            p.Tentacle = GetOrBuild("Tentaculo", overwrite, () => BuildTentacle(art, materials));
            p.Altar = GetOrBuild("AltarDelSignoAntiguo", overwrite, () => BuildAltar(art, materials));
            p.Inscription = GetOrBuild("Inscripcion", overwrite, () => BuildInscription(art));
            p.Player = GetOrBuild("Jugador", overwrite, () => BuildPlayer(art, materials, p, playerLayer));

            var goldPrefab = p.Coin.GetComponent<GoldPickup>();
            p.DeepOne = GetOrBuild("Profundo", overwrite, () => BuildDeepOne(art, materials, enemyLayer, goldPrefab));
            p.Cultist = GetOrBuild("Sectario", overwrite, () => BuildCultist(art, materials, enemyLayer, goldPrefab, p));
            p.Eye = GetOrBuild("OjoDelVacio", overwrite, () => BuildEye(art, materials, enemyLayer, goldPrefab));
            p.Boss = GetOrBuild("Arcipreste", overwrite, () => BuildBoss(art, materials, enemyLayer, goldPrefab, p));
            return p;
        }

        static GameObject BuildDeepOne(Art art, Materials materials, int enemyLayer, GoldPickup goldPrefab)
        {
            var deepOne = EnemyBody("Profundo", art[AbismoArt.DeepOne], materials, enemyLayer, new Vector2(0.9f, 1.5f), false);
            var deepOneAI = deepOne.Root.AddComponent<DeepOneEnemy>();
            Wire(deepOneAI, deepOne, goldPrefab);
            deepOneAI.ConfigureStats("Profundo", 45, 20, 0f, true);
            return SavePrefab(deepOne.Root, "Profundo");
        }

        static GameObject BuildCultist(Art art, Materials materials, int enemyLayer, GoldPickup goldPrefab, Prefabs p)
        {
            var cultist = EnemyBody("Sectario", art[AbismoArt.Cultist], materials, enemyLayer, new Vector2(0.8f, 1.8f), false);
            AddGlow(cultist.Visual, art, materials, new Vector2(0.34f, 1.75f), 0.25f, new Color(1f, 0.55f, 0.25f, 0.45f), 4);
            var cultistAI = cultist.Root.AddComponent<CultistEnemy>();
            Wire(cultistAI, cultist, goldPrefab);
            cultistAI.projectilePrefab = p.Orb.GetComponent<Projectile>();
            cultistAI.ConfigureStats("Sectario", 30, 25, 0f, true);
            return SavePrefab(cultist.Root, "Sectario");
        }

        static GameObject BuildEye(Art art, Materials materials, int enemyLayer, GoldPickup goldPrefab)
        {
            var eye = EnemyBody("OjoDelVacio", art[AbismoArt.Eye], materials, enemyLayer, new Vector2(0.9f, 0.9f), true);
            AddGlow(eye.Visual, art, materials, new Vector2(0f, 0.19f), 0.45f, new Color(1f, 0.3f, 0.35f, 0.25f), 4);
            var eyeAI = eye.Root.AddComponent<FlyingEyeEnemy>();
            Wire(eyeAI, eye, goldPrefab);
            eyeAI.ConfigureStats("Ojo del Vacío", 22, 15, 0f, true);
            return SavePrefab(eye.Root, "OjoDelVacio");
        }

        static GameObject BuildBoss(Art art, Materials materials, int enemyLayer, GoldPickup goldPrefab, Prefabs p)
        {
            var boss = EnemyBody("Arcipreste", art[AbismoArt.Boss], materials, enemyLayer, new Vector2(1.9f, 3.6f), false);
            AddGlow(boss.Visual, art, materials, new Vector2(0.34f, 2.9f), 0.2f, new Color(0.85f, 1f, 0.45f, 0.5f), 4);
            AddGlow(boss.Visual, art, materials, new Vector2(0.03f, 3.45f), 0.45f, new Color(0.45f, 1f, 0.8f, 0.35f), 4);
            var bossAI = boss.Root.AddComponent<BossArchpriest>();
            Wire(bossAI, boss, goldPrefab);
            bossAI.tentaclePrefab = p.Tentacle.GetComponent<TentacleStrike>();
            bossAI.orbPrefab = p.Orb.GetComponent<Projectile>();
            bossAI.ConfigureStats("El Arcipreste de las Mareas", 420, 300, 1f, false);
            return SavePrefab(boss.Root, "Arcipreste");
        }

        static GameObject BuildProjectile(string name, Sprite sprite, Art art, Materials materials, Color color,
                                          float speed, int damage, float radius, float life, bool piercing, float spin, float scale)
        {
            var root = new GameObject(name);
            AddGlow(root, art, materials, Vector2.zero, 0.3f * scale, new Color(color.r, color.g, color.b, 0.45f), 29);
            var sr = AddSprite(Child("Sprite", root, Vector2.zero), sprite, materials.Additive, 30, color);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            var projectile = root.AddComponent<Projectile>();
            projectile.spriteRenderer = sr;
            projectile.Configure(speed, damage, radius, life, piercing, spin);
            return SavePrefab(root, name);
        }

        static GameObject BuildCoin(Art art, Materials materials)
        {
            var root = new GameObject("Moneda");
            var visual = Child("Visual", root, Vector2.zero);
            AddSprite(visual, art[AbismoArt.Coin], null, 15);
            AddGlow(visual, art, materials, Vector2.zero, 0.18f, new Color(1f, 0.8f, 0.3f, 0.3f), 14);
            root.AddComponent<GoldPickup>().visual = visual.transform;
            return SavePrefab(root, "Moneda");
        }

        static GameObject BuildFragment(Art art, Materials materials)
        {
            var root = new GameObject("FragmentoDeMente");
            var visual = Child("Visual", root, Vector2.zero);
            AddSprite(visual, art[AbismoArt.Fragment], null, 15);
            AddGlow(visual, art, materials, Vector2.zero, 0.5f, new Color(0.65f, 0.45f, 1f, 0.5f), 14);
            root.AddComponent<MindFragment>().visual = visual.transform;
            return SavePrefab(root, "FragmentoDeMente");
        }

        static GameObject BuildTentacle(Art art, Materials materials)
        {
            var root = new GameObject("Tentaculo");
            var warning = AddSprite(Child("Aviso", root, Vector2.zero), art[AbismoArt.Glow], materials.Additive, 3, new Color(1f, 0.15f, 0.1f, 0.5f));
            warning.transform.localScale = new Vector3(0.55f, 0.15f, 1f);
            var body = Child("Cuerpo", root, Vector2.zero);
            AddSprite(body, art[AbismoArt.Tentacle], null, 6);
            var strike = root.AddComponent<TentacleStrike>();
            strike.tentacle = body.transform;
            strike.warning = warning;
            return SavePrefab(root, "Tentaculo");
        }

        static GameObject BuildAltar(Art art, Materials materials)
        {
            var root = new GameObject("AltarDelSignoAntiguo");
            AddSprite(Child("Sprite", root, Vector2.zero), art[AbismoArt.Altar], null, 2);
            var glow = AddGlow(root, art, materials, new Vector2(0f, 1.95f), 0.9f, new Color(0.45f, 1f, 0.8f, 0.1f), 3);
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2.4f, 2.6f);
            box.offset = new Vector2(0f, 1.3f);
            root.AddComponent<ElderSignAltar>().glow = glow;
            return SavePrefab(root, "AltarDelSignoAntiguo");
        }

        static GameObject BuildInscription(Art art)
        {
            var root = new GameObject("Inscripcion");
            AddSprite(Child("Sprite", root, Vector2.zero), art[AbismoArt.Inscription], null, 2);
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.8f, 2.2f);
            box.offset = new Vector2(0f, 1.1f);
            root.AddComponent<LoreInscription>();
            return SavePrefab(root, "Inscripcion");
        }

        static GameObject BuildPlayer(Art art, Materials materials, Prefabs prefabs, int layer)
        {
            var root = new GameObject("Jugador") { layer = layer };
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
            var flash = root.AddComponent<FlashEffect>();

            var visual = Child("Visual", root, Vector2.zero);
            var body = AddSprite(visual, art[AbismoArt.Player], materials.Flash, 10);
            AddGlow(visual, art, materials, new Vector2(0.1f, 1.53f), 0.3f, new Color(0.35f, 1f, 0.75f, 0.3f), 9);
            var pivot = Child("ArmaPivote", visual, new Vector2(0.4f, 0.9f));
            var weapon = AddSprite(Child("Arma", pivot, Vector2.zero), art[AbismoArt.Weapon], materials.Flash, 11);
            var slash = AddSprite(Child("Tajo", visual, new Vector2(1f, 1f)), art[AbismoArt.Slash], materials.Additive, 12, new Color(0.75f, 1f, 0.92f, 1f));
            slash.enabled = false;

            controller.visual = visual.transform;
            controller.bodyRenderer = body;
            controller.weaponPivot = pivot.transform;
            controller.weaponRenderer = weapon;
            controller.slashRenderer = slash;
            controller.spellPrefab = prefabs.Spell.GetComponent<Projectile>();
            controller.flash = flash;
            flash.renderers = new[] { body, weapon };
            return SavePrefab(root, "Jugador");
        }

        class EnemyParts
        {
            public GameObject Root, Visual;
            public FlashEffect Flash;
        }

        static EnemyParts EnemyBody(string name, Sprite sprite, Materials materials, int layer, Vector2 size, bool flying)
        {
            var root = new GameObject(name) { layer = layer };
            var rb = root.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (flying)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                var circle = root.AddComponent<CircleCollider2D>();
                circle.radius = size.x * 0.5f;
                circle.offset = new Vector2(0f, 0.15f);
            }
            else
            {
                rb.gravityScale = 3f;
                var capsule = root.AddComponent<CapsuleCollider2D>();
                capsule.size = size;
                capsule.offset = new Vector2(0f, size.y * 0.5f);
            }

            var visual = Child("Visual", root, Vector2.zero);
            var body = AddSprite(visual, sprite, materials.Flash, 5);
            var flash = root.AddComponent<FlashEffect>();
            flash.renderers = new[] { body };
            return new EnemyParts { Root = root, Visual = visual, Flash = flash };
        }

        static void Wire(Enemy enemy, EnemyParts parts, GoldPickup gold)
        {
            enemy.visual = parts.Visual.transform;
            enemy.flash = parts.Flash;
            enemy.goldPrefab = gold;
        }

        // ------------------------------------------------------------------
        // Escena
        // ------------------------------------------------------------------

        static void BuildScene(Art art, Materials materials, Tiles tiles, Prefabs prefabs, int groundLayer)
        {
            var level = LevelData.Load(LevelFile);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var grid = new GameObject("Nivel");
            grid.AddComponent<Grid>();
            var back = CreateTilemap("ParedDeFondo", grid.transform, -10, 0, false, false);
            var ground = CreateTilemap("Suelo", grid.transform, 0, groundLayer, true, false);
            var platforms = CreateTilemap("Plataformas", grid.transform, 1, groundLayer, true, true);

            var entities = new GameObject("Entidades").transform;
            var hazards = new GameObject("Peligros").transform;
            var zones = new GameObject("Zonas").transform;

            PlayerController player = null;
            BossArchpriest boss = null;

            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    char ch = level.At(x, y);
                    var cell = new Vector3Int(x, y, 0);
                    var feet = new Vector3(x + 0.5f, y, 0f);

                    // Detrás de plataformas, enemigos y objetos de interiores también va pared de fondo.
                    bool needsBackWall = ch != '#' && ch != ':' && ch != '.';
                    bool indoors = level.At(x - 1, y) == ':' || level.At(x + 1, y) == ':' || level.At(x, y + 1) == ':' || level.At(x, y - 1) == ':';
                    if (needsBackWall && indoors) back.SetTile(cell, tiles.Back);

                    switch (ch)
                    {
                        case '#':
                            var tile = level.At(x, y + 1) == '#'
                                ? (PixelCanvas.Hash(x, y, 1) < 0.5f ? tiles.InnerA : tiles.InnerB)
                                : tiles.Top;
                            ground.SetTile(cell, tile);
                            break;
                        case ':': back.SetTile(cell, tiles.Back); break;
                        case '=': platforms.SetTile(cell, tiles.Platform); break;
                        case 'P': player = Spawn(prefabs.Player, feet, entities).GetComponent<PlayerController>(); break;
                        case 'A': Spawn(prefabs.Altar, feet, entities); break;
                        case 'D': Spawn(prefabs.DeepOne, feet, entities); break;
                        case 'C': Spawn(prefabs.Cultist, feet, entities); break;
                        case 'V': Spawn(prefabs.Eye, feet + Vector3.up * 0.5f, entities); break;
                        case 'B': boss = Spawn(prefabs.Boss, feet, entities).GetComponent<BossArchpriest>(); break;
                        case '$':
                            var coin = Spawn(prefabs.Coin, feet + Vector3.up * 0.4f, entities);
                            coin.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
                            Record(coin.transform);
                            var gold = coin.GetComponent<GoldPickup>();
                            gold.Configure(30);
                            Record(gold);
                            break;
                        default:
                            if (ch >= '1' && ch <= '9')
                            {
                                var stone = Spawn(prefabs.Inscription, feet, entities).GetComponent<LoreInscription>();
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

            CreateHazards(level, art, hazards);
            var gates = CreateGates(level, art, groundLayer);
            if (boss != null) CreateBossArena(boss, gates);

            // Cámara
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = cameraObject.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.035f, 0.04f);
            cameraObject.AddComponent<AudioListener>();
            var follow = cameraObject.AddComponent<CameraFollow>();
            follow.Configure(player.transform, new Rect(0f, 0f, level.Width, level.Height));
            Vector3 cameraStart = ClampCamera(player.transform.position + new Vector3(1.8f, 1.6f, 0f), level);
            cameraStart.z = -10f;
            cameraObject.transform.position = cameraStart;

            // Sistemas
            var systems = new GameObject("Sistemas");
            systems.AddComponent<InputReader>();
            systems.AddComponent<Sfx>();
            var manager = systems.AddComponent<GameManager>();

            var hudObject = new GameObject("HUD");
            var hud = hudObject.AddComponent<HUD>();
            hud.flaskFullSprite = art[AbismoArt.FlaskFull];
            hud.flaskEmptySprite = art[AbismoArt.FlaskEmpty];
            hud.goldSprite = art[AbismoArt.Coin];
            hud.vignetteSprite = art[AbismoArt.Vignette];

            manager.player = player;
            manager.cameraFollow = follow;
            manager.hud = hud;
            manager.mindFragmentPrefab = prefabs.Fragment.GetComponent<MindFragment>();
            manager.additiveMaterial = materials.Additive;
            manager.Configure(-3f, level.Text('a', "Costa de Innsmouth"));

            BuildBackground(art, materials, cam, cameraStart, level);
            var motes = new GameObject("Motas").AddComponent<AmbientMotes>();
            motes.sprite = art[AbismoArt.Mote];
            motes.material = materials.Additive;

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

        static Tilemap CreateTilemap(string name, Transform parent, int order, int layer, bool solid, bool oneWay)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            var tilemapRenderer = go.AddComponent<TilemapRenderer>();
            tilemapRenderer.sortingOrder = order;
            if (DefaultSpriteMaterial != null) tilemapRenderer.sharedMaterial = DefaultSpriteMaterial;
            if (solid)
            {
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
            }
            return tilemap;
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
        static void CreateHazards(LevelData level, Art art, Transform parent)
        {
            for (int y = 0; y < level.Height; y++)
            {
                int x = 0;
                while (x < level.Width)
                {
                    char ch = level.At(x, y);
                    if (ch != '^' && ch != '~')
                    {
                        x++;
                        continue;
                    }
                    int start = x;
                    while (x < level.Width && level.At(x, y) == ch) x++;
                    int length = x - start;
                    bool spikes = ch == '^';

                    var go = new GameObject(spikes ? "CoralEspinoso" : "AguaAbisal");
                    go.transform.SetParent(parent, false);
                    go.transform.position = new Vector3(start + length * 0.5f, y + 0.5f, 0f);
                    Sprite sprite = spikes ? art[AbismoArt.Spikes]
                        : (level.At(start, y + 1) == '~' ? art[AbismoArt.Water] : art[AbismoArt.WaterTop]);
                    var sr = AddSprite(go, sprite, null, spikes ? 1 : 3);
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.tileMode = SpriteTileMode.Continuous;
                    sr.size = new Vector2(length, 1f);

                    var box = go.AddComponent<BoxCollider2D>();
                    box.isTrigger = true;
                    box.size = spikes ? new Vector2(length - 0.2f, 0.5f) : new Vector2(length, 0.8f);
                    box.offset = spikes ? new Vector2(0f, -0.2f) : new Vector2(0f, -0.1f);
                    go.AddComponent<Hazard>().Configure(spikes ? 20 : 30, false);
                }
            }
        }

        /// <summary>Rejas (|) de la arena del jefe: una por cada columna vertical.</summary>
        static List<GameObject> CreateGates(LevelData level, Art art, int groundLayer)
        {
            var gates = new List<GameObject>();
            Transform parent = null;
            for (int x = 0; x < level.Width; x++)
            {
                int y = 0;
                while (y < level.Height)
                {
                    if (level.At(x, y) != '|')
                    {
                        y++;
                        continue;
                    }
                    int start = y;
                    while (y < level.Height && level.At(x, y) == '|') y++;
                    int length = y - start;

                    if (parent == null) parent = new GameObject("Rejas").transform;
                    var gate = new GameObject("Reja") { layer = groundLayer };
                    gate.transform.SetParent(parent, false);
                    gate.transform.position = new Vector3(x + 0.5f, start + length * 0.5f, 0f);
                    var sr = AddSprite(gate, art[AbismoArt.Gate], null, 4);
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.tileMode = SpriteTileMode.Continuous;
                    sr.size = new Vector2(1f, length);
                    gate.AddComponent<BoxCollider2D>().size = new Vector2(1f, length);
                    gates.Add(gate);
                }
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
            float halfH = CameraSize, halfW = CameraSize * 16f / 9f;
            p.x = level.Width < halfW * 2f ? level.Width * 0.5f : Mathf.Clamp(p.x, halfW, level.Width - halfW);
            p.y = level.Height < halfH * 2f ? level.Height * 0.5f : Mathf.Clamp(p.y, halfH, level.Height - halfH);
            return p;
        }

        // ------------------------------------------------------------------
        // Fondo con paralaje
        // ------------------------------------------------------------------

        static void BuildBackground(Art art, Materials materials, Camera cam, Vector3 cameraStart, LevelData level)
        {
            var root = new GameObject("Fondo").transform;
            float halfW = CameraSize * 16f / 9f;
            float minCamera = halfW, maxCamera = Mathf.Max(halfW, level.Width - halfW);
            float dMin = minCamera - cameraStart.x, dMax = maxCamera - cameraStart.x;

            // Cielo: hijo de la cámara, así siempre la cubre.
            var sky = new GameObject("Cielo");
            sky.transform.SetParent(cam.transform, false);
            sky.transform.localPosition = new Vector3(0f, 0f, 50f);
            AddSprite(sky, art[AbismoArt.Sky], null, -100);
            Vector3 skySize = art[AbismoArt.Sky].bounds.size;
            sky.transform.localScale = new Vector3(MaxHalfWidth * 2.2f / skySize.x, CameraSize * 2.4f / skySize.y, 1f);

            void Layer(string name, string spriteName, int order, Vector2 factor, float relativeX, float y, bool tiled,
                       Color color, Vector2 scroll, Material material = null)
            {
                Sprite sprite = art[spriteName];
                var go = new GameObject(name);
                go.transform.SetParent(root, false);
                var sr = AddSprite(go, sprite, material, order, color);
                float width = sprite.bounds.size.x;
                float x = cameraStart.x + relativeX;
                if (tiled)
                {
                    float travel = Mathf.Abs((1f - factor.x) * (dMax - dMin));
                    float needed = 2f * MaxHalfWidth + travel + 2f * width + 4f;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.tileMode = SpriteTileMode.Continuous;
                    sr.size = new Vector2(Mathf.Ceil(needed / width) * width, sprite.bounds.size.y);
                    x = cameraStart.x + (1f - factor.x) * (dMin + dMax) * 0.5f;
                }
                go.transform.position = new Vector3(x, y, 0f);
                go.AddComponent<ParallaxLayer>().Configure(factor, scroll, tiled ? width : 0f);
            }

            float cy = cameraStart.y;
            Layer("Luna", AbismoArt.Moon, -98, new Vector2(0.98f, 0.97f), -7f, cy + 3.5f, false, Color.white, Vector2.zero);
            Layer("ElDurmiente", AbismoArt.Colossus, -97, new Vector2(0.95f, 0.93f), 6f, cy - 7.2f, false, Color.white, Vector2.zero);
            Layer("CiudadCiclopea", AbismoArt.FarCity, -95, new Vector2(0.88f, 0.9f), 0f, cy - 7.5f, true, Color.white, Vector2.zero);
            Layer("Ruinas", AbismoArt.Ruins, -90, new Vector2(0.7f, 0.8f), 0f, cy - 8f, true, Color.white, Vector2.zero);
            Layer("NieblaLejana", AbismoArt.Fog, -85, new Vector2(0.6f, 0.75f), 0f, cy - 4f, true,
                  new Color(0.6f, 0.95f, 0.85f, 0.18f), new Vector2(0.25f, 0f));
            Layer("NieblaCercana", AbismoArt.Fog, 50, new Vector2(-0.25f, 1f), 0f, cy - 5.5f, true,
                  new Color(0.65f, 0.95f, 0.9f, 0.08f), new Vector2(0.6f, 0f));
        }

        // ------------------------------------------------------------------
        // Lectura del mapa de texto
        // ------------------------------------------------------------------

        class LevelData
        {
            public int Width, Height;
            char[,] cells;
            readonly Dictionary<char, string> texts = new Dictionary<char, string>();

            /// <summary>Fuera del mapa todo cuenta como roca sólida.</summary>
            public char At(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? '#' : cells[x, y];

            public string Text(char key, string fallback) => texts.TryGetValue(key, out var t) ? t : fallback;

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
                        if (line.Length >= 2 && line[1] == ':') data.texts[line[0]] = line.Substring(2).Trim().Replace("\\n", "\n");
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
