using System;
using System.Reflection;
using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth  = 100;
    private int currentHealth;

    [Header("Death")]
    public float destroyDelay = 3f;   // seconds after death before GameObject is removed

    private bool isDead = false;


    void Awake()
    {
        currentHealth = maxHealth;
    }
    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log($"[ZombieHealth] Ready. HP: {currentHealth}/{maxHealth}");
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        Debug.Log($"[ZombieHealth] Took {damage} dmg. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[ZombieHealth] Zombie died!");

        // Tell AI to stop, face player, and trigger death animation
        ZombieAI ai = GetComponent<ZombieAI>();
        if (ai != null) ai.OnDead();   // OnDead() also calls TriggerDeathAnim()

        // Disable all colliders so bullets / physics stop interacting
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        NotifyGameManager();

        Destroy(gameObject, destroyDelay);
    }

    // ── GameManager notification (reflection-based, works with any method name) ──

    void NotifyGameManager()
    {
        MonoBehaviour gameManager = FindGameManager();
        if (gameManager == null)
        {
            Debug.Log("[ZombieHealth] No GameManager found. Skipping death notification.");
            return;
        }

        bool notified =
            TryInvokeGameManager(gameManager, "OnZombieKilled") ||
            TryInvokeGameManager(gameManager, "ZombieKilled")   ||
            TryInvokeGameManager(gameManager, "OnEnemyKilled");

        if (notified)
            Debug.Log($"[ZombieHealth] Notified GameManager on {gameManager.gameObject.name}.");
        else
            Debug.LogWarning("[ZombieHealth] GameManager found, but it has no OnZombieKilled, " +
                             "ZombieKilled, or OnEnemyKilled method.");
    }

    MonoBehaviour FindGameManager()
    {
        GameObject namedObject = GameObject.Find("GameManager");
        if (namedObject != null)
        {
            foreach (MonoBehaviour component in namedObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "GameManager")
                    return component;

            MonoBehaviour[] all = namedObject.GetComponents<MonoBehaviour>();
            if (all.Length > 0) return all[0];
        }

        foreach (MonoBehaviour component in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (component != null && component.GetType().Name == "GameManager")
                return component;

        return null;
    }

    bool TryInvokeGameManager(MonoBehaviour gameManager, string methodName)
    {
        MethodInfo method = gameManager.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        if (method == null) return false;

        ParameterInfo[] parameters = method.GetParameters();
        try
        {
            if (parameters.Length == 0)
            {
                method.Invoke(gameManager, null);
                return true;
            }
            if (parameters.Length == 1)
            {
                Type pt = parameters[0].ParameterType;
                object arg = null;
                if (pt.IsAssignableFrom(typeof(GameObject)))      arg = gameObject;
                else if (pt.IsAssignableFrom(typeof(ZombieHealth))) arg = this;
                else if (pt.IsAssignableFrom(typeof(Transform)))   arg = transform;
                else return false;

                method.Invoke(gameManager, new[] { arg });
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ZombieHealth] GameManager notification failed: {e.Message}");
            return true;
        }
        return false;
    }
    public void SetHealth(int newHealth)
{
    maxHealth = newHealth;
    currentHealth = newHealth;
    isDead = false;

    Debug.Log($"[ZombieHealth] Health scaled to {currentHealth}/{maxHealth}");
}

    public int  GetHealth() => currentHealth;
    public bool IsDead()    => isDead;
}
