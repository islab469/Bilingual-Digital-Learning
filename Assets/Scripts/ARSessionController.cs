using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;

public class ARSessionController : MonoBehaviour
{
    public ARSession arSession;            // 拖入 ARSession
    public GameObject arCamera;            // 拖入 XR Origin 或 AR Camera GameObject
    public GameObject loadingOverlay;      // 黑底 + 顯示「AR啟動中...」
    public GameObject fallbackUI;          // 不支援 AR 時顯示的 UI
    public GameObject contentCanvas;       // 題目用的 Canvas（World Space）
    public Camera arUnityCamera;           // 拖入 Main Camera 的 Camera 組件

    void Awake()
    {
        // 避免黑畫面 + 裝置睡眠
        Application.runInBackground = true;
        Screen.fullScreen = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Start()
    {
        if (arCamera != null) arCamera.SetActive(false);
        if (loadingOverlay != null) loadingOverlay.SetActive(true);
        if (contentCanvas != null) contentCanvas.SetActive(false);

        // 顯示 AR 狀態變化
        ARSession.stateChanged += OnARSessionStateChanged;

        // 啟動流程
        StartCoroutine(InitializeARSession());
        StartCoroutine(ForceRefreshCamera());
        StartCoroutine(StartARSession());
    }

    void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
    {
        Debug.Log("📡 AR Session 狀態變更為：" + args.state);
    }

    IEnumerator StartARSession()
    {
        yield return new WaitForSeconds(1.0f); // 延遲 1 秒
        arSession.enabled = true;
    }


    IEnumerator ForceRefreshCamera()
    {
        yield return new WaitForEndOfFrame();
        if (arCamera != null)
        {
            arCamera.SetActive(false);
            yield return null;
            arCamera.SetActive(true);
        }
    }

    IEnumerator InitializeARSession()
    {
        Debug.Log("⏳ 檢查 AR 支援狀態中...");
        yield return ARSession.CheckAvailability();

        if (ARSession.state == ARSessionState.Unsupported)
        {
            Debug.LogError("🚫 裝置不支援 AR");
            if (fallbackUI != null) fallbackUI.SetActive(true);
            if (loadingOverlay != null) loadingOverlay.SetActive(false);
            yield break;
        }

        // 強制重啟 ARSession（Android 某些裝置需要這步驟）
        if (arSession != null)
        {
            arSession.enabled = false;
            yield return null;
            arSession.enabled = true;
        }

        yield return new WaitForSeconds(0.5f); // 防止閃屏

        float timeout = 15f;
        while (ARSession.state != ARSessionState.SessionTracking && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (ARSession.state == ARSessionState.SessionTracking)
        {
            Debug.Log("✅ AR tracking 成功");
        }
        else
        {
            Debug.LogWarning("⚠️ AR tracking 失敗，狀態：" + ARSession.state);
        }

        if (arCamera != null) arCamera.SetActive(true);
        if (loadingOverlay != null) loadingOverlay.SetActive(false);

        // 將 Canvas 放在 AR 相機正前方
        if (contentCanvas != null && arUnityCamera != null)
        {
            Transform cam = arUnityCamera.transform;
            Vector3 forward = cam.forward.sqrMagnitude < 0.001f ? Vector3.forward : cam.forward.normalized;
            Vector3 positionInFront = cam.position + forward * 1.5f;

            contentCanvas.transform.position = positionInFront;
            contentCanvas.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            contentCanvas.transform.localScale = Vector3.one * 0.01f;
            contentCanvas.SetActive(true);

            Debug.Log("📦 Canvas 放置於：" + positionInFront);
        }
    }
}
