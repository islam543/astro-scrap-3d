using UnityEngine;

public class CyberMonsterHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth    = 150;
    private int currentHealth;

    [Header("Death")]
    public float destroyDelay = 3.5f;

    private bool isDead = false;

    void Start() => currentHealth = maxHealth;

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        Debug.Log($"[CyberMonsterHealth] Took {damage} dmg. HP: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("[CyberMonsterHealth] Cyber Monster died!");

        CyberMonsterAI ai = GetComponent<CyberMonsterAI>();
        if (ai != null) ai.OnDead();

        // Disable all colliders so bullets pass through
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        Destroy(gameObject, destroyDelay);
    }
}
