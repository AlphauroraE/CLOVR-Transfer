/// <summary>
/// This file provides a comprehensive interface for executing and managing the surveys.
/// It is designed to create an interactive environemnet for participants to take surveys and store their responses for further analysis
/// </summary>


using System;
using System.Collections; 
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;
using UnityEngine.Events;
using UnityEngine.UI;

namespace XRT_OVR_Grabber
{

/// <summary>
/// High level flowchart gives a brief overview of how a user progresses through the questionare from start to finish
    /* Basic logic path for someone doing a full survey/questionnaire portion. 
     *         
     *        Restart questionnaire
     *        |     
     *  o --- 0 --- o --- o --- 0 --- o 
     *  |     |     |     |     |     |
     *  Load questionnaire|     |     |
     *        |     |     |     |     |
     *        Questionnaire loop starts (Instructor assigns survey)
     *              |     |     |     |
     *              Questionnaire taker saves response (Next question)
     *                    |     |     |
     *                    Questionnaire complete (Instructor saves)
     *                          |     |
     *                          Clear all reponses and questions
     *                                |
     *                                Save all reponses 
     *                          
     */
    /// <summary>

    /// <summary>
    /// Main class managing entire questionare process. 
    /// <summary>
    public class SurveyInterface : MonoBehaviour
    {
        
        public GameObject titleScreenPrefab;        /// <summary> This is the gameobject of the title screen presented to the participant. <summary>
        public GameObject questionScreenPrefab;     /// <summary> This is the gameobject of the question presented to the participant. <summary>
        [Header("Severe-response handling")]
        [Tooltip("Optional override message shown on the Outro when a participant selects a 'Severe' response.")]
        public string severePopupMessage = "You are no longer eligible for this study, please notify the study runner";
        public GameObject finishedScreenPrefab;     /// <summary> This is the gameobject of the last screen of the questionnaires     <summary>
        public Text titleTextbox;                   /// <summary>  Used for the title - description of the questionnaire on the first screen of the questionnaire.<summary>
        public Text questionnaireNameTextbox;       ///<summary>  This is the text component of the question.<summary>
        public Text finishScreenTextbox;            /// <summary> This is the text component of the finishing portion <summary> 
        public UISubcategory questionnaireBox;      /// <summary> This controls how many questions are displayed to the participant through the questionnaire screen. <summary>
        string folderLocationForSurveys = "";       /// <summary> This is the location where the surveys are located. The user can specify any location on their computer. <summary>
        int currentlyActiveQuestionnaire = 0;       /// <summary> Sets which questionnaire is being used. 

       
        public LoggingManagerAPI LoggerManager;    
        XML_Reader XMLReader = new XML_Reader();
        List<Questionnaire> assignedQuestionnaires = new List<Questionnaire>();
        public List<string> questionnairesAsString = new List<string>(); 

 
        private UnityAction<string> _nextQuestionAction;
        private UnityAction saveAllQuestionnaireResults;
        private UnityAction finishTheQuestionnaire; 
    private UnityAction _autoAdvanceAction;

    [Header("Auto-advance settings")]
    public bool autoAdvanceQuestionnaires = false; // if true, automatically advances to the next questionnaire after finish
    public bool autoStartNextQuestionnaire = false; // if true, automatically starts the next questionnaire (skips title screen)
    public float autoAdvanceDelay = 1.0f; // seconds to wait before advancing
    [Header("UI watch settings")]
    public int uiWatchSeconds = 3; // how many seconds to monitor UI active state after setup
    public float uiWatchInterval = 1.0f; // interval between checks in seconds

        /// <summary>
        ///  Reads survey XML files from a specified folder. It checks if the directory exists, and if so, loads all of the XML files in that directory into the 'assignedQuestionnaires'"
        /// <summary>
        /// <returns> boolean, confirmation of success of reading files from folder. </returns>
        bool ReadSurveysFromFolder()
        {
            //List<string> filedSurveys;
            if (!Directory.Exists(folderLocationForSurveys))
            {
                Debug.LogError("Directory not valid");
                return false;
            }
            var files = Directory.GetFiles(folderLocationForSurveys);
            if (files.Length == 0)
            {
                Debug.LogError("Survey directory is empty.");
                return false;
            }

            //Load all files via XML loading. 
            questionnairesAsString.Clear();
            assignedQuestionnaires.Clear();
            foreach (string file in files)
            {
                if (file.Contains(".meta"))
                    continue;

                var survey = XMLReader.Load_XML_Questionnaire(file);

                questionnairesAsString.Add(survey.questionnaireName);
                assignedQuestionnaires.Add(survey);
            }
            //GetStringsForQuestionnaires(); 
            return true;
        }
        
