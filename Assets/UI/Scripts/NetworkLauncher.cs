using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkLauncher Instance { get; private set; }

    public const string PlayerNameKeyPrefix = "pn_";

    [SerializeField] string multiplayerSceneName = "MultiplayerScene";
    [SerializeField] int maxPlayers = 4;
    [SerializeField] int botCount = 3;

    public event Action<List<SessionInfo>> SessionsUpdated;
    public IReadOnlyList<SessionInfo> Sessions => _sessions;

    private NetworkRunner _runner;
    private List<SessionInfo> _sessions = new List<SessionInfo>();
    private bool _inLobby;
    private bool _starting;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LaunchSoloVsBots() => Launch(NewId("solo"), GameMode.Single, isPublic: false, bots: botCount, matchId: NewMatchId());

    public void CreateRoom() => Launch(NewId("room"), GameMode.Shared, isPublic: true, bots: 0, matchId: NewMatchId());

    public void JoinRoom(string sessionName) => Launch(sessionName, GameMode.Shared, isPublic: true, bots: 0, matchId: null);

    public async void JoinLobby()
    {
        if (_inLobby || _starting) return;
        _inLobby = true;
        var result = await GetRunner().JoinSessionLobby(SessionLobby.Shared);
        if (!result.Ok)
        {
            _inLobby = false;
            Debug.LogError($"[NetworkLauncher] JoinLobby failed: {result.ShutdownReason}");
        }
    }

    private async void Launch(string sessionName, GameMode mode, bool isPublic, int bots, string matchId)
    {
        if (_starting) return;
        _starting = true;
        _inLobby = false;
        Session.PendingBotCount = bots;
        Session.MatchId = matchId;

        int index = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{multiplayerSceneName}.unity");
        if (index < 0)
        {
            Debug.LogError($"[NetworkLauncher] Scene '{multiplayerSceneName}' is not in Build Settings.");
            _starting = false;
            return;
        }

        var runner = GetRunner();
        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(index), LoadSceneMode.Single);

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = maxPlayers,
            Scene = sceneInfo,
            SceneManager = runner.GetComponent<INetworkSceneManager>(),
            ObjectProvider = runner.GetComponent<INetworkObjectProvider>(),
        });

        _starting = false;

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkLauncher] StartGame '{sessionName}' failed: {result.ShutdownReason}");
            Session.PendingBotCount = 0;
            return;
        }

        if (runner.SessionInfo != null && runner.SessionInfo.IsValid)
        {
            runner.SessionInfo.IsVisible = isPublic;
            PublishLocalPlayerName(runner);
        }
    }

    private void PublishLocalPlayerName(NetworkRunner runner)
    {
        string name = string.IsNullOrEmpty(Session.Username)
            ? $"Player {runner.LocalPlayer.PlayerId}"
            : Session.Username;
        runner.SessionInfo.UpdateCustomProperties(new Dictionary<string, SessionProperty>
        {
            { PlayerNameKeyPrefix + runner.LocalPlayer.PlayerId, name }
        });
    }

    private NetworkRunner GetRunner()
    {
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            gameObject.AddComponent<NetworkSceneManagerDefault>();
            gameObject.AddComponent<NetworkObjectProviderDefault>();
            _runner.AddCallbacks(this);
        }
        return _runner;
    }

    private static string NewId(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);

    private static string NewMatchId() => Guid.NewGuid().ToString();

    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _sessions = sessionList ?? new List<SessionInfo>();
        SessionsUpdated?.Invoke(_sessions);
    }

    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
}
