using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;
using HeroGame.Map3;

// Explicit editor command: never modifies a scene on editor startup.
public static class Map3GolemBuilder
{
    const string Art = "Assets/Art/Monster/Map3/Golem/";
    const string Anim = Art + "Animations/";
    const string Prefab = "Assets/Prefabs/Monster/Map3/Boss_Golem.prefab";
    const string Scene = "Assets/Scenes/Map3/Map3.unity";

    [MenuItem("Tools/Map 3/Create Golem Boss")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Docs/Map3/Golem");
        if (!File.Exists("Docs/Map3/Golem/Map3-before-golem.unity"))
            File.Copy(Scene, "Docs/Map3/Golem/Map3-before-golem.unity");
        EditorSceneManager.OpenScene(Scene);
        Directory.CreateDirectory(Anim);
        AssetDatabase.Refresh();

        var normal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Map3/Cave_Goblin.prefab");
        var normalSprite = normal.GetComponent<SpriteRenderer>().sprite;
        Rect goblinPixels = OpaqueRect(normalSprite.texture, normalSprite.rect);
        float normalHeight = goblinPixels.height / normalSprite.pixelsPerUnit;
        var raw = ReadTexture(Art + "Golem_IdleA.png");
        Rect golemPixels = Scan(raw, new Rect(0, raw.height - 64, 64, 64));
        float ppu = golemPixels.height / normalHeight;
        UnityEngine.Object.DestroyImmediate(raw);

        var idle = MakeClip("Idle", "IdleA", 6, true, ppu);
        var move = MakeClip("Move", "Run", 10, true, ppu);
        var attack = MakeClip("Attack", "AttackA", 10, false, ppu);
        var hurt = MakeClip("Hurt", "HitA", 10, false, ppu);
        var dead = MakeClip("Dead", "DeathA", 8, false, ppu);
        AnimationUtility.SetAnimationEvents(attack, new[] {
            new AnimationEvent { time = 0.7f, functionName = "DealAttackHit" },
            new AnimationEvent { time = attack.length - 0.01f, functionName = "FinishAction" }
        });
        AnimationUtility.SetAnimationEvents(hurt, new[] {
            new AnimationEvent { time = hurt.length - 0.01f, functionName = "FinishAction" }
        });

        string controllerPath = Anim + "Boss_Golem.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (!controller)
        {
            AssetDatabase.CopyAsset("Assets/Art/Monster/Map3/Goblin/Animations/Enemy_Goblin.controller", controllerPath);
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        }
        controller.name = "Boss_Golem";
        var clips = new Dictionary<string, AnimationClip> {
            { "Idle", idle }, { "Move", move }, { "Attack", attack }, { "Hurt", hurt }, { "Dead", dead }
        };
        var machine = controller.layers[0].stateMachine;
        foreach (var child in machine.states)
        {
            child.state.motion = clips[child.state.name];
            foreach (var transition in child.state.transitions) transition.duration = 0;
        }
        foreach (var transition in machine.anyStateTransitions) transition.duration = 0;
        EditorUtility.SetDirty(controller);

        var go = new GameObject("Boss_Golem");
        go.tag = "Enemy";
        go.layer = LayerMask.NameToLayer("Enemy");
        go.transform.localScale = new Vector3(2, 2, 1);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprites("IdleA")[0];
        renderer.sharedMaterial = normal.GetComponent<SpriteRenderer>().sharedMaterial;
        renderer.sortingOrder = 10;
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        var body = go.AddComponent<Rigidbody2D>();
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.mass = 6;
        body.gravityScale = 2;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var box = go.AddComponent<BoxCollider2D>();
        // Central body hitbox, with its bottom aligned to the opaque feet.
        box.size = new Vector2(golemPixels.width * 0.7f / ppu, golemPixels.height / ppu);
        box.offset = (golemPixels.center - new Vector2(32, 32)) / ppu;
        var health = go.AddComponent<CaveEnemyHealth>();
        var ai = go.AddComponent<CaveEnemy>();
        ai.kind = CaveEnemyKind.Golem;
        ai.groundMask = LayerMask.GetMask("Ground");
        ai.spriteFacesRight = true;
        ai.detectionRange = 9;
        ai.leashRange = 14;
        ai.patrolDistance = 3;
        ai.patrolSpeed = 0.65f;
        ai.chaseSpeed = 1.5f;
        ai.idleSeconds = 1.2f;
        ai.damage = 30;
        ai.attackCooldown = 1.5f;
        ai.attackRange = 0.65f;
        ai.attackRadius = 1.1f;
        ai.attackOffset = new Vector2(box.size.x + 0.35f, box.offset.y * 2 - 0.25f);
        ai.attackDuration = attack.length;
        ai.hurtDuration = hurt.length;
        ai.deathDuration = dead.length;
        ai.corpseSeconds = 2;
        var serializedHealth = new SerializedObject(health);
        serializedHealth.FindProperty("maxHealth").intValue = 600;
        serializedHealth.FindProperty("animator").objectReferenceValue = animator;
        // Leave patrolScript null: CaveEnemy owns death cleanup and animation.
        serializedHealth.ApplyModifiedPropertiesWithoutUndo();
        var asset = PrefabUtility.SaveAsPrefabAsset(go, Prefab);
        UnityEngine.Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();

        var existing = GameObject.Find("Boss_Golem");
        if (!existing)
        {
            var ground = GameObject.Find("Ground").GetComponent<Tilemap>();
            Vector3 floor = FindBossFloor(ground, normalHeight * 2);
            existing = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            float bottom = (boxOffset(asset).y - asset.GetComponent<BoxCollider2D>().size.y * 0.5f) * 2;
            existing.transform.position = floor - Vector3.up * bottom + Vector3.up * 0.05f;
        }
        EditorSceneManager.SaveScene(existing.scene);
        Validate();
        Debug.Log("GOLEM_BUILD_PASSED position=" + existing.transform.position + ", visible height=" + normalHeight * 2 + ", PPU=" + ppu);
    }

