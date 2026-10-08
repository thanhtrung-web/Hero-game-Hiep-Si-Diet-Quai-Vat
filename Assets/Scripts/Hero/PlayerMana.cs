using UnityEngine;

public class PlayerMana : MonoBehaviour
{
    public float CurrentMana => currentMana;
    public float MaxMana => maxMana;

    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float manaRegenPerSecond = 10f;
    [SerializeField] private float currentMana;

    private void Awake()
    {
        currentMana = maxMana;
    }

    private void Update()
    {
        currentMana = Mathf.Min(
            maxMana,
            currentMana + manaRegenPerSecond * Time.deltaTime
        );
    }

    public bool TrySpend(float amount)
    {
        if (amount < 0f || currentMana < amount)
            return false;

        currentMana -= amount;
        return true;
    }
}