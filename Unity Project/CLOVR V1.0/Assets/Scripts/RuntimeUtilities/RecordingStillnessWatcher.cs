using UnityEngine;

namespace XRT_OVR_Grabber
{
    /// <summary>
    /// Watches headset motion while recording; when the headset remains sufficiently still
    /// for `secondsBeforeAlertStop`, shows the `RecordingStopPopup` prompting the user
    /// to stop recording or dismiss the prompt.
    /// </summary>
    public class RecordingStillnessWatcher : MonoBehaviour
    {
        [Tooltip("Enable debug logs to the Console.")]
        public bool enableDebugLogs = false;

        [Tooltip("Seconds of sustained stillness before showing the stop-recording popup.")]
        public float secondsBeforeAlertStop = 2.0f;

        [Tooltip("Position/transform to place the popup near (optional). If null, the watcher will use `headTransform` or `Camera.main`.")]
        public Transform popupPositionTarget;

        [Tooltip("Optional transform representing the player's head or center-eye. Assign your VR head/camera here if Camera.main is not available.")]
        public Transform headTransform;

        [Tooltip("Movement threshold (position delta magnitude). Movement larger than this resets the still timer.")]
        public float movementThreshold = 0.005f;

        [Tooltip("Rotation threshold (degrees). Rotation larger than this resets the still timer.")]
        public float rotationThresholdDeg = 1.0f;

        [Tooltip("Reference to the in-scene Recording Stop popup (assign in inspector).")]
        public RecordingStopPopup stopPopup;

        [Tooltip("Optional reference to the LoggingManagerAPI. If null, the watcher will try to find one at runtime.")]
        public LoggingManagerAPI loggerManager;

        Vector3 lastHeadPos;
        Quaternion lastHeadRot;
        float stillAccum = 0f;

        // Throttle expensive searches to avoid per-frame work
        float searchTimer = 0f;
        const float searchInterval = 1.0f;

        void Start()
        {
            if (loggerManager == null)
                loggerManager = FindObjectOfType<LoggingManagerAPI>();
            if (stopPopup == null)
                stopPopup = FindObjectOfType<RecordingStopPopup>();

            Transform effectiveHead = headTransform != null ? headTransform : (Camera.main != null ? Camera.main.transform : null);
            if (effectiveHead != null)
            {
                lastHeadPos = effectiveHead.position;
                lastHeadRot = effectiveHead.rotation;
            }
            else
            {
                lastHeadPos = Vector3.zero;
                lastHeadRot = Quaternion.identity;
            }
            if (enableDebugLogs)
                Debug.Log($"RecordingStillnessWatcher: Start() loggerManager={(loggerManager!=null)}, stopPopup={(stopPopup!=null)}, secondsBeforeAlertStop={secondsBeforeAlertStop:F2}, movementThreshold={movementThreshold:F4}, rotationThresholdDeg={rotationThresholdDeg:F2}");
            if (enableDebugLogs)
                Debug.Log($"RecordingStillnessWatcher: initial head pos={lastHeadPos} rot={lastHeadRot.eulerAngles}");
        }