    static Vector2 boxOffset(GameObject go) => go.GetComponent<BoxCollider2D>().offset;

    static Vector3 FindBossFloor(Tilemap ground, float height)
    {
        // Find a wide, open platform toward the right-hand end without editing tiles.
        BoundsInt b = ground.cellBounds;
        for (int x = b.xMax - 6; x >= b.xMin + 6; x--)
        for (int y = b.yMax - 1; y >= b.yMin; y--)
        {
            bool clear = true;
            for (int dx = -4; dx <= 4 && clear; dx++)
            {
                if (!ground.HasTile(new Vector3Int(x + dx, y, 0))) { clear = false; break; }
                for (int dy = 1; dy <= Mathf.CeilToInt(height) + 1; dy++)
                    if (ground.HasTile(new Vector3Int(x + dx, y + dy, 0))) { clear = false; break; }
            }
            if (clear) return ground.CellToWorld(new Vector3Int(x, y + 1, 0)) + Vector3.right * 0.5f;
        }
        throw new InvalidOperationException("No clear boss platform found; prefab created, scene unchanged.");
    }

    static Texture2D ReadTexture(string path)
    {
        var t = new Texture2D(2, 2);
        t.LoadImage(File.ReadAllBytes(path));
        return t;
    }

    static Rect OpaqueRect(Texture2D texture, Rect rect)
    {
        var t = ReadTexture(AssetDatabase.GetAssetPath(texture));
        var result = Scan(t, rect);
        UnityEngine.Object.DestroyImmediate(t);
        return result;
    }

    static Rect Scan(Texture2D t, Rect rect)
    {
        int minX = 64 * 100, minY = minX, maxX = -1, maxY = -1;
        for (int y = (int)rect.yMin; y < rect.yMax; y++)
        for (int x = (int)rect.xMin; x < rect.xMax; x++)
            if (t.GetPixel(x, y).a > 0.1f) {
                minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
            }
        return new Rect(minX - rect.x, minY - rect.y, maxX - minX + 1, maxY - minY + 1);
    }

    static Sprite[] LoadSprites(string sheet) => AssetDatabase.LoadAllAssetsAtPath(Art + "Golem_" + sheet + ".png")
        .OfType<Sprite>().OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();

    static AnimationClip MakeClip(string action, string sheet, float fps, bool loop, float ppu)
    {
        string path = Art + "Golem_" + sheet + ".png";
        var t = ReadTexture(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var previous = provider.GetSpriteRects();
        var rects = new List<SpriteRect>();
        int index = 0;
        for (int y = t.height - 64; y >= 0; y -= 64)
        for (int x = 0; x < t.width; x += 64)
        {
            if (Scan(t, new Rect(x, y, 64, 64)).width <= 0) continue;
            string name = "Golem_" + sheet + "_" + index++;
            rects.Add(new SpriteRect { name = name, rect = new Rect(x, y, 64, 64),
                alignment = SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f),
                spriteID = previous.FirstOrDefault(r => r.name == name)?.spriteID ?? GUID.Generate() });
        }
        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        UnityEngine.Object.DestroyImmediate(t);
        Sprite[] sprites = LoadSprites(sheet);
        var clipPath = Anim + "Golem_" + action + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
        clip.name = "Golem_" + action; clip.frameRate = fps;
        var keys = sprites.Select((s, i) => new ObjectReferenceKeyframe { time = i / fps, value = s }).ToList();
        keys.Add(new ObjectReferenceKeyframe { time = sprites.Length / fps, value = sprites.Last() });
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    public static void Validate()
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        if (!go || !go.CompareTag("Enemy") || go.layer != LayerMask.NameToLayer("Enemy") || go.transform.localScale.x != 2)
            throw new Exception("Invalid Golem prefab");
        var controller = go.GetComponent<Animator>().runtimeAnimatorController;
        if (controller.animationClips.Length != 5 || !go.GetComponent<EnemyHealth>()) throw new Exception("Missing combat/animation");
        var clip = controller.animationClips.Single(c => c.name == "Golem_Attack");
        if (AnimationUtility.GetAnimationEvents(clip).Count(e => e.functionName == "DealAttackHit") != 1)
            throw new Exception("Attack must have exactly one damage event");
        foreach (var c in controller.animationClips)
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(c))
            if (AnimationUtility.GetObjectReferenceCurve(c, binding).Any(k => !k.value)) throw new Exception("Missing animation sprite");
        Debug.Log("GOLEM_VALIDATION_PASSED");
    }
}
