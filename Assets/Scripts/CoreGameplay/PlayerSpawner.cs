using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    public GameObject PlayerPrefab;

    private bool _spawnCatchupDone;

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

    private void Update()
    {
        if (_spawnCatchupDone) return;

        var runner = ActiveRunner;
        if (runner == null || !runner.IsRunning) return;

        bool sessionHasPlayers = false;
        foreach (PlayerRef player in runner.ActivePlayers)
        {
            sessionHasPlayers = true;
            TrySpawnFor(runner, player);
        }

        if (sessionHasPlayers)
        {
            DisableOrbitForLocal();
            _spawnCatchupDone = true;
        }
    }

    void IPlayerJoined.PlayerJoined(PlayerRef player)
    {
        var runner = ActiveRunner;
        if (runner == null) return;
        TrySpawnFor(runner, player);

        if (player == runner.LocalPlayer)
            DisableOrbitForLocal();
    }

    private void TrySpawnFor(NetworkRunner runner, PlayerRef player)
    {
        bool shouldSpawn = runner.GameMode == GameMode.Shared
            ? (player == runner.LocalPlayer)
            : runner.IsServer;
        if (!shouldSpawn) return;

        if (PlayerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Cannot spawn player: PlayerPrefab is null!");
            return;
        }

        if (IsPlayerSpawned(player)) return;

        int playerIndex = GetPlayerIndex(player);
        Vector3 spawnPos = GetSpawnPosition(playerIndex, out Quaternion spawnRot);
        Debug.Log($"[Seat] {player} id={player.PlayerId} seat={playerIndex} pos={spawnPos} session={runner.SessionInfo?.Name} local={runner.LocalPlayer.PlayerId}");
        runner.Spawn(PlayerPrefab, spawnPos, spawnRot, player);
    }

    private bool IsPlayerSpawned(PlayerRef player)
    {
        PlayerMovement[] allMovements = FindObjectsByType<PlayerMovement>();
        foreach (PlayerMovement pm in allMovements)
        {
            if (pm.Object != null && pm.Object.IsValid && pm.Object.InputAuthority == player)
                return true;
        }
        return false;
    }

    private void DisableOrbitForLocal()
    {
        OrbitCamera orbitCam = FindAnyObjectByType<OrbitCamera>();
        if (orbitCam != null)
            orbitCam.enabled = false;
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
        return Mathf.Max(0, player.PlayerId - 1);
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
        var runner = ActiveRunner;
        if (runner == null || !runner.IsRunning) return;

        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>();
        foreach (PlayerMovement playerMovement in allPlayers)
        {
            if (playerMovement.Object != null && playerMovement.Object.HasInputAuthority)
            {
                int playerIndex = GetPlayerIndex(runner.LocalPlayer);
                if (playerIndex >= 0)
                {
                    Vector3 spawnPos = GetSpawnPosition(playerIndex, out Quaternion spawnRot);
                    NetworkTransform nt = playerMovement.GetComponent<NetworkTransform>();
                    if (nt != null)
                    {
                        nt.Teleport(spawnPos, spawnRot);
                    }
                    else
                    {
                        playerMovement.transform.position = spawnPos;
                        playerMovement.transform.rotation = spawnRot;
                    }
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
