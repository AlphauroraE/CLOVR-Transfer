using UnityEngine;

namespace XRT_OVR_Grabber
{
    /// <summary>
    /// Shows the recording reminder based on the project's paused state.
    /// Motion-based detection has been removed; this class only supports
    /// pause-state based reminders when `usePauseStateInsteadOfMotion` is true.
    /// </summary>
    public class PauseStateWatcher : MonoBehaviour
    {
        [Tooltip("Enable debug logs to the Console.")]
        public bool enableDebugLogs = false;
        
        [Tooltip("Seconds of sustained motion before showing the reminder.")]
        public float secondsBeforeAlert = 5.0f;

        // [Tooltip("When true, use the project's paused state instead of headset motion to trigger the reminder.")]
        // public bool usePauseStateInsteadOfMotion = true;
        [Tooltip("Reference to the in-scene Recording Reminder popup (assign in inspector).")]
        public RecordingReminderPopup reminderPopup;

        [Tooltip("Optional transform of the Record button to point at (assign in inspector).")]
        public Transform recordButtonTransform;

        LoggingManagerAPI loggerManager;
        bool lastPauseState = false;
        float notPausedAccum = 0f;

        void Start()
        {
            loggerManager = FindObjectOfType<LoggingManagerAPI>();
            if (enableDebugLogs)
                Debug.Log("PauseStateWatcher: Start() - loggerManager found: " + (loggerManager != null));
            if (loggerManager != null)
                lastPauseState = loggerManager.pauseRecording;
        }

        void Update()
        {
            if (loggerManager != null && (loggerManager.isRecording || loggerManager.pauseRecording))
            {
                notPausedAccum = 0f;
                if (reminderPopup != null)
                    reminderPopup.Hide();
                return;
            }

            // Reset suppression when pause state toggles (user paused/unpaused the app)
            if (loggerManager != null && reminderPopup != null)
            {
                bool currentPause = loggerManager.pauseRecording;
                if (currentPause != lastPauseState)
                {
                    if (enableDebugLogs) Debug.Log("PauseStateWatcher: pause state changed, clearing popup suppression");
                    reminderPopup.ClearSuppression();
                }
                lastPauseState = currentPause;
            }

            // Use the project's paused state to trigger the reminder
            // if (usePauseStateInsteadOfMotion && loggerManager != null)
            if (loggerManager != null)
            {
                bool paused = loggerManager.pauseRecording;
                if (enableDebugLogs) Debug.Log($"PauseStateWatcher: pause state={paused}, notPausedAccum={notPausedAccum:F2}");

                if (!paused && !loggerManager.isRecording)
                {
                    notPausedAccum += Time.deltaTime;
                }
                else
                {
                    notPausedAccum = 0f;
                }

                if (notPausedAccum >= secondsBeforeAlert)
                {
                    if (reminderPopup != null && !loggerManager.isRecording)
                    {
                        if (enableDebugLogs) Debug.Log("PauseStateWatcher: notPaused threshold reached -> showing reminder");
                        reminderPopup.Show(recordButtonTransform);
                    }
                }

                return;
            }
        }
    }
}