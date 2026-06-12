using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class MockPlayerSpawner : SimulationBehaviour
{
    public GameObject PlayerPrefab;
    
    [Tooltip("The index used to determine which spawn point to use for the mock player")]
    public int currentSpawnIndex = 1; 

    private void Update()
    {
        if (Runner == null || !Runner.IsRunning || !Runner.GetVisible()) return;

        if (Input.GetKeyDown(KeyCode.M))
        {
            SpawnMockPlayer();
        }
    }

    public void SpawnMockPlayer()
    {
        bool canSpawn = (Runner.GameMode == GameMode.Shared) || Runner.IsServer;
        
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
        
        Runner.Spawn(PlayerPrefab, spawnPos, spawnRot, PlayerRef.None);
        
        Debug.Log($"Spawned mock player at spawn index {currentSpawnIndex}");
        currentSpawnIndex++;
    }

    private Vector3 GetSpawnPosition(int index, out Quaternion rotation)
    {
        rotation = Quaternion.identity;
        int sIndex = index % 4; 
        string pName = $"P{sIndex + 1}"; 

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
