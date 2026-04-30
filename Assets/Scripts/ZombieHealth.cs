using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth  = 100;
    private int currentHealth;

    [Header("Death")]
    public float destroyDelay = 3f;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"[ZombieHealth] Took {damage} dmg. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[ZombieHealth] Zombie died!");

        // Notify the AI to stop and play death animation
        ZombieAI ai = GetComponent<ZombieAI>();
        if (ai != null) ai.OnDead();

        // Disable collider so bullets pass through the corpse
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, destroyDelay);
    }
}
