using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AIB.Runtime
{
    /// <summary>
    /// Maps stage IDs to Unity scenes and behaviour metadata for the nursery binary.
    /// Used by the bootstrap scene to route --nursery-stage CLI args to the correct scene.
    /// Also used by Observer to load the correct stage from replay manifests.
    /// </summary>
    [CreateAssetMenu(fileName = "NurseryStageRegistry", menuName = "AIB/Nursery Stage Registry")]
    public class NurseryStageRegistry : ScriptableObject
    {
        [SerializeField]
        private List<StageEntry> _stages = new List<StageEntry>();

        public IReadOnlyList<StageEntry> Stages => _stages;

        public StageEntry GetStage(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                return null;

            foreach (StageEntry entry in _stages)
            {
                if (string.Equals(entry.stageId, stageId, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            return null;
        }

        public bool TryGetStage(string stageId, out StageEntry entry)
        {
            entry = GetStage(stageId);
            return entry != null;
        }

#if UNITY_EDITOR
        [ContextMenu("Register Crib Stage")]
        private void RegisterCribStage()
        {
            StageEntry crib = new StageEntry
            {
                stageId = "crib_1x1",
                stageVersion = 1,
                sceneName = "SupineCribBodySchema",
                scenePath = "Assets/AIB/Scenes/SupineCribBodySchema.unity",
                behaviorName = "AIBCribBodySchema",
                description = "Supine crib body-schema discovery stage. 14-joint articulated body with motor babble and proprioceptive feedback. No navigation objective."
            };

            Upsert(crib);
            EditorUtility.SetDirty(this);
        }

        private void Upsert(StageEntry entry)
        {
            for (int i = 0; i < _stages.Count; i++)
            {
                if (string.Equals(_stages[i].stageId, entry.stageId, StringComparison.OrdinalIgnoreCase))
                {
                    _stages[i] = entry;
                    return;
                }
            }

            _stages.Add(entry);
        }
#endif
    }

    [Serializable]
    public class StageEntry
    {
        [Tooltip("Unique machine-readable stage identifier, e.g. 'crib_1x1'")]
        public string stageId;

        [Tooltip("Schema version for this stage definition")]
        public int stageVersion = 1;

        [Tooltip("Unity scene name (without path), e.g. 'SupineCribBodySchema'")]
        public string sceneName;

        [Tooltip("Unity scene asset path, e.g. 'Assets/AIB/Scenes/SupineCribBodySchema.unity'")]
        public string scenePath;

        [Tooltip("ML-Agents behaviour name for contract verification, e.g. 'AIBCribBodySchema'")]
        public string behaviorName;

        [Tooltip("Human-readable description of the stage")]
        public string description;
    }
}
