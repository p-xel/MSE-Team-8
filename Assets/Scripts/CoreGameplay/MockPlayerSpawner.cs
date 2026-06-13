using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class MockPlayerSpawner : SimulationBehaviour
{
    public GameObject PlayerPrefab;

    [Tooltip("The index used to determine which spawn point to use for the mock player")]
    public int currentSpawnIndex = 1;

    private bool _pendingBotsSpawned;

    private NetworkRunner ActiveRunner
    {
        get
        {
            if (Runner != null) return Runner;
            foreach (var r in NetworkRunner.Instances)
                if (r != null && r.IsRunning) return r;
            return null;
        }
    }

    private void Update()
    {
        var runner = ActiveRunner;
        if (runner == null || !runner.IsRunning) return;

        // Check if there are bots queued to be spawned from the matchmaking session, and spawn them.
        if (!_pendingBotsSpawned && Session.PendingBotCount > 0)
        {
            int count = Session.PendingBotCount;
            Session.PendingBotCount = 0;
            _pendingBotsSpawned = true;
            for (int i = 0; i < count; i++)
            {
                SpawnMockPlayer();
            }
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            SpawnMockPlayer();
        }
    }

    public void SpawnMockPlayer()
    {
        var runner = ActiveRunner;
        if (runner == null) return;

        bool canSpawn = (runner.GameMode == GameMode.Shared) || runner.IsServer;

        if (!canSpawn)
        {
            Debug.LogWarning("You do not have authority to spawn mock players.");
            return;
        }

        if (PlayerPrefab == null)
        {
            Debug.LogError("PlayerPrefab is not assigned in MockPlayerSpawner!");
            return;
        }

        Vector3 spawnPos = GetSpawnPosition(currentSpawnIndex, out Quaternion spawnRot);

        runner.Spawn(PlayerPrefab, spawnPos, spawnRot, PlayerRef.None);

        Debug.Log($"Spawned mock player at spawn index {currentSpawnIndex}");
        currentSpawnIndex++;
    }

    private Vector3 GetSpawnPosition(int index, out Quaternion rotation)
    {
        rotation = Quaternion.identity;
        int sIndex = index % 4;
        string pName = $"P{sIndex + 1}";

        // Lookup: search for "P1", "P2"..., directly or under a parent "Spawn" folder.
        GameObject pObj = GameObject.Find(pName);

        if (pObj == null)
        {
            GameObject spawnParent = GameObject.Find("Spawn");
            if (spawnParent != null)
            {
                Transform pChild = spawnParent.transform.Find(pName);
                if (pChild != null)
                {
                    pObj = pChild.gameObject;
                }
            }
        }

        if (pObj != null)
        {
            Transform spawnChild = pObj.transform.Find("Spawn");
            if (spawnChild != null)
            {
                rotation = spawnChild.rotation;
                return spawnChild.position;
            }

            rotation = pObj.transform.rotation;
            return pObj.transform.position;
        }

        return new Vector3(0, 1, 0);
    }
}
