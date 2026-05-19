using System;
using System.Collections.Generic;
using ArenasParameters;
using UnityEngine;

namespace AIB
{
    public class ModelSelector : MonoBehaviour
    {
        [Header("Agent Model Prefabs")]
        [SerializeField] private GameObject abeVisualMeshPrefab;
        [SerializeField] private GameObject babyAbbyVisualMeshPrefab;

        [Header("Mother Model Prefabs")]
        [SerializeField] private GameObject motherVisualMeshPrefab;
        [SerializeField] private GameObject motherAbbyVisualMeshPrefab;

        [Header("Spawn Parents")]
        [SerializeField] private Transform agentParent;
        [SerializeField] private Transform motherParent;

        private readonly Dictionary<string, GameObject> _agentModelPrefabs =
            new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GameObject> _motherModelPrefabs =
            new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        private GameObject _spawnedAgentModel;
        private GameObject _spawnedMotherModel;

        private const string DefaultAgentModel = "abe";
        private const string DefaultMotherModel = "mother";

        private void Awake()
        {
            BuildLookups();
        }

        public void ApplyConfiguration(ArenaConfiguration configuration)
        {
            BuildLookups();

            string agentKey = string.IsNullOrWhiteSpace(configuration?.agentModel)
                ? DefaultAgentModel
                : configuration.agentModel;
            string motherKey = string.IsNullOrWhiteSpace(configuration?.motherModel)
                ? DefaultMotherModel
                : configuration.motherModel;

            SpawnSelectedModel(agentKey, _agentModelPrefabs, ref _spawnedAgentModel, agentParent);
            SpawnSelectedModel(motherKey, _motherModelPrefabs, ref _spawnedMotherModel, motherParent);
        }

        private void BuildLookups()
        {
            _agentModelPrefabs.Clear();
            _motherModelPrefabs.Clear();

            _agentModelPrefabs["abe"] = abeVisualMeshPrefab;
            _agentModelPrefabs["baby_abby"] = babyAbbyVisualMeshPrefab;

            _motherModelPrefabs["mother"] = motherVisualMeshPrefab;
            _motherModelPrefabs["mother_abby"] = motherAbbyVisualMeshPrefab;
        }

        private static void SpawnSelectedModel(
            string key,
            Dictionary<string, GameObject> lookup,
            ref GameObject spawnedInstance,
            Transform parent
        )
        {
            if (lookup == null || lookup.Count == 0 || parent == null)
            {
                return;
            }

            if (!lookup.TryGetValue(key, out GameObject selectedPrefab) || selectedPrefab == null)
            {
                return;
            }

            if (spawnedInstance != null)
            {
                Destroy(spawnedInstance);
            }

            spawnedInstance = Instantiate(selectedPrefab, parent);
            spawnedInstance.transform.localPosition = Vector3.zero;
            spawnedInstance.transform.localRotation = Quaternion.identity;
        }
    }
}
