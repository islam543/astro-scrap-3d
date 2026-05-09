using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Attached to a GameObject in StartScene.
/// Builds a full main-menu canvas at runtime: Play, Settings, Quit.
/// Settings panel includes Volume and Mouse Sensitivity sliders (saved via PlayerPrefs).
/// </summary>
public class StartMenuController : MonoBehaviour
{
    public const string PrefSensitivity = "MouseSensitivity";
    public const string PrefVolume      = "MasterVolume";

    [Header("Scene to load when Play is pressed")]
    public string gameSceneName = "3DGame";

    // ── panels ────────────────────────────────────────────────
    private GameObject mainPanel;
    private GameObject settingsPanel;

    // ── font cache ────────────────────────────────────────────
    private Font menuFont;

    // ── Unity lifecycle ───────────────────────────────────────
    void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Load saved volume
        float savedVolume = PlayerPrefs.GetFloat(PrefVolume, 1f);
        AudioListener.volume = savedVolume;

        EnsureEventSystem();
        BuildCanvas();
    }

    // ── button callbacks ──────────────────────────────────────
    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (mainPanel    != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel     != null) mainPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("[StartMenuController] Quit pressed.");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ── canvas builder ────────────────────────────────────────
    void BuildCanvas()
    {
        if (GameObject.Find("StartMenuCanvas") != null) return; // already built

        // Root canvas
        GameObject canvasGO = new GameObject("StartMenuCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        // Dark full-screen background
        GameObject bg = CreateFullPanel("Background", canvasGO.transform, new Color(0.04f, 0.04f, 0.06f, 1f));

        // Main menu panel
        mainPanel = CreateMainPanel(bg.transform);

        // Settings panel (starts hidden)
        settingsPanel = CreateSettingsPanel(bg.transform);
        settingsPanel.SetActive(false);
    }

    // ── main menu ─────────────────────────────────────────────
    GameObject CreateMainPanel(Transform parent)
    {
        GameObject panel = new GameObject("MainPanel");
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        FillParent(rect);

        RectTransform column = CreateColumn(panel.transform, new Vector2(400f, 500f), 20f);

        // Title
        MakeText(column, "CYBER ZOMBIE SIEGE", 52, FontStyle.Bold, Color.white, new Vector2(400f, 80f));
        MakeText(column, "Survive. Shoot. Conquer.", 22, FontStyle.Normal,
                 new Color(0.75f, 0.88f, 0.82f), new Vector2(400f, 40f));

        // Spacer
        MakeSpacer(column, 10f);

        // Buttons
        MakeButton(column, "PLAY",     new Color(0.08f, 0.54f, 0.40f), PlayGame);
        MakeButton(column, "SETTINGS", new Color(0.18f, 0.28f, 0.40f), OpenSettings);
        MakeButton(column, "QUIT",     new Color(0.40f, 0.10f, 0.10f), QuitGame);

        return panel;
    }

    // ── settings panel ────────────────────────────────────────
    GameObject CreateSettingsPanel(Transform parent)
    {
        GameObject panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        FillParent(rect);

        // Taller column to fit both sliders
        RectTransform column = CreateColumn(panel.transform, new Vector2(480f, 520f), 16f);

        MakeText(column, "SETTINGS", 44, FontStyle.Bold, Color.white, new Vector2(480f, 70f));

        // ── Volume ──
        MakeText(column, "Volume", 22, FontStyle.Normal, new Color(0.82f, 0.88f, 0.82f), new Vector2(480f, 36f));
        float savedVol = PlayerPrefs.GetFloat(PrefVolume, 1f);
        MakeSlider(column, savedVol, 0f, 1f, v =>
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(PrefVolume, v);
            PlayerPrefs.Save();
        });

        MakeSpacer(column, 8f);

        // ── Mouse Sensitivity ──
        MakeText(column, "Mouse Sensitivity", 22, FontStyle.Normal, new Color(0.82f, 0.88f, 0.82f), new Vector2(480f, 36f));
        float savedSens = PlayerPrefs.GetFloat(PrefSensitivity, 1f);
        MakeSlider(column, savedSens, 0.1f, 3f, v =>
        {
            PlayerPrefs.SetFloat(PrefSensitivity, v);
            PlayerPrefs.Save();
            Debug.Log($"[StartMenuController] Sensitivity saved: {v:0.00}x");
        });

        MakeSpacer(column, 8f);
        MakeButton(column, "BACK", new Color(0.18f, 0.28f, 0.40f), CloseSettings);

        return panel;
    }

    // ── helper builders ───────────────────────────────────────
    GameObject CreateFullPanel(string goName, Transform parent, Color color)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        RectTransform r = go.AddComponent<RectTransform>();
        FillParent(r);
        go.AddComponent<Image>().color = color;
        return go;
    }

    RectTransform CreateColumn(Transform parent, Vector2 size, float spacing)
    {
        GameObject col = new GameObject("Column");
        col.transform.SetParent(parent, false);
        RectTransform rect = col.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.sizeDelta        = size;
        rect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment          = TextAnchor.MiddleCenter;
        vlg.spacing                 = spacing;
        vlg.childForceExpandHeight  = false;
        vlg.childForceExpandWidth   = false;
        return rect;
    }

    Text MakeText(RectTransform parent, string content, int size, FontStyle style,
                  Color color, Vector2 rectSize)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font                  = GetFont();
        t.text                  = content;
        t.fontSize              = size;
        t.fontStyle             = style;
        t.color                 = color;
        t.alignment             = TextAnchor.MiddleCenter;
        t.horizontalOverflow    = HorizontalWrapMode.Wrap;
        t.verticalOverflow      = VerticalWrapMode.Overflow;
        t.rectTransform.sizeDelta = rectSize;
        return t;
    }

    void MakeSpacer(RectTransform parent, float height)
    {
        GameObject go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(10f, height);
    }

    void MakeButton(RectTransform parent, string label, Color bg, UnityEngine.Events.UnityAction onClick)
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

        // Label inside button
        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        Text t = txtGO.AddComponent<Text>();
        t.font       = GetFont();
        t.text       = label;
        t.fontSize   = 26;
        t.fontStyle  = FontStyle.Bold;
        t.alignment  = TextAnchor.MiddleCenter;
        t.color      = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        RectTransform tr = t.rectTransform;
        tr.anchorMin  = Vector2.zero;
        tr.anchorMax  = Vector2.one;
        tr.offsetMin  = Vector2.zero;
        tr.offsetMax  = Vector2.zero;
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

        // Background
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(go.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.14f, 0.16f);
        RectTransform bgR = bgImg.rectTransform;
        bgR.anchorMin  = Vector2.zero; bgR.anchorMax = Vector2.one;
        bgR.offsetMin  = new Vector2(0, 14f); bgR.offsetMax = new Vector2(0, -14f);

        // Fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(go.transform, false);
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.1f, 0.7f, 0.5f);
        RectTransform fillR = fillImg.rectTransform;
        fillR.anchorMin = Vector2.zero; fillR.anchorMax = Vector2.one;
        fillR.offsetMin = new Vector2(0, 14f); fillR.offsetMax = new Vector2(0, -14f);

        // Handle
        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(go.transform, false);
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = new Color(1f, 0.88f, 0.52f);
        handleImg.rectTransform.sizeDelta = new Vector2(22f, 32f);

        slider.fillRect   = fillR;
        slider.handleRect = handleImg.rectTransform;
        slider.targetGraphic = handleImg;
        slider.onValueChanged.AddListener(onChange);
    }

    static void FillParent(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    Font GetFont()
    {
        if (menuFont != null) return menuFont;
        menuFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (menuFont == null)
            menuFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return menuFont;
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }
}
