#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroGame.Map3.Editor
{
    public static class CaveEnemyBuilder
    {
        private const string Art = "Assets/Art/Monster/Map3";
        private const string Prefabs = "Assets/Prefabs/Monster/Map3";
        private const string DemoScene = "Assets/Scenes/Map3/Map3_EnemyDemo.unity";
        [Serializable] private class Manifest { public Character[] characters; }
        [Serializable] private class Character
        {
            public string name;
            public float ppu, pivotX, pivotY, bodyWidth, bodyHeight, bodyCenterY;
            public StateFrames[] states;
        }
        [Serializable] private class StateFrames { public string name; public int count; }

        [MenuItem("Tools/Map3 Cave/1. Build Enemy Prefabs and Animations")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode before building."); return; }
            string manifestPath = Art + "/enemy_manifest.json";
            if (!File.Exists(manifestPath)) throw new FileNotFoundException("Import the entire Map3_Enemies Assets folder first.", manifestPath);
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            int groundLayer = EnsureGroundLayer();
            EnsureFolder(Prefabs); EnsureFolder(Art + "/Shared");
            PhysicsMaterial2D physics = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Art + "/Shared/EnemyNoFriction.physicsMaterial2D");
            if (!physics)
            {
                physics = new PhysicsMaterial2D("EnemyNoFriction");
                AssetDatabase.CreateAsset(physics, Art + "/Shared/EnemyNoFriction.physicsMaterial2D");
            }
            physics.friction = 0; physics.bounciness = 0; EditorUtility.SetDirty(physics);
            foreach (Character character in manifest.characters) BuildCharacter(character, groundLayer, physics);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("Map3 Cave: built Goblin, Slime, Bat prefabs + 15 clips + 3 Animator Controllers. Assign your Ground objects to the Ground layer.");
            EditorUtility.DisplayDialog("Map3 Cave enemies ready", "Goblin, Slime, Bat are in Assets/Prefabs/Monster/Map3.\nAssign your map terrain to the Ground layer.\nUse Tools > Map3 Cave > 2. Create Enemy Demo Scene to test.", "OK");
        }

        private static void BuildCharacter(Character character, int groundLayer, PhysicsMaterial2D physics)
        {
            string root = Art + "/" + character.name;
            string animationRoot = root + "/Animations";
            EnsureFolder(animationRoot);
            var clips = new Dictionary<string, AnimationClip>();
            foreach (StateFrames state in character.states)
            {
                var sprites = new List<Sprite>();
                for (int index = 0; index < state.count; index++)
                {
                    string path = root + "/" + state.name + "/" + index.ToString("00") + ".png";
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (!importer) throw new FileNotFoundException("Missing animation frame", path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = character.ppu;
                    importer.spritePivot = new Vector2(character.pivotX, character.pivotY);
                    TextureImporterSettings settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = importer.spritePivot;
                    importer.SetTextureSettings(settings);
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (!sprite) throw new InvalidOperationException("Sprite import failed: " + path);
                    sprites.Add(sprite);
                }
                float fps = FrameRate(character.name, state.name);
                string clipPath = animationRoot + "/" + character.name + "_" + state.name + ".anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
                clip.ClearCurves(); clip.frameRate = fps;
                var keys = new ObjectReferenceKeyframe[sprites.Count + 1];
                for (int index = 0; index < sprites.Count; index++)
                    keys[index] = new ObjectReferenceKeyframe { time = index / fps, value = sprites[index] };
                keys[sprites.Count] = new ObjectReferenceKeyframe { time = sprites.Count / fps, value = sprites[sprites.Count - 1] };
                AnimationUtility.SetObjectReferenceCurve(clip,
                    EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
                var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
                clipSettings.loopTime = state.name == "Idle" || state.name == "Move";
                AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
                var events = new List<AnimationEvent>();
                if (state.name == "Attack") events.Add(new AnimationEvent
                {
                    functionName = "DealAttackHit", time = HitFrame(character.name) / fps
                });
                if (state.name == "Attack" || state.name == "Hurt") events.Add(new AnimationEvent
                {
                    functionName = "FinishAction", time = sprites.Count / fps
                });
                AnimationUtility.SetAnimationEvents(clip, events.ToArray());
                EditorUtility.SetDirty(clip); clips[state.name] = clip;
            }
            string controllerPath = animationRoot + "/" + character.name + ".controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState state in machine.states) machine.RemoveState(state.state);
            foreach (var entry in clips)
            {
                AnimatorState state = machine.AddState(entry.Key); state.motion = entry.Value;
                state.writeDefaultValues = false;
                if (entry.Key == "Idle") machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            GameObject rootObject = new GameObject("Cave_" + character.name);
            try
            {
                SpriteRenderer renderer = rootObject.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + "/Idle/00.png");
                renderer.sortingOrder = 10;
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                string materialPath = Art + "/Shared/EnemySprite.material.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material && shader) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
                if (material) renderer.sharedMaterial = material;
                Animator animator = rootObject.AddComponent<Animator>(); animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Rigidbody2D body = rootObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Dynamic; body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.gravityScale = character.name == "Bat" ? 0 : 3;
                body.mass = character.name == "Slime" ? 4 : 2;
                BoxCollider2D shape = rootObject.AddComponent<BoxCollider2D>();
                shape.size = character.name == "Bat" ? new Vector2(0.9f, 0.65f) : new Vector2(character.bodyWidth, character.bodyHeight);
                shape.offset = new Vector2(0, character.bodyCenterY); shape.sharedMaterial = physics;
                CaveHealth health = rootObject.AddComponent<CaveHealth>(); health.invulnerabilitySeconds = 0.15f;
                CaveEnemy enemy = rootObject.AddComponent<CaveEnemy>();
                enemy.kind = (CaveEnemyKind)Enum.Parse(typeof(CaveEnemyKind), character.name);
                enemy.groundMask = 1 << groundLayer;
                enemy.attackDuration = clips["Attack"].length;
                enemy.hurtDuration = clips["Hurt"].length;
                enemy.deathDuration = clips["Dead"].length;
                ConfigureEnemy(enemy, health, character.bodyCenterY);
                PrefabUtility.SaveAsPrefabAsset(rootObject, Prefabs + "/Cave_" + character.name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(rootObject); }
        }

        private static void ConfigureEnemy(CaveEnemy enemy, CaveHealth health, float centerY)
        {
            switch (enemy.kind)
            {
                case CaveEnemyKind.Goblin:
                    health.maxHealth = 45; enemy.patrolSpeed = 1.2f; enemy.chaseSpeed = 2.8f;
                    enemy.damage = 10; enemy.attackRange = 1.7f; enemy.attackRadius = 0.85f;
                    enemy.attackOffset = new Vector2(0.8f, centerY); enemy.attackCooldown = 0.7f;
                    enemy.idleSeconds = 0.8f; break;
                case CaveEnemyKind.Slime:
                    health.maxHealth = 90; enemy.patrolSpeed = 0.55f; enemy.chaseSpeed = 1.1f;
                    enemy.damage = 20; enemy.attackRange = 2.3f; enemy.attackRadius = 1.15f;
                    enemy.attackOffset = new Vector2(1.15f, centerY); enemy.attackCooldown = 1.4f;
                    enemy.idleSeconds = 1.3f; enemy.patrolDistance = 2; break;
                case CaveEnemyKind.Bat:
                    health.maxHealth = 30; enemy.patrolSpeed = 1.1f; enemy.chaseSpeed = 2.2f;
                    enemy.damage = 8; enemy.attackRange = 3; enemy.attackRadius = 0.95f;
                    enemy.attackOffset = Vector2.zero; enemy.attackCooldown = 1.2f;
                    enemy.swoopSpeed = 5.5f; enemy.swoopStart = 0.15f; enemy.swoopEnd = 0.75f;
                    enemy.detectionRange = 7; break;
            }
        }
        private static float FrameRate(string enemy, string state)
        {
            if (state == "Idle") return enemy == "Slime" ? 12 : enemy == "Bat" ? 12 : 6;
            if (state == "Move") return enemy == "Slime" ? 10 : 12;
            if (state == "Attack") return enemy == "Slime" ? 16 : enemy == "Bat" ? 14 : 12;
            return state == "Dead" ? 10 : 12;
        }
        private static float HitFrame(string enemy) { return enemy == "Slime" ? 9 : enemy == "Bat" ? 8 : 6; }

        [MenuItem("Tools/Map3 Cave/2. Create Enemy Demo Scene")]
        public static void CreateDemo()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var prefabs = new GameObject[3];
            string[] names = { "Goblin", "Slime", "Bat" };
            for (int i = 0; i < names.Length; i++)
            {
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Cave_" + names[i] + ".prefab");
                if (!prefabs[i]) { Debug.LogError("Run Tools > Map3 Cave > 1. Build Enemy Prefabs and Animations first."); return; }
            }
            if (File.Exists(DemoScene) && !EditorUtility.DisplayDialog("Replace demo scene?", "Only Map3_EnemyDemo.unity will be rebuilt. Your Map3 scene is separate.", "Rebuild demo", "Cancel")) return;
            int groundLayer = EnsureGroundLayer();
            // Create a plain square texture so the demo needs no UI-package resources.
            string squarePath = Art + "/Shared/DemoSquare.png";
            EnsureFolder(Art + "/Shared");
            if (!File.Exists(squarePath))
            {
                Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                var pixels = new Color[256]; for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(squarePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(squarePath);
            }
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(squarePath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(squarePath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true;
            camera.orthographicSize = 7; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.12f, 0.15f);
            camera.transform.position = new Vector3(5, 3.5f, -10);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Shared/EnemySprite.material.mat");
            CreateGround("Ground_Left", new Vector2(7, -0.5f), new Vector2(20, 1), groundLayer, square, material);
            CreateGround("Ground_Right", new Vector2(29.5f, -0.5f), new Vector2(21, 1), groundLayer, square, material);
            CreateGround("Rock_Step", new Vector2(33, 0.35f), new Vector2(3, 0.7f), groundLayer, square, material);
            Vector2[] spawns = { new Vector2(6, 0.08f), new Vector2(13, 0.08f), new Vector2(23, 3) };
            for (int i = 0; i < prefabs.Length; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], scene);
                instance.transform.position = spawns[i];
            }
            GameObject player = new GameObject("DemoPlayer_BlueSquare"); player.transform.position = new Vector3(0, 1.5f, 0);
            SpriteRenderer visual = player.AddComponent<SpriteRenderer>(); visual.sprite = square;
            visual.color = new Color(0.25f, 0.75f, 1); visual.sortingOrder = 11; visual.sharedMaterial = material;
            Rigidbody2D body = player.AddComponent<Rigidbody2D>(); body.gravityScale = 3;
            body.constraints = RigidbodyConstraints2D.FreezeRotation; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            BoxCollider2D collider = player.AddComponent<BoxCollider2D>(); collider.size = new Vector2(0.8f, 1.4f);
            collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Art + "/Shared/EnemyNoFriction.physicsMaterial2D");
            CaveHealth health = player.AddComponent<CaveHealth>(); health.maxHealth = 100; health.invulnerabilitySeconds = 0.5f;
            CavePlayerTarget marker = player.AddComponent<CavePlayerTarget>(); marker.health = health;
            CaveEnemyDemoPlayer demo = player.AddComponent<CaveEnemyDemoPlayer>(); demo.groundMask = 1 << groundLayer;
            demo.enemyPrefabs = prefabs; demo.enemySpawnPoints = spawns;
            EnsureFolder("Assets/Scenes/Map3"); EditorSceneManager.SaveScene(scene, DemoScene);
            Selection.activeGameObject = player;
            Debug.Log("Demo ready: Play, click Game view. A/D move; Space jump; J attack; K die; R reset all.");
        }
        private static void CreateGround(string name, Vector2 center, Vector2 size, int layer, Sprite sprite, Material material)
        {
            GameObject ground = new GameObject(name); ground.layer = layer; ground.transform.position = center;
            SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled; renderer.size = size;
            renderer.color = new Color(0.23f, 0.3f, 0.32f); renderer.sharedMaterial = material;
            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>(); collider.size = size;
        }

        [MenuItem("Tools/Map3 Cave/3. Validate Generated Assets")]
        public static void Validate()
        {
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Art + "/enemy_manifest.json"));
            int checkedClips = 0;
            foreach (Character character in manifest.characters)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Cave_" + character.name + ".prefab");
                if (!prefab || !prefab.GetComponent<CaveEnemy>() || !prefab.GetComponent<CaveHealth>())
                    throw new InvalidOperationException("Missing prefab or enemy components: " + character.name);
                Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
                if (!body || body.bodyType != RigidbodyType2D.Dynamic ||
                    (character.name == "Bat" && body.gravityScale != 0))
                    throw new InvalidOperationException("Invalid Rigidbody2D settings: " + character.name);
                CaveEnemy enemy = prefab.GetComponent<CaveEnemy>();
                if (enemy.groundMask.value == 0 || enemy.damage <= 0)
                    throw new InvalidOperationException("Missing terrain mask or damage: " + character.name);
                foreach (StateFrames state in character.states)
                {
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Art + "/" + character.name +
                        "/Animations/" + character.name + "_" + state.name + ".anim");
                    if (!clip) throw new InvalidOperationException("Missing animation: " + character.name + "/" + state.name);
                    bool mustLoop = state.name == "Idle" || state.name == "Move";
                    if (AnimationUtility.GetAnimationClipSettings(clip).loopTime != mustLoop)
                        throw new InvalidOperationException("Invalid loop flag: " + clip.name);
                    ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip,
                        EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
                    if (keys == null || keys.Length != state.count + 1)
                        throw new InvalidOperationException("Missing frames: " + clip.name);
                    foreach (ObjectReferenceKeyframe key in keys)
                        if (!key.value) throw new InvalidOperationException("Broken sprite reference: " + clip.name);
                    if (state.name == "Attack")
                    {
                        int hits = 0, finishes = 0;
                        foreach (AnimationEvent e in AnimationUtility.GetAnimationEvents(clip))
                        {
                            if (e.functionName == "DealAttackHit") hits++;
                            if (e.functionName == "FinishAction") finishes++;
                        }
                        if (hits != 1 || finishes != 1)
                            throw new InvalidOperationException("Attack must have one hit event and one finish event: " + clip.name);
                    }
                    checkedClips++;
                }
            }
            Debug.Log("PASS: 3 enemy prefabs, " + checkedClips + " animation clips, sprite references, loops and attack events validated.");
            EditorUtility.DisplayDialog("Map3 Cave validation", "PASS: 3 prefabs and 15 animation clips.\nUse the demo scene to check movement and combat in Play Mode.", "OK");
        }
        private static int EnsureGroundLayer()
        {
            int existing = LayerMask.NameToLayer("Ground"); if (existing >= 0) return existing;
            var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = manager.FindProperty("layers");
            for (int index = 8; index < 32; index++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(index).stringValue))
                {
                    layers.GetArrayElementAtIndex(index).stringValue = "Ground";
                    manager.ApplyModifiedProperties(); return index;
                }
            throw new InvalidOperationException("No free layer slot for Ground. Create/reuse a Ground layer before building.");
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
