using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CartoonButtonsSetup
{
    private const string Palette = "Assets/Sprites/CartoonButtonsPalette/pill_";
    private const string Window = "Assets/Sprites/Cartoon-Buttons-FREE-Sample/Cartoon-Buttons-FREE-Sample/png/2x/candy/panels/panel_window_320x256.png";
    private const string Hud = "Assets/Prefabs/TopHudPanel.prefab";

    [MenuItem("Tools/Simon/Apply Cartoon Buttons")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || !scene.IsValid() || scene.name != "SimonScene" || scene.isDirty)
            throw new System.InvalidOperationException("Open a saved SimonScene in Edit mode before applying button skins.");

        ImportSprites();
        GameObject prefab = PrefabUtility.LoadPrefabContents(Hud);
        try
        {
            Transform root = prefab.transform;
            Style(root.Find("ScorePanel/ScoreImagen"), "yellow", false);
            Image scoreBacking = root.Find("ScorePanel").GetComponent<Image>();
            scoreBacking.color = Color.clear;
            scoreBacking.raycastTarget = false;
            Style(root.Find("LevelPanel/LevelImage"), "blue", false);
            Style(root.Find("MenuButton"), "blue", true);
            PrefabUtility.SaveAsPrefabAsset(prefab, Hud);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        SimonGameManager manager = Object.FindFirstObjectByType<SimonGameManager>();
        if (manager == null)
            throw new System.InvalidOperationException("SimonGameManager is missing.");
        SerializedObject settings = new SerializedObject(manager);
        Set(settings, "memoryModeCardSprite", Pill("green", "normal"));
        Set(settings, "memoryModeHoverSprite", Pill("green", "hover"));
        Set(settings, "memoryModePressedSprite", Pill("green", "pressed"));
        Set(settings, "memoryModeDisabledSprite", Pill("green", "disabled"));
        Set(settings, "rhythmModeCardSprite", Pill("blue", "normal"));
        Set(settings, "rhythmModeHoverSprite", Pill("blue", "hover"));
        Set(settings, "rhythmModePressedSprite", Pill("blue", "pressed"));
        Set(settings, "rhythmModeDisabledSprite", Pill("blue", "disabled"));
        settings.ApplyModifiedPropertiesWithoutUndo();

        Button startButton = settings.FindProperty("startButton").objectReferenceValue as Button;
        Canvas sceneCanvas = startButton != null ? startButton.GetComponentInParent<Canvas>() : null;
        if (sceneCanvas == null)
            throw new System.InvalidOperationException("Simon StartButton canvas is missing.");
        Transform canvas = sceneCanvas.transform;
        Transform box = Find(canvas, "PauseBox");
        Transform start = Find(canvas, "StartButton");
        if (box == null || start == null)
            throw new System.InvalidOperationException("Simon pause box or StartButton is missing.");

        Image boxImage = box.GetComponent<Image>();
        boxImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Window);
        boxImage.type = Image.Type.Sliced;
        boxImage.preserveAspect = false;
        boxImage.color = Color.white;
        TMP_Text title = box.Find("PauseTitle").GetComponent<TMP_Text>();
        title.color = new Color32(69, 38, 82, 255);
        Style(box.Find("ResumeButton"), "green", true);
        Style(box.Find("RestartButton"), "yellow", true);
        Style(box.Find("MenuButton"), "red", true);
        Style(box.Find("CloseButton"), "red", true);
        Style(start, "orange", true);

        // The former StartButton is a baked image: keep its Button action, but use live text.
        TMP_Text label = start.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            GameObject labelObject = new GameObject("PlayLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(start, false);
            label = labelObject.GetComponent<TMP_Text>();
        }
        label.font = title.font;
        label.text = "JUGAR";
        label.fontStyle = FontStyles.Bold;
        label.fontSize = 29f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color32(69, 38, 82, 255);
        label.raycastTarget = false;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Cartoon Buttons applied to shared HUD and Simon (mode, pause, play).");
    }

    private static void ImportSprites()
    {
        string[] files = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites/CartoonButtonsPalette" });
        if (files.Length != 25)
            throw new System.InvalidOperationException("Generate all 25 palette PNGs first: python Tools/recolor_cartoon_buttons.py");
        foreach (string guid in files)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(72f, 0f, 72f, 0f); // 2x source: horizontal 3-slice
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        TextureImporter panel = (TextureImporter)AssetImporter.GetAtPath(Window);
        panel.textureType = TextureImporterType.Sprite;
        panel.spriteImportMode = SpriteImportMode.Single;
        panel.spriteBorder = new Vector4(64f, 64f, 64f, 128f);
        panel.SaveAndReimport();
    }

    private static Sprite Pill(string colour, string state)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Palette + colour + "_" + state + ".png");
        if (sprite == null)
            throw new System.InvalidOperationException("Missing Cartoon button sprite: " + colour + "/" + state);
        return sprite;
    }

    private static void Set(SerializedObject settings, string property, Sprite sprite)
    {
        settings.FindProperty(property).objectReferenceValue = sprite;
    }

    private static void Style(Transform transform, string colour, bool interactive)
    {
        if (transform == null)
            throw new System.InvalidOperationException("Missing UI object for Cartoon button: " + colour);
        Image image = transform.GetComponent<Image>();
        image.sprite = Pill(colour, "normal");
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = Color.white;
        if (!interactive)
        {
            image.raycastTarget = false;
            return;
        }
        CartoonButtonSkin skin = transform.GetComponent<CartoonButtonSkin>();
        if (skin == null)
            skin = transform.gameObject.AddComponent<CartoonButtonSkin>();
        skin.Configure(Pill(colour, "normal"), Pill(colour, "hover"),
            Pill(colour, "pressed"), Pill(colour, "disabled"), Pill(colour, "focused"));
    }

    private static Transform Find(Transform root, string name)
    {
        if (root.name == name)
            return root;
        foreach (Transform child in root)
        {
            Transform result = Find(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
