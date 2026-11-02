using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UnifiedMiniGameMenusSetup
{
    private const string Simon = "Assets/Scenes/SimonScene.unity";
    private const string Basic = "Assets/Scenes/BasicrhythmScene.unity";
    private const string Tempo = "Assets/Scenes/TempoTapScene.unity";
    private const string SharedPause = "Assets/Prefabs/UI/SharedPausePanel.prefab";
    private const string Palette = "Assets/Sprites/CartoonButtonsPalette/pill_";
    private const string Window = "Assets/Sprites/Cartoon-Buttons-FREE-Sample/Cartoon-Buttons-FREE-Sample/png/2x/candy/panels/panel_window_320x256.png";

    [MenuItem("Tools/UI/Unify Mini-Game Pause Menus")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || !original.IsValid() || original.isDirty)
            throw new System.InvalidOperationException("Save open scenes and leave Play Mode before updating pause menus.");

        string originalPath = original.path;
        try
        {
            EditorSceneManager.OpenScene(Simon, OpenSceneMode.Single);
            Transform source = Find(Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include).transform, "PausePanel");
            if (source == null)
                throw new System.InvalidOperationException("Simon's pause panel is missing.");
            PrefabUtility.SaveAsPrefabAsset(source.gameObject, SharedPause);

            StyleBasic();
            StyleTempo();
        }
        finally
        {
            EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        }
        Debug.Log("Shared pause panel and Cartoon start button configured for both rhythm games.");
    }

    private static void StyleBasic()
    {
        Scene scene = EditorSceneManager.OpenScene(Basic, OpenSceneMode.Single);
        Transform panel = Find(Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include).transform, "PausePanel");
        if (panel == null) throw new System.InvalidOperationException("Basic Rhythm pause panel is missing.");
        Transform box = panel.Find("PauseBox");
        Image dimmer = panel.GetComponent<Image>();
        dimmer.color = new Color(0.04f, 0.10f, 0.20f, 0.8f);
        dimmer.raycastTarget = true;

        RectTransform boxRect = (RectTransform)box;
        boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = new Vector2(0f, -32f);
        boxRect.sizeDelta = new Vector2(500f, 500f);
        Image window = box.GetComponent<Image>();
        window.sprite = Load(Window);
        window.type = Image.Type.Sliced;
        window.preserveAspect = false;
        window.color = Color.white;
        window.raycastTarget = false;

        TMP_FontAsset font = box.Find("ResumeButton").GetComponentInChildren<TMP_Text>(true)?.font;
        StyleButton(box, "ResumeButton", "green", "CONTINUAR", new Vector2(0f, 68f), new Vector2(310f, 88f), 30f, font);
        StyleButton(box, "RestartButton", "yellow", "REINICIAR", new Vector2(0f, -33f), new Vector2(310f, 88f), 30f, font);
        StyleButton(box, "MenuButton", "red", "SALIR AL MENÚ", new Vector2(0f, -134f), new Vector2(310f, 88f), 27f, font);
        StyleButton(box, "CloseButton", "red", "X", new Vector2(210f, 207f), new Vector2(66f, 66f), 36f, font);
        Transform title = box.Find("PauseTitle");
        if (title == null)
        {
            GameObject titleObject = new GameObject("PauseTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            title = titleObject.transform;
            title.SetParent(box, false);
        }
        RectTransform rect = (RectTransform)title;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 186f);
        rect.sizeDelta = new Vector2(320f, 62f);
        TMP_Text heading = title.GetComponent<TMP_Text>();
        heading.text = "EN PAUSA";
        if (font != null) heading.font = font;
        heading.fontSize = 44f;
        heading.fontStyle = FontStyles.Bold;
        heading.alignment = TextAlignmentOptions.Center;
        heading.color = new Color32(69, 38, 82, 255);
        heading.raycastTarget = false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void StyleTempo()
    {
        Scene scene = EditorSceneManager.OpenScene(Tempo, OpenSceneMode.Single);
        TempoTapGameManager manager = Object.FindFirstObjectByType<TempoTapGameManager>(FindObjectsInactive.Include);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        TempoTapWorldScroll scroll = Object.FindFirstObjectByType<TempoTapWorldScroll>(FindObjectsInactive.Include);
        if (manager == null || canvas == null || scroll == null)
            throw new System.InvalidOperationException("Tempo Tap manager, Canvas or world scroll is missing.");

        Transform pause = Find(canvas.transform, "PausePanel");
        if (pause == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPause);
            pause = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).transform;
            pause.SetParent(canvas.transform, false);
        }
        SerializedObject managerData = new SerializedObject(manager);
        managerData.FindProperty("pausePanel").objectReferenceValue = pause.gameObject;
        managerData.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject scrollData = new SerializedObject(scroll);
        scrollData.FindProperty("runner").objectReferenceValue = manager.runner;
        scrollData.ApplyModifiedPropertiesWithoutUndo();

        Transform start = manager.startPanel != null ? manager.startPanel.transform.Find("StartButton") : null;
        if (start == null)
            throw new System.InvalidOperationException("Tempo Tap StartButton is missing.");
        TMP_FontAsset font = manager.feedbackText != null ? manager.feedbackText.font : null;
        StyleButton(start.parent, "StartButton", "orange", "JUGAR", new Vector2(0f, -245f),
            new Vector2(219f, 64f), 29f, font);
        RectTransform startRect = (RectTransform)start;
        startRect.anchorMin = startRect.anchorMax = new Vector2(0.5f, 0.5f);

        // Also keep the Edit Mode preview uncluttered; gameplay stability is unchanged.
        if (manager.stabilityFill != null)
            manager.stabilityFill.transform.parent.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void StyleButton(Transform parent, string name, string colour, string text,
        Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font)
    {
        Transform target = parent.Find(name);
        if (target == null) throw new System.InvalidOperationException("Missing button: " + name);
        Button button = target.GetComponent<Button>();
        if (button == null) throw new System.InvalidOperationException("Missing Button component: " + name);
        RectTransform rect = (RectTransform)target;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        CartoonButtonSkin skin = target.GetComponent<CartoonButtonSkin>();
        if (skin == null) skin = target.gameObject.AddComponent<CartoonButtonSkin>();
        skin.Configure(Load(Palette + colour + "_normal.png"), Load(Palette + colour + "_hover.png"),
            Load(Palette + colour + "_pressed.png"), Load(Palette + colour + "_disabled.png"),
            Load(Palette + colour + "_focused.png"));

        TMP_Text label = target.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(target, false);
            label = go.GetComponent<TMP_Text>();
        }
        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        label.text = text;
        if (font != null) label.font = font;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = 22f;
        label.fontSizeMax = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color32(69, 38, 82, 255);
        label.raycastTarget = false;
    }

    private static Sprite Load(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new System.InvalidOperationException("Missing UI sprite: " + path);
        return sprite;
    }

    private static Transform Find(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = Find(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
