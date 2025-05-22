using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class QuestionManager : MonoBehaviour
{
    [System.Serializable]
    public class QuestionEntry
    {
        public string question;
        public string[] options;
        public char answer;
    }

    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    public TextMeshProUGUI resultText;
    public Button nextButton;
    public Button retryButton;

    private List<QuestionEntry> questions = new List<QuestionEntry>();
    private List<QuestionEntry> currentQuizQuestions = new List<QuestionEntry>();
    private QuestionEntry currentQuestion;

    private int maxQuestionCount = 5;
    private int currentQuestionIndex = 0;
    private int correctCount = 0;
    private int wrongCount = 0;

    private HashSet<int> lastQuizIndices = new HashSet<int>();

    IEnumerator Start()
    {
        yield return LoadQuestions();
        if (questions.Count == 0)
        {
            Debug.LogError("Can't load questions. Please check the file path and format.");
            questionText.text = "Cannot load questions.";
            yield break;
        }

        retryButton.gameObject.SetActive(false);
        retryButton.onClick.AddListener(RestartQuiz);

        PrepareNewQuiz();
        ShowRandomQuestion();

        nextButton.onClick.AddListener(ShowRandomQuestion);
    }

    IEnumerator LoadQuestions()
    {
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "Question.txt");

        UnityWebRequest request = UnityWebRequest.Get(filePath);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to read questions: " + request.error);
            yield break;
        }

        string fileContent = request.downloadHandler.text;
        Debug.Log("File content length: " + fileContent.Length);

        string[] lines = fileContent.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Debug.Log("Number of lines read: " + lines.Length);

        for (int i = 0; i + 5 < lines.Length; i += 6)
        {
            QuestionEntry q = new QuestionEntry();
            q.question = lines[i].Trim();
            q.options = new string[4];
            q.options[0] = lines[i + 1].Substring(3).Trim();
            q.options[1] = lines[i + 2].Substring(3).Trim();
            q.options[2] = lines[i + 3].Substring(3).Trim();
            q.options[3] = lines[i + 4].Substring(3).Trim();

            string ansLine = lines[i + 5].Trim();
            if (!string.IsNullOrEmpty(ansLine) && ansLine.Length >= 9)
            {
                q.answer = char.ToUpper(ansLine[8]);
            }
            else
            {
                q.answer = ' ';
            }

            questions.Add(q);
        }

        Debug.Log("Loaded question count: " + questions.Count);
    }

    void PrepareNewQuiz()
    {
        currentQuestionIndex = 0;
        correctCount = 0;
        wrongCount = 0;
        resultText.text = "";
        nextButton.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(false);

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < questions.Count; i++)
        {
            if (!lastQuizIndices.Contains(i))
                availableIndices.Add(i);
        }

        if (availableIndices.Count < maxQuestionCount)
        {
            availableIndices.Clear();
            for (int i = 0; i < questions.Count; i++)
                availableIndices.Add(i);
        }

        currentQuizQuestions.Clear();
        lastQuizIndices.Clear();

        while (currentQuizQuestions.Count < maxQuestionCount)
        {
            int randIndex = availableIndices[Random.Range(0, availableIndices.Count)];
            currentQuizQuestions.Add(questions[randIndex]);
            lastQuizIndices.Add(randIndex);
            availableIndices.Remove(randIndex);
        }
    }

    void ShowRandomQuestion()
    {
        if (currentQuestionIndex >= maxQuestionCount)
        {
            ShowFinalResult();
            return;
        }

        currentQuestion = currentQuizQuestions[currentQuestionIndex];
        currentQuestionIndex++;

        questionText.text = "Question " + currentQuestionIndex + ": " + currentQuestion.question;
        resultText.text = "";
        nextButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);

        for (int i = 0; i < 4; i++)
        {
            optionTexts[i].text = (char)('A' + i) + ". " + currentQuestion.options[i];
            int index = i;
            optionButtons[i].interactable = true;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnSelect(index));
            optionButtons[i].GetComponent<Image>().color = Color.white;
        }
    }

    void OnSelect(int index)
    {
        char selected = (char)('A' + index);
        bool isCorrect = selected == currentQuestion.answer;

        if (isCorrect) correctCount++;
        else wrongCount++;

        if (isCorrect)
        {
            resultText.text = "Correct!";
            if (audioSource != null && correctSound != null)
                audioSource.PlayOneShot(correctSound);
        }
        else
        {
            int answerIndex = currentQuestion.answer - 'A';
            string correctOptionText = "";
            if (answerIndex >= 0 && answerIndex < currentQuestion.options.Length)
            {
                correctOptionText = currentQuestion.options[answerIndex];
            }
            resultText.text = $"Wrong! The correct answer is {(char)currentQuestion.answer}: {correctOptionText}";
            if (audioSource != null && wrongSound != null)
                audioSource.PlayOneShot(wrongSound);
        }

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].interactable = false;
            var btnImage = optionButtons[i].GetComponent<Image>();

            if (i == index)
            {
                btnImage.color = isCorrect ? Color.green : Color.red;
            }
            else if (i == currentQuestion.answer - 'A')
            {
                btnImage.color = Color.green;
            }
            else
            {
                btnImage.color = Color.white;
            }
        }

        nextButton.gameObject.SetActive(true);
    }

    void ShowFinalResult()
    {
        questionText.text = "Quiz Finished!";
        resultText.text = "You got " + correctCount + " right and " + wrongCount + " wrong.\n" + GetScoreMessage();
        nextButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(true);

        foreach (var btn in optionButtons)
            btn.gameObject.SetActive(false);
    }

    void RestartQuiz()
    {
        PrepareNewQuiz();
        ShowRandomQuestion();

        foreach (var btn in optionButtons)
            btn.gameObject.SetActive(true);
    }

    string GetScoreMessage()
    {
        if (correctCount == maxQuestionCount)
            return "Perfect! You answered all questions correctly! Fantastic job!";
        else if (correctCount == 0)
            return "Don't give up! Try again and you will improve!";
        else if (correctCount == maxQuestionCount - 1 && wrongCount == 1)
            return "Almost perfect! Just one mistake, great work!";
        else if (correctCount >= maxQuestionCount * 0.8f)
            return "Great job! You're doing really well!";
        else if (correctCount >= maxQuestionCount * 0.5f)
            return "Good effort! Keep practicing and you'll get better!";
        else
            return "Keep practicing! Every mistake is a chance to learn!";
    }
}
