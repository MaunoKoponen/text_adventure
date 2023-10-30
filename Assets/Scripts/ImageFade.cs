using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageFade : MonoBehaviour
{
    public Image targetImage;
    public float fadeDuration = 0.25f;

    public void SetImageWithFade(Sprite newSprite)
    {
        StartCoroutine(FadeOut(() =>
        {
            targetImage.sprite = newSprite;
            StartCoroutine(FadeIn());
        }));
    }

    IEnumerator FadeOut(System.Action onCompleted = null)
    {
        float startAlpha = targetImage.color.a;
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / fadeDuration)
        {
            Color newColor = new Color(targetImage.color.r, targetImage.color.g, targetImage.color.b, Mathf.Lerp(startAlpha, 0f, t));
            targetImage.color = newColor;
            yield return null;
        }

        targetImage.color = new Color(targetImage.color.r, targetImage.color.g, targetImage.color.b, 0f);
        onCompleted?.Invoke();
    }

    IEnumerator FadeIn()
    {
        float startAlpha = targetImage.color.a;
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / fadeDuration)
        {
            Color newColor = new Color(targetImage.color.r, targetImage.color.g, targetImage.color.b, Mathf.Lerp(startAlpha, 1f, t));
            targetImage.color = newColor;
            yield return null;
        }

        targetImage.color = new Color(targetImage.color.r, targetImage.color.g, targetImage.color.b, 1f);
    }
}