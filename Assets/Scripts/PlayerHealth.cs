using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;
    private bool gameOverSent = false;

    public event System.Action<int, int> HealthChanged;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        NotifyHealthChanged();
        Debug.Log($"[PlayerHealth] Ready. HP: {currentHealth}/{maxHealth}");
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth  = Mathf.Max(currentHealth, 0);
        Debug.Log($"[PlayerHealth] Took {damage} dmg. HP: {currentHealth}/{maxHealth}");
        NotifyHealthChanged();

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

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"[PlayerHealth] Healed {amount} HP. HP: {currentHealth}/{maxHealth}");
        NotifyHealthChanged();
    }

    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
