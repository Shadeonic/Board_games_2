using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlaylist : MonoBehaviour
{
    [Header("Audio iestatījumi")]
    public AudioSource audioSource;
    public List<AudioClip> playlist;

    private int currentIndex = 0;

    void Start()
    {
        if (playlist != null && playlist.Count > 0 && audioSource != null)
        {
            audioSource.loop = false;
            StartCoroutine(PlayPlaylistRoutine());
        }
        else
        {
            Debug.LogError("Add audiosource and at least 1 sound to the list!");
        }
    }

    private IEnumerator PlayPlaylistRoutine()
    {
        while (true)
        {
            audioSource.clip = playlist[currentIndex];
            audioSource.Play();
            Debug.Log($"Now is playing [{currentIndex}]: {audioSource.clip.name}");

            while (audioSource.isPlaying)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            currentIndex = (currentIndex + 1) % playlist.Count;
        }
    }
}
