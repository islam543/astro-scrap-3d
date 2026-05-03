using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    [Header("Scenes")]
    public string gameSceneName = "3DGame";

    private Font menuFont;

    void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureEventSystem();
        BuildMenu();
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    private void BuildMenu()
    {
        if (GameObject.Find("StartMenuCanvas") != null)
            return;

        GameObject canvasObject = new GameObject("StartMenuCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("Background");
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.035f, 0.04f, 0.045f, 1f);

        GameObject content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(620f, 360f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 24f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        CreateText(content.transform, "CYBER ZOMBIE SIEGE", 54, FontStyle.Bold, Color.white, new Vector2(620f, 95f));
        CreateText(content.transform, "Survive the zombie waves and defeat Cyber Monsters 2.", 24, FontStyle.Normal, new Color(0.82f, 0.92f, 0.88f), new Vector2(620f, 64f));
        CreatePlayButton(content.transform);
    }

    private void CreatePlayButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("PlayButton");
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320f, 64f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.55f, 0.42f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(PlayGame);

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.55f, 0.42f, 1f);
        colors.highlightedColor = new Color(0.13f, 0.7f, 0.53f, 1f);
        colors.pressedColor = new Color(0.06f, 0.4f, 0.33f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text label = CreateText(buttonObject.transform, "PLAY", 28, FontStyle.Bold, Color.white, rect.sizeDelta);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private Text CreateText(Transform parent, string value, int size, FontStyle style, Color color, Vector2 rectSize)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = GetFont();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.rectTransform.sizeDelta = rectSize;
        return text;
    }

    private Font GetFont()
    {
        if (menuFont != null) return menuFont;

        menuFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (menuFont == null)
            menuFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return menuFont;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }
}
