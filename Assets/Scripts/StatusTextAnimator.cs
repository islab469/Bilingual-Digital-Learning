using UnityEngine;
using TMPro;
using System.Collections;

public class StatusTextAnimator : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public string baseText = "AR啟動中";
    public float interval = 0.5f;

    void Start()
    {
        StartCoroutine(AnimateDots());
    }

    IEnumerator AnimateDots()
    {
        int dotCount = 0;

        while (true)
        {
            statusText.text = baseText + new string('.', dotCount);
            dotCount = (dotCount + 1) % 4; // 循環 0~3 個點
            yield return new WaitForSeconds(interval);
        }
    }
}
