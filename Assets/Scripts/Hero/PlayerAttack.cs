using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator), typeof(PlayerMana))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Cooldowns")]
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private float fireballCooldown = 1.2f;
    [SerializeField] private float flameCooldown = 2f;

    [Header("Mana Costs")]
    [SerializeField] private float fireballManaCost = 20f;
    [SerializeField] private float flameManaCost = 30f;

    private Animator animator;
    private PlayerMana mana;

    private float nextAttackTime;
    private float nextFireballTime;
    private float nextFlameTime;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        mana = GetComponent<PlayerMana>();
    }

    private void Update()
    {
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && health.IsDead)
            return; 

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || IsPerformingSkill())
            return;

        if (keyboard.jKey.wasPressedThisFrame &&
            Time.time >= nextAttackTime)
        {
            ActivateSkill("Attack");
            nextAttackTime = Time.time + attackCooldown;
        }
        else if (keyboard.kKey.wasPressedThisFrame &&
                 Time.time >= nextAttackTime)
        {
            ActivateSkill("Attack2");
            nextAttackTime = Time.time + attackCooldown;
        }
        else if (keyboard.uKey.wasPressedThisFrame &&
                 Time.time >= nextFireballTime)
        {
            if (!TryUseMana(fireballManaCost))
                return;

            ActivateSkill("CastFireball");
            nextFireballTime = Time.time + fireballCooldown;
        }
        else if (keyboard.iKey.wasPressedThisFrame &&
                 Time.time >= nextFlameTime)
        {
            if (!TryUseMana(flameManaCost))
                return;

            ActivateSkill("Flame");
            nextFlameTime = Time.time + flameCooldown;
        }
    }

    private bool TryUseMana(float amount)
    {
        if (mana.TrySpend(amount))
            return true;

        Debug.Log("Not enough mana!", this);
        return false;
    }

    private bool IsPerformingSkill()
    {
        if (animator.IsInTransition(0))
            return true;

        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        return state.IsName("Hurt") ||
               state.IsName("Attack") ||
               state.IsName("Attack2") ||
               state.IsName("CastFireball") ||
               state.IsName("Flame");
    }

    private void ActivateSkill(string triggerName)
    {
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Attack2");
        animator.ResetTrigger("CastFireball");
        animator.ResetTrigger("Flame");

        animator.SetTrigger(triggerName);
    }
}