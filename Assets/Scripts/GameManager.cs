using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public bool pauseOnGameOver = true;
    public bool unlockCursorOnGameOver = true;

    [Header("Start Flow")]
    public bool showStartScreenOnAwake = false;
    public bool createRuntimeMenus = true;
    public bool startFirstRoundOnAwake = true;
    public string gameTitle = "Cyber Zombie Siege";

    [Header("Rounds")]
    public GameObject[] zombiePrefabs;
    public GameObject bossPrefab;
    public Transform[] zombieSpawnPoints;
    public Transform bossSpawnPoint;
    public int roundOneZombieCount = 4;
    public int roundTwoZombieCount = 7;
    public float secondsBetweenRounds = 3f;
    public bool disableSceneEnemiesOnStart = true;
    public float zombieMoveSpeed = 1.6f;
    public string spawnedEnemiesParentName = "SpawnedEnemies";
    public string spawnedZombiesParentName = "Zombies";
    public string spawnedBossesParentName = "Bosses";

    [Header("Lighting Safety")]
    public bool autoFixSceneLighting = true;
    public float minimumDirectionalLightIntensity = 1.35f;
    public float maxFogDensity = 0.008f;

    [Header("Zombie Scaling")]
    public int roundOneZombieHealth = 100;
    public int roundTwoZombieHealth = 180;
    public int roundThreeZombieHealth = 250;

    private enum GamePhase { MainMenu, Instructions, Playing, Victory, GameOver }

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly List<Vector3> discoveredZombiePositions = new List<Vector3>();
    private readonly Vector3[] fallbackSpawnOffsets =
    {
        new Vector3(10f, 0f, 12f),
        new Vector3(-11f, 0f, 9f),
        new Vector3(14f, 0f, -7f),
        new Vector3(-9f, 0f, -13f),
        new Vector3(4f, 0f, 17f),
        new Vector3(17f, 0f, 3f),
        new Vector3(-16f, 0f, -2f),
        new Vector3(2f, 0f, -18f)
    };

    private const float MinimumEnemyDetectionRange = 50f;
    private const float ZombieSphereColliderCenterY = 1f;

    private Canvas runtimeCanvas;
    private GameObject startScreenPanel;
    private GameObject settingsPanel;
    private GameObject instructionsPanel;
    private GameObject victoryPanel;
    private GameObject hudPanel;
    private Text hudText;
    private Text playerHealthText;
    private Text roundAnnouncementText;
    private Text weaponListText;
    private Text volumeValueText;
    private Text sensitivityValueText;
    private Font runtimeFont;

    private GameObject playerObject;
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private Transform spawnedEnemiesRoot;
    private Transform spawnedZombiesParent;
    private Transform spawnedBossesParent;
    private GamePhase phase = GamePhase.MainMenu;
    private int currentRound = 0;
    private int enemiesRemaining = 0;
    private bool isGameOver = false;
    private bool isVictory = false;
    private float sensitivitySetting = 1f;
    private Coroutine roundAnnouncementCoroutine;

    public bool IsGameOver => isGameOver;
    public bool CanPlayerAct => phase == GamePhase.Playing && !isGameOver && !isVictory;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;

        if (autoFixSceneLighting)
            RestoreReasonableLighting();

        FindPlayer();
        SubscribeToPlayerHealth();
        CacheEnemyTemplates();
        EnsureSpawnedEnemyParents();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (createRuntimeMenus)
            BuildRuntimeUi();

        if (showStartScreenOnAwake)
            ShowStartScreen();
        else
        {
            HideRuntimeUi();
            SetGameplayPaused(false);
            if (startFirstRoundOnAwake)
                StartRound(1);
        }
    }

    void Update()
    {
        if (isGameOver && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            RestartScene();
    }

    public void ShowStartScreen()
    {
        phase = GamePhase.MainMenu;
        isGameOver = false;
        isVictory = false;
        currentRound = 0;
        enemiesRemaining = 0;
        activeEnemies.Clear();

        SetPanel(startScreenPanel, true);
        SetPanel(settingsPanel, false);
        SetPanel(instructionsPanel, false);
        SetPanel(victoryPanel, false);
        SetPanel(hudPanel, false);
        if (roundAnnouncementText != null)
            roundAnnouncementText.gameObject.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        SetGameplayPaused(true);
    }

    public void ShowInstructionsBoard()
    {
        phase = GamePhase.Instructions;
        RefreshWeaponList();

        SetPanel(startScreenPanel, false);
        SetPanel(settingsPanel, false);
        SetPanel(instructionsPanel, true);
        SetPanel(victoryPanel, false);
        SetPanel(hudPanel, false);

        SetGameplayPaused(true);
    }

    public void StartRoundOneFromBoard()
    {
        SetPanel(instructionsPanel, false);
        SetPanel(hudPanel, true);
        SetGameplayPaused(false);
        StartRound(1);
    }

    public void ShowSettings()
    {
        SetPanel(startScreenPanel, false);
        SetPanel(settingsPanel, true);
        SetPanel(instructionsPanel, false);
    }

    public void BackToStartFromSettings()
    {
        SetPanel(settingsPanel, false);
        SetPanel(startScreenPanel, true);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        phase = GamePhase.GameOver;
        Debug.Log("[GameManager] Game Over.");

        SetPanel(startScreenPanel, false);
        SetPanel(settingsPanel, false);
        SetPanel(instructionsPanel, false);
        SetPanel(victoryPanel, false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (unlockCursorOnGameOver)
            UnlockCursor();

        SetPlayerControls(false);

        if (pauseOnGameOver)
            Time.timeScale = 0f;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();

#if UNITY_EDITOR
        if (activeScene.buildIndex < 0 && !string.IsNullOrEmpty(activeScene.path))
        {
            EditorSceneManager.LoadSceneInPlayMode(activeScene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif

        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
    }

    public void OnZombieKilled(GameObject zombie)
    {
        RegisterEnemyKilled(zombie);
    }

    public void OnEnemyKilled(GameObject enemy)
    {
        RegisterEnemyKilled(enemy);
    }

    public void OnBossKilled(GameObject boss)
    {
        RegisterEnemyKilled(boss);
    }

    private void StartRound(int roundNumber)
    {
        StopAllCoroutines();
        roundAnnouncementCoroutine = null;
        phase = GamePhase.Playing;
        currentRound = roundNumber;
        enemiesRemaining = 0;
        activeEnemies.Clear();
        SetPanel(hudPanel, true);

        if (currentRound == 1)
        {
            SpawnZombieRound(roundOneZombieCount);
            Debug.Log("[GameManager] Round 1 started.");
        }
        else if (currentRound == 2)
        {
            SpawnZombieRound(roundTwoZombieCount);
            Debug.Log("[GameManager] Round 2 started.");
        }
        else
        {
            SpawnBossRound();
            Debug.Log("[GameManager] Boss round started.");
        }

        UpdateHud();
        ShowRoundAnnouncement();
    }
    private int GetZombieHealthForRound(int roundNumber)
    {
        if (roundNumber == 2) return roundTwoZombieHealth;
        if (roundNumber >= 3) return roundThreeZombieHealth;
        return roundOneZombieHealth;
    }

    private void SpawnZombieRound(int zombieCount)
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
        {
            Debug.LogWarning("[GameManager] No zombie prefab or scene zombie template found.");
            CompleteRound();
            return;
        }

        for (int i = 0; i < zombieCount; i++)
        {
            GameObject prefab = zombiePrefabs[i % zombiePrefabs.Length];
            if (prefab == null) continue;

            Vector3 spawnPosition = GetZombieSpawnPosition(i);
            Quaternion spawnRotation = Quaternion.LookRotation(GetDirectionToPlayer(spawnPosition), Vector3.up);
            EnsureSpawnedEnemyParents();
            GameObject zombie = Instantiate(prefab, spawnPosition, spawnRotation, spawnedZombiesParent);
            zombie.name = $"Round {currentRound} Zombie {i + 1}";
            PrepareSpawnedEnemy(zombie, false);
        }
    }

    private void SpawnBossRound()
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("[GameManager] No Cyber Monsters 2 boss prefab or scene template found.");
            CompleteRound();
            return;
        }

        Vector3 spawnPosition = GetBossSpawnPosition();
        Quaternion spawnRotation = Quaternion.LookRotation(GetDirectionToPlayer(spawnPosition), Vector3.up);
        EnsureSpawnedEnemyParents();
        GameObject boss = Instantiate(bossPrefab, spawnPosition, spawnRotation, spawnedBossesParent);
        boss.name = "Round 3 Boss - Cyber Monsters 2";
        PrepareSpawnedEnemy(boss, true);
    }

    private void PrepareSpawnedEnemy(GameObject enemy, bool isBoss)
    {
        if (enemy == null) return;

        if (!isBoss)
            NormalizeZombieSphereColliders(enemy);

        if (!isBoss)
        {
            foreach (CyberMonsterAI cyberAI in enemy.GetComponentsInChildren<CyberMonsterAI>(true))
                cyberAI.enabled = false;
        }
        else
        {
            foreach (ZombieAI zombieAI in enemy.GetComponentsInChildren<ZombieAI>(true))
                zombieAI.enabled = false;
        }

        float zombieSpeed = GetZombieChaseSpeedForRound(currentRound);
        int zombieDamage = GetZombieAttackDamageForRound(currentRound);
        int zombieHealth = GetZombieHealthForRound(currentRound);

        foreach (ZombieHealth zombieHealthComponent in enemy.GetComponentsInChildren<ZombieHealth>(true))
        {
            zombieHealthComponent.SetHealth(zombieHealth);
        }

        foreach (ZombieAI zombieAI in enemy.GetComponentsInChildren<ZombieAI>(true))
        {
            zombieAI.playerTarget = playerTransform;
            zombieAI.detectionRange = Mathf.Max(zombieAI.detectionRange, MinimumEnemyDetectionRange);
            zombieAI.chaseSpeed = zombieSpeed;
            zombieAI.attackDamage = zombieDamage;
        }

        foreach (CyberMonsterAI cyberAI in enemy.GetComponentsInChildren<CyberMonsterAI>(true))
            cyberAI.detectionRange = Mathf.Max(cyberAI.detectionRange, MinimumEnemyDetectionRange);

        foreach (NavMeshAgent agent in enemy.GetComponentsInChildren<NavMeshAgent>(true))
        {
            if (!isBoss)
                {
                agent.speed = zombieSpeed;
                agent.acceleration = 18f;
                agent.angularSpeed = 720f;
                agent.stoppingDistance = 1.4f;
            }
        }

        enemy.SetActive(true);
        activeEnemies.Add(enemy);
        enemiesRemaining++;
    }

    private void NormalizeZombieSphereColliders(GameObject zombie)
    {
        if (zombie == null) return;

        foreach (SphereCollider col in zombie.GetComponentsInChildren<SphereCollider>(true))
        {
            Vector3 center = col.center;
            center.y = ZombieSphereColliderCenterY;
            col.center = center;
        }
    }

    private void RegisterEnemyKilled(GameObject enemy)
    {
        if (phase != GamePhase.Playing) return;

        if (enemy != null)
            activeEnemies.Remove(enemy);

        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        UpdateHud();

        if (enemiesRemaining <= 0)
            CompleteRound();
    }

    private void CompleteRound()
    {
        if (currentRound == 1)
        {
            StartCoroutine(StartNextRoundAfterDelay(2));
            return;
        }

        if (currentRound == 2)
        {
            StartCoroutine(StartNextRoundAfterDelay(3));
            return;
        }

        ShowVictory();
    }

    private IEnumerator StartNextRoundAfterDelay(int nextRound)
    {
        if (hudText != null)
            hudText.text = currentRound == 2
                ? "Round 2 cleared. Boss incoming..."
                : "Round 1 cleared. Round 2 incoming...";

        yield return new WaitForSeconds(secondsBetweenRounds);
        StartRound(nextRound);
    }

    private void ShowVictory()
    {
        isVictory = true;
        phase = GamePhase.Victory;

        SetPanel(hudPanel, false);
        SetPanel(victoryPanel, true);
        SetGameplayPaused(true);
        Debug.Log("[GameManager] Victory. Cyber Monsters 2 defeated.");
    }

    private void CacheEnemyTemplates()
    {
        ZombieHealth[] sceneZombies = FindObjectsByType<ZombieHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        CyberMonsterHealth[] sceneBosses = FindObjectsByType<CyberMonsterHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
        {
            List<GameObject> discoveredZombies = new List<GameObject>();
            foreach (ZombieHealth zombie in sceneZombies)
            {
                if (zombie == null || zombie.GetComponent<CyberMonsterHealth>() != null) continue;
                discoveredZombies.Add(zombie.gameObject);
            }
            zombiePrefabs = discoveredZombies.ToArray();
        }

        if (bossPrefab == null && sceneBosses.Length > 0)
            bossPrefab = sceneBosses[0].gameObject;

        foreach (GameObject zombiePrefab in zombiePrefabs)
        {
            if (zombiePrefab == null) continue;
            discoveredZombiePositions.Add(zombiePrefab.transform.position);
            DisableSceneTemplate(zombiePrefab);
        }

        DisableSceneTemplate(bossPrefab);
    }

    private void DisableSceneTemplate(GameObject template)
    {
        if (!disableSceneEnemiesOnStart || template == null) return;
        if (template.scene.IsValid() && template.scene == SceneManager.GetActiveScene())
            template.SetActive(false);
    }

    private void EnsureSpawnedEnemyParents()
    {
        spawnedEnemiesRoot = EnsureChildTransform(null, spawnedEnemiesParentName);
        spawnedZombiesParent = EnsureChildTransform(spawnedEnemiesRoot, spawnedZombiesParentName);
        spawnedBossesParent = EnsureChildTransform(spawnedEnemiesRoot, spawnedBossesParentName);
    }

    private Transform EnsureChildTransform(Transform parent, string objectName)
    {
        Transform existing = null;
        if (parent != null)
        {
            existing = parent.Find(objectName);
        }
        else
        {
            GameObject existingObject = GameObject.Find(objectName);
            if (existingObject != null)
                existing = existingObject.transform;
        }

        if (existing == null)
        {
            GameObject created = new GameObject(objectName);
            existing = created.transform;
        }

        existing.gameObject.SetActive(true);
        existing.gameObject.hideFlags = HideFlags.None;
        existing.SetParent(parent, false);
        return existing;
    }

    private Vector3 GetZombieSpawnPosition(int index)
    {
        if (zombieSpawnPoints != null && zombieSpawnPoints.Length > 0)
        {
            Transform point = zombieSpawnPoints[index % zombieSpawnPoints.Length];
            if (point != null) return SampleNavMesh(point.position);
        }

        Vector3 basePosition = playerTransform != null ? playerTransform.position : Vector3.zero;
        Vector3 offset = fallbackSpawnOffsets[index % fallbackSpawnOffsets.Length];

        if (discoveredZombiePositions.Count > index)
            basePosition = discoveredZombiePositions[index];

        return SampleNavMesh(basePosition + offset * (currentRound == 2 ? 1.2f : 1f));
    }

    private Vector3 GetBossSpawnPosition()
    {
        if (bossSpawnPoint != null)
            return SampleNavMesh(bossSpawnPoint.position);

        Vector3 basePosition = playerTransform != null ? playerTransform.position : Vector3.zero;
        return SampleNavMesh(basePosition + new Vector3(0f, 0f, 22f));
    }

    private Vector3 SampleNavMesh(Vector3 requestedPosition)
    {
        if (NavMesh.SamplePosition(requestedPosition, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            return hit.position;

        return requestedPosition;
    }

    private Vector3 GetDirectionToPlayer(Vector3 fromPosition)
    {
        if (playerTransform == null) return Vector3.forward;
        Vector3 direction = playerTransform.position - fromPosition;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }

    private void FindPlayer()
    {
        playerObject = GameObject.FindWithTag("Player");
        playerTransform = playerObject != null ? playerObject.transform : null;
    }

    private void SubscribeToPlayerHealth()
    {
        PlayerHealth foundHealth = FindPlayerHealth();
        if (foundHealth == playerHealth) return;

        if (playerHealth != null)
            playerHealth.HealthChanged -= OnPlayerHealthChanged;

        playerHealth = foundHealth;

        if (playerHealth != null)
            playerHealth.HealthChanged += OnPlayerHealthChanged;
    }

    private PlayerHealth FindPlayerHealth()
    {
        if (playerHealth != null)
            return playerHealth;

        if (playerObject == null)
            FindPlayer();

        if (playerObject == null)
            return null;

        PlayerHealth foundHealth = playerObject.GetComponent<PlayerHealth>();
        if (foundHealth == null)
            foundHealth = playerObject.GetComponentInChildren<PlayerHealth>();

        return foundHealth;
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        UpdateHud();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= OnPlayerHealthChanged;
    }

    private void SetGameplayPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        SetPlayerControls(!paused);

        if (paused)
            UnlockCursor();
        else
            LockCursor();
    }

    private void SetPlayerControls(bool enabled)
    {
        if (playerObject == null)
            FindPlayer();

        if (playerObject == null) return;

        SetComponentEnabledByName(playerObject, "FirstPersonController", enabled);
        SetComponentEnabledByName(playerObject, "PlayerShoot", enabled);
        SetComponentEnabledByName(playerObject, "WeaponHolder", enabled);
    }

    private void SetComponentEnabledByName(GameObject root, string typeName, bool enabled)
    {
        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component != null && component.GetType().Name == typeName)
                component.enabled = enabled;
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateHud()
    {
        SubscribeToPlayerHealth();

        if (hudText != null)
        {
            string roundName = currentRound == 3 ? "Round 3: Cyber Monsters 2" : $"Round {currentRound}: Zombies";
            string targetName = currentRound == 3 ? "Boss health target" : "Zombies left";
            hudText.text = $"{roundName}\n{targetName}: {enemiesRemaining}";
        }

        if (playerHealthText != null)
        {
            PlayerHealth health = FindPlayerHealth();
            playerHealthText.text = health != null
                ? $"HP: {health.GetHealth()}/{health.GetMaxHealth()}"
                : "HP: --/--";
        }
    }

    private void BuildRuntimeUi()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("GameFlowCanvas");
        runtimeCanvas = canvasObject.AddComponent<Canvas>();
        runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        runtimeCanvas.sortingOrder = 50;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        startScreenPanel = CreateStartScreen(runtimeCanvas.transform);
        settingsPanel = CreateSettingsPanel(runtimeCanvas.transform);
        instructionsPanel = CreateInstructionsPanel(runtimeCanvas.transform);
        victoryPanel = CreateVictoryPanel(runtimeCanvas.transform);
        hudPanel = CreateHud(runtimeCanvas.transform);
        roundAnnouncementText = CreateRoundAnnouncementText(runtimeCanvas.transform);
    }

    private void HideRuntimeUi()
    {
        SetPanel(startScreenPanel, false);
        SetPanel(settingsPanel, false);
        SetPanel(instructionsPanel, false);
        SetPanel(victoryPanel, false);
        SetPanel(hudPanel, false);
    }

    private void RestoreReasonableLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.42f, 0.48f, 0.56f);
        RenderSettings.ambientEquatorColor = new Color(0.36f, 0.39f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.23f, 0.24f);
        RenderSettings.ambientIntensity = Mathf.Max(RenderSettings.ambientIntensity, 1.15f);

        if (RenderSettings.fog)
        {
            RenderSettings.fogColor = new Color(0.22f, 0.25f, 0.32f);
            RenderSettings.fogDensity = Mathf.Min(RenderSettings.fogDensity, maxFogDensity);
        }

        Light directionalLight = null;
        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (light != null && light.type == LightType.Directional)
            {
                directionalLight = light;
                break;
            }
        }

        if (directionalLight == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
        }

        directionalLight.color = new Color(1f, 0.96f, 0.86f);
        directionalLight.intensity = Mathf.Max(directionalLight.intensity, minimumDirectionalLightIntensity);
        directionalLight.shadows = LightShadows.Soft;
        directionalLight.shadowStrength = Mathf.Min(directionalLight.shadowStrength, 0.7f);
    }

    private GameObject CreateStartScreen(Transform parent)
    {
        GameObject panel = CreateFullPanel("StartScreen", parent, new Color(0.02f, 0.025f, 0.025f, 0.92f));
        RectTransform content = CreateCenteredColumn(panel.transform, new Vector2(520f, 520f), 18f);

        CreateText("Title", content, gameTitle, 52, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(520f, 88f));
        CreateText("Subtitle", content, "Survive two zombie rounds. Defeat Cyber Monsters 2 on round three.", 22, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.78f, 0.9f, 0.86f), new Vector2(520f, 70f));
        CreateButton("PlayButton", content, "PLAY", ShowInstructionsBoard);
        CreateButton("SettingsButton", content, "SETTINGS", ShowSettings);
        CreateButton("QuitButton", content, "QUIT GAME", QuitGame);

        return panel;
    }

    private GameObject CreateSettingsPanel(Transform parent)
    {
        GameObject panel = CreateFullPanel("SettingsPanel", parent, new Color(0.02f, 0.025f, 0.025f, 0.94f));
        RectTransform content = CreateCenteredColumn(panel.transform, new Vector2(620f, 430f), 16f);

        CreateText("SettingsTitle", content, "Settings", 44, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(620f, 70f));
        CreateSliderRow(content, "Master Volume", AudioListener.volume, 0f, 1f, OnVolumeChanged, out volumeValueText);
        CreateSliderRow(content, "Mouse Sensitivity", sensitivitySetting, 0.35f, 2.5f, OnSensitivityChanged, out sensitivityValueText);
        CreateButton("BackButton", content, "BACK", BackToStartFromSettings);

        OnVolumeChanged(AudioListener.volume);
        OnSensitivityChanged(sensitivitySetting);
        return panel;
    }

    private GameObject CreateInstructionsPanel(Transform parent)
    {
        GameObject panel = CreateFullPanel("InstructionsPanel", parent, new Color(0.02f, 0.025f, 0.025f, 0.94f));
        RectTransform content = CreateCenteredColumn(panel.transform, new Vector2(760f, 660f), 16f);

        CreateText("BoardTitle", content, "Mission Board", 46, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(760f, 72f));
        string instructions =
            "Round 1: zombies attack from several places.\n" +
            "Round 2: a larger zombie wave arrives.\n" +
            "Round 3: Cyber Monsters 2 enters as the boss.\n\n" +
            "Controls: WASD move, mouse aim, left click shoot, E pick up weapon, G drop weapon.\n" +
            "After game over, press R to restart.";
        CreateText("InstructionText", content, instructions, 24, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.86f, 0.93f, 0.9f), new Vector2(760f, 250f));
        weaponListText = CreateText("WeaponText", content, "", 24, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.52f), new Vector2(760f, 90f));
        CreateButton("StartRoundButton", content, "START ROUND 1", StartRoundOneFromBoard);
        CreateButton("BackToMenuButton", content, "BACK", ShowStartScreen);

        return panel;
    }

    private GameObject CreateVictoryPanel(Transform parent)
    {
        GameObject panel = CreateFullPanel("VictoryPanel", parent, new Color(0.02f, 0.025f, 0.025f, 0.94f));
        RectTransform content = CreateCenteredColumn(panel.transform, new Vector2(620f, 420f), 18f);

        CreateText("VictoryTitle", content, "Victory", 52, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(620f, 86f));
        CreateText("VictoryText", content, "Cyber Monsters 2 is down. The playground is clear.", 24, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.78f, 0.9f, 0.86f), new Vector2(620f, 80f));
        CreateButton("RestartButton", content, "PLAY AGAIN", RestartScene);
        CreateButton("VictoryQuitButton", content, "QUIT GAME", QuitGame);

        return panel;
    }

    private GameObject CreateHud(Transform parent)
    {
        GameObject panel = new GameObject("RoundHud");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(360f, 124f);

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.02f, 0.025f, 0.025f, 0.72f);

        hudText = CreateText("RoundHudText", rect, "", 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, new Vector2(328f, 68f));
        hudText.rectTransform.anchorMin = new Vector2(0f, 1f);
        hudText.rectTransform.anchorMax = new Vector2(0f, 1f);
        hudText.rectTransform.pivot = new Vector2(0f, 1f);
        hudText.rectTransform.anchoredPosition = new Vector2(16f, -12f);

        playerHealthText = CreateText("PlayerHealthText", rect, "HP: --/--", 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.88f, 0.52f), new Vector2(328f, 34f));
        playerHealthText.rectTransform.anchorMin = new Vector2(0f, 1f);
        playerHealthText.rectTransform.anchorMax = new Vector2(0f, 1f);
        playerHealthText.rectTransform.pivot = new Vector2(0f, 1f);
        playerHealthText.rectTransform.anchoredPosition = new Vector2(16f, -82f);
        return panel;
    }

    private Text CreateRoundAnnouncementText(Transform parent)
    {
        Text text = CreateText("RoundAnnouncementText", parent, "", 54, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(560f, 90f));
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -92f);
        text.gameObject.SetActive(false);
        return text;
    }

    private void ShowRoundAnnouncement()
    {
        if (roundAnnouncementText == null) return;

        if (roundAnnouncementCoroutine != null)
            StopCoroutine(roundAnnouncementCoroutine);

        string message = currentRound == 3 ? "BOSS ROUND" : $"ROUND {currentRound}";
        roundAnnouncementCoroutine = StartCoroutine(ShowRoundAnnouncementRoutine(message));
    }

    private IEnumerator ShowRoundAnnouncementRoutine(string message)
    {
        roundAnnouncementText.text = message;
        roundAnnouncementText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        roundAnnouncementText.gameObject.SetActive(false);
        roundAnnouncementCoroutine = null;
    }

    private float GetZombieChaseSpeedForRound(int roundNumber)
    {
        if (roundNumber == 2) return 4.0f;
        if (roundNumber >= 3) return 4.5f;
        return 2.8f;
    }

    private int GetZombieAttackDamageForRound(int roundNumber)
    {
        if (roundNumber == 2) return 18;
        if (roundNumber >= 3) return 25;
        return 10;
    }

    private void CreateSliderRow(Transform parent, string label, float value, float min, float max, UnityAction<float> onChanged, out Text valueText)
    {
        GameObject row = new GameObject(label + "Row");
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(620f, 78f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 16f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        CreateText(label + "Label", row.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, new Vector2(220f, 54f));
        Slider slider = CreateSlider(label + "Slider", row.transform, value, min, max);
        slider.onValueChanged.AddListener(onChanged);
        valueText = CreateText(label + "Value", row.transform, "", 20, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.78f, 0.9f, 0.86f), new Vector2(86f, 54f));
    }

    private Slider CreateSlider(string name, Transform parent, float value, float min, float max)
    {
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(270f, 38f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        Image background = CreateImage("Background", sliderObject.transform, new Color(0.12f, 0.14f, 0.14f, 1f));
        SetAnchors(background.rectTransform, Vector2.zero, Vector2.one);
        background.rectTransform.offsetMin = new Vector2(0f, 13f);
        background.rectTransform.offsetMax = new Vector2(0f, -13f);

        Image fill = CreateImage("Fill", sliderObject.transform, new Color(0.1f, 0.72f, 0.5f, 1f));
        SetAnchors(fill.rectTransform, Vector2.zero, new Vector2(1f, 1f));
        fill.rectTransform.offsetMin = new Vector2(0f, 13f);
        fill.rectTransform.offsetMax = new Vector2(0f, -13f);

        Image handle = CreateImage("Handle", sliderObject.transform, new Color(1f, 0.88f, 0.52f, 1f));
        handle.rectTransform.sizeDelta = new Vector2(24f, 34f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;

        return slider;
    }

    private Button CreateButton(string name, Transform parent, string label, UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 58f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.1f, 0.55f, 0.42f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.1f, 0.55f, 0.42f, 1f);
        colors.highlightedColor = new Color(0.16f, 0.68f, 0.53f, 1f);
        colors.pressedColor = new Color(0.08f, 0.42f, 0.34f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateText(name + "Text", buttonObject.transform, label, 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, rect.sizeDelta);
        SetAnchors(text.rectTransform, Vector2.zero, Vector2.one);
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        return button;
    }

    private Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle style, TextAnchor alignment, Color color, Vector2 size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text uiText = textObject.AddComponent<Text>();
        uiText.font = GetRuntimeFont();
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.fontStyle = style;
        uiText.alignment = alignment;
        uiText.color = color;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = uiText.rectTransform;
        rect.sizeDelta = size;
        return uiText;
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private GameObject CreateFullPanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        SetAnchors(rect, Vector2.zero, Vector2.one);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = color;

        return panel;
    }

    private RectTransform CreateCenteredColumn(Transform parent, Vector2 size, float spacing)
    {
        GameObject content = new GameObject("Content");
        content.transform.SetParent(parent, false);
        RectTransform rect = content.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = spacing;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        return rect;
    }

    private void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
    }

    private Font GetRuntimeFont()
    {
        if (runtimeFont != null) return runtimeFont;

        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return runtimeFont;
    }

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    private void RefreshWeaponList()
    {
        if (weaponListText == null) return;

        WeaponPickup[] pickups = FindObjectsByType<WeaponPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (pickups.Length == 0)
        {
            weaponListText.text = "Weapon pick: place a weapon pickup in the scene, then press E near it.";
            return;
        }

        List<string> names = new List<string>();
        foreach (WeaponPickup pickup in pickups)
        {
            if (pickup != null && !names.Contains(pickup.weaponName))
                names.Add(pickup.weaponName);
        }

        weaponListText.text = "Weapon pick: " + string.Join(", ", names) + ". Press E when you are close to equip.";
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void OnSensitivityChanged(float value)
    {
        sensitivitySetting = value;
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("0.00") + "x";

        ApplySensitivity(value);
    }

    private void ApplySensitivity(float value)
    {
        if (playerObject == null)
            FindPlayer();

        if (playerObject == null) return;

        foreach (MonoBehaviour component in playerObject.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null || component.GetType().Name != "FirstPersonController") continue;

            FieldInfo rotationSpeed = component.GetType().GetField("RotationSpeed", BindingFlags.Instance | BindingFlags.Public);
            if (rotationSpeed != null)
                rotationSpeed.SetValue(component, value);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }
}
