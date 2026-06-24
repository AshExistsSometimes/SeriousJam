using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;


public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private UnityEngine.UI.Image background;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private MainMenu menu;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 10f;

    private bool hovered;

    private void Awake()
    {
        SetAlpha(0f);

        if (buttonText != null)
            buttonText.color = Color.white;
    }

    private void Update()
    {
        if (menu != null && menu.IsLocked)
        {
            if (hovered)
            {
                SetAlpha(1f);

                if (buttonText != null)
                    buttonText.color = Color.black;
            }

            return;
        }

        float targetAlpha = hovered ? 1f : 0f;

        Color color = background.color;
        color.a = Mathf.MoveTowards(
            color.a,
            targetAlpha,
            fadeSpeed * Time.deltaTime);

        background.color = color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (menu != null && menu.IsLocked)
            return;

        hovered = true;

        if (buttonText != null)
            buttonText.color = Color.black;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (menu != null && menu.IsLocked)
            return;

        hovered = false;

        if (buttonText != null)
            buttonText.color = Color.white;
    }

    private void SetAlpha(float alpha)
    {
        if (background == null)
            return;

        Color color = background.color;
        color.a = alpha;
        background.color = color;
    }
}