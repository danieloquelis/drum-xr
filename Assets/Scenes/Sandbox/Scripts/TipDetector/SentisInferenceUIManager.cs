using System.Collections.Generic;
using PassthroughCameraSamples.MultiObjectDetection;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SentisInferenceUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private SentisObjectDetectedUiManager m_detectionCanvas;
    [SerializeField] private RawImage m_displayImage;
    [SerializeField] private Sprite m_boxTexture;
    [SerializeField] private Color m_boxColor;

    public UnityEvent<int> OnObjectsDetected;

    private List<GameObject> m_boxPool = new();
    private List<GameObject> m_activeBoxes = new();

    private Transform m_displayLocation;

    private void Start()
    {
        m_displayLocation = m_displayImage.transform;
    }

    public void SetDetectionCapture(Texture image)
    {
        m_displayImage.texture = image;
    }

    public void OnObjectDetectionError()
    {
        ClearAnnotations();
        OnObjectsDetected?.Invoke(0);
    }

    public void DrawUIBoxes(Tensor<float> boxes, Tensor<int> classIds, float imageWidth, float imageHeight)
    {
        ClearAnnotations();

        var displayWidth = m_displayImage.rectTransform.rect.width;
        var displayHeight = m_displayImage.rectTransform.rect.height;

        float scaleX = displayWidth / imageWidth;
        float scaleY = displayHeight / imageHeight;
        float halfWidth = displayWidth / 2f;
        float halfHeight = displayHeight / 2f;

        int numBoxes = boxes.shape[0];
        if (numBoxes <= 0)
        {
            OnObjectsDetected?.Invoke(0);
            return;
        }

        int max = Mathf.Min(numBoxes, 200);
        OnObjectsDetected?.Invoke(max);

        for (int i = 0; i < max; i++)
        {
            // If classId is -1 (filtered), skip
            if (classIds[i] < 0)
                continue;

            float x = boxes[i, 0] * scaleX - halfWidth;
            float y = boxes[i, 1] * scaleY - halfHeight;
            float w = boxes[i, 2] * scaleX;
            float h = boxes[i, 3] * scaleY;

            GameObject panel = GetBoxFromPool();
            panel.transform.SetParent(m_displayLocation, false);

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.localPosition = new Vector3(x, -y, 0);
            rt.sizeDelta = new Vector2(w, h);

            m_activeBoxes.Add(panel);
        }
    }

    private void ClearAnnotations()
    {
        foreach (var box in m_activeBoxes)
        {
            if (box != null) box.SetActive(false);
        }
        m_activeBoxes.Clear();
    }

    private GameObject GetBoxFromPool()
    {
        foreach (var box in m_boxPool)
        {
            if (!box.activeSelf)
            {
                box.SetActive(true);
                return box;
            }
        }

        GameObject newBox = CreateNewBox(m_boxColor);
        m_boxPool.Add(newBox);
        return newBox;
    }

    private GameObject CreateNewBox(Color color)
    {
        var panel = new GameObject("BBox");

        panel.AddComponent<CanvasRenderer>();
        var img = panel.AddComponent<Image>();
        img.color = color;
        img.sprite = m_boxTexture;
        img.type = Image.Type.Sliced;
        img.fillCenter = false;

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        return panel;
    }


}
