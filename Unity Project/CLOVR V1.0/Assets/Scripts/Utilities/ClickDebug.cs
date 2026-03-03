using UnityEngine;
using UnityEngine.EventSystems;

namespace XRT_OVR_Grabber
{
    public class ClickDebug : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public string id = "";

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"ClickDebug: OnPointerClick {id} on {gameObject.name}");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log($"ClickDebug: OnPointerEnter {id} on {gameObject.name}");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log($"ClickDebug: OnPointerExit {id} on {gameObject.name}");
        }
    }
}
