using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool enableDamageTest = true;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        animator.SetBool("IsDead", false);
    }

    private void Update()
    {
        if (!enableDamageTest || IsDead)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
            TakeDamage(20);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"Player HP: {currentHealth}/{maxHealth}", this);

        if (IsDead)
            animator.SetBool("IsDead", true);
        else
            animator.SetTrigger("Hurt");
    }
}