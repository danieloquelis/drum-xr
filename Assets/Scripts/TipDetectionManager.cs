using System.Collections;
using PassthroughCameraSamples;
using UnityEngine;

public class TipDetectionManager : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager m_webCamTextureManager;
    
    [Header("Sentis inference ref")]
    [SerializeField] private TipDetector m_runInference;
    
    private bool m_isSentisReady = false;
    
    private IEnumerator Start()
    {
        // Wait until Sentis model is loaded
        var sentisInference = FindAnyObjectByType<TipDetector>();
        while (!sentisInference.IsModelLoaded)
        {
            yield return null;
        }
        m_isSentisReady = true;
    }

    // Update is called once per frame
    void Update()
    {
        var hasWebCamTextureData = m_webCamTextureManager.WebCamTexture != null;
        if (!hasWebCamTextureData || m_runInference.IsRunning() || !m_isSentisReady) return;
        
        m_runInference.RunInference(m_webCamTextureManager.WebCamTexture);
    }
}
