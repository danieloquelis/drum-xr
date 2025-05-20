using System.Collections;
using PassthroughCameraSamples;
using UnityEngine;

public class TipDetectionManager : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    
    [Header("Sentis inference ref")]
    [SerializeField] private TipDetector runInference;
    
    private bool m_isSentisReady = false;
    private bool m_isCameraReady = false;
    
    private WebCamTexture m_webCamTexture;
    private Texture2D m_cachedTexture;
    private Color32[] m_pixelBuffer;
    
    
    private IEnumerator Start()
    {
        // Wait until Sentis model is loaded
        var sentisInference = FindAnyObjectByType<TipDetector>();
        while (!sentisInference.IsModelLoaded)
        {
            yield return null;
        }
        m_isSentisReady = true;
        
        StartCoroutine(InitializeCameraWhenReady());
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_isCameraReady || runInference.IsRunning() || !m_isSentisReady) return;
        
        m_webCamTexture.GetPixels32(m_pixelBuffer);
        m_cachedTexture.SetPixels32(m_pixelBuffer);
        m_cachedTexture.Apply();
        
        runInference.RunInference(m_cachedTexture);
    }

    private IEnumerator InitializeCameraWhenReady()
    {
        var webCamTexture = webCamTextureManager.WebCamTexture;
        while (!webCamTexture || webCamTexture.width < runInference.inputSize.x)
            yield return null;

        m_webCamTexture = webCamTexture;
        
        m_cachedTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
        m_pixelBuffer = new Color32[webCamTexture.width * webCamTexture.height];
        
        m_isCameraReady = true;
    }
}
