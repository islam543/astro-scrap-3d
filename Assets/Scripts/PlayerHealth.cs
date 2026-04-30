using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth  = Mathf.Max(currentHealth, 0);
        Debug.Log($"[PlayerHealth] Took {damage} dmg. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Debug.Log("[PlayerHealth] Player is dead! (add game-over logic here)");
    }

    public int GetHealth() => currentHealth;
}
