using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
public class SimonZoneUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Image mainImage;
    [SerializeField] private Button zoneButton;
    [SerializeField] private TMP_Text labelText;
    private int index;
    private Action<int> onPressed;

    private void Awake()
    {
        AutoResolveReferences();
    }

    public void Setup(int newIndex, string label, Action<int> callback)
    {
        AutoResolveReferences();

        index = newIndex;
        onPressed = callback;

        if (mainImage != null)
        {
            mainImage.color = Color.clear;
            mainImage.raycastTarget = true;
        }

        if (labelText != null)
        {
            labelText.text = label;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
        }

        if (zoneButton != null)
        {
            zoneButton.transition = Selectable.Transition.None;
            zoneButton.onClick.RemoveAllListeners();
            zoneButton.onClick.AddListener(HandlePress);
        }

        HideUnusedChildImages();
    }

    public void SetInteractable(bool value)
    {
        if (zoneButton != null)
            zoneButton.interactable = value;
    }

    public void SetOff()
    {
        if (mainImage != null)
            mainImage.color = Color.clear;

        if (labelText != null)
            labelText.color = Color.white;
    }

    private void HandlePress()
    {
        onPressed?.Invoke(index);
    }

    private void AutoResolveReferences()
    {
        if (mainImage == null)
            mainImage = GetComponent<Image>();

        if (zoneButton == null)
            zoneButton = GetComponent<Button>();

        if (labelText == null)
        {
            Transform labelTransform = transform.Find("Label");
            if (labelTransform != null)
                labelText = labelTransform.GetComponent<TMP_Text>();
        }

        if (labelText == null)
            labelText = GetComponentInChildren<TMP_Text>(true);
    }

    private void HideUnusedChildImages()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i] == mainImage)
                continue;

            images[i].enabled = false;
            images[i].raycastTarget = false;
        }
    }
}
