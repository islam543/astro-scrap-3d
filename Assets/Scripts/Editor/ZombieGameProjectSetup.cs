using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ZombieGameProjectSetup
{
    private const string StartScenePath = "Assets/Scenes/StartScene.unity";
    private const string GameScenePath = "Assets/Scenes/3DGame.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Zombie Game/Apply Requested Fixes")]
    public static void ApplyRequestedFixes()
    {
        CreateStartScene();
        FixMainSceneLightingAndFlow();
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[ZombieGameProjectSetup] Applied start scene, lighting, build settings, and game flow fixes.");
    }

    private static void CreateStartScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 1.2f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.04f, 0.045f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 1000f;
        cameraObject.AddComponent<AudioListener>();

        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = new Color(1f, 0.96f, 0.86f);
        light.shadows = LightShadows.Soft;

        GameObject menuObject = new GameObject("StartMenuController");
        StartMenuController controller = menuObject.AddComponent<StartMenuController>();
        controller.gameSceneName = "3DGame";

        EditorSceneManager.SaveScene(scene, StartScenePath);
    }

    private static void FixMainSceneLightingAndFlow()
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.42f, 0.48f, 0.56f);
        RenderSettings.ambientEquatorColor = new Color(0.36f, 0.39f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.23f, 0.24f);
        RenderSettings.ambientIntensity = 1.15f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.22f, 0.25f, 0.32f);
        RenderSettings.fogDensity = 0.008f;

        Light directionalLight = FindDirectionalLight();
        if (directionalLight == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
        }

        directionalLight.color = new Color(1f, 0.96f, 0.86f);
        directionalLight.intensity = 1.4f;
        directionalLight.shadows = LightShadows.Soft;
        directionalLight.shadowStrength = 0.65f;

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            SerializedObject serialized = new SerializedObject(gameManager);
            SetBool(serialized, "showStartScreenOnAwake", false);
            SetBool(serialized, "createRuntimeMenus", true);
            SetBool(serialized, "startFirstRoundOnAwake", true);
            SetBool(serialized, "autoFixSceneLighting", true);
            SetFloat(serialized, "minimumDirectionalLightIntensity", 1.35f);
            SetFloat(serialized, "maxFogDensity", 0.008f);
            SetFloat(serialized, "zombieMoveSpeed", 1.6f);
            SetString(serialized, "spawnedEnemiesParentName", "SpawnedEnemies");
            SetString(serialized, "spawnedZombiesParentName", "Zombies");
            SetString(serialized, "spawnedBossesParentName", "Bosses");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameManager);
        }
        else
        {
            Debug.LogWarning("[ZombieGameProjectSetup] No GameManager found in 3DGame. Add one if rounds should auto-spawn.");
        }

        int zombieTemplates = Object.FindObjectsByType<ZombieHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        if (zombieTemplates == 0)
            Debug.LogWarning("[ZombieGameProjectSetup] No ZombieHealth objects found in 3DGame. Assign zombiePrefabs on GameManager or place a zombie template in the scene.");

        EditorSceneManager.SaveScene(scene);
    }

    private static Light FindDirectionalLight()
    {
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (light != null && light.type == LightType.Directional)
                return light;
        }

        return null;
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(StartScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
            new EditorBuildSettingsScene(SampleScenePath, true)
        };
    }

    private static void SetBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.boolValue = value;
    }

    private static void SetFloat(SerializedObject serialized, string propertyName, float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    private static void SetString(SerializedObject serialized, string propertyName, string value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.stringValue = value;
    }
}
