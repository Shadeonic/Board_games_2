using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Music volume")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TMP_InputField musicValueInput;

    [Header("Effects volume")]
    [SerializeField] private Slider effectsSlider;
    [SerializeField] private TMP_InputField effectsValueInput;

    [Header("Music track")]
    [SerializeField] private TMP_Text trackNameText;
    [SerializeField] private Button previousTrackButton;
    [SerializeField] private Button nextTrackButton;
    [SerializeField] private TMP_Dropdown trackDropdown;

    [Header("Display")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Buttons")]
    [SerializeField] private Button restoreButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button backButton;

    [Header("Going back")]
    [Tooltip("Your SceneChangerScript. Back calls its ToMainMenu().")]
    [SerializeField] private SceneChangerScript sceneChanger;

    [Header("Popups")]
    [SerializeField] private ConfirmDialog dialog;
    [Tooltip("Your animated dice. Shown for a moment after saving.")]
    [SerializeField] private GameObject savingIndicator;
    [SerializeField] private float savingIndicatorTime = 1.5f;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private Coroutine indicatorRoutine;
    private bool started;

    private SettingsManager S => SettingsManager.Instance;

    private void Start()
    {
        if (S == null)
        {
            Debug.LogError("SettingsMenu: no SettingsManager in the scene. Add the SettingsManager prefab.");
            enabled = false;
            return;
        }

        if (savingIndicator != null) savingIndicator.SetActive(false);

        BuildTrackList();
        BuildResolutionList();
        BuildQualityList();

        musicSlider.minValue = 0f; musicSlider.maxValue = 1f;
        effectsSlider.minValue = 0f; effectsSlider.maxValue = 1f;

        musicSlider.onValueChanged.AddListener(v => { S.PreviewMusicVolume(v); UpdateValueTexts(); });
        effectsSlider.onValueChanged.AddListener(v => { S.PreviewEffectsVolume(v); UpdateValueTexts(); });

        SetupVolumeInput(musicValueInput, v => S.PreviewMusicVolume(v));
        SetupVolumeInput(effectsValueInput, v => S.PreviewEffectsVolume(v));

        previousTrackButton.onClick.AddListener(() => ChangeTrack(-1));
        nextTrackButton.onClick.AddListener(() => ChangeTrack(+1));
        if (trackDropdown != null)
            trackDropdown.onValueChanged.AddListener(i => { S.PreviewMusicTrack(i); RefreshUI(); });

        resolutionDropdown.onValueChanged.AddListener(i =>
        {
            if (i >= 0 && i < resolutions.Count) S.SetResolution(resolutions[i].x, resolutions[i].y);
        });
        qualityDropdown.onValueChanged.AddListener(i => S.SetQuality(i));

        restoreButton.onClick.AddListener(OnRestoreClicked);
        saveButton.onClick.AddListener(OnSaveClicked);
        backButton.onClick.AddListener(OnBackClicked);

        // Playlist went to the next song by itself -> update "Now is playing"
        SettingsManager.OnMusicTrackChanged += RefreshUI;

        started = true;
        RefreshUI();
    }

    private void OnEnable()
    {
        // When the panel is opened again, show the real values
        if (started) RefreshUI();
    }

    private void OnDestroy()
    {
        SettingsManager.OnMusicTrackChanged -= RefreshUI;

        // Leaving the scene some other way without saving -> don't keep half-changed values
        if (S != null && S.HasUnsavedChanges) S.DiscardChanges();
    }

    // ---------- Buttons ----------

    private void OnSaveClicked()
    {
        if (!S.HasUnsavedChanges)
        {
            if (dialog != null) dialog.Warn("Nothing has changed.\nSettings are already saved.");
            return;
        }
        SaveNow();
    }

    private void OnRestoreClicked()
    {
        if (dialog == null)
        {
            RestoreNow();
            return;
        }
        dialog.Ask("Restore default settings?\nYour current settings will be lost.",
                   "Yes", RestoreNow,
                   "No", null);
    }

    private void OnBackClicked()
    {
        if (!S.HasUnsavedChanges || dialog == null)
        {
            if (S.HasUnsavedChanges) S.DiscardChanges();
            Leave();
            return;
        }

        dialog.Ask("You have unsaved changes.\nSave them?",
                   "Save", () => { SaveNow(); StartCoroutine(LeaveAfterIndicator()); },
                   "NOOO", () => { S.DiscardChanges(); Leave(); },
                   showCancel: true);
    }

    private void SaveNow()
    {
        S.Save();
        ShowSavingIndicator();
        RefreshUI();
    }

    private void RestoreNow()
    {
        S.RestoreDefaults(); // also saves
        ShowSavingIndicator();
        RefreshUI();
    }

    private void Leave()
    {
        if (sceneChanger != null)
            sceneChanger.ToMainMenu();
        else
            Debug.LogError("SettingsMenu: drag your SceneChangerScript into Scene Changer.");
    }

    private IEnumerator LeaveAfterIndicator()
    {
        yield return new WaitForSecondsRealtime(savingIndicatorTime);
        Leave();
    }

    // ---------- Music track ----------

    private void ChangeTrack(int step)
    {
        int count = S.MusicTracks.Count;
        if (count == 0) return;

        int index = (S.Current.musicTrack + step + count) % count; // wraps around
        S.PreviewMusicTrack(index);
        RefreshUI();
    }

    private void BuildTrackList()
    {
        if (trackDropdown == null) return;

        trackDropdown.ClearOptions();
        var names = new List<string>();
        foreach (AudioClip clip in S.MusicTracks)
            names.Add(clip != null ? clip.name : "-");
        trackDropdown.AddOptions(names);
    }

    // ---------- Resolution / quality ----------

    private void BuildResolutionList()
    {
        resolutions.Clear();
        foreach (Resolution r in Screen.resolutions)
        {
            var size = new Vector2Int(r.width, r.height);
            if (!resolutions.Contains(size)) resolutions.Add(size); // skip same size with other Hz
        }

        var saved = new Vector2Int(S.Current.resolutionWidth, S.Current.resolutionHeight);
        if (saved.x > 0 && !resolutions.Contains(saved)) resolutions.Add(saved);
        if (resolutions.Count == 0) resolutions.Add(new Vector2Int(Screen.width, Screen.height));

        resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

        var options = new List<string>();
        foreach (Vector2Int r in resolutions) options.Add(r.x + " x " + r.y);

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    private void BuildQualityList()
    {
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(SettingsManager.QualityNames));
    }

    // ---------- Showing values ----------

    private void RefreshUI()
    {
        SettingsData d = S.Current;

        musicSlider.SetValueWithoutNotify(d.musicVolume);
        effectsSlider.SetValueWithoutNotify(d.effectsVolume);
        UpdateValueTexts();

        bool hasTracks = S.MusicTracks.Count > 0 && d.musicTrack < S.MusicTracks.Count;
        trackNameText.text = hasTracks && S.MusicTracks[d.musicTrack] != null
            ? S.MusicTracks[d.musicTrack].name
            : "-";
        previousTrackButton.interactable = hasTracks;
        nextTrackButton.interactable = hasTracks;
        if (trackDropdown != null && hasTracks) trackDropdown.SetValueWithoutNotify(d.musicTrack);

        int resIndex = resolutions.IndexOf(new Vector2Int(d.resolutionWidth, d.resolutionHeight));
        resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0, resIndex));
        resolutionDropdown.RefreshShownValue();

        qualityDropdown.SetValueWithoutNotify(d.quality);
        qualityDropdown.RefreshShownValue();
    }

    private void UpdateValueTexts()
    {
        musicValueInput.SetTextWithoutNotify(Mathf.RoundToInt(S.Current.musicVolume * 100f).ToString());
        effectsValueInput.SetTextWithoutNotify(Mathf.RoundToInt(S.Current.effectsVolume * 100f).ToString());
    }

    private void SetupVolumeInput(TMP_InputField input, System.Action<float> apply)
    {
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterLimit = 3;

        input.onEndEdit.AddListener(text =>
        {
            if (int.TryParse(text, out int percent))
            {
                percent = Mathf.Clamp(percent, 0, 100);
                apply(percent / 100f);
            }
            RefreshUI(); // moves the slider and fixes the text (e.g. 150 -> 100)
        });
    }

    private void ShowSavingIndicator()
    {
        if (savingIndicator == null) return;
        if (indicatorRoutine != null) StopCoroutine(indicatorRoutine);
        indicatorRoutine = StartCoroutine(IndicatorRoutine());
    }

    private IEnumerator IndicatorRoutine()
    {
        savingIndicator.SetActive(true);
        savingIndicator.transform.SetAsLastSibling();
        yield return new WaitForSecondsRealtime(savingIndicatorTime);
        savingIndicator.SetActive(false);
        indicatorRoutine = null;
    }
}