using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Android;
using Unity.Barracuda;

public class ModelRunner : MonoBehaviour
{
    [Header("Model & UI")]
    public NNModel onnxModelAsset;           // ��JONNX�ҫ��귽
    public RawImage cameraPreview;           // ����e�����
    public TMP_Text resultText;              // ��X���G���

    private IWorker worker;
    private WebCamTexture webcamTexture;

    // �ҫ���J�j�p�]�n��V�m�ɤ@�P�^
    private const int INPUT_SIZE = 224;

    void Start()
    {
        RequestCameraPermissionAndStart();
        LoadModel();
    }

    void RequestCameraPermissionAndStart()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
        }
        else
        {
            StartCamera();
        }
    }

    void StartCamera()
    {
        if (webcamTexture == null)
        {
            webcamTexture = new WebCamTexture(INPUT_SIZE, INPUT_SIZE);
            cameraPreview.texture = webcamTexture;
            webcamTexture.Play();
        }
    }

    void LoadModel()
    {
        var model = ModelLoader.Load(onnxModelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, model);
    }

    void Update()
    {
        if (webcamTexture != null && webcamTexture.didUpdateThisFrame)
        {
            using (var input = Preprocess(webcamTexture))
            {
                worker.Execute(input);
                var output = worker.PeekOutput();
                int predictedClass = ArgMax(output.ToReadOnlyArray());
                resultText.text = $"���ѵ��G�GClass {predictedClass}";
                output.Dispose();
            }
        }
    }

    Tensor Preprocess(WebCamTexture webCamTex)
    {
        Texture2D snap = new Texture2D(webCamTex.width, webCamTex.height);
        snap.SetPixels(webCamTex.GetPixels());
        snap.Apply();

        Texture2D resized = ResizeTexture(snap, INPUT_SIZE, INPUT_SIZE);

        float[] floatValues = new float[INPUT_SIZE * INPUT_SIZE * 3];
        Color32[] pixels = resized.GetPixels32();

        for (int i = 0; i < pixels.Length; i++)
        {
            // MobileNetV2 �w�B�z�d�ҡGRGB ���� -1~1
            floatValues[i * 3 + 0] = (pixels[i].r / 255f) * 2f - 1f;
            floatValues[i * 3 + 1] = (pixels[i].g / 255f) * 2f - 1f;
            floatValues[i * 3 + 2] = (pixels[i].b / 255f) * 2f - 1f;
        }

        return new Tensor(1, INPUT_SIZE, INPUT_SIZE, 3, floatValues);
    }

    Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    int ArgMax(float[] array)
    {
        int maxIndex = 0;
        float maxValue = float.MinValue;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] > maxValue)
            {
                maxValue = array[i];
                maxIndex = i;
            }
        }
        return maxIndex;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && webcamTexture == null && Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            StartCamera();
        }
    }

    void OnDestroy()
    {
        if (worker != null)
        {
            worker.Dispose();
        }
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
        }
    }
}
