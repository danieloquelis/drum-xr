using Meta.XR.ImmersiveDebugger;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Audio
{
    public class DrumAudioClassifier : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private AudioDetector audioDetector;
        [SerializeField] private int fftSize = 1024;

        [Header("Events")]
        public UnityEvent<string> onDrumClassDetected;

        private DrumProfileCollection m_profiles;

        private void Start()
        {
            LoadProfiles();
            PrintProfiles();
            audioDetector.onAudioDetected.AddListener(OnAudioDetected);
            audioDetector.StartMicStreaming();
        }

        private void OnAudioDetected(AudioWaveform waveform)
        {
            float[] spectrum = new float[fftSize];
            AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

            string detectedClass = DetectDrumClass(spectrum);
            Debug.Log($"Detected drum hit: {detectedClass}");
            onDrumClassDetected?.Invoke(detectedClass);
        }

        private void PrintProfiles()
        {
            foreach (var profile in m_profiles.profiles)
            {
                Debug.Log($"Class: {profile.className}, Snapshots: {profile.Snapshots.Count}");
            }
        }

        private string DetectDrumClass(float[] current)
        {
            string bestMatch = null;
            float bestScore = float.MinValue;

            foreach (var profile in m_profiles.profiles)
            {
                foreach (var snapshot in profile.Snapshots)
                {   
                    float score = CosineSimilarity(current, snapshot);
                    Debug.Log($"Comparing current: {current} vs snapshot: {snapshot} with score: {score}");
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = profile.className;
                    }
                }
            }

            return bestMatch;
        }

        private float CosineSimilarity(float[] a, float[] b)
        {
            float dot = 0f, magA = 0f, magB = 0f;
            for (int i = 0; i < Mathf.Min(a.Length, b.Length); i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            return dot / (Mathf.Sqrt(magA) * Mathf.Sqrt(magB) + 1e-6f);
        }

        [DebugMember]
        private void LoadProfiles()
        {
            if (!PlayerPrefs.HasKey("drum_profiles"))
            {
                Debug.LogError("No calibration data found");
                return;
            }

            var json = PlayerPrefs.GetString("drum_profiles");
            m_profiles = JsonConvert.DeserializeObject<DrumProfileCollection>(json);

            Debug.LogWarning("Loaded calibration data");
        }
    }
}
