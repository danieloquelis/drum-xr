using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Rythm
{
    public class BeatmapManager : MonoBehaviour
    {
        public AudioSource fullSongAudio;
        public float noteSpeed = 2.0f; // units per second

        [Tooltip("Offset added to every note's time to compensate for latency.")]
        public float globalNoteTimeOffset = 0.05f;

        public GameObject kickPrefab, snarePrefab, tomPrefab, cymbalPrefab;
        public Transform kickSpawn, snareSpawn, tomSpawn, cymbalSpawn;
        public Transform kickHitTarget, snareHitTarget, tomHitTarget, cymbalHitTarget;

        private Dictionary<string, Queue<BeatNote>> beatQueues;
        private float leadTime;

        void Start()
        {
            // Calculate lead time based on distance and speed (use kick lane for reference)
            leadTime = Vector3.Distance(kickSpawn.position, kickHitTarget.position) / noteSpeed;

            beatQueues = new Dictionary<string, Queue<BeatNote>>
            {
                { "kick", LoadBeatmap("beatmaps/kick") },
                { "snare", LoadBeatmap("beatmaps/snare") },
                //{ "tom", LoadBeatmap("beatmaps/tom") },
                //{ "cymbal", LoadBeatmap("beatmaps/cymbal") }
            };

            fullSongAudio.Play();
        }

        void Update()
        {
            float currentTime = fullSongAudio.time;

            foreach (var pair in beatQueues)
            {
                string type = pair.Key;
                Queue<BeatNote> queue = pair.Value;
                Transform spawn = GetSpawnPoint(type);

                while (queue.Count > 0 && queue.Peek().time + globalNoteTimeOffset <= currentTime + leadTime)
                {
                    var note = queue.Dequeue();
                    SpawnNote(type, spawn, note.time + globalNoteTimeOffset);
                }
            }
        }

        void SpawnNote(string type, Transform spawnPoint, float targetTime)
        {
            GameObject prefab = GetPrefab(type);
            if (prefab == null || spawnPoint == null) return;

            GameObject note = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            NoteMover mover = note.GetComponent<NoteMover>();
            mover.Initialize(targetTime, fullSongAudio, GetTargetPosition(type));
        }

        GameObject GetPrefab(string type)
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

        Transform GetSpawnPoint(string type)
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

        Vector3 GetTargetPosition(string type)
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

        Queue<BeatNote> LoadBeatmap(string path)
        {
            TextAsset json = Resources.Load<TextAsset>(path);
            if (json == null)
            {
                Debug.LogError($"Beatmap not found at Resources/{path}.json");
                return new Queue<BeatNote>();
            }

            var notes = JsonConvert.DeserializeObject<List<BeatNote>>(json.text);
            return new Queue<BeatNote>(notes);
        }
    }
}

