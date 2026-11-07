using System.Linq;
using UnityEngine;
using UnityEditor;
using HeroGame.Map3;
public static class GolemBossUpgrade {
 [InitializeOnLoadMethod] static void Queue(){EditorApplication.update += ApplyRequested;}
 static void ApplyRequested(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=ApplyRequested;if(!System.IO.File.Exists("Tools/GolemUpgrade.request"))return;try{Build();System.IO.File.Delete("Tools/GolemUpgrade.request");System.IO.File.WriteAllText("Logs/Golem-upgrade-result.txt","GOLEM_UPGRADE_PASSED");}catch(System.Exception e){Debug.LogException(e);System.IO.File.WriteAllText("Logs/Golem-upgrade-result.txt",e.ToString());}}
 [MenuItem("Tools/Map 3/Upgrade Golem Boss")] public static void Build(){
 string path="Assets/Prefabs/Monster/Map3/Boss_Golem.prefab";
 var go=PrefabUtility.LoadPrefabContents(path);
 try{
 var old=go.GetComponent<CaveEnemy>();if(old)Object.DestroyImmediate(old);
 var health=go.GetComponent<CaveEnemyHealth>();if(health)Object.DestroyImmediate(health);
 var animator=go.GetComponent<Animator>();if(animator)animator.enabled=false;
 var boss=go.GetComponent<GolemBoss>();if(!boss)boss=go.AddComponent<GolemBoss>();
 string[] names={"IdleA","IdleB","Run","AttackA","AttackB","AttackC","AttackA_FX","AttackB_FX","AttackC_FX","HitA","HitB","Upgrade","Reset","DeathA","DeathB","Death_FX"};
 float ppu=go.GetComponent<SpriteRenderer>().sprite.pixelsPerUnit;
 var method=typeof(Map3GolemBuilder).GetMethod("MakeClip",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
 boss.sequences=names.Select(n=>{var clip=(AnimationClip)method.Invoke(null,new object[]{"Boss_"+n,n,10f,false,ppu});var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip)[0];var frames=AnimationUtility.GetObjectReferenceCurve(clip,binding).Select(k=>(Sprite)k.value).ToArray();return new GolemBoss.Sequence{name=n,frames=frames.Take(frames.Length-1).ToArray()};}).ToArray();
 boss.groundMask=LayerMask.GetMask("Ground");
 if(boss.sequences.Any(s=>s.frames.Length==0||s.frames.Any(f=>!f)))throw new System.Exception("Missing Golem frames");
 PrefabUtility.SaveAsPrefabAsset(go,path);AssetDatabase.SaveAssets();Debug.Log("GOLEM_UPGRADE_PASSED: 16 sequences, three attacks and FX, two phases");
 }finally{PrefabUtility.UnloadPrefabContents(go);}
 }
}

