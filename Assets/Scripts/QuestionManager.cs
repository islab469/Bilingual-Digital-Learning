using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using System.Collections;

public class QuestionManager : MonoBehaviour
{
    [System.Serializable]
    public class QuestionEntry
    {
        public string question;
        public string[] options;
        public char answer;
    }

    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    public TextMeshProUGUI resultText;
    public Button nextButton;

    private List<QuestionEntry> questions = new List<QuestionEntry>();
    private QuestionEntry currentQuestion;

    void Start()
    {
        StartCoroutine(LoadQuestions());
        nextButton.onClick.AddListener(ShowRandomQuestion);
    }

    IEnumerator LoadQuestions()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Question.txt");
        string content = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        UnityWebRequest www = UnityWebRequest.Get(path);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ 無法從 StreamingAssets 載入題庫：" + www.error);
            yield break;
        }

        content = www.downloadHandler.text;
#else
        if (!File.Exists(path))
        {
            Debug.LogError("❌ 找不到 StreamingAssets/Question.txt");
            yield break;
        }

        content = File.ReadAllText(path);
#endif

        Debug.Log("✅ 成功載入題庫內容，前50字：" + content.Substring(0, Mathf.Min(50, content.Length)));

        string[] lines = content.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i + 5 < lines.Length; i += 6)
        {
            try
            {
                QuestionEntry q = new QuestionEntry();
                q.question = lines[i].Trim();
                q.options = new string[4];
                q.options[0] = lines[i + 1].Substring(3).Trim();
                q.options[1] = lines[i + 2].Substring(3).Trim();
                q.options[2] = lines[i + 3].Substring(3).Trim();
                q.options[3] = lines[i + 4].Substring(3).Trim();
                q.answer = lines[i + 5].Trim()[lines[i + 5].Length - 1];

                questions.Add(q);
            }
            catch
            {
                Debug.LogError($"❌ 題庫第 {i / 6 + 1} 題格式錯誤，已跳過");
            }
        }

        if (questions.Count > 0)
        {
            ShowRandomQuestion();
        }
        else
        {
            Debug.LogWarning("⚠️ 題庫為空或全數錯誤，請檢查 Question.txt 格式");
        }
    }

    void ShowRandomQuestion()
    {
        if (questions.Count == 0)
        {
            Debug.LogError("❌ 題庫為空，請確認 Question.txt 格式");
            return;
        }

        currentQuestion = questions[Random.Range(0, questions.Count)];
        questionText.text = currentQuestion.question;
        resultText.text = "";
        nextButton.gameObject.SetActive(false);

        for (int i = 0; i < 4; i++)
        {
            optionTexts[i].text = $"{(char)('A' + i)}. {currentQuestion.options[i]}";
            int index = i;
            optionButtons[i].interactable = true;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnSelect(index));
        }
    }

    void OnSelect(int index)
    {
        char selected = (char)('A' + index);
        bool isCorrect = selected == currentQuestion.answer;

        resultText.text = isCorrect
            ? "✅ 答對了！"
            : $"❌ 答錯了！正確答案是 {currentQuestion.answer}";

        foreach (var btn in optionButtons)
            btn.interactable = false;

        nextButton.gameObject.SetActive(true);
    }
}
