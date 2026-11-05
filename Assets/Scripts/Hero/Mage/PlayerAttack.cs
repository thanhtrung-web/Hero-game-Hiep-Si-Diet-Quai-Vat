using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(Animator), typeof(PlayerStamina))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Cooldowns")]
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private float fireballCooldown = 1.2f;
    [SerializeField] private float flameCooldown = 2f;

    // FormerlySerializedAs giữ lại giá trị đã chỉnh trong prefab khi đổi tên biến
    [Header("Stamina Costs")]
    [FormerlySerializedAs("fireballManaCost")]
    [SerializeField] private float fireballStaminaCost = 20f;
    [FormerlySerializedAs("flameManaCost")]
    [SerializeField] private float flameStaminaCost = 30f;

    private Animator animator;
    private PlayerStamina stamina;
    private PlayerHealth health;

    private float nextAttackTime;
    private float nextFireballTime;
    private float nextFlameTime;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        stamina = GetComponent<PlayerStamina>();
        health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
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
            if (!TryUseStamina(fireballStaminaCost))
                return;

            ActivateSkill("CastFireball");
            nextFireballTime = Time.time + fireballCooldown;
        }
        else if (keyboard.iKey.wasPressedThisFrame &&
                 Time.time >= nextFlameTime)
        {
            if (!TryUseStamina(flameStaminaCost))
                return;

            ActivateSkill("Flame");
            nextFlameTime = Time.time + flameCooldown;
        }
    }

    private bool TryUseStamina(float amount)
    {
        if (stamina.TrySpend(amount))
            return true;

        Debug.Log("Not enough stamina!", this);
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