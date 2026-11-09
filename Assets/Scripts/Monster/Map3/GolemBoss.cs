using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HeroGame.Map3 {
[RequireComponent(typeof(AudioSource))]
public class GolemBoss : EnemyHealth {
 public override bool IsBoss => true;
 public override string BossTitle => "Cổ Thạch Vương - Ancient Golem";
 [System.Serializable] public class Sequence { public string name; public Sprite[] frames; }
 public Sequence[] sequences;
 public int bossHealth=900;
 public float detectionRange=10, leashRange=16, speed=1.5f;
 public LayerMask groundMask;
 [Header("Golem Sound Effects")]
 public AudioClip windupSound, punchSound, slamSound, waveSound, hurtSound, upgradeSound, deathSound;
 [Range(0f,1f)] public float soundVolume=.7f;
 public UnityEngine.Audio.AudioMixerGroup soundMixerGroup;
 private AudioSource soundSource;
 private float nextHurtSound;

 private void PlaySound(AudioClip clip, bool replace = false)
 {
     if (!clip) return;
     if (replace) soundSource.Stop();
     Transform listener = target ? target.transform : Camera.main?.transform;
     if (!listener) return;
     float distance = Vector2.Distance(transform.position, listener.position);
     float gain = 1f - Mathf.InverseLerp(5f, 22f, distance);
     if (gain > 0f) soundSource.PlayOneShot(clip, soundVolume * gain);
 }
 public int CurrentHP {get;private set;}
 SpriteRenderer visual, fx; Rigidbody2D body; Collider2D shape; KnightController target;
 GameObject waveObject; void OnDestroy(){if(waveObject)Destroy(waveObject);}
 Vector2 home; bool busy, enraged, upgrading; float facing=1, ready, stagger; int lastAttack=-1;
 protected override void Awake(){
 soundSource = GetComponent<AudioSource>();
 soundSource.outputAudioMixerGroup = soundMixerGroup;
 CurrentHP=bossHealth; currentHealth=bossHealth; maxHealth=bossHealth; visual=GetComponent<SpriteRenderer>();body=GetComponent<Rigidbody2D>();shape=GetComponent<Collider2D>();home=transform.position;
 var child=new GameObject("Golem FX");child.transform.SetParent(transform,false);fx=child.AddComponent<SpriteRenderer>();fx.sharedMaterial=visual.sharedMaterial;fx.sortingOrder=visual.sortingOrder+1;
 }
 Sprite[] Frames(string name){foreach(var s in sequences)if(s.name==name)return s.frames;return System.Array.Empty<Sprite>();}
 void Pose(string name){var f=Frames(name);if(f.Length>0)visual.sprite=f[(int)(Time.time*8)%f.Length];}
 void Stop(){body.linearVelocity=new Vector2(0,body.linearVelocity.y);}
 void Update(){if(isDead)return;if(!target){var p=GameObject.FindGameObjectWithTag("Player");if(p)target=p.GetComponentInChildren<KnightController>();}
 if(busy)return;
 if(CurrentHP<=bossHealth/2&&!enraged){StartCoroutine(Upgrade());return;}
 if(!target||target.CurrentHealth<=0||Vector2.Distance(home,transform.position)>leashRange||Vector2.Distance(target.transform.position,transform.position)>detectionRange){Stop();Pose(enraged?"IdleB":"IdleA");return;}
 var pc=target.GetComponent<Collider2D>();Vector2 delta=(Vector2)pc.bounds.center-(Vector2)shape.bounds.center;
 facing=delta.x<0?-1:1;visual.flipX=facing<0;
 bool sight=!Physics2D.Linecast(shape.bounds.center,pc.bounds.center,groundMask);
 float gap=Mathf.Max(0,shape.Distance(pc).distance);
 if(gap<=3.5f&&sight&&Time.time>=ready){int attack=gap>1.2f?2:Random.Range(0,3);if(attack==lastAttack)attack=(attack+1)%3;lastAttack=attack;StartCoroutine(Attack(attack));return;}
 if(gap>0.6f&&sight){Bounds b=shape.bounds;var foot=new Vector2(b.center.x+facing*(b.extents.x+.15f),b.min.y+.15f);
 bool safe=Physics2D.Raycast(foot,Vector2.down,.7f,groundMask)&&!Physics2D.Raycast(b.center,Vector2.right*facing,b.extents.x+.2f,groundMask);
 body.linearVelocity=new Vector2(safe?facing*speed*(enraged?1.35f:1):0,body.linearVelocity.y);Pose(safe?"Run":enraged?"IdleB":"IdleA");}else{Stop();Pose(enraged?"IdleB":"IdleA");}}
 IEnumerator Play(string name,float fps){var frames=Frames(name);foreach(var sprite in frames){visual.sprite=sprite;yield return new WaitForSeconds(1/fps);}}
 IEnumerator Effect(string name,Vector2 local){fx.transform.localPosition=local;fx.flipX=facing<0;foreach(var sprite in Frames(name)){fx.sprite=sprite;yield return new WaitForSeconds(.08f);}fx.sprite=null;}
 void Hit(Vector2 center,float radius,int damage,HashSet<KnightController> victims){foreach(var c in Physics2D.OverlapCircleAll(center,radius)){var k=c.GetComponentInParent<KnightController>();if(k&&k.CurrentHealth>0&&!victims.Contains(k)&&!Physics2D.Linecast(shape.bounds.center,c.bounds.center,groundMask)){victims.Add(k);k.TakeDamage(damage);}}}
 IEnumerator Attack(int index){busy=true;Stop();PlaySound(windupSound,true);string name="Attack"+(char)('A'+index);var frames=Frames(name);float fps=enraged?12:9;int impact=Mathf.Clamp(Mathf.RoundToInt(frames.Length*.55f),1,frames.Length-1);var victims=new HashSet<KnightController>();
 for(int i=0;i<frames.Length;i++){visual.sprite=frames[i];if(i==impact){PlaySound(index==0?punchSound:index==1?slamSound:waveSound,true);Vector2 center=shape.bounds.center;
 if(index==0){center+=Vector2.right*facing*1.05f;Hit(center,1.15f,enraged?36:28,victims);StartCoroutine(Effect(name+"_FX",new Vector2(facing*.5f,0)));}
 else if(index==1){Hit(center,2.5f,enraged?46:36,victims);StartCoroutine(Effect(name+"_FX",new Vector2(0,-.3f)));}
 else {StartCoroutine(Wave(facing,enraged?32:24));}
 }yield return new WaitForSeconds(1/fps);}
 yield return Play("Reset",enraged?16:12);ready=Time.time+(enraged?.65f:1.2f);busy=false;}
 IEnumerator Wave(float direction,int damage){var go=new GameObject("Golem stone wave");waveObject=go;var sr=go.AddComponent<SpriteRenderer>();sr.sharedMaterial=visual.sharedMaterial;sr.sortingOrder=fx.sortingOrder;sr.flipX=direction<0;go.transform.localScale=transform.lossyScale;
 var frames=Frames("AttackC_FX");Vector2 p=shape.bounds.center;var victims=new HashSet<KnightController>();float elapsed=0;
 while(elapsed<.9f&&!isDead){float dt=Time.deltaTime;Vector2 next=p+Vector2.right*direction*5*dt;if(Physics2D.Linecast(p,next,groundMask))break;p=next;go.transform.position=p;if(frames.Length>0)sr.sprite=frames[(int)(elapsed*12)%frames.Length];Hit(p,.65f,damage,victims);elapsed+=dt;yield return null;}Destroy(go);}
 IEnumerator Upgrade(){busy=true;upgrading=true;Stop();PlaySound(upgradeSound,true);yield return Play("Upgrade",10);enraged=true;upgrading=false;busy=false;}
 public override void TakeDamage(int amount){if(isDead||amount<=0)return;CurrentHP=Mathf.Max(0,CurrentHP-amount); currentHealth = CurrentHP; NotifyDamageTaken(amount); NotifyHealthChanged(currentHealth, maxHealth); SpawnDamagePopup(amount);if(CurrentHP==0){isDead=true;StopAllCoroutines();if(waveObject)Destroy(waveObject);fx.sprite=null;StartCoroutine(Death());return;}
 if(Time.time>=nextHurtSound){nextHurtSound=Time.time+.18f;PlaySound(hurtSound);}
 // Attacks and transformation have poise: damage still applies, animation is not interrupted.
 if(!busy&&Time.time>=stagger){stagger=Time.time+1.5f;StartCoroutine(Hurt());}}
 IEnumerator Hurt(){busy=true;Stop();yield return Play(enraged?"HitB":"HitA",12);busy=false;}
 IEnumerator Death(){Stop();PlaySound(deathSound,true);shape.enabled=false;body.simulated=false;StartCoroutine(Effect("Death_FX",Vector2.zero));yield return Play(enraged||upgrading?"DeathB":"DeathA",9);yield return new WaitForSeconds(2);Destroy(gameObject);}
 // Legacy GUI replaced by BossHealthBarUI
 void LegacyOnGUI(){if(!target||isDead||Vector2.Distance(target.transform.position,transform.position)>detectionRange+3)return;float w=Mathf.Min(500,Screen.width*.55f);Rect r=new Rect((Screen.width-w)/2,24,w,24);GUI.Box(r,"GOLEM"+(enraged?" â€” ENRAGED":""));var old=GUI.color;GUI.color=enraged?Color.red:Color.cyan;GUI.DrawTexture(new Rect(r.x,r.y+26,w*CurrentHP/bossHealth,8),Texture2D.whiteTexture);GUI.color=old;}
}}

