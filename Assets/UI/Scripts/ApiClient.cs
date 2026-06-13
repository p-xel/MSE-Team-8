using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }

    [SerializeField] string baseUrl = "https://project31-unity-test.cleverapps.io/api";

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

    public void Login(string username, string password, Action<AuthResponse> onSuccess, Action<string> onError)
    {
        var body = JsonUtility.ToJson(new LoginRequest { username = username, password = password });
        StartCoroutine(Send("POST", "/auth/login", body, false, onSuccess, onError));
    }

    public void Register(string username, string password, Action<AuthResponse> onSuccess, Action<string> onError)
    {
        var body = JsonUtility.ToJson(new RegisterRequest { username = username, password = password });
        StartCoroutine(Send("POST", "/auth/register", body, false, onSuccess, onError));
    }

    public void GetMe(Action<AccountResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(Send("GET", "/me", null, true, onSuccess, onError));
    }

    public void GetStats(Action<AccountStatsResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(Send("GET", "/me/stats", null, true, onSuccess, onError));
    }

    public void GetMatches(Action<List<MatchRecordResponse>> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendList("GET", "/me/matches", null, true, onSuccess, onError));
    }

    public void GetLeaderboard(Action<List<LeaderboardEntryResponse>> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendList("GET", "/leaderboard", null, false, onSuccess, onError));
    }

    public void SubmitMatch(MatchSubmissionRequest request, Action<AccountStatsResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(Send("POST", "/me/matches", JsonUtility.ToJson(request), true, onSuccess, onError));
    }

    public void UpdateSelectedCharacter(string character, Action<AccountResponse> onSuccess, Action<string> onError)
    {
        var body = JsonUtility.ToJson(new UpdateCharacterRequest { selectedCharacter = character });
        StartCoroutine(Send("PATCH", "/me/selected-character", body, true, onSuccess, onError));
    }

    IEnumerator Send<T>(string method, string path, string body, bool auth, Action<T> onSuccess, Action<string> onError)
    {
        using (var request = BuildRequest(method, path, body, auth))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(Describe(request));
                yield break;
            }
            var text = request.downloadHandler.text;
            T result = string.IsNullOrEmpty(text) ? default : JsonUtility.FromJson<T>(text);
            onSuccess?.Invoke(result);
        }
    }

    IEnumerator SendList<T>(string method, string path, string body, bool auth, Action<List<T>> onSuccess, Action<string> onError)
    {
        using (var request = BuildRequest(method, path, body, auth))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(Describe(request));
                yield break;
            }
            var wrapped = "{\"items\":" + request.downloadHandler.text + "}";
            var result = JsonUtility.FromJson<ListWrapper<T>>(wrapped);
            onSuccess?.Invoke(result != null && result.items != null ? result.items : new List<T>());
        }
    }

    UnityWebRequest BuildRequest(string method, string path, string body, bool auth)
    {
        var request = new UnityWebRequest(baseUrl + path, method);
        request.downloadHandler = new DownloadHandlerBuffer();
        if (body != null)
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.SetRequestHeader("Content-Type", "application/json");
        }
        if (auth && !string.IsNullOrEmpty(Session.Token))
            request.SetRequestHeader("Authorization", "Bearer " + Session.Token);
        return request;
    }

    static string Describe(UnityWebRequest request)
    {
        var text = request.downloadHandler != null ? request.downloadHandler.text : null;
        return string.IsNullOrEmpty(text) ? request.error : text;
    }
}
