#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HeroGame.Map3;

// Batch-only integration check in a disposable scene; never saves Map3.
public class Map3GolemCombatCheck : MonoBehaviour
{
    const string Key = "Map3GolemCombatCheck.Running";
    static double deadline;
    static bool positioned;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        if (!Application.isBatchMode || !SessionState.GetBool(Key, false)) return;
        positioned = false;
        deadline = Time.realtimeSinceStartupAsDouble + 15;
        new GameObject("GolemCombatCheck").AddComponent<Map3GolemCombatCheck>();
    }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Run this check in batch mode.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var floor = new GameObject("TestFloor"); floor.layer = LayerMask.NameToLayer("Ground");
        floor.transform.position = new Vector3(0, -0.5f, 0);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(30, 1);
        var boss = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Map3/Boss_Golem.prefab"));
        boss.name = "Boss_Golem"; boss.transform.position = new Vector3(0, 1.42f, 0);
        var knight = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hero/Knight/Knight.prefab"));
        knight.transform.position = new Vector3(1.5f, 2, 0);
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    void Update() { Tick(); }

    static void Tick()
    {
        try
        {
            var boss = UnityEngine.Object.FindAnyObjectByType<GolemBoss>();
            var knight = UnityEngine.Object.FindAnyObjectByType<KnightController>();
            if (!boss || !knight) throw new Exception("Missing test actors");
            if (knight.CurrentHealth <= 0) return; // Wait for Start.
            if (!positioned)
            {
                knight.enabled = false;
                var kbody = knight.GetComponent<Rigidbody2D>();
                kbody.gravityScale = 0; kbody.linearVelocity = Vector2.zero;
                kbody.constraints = RigidbodyConstraints2D.FreezeAll;
                var col = knight.GetComponentsInChildren<Collider2D>().First(c => !c.isTrigger);
                knight.transform.position += new Vector3(0, 0.05f - col.bounds.min.y, 0);
                var scale = knight.transform.localScale; scale.x = -Mathf.Abs(scale.x); knight.transform.localScale = scale;
                Physics2D.SyncTransforms(); positioned = true;
                deadline = Time.realtimeSinceStartupAsDouble + 45;
            }
            if (knight.CurrentHealth < knight.MaxHealth)
            {
                if (knight.MaxHealth - knight.CurrentHealth != boss.normalDamage) throw new Exception("Expected one natural boss normal hit");
                int hp = knight.CurrentHealth;
                boss.OnAttackImpact(); boss.OnAttackImpact();
                if (knight.CurrentHealth != hp) throw new Exception("Repeated event dealt duplicate damage");
                int before = boss.CurrentHP;
                knight.DealDamageToEnemies(20);
                if (boss.CurrentHP != before - 20) throw new Exception("Knight hit did not remove 20 HP");
                int deathCount = 0;
                boss.Died += _ => deathCount++;
                boss.TakeDamage(10000);
                boss.TakeDamage(10000);
                if (!boss.IsDead || deathCount != 1 || boss.GetComponent<Collider2D>().enabled)
                    throw new Exception("Boss death handling failed");
                Debug.Log("GOLEM_COMBAT_PASSED");
                Finish(0); return;
            }
            if (Time.realtimeSinceStartupAsDouble > deadline) throw new Exception("Boss attack check timed out"
                + ", boss=" + boss.transform.position + ", knight=" + knight.transform.position
                + ", boss collider=" + boss.GetComponent<Collider2D>().bounds
                + ", knight collider=" + knight.GetComponent<Collider2D>().bounds
                + ", time=" + Time.time + ", animation=" + boss.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }

    static void Finish(int code)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.Exit(code);
    }
}
#endif
