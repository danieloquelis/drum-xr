using System.Collections;
using System.Text;
using Meta.XR.ImmersiveDebugger;
using Newtonsoft.Json;
using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Roboflow
{
    public class RoboflowInference : MonoBehaviour
    {
        public enum ErrorType
        {
            PcaNotReady,
            PcaPermissionNotGranted,
            HttpError
        }
    
        [Header("Dependencies")] 
        [SerializeField] private WebCamTextureManager webCamTextureManager;
    
        [Header("Roboflow Settings")]
        [SerializeField] private string apiKey;
    
        [Tooltip("The name that appears in the url in kebab-case. Ex: my-workspace")]
        [SerializeField] private string workspace;
    
        [Tooltip("The name of the workflow project in kebab-case. Ex: my-project")]
        [SerializeField] private string workflow;

        [Header("Events")]
        public UnityEvent onStart;
        public UnityEvent<ErrorType> onError;
        public UnityEvent<string> onSuccess;
    
        public void RunInference()
        {
            onStart?.Invoke();
            StartCoroutine(CaptureAndSend());
        }

        [DebugMember]
        private IEnumerator CaptureAndSend()
        {
            if (!webCamTextureManager || !webCamTextureManager.WebCamTexture)
            {
                onError?.Invoke(ErrorType.PcaNotReady);
                yield break;
            }

            if (PassthroughCameraPermissions.HasCameraPermission != true)
            {
                onError?.Invoke(ErrorType.PcaPermissionNotGranted);
                yield break;
            }
        
            var webCamTexture = webCamTextureManager.WebCamTexture;
            var imageBase64 = GetSnapBase64(webCamTexture);
            var request = BuildRequest(imageBase64);
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"Roboflow inference failed with error: {request.error}");
                onError?.Invoke(ErrorType.HttpError);
            }
        }

        private UnityWebRequest BuildRequest(string imageBase64) 
        {
            var payload = JsonConvert.SerializeObject(new Payload(apiKey, imageBase64));
            var request = new UnityWebRequest(GetUrl(), "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(payload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            return request;
        }
        
        private string GetUrl()
        {
            return $"https://serverless.roboflow.com/infer/workflows/{workspace}/{workflow}";
        }

        private static string GetSnapBase64(WebCamTexture webCamTexture)
        {
            var snap = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            snap.SetPixels(webCamTexture.GetPixels());
            snap.Apply();
        
            var imageBytes = snap.EncodeToPNG();
            return System.Convert.ToBase64String(imageBytes);
        }
    }
}
