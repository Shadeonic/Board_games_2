using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple popup: a message with Yes / No / (optional) Cancel buttons,
/// or just one OK button for warnings.
/// Put it on the popup panel and keep the panel DISABLED in the scene.
/// </summary>
public class ConfirmDialog : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;

    [SerializeField] private Button yesButton;
    [SerializeField] private TMP_Text yesLabel;

    [SerializeField] private Button noButton;
    [SerializeField] private TMP_Text noLabel;

    [Tooltip("Optional. Closes the popup and does nothing.")]
    [SerializeField] private Button cancelButton;

    private Action onYes;
    private Action onNo;
    private bool initialized;

    /// <summary>Question with two answers (and optionally Cancel).</summary>
    public void Ask(string message, string yesText, Action yesAction,
                    string noText, Action noAction, bool showCancel = false)
    {
        Init();
        messageText.text = message;

        yesLabel.text = yesText;
        onYes = yesAction;

        noButton.gameObject.SetActive(true);
        noLabel.text = noText;
        onNo = noAction;

        if (cancelButton != null) cancelButton.gameObject.SetActive(showCancel);

        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // draw on top of the other UI
    }

    /// <summary>Warning with only an OK button.</summary>
    public void Warn(string message, string okText = "OK")
    {
        Init();
        messageText.text = message;

        yesLabel.text = okText;
        onYes = null;

        noButton.gameObject.SetActive(false);
        onNo = null;

        if (cancelButton != null) cancelButton.gameObject.SetActive(false);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void Init()
    {
        if (initialized) return;
        initialized = true;

        yesButton.onClick.AddListener(() => Close(onYes));
        noButton.onClick.AddListener(() => Close(onNo));
        if (cancelButton != null) cancelButton.onClick.AddListener(() => Close(null));
    }

    private void Close(Action action)
    {
        gameObject.SetActive(false);
        onYes = null;
        onNo = null;
        action?.Invoke();
    }
}
