using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Rythm
{
    public class BeatmapManager : MonoBehaviour
    {
        [SerializeField] public AudioSource fullSongAudio;
        [SerializeField] private float noteSpeed = 2.0f;

        [Tooltip("Offset added to every note's time to compensate for latency.")]
        [SerializeField] private float globalNoteTimeOffset = 0.05f;

        public GameObject kickPrefab, snarePrefab, tomPrefab, cymbalPrefab;
        public Transform kickSpawn, snareSpawn, tomSpawn, cymbalSpawn;
        public Transform kickHitTarget, snareHitTarget, tomHitTarget, cymbalHitTarget;

        private Dictionary<string, Queue<BeatNote>> m_beatQueues;
        private float m_leadTime;
        private bool m_isPlaying;

        public void StartSong()
        {
            // Calculate lead time based on distance and speed (use kick lane for reference)
            m_leadTime = Vector3.Distance(kickSpawn.position, kickHitTarget.position) / noteSpeed;

            m_beatQueues = new Dictionary<string, Queue<BeatNote>>
            {
                { "kick", LoadBeatmap("beatmaps/kick") },
                { "snare", LoadBeatmap("beatmaps/snare") },
                //{ "tom", LoadBeatmap("beatmaps/tom") },
                //{ "cymbal", LoadBeatmap("beatmaps/cymbal") }
            };

            fullSongAudio.Play();
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

        private void SpawnNote(string type, Transform spawnPoint, float targetTime)
        {
            var prefab = GetPrefab(type);
            if (!prefab || !spawnPoint) return;

            var note = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            var mover = note.GetComponent<NoteMover>();
            mover.Initialize(targetTime, fullSongAudio, GetTargetPosition(type));
        }

        private GameObject GetPrefab(string type)
        {
            return type switch
            {
                "kick" => kickPrefab,
                "snare" => snarePrefab,
                "tom" => tomPrefab,
                "cymbal" => cymbalPrefab,
                _ => null
            };
        }

        private Transform GetSpawnPoint(string type)
        {
            return type switch
            {
                "kick" => kickSpawn,
                "snare" => snareSpawn,
                "tom" => tomSpawn,
                "cymbal" => cymbalSpawn,
                _ => null
            };
        }

        private Vector3 GetTargetPosition(string type)
        {
            return type switch
            {
                "kick" => kickHitTarget.position,
                "snare" => snareHitTarget.position,
                "tom" => tomHitTarget.position,
                "cymbal" => cymbalHitTarget.position,
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