        void Update()
        {
            // Try to resolve loggerManager dynamically if not assigned in inspector
            if (loggerManager == null)
            {
                loggerManager = FindObjectOfType<LoggingManagerAPI>();
                if (loggerManager != null && enableDebugLogs)
                    Debug.Log("RecordingStillnessWatcher: Found LoggingManagerAPI instance at runtime.");
            }

            // Determine which transform to use for head tracking each frame
            Transform head = headTransform != null ? headTransform : (Camera.main != null ? Camera.main.transform : null);
            if (head == null)
            {
                // Try to find the runtime-cloned HMD (common name: Unity_SteamVR_Handler(Clone) -> HMD)
                searchTimer -= Time.deltaTime;
                if (searchTimer <= 0f)
                {
                    searchTimer = searchInterval;
                    Transform found = TryFindRuntimeHMD();
                    if (found != null)
                    {
                        headTransform = found;
                        head = headTransform;
                        if (enableDebugLogs) Debug.Log($"RecordingStillnessWatcher: Auto-assigned headTransform to runtime HMD '{found.name}'");
                    }
                }

                if (head == null)
                {
                    if (enableDebugLogs) Debug.Log("RecordingStillnessWatcher: Update() missing headTransform and Camera.main; assign `headTransform` or ensure a Camera is tagged MainCamera.");
                    stillAccum = 0f;
                    if (stopPopup != null) stopPopup.Hide();
                    return;
                }
            }

            if (loggerManager == null)
            {
                if (enableDebugLogs) Debug.Log("RecordingStillnessWatcher: Update() missing LoggingManagerAPI - make sure a LoggingManagerAPI is present and active in the scene or assign it in the inspector.");
                stillAccum = 0f;
                if (stopPopup != null) stopPopup.Hide();
                return;
            }

            // If not recording, hide popup and reset suppression so next recording can show it again
            if (!loggerManager.isRecording)
            {
                if (enableDebugLogs) Debug.Log("RecordingStillnessWatcher: not recording -> reset stillAccum and hide popup");
                stillAccum = 0f;
                if (stopPopup != null)
                {
                    stopPopup.Hide();
                    stopPopup.ClearSuppression();
                }
                // update last head pos/rot for next recording
                lastHeadPos = head.position;
                lastHeadRot = head.rotation;
                return;
            }

            if (enableDebugLogs) Debug.Log($"RecordingStillnessWatcher: recording active -> monitoring movement. secondsBeforeAlertStop={secondsBeforeAlertStop:F2}, stillAccum={stillAccum:F2}");

            // When recording, monitor head motion
            Vector3 currentPos = head.position;
            Quaternion currentRot = head.rotation;

            if (enableDebugLogs)
            {
                Debug.Log($"RecordingStillnessWatcher: head transform='{head.name}' currentPos={currentPos} lastPos={lastHeadPos} currentRot={currentRot.eulerAngles} lastRot={lastHeadRot.eulerAngles}");
            }

            float posDelta = Vector3.Distance(currentPos, lastHeadPos);
            float rotDeltaDeg = Quaternion.Angle(currentRot, lastHeadRot);

            if (posDelta > movementThreshold || rotDeltaDeg > rotationThresholdDeg)
            {
                // movement detected -> reset accumulation and hide popup
                if (enableDebugLogs) Debug.Log($"RecordingStillnessWatcher: movement detected posDelta={posDelta:F6} (threshold={movementThreshold:F6}) rotDeltaDeg={rotDeltaDeg:F3} (threshold={rotationThresholdDeg:F3}) -> resetting stillAccum and clearing any suppression");
                stillAccum = 0f;
                if (stopPopup != null)
                {
                    stopPopup.Hide();
                    // Clear suppression so a later stillness event can show the popup again
                    stopPopup.ClearSuppression();
                }
            }
            else
            {
                stillAccum += Time.deltaTime;
                if (enableDebugLogs) Debug.Log($"RecordingStillnessWatcher: no significant movement posDelta={posDelta:F6} rotDeltaDeg={rotDeltaDeg:F3} -> stillAccum={stillAccum:F3}");
                if (stillAccum >= secondsBeforeAlertStop)
                {
                    if (stopPopup != null)
                    {
                        if (enableDebugLogs) Debug.Log("RecordingStillnessWatcher: stillness threshold reached -> showing stop popup");
                        // choose popup position target if provided, else use head
                        Transform t = popupPositionTarget != null ? popupPositionTarget : head;
                        stopPopup.Show(t);
                    }
                }
            }

            lastHeadPos = currentPos;
            lastHeadRot = currentRot;
        }

        Transform TryFindRuntimeHMD()
        {
            // Look for GameObjects named like Unity_SteamVR_Handler* and find a child named HMD
            var all = GameObject.FindObjectsOfType<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t.name.Contains("Unity_SteamVR_Handler"))
                {
                    var child = FindChildByNameContains(t, "HMD");
                    if (child != null)
                        return child;

                    // fallback: any Camera under this handler
                    var cam = t.GetComponentInChildren<Camera>();
                    if (cam != null)
                        return cam.transform;
                }
            }

            // Fallback: find any transform with HMD in the name
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t.name.ToLower().Contains("hmd"))
                    return t;
            }

            return null;
        }

        Transform FindChildByNameContains(Transform parent, string namePart)
        {
            if (parent.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                var found = FindChildByNameContains(c, namePart);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
