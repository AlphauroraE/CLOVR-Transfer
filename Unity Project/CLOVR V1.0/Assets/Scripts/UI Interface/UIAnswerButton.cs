using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace XRT_OVR_Grabber
{
    public class UIAnswerButton : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI response;
        public TMPro.TextMeshProUGUI label;
        public GameObject spacer;
        public GameObject labelObject; 
        public Button button;
        // private MyButton button;


        public void SetButtonResponse(string _label)
        {

            response.text = _label;
        }

        public void SetButtonLabel(string name)
        {
            if (name == "")
            {
                spacer.SetActive(true);
                labelObject.SetActive(false);
            }
            else
            {
                spacer.SetActive(false);
                labelObject.SetActive(true);
                label.text = name;
            }
        }

        private IEnumerator Start()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(ButtonAction);

            yield return null; // wait a frame
            EventSystem.current.SetSelectedGameObject(null);
            button.OnPointerExit(null);
            button.OnDeselect(null);
        }

        public void ButtonAction()
        {
            QuestionnaireEvents.QuestionnaireButtonPressedNextQ.Invoke(response.text);
        }
    }
}