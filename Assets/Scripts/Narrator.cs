using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Narrator : MonoBehaviour
{
    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayNarration(string roomName)
    {
        AudioClip clipToPlay = Resources.Load<AudioClip>($"Sounds/{roomName}");
        if (clipToPlay)
        {
            if (fadeCoroutine != null) // Stop ongoing fade out if any
            {
                StopCoroutine(fadeCoroutine);
            }

            audioSource.clip = clipToPlay;
            audioSource.volume = 1; // Reset volume
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"No narration found for room {roomName}!");
        }
    }

    public void MuteNarration()
    {
        audioSource.Pause();
    }

    public void UnmuteNarration()
    {
        audioSource.UnPause();
    }

    public void FadeOut(float fadeDuration)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeOutRoutine(fadeDuration));
    }

    private IEnumerator FadeOutRoutine(float fadeDuration)
    {
        float startVolume = audioSource.volume;
        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume; // Reset volume
    }

    public void StopNarration()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        audioSource.Stop();
    }
}