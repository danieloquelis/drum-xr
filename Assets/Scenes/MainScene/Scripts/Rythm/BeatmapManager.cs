using System.Collections.Generic;
using Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Rythm
{
    public class BeatmapManager : MonoBehaviour
    {
        [SerializeField] public AudioSource fullSongAudio;
        [SerializeField] private float noteSpeed = 2.0f;

        [Tooltip("Offset added to every note's time to compensate for latency.")]
        [SerializeField] private float globalNoteTimeOffset = 0.05f;

        public List<GameObject> notePrefabs;
        public List<Transform> noteSpawnPoints;
        public List<NoteTarget> noteTargets;

        public UnityEvent<DrumPadType> onNoteHitEventReceived;
        
        private Dictionary<DrumPadType, Queue<BeatNote>> m_beatQueues;
        private float m_leadTime;
        private bool m_isPlaying;

        private void Start()
        {
            Assert.IsNotNull(noteTargets, "Note targets have not been set");
            
            foreach (var noteTarget in noteTargets)
            {
                noteTarget.onNoteHit.AddListener(OnNoteHit);    
            }
        }

        private void OnNoteHit(DrumPadType noteType)
        {
            onNoteHitEventReceived?.Invoke(noteType);
        }

        private void Update()
        {
            if (!fullSongAudio.isPlaying) return;
            
            var currentTime = fullSongAudio.time;

            foreach (var pair in m_beatQueues)
            {
                var type = pair.Key;
                var queue = pair.Value;
                var spawn = GetSpawnPoint(type);

                while (queue.Count > 0 && queue.Peek().time + globalNoteTimeOffset <= currentTime + m_leadTime)
                {
                    var note = queue.Dequeue();
                    SpawnNote(type, spawn, note.time + globalNoteTimeOffset);
                }
            }
        }

        public void StartSong()
        {
            // Calculate lead time based on distance and speed (use kick lane for reference)
            m_leadTime = Vector3.Distance(noteSpawnPoints[0].position, noteTargets[0].transform.position) / noteSpeed;

            m_beatQueues = new Dictionary<DrumPadType, Queue<BeatNote>>
            {
                { DrumPadType.Kick, LoadBeatmap("beatmaps/kick") },
                { DrumPadType.Snare, LoadBeatmap("beatmaps/snare") },
                //{ "tom", LoadBeatmap("beatmaps/tom") },
                //{ "cymbal", LoadBeatmap("beatmaps/cymbal") }
            };

            fullSongAudio.Play();
        }

        public void StopSong()
        {
            fullSongAudio.Stop();
            m_beatQueues.Clear();
            m_leadTime = 0;
            m_isPlaying = false;
        }
        
        private void SpawnNote(DrumPadType type, Transform spawnPoint, float targetTime)
        {
            var prefab = GetPrefab(type);
            if (!prefab || !spawnPoint) return;

            var note = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            var mover = note.GetComponent<NoteMover>();
            mover.Initialize(targetTime, fullSongAudio, GetTargetPosition(type));
        }

        private GameObject GetPrefab(DrumPadType type)
        {
            return type switch
            {
                DrumPadType.Kick => notePrefabs[0],
                DrumPadType.Snare => notePrefabs[1],
                DrumPadType.Tom => notePrefabs[2],
                DrumPadType.Cymbal => notePrefabs[3],
                _ => null
            };
        }

        private Transform GetSpawnPoint(DrumPadType padType)
        {
            return padType switch
            {
                DrumPadType.Kick => noteSpawnPoints[0],
                DrumPadType.Snare => noteSpawnPoints[1],
                DrumPadType.Tom => noteSpawnPoints[2],
                DrumPadType.Cymbal => noteSpawnPoints[3],
                _ => null
            };
        }

        private Vector3 GetTargetPosition(DrumPadType padType)
        {
            return padType switch
            {
                DrumPadType.Kick => noteTargets[0].transform.position,
                DrumPadType.Snare => noteTargets[1].transform.position,
                DrumPadType.Tom => noteTargets[2].transform.position,
                DrumPadType.Cymbal => noteTargets[3].transform.position,
                _ => Vector3.zero
            };
        }

        private Queue<BeatNote> LoadBeatmap(string path)
        {
            var json = Resources.Load<TextAsset>(path);
            if (!json)
            {
                Debug.LogError($"Beatmap not found at Resources/{path}.json");
                return new Queue<BeatNote>();
            }

            var notes = JsonConvert.DeserializeObject<List<BeatNote>>(json.text);
            return new Queue<BeatNote>(notes);
        }
    }
}

