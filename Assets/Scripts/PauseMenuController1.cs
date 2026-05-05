using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController1 : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused;
    private GameObject pauseRoot;

    private void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;

        CachePauseUiReferences();
        ConfigurePauseUiSafety();
        SetPauseUiActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Escape is handled here in builds. If the Unity Editor exits Play Mode first,
        // rebind the Editor "Exit Play Mode" shortcut away from Escape.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                Continue();
            else
                Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;

        SetPauseUiActive(true);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Continue()
    {
        isPaused = false;

        SetPauseUiActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void CachePauseUiReferences()
    {
        if (pausePanel == null)
            pausePanel = FindSceneGameObject("PausePanel");

        if (settingsPanel == null)
            settingsPanel = FindSceneGameObject("SettingsPanel");

        if (pausePanel != null)
        {
            Canvas containingCanvas = pausePanel.GetComponentInParent<Canvas>(true);
            if (containingCanvas != null)
            {
                pauseRoot = containingCanvas.gameObject;
                return;
            }
        }

        pauseRoot = FindPauseCanvas();
        if (pauseRoot == null)
            pauseRoot = pausePanel;
    }

    private void ConfigurePauseUiSafety()
    {
        GameObject root = GetPauseUiRoot();
        if (root == null) return;

        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
        }

        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.GetComponent<Button>() == null)
                image.raycastTarget = false;
        }
    }

    private void SetPauseUiActive(bool active)
    {
        GameObject root = GetPauseUiRoot();
        if (root != null)
            root.SetActive(active);
    }

    private GameObject GetPauseUiRoot()
    {
        return pauseRoot != null ? pauseRoot : pausePanel;
    }

    private GameObject FindPauseCanvas()
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas != null && (canvas.name == "PauseCanvas" || canvas.name == "PauseMenuCanvas"))
                return canvas.gameObject;
        }

        return null;
    }

    private GameObject FindSceneGameObject(string objectName)
    {
        foreach (Transform sceneTransform in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sceneTransform != null && sceneTransform.name == objectName)
                return sceneTransform.gameObject;
        }

        return null;
    }
}
