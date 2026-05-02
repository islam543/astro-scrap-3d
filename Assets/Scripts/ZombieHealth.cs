using System;
using System.Reflection;
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

        ZombieAI ai = GetComponent<ZombieAI>();
        if (ai != null) ai.OnDead();

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        NotifyGameManager();

        Destroy(gameObject, destroyDelay);
    }

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
            TryInvokeGameManager(gameManager, "ZombieKilled") ||
            TryInvokeGameManager(gameManager, "OnEnemyKilled");

        if (notified)
            Debug.Log($"[ZombieHealth] Notified GameManager on {gameManager.gameObject.name}.");
        else
            Debug.LogWarning("[ZombieHealth] GameManager found, but it has no OnZombieKilled, ZombieKilled, or OnEnemyKilled method.");
    }

    MonoBehaviour FindGameManager()
    {
        GameObject namedObject = GameObject.Find("GameManager");
        if (namedObject != null)
        {
            MonoBehaviour[] components = namedObject.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component != null && component.GetType().Name == "GameManager")
                    return component;
            }

            if (components.Length > 0)
                return components[0];
        }

        foreach (MonoBehaviour component in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (component != null && component.GetType().Name == "GameManager")
                return component;
        }

        return null;
    }

    bool TryInvokeGameManager(MonoBehaviour gameManager, string methodName)
    {
        MethodInfo method = gameManager.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (method == null)
            return false;

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
                Type parameterType = parameters[0].ParameterType;
                object argument = null;

                if (parameterType.IsAssignableFrom(typeof(GameObject)))
                    argument = gameObject;
                else if (parameterType.IsAssignableFrom(typeof(ZombieHealth)))
                    argument = this;
                else if (parameterType.IsAssignableFrom(typeof(Transform)))
                    argument = transform;
                else
                    return false;

                method.Invoke(gameManager, new[] { argument });
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

    public int GetHealth() => currentHealth;
    public bool IsDead() => isDead;
}
