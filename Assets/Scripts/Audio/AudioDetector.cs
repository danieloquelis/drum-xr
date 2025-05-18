using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;

namespace Audio
{
    public class AudioDetector: MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private int sampleSize = 1024;
        [SerializeField] private float silenceThreshold = 0.01f;

        [Header("Events")]
        public UnityEvent onStreamStarted;
        public UnityEvent onStreamStopped;
        public UnityEvent<AudioWaveform> onAudioDetected;
        public UnityEvent onSilenceDetected;

        private string m_micName;
        private bool m_isStreaming;
        private bool m_wasSilent;

        public void StartMicStreaming()
        {
            StartCoroutine(InitMic());
        }

        public void StopMicStreaming()
        {
            if (!m_isStreaming) return;
        
            Microphone.End(m_micName);
            audioSource.Stop();
            m_isStreaming = false;
            onStreamStopped?.Invoke();
        }

        private IEnumerator InitMic()
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
                while (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                    yield return null;
            }


            if (Microphone.devices.Length == 0)
            {
                yield break;
            }

            m_micName = Microphone.devices[0];

            audioSource.clip = Microphone.Start(m_micName, true, 1, 44100);
            audioSource.loop = true;

            while (Microphone.GetPosition(m_micName) <= 0)
                yield return null;

            audioSource.Play();
            m_isStreaming = true;
            m_wasSilent = true;
            onStreamStarted?.Invoke();
        }

        private void Update()
        {
            if (!m_isStreaming || !audioSource.clip) return;

            var samples = new float[sampleSize];
            var micPosition = Microphone.GetPosition(m_micName);

            if (micPosition < sampleSize) return;

            audioSource.clip.GetData(samples, micPosition - sampleSize);

            var level = samples.Sum(Mathf.Abs);
            level /= sampleSize;

            if (level > silenceThreshold)
            {
                if (!m_wasSilent) return;
                
                var audioWaveform = new AudioWaveform
                {
                    samples = samples,
                    timestamp = Time.time
                };
                
                onAudioDetected?.Invoke(audioWaveform);
                m_wasSilent = false;
            }
            else
            {
                if (m_wasSilent) return;
            
                onSilenceDetected?.Invoke();
                m_wasSilent = true;
            }
        }     
    }
}