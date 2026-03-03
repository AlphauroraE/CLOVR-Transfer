using System;
using UnityEngine;
using UnityEngine.UI;

namespace XRT_OVR_Grabber
{
    /// <summary>
    /// Simple popup controller that shows/hides a reminder to start recording.
    /// Inspector: assign a world-space panel GameObject to `popupPanel`, and
    /// optionally a `startRecordingButton` and `closeButton`.
    /// </summary>
    public class RecordingReminderPopup : MonoBehaviour
    {
        [Tooltip("Root GameObject for the popup (enable/disable to show/hide).")]
        public GameObject popupPanel;

        [Tooltip("Optional start-recording Button inside the popup.")]
        public Button startRecordingButton;

        [Tooltip("Optional close/dismiss Button inside the popup.")]
        public Button closeButton;

        [Tooltip("Optional transform of the Record button to point at for context.")]
        public Transform recordButtonTarget;

        [Tooltip("Optional reference to the main UI controller. If set, the popup will call its `UI___StartRecording()` method to match UI behavior.")]
        public InstructorsInterface instructorsInterface;

        [Tooltip("When dismissed, keep the popup hidden until explicitly reset by the watcher.")]
        public bool suppressed = false;

        LoggingManagerAPI loggerManager;

        void Start()
        {
            loggerManager = FindObjectOfType<LoggingManagerAPI>();
            if (instructorsInterface == null)
                instructorsInterface = FindObjectOfType<InstructorsInterface>();
            if (startRecordingButton != null)
                startRecordingButton.onClick.AddListener(OnStartRecordingClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnDismissClicked);
            if (popupPanel != null)
                popupPanel.SetActive(false);
        }

        void Update()
        {
            // Temporary test hotkey: press R to simulate pressing Start Recording on the popup.
            if (Input.GetKeyDown(KeyCode.R) && popupPanel != null && popupPanel.activeSelf)
            {
                OnStartRecordingClicked();
            }
        }

        public void Show(Transform target)
        {
            if (suppressed) return;

            if (popupPanel == null) return;
            popupPanel.SetActive(true);
            if (target != null)
            {
                recordButtonTarget = target;
                // Try to position popup near the target and face the user.
                popupPanel.transform.position = target.position + Vector3.up * 0.15f;
                if (Camera.main != null)
                {
                    popupPanel.transform.LookAt(Camera.main.transform);
                    popupPanel.transform.Rotate(0f, 180f, 0f);
                }
            }
        }

        public void Hide()
        {
            if (popupPanel == null) return;
            popupPanel.SetActive(false);
        }

        void OnDismissClicked()
        {
            // keep hidden until watcher resets suppression
            suppressed = true;
            Hide();
        }

        /// <summary> Clear dismissal suppression so the popup can show again. </summary>
        public void ClearSuppression()
        {
            suppressed = false;
        }

        void OnStartRecordingClicked()
        {
            // Prefer invoking the main UI start-recording method so any UI-side logic runs.
            if (instructorsInterface != null)
            {
                instructorsInterface.UI___StartRecording();
            }
            else if (loggerManager != null && !loggerManager.isRecording)
            {
                loggerManager.StartRecording();
            }
            Hide();
        }
    }
}
