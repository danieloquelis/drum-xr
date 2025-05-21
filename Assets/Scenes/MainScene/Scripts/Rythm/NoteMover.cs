using UnityEngine;

namespace Rythm
{
    public class NoteMover : MonoBehaviour
{
    private float targetTime;
    private AudioSource audioSource;
    private Vector3 targetPosition;
    private bool initialized = false;

    public void Initialize(float time, AudioSource source, Vector3 targetPos)
    {
        targetTime = time;
        audioSource = source;
        targetPosition = targetPos;
        initialized = true;
    }

    void Update()
    {
        if (!initialized || audioSource == null) return;

        float timeLeft = targetTime - audioSource.time;

        if (timeLeft <= 0f)
        {
            transform.position = targetPosition;
            return;
        }

        float step = Vector3.Distance(transform.position, targetPosition) / timeLeft * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("NoteHitZone"))
        {
            Destroy(gameObject);
        }
    }
}

}
