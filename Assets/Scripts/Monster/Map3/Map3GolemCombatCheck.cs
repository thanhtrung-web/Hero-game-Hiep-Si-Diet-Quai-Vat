#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
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
            var boss = UnityEngine.Object.FindAnyObjectByType<CaveEnemy>();
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
                boss.debugAttack = true;
            }
            if (knight.CurrentHealth < knight.MaxHealth)
            {
                if (knight.MaxHealth - knight.CurrentHealth != 30) throw new Exception("Expected one natural boss hit of 30");
                int hp = knight.CurrentHealth;
                boss.DealAttackHit(); boss.DealAttackHit();
                if (knight.CurrentHealth != hp) throw new Exception("Repeated event dealt duplicate damage");
                var health = boss.GetComponent<CaveEnemyHealth>();
                var field = typeof(EnemyHealth).GetField("currentHealth", BindingFlags.Instance | BindingFlags.NonPublic);
                int before = (int)field.GetValue(health);
                knight.DealDamageToEnemies(20);
                if ((int)field.GetValue(health) != before - 20) throw new Exception("Knight must deal exactly 20 damage to boss");
                health.TakeDamage(10000);
                if (!health.IsDead || boss.State != CaveEnemyState.Dead || boss.GetComponent<Collider2D>().enabled)
                    throw new Exception("Boss death did not disable hitbox");
                Debug.Log("GOLEM_COMBAT_PASSED: natural animation hit=30; duplicate events=0; Knight hit=20; death state and collider OK");
                Finish(0); return;
            }
            if (Time.realtimeSinceStartupAsDouble > deadline) throw new Exception("Boss attack check timed out; state=" + boss.State
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
