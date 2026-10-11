using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeHit : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 hitboxSize = new Vector2(1f, 0.8f);

    // Đổi tên này nếu Trigger trong Animator của bạn khác.
    [SerializeField] private string attack1Trigger = "Attack_1";
    [SerializeField] private string attack2Trigger = "Attack_2";

    private bool attackLocked;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (attackLocked || Keyboard.current == null)
            return;

        if (Keyboard.current.jKey.wasPressedThisFrame)
            StartAttack(attack1Trigger);
        else if (Keyboard.current.kKey.wasPressedThisFrame)
            StartAttack(attack2Trigger);
    }

    private void StartAttack(string triggerName)
    {
        attackLocked = true;
        animator.SetTrigger(triggerName);
    }

    // Gọi bằng Animation Event ở frame lưỡi kiếm chạm mục tiêu.
    public void DealDamage(int damage)
    {
        if (attackPoint == null)
        {
            Debug.LogError("Chưa gán Attack Point cho PlayerMeleeHit.", this);
            return;
        }

        Collider2D[] targets = Physics2D.OverlapBoxAll(
            attackPoint.position, hitboxSize, 0f);

        HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();

        foreach (Collider2D target in targets)
        {
            EnemyHealth enemy = target.GetComponentInParent<EnemyHealth>();

            if (enemy != null && damagedEnemies.Add(enemy))
                enemy.TakeDamage(damage);
        }
    }

    // Gọi bằng Animation Event ở frame cuối của animation chém.
    public void FinishAttack()
    {
        attackLocked = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, hitboxSize);
    }
}