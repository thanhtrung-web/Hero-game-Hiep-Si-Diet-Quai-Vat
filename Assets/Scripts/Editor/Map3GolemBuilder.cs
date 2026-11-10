using System;
using System.Linq;
using HeroGame.Map3;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class Map3GolemBuilder
{
    internal const string Prefab = "Assets/Prefabs/Monster/Map3/Boss_Golem.prefab";
    const string Scene = "Assets/Scenes/Map3/Map3.unity";

    [MenuItem("Tools/Map 3/Create Golem Boss")]
    public static void Build()
    {
        Validate();
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(Scene);
        if (scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<GolemBoss>(true))) return;

        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        var box = asset.GetComponent<BoxCollider2D>();
        float scale = asset.transform.localScale.y;
        Vector3 floor = FindBossFloor(GameObject.Find("Ground").GetComponent<Tilemap>(), box.size.y * scale);
        var boss = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        boss.transform.position = floor + Vector3.up * (0.05f + (box.size.y / 2 - box.offset.y) * scale);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/Map 3/Validate Golem Boss")]
    public static void Validate()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        if (!asset || !asset.CompareTag("Enemy") || asset.layer != LayerMask.NameToLayer("Enemy") ||
            !asset.GetComponent<SpriteRenderer>() || !asset.GetComponent<AudioSource>() ||
            !asset.GetComponent<Rigidbody2D>() || !asset.GetComponent<BoxCollider2D>())
            throw new InvalidOperationException("Check components, layer and tag on Boss_Golem.");
        var boss = asset.GetComponent<GolemBoss>();
        var animator = asset.GetComponent<Animator>();
        if (!boss || !animator || !animator.enabled || !boss.fxController || boss.groundMask.value == 0)
            throw new InvalidOperationException("Check GolemBoss, Ground Mask and both Animators.");
        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (!controller) throw new InvalidOperationException("Missing main Animator Controller.");
        foreach (string name in new[] { "AttackA", "AttackB", "AttackC" })
        {
            var state = controller.layers[0].stateMachine.states.FirstOrDefault(s => s.state.name == name).state;
            var clip = state ? state.motion as AnimationClip : null;
            if (!clip || clip.isLooping) throw new InvalidOperationException($"Assign a one-shot clip to {name}.");
            var hits = AnimationUtility.GetAnimationEvents(clip).Where(e => e.functionName == nameof(GolemBoss.OnAttackImpact)).ToArray();
            if (hits.Length != 1 || hits[0].time <= 0 || hits[0].time >= clip.length)
                throw new InvalidOperationException($"{clip.name} needs exactly one OnAttackImpact event inside the clip.");
        }
        Debug.Log("Golem prefab and attack events validated.", asset);
    }

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

}
