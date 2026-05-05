using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Attach this to a new empty GameObject in the 3DGame scene (e.g. "PauseMenuController").
/// Handles Escape toggling the pause screen.
/// Buttons: Continue | Settings | Quit
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Scene names")]
    [Tooltip("The main-menu scene to load when Quit is pressed (must be in Build Settings).")]
    public string mainMenuScene = "StartScene";

    // ── panels (built at runtime) ─────────────────────────────
    private Canvas   pauseCanvas;
    private GameObject pausePanel;
    private GameObject settingsPanel;

    private bool isPaused = false;

    // ── font cache ────────────────────────────────────────────
    private Font uiFont;

    // ── lifecycle ─────────────────────────────────────────────
    void Start()
    {
        EnsureEventSystem();
        BuildCanvas();
        SetPaused(false);          // start hidden
    }

    void Update()
    {
        // Don't allow pausing while GameManager shows its own overlay
        if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct && !isPaused)
            return;

        bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        if (escPressed)
            TogglePause();
    }

    // ── public button callbacks ───────────────────────────────
    public void Continue()
    {
        SetPaused(false);
    }

    public void OpenSettings()
    {
        if (pausePanel    != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel    != null) pausePanel.SetActive(true);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;            // IMPORTANT: always reset before scene load

        // Check whether the main-menu scene exists in Build Settings
        int idx = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + mainMenuScene + ".unity");
        if (idx >= 0)
        {
            SceneManager.LoadScene(mainMenuScene);
        }
        else
        {
            Debug.LogWarning($"[PauseMenuController] Scene '{mainMenuScene}' not in Build Settings. Quitting instead.");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    // ── pause logic ───────────────────────────────────────────
    void TogglePause()
    {
        SetPaused(!isPaused);
    }

    void SetPaused(bool paused)
    {
        isPaused = paused;

        Time.timeScale = paused ? 0f : 1f;

        // Cursor
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = paused;

        // Show/hide panels
        if (pausePanel    != null) pausePanel.SetActive(paused);
        if (settingsPanel != null) settingsPanel.SetActive(false); // always hide settings on resume

        // Disable player controls while paused (so WASD / mouse look don't register)
        SetPlayerControls(!paused);
    }

    void SetPlayerControls(bool enabled)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        foreach (MonoBehaviour mb in player.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            string t = mb.GetType().Name;
            if (t == "FirstPersonController" || t == "PlayerShoot" || t == "WeaponHolder")
                mb.enabled = enabled;
        }
    }

    // ── canvas builder ────────────────────────────────────────
    void BuildCanvas()
    {
        if (GameObject.Find("PauseMenuCanvas") != null) return;

        GameObject canvasGO = new GameObject("PauseMenuCanvas");
        pauseCanvas = canvasGO.AddComponent<Canvas>();
        pauseCanvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        pauseCanvas.sortingOrder  = 100;   // on top of everything
        canvasGO.AddComponent<GraphicRaycaster>();

        CanvasScaler sc = canvasGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.5f;

        pausePanel    = BuildPausePanel(canvasGO.transform);
        settingsPanel = BuildSettingsPanel(canvasGO.transform);
    }

    GameObject BuildPausePanel(Transform parent)
    {
        // Semi-transparent dark overlay
        GameObject overlay = CreateFullPanel("PauseOverlay", parent, new Color(0f, 0f, 0f, 0.65f));

        RectTransform col = CreateColumn(overlay.transform, new Vector2(380f, 440f), 18f);

        MakeText(col, "PAUSED", 52, FontStyle.Bold, Color.white, new Vector2(380f, 80f));
        MakeSpacer(col, 6f);
        MakeButton(col, "CONTINUE", new Color(0.08f, 0.54f, 0.40f), Continue);
        MakeButton(col, "SETTINGS", new Color(0.18f, 0.28f, 0.40f), OpenSettings);
        MakeButton(col, "QUIT TO MENU", new Color(0.40f, 0.10f, 0.10f), QuitToMenu);

        return overlay;
    }

    GameObject BuildSettingsPanel(Transform parent)
    {
        GameObject overlay = CreateFullPanel("SettingsOverlay", parent, new Color(0f, 0f, 0f, 0.75f));

        RectTransform col = CreateColumn(overlay.transform, new Vector2(460f, 380f), 18f);

        MakeText(col, "SETTINGS", 44, FontStyle.Bold, Color.white, new Vector2(460f, 70f));
        MakeText(col, "Master Volume", 22, FontStyle.Normal,
                 new Color(0.78f, 0.9f, 0.86f), new Vector2(460f, 36f));
        MakeSlider(col, AudioListener.volume, 0f, 1f, v => AudioListener.volume = v);
        MakeSpacer(col, 8f);
        MakeButton(col, "BACK", new Color(0.18f, 0.28f, 0.40f), CloseSettings);

        return overlay;
    }

    // ── UI helpers ────────────────────────────────────────────
    GameObject CreateFullPanel(string goName, Transform parent, Color color)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    RectTransform CreateColumn(Transform parent, Vector2 size, float spacing)
    {
        GameObject col = new GameObject("Column");
        col.transform.SetParent(parent, false);
        RectTransform r = col.AddComponent<RectTransform>();
        r.anchorMin        = new Vector2(0.5f, 0.5f);
        r.anchorMax        = new Vector2(0.5f, 0.5f);
        r.pivot            = new Vector2(0.5f, 0.5f);
        r.sizeDelta        = size;
        r.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.MiddleCenter;
        vlg.spacing                = spacing;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth  = false;
        return r;
    }

    Text MakeText(RectTransform parent, string content, int size, FontStyle style,
                  Color color, Vector2 rectSize)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font               = GetFont();
        t.text               = content;
        t.fontSize           = size;
        t.fontStyle          = style;
        t.color              = color;
        t.alignment          = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        t.rectTransform.sizeDelta = rectSize;
        return t;
    }

    void MakeSpacer(RectTransform parent, float height)
    {
        GameObject go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(10f, height);
    }

    void MakeButton(RectTransform parent, string label, Color bg,
                    UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(340f, 62f);

        Image img = go.AddComponent<Image>();
        img.color = bg;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        ColorBlock cb = btn.colors;
        cb.normalColor      = bg;
        cb.highlightedColor = bg * 1.25f;
        cb.pressedColor     = bg * 0.75f;
        cb.selectedColor    = cb.highlightedColor;
        btn.colors          = cb;

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        Text t = txtGO.AddComponent<Text>();
        t.font       = GetFont();
        t.text       = label;
        t.fontSize   = 24;
        t.fontStyle  = FontStyle.Bold;
        t.alignment  = TextAnchor.MiddleCenter;
        t.color      = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        RectTransform tr = t.rectTransform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
    }

    void MakeSlider(RectTransform parent, float value, float min, float max,
                    UnityEngine.Events.UnityAction<float> onChange)
    {
        GameObject go = new GameObject("Slider");
        go.transform.SetParent(parent, false);
        RectTransform sr = go.AddComponent<RectTransform>();
        sr.sizeDelta = new Vector2(340f, 40f);

        Slider slider = go.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value    = value;

        Image bgImg = CreateImage("BG", go.transform, new Color(0.12f, 0.14f, 0.16f));
        bgImg.rectTransform.anchorMin = Vector2.zero;
        bgImg.rectTransform.anchorMax = Vector2.one;
        bgImg.rectTransform.offsetMin = new Vector2(0, 14f);
        bgImg.rectTransform.offsetMax = new Vector2(0, -14f);

        Image fillImg = CreateImage("Fill", go.transform, new Color(0.1f, 0.7f, 0.5f));
        fillImg.rectTransform.anchorMin = Vector2.zero;
        fillImg.rectTransform.anchorMax = Vector2.one;
        fillImg.rectTransform.offsetMin = new Vector2(0, 14f);
        fillImg.rectTransform.offsetMax = new Vector2(0, -14f);

        Image handleImg = CreateImage("Handle", go.transform, new Color(1f, 0.88f, 0.52f));
        handleImg.rectTransform.sizeDelta = new Vector2(22f, 32f);

        slider.fillRect      = fillImg.rectTransform;
        slider.handleRect    = handleImg.rectTransform;
        slider.targetGraphic = handleImg;
        slider.onValueChanged.AddListener(onChange);
    }

    Image CreateImage(string goName, Transform parent, Color color)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    Font GetFont()
    {
        if (uiFont != null) return uiFont;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return uiFont;
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }
}
