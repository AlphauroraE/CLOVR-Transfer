using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace XRT_OVR_Grabber
{
    public class SevereResponsePopup : MonoBehaviour
    {
        [Tooltip("Button to undo severe response and return to the previous survey question.")]
        public Button undoButton;

        [Tooltip("Button to confirm disqualification and finalize questionnaire.")]
        public Button dismissButton;

        [Tooltip("Optional close button for runner to hide the popup without action.")]
        public Button closeButton;

        public UnityAction OnUndo;
        public UnityAction OnConfirm;
        public UnityAction OnClose;

        void Start()
        {
            // Deactivate the entire GameObject at start
            this.gameObject.SetActive(false);

            if (undoButton != null)
                undoButton.onClick.AddListener(HandleUndoClicked);

            if (dismissButton != null)
                dismissButton.onClick.AddListener(HandleConfirmClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleCloseClicked);
        }

        public void Show()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 3;

            this.gameObject.SetActive(true);
        }

        public void Hide()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 1;

            this.gameObject.SetActive(false);
        }

        void HandleUndoClicked()
        {
            OnUndo?.Invoke();
        }

        void HandleConfirmClicked()
        {
            OnConfirm?.Invoke();
        }

        void HandleCloseClicked()
        {
            OnClose?.Invoke();
            Hide();
        }
    }
}