        /// <summary>
        /// This function populates the 'questionaresAsString' list with the names of the questionaires
        /// <summary>
        void GetStringsForQuestionnaires()
        {
            questionnairesAsString.Clear();
            foreach(Questionnaire q in assignedQuestionnaires)
            {
                questionnairesAsString.Add(q.questionnaireName);
            }
        }

        /// <summary>
        /// This is for the UI shown on the PARTICIPANT UI. This simply updates the UI graphics.
        /// </summary>
        /// <returns></returns>
        bool UpdateQuestionnaireQuestion()
        {
            questionnaireBox.ClearButtons(); 
            var label = assignedQuestionnaires[currentlyActiveQuestionnaire].GetCurrentQuestion();
            questionnaireBox.SetQuestionLabel(label[0]);
            questionnaireBox.SetQuestionButtons(label[2], label[3]);
            return true;
        }

        /// <summary>
        /// This is for getting the current question on base of the API's current question index. 
        /// </summary>
        /// <returns></returns>
        public string[] _GetCurrentQuestionValues()
        {
            //Need to get the question, subcategories, and answers
            return assignedQuestionnaires[currentlyActiveQuestionnaire].GetCurrentQuestion();
        }

        /// <summary>
        /// This is the safe approach to loading the questionnaires and preparing them to be used by the API. 
        /// </summary>
        public void _LoadAndPrepareQuestionnaires()
        {
            var status = ReadSurveysFromFolder();
            if (!status)
                Debug.LogError("Error in loading and preparing the surveys.");
            // Log contents of assignedQuestionnaires for debugging
            try
            {
                if (assignedQuestionnaires != null)
                {
                    string names = "";
                    for (int i = 0; i < assignedQuestionnaires.Count; i++)
                    {
                        if (assignedQuestionnaires[i] != null)
                            names += assignedQuestionnaires[i].questionnaireName + (i < assignedQuestionnaires.Count - 1 ? ", " : "");
                        else
                            names += "<null>" + (i < assignedQuestionnaires.Count - 1 ? ", " : "");
                    }
                    Debug.Log($"[SurveyInterface] Loaded {assignedQuestionnaires.Count} questionnaires: {names}");
                }
                else
                {
                    Debug.Log("[SurveyInterface] assignedQuestionnaires is null after loading.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[SurveyInterface] Error while logging assignedQuestionnaires: " + e.Message);
            }

            status = UpdateQuestionnaireQuestion();
            if (!status)
                Debug.LogError("Error in loading and preparing the surveys.");
        }

        

        //////////////////////////////////////// Direct Questionnaire Controls /////////////////////////
        /// <summary>
        /// This function instructs the API to load a index-specified questionnaire. These will be loaded as they are sorted in the folder the XML files they reside in. 
        /// </summary>

        public void UI_SetupNextQuestionnaire(int index) // Called from InstructorsInterface.cs
        {
            Debug.Log("[SurveyInterface] UI_SetupNextQuestionnaire called with index=" + index + ", assignedCount=" + (assignedQuestionnaires != null ? assignedQuestionnaires.Count : 0));
            //QuestionnaireInterface._GetCurrentQuestionValues(); ????
            currentlyActiveQuestionnaire = index;
            _StartQuestionnaire(index);
            // Only let title screen be active right now
            if (titleScreenPrefab == null || questionScreenPrefab == null || finishedScreenPrefab == null)
            {
                Debug.LogError("[SurveyInterface] One or more UI prefab references are null: title=" + (titleScreenPrefab==null) + ", question=" + (questionScreenPrefab==null) + ", finished=" + (finishedScreenPrefab==null));
            }
            Debug.Log("[SurveyInterface] Setting UI active states: title->true, question->false, finished->false");
            titleScreenPrefab.SetActive(true);
            questionScreenPrefab.SetActive(false);
            finishedScreenPrefab.SetActive(false);
            Debug.Log("[SurveyInterface] After SetActive: titleActive=" + (titleScreenPrefab!=null ? titleScreenPrefab.activeSelf.ToString() : "null") + ", questionActive=" + (questionScreenPrefab!=null ? questionScreenPrefab.activeSelf.ToString() : "null") + ", finishedActive=" + (finishedScreenPrefab!=null ? finishedScreenPrefab.activeSelf.ToString() : "null"));
            // Start a short watcher to see if another script flips these active states shortly after setup
            try
            {
                if (uiWatchSeconds > 0 && this.gameObject.activeInHierarchy)
                {
                    StartCoroutine(WatchUIActiveStatesCoroutine(uiWatchSeconds, uiWatchInterval));
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[SurveyInterface] Failed to start UI watch coroutine: " + e.Message);
            }

            //LoggerManager.ToggleOverlayVisibility(true, LoggerManager.overlayPointerL);
            //LoggerManager.ToggleOverlayVisibility(true, LoggerManager.overlayPointerR);
        }

        /// <summary>
        /// This triggers the API to move to the next question in the questionnaire.
        /// </summary>
        public void UI_MoveToNextQuestion()
        {
            UpdateQuestionnaireQuestion();
        }
        /// <summary>
        /// This starts the Questionnaire so the participant can start viewing the questions. 
        /// </summary>
        public void UI_StartQuestionnaire()
        {
            Debug.Log("[SurveyInterface] UI_StartQuestionnaire called. titleActive(before)=" + (titleScreenPrefab!=null ? titleScreenPrefab.activeSelf.ToString() : "null") + ", questionActive(before)=" + (questionScreenPrefab!=null ? questionScreenPrefab.activeSelf.ToString() : "null"));
            titleScreenPrefab.SetActive(false);
            questionScreenPrefab.SetActive(true);
            Debug.Log("[SurveyInterface] UI_StartQuestionnaire changed states: titleActive(after)=" + (titleScreenPrefab!=null ? titleScreenPrefab.activeSelf.ToString() : "null") + ", questionActive(after)=" + (questionScreenPrefab!=null ? questionScreenPrefab.activeSelf.ToString() : "null"));
            UpdateQuestionnaireQuestion();

        }

        /// <summary>
        /// This triggers upon completing all the questions and show a final screen to the user, confirming they completed the questionnaire. 
        /// </summary>
        public void UI_MoveToCompletedScreen()
        {
            questionnaireBox.ClearButtons(); 
            questionScreenPrefab.SetActive(false);
            finishedScreenPrefab.SetActive(true);
        }

        /// <summary>
        /// This closes off the UI for the questionnaire and closes off the UI. 
        /// </summary>
        public void UI_CloseQuestionnaireFinal()
        {
            titleScreenPrefab.SetActive(false);
            questionScreenPrefab.SetActive(false);
            finishedScreenPrefab.SetActive(false);
            LoggerManager.ToggleOverlayVisibility(false);
            Debug.Log("[SurveyInterface] UI_CloseQuestionnaireFinal called. titleActive=" + (titleScreenPrefab!=null ? titleScreenPrefab.activeSelf.ToString() : "null") + ", questionActive=" + (questionScreenPrefab!=null ? questionScreenPrefab.activeSelf.ToString() : "null") + ", finishedActive=" + (finishedScreenPrefab!=null ? finishedScreenPrefab.activeSelf.ToString() : "null"));

            //LoggerManager.ToggleOverlayVisibility(false, LoggerManager.overlayPointerL);
            //.ToggleOverlayVisibility(false, LoggerManager.overlayPointerR);
            QuestionnaireEvents.ToggleKeyboard.Invoke(false);
        }        
        ///////////////////////////////////////////

        /// <summary>
        /// This function initializes the start of a particular questionnaire based on the given index (option)
        /// <summary>
        public void _StartQuestionnaire(int option)
        {
            currentlyActiveQuestionnaire = option;
            //TODO: I think we're binding a gameobject per each of the questionnaire's responses or somehow what will show up on screen for the participant at this point. 
            // Log text references and values for debugging
            try
            {
                string titleVal = "<null>";
                string nameVal = "<null>";
                if (assignedQuestionnaires != null && assignedQuestionnaires.Count > currentlyActiveQuestionnaire && assignedQuestionnaires[currentlyActiveQuestionnaire] != null)
                {
                    titleVal = assignedQuestionnaires[currentlyActiveQuestionnaire].GetTitle();
                    nameVal = assignedQuestionnaires[currentlyActiveQuestionnaire].GetQuestionnaireName();
                }
                Debug.Log("[SurveyInterface] _StartQuestionnaire called for index=" + option + ", titleTextbox ref=" + (titleTextbox!=null) + ", questionnaireNameTextbox ref=" + (questionnaireNameTextbox!=null) + ", titleValue='" + titleVal + "', nameValue='" + nameVal + "'");
            }
            catch (Exception e)
            {
                Debug.LogError("[SurveyInterface] Error logging questionnaire start values: " + e.Message);
            }

            if (assignedQuestionnaires != null && assignedQuestionnaires.Count > currentlyActiveQuestionnaire && assignedQuestionnaires[currentlyActiveQuestionnaire] != null)
            {
                if (titleTextbox != null)
                    titleTextbox.text = assignedQuestionnaires[currentlyActiveQuestionnaire].GetTitle();
                if (questionnaireNameTextbox != null)
                    questionnaireNameTextbox.text = assignedQuestionnaires[currentlyActiveQuestionnaire].GetQuestionnaireName();
            }
        }

        IEnumerator WatchUIActiveStatesCoroutine(int seconds, float interval)
        {
            int checks = Mathf.Max(1, Mathf.CeilToInt(seconds / interval));
            for (int i = 0; i < checks; i++)
            {
                yield return new WaitForSeconds(interval);
                Debug.Log("[SurveyInterface] UI Watch check " + (i+1) + "/" + checks + ": titleActive=" + (titleScreenPrefab!=null ? titleScreenPrefab.activeSelf.ToString() : "null") + ", questionActive=" + (questionScreenPrefab!=null ? questionScreenPrefab.activeSelf.ToString() : "null") + ", finishedActive=" + (finishedScreenPrefab!=null ? finishedScreenPrefab.activeSelf.ToString() : "null"));
            }
            yield break;
        }

        [SerializeField]
        float topTimer =0.05f;
        float currentTimer = 0.0f;
        bool ghostingLock = false;
        private void Update()
        {

            if (currentTimer > topTimer)
            {
                ghostingLock = false;
            }
            else
            {
                currentTimer += Time.deltaTime;
            }

        }


        /// <summary>
        /// Saves responses and moves onto next question 
        /// <summary>
        public void _SaveResponseAndMoveToNextQuestion(string value)
        {
            if (ghostingLock)
            {
                return; 
            }
            else
            {
                ghostingLock = true;
            }

            //string inValue = "";
            // Save the response first
            assignedQuestionnaires[currentlyActiveQuestionnaire].SaveResponse(value);

            // Detect SSQ severe response (value may be like "3 (Severe)" or just "3").
            try
            {
                string qName = assignedQuestionnaires[currentlyActiveQuestionnaire].questionnaireName ?? "";
                bool isSSQ = qName.ToLower().Contains("ssq") || qName.ToLower().Contains("simulator");

                // Normalize the response token
                string token = value != null ? value.Trim() : "";
                string firstToken = token == "" ? "" : token.Split(' ')[0].Trim();

                bool severeSelected = false;
                var q = assignedQuestionnaires[currentlyActiveQuestionnaire];
                try
                {
                    // 1) If labels are present, check whether the selected answer maps to a label containing "severe".
                    if (q.labels != null && q.labels.Count > 0)
                    {
                        // Try to find selected index by matching answer token to answers list
                        int selIndex = -1;
                        if (q.answers != null)
                        {
                            for (int i = 0; i < q.answers.Count; i++)
                            {
                                if (q.answers[i] != null && q.answers[i].Trim() == firstToken)
                                {
                                    selIndex = i;
                                    break;
                                }
                            }
                        }

                        // If not found by answer value, maybe the button sent the label text directly
                        if (selIndex == -1)
                        {
                            for (int i = 0; i < q.labels.Count; i++)
                            {
                                if (q.labels[i] != null && q.labels[i].Trim().ToLower().Contains(firstToken.ToLower()))
                                {
                                    selIndex = i;
                                    break;
                                }
                            }
                        }

                        if (selIndex >= 0 && selIndex < q.labels.Count)
                        {
                            if (q.labels[selIndex] != null && q.labels[selIndex].ToLower().Contains("severe"))
                                severeSelected = true;
                        }
                    }

                    // 2) Fallback: if answers are numeric, treat the maximal answer as severe and compare numerically
                    if (!severeSelected && q.answers != null && q.answers.Count > 0)
                    {
                        int selNum;
                        int maxNum = int.MinValue;
                        bool selParsed = int.TryParse(firstToken, out selNum);
                        for (int i = 0; i < q.answers.Count; i++)
                        {
                            int v;
                            if (int.TryParse(q.answers[i], out v))
                            {
                                if (v > maxNum) maxNum = v;
                            }
                        }
                        if (selParsed && selNum == maxNum && maxNum != int.MinValue)
                            severeSelected = true;
                    }
                }
                catch (Exception) { /* ignore and let other heuristics decide */ }

                if (isSSQ && severeSelected)
                {
                    Debug.Log("[SurveyInterface] SSQ severe response detected. Stopping questionnaire and showing Outro with disqualification message.");

                    // Save partial responses (pad unanswered questions with blanks) and finalize this questionnaire run
                    try
                    {
                        var qObj = assignedQuestionnaires[currentlyActiveQuestionnaire];
                        if (qObj != null)
                        {
                            qObj.SaveIncompleteResponsesAndFinalize();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("[SurveyInterface] Error while saving incomplete questionnaire after severe response: " + e.Message);
                    }

                    // Prefer using the Outro/finished screen to show the message.
                    try
                    {
                        if (finishScreenTextbox != null)
                        {
                            finishScreenTextbox.text = string.IsNullOrEmpty(severePopupMessage) ? "You are no longer eligible for this study, please notify the study runner" : severePopupMessage;
                        }

                        // Show the finished/outro screen immediately
                        UI_MoveToCompletedScreen();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("[SurveyInterface] Error while showing Outro for severe response: " + e.Message);
                    }

                    // Don't progress to the next question
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[SurveyInterface] Error while evaluating severe-response logic: " + e.Message);
            }

            // Normal flow: update to next question
            UpdateQuestionnaireQuestion();
        }


        [SerializeField]
        Material backgroundMaterial;
        IEnumerator PanelSwitchAnimation()
        {
            float colorStep = 0.0f; 

            //for (int i=0; i < 1000; i++)
            while(colorStep < 1.0f)
            {
                backgroundMaterial.color = new Color(colorStep, colorStep, colorStep);
                colorStep += 0.001f;
                yield return new WaitForSeconds(0.01f);
            }

            //for (int i = 1000; i < 1000; i++)
            while (colorStep > 0)
            {
                backgroundMaterial.color = new Color(colorStep, colorStep, colorStep);
                colorStep -= 0.001f;
                yield return new WaitForSeconds(0.01f);
            }
        } 

        /// <summary>
        /// Clears responses of the currently active questionnaire
        /// <summary>
        public void _ClearCurrentQuestionnaire()
        {
            assignedQuestionnaires[currentlyActiveQuestionnaire].ClearResponses();
        }

        public void _ClearAllQuestionnaireResponses()
        {
            foreach (Questionnaire q in assignedQuestionnaires)
            {
                q.ClearResponses(); 
            }
        }


        /// <summary>
        /// Retrieves the name of the currently active questionnare 
        /// <summary>
        public string _GetCurrentlyAssignedQuestionnaireName()
        {
            return assignedQuestionnaires[currentlyActiveQuestionnaire].questionnaireName;
        }

        /// <summary> 
        /// This clears the data of all assigned questionnaires
        /// <summary>
        public void _CancelQuestionnaires()
        {
            foreach (Questionnaire q in assignedQuestionnaires)
            {
                q.ClearQuestionnaire();
            }
        }

        /// <summary> 
        /// Exports the data out to a single unique file that contains the questionnaire data.
        /// <summary>
        public void _SaveAllReponsesAndExport()
        {
            string exportLocation = LoggerManager.GetQuestionnaireOutputLocation();
           // Debug.Log(assignedQuestionnaires.Count);
            foreach (Questionnaire q in assignedQuestionnaires)
            {
                string outputLocation =  exportLocation + q.GetQuestionnaireName() + ".csv";
                try
                {
                    StreamWriter writer = new StreamWriter(outputLocation, true);
                    string questionnairesToString = q.GetQuestionnaireHeader();
                    questionnairesToString += q.GetStringVer();
                    //Debug.Log(questionnairesToString);

                    writer.Write(questionnairesToString);
                    writer.Close();
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }

                q.ClearQuestionnaire();
            }
        }

        public void DirectExportToFolder(string location)
        {
            foreach (Questionnaire q in assignedQuestionnaires)
            {
                string outputLocation = location+ "\\" + q.GetQuestionnaireName() + ".csv";
                try
                {
                    StreamWriter writer = new StreamWriter(outputLocation, true);
                    string questionnairesToString = q.GetQuestionnaireHeader();
                    questionnairesToString += q.GetStringVer();
                    //Debug.Log(questionnairesToString);

                    writer.Write(questionnairesToString);
                    writer.Close();
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }
                q.ClearQuestionnaire();
            }
        }


        /// <summary>
        /// Used just for clearing out files that may not work (E.g. not XML files.) 
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public List<string> CheckIfContainsValidXMLFiles(string location)
        {
            List<string> outputFiles = new List<string>();
            foreach (string s in System.IO.Directory.GetFiles(location))
            {
                if (!s.Contains(".xml"))
                {
                    outputFiles.Add(s); 
                }
            }

            return outputFiles; 
        }


        /// <summary>
        /// Initializes survey by loading the necessary configurations and setting up event listeners. It prepaers the environment for surveys
        /// <summary>
        bool initialized = false; 
        private void _Init()
        {
            try
            {
                folderLocationForSurveys = LoggerManager.xmlQuestionnaireLocation; //Application.dataPath + "/Resources/XML_Questionnaires";
                //Debug.Log(folderLocationForSurveys);
                if (folderLocationForSurveys == "" || (!Directory.Exists(folderLocationForSurveys)))
                {
                    Debug.Log("Nothing loaded or invalid directory"); 
                    initialized = false;
                    return;
                }
                _LoadAndPrepareQuestionnaires();
                
                //Try to find the Logger Manager to administer the current experiment settings. 

                 
                UI_CloseQuestionnaireFinal();
                initialized = true;
                
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                Debug.LogError("Questionnaire location not valid. Nothing loaded");
                initialized = false;
            }
        }

        /// <summary>
        /// Debugging tool for creating and writing a predefined questionnaire to an XML file
        /// <summary>
        void DebugQuestionnaire()
        {
            List<string> questions = new List<string>
            {"I felt like I was actually there in the environment of the presentation.",
            "It seemed as though I actually took part in the action of the presentation.",
            "It was as though my true location had shifted into the environment of the presentation.",
            "I felt as though I was physically present in the environment of the presentation.",
            "I experienced the environment in the presentation as though I had stepped into a different place.",
            "I was convinced that things were actually happening around me.",
            "I had the feeling that I was in the middle of the action rather than merely observing.",
            "I felt like the objects in the presentation surrounded me.",
            "I experienced both the confined and open spaces in the presentation as though I was really there.",
            "I was convinced that the objects in the presentation were located on the various sides of my body.",

            "The objects in the presentation gave me the feeling that I could do things with them.",
            "I had the impression that I could be active in the environment of the presentation.",
            "I had the impression that I could act in the environment of the presentation.",
            "I had the impression that I could reach for the objects in the presentation.",
            "I felt like I could move around among the objects in the presentation.",
            "I felt like I could jump into the action.",
            "The objects in the presentation gave me the feeling that I could actually touch them.",
            "It seemed to me that I could do whatever I wanted in the environment of the presentation.",
            "It seemed to me that I could have some effect on things in the presentation, as I do in real life.",
            "I felt that I could move freely in the environment of the presentation."};
            List<string> answers = new List<string> { "1", "2", "3", "4", "5" };
            List<string> subQuestions = new List<string> { "Self-localization", "Possible Actions" };
            List<string> labels = new List<string> { "Strongly Disagree", "Disagree","Neutral","Agree","Strongly Agree"}; 
            string title = "Please take some time for this questionnaire.";
            string questionnaireName = "Spatial Precence Experience Scale";

            Questionnaire _q = new Questionnaire(questionnaireName, title, questions, subQuestions, answers, labels);
            string outputLoc = Application.dataPath + "/Resources/output.xml";
            XMLReader.Write_XML_Questtionaire(_q, outputLoc);
        }

        
        public void Awake()
        {
            _InitializeSurveyer += ManualInitialize;
            _nextQuestionAction += _SaveResponseAndMoveToNextQuestion;
            saveAllQuestionnaireResults += _SaveAllReponsesAndExport;
            finishTheQuestionnaire += UI_MoveToCompletedScreen;
            _autoAdvanceAction += AutoAdvanceAfterFinish;
            //_Init();
            //DebugQuestionnaire();
        }
        

        UnityAction _InitializeSurveyer; 

        public void ManualInitialize()
        {
            _Init(); 
            //DebugQuestionnaire();
        }
        
        public void ManualInitializationWithLocation(string location)
        {
            try
            {
                folderLocationForSurveys = location; 
                if (folderLocationForSurveys == "") 
                {
                    Debug.Log("Nothing loaded or invalid directory");
                    initialized = false;
                    return;
                }
                _LoadAndPrepareQuestionnaires();

                //Try to find the Logger Manager to administer the current experiment settings. 
                UI_CloseQuestionnaireFinal();
                LoggerManager.xmlQuestionnaireLocation = location;
                initialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                Debug.LogError("Questionnaire location not valid. Nothing loaded");
                initialized = false;
            }
        }


        /// <summary>
        /// Called when a script or object becomes active. This function is being used to set up event listeners
        /// <summary>
        private void OnEnable()
        {
            QuestionnaireEvents.ProjectInitialized.AddListener(_InitializeSurveyer);
            QuestionnaireEvents.QuestionnaireButtonPressedNextQ.AddListener(_nextQuestionAction);
            QuestionnaireEvents.QuestionnaireSaveAll.AddListener(saveAllQuestionnaireResults);
            QuestionnaireEvents.QuestionnaireFinished.AddListener(finishTheQuestionnaire);
            QuestionnaireEvents.QuestionnaireFinished.AddListener(_autoAdvanceAction);
        }

        /// <summary>
        /// Called when a script becomes inactive. Used to remove the event listeners set up in 'OnEnable()'
        /// <summary>
        private void OnDestroy()
        {
            QuestionnaireEvents.ProjectInitialized.RemoveListener(_InitializeSurveyer);
            QuestionnaireEvents.QuestionnaireButtonPressedNextQ.RemoveListener(_nextQuestionAction);
            QuestionnaireEvents.QuestionnaireSaveAll.RemoveListener(saveAllQuestionnaireResults);
            QuestionnaireEvents.QuestionnaireFinished.RemoveListener(finishTheQuestionnaire);
            QuestionnaireEvents.QuestionnaireFinished.RemoveListener(_autoAdvanceAction);
        
        }

        // Auto-advance helper: when a questionnaire finishes this will optionally move to the next one after a delay
        void AutoAdvanceAfterFinish()
        {
            Debug.Log("[SurveyInterface] AutoAdvanceAfterFinish called. autoAdvanceQuestionnaires=" + autoAdvanceQuestionnaires + ", autoStartNextQuestionnaire=" + autoStartNextQuestionnaire + ", autoAdvanceDelay=" + autoAdvanceDelay);
            if (!autoAdvanceQuestionnaires)
            {
                Debug.Log("[SurveyInterface] autoAdvanceQuestionnaires disabled — not advancing.");
                return;
            }

            StartCoroutine(AutoAdvanceCoroutine());
        }

        IEnumerator AutoAdvanceCoroutine()
        {
            if (autoAdvanceDelay > 0f)
                yield return new WaitForSeconds(autoAdvanceDelay);

            int nextIndex = currentlyActiveQuestionnaire + 1;
            Debug.Log("[SurveyInterface] AutoAdvanceCoroutine running. currentlyActiveQuestionnaire=" + currentlyActiveQuestionnaire + ", nextIndex=" + nextIndex + ", totalLoaded=" + (assignedQuestionnaires != null ? assignedQuestionnaires.Count : 0));

            if (assignedQuestionnaires != null && nextIndex < assignedQuestionnaires.Count)
            {
                Debug.Log("[SurveyInterface] Advancing to next questionnaire index: " + nextIndex + " ('" + (assignedQuestionnaires[nextIndex] != null ? assignedQuestionnaires[nextIndex].questionnaireName : "<null>") + "')");
                // Setup the next questionnaire (keeps existing behavior)
                UI_SetupNextQuestionnaire(nextIndex);

                // Re-enable overlay and keyboard so the UI is visible in the headset
                try
                {
                    if (LoggerManager != null)
                    {
                        LoggerManager.ToggleOverlayVisibility(true);
                        Debug.Log("[SurveyInterface] LoggerManager.ToggleOverlayVisibility(true) called to ensure overlay is visible.");
                    }
                    else
                    {
                        Debug.LogWarning("[SurveyInterface] LoggerManager is null; cannot toggle overlay visibility.");
                    }

                    // Re-open keyboard if needed
                    QuestionnaireEvents.ToggleKeyboard.Invoke(true);
                    Debug.Log("[SurveyInterface] QuestionnaireEvents.ToggleKeyboard.Invoke(true) called.");
                }
                catch (Exception e)
                {
                    Debug.LogError("[SurveyInterface] Error while trying to re-enable overlay/keyboard: " + e.Message);
                }

                // Optionally skip title screen and start immediately
                if (autoStartNextQuestionnaire)
                {
                    UI_StartQuestionnaire();
                }
            }
            else
            {
                Debug.Log("[SurveyInterface] No next questionnaire to advance to (nextIndex=" + nextIndex + ").");
            }
            // else: no more questionnaires — leave finished screen visible
            yield break;
        }
    }

    

    /// <summary>
    /// Represents a questionnaire with a list of questions, answers, and other metadata. Also provides methods for managing and retrieving questionnaire details
    /// <summary> 
    public class Questionnaire
    {

        public string title;
        public string questionnaireName;
        public List<string> questions;
        public List<string> subquestions;
        public List<string> answers;
        public List<string> labels;
        public List<string> timeStamps; 

        //Responses are the ones given by the user. 
        //List<List<string>> storedQuestionnaires = new List<List<string>>();
        //List<string> tempStoredResponses = new List<string>();
        List<string> storedQuestionnaires = new List<string>();
        string tempStoredResponses = ""; 


        int questionIndex    = 0;
        int subCategoryIndex = 0;
        string subcategoryResponses; 

    /// <summary> Default Constructor<summary> 
    public Questionnaire(){ timeStamps = new List<string>(); }
        
        /// <summary>
        /// Initializes a questionnaire with given details 
        /// <summary> 
        public Questionnaire(string _questionnaireName, string _title, List<string> _questions, List<string> _subQ, List<string> _answers, List<string> _labels)
        {
            questionnaireName = _questionnaireName;
            title = _title;
            questions = _questions;
            subquestions = _subQ;
            answers = _answers;
            labels = _labels;
            timeStamps = new List<string>();
        }

        /// <summary>
        /// returns the question, it's subquestions and possible answers in an array format
        /// <summary>
        public string[] GetCurrentQuestion()
        {
            string[] values = {
                (string) questions[questionIndex],
                (string) ListToString(subquestions),
                (string) ListToString(answers),
                (string) ListToString(labels)
            };
            return values; 
        }


        /// <summary>
        /// Saves the user's response. If the user has answered all sub-questions of the current question, it moves to next question 
        /// If all questions are answered, the questionnaire is considered completed, and related events are invoked
        /// <summary> 
        public void SaveResponse(string input)
        {

            //Subcategory is going to be disabled.
            /*
            if(subCategoryIndex >= subquestions.Count)
            {
                //Stores all responses given to the category. 
                subcategoryResponses += "," + input;
                subCategoryIndex = subquestions.Count;
                
                tempStoredResponses.Add(subcategoryResponses);
                questionIndex++;
            }
            else
            {
                //This takes a subcategory and pins it to a string.
                subCategoryIndex++;
                subcategoryResponses += "," + input;
                return;
            }*/

            if (questionIndex == 0)
            {
                tempStoredResponses += input;
            }
            else
            {
                tempStoredResponses += "," + input;
            }
            questionIndex++;

            if (questionIndex >= questions.Count)
            {
                SaveCompletedQuestionnaire();
                Debug.Log("[Questionnaire] All questions answered for '" + questionnaireName + "'. Invoking QuestionnaireFinished.");
                QuestionnaireEvents.QuestionnaireFinished.Invoke();
                tempStoredResponses = "";
            }
        }


        /// <summary>
        /// Saves all responses from 'tempStoredResponses' to 'storedQuestionnaires' and then clears up the temp storage
        /// <summary> 
        public void SaveCompletedQuestionnaire()
        {
            storedQuestionnaires.Add(tempStoredResponses);
            timeStamps.Add(DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-fff"));
            questionIndex = 0; 
        }

        /// <summary>
        /// Save the current (possibly partial) responses by padding unanswered questions with empty entries,
        /// then finalize this questionnaire run (store and reset temp responses).
        /// Does NOT invoke QuestionnaireFinished event — caller can decide whether to trigger end-of-questionnaire behavior.
        /// </summary>
        public void SaveIncompleteResponsesAndFinalize()
        {
            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(tempStoredResponses))
                parts = new List<string>(tempStoredResponses.Split(','));

            int totalQuestions = (questions != null) ? questions.Count : 0;
            while (parts.Count < totalQuestions)
                parts.Add("");

            tempStoredResponses = string.Join(",", parts);
            // reuse existing saving machinery
            SaveCompletedQuestionnaire();
        }

        /// <summary>
        /// Creates and returns a string of all the questions in a comma separated format. 
        /// <summary> 
        public string GetQuestionnaireHeader()
        {
            string outString = "";
            bool first = true;
            foreach (string s in questions)
            {
                if (first)
                {
                    first = false;
                    outString += s;
                }
                else
                {
                    outString += "," + s;
                }
            }

            return outString + ",timestamp" + "\n";
        }

        /////////////////////////////////////////////////////////////////// These are printers for instances in the questionnaire. 
        
        /// <summary>
        /// Prints the header of the file - basically only the questions. All other information will be on the title of the questionnaire. 
        /// </summary>
        /// <returns></returns>
        public string PrintHeader()
        {
            string outputValues = "";
            foreach (string s in questions)
            {
                outputValues += s;
            }
            return outputValues + ",timestamp" + "\n";
        }

        /// <summary>
        /// This converts a given string into a spaced out version 
        /// </summary>
        /// <param name="stringIn"></param>
        /// <param name="spacer"></param>
        /// <returns></returns>
        public string ListToString(List<string> stringIn, string spacer = ",")
        {
            string stringOut = "";
            bool firstString = true;

            foreach (string s in stringIn)
            {
                if (firstString)
                {
                    stringOut += s;
                    firstString = false;
                    continue;
                }
                stringOut += spacer + s;
            }
            return stringOut;
        }
        ///////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears the questionnaire of all results from all participants. Use this only if you want to erase all results.
        /// </summary>
        public void ClearQuestionnaire()
        {
            storedQuestionnaires.Clear();
            timeStamps.Clear();
            tempStoredResponses = ""; 
            ///tempStoredResponses.Clear(); 
        }

        /// <summary>
        /// Clears the currently executed questionnaire. Use this if you want to cancel the results from one participant from one instance. 
        /// </summary>
        public void ClearResponses()
        {
            tempStoredResponses = "";
        }
        
        /// <summary> Getter Function <summary> 
        public string GetQuestionnaireName()
        {
            return questionnaireName;
        }

        /// <summary> Getter Function <summary> 
        public string GetTitle()
        {
            return title; 
        }

        /// <summary> Getter Function <summary> 
        public List<string> GetQuestions()
        {
            return questions;
        }

        /// <summary> Getter Function <summary> 
        public List<string> GetSubQuestions()
        {
            return subquestions;
        }

        /// <summary> Getter Function <summary> 
        public List<string> GetAnswers()
        {
            return answers;
        }

        public List<string> GetLabels()
        {
            return labels;
        }

        /// <summary>
        /// returns all stored responses in a formatted string
        /// <summary> 
        public string GetStringVer()
        {
            string varOut = "";
            //Each column is a question and each row is a trial. 
            /*foreach(List<string> arr in storedQuestionnaires)
            {
                string tempVar = "";
                bool first = true;
                foreach (string s in arr)
                {
                    if (first)
                    {
                        tempVar += s;
                        first = false;
                    }
                    else
                    {
                        tempVar += "," + s;
                    }
                }
                varOut += tempVar + "\n";
            }*/
            int counter = 0;
            foreach(string s in storedQuestionnaires)
            {
                varOut += s +"," + timeStamps[counter] + "\n";
                counter++; 
            }

            return varOut;
        }
    }
}


// Use only for reference in manually creating a new questionnaire. 
