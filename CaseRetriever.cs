using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class CaseRetriever : MonoBehaviour
{
    [SerializeField]
    private bool debugMode;

    public Question[][] CaseQuestions;
    public string[][] CaseHeadings;
    public string[][] OrderedDifferentials;
    public string[][] OrderedDifferentialsFeedback;

    //Case Save Path Here <-----------------------------------------------------------------------------------------
    private string caseSavePath;
    public static string STREAMING_ASSETS_LOCATION = "https://seriousgame.cc.ic.ac.uk/StreamingAssets";
    public static string PHP_LOCATION = STREAMING_ASSETS_LOCATION + "/PHP";

    private void Awake()
    {
        caseSavePath = STREAMING_ASSETS_LOCATION + "/CaseQuestionsSave/";
        Debug.Log("The casesavepath is " + caseSavePath);

        /*
        if (GetComponent<GameFlow>().offlineMode)
        {
            //caseSavePath = Application.streamingAssetsPath + "/CaseQuestionsSave/"; //UNITY
        }
        else
        {
            //caseSavePath = Application.persistentDataPath + "/CaseQuestionsSave/"; //BUILD
        }*/

        /*
        if (!Directory.Exists(caseSavePath))
        {
            Directory.CreateDirectory(caseSavePath);
        }*/
    }

    private void GetCaseQuestion(int n)
    {
        //Change to persistent (2 places) in the final port
        string path = caseSavePath + "CaseQuestions" + n + ".txt";
        Debug.Log("The case path is " + path);
        StartCoroutine(GetText(path));

        if (!File.Exists(path))
        {
            StartCoroutine(GetCase(n));
            Debug.Log("I couldn't find the file initially so I'm getting the case");
            return;
        }

        string saveString = File.ReadAllText(path);
        CaseSave caseSave = JsonUtility.FromJson<CaseSave>(saveString);

        if (caseSave == null || saveString == null)
        {
            StartCoroutine(GetCase(n));
            return;
        }

        if (caseSave.caseQuestions.Length < 5)
        {
            StartCoroutine(GetCase(n));
            return;
        }

        CaseQuestions[n - 1] = caseSave.caseQuestions;
        CaseHeadings[n - 1] = caseSave.caseHeadings;
        OrderedDifferentials[n - 1] = caseSave.orderedDifferentials;
        OrderedDifferentialsFeedback[n - 1] = caseSave.orderedDifferentialsFeedback;

        if (debugMode)
        {
            Debug.Log("Case " + n + " Questions: ");
            for (int i = 0; i < CaseQuestions[n - 1].Length; i++)
            {
                Debug.Log(CaseQuestions[n - 1][i]);
            }

            Debug.Log("Case " + n + " Headings: ");
            for (int i = 0; i < CaseHeadings[n - 1].Length; i++)
            {
                Debug.Log(CaseHeadings[n - 1][i]);
            }

            Debug.Log("Case " + n + " Ordered Differentials: ");
            for (int i = 0; i < OrderedDifferentials[n - 1].Length; i++)
            {
                Debug.Log(OrderedDifferentials[n - 1][i] + "Feedback: " + OrderedDifferentialsFeedback[n - 1][i]);
            }
        }

    }

    IEnumerator GetText(string path)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Show results as text
                Debug.Log(www.downloadHandler.text);

            }
        }
    }

    public void GetAllCases()
    {
        //Listing
        CaseQuestions = new Question[10][];
        CaseHeadings = new string[10][];
        OrderedDifferentials = new string[10][];
        OrderedDifferentialsFeedback = new string[10][];
        GetCaseQuestion(1);
        GetCaseQuestion(2);
        GetCaseQuestion(3);
    }

    IEnumerator GetCase(int n)
    {
        //coroutineDone = false;

        WWWForm form = new WWWForm();
        form.AddField("case", n);

        using (UnityWebRequest www = UnityWebRequest.Post(PHP_LOCATION + "/getcase.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                string[] caseData = www.downloadHandler.text.Split('\t');
                if (caseData[0] != "0") //Errors
                {
                    Debug.LogError(caseData[0]);
                }
                else
                {
                    /*for (int i = 0; i < caseData.Length; i++)
                    {
                        Debug.Log(caseData[i]);
                    }*/
                    int nextDataId = 1;
                    List<Question> caseQuestions = new List<Question>();
                    List<string> caseHeadings = new List<string>();
                    List<string> orderedDifferentials = new List<string>(5);
                    List<string> orderedDifferentialsFeedback = new List<string>(5);

                    //Case Questions:
                    while(caseData[nextDataId] != "0")
                    {
                        caseQuestions.Add(new Question(
                            int.Parse(caseData[nextDataId]),
                            int.Parse(caseData[nextDataId + 1]),
                            caseData[nextDataId + 2],
                            caseData[nextDataId + 3],
                            caseData[nextDataId + 4],
                            Question.DifferentialParse(caseData[nextDataId + 5]),
                            Question.DifferentialParse(caseData[nextDataId + 6]),
                            Question.DifferentialParse(caseData[nextDataId + 7]),
                            Question.DifferentialParse(caseData[nextDataId + 8]),
                            Question.DifferentialParse(caseData[nextDataId + 9]),
                            caseData[nextDataId + 10]
                            ));
                        nextDataId += 11;
                    }

                    nextDataId += 1;

                    //Sub Questions:
                    while (caseData[nextDataId] != "0")
                    {
                        caseQuestions[int.Parse(caseData[nextDataId]) - 1].SetRequired(caseQuestions[int.Parse(caseData[nextDataId + 1]) - 1]);
                        nextDataId += 2;
                    }

                    nextDataId += 1;

                    //Headings:
                    while (caseData[nextDataId] != "0")
                    {
                        caseHeadings.Add(caseData[nextDataId + 1]);
                        nextDataId += 2;
                    }

                    nextDataId += 1;

                    //Ordered Differentials:
                    while (caseData[nextDataId] != "0")
                    {
                        orderedDifferentials.Add(caseData[nextDataId + 1]);
                        orderedDifferentialsFeedback.Add(caseData[nextDataId + 2]);
                        nextDataId += 3;
                    }

                    if (debugMode)
                    {
                        for (int i = 0; i < caseQuestions.Count; i++)
                        {
                            Debug.Log(caseQuestions[i]);
                        }
                        Debug.Log("Case " + n + " Retrieved!");
                    }

                    CaseSave caseSave = new CaseSave
                    {
                        caseQuestions = caseQuestions.ToArray(),
                        caseHeadings = caseHeadings.ToArray(),
                        orderedDifferentials = orderedDifferentials.ToArray(),
                        orderedDifferentialsFeedback = orderedDifferentialsFeedback.ToArray()
                    };
                    string json = JsonUtility.ToJson(caseSave);
                    File.WriteAllText(caseSavePath + "CaseQuestions" + n + ".txt", json);

                    CaseQuestions[n - 1] = caseQuestions.ToArray();
                    CaseHeadings[n - 1] = caseHeadings.ToArray();
                    OrderedDifferentials[n - 1] = orderedDifferentials.ToArray();
                    OrderedDifferentialsFeedback[n - 1] = orderedDifferentialsFeedback.ToArray();
                }
            }
        }
    }
}

