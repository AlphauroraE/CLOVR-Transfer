using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XRT_OVR_Grabber
{
    public class UISubcategory : MonoBehaviour
    {
        Text subcategoryLabel;
        public GameObject labelBox;
        public GameObject spawnBar;
        public GameObject buttonPrefab;
        public List<GameObject> trackedButtons;

        [SerializeField] float widthMultiplier = 0.85f;    // <1 reduces spacing between buttons
        [SerializeField] float spacingPadding = -8f;

        // remember original HorizontalRail spacing so we can restore it later
        // NEW
        float savedHorizontalRailSpacing = float.NaN;


        // Tweaked but kinda works:
        public void SetQuestionButtons(string answers, string labels)
        {
            var answerList = answers.Split(",");
            var labelList = labels.Split(",");
            subcategoryLabel = labelBox.GetComponent<Text>();

            // Count how many buttons there will be
            int total = answerList.Length;


            // when <= 5 we keep original behavior (no layout changes)
            // when > 5 we apply a small uniform scale and horizontal centering
            float scaleFactor = 1f;
            float spacing = 0f;
            float startX = 0f;

            if (total > 5)
            {
                // --- NEW: set HorizontalLayoutGroup.spacing = -18 for >5 buttons (save original once) ---
                var hl = spawnBar != null ? spawnBar.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() : null;
                if (hl != null)
                {
                    if (float.IsNaN(savedHorizontalRailSpacing))
                        savedHorizontalRailSpacing = hl.spacing; // remember original
                    hl.spacing = -18f; // apply the desired spacing for >5 buttons
                }
                // --- end NEW ---

                // compute scale (tweak multipliers as desired)
                scaleFactor = Mathf.Clamp(1f - 0.12f * (total - 5), 0.45f, 0.9f);

                // estimate spacing from prefab width (fallback to 100 if missing)
                var prefabRT = buttonPrefab.GetComponent<RectTransform>();
                float baseWidth = (prefabRT != null) ? prefabRT.sizeDelta.x : 100f;

                // tighter spacing: multiply width and add padding
                spacing = baseWidth * scaleFactor * widthMultiplier + spacingPadding;

                // compute start X so the whole row is centered (leftmost x)
                startX = -((total - 1) * spacing) / 2f;
            }
            else
            {
                // restore original HorizontalLayoutGroup spacing for <=5 cases
                var hl = spawnBar != null ? spawnBar.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() : null;
                if (hl != null && !float.IsNaN(savedHorizontalRailSpacing))
                    hl.spacing = savedHorizontalRailSpacing;
            }

            // preserve prefab's original scale and apply a relative scale factor
            Vector3 prefabScale = buttonPrefab.transform.localScale;

            int index = 0;
            foreach (string s in answerList)
            {
                var gameObj = Instantiate(buttonPrefab, spawnBar.transform);
                var buttonClass = gameObj.GetComponent<UIAnswerButton>();


                if (total > 5)
                {
                    var rt = gameObj.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        // apply relative scaling (preserves original prefab proportions)
                        rt.localScale = Vector3.Scale(prefabScale, Vector3.one * scaleFactor);
                        // preserve existing y; place x in computed row
                        float y = rt.anchoredPosition.y;
                        rt.anchoredPosition = new Vector2(startX + index * spacing, y);
                    }
                }


                buttonClass.SetButtonResponse(s);
                if (index < labelList.Length)
                {
                    //Plops the label with the adjacent label name. 
                    buttonClass.SetButtonLabel(labelList[index]);
                }
                else
                {
                    //Hides the label marker. 
                    buttonClass.SetButtonLabel("");
                }
                trackedButtons.Add(gameObj);
                index++;
            }
        }

    public void SetQuestionLabel(string categoryName = "")
    {
        //Debug.Log(categoryName);
        subcategoryLabel = labelBox.GetComponent<Text>();
        subcategoryLabel.text = categoryName;
    }

        public void ClearButtons()
        {
            foreach (GameObject g in trackedButtons)
            {
                Destroy(g);
            }
            trackedButtons.Clear();
        }

    }
}