using UnityEngine;
using Fusion;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    public GameObject PlayerPrefab;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RepositionLocalPlayer();
    }

    void IPlayerJoined.PlayerJoined(PlayerRef player)
    {
        bool shouldSpawn = false;
        if (Runner.GameMode == GameMode.Shared)
        {
            shouldSpawn = (player == Runner.LocalPlayer);
        }
        else
        {
            shouldSpawn = Runner.IsServer;
        }

        if (shouldSpawn)
        {
            if (PlayerPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] Cannot spawn player: PlayerPrefab is null!");
                return;
            }

            int playerIndex = GetPlayerIndex(player);
            Vector3 spawnPos = GetSpawnPosition(playerIndex, out Quaternion spawnRot);
            Runner.Spawn(PlayerPrefab, spawnPos, spawnRot, player);
        }

        if (player == Runner.LocalPlayer)
        {
            OrbitCamera orbitCam = FindAnyObjectByType<OrbitCamera>();
            if (orbitCam != null)
            {
                orbitCam.enabled = false;
            }
        }
    }

    void IPlayerLeft.PlayerLeft(PlayerRef player)
    {
        PlayerHand[] allHands = FindObjectsByType<PlayerHand>();
        foreach (PlayerHand hand in allHands)
        {
            if (hand.Object != null && hand.Object.IsValid && hand.Object.StateAuthority == player)
            {
                Runner.Despawn(hand.Object);
                break;
            }
        }

        PlayerMovement[] allMovements = FindObjectsByType<PlayerMovement>();
        foreach (PlayerMovement pm in allMovements)
        {
            if (pm.Object != null && pm.Object.IsValid && pm.Object.StateAuthority == player)
            {
                Runner.Despawn(pm.Object);
                break;
            }
        }
    }

    private int GetPlayerIndex(PlayerRef player)
    {
        var activePlayers = new List<PlayerRef>(Runner.ActivePlayers);
        if (!activePlayers.Contains(player))
        {
            activePlayers.Add(player);
        }
        activePlayers.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));
        return activePlayers.IndexOf(player);
    }

    private Vector3 GetSpawnPosition(int playerIndex, out Quaternion rotation)
    {
        rotation = Quaternion.identity;
        int spawnIndex = playerIndex % 4;
        string pName = $"P{spawnIndex + 1}";

        GameObject pObj = FindGameObjectInScene(pName);
        if (pObj != null)
        {
            Transform spawnChild = pObj.transform.Find("Spawn");
            if (spawnChild != null)
            {
                rotation = spawnChild.rotation;
                return spawnChild.position;
            }
        }

        GameObject spawnParent = FindGameObjectInScene("Spawn");
        if (spawnParent != null)
        {
            Transform pChild = spawnParent.transform.Find(pName);
            if (pChild != null)
            {
                rotation = pChild.rotation;
                return pChild.position;
            }
        }

        if (pObj != null)
        {
            rotation = pObj.transform.rotation;
            return pObj.transform.position;
        }

        return new Vector3(0, 1, 0);
    }

    private void RepositionLocalPlayer()
    {
        if (Runner == null || !Runner.IsRunning) return;

        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>();
        foreach (PlayerMovement playerMovement in allPlayers)
        {
            if (playerMovement.Object != null && playerMovement.Object.HasInputAuthority)
            {
                int playerIndex = GetPlayerIndex(Runner.LocalPlayer);
                if (playerIndex >= 0)
                {
                    Vector3 spawnPos = GetSpawnPosition(playerIndex, out Quaternion spawnRot);
                    playerMovement.transform.position = spawnPos;
                    playerMovement.transform.rotation = spawnRot;
                }
                break;
            }
        }
    }

    private GameObject FindGameObjectInScene(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null) return obj;

        var activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        foreach (GameObject root in rootObjects)
        {
            GameObject result = FindInChildren(root.transform, name);
            if (result != null) return result;
        }

        return null;
    }

    private GameObject FindInChildren(Transform parent, string name)
    {
        if (parent.name == name) return parent.gameObject;
        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject result = FindInChildren(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
