using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlaylist : MonoBehaviour
{
    [Header("Audio iestatījumi")]
    public AudioSource audioSource;
    public List<AudioClip> playlist;

    public event Action<int> OnTrackChanged;

    public int CurrentIndex => currentIndex;

    private int currentIndex = 0;
    private Coroutine routine;
    private bool started;

    void Start()
    {
        if (playlist != null && playlist.Count > 0 && audioSource != null)
        {
            audioSource.loop = false;
            started = true;
            routine = StartCoroutine(PlayPlaylistRoutine());
        }
        else
        {
            Debug.LogError("Add audiosource and at least 1 sound to the list!");
        }
    }

    public void PlayIndex(int index)
    {
        if (playlist == null || playlist.Count == 0) return;

        currentIndex = Mathf.Clamp(index, 0, playlist.Count - 1);
        if (!started) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayPlaylistRoutine());
    }

    private IEnumerator PlayPlaylistRoutine()
    {
        while (true)
        {
            audioSource.clip = playlist[currentIndex];
            audioSource.Play();
            Debug.Log($"Now is playing [{currentIndex}]: {audioSource.clip.name}");
            OnTrackChanged?.Invoke(currentIndex);

            while (audioSource.isPlaying)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.5f);
            currentIndex = (currentIndex + 1) % playlist.Count;
        }
    }
}