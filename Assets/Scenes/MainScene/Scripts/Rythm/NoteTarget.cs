using Models;
using Rythm;
using UnityEngine;
using UnityEngine.Events;

public class NoteTarget : MonoBehaviour
{
    public DrumPadType type;
    public UnityEvent<DrumPadType> onNoteHit;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out NoteMover note))
        {
            return;
        }
        
        onNoteHit?.Invoke(type);
    }
}
