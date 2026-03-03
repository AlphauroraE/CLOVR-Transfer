using UnityEngine;
using UnityEngine.UI;

namespace XRT_OVR_Grabber
{
    /// <summary>
    /// Popup shown while a recording is in progress when the headset remains still.
    /// Provides a Stop Recording button and a Dismiss button (suppresses until recording state changes).
    /// </summary>
    public class RecordingStopPopup : MonoBehaviour
    {
        [Tooltip("Enable debug logs for this popup.")]
        public bool enableDebugLogs = false;
        [Tooltip("Root GameObject for the popup (enable/disable to show/hide).")]
        public GameObject popupPanel;

        [Tooltip("Button to stop the active recording.")]
        public Button stopRecordingButton;

        [Tooltip("Optional close/dismiss Button inside the popup.")]
        public Button closeButton;

        [Tooltip("Optional transform used to position the popup (assign in inspector).")]
        public Transform positionTarget;
        [Tooltip("When true, the popup will auto-position itself near `positionTarget` when shown. Disable to keep inspector-set RectTransform position.")]
        public bool autoPosition = false;

        [Tooltip("Offset applied when auto-positioning (world units for world-space canvas).")]
        public Vector3 positionOffset = new Vector3(0f, 0.15f, 0f);

        [Tooltip("If true, rotate the popup to face the main camera when auto-positioning.")]
        public bool faceCamera = true;

        [Tooltip("Optional reference to the main UI controller. If set, the popup will call its `UI___StopRecording()` method to match UI behavior.")]
        public InstructorsInterface instructorsInterface;

        [Tooltip("When dismissed, keep the popup hidden until explicitly reset by the watcher.")]
        public bool suppressed = false;

        LoggingManagerAPI loggerManager;

        void Start()
        {
            loggerManager = FindObjectOfType<LoggingManagerAPI>();
            if (instructorsInterface == null)
                instructorsInterface = FindObjectOfType<InstructorsInterface>();
            if (stopRecordingButton != null)
                stopRecordingButton.onClick.AddListener(OnStopRecordingClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnDismissClicked);
            if (popupPanel != null)
                popupPanel.SetActive(false);
            if (enableDebugLogs) Debug.Log($"RecordingStopPopup: Start() found loggerManager={(loggerManager!=null)} instructorsInterface={(instructorsInterface!=null)}");
        }

        public void Show(Transform target)
        {
            if (suppressed) return;
            if (popupPanel == null) return;
            if (enableDebugLogs) Debug.Log($"RecordingStopPopup: Show() target={(target!=null)} suppressed={suppressed}");

            // Activate first so RectTransform is available and layout can update
            popupPanel.SetActive(true);

            if (!autoPosition)
                return;

            if (target != null)
            {
                positionTarget = target;

                var rect = popupPanel.GetComponent<RectTransform>();
                Canvas parentCanvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;

                if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
                {
                    // Screen-space canvas: convert world point to canvas local position
                    if (Camera.main != null)
                    {
                        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, target.position + positionOffset);
                        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
                        Vector2 localPoint;
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, out localPoint))
                        {
                            rect.anchoredPosition = localPoint;
                        }
                    }
                }
                else
                {
                    // World-space canvas or no canvas: position in world space
                    popupPanel.transform.position = target.position + positionOffset;
                    if (faceCamera && Camera.main != null)
                    {
                        popupPanel.transform.LookAt(Camera.main.transform);
                        popupPanel.transform.Rotate(0f, 180f, 0f);
                    }
                }
            }
        }

        public void Hide()
        {
            if (popupPanel == null) return;
            if (enableDebugLogs) Debug.Log("RecordingStopPopup: Hide()");
            popupPanel.SetActive(false);
        }

        void OnDismissClicked()
        {
            suppressed = true;
            if (enableDebugLogs) Debug.Log("RecordingStopPopup: Dismiss clicked -> suppressed");
            Hide();
        }

        /// <summary> Clear dismissal suppression so the popup can show again. </summary>
        public void ClearSuppression()
        {
            suppressed = false;
        }

        void OnStopRecordingClicked()
        {
            if (enableDebugLogs) Debug.Log("RecordingStopPopup: StopRecording clicked");
            if (instructorsInterface != null)
            {
                instructorsInterface.UI___StopRecording();
            }
            else if (loggerManager != null && loggerManager.isRecording)
            {
                loggerManager.StopRecording();
            }
            Hide();
        }
    }
}
