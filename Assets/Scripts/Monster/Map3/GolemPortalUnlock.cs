using UnityEngine;

namespace HeroGame.Map3
{
    [RequireComponent(typeof(GolemBoss))]
    public sealed class GolemPortalUnlock : MonoBehaviour
    {
        [SerializeField] private GameObject portalObject;
        private GolemBoss boss;

        private void Awake()
        {
            boss = GetComponent<GolemBoss>();
            if (!portalObject)
            {
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    var exit = root.GetComponentInChildren<GameWinPortal>(true);
                    if (!exit) continue;
                    portalObject = exit.gameObject;
                    break;
                }
            }
            if (!portalObject)
            {
                Debug.LogError("Assign the Map 3 exit portal to GolemPortalUnlock.", this);
                enabled = false;
                return;
            }

            portalObject.SetActive(false);
            boss.Died += Unlock;
        }

        private void Start()
        {
            if (boss.IsDead) Unlock(boss);
        }

        private void Unlock(EnemyHealth defeatedBoss)
        {
            portalObject.SetActive(true);
            foreach (var visual in portalObject.GetComponentsInChildren<SpriteRenderer>(true))
                visual.enabled = true;
            foreach (var trigger in portalObject.GetComponentsInChildren<Collider2D>(true))
                trigger.enabled = true;
        }

        private void OnDestroy()
        {
            if (boss) boss.Died -= Unlock;
        }
    }
}

