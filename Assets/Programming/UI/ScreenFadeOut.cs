using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFadeOut : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;
    public GameObject FadeImageObject;

    private void Start()
    {
        FadeImageObject.active = true;

        StartCoroutine(FadeFromBlack());
    }


    private IEnumerator FadeFromBlack()
    {
        if (fadeImage == null)
            yield break;

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            color.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            fadeImage.color = color;

            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;

        FadeImageObject.active = false;
    }
}
