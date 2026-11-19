using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SettingsData
{
    public float musicVolume = 0.75f;
    public float effectsVolume = 0.75f;
    public int musicTrack = 0;        // song the playlist continues from
    public int resolutionWidth = 0;   // 0 = not set yet, use the screen's own resolution
    public int resolutionHeight = 0;
    public int quality = 2;           // 0 = Low, 1 = Medium, 2 = High

    public SettingsData Clone() => (SettingsData)MemberwiseClone();

    // musicTrack is not compared: switching songs works right away and
    // doesn't count as an "unsaved change"
    public bool SameAs(SettingsData other) =>
        Mathf.Approximately(musicVolume, other.musicVolume) &&
        Mathf.Approximately(effectsVolume, other.effectsVolume) &&
        resolutionWidth == other.resolutionWidth &&
        resolutionHeight == other.resolutionHeight &&
        quality == other.quality;
}

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    /// <summary>Fires when the effects volume changes (used by EffectsVolume).</summary>
    public static event Action OnEffectsVolumeChanged;

    /// <summary>Fires when a new song starts (by arrows or by the playlist itself).</summary>
    public static event Action OnMusicTrackChanged;

    /// <summary>Names shown in the quality dropdown, index = quality value.</summary>
    public static readonly string[] QualityNames = { "Low", "Medium", "High" };

    private const string SettingsFileName = "settings.json";

    [Header("This scene's audio")]
    [SerializeField] private AudioPlaylist playlist;
    [Tooltip("Can be left empty if the AudioPlaylist has its Audio Source set")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectsSource;

    [Header("Default values (used by Restore)")]
    [Range(0f, 1f)][SerializeField] private float defaultMusicVolume = 0.75f;
    [Range(0f, 1f)][SerializeField] private float defaultEffectsVolume = 0.75f;
    [SerializeField] private int defaultMusicTrack = 0;
    [Tooltip("0 = Low, 1 = Medium, 2 = High")]
    [Range(0, 2)][SerializeField] private int defaultQuality = 2;

    // Static = shared by all scenes, survives scene loading
    private static SettingsData saved;
    private static SettingsData current;

    public SettingsData Saved => saved;

    public SettingsData Current => current;

    private static readonly List<AudioClip> noTracks = new List<AudioClip>();
    public IReadOnlyList<AudioClip> MusicTracks =>
        playlist != null && playlist.playlist != null ? playlist.playlist : noTracks;

    public float EffectsVolume => current != null ? current.effectsVolume : 1f;
    public bool HasUnsavedChanges => !current.SameAs(saved);

    private static string FilePath => Path.Combine(Application.persistentDataPath, SettingsFileName);

    private void Awake()
    {
        Instance = this;

        if (musicSource == null && playlist != null) musicSource = playlist.audioSource;

        // Only the very first time in the game: load from the file
        if (saved == null)
        {
            saved = Load();
            current = saved.Clone();
            ApplyDisplay();
        }

        if (playlist != null) playlist.OnTrackChanged += HandlePlaylistTrackChanged;

        ApplyAudio();
        OnEffectsVolumeChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (playlist != null) playlist.OnTrackChanged -= HandlePlaylistTrackChanged;
        if (Instance == this) Instance = null;

        // Leaving the scene without saving -> forget the unsaved changes
        if (current != null && !current.SameAs(saved))
        {
            int track = current.musicTrack;
            current = saved.Clone();
            current.musicTrack = track;
        }
    }

    // ---------- Changing values (preview, not saved yet) ----------

    public void PreviewMusicVolume(float value)
    {
        current.musicVolume = Mathf.Clamp01(value);
        ApplyAudio();
    }

    public void PreviewEffectsVolume(float value)
    {
        current.effectsVolume = Mathf.Clamp01(value);
        ApplyAudio();
        OnEffectsVolumeChanged?.Invoke();
    }

    public void PreviewMusicTrack(int index)
    {
        SetTrack(ClampTrack(index));
        if (playlist != null) playlist.PlayIndex(current.musicTrack);
    }

    public void SetResolution(int width, int height)
    {
        current.resolutionWidth = width;
        current.resolutionHeight = height;
    }

    public void SetQuality(int quality)
    {
        current.quality = Mathf.Clamp(quality, 0, QualityNames.Length - 1);
    }

    // ---------- Save / discard / restore ----------

    public void Save()
    {
        saved = current.Clone();
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(saved, true));
            Debug.Log("Settings saved to " + FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Could not save settings: " + e.Message);
        }
        ApplyDisplay();
    }

    public void DiscardChanges()
    {
        int track = current.musicTrack;
        current = saved.Clone();
        current.musicTrack = track;
        ApplyAudio();
        OnEffectsVolumeChanged?.Invoke();
    }

    public void RestoreDefaults()
    {
        current = CreateDefaults();
        ApplyAudio();
        OnEffectsVolumeChanged?.Invoke();
        Save();
    }

    // ---------- Internals ----------

    private void HandlePlaylistTrackChanged(int index)
    {
        SetTrack(index);
    }

    private void SetTrack(int index)
    {
        current.musicTrack = index;
        saved.musicTrack = index; // the playing song is remembered without pressing Save
        OnMusicTrackChanged?.Invoke();
    }

    private SettingsData CreateDefaults()
    {
        Resolution native = Screen.currentResolution;
        return new SettingsData
        {
            musicVolume = defaultMusicVolume,
            effectsVolume = defaultEffectsVolume,
            musicTrack = ClampTrack(defaultMusicTrack),
            resolutionWidth = native.width,
            resolutionHeight = native.height,
            quality = defaultQuality
        };
    }

    private SettingsData Load()
    {
        SettingsData data = null;
        if (File.Exists(FilePath))
        {
            try
            {
                data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Settings file is broken, using defaults: " + e.Message);
            }
        }

        if (data == null) return CreateDefaults();

        // Fill anything missing
        if (data.resolutionWidth <= 0 || data.resolutionHeight <= 0)
        {
            data.resolutionWidth = Screen.currentResolution.width;
            data.resolutionHeight = Screen.currentResolution.height;
        }
        data.quality = Mathf.Clamp(data.quality, 0, QualityNames.Length - 1);
        data.musicTrack = ClampTrack(data.musicTrack);
        return data;
    }

    private void ApplyAudio()
    {
        if (effectsSource != null) effectsSource.volume = current.effectsVolume;
        if (musicSource != null) musicSource.volume = current.musicVolume;

        // Make the playlist play the chosen song (before Start it just sets where it begins)
        if (playlist != null && MusicTracks.Count > 0 && playlist.CurrentIndex != current.musicTrack)
            playlist.PlayIndex(ClampTrack(current.musicTrack));
    }

    private static void ApplyDisplay()
    {
        ApplyQuality(current.quality);

        // Resolution can't be changed on phones/web, and doesn't work in the Editor (test in a build)
        if (Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer) return;

        if (current.resolutionWidth > 0 && current.resolutionHeight > 0 &&
            (Screen.width != current.resolutionWidth || Screen.height != current.resolutionHeight))
        {
            Screen.SetResolution(current.resolutionWidth, current.resolutionHeight, Screen.fullScreenMode);
        }
    }

    private static void ApplyQuality(int quality)
    {
        // 1) If the project has quality levels, pick a matching one (lowest / middle / highest)
        int levels = QualitySettings.names.Length;
        if (levels > 0)
        {
            int level = quality == 0 ? 0 : quality == 1 ? levels / 2 : levels - 1;
            QualitySettings.SetQualityLevel(level, true);
        }

        // 2) Then set the important values ourselves, so the difference is real
        switch (quality)
        {
            case 0: // Low
                SetTextureLimit(2);                 // textures at 1/4 resolution
                QualitySettings.antiAliasing = 0;   // no smoothing of edges
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 30;   // 30 FPS
                break;

            case 1: // Medium
                SetTextureLimit(1);                 // textures at 1/2 resolution
                QualitySettings.antiAliasing = 2;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;   // 60 FPS
                break;

            default: // High
                SetTextureLimit(0);                 // full resolution textures
                QualitySettings.antiAliasing = 4;
                QualitySettings.vSyncCount = 1;     // synced to the monitor
                Application.targetFrameRate = -1;
                break;
        }

        Debug.Log("Quality set to " + QualityNames[Mathf.Clamp(quality, 0, QualityNames.Length - 1)]);
    }

    private static void SetTextureLimit(int limit)
    {
#if UNITY_2022_2_OR_NEWER
        QualitySettings.globalTextureMipmapLimit = limit;
#else
        QualitySettings.masterTextureLimit = limit;
#endif
    }

    private int ClampTrack(int index)
    {
        int count = MusicTracks.Count;
        if (count == 0) return Mathf.Max(0, index);
        return Mathf.Clamp(index, 0, count - 1);
    }
}