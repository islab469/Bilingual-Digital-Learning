using UnityEngine;

public class CanvasGestureController : MonoBehaviour
{
    public float scaleSpeed = 0.01f;      // 控制縮放靈敏度
    public float moveSpeed = 0.0015f;     // 控制拖曳速度
    public float minScale = 0.008f;
    public float maxScale = 0.03f;

    public Camera arCamera;               // 拖入 Main Camera (ARCamera)

    private Vector2 lastSingleTouchPosition;
    private float lastPinchDistance;

    void Start()
    {
        // 🚀 一開始對準相機
        FaceToCamera(arCamera);
    }

    void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                lastSingleTouchPosition = t.position;
            }
            else if (t.phase == TouchPhase.Moved)
            {
                Vector2 delta = t.position - lastSingleTouchPosition;
                Vector3 move = new Vector3(delta.x * moveSpeed, delta.y * moveSpeed, 0);

                // ✅ 將位移轉換為世界空間（以攝影機角度）
                if (arCamera == null) arCamera = Camera.main;
                Vector3 worldMove = arCamera.transform.TransformDirection(move);
                transform.position += new Vector3(worldMove.x, worldMove.y, 0);

                lastSingleTouchPosition = t.position;
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float currentDistance = Vector2.Distance(t0.position, t1.position);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                lastPinchDistance = currentDistance;
            }
            else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                if (Mathf.Approximately(lastPinchDistance, 0)) return;

                float delta = currentDistance - lastPinchDistance;
                float factor = 1 + delta * scaleSpeed;

                Vector3 newScale = transform.localScale * factor;

                float clamped = Mathf.Clamp(newScale.x, minScale, maxScale);
                transform.localScale = new Vector3(clamped, clamped, clamped);

                lastPinchDistance = currentDistance;
            }
        }
    }

    // ✅ 一開始讓 Canvas 正對相機
    public void FaceToCamera(Camera cam)
    {
        if (cam == null) cam = Camera.main;
        Vector3 dir = transform.position - cam.transform.position;
        transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}
