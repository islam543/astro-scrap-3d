using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;
    private bool gameOverSent = false;

    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log($"[PlayerHealth] Ready. HP: {currentHealth}/{maxHealth}");
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth  = Mathf.Max(currentHealth, 0);
        Debug.Log($"[PlayerHealth] Took {damage} dmg. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("[PlayerHealth] Player is dead.");

        if (gameOverSent) return;
        gameOverSent = true;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            GameObject gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();
        }

        gameManager.GameOver();
    }

    public int GetHealth() => currentHealth;
    public bool IsDead() => isDead;
}
