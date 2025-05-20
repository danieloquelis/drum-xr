using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using Audio; 

public class CalibrationAudioManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private AudioDetector audioDetector;
    public int sampleCountPerClass = 5;
    public int fftSize = 1024;

    [Header("Events")]
    public UnityEvent<string> onPrompt;
    public UnityEvent onCalibrationComplete;

    private readonly string[] m_drumClasses = { "snare_drum", "tom_toms", "bass_drum" };
    private readonly DrumProfileCollection m_profileCollection = new();

    private DrumProfile m_currentProfile;
    private int m_currentSampleCount;
    private bool m_isCalibrating;

    private void Start()
    {
        audioDetector.onAudioDetected.AddListener(OnAudioDetected);
        audioDetector.onStreamStarted.AddListener(() => StartCoroutine(CalibrationRoutine()));
        audioDetector.StartMicStreaming();
    }

    private IEnumerator CalibrationRoutine()
    {
        m_isCalibrating = true;

        foreach (var drumClass in m_drumClasses)
        {
            m_currentProfile = new DrumProfile { className = drumClass };
            m_currentSampleCount = 0;
            Debug.LogWarning($"Hit the {drumClass} {sampleCountPerClass} times");
            onPrompt?.Invoke($"Hit the {drumClass} {sampleCountPerClass} times");

            while (m_currentSampleCount < sampleCountPerClass)
            {
                yield return null;
            }

            m_profileCollection.profiles.Add(m_currentProfile);
        }

        SaveProfiles();
        onCalibrationComplete?.Invoke();
        Debug.LogWarning("Calibration completed!");
        m_isCalibrating = false;
    }

    private void OnAudioDetected(AudioWaveform waveform)
    {
        if (!m_isCalibrating || m_currentProfile == null) return;

        float[] spectrum = new float[fftSize];
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        m_currentProfile.Snapshots.Add(spectrum);
        m_currentSampleCount++;

        Debug.LogWarning("[Calibration] Sample captured");
    }

    private void SaveProfiles()
    {
        var json = JsonConvert.SerializeObject(m_profileCollection);
        PlayerPrefs.SetString("drum_profiles", json);
        PlayerPrefs.Save();
    }
}
