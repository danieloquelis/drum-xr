using System;
using Detection;
using Meta.XR;
using Meta.XR.ImmersiveDebugger;
using Models;
using Newtonsoft.Json;
using PassthroughCameraSamples;
using Roboflow;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Scenes.DrumScanScene.Scripts.Detection
{
    public class DetectionManager : MonoBehaviour
    {
        [Header("PCA")] 
        [SerializeField] private WebCamTextureManager webCamTextureManager;
    
        [Header("Roboflow")]
        [SerializeField] private RoboflowInference inference;

        [Header("Placement")]
        [SerializeField] private EnvironmentRaycastManager environmentRaycastManager;
        [SerializeField] private DrumPadSpawner drumPadSpawner;

        [Header("UI")] 
        [SerializeField] private GameObject instructionDialog;
        [SerializeField] private TMP_Text instructionTitle;
        [SerializeField] private TMP_Text instructionDescription;
        [SerializeField] private GameObject errorDialog;
        [SerializeField] private GameObject successDialog;
        
        private bool m_isLoading;
        private PassthroughCameraEye Eye => webCamTextureManager.Eye;

        private void Start()
        {
            Assert.IsNotNull(inference, "RoboflowInference prefab is not set");
        
            inference.onStart.AddListener(OnDetectionStart);
            inference.onSuccess.AddListener(OnDetectionSuccess);
            inference.onError.AddListener(OnDetectionError);
            
            errorDialog.SetActive(false);
            successDialog.SetActive(false);
        }

        public void StartDetection()
        {
            if (m_isLoading) return;
            
            instructionDialog.SetActive(false);
            successDialog.SetActive(false);
            errorDialog.SetActive(false);
            
            inference.RunInference();
        }

        private void OnDetectionStart()
        {
            m_isLoading = true;
        }

        [DebugMember]
        private void OnDetectionSuccess(string result)
        {
            m_isLoading = false;
            
            RootResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<RootResponse>(result);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse response: {e.Message}");
                ShowErrorDialog();
                return;
            }
            
            if (response?.Outputs == null || response.Outputs.Count == 0)
            {
                ShowNoDrumsDetectedInstruction();
                return;
            }

            var detection = response.Outputs[0];
            var predictions = detection.RawRecognition?.Predictions;

            if (predictions == null || predictions.Count == 0)
            {
                ShowNoDrumsDetectedInstruction();
                return;
            }

            var imageWidth = detection.RawRecognition.Image.Width;
            var imageHeight = detection.RawRecognition.Image.Height;
            var camRes = PassthroughCameraUtils.GetCameraIntrinsics(Eye).Resolution;
            
            foreach (var prediction in predictions)
            {
                var px = Mathf.RoundToInt(prediction.X / imageWidth * camRes.x);
                var py = Mathf.RoundToInt((1f - prediction.Y / imageHeight) * camRes.y);

                var ray = PassthroughCameraUtils.ScreenPointToRayInWorld(Eye, new Vector2Int(px, py));

                if (!environmentRaycastManager.Raycast(ray, out var hit)) continue;
                
                var drumPadType = GetDrumPadType(prediction.Class);
                drumPadSpawner.SpawnAnchor(hit.point, drumPadType);
            }
            
            successDialog.SetActive(true);
            webCamTextureManager.WebCamTexture.Stop();
        }

        private static DrumPadType GetDrumPadType(string className)
        {
            return className switch
            {
                "snare_drum" => DrumPadType.Snare,
                "bass_drum" => DrumPadType.Kick,
                "tom_toms" => DrumPadType.Tom,
                "hit_hat" => DrumPadType.HiHat,
                "crash_cymbal" => DrumPadType.Cymbal,
                _ => DrumPadType.Unknown
            };
        }

        private void ShowNoDrumsDetectedInstruction()
        {
            instructionDialog.SetActive(true);
            instructionTitle.text = "No drums detected";
            instructionDescription.text = "Try to step in front of your drum set and trigger the scanning again.";
        }

        private void OnDetectionError(RoboflowInference.ErrorType error)
        {
            ShowErrorDialog();
        }

        private void ShowErrorDialog()
        {
            m_isLoading = false;
            errorDialog.SetActive(true);
        }
    }
}
