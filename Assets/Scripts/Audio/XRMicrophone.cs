using UnityEngine;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class XRMicrophone: MonoBehaviour
    {
        private AudioSource m_audioSource;

        private void Start()
        {
            m_audioSource = GetComponent<AudioSource>();
            m_audioSource.mute = true;
            m_audioSource.loop = true;
            m_audioSource.playOnAwake = false;
        }
    }
}