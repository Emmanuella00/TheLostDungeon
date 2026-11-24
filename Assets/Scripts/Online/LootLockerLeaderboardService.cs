using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TheLostDungeon.Online
{
    /// <summary>
    /// REST API INTEGRATION with the LootLocker game backend, using UnityWebRequest + JSON.
    ///
    ///   POST {base}/game/v2/session/guest                 -> guest login, returns a session token
    ///   POST {base}/game/leaderboards/{key}/submit         -> submit the player's score
    ///   GET  {base}/game/leaderboards/{key}/list?count=N   -> top N scores
    ///
    /// Every call after login sends the token in the "x-session-token" header. The guest's
    /// player_identifier is stored, so the same device stays the same player between sessions.
    ///
    /// Error handling: timeouts, no connection, HTTP errors and bad JSON all come back as a
    /// ServiceResult with a readable message instead of exceptions; an expired session (401) is
    /// renewed once automatically.
    /// </summary>
    public class LootLockerLeaderboardService : ILeaderboardService
    {
        private const string PlayerIdentifierKey = "leaderboard.playerIdentifier";

        private readonly LeaderboardConfig config;
        private string sessionToken;
        private string memberId;

        public LootLockerLeaderboardService(LeaderboardConfig config)
        {
            this.config = config;
        }

        public bool IsConfigured => config != null && config.IsConfigured;
        public bool IsConnected => !string.IsNullOrEmpty(sessionToken);

        // ------------------------------------------------------------------ JSON shapes

        [Serializable] private class SessionRequest { public string game_key; public string game_version; public string player_identifier; }
        [Serializable] private class SessionResponse { public bool success; public string session_token; public string player_identifier; public int player_id; public string public_uid; }
        [Serializable] private class SubmitRequest { public string member_id; public int score; public string metadata; }
        [Serializable] private class ListResponse { public bool success; public ListItem[] items; }
        [Serializable] private class ListItem { public string member_id; public int rank; public int score; public string metadata; public PlayerInfo player; }
        [Serializable] private class PlayerInfo { public int id; public string name; public string public_uid; }

        /// <summary>Extra data stored with each score (the leaderboard must have Metadata enabled).</summary>
        [Serializable] public class ScoreMetadata { public string name; public int levels; public int stars; public long ticks; }

        // ------------------------------------------------------------------ API

        public IEnumerator Connect(Action<ServiceResult<bool>> done)
        {
            if (!IsConfigured) { done(ServiceResult<bool>.Fail("No LootLocker game key set.")); yield break; }
            if (IsConnected) { done(ServiceResult<bool>.Ok(true)); yield break; }

            var body = new SessionRequest
            {
                game_key = config.gameKey.Trim(),
                game_version = config.gameVersion,
                player_identifier = PlayerPrefs.GetString(PlayerIdentifierKey, "")
            };

            string response = null, error = null;
            yield return Send("POST", "/game/v2/session/guest", JsonUtility.ToJson(body), false, (r, e) => { response = r; error = e; });
            if (error != null) { done(ServiceResult<bool>.Fail(error)); yield break; }

            SessionResponse session = TryParse<SessionResponse>(response);
            if (session == null || string.IsNullOrEmpty(session.session_token))
            {
                done(ServiceResult<bool>.Fail("Login failed: unexpected response from the server."));
                yield break;
            }

            sessionToken = session.session_token;
            memberId = session.player_id.ToString();   // LootLocker lists player leaderboards by player id
            if (!string.IsNullOrEmpty(session.player_identifier))
            {
                PlayerPrefs.SetString(PlayerIdentifierKey, session.player_identifier);
                PlayerPrefs.Save();
            }
            done(ServiceResult<bool>.Ok(true));
        }

        public IEnumerator SubmitScore(LeaderboardEntry entry, Action<ServiceResult<bool>> done)
        {
            var meta = new ScoreMetadata { name = entry.name, levels = entry.levels, stars = entry.stars, ticks = entry.submittedUtcTicks };
            var body = new SubmitRequest { member_id = memberId ?? "", score = entry.score, metadata = JsonUtility.ToJson(meta) };

            string error = null;
            yield return SendAuthenticated("POST", $"/game/leaderboards/{Uri.EscapeDataString(config.leaderboardKey)}/submit",
                                           JsonUtility.ToJson(body), (r, e) => error = e);
            done(error == null ? ServiceResult<bool>.Ok(true) : ServiceResult<bool>.Fail(error));
        }

        public IEnumerator GetTopScores(int count, Action<ServiceResult<List<LeaderboardEntry>>> done)
        {
            string response = null, error = null;
            yield return SendAuthenticated("GET", $"/game/leaderboards/{Uri.EscapeDataString(config.leaderboardKey)}/list?count={count}",
                                           null, (r, e) => { response = r; error = e; });
            if (error != null) { done(ServiceResult<List<LeaderboardEntry>>.Fail(error)); yield break; }

            List<LeaderboardEntry> entries = ParseList(response);
            if (entries == null) { done(ServiceResult<List<LeaderboardEntry>>.Fail("Couldn't read the leaderboard data.")); yield break; }

            foreach (LeaderboardEntry e in entries) e.isLocalPlayer = !string.IsNullOrEmpty(memberId) && e.memberId == memberId;
            done(ServiceResult<List<LeaderboardEntry>>.Ok(entries));
        }

        /// <summary>Turns LootLocker's list JSON into entries (public static so it can be unit tested).</summary>
        public static List<LeaderboardEntry> ParseList(string json)
        {
            ListResponse list = TryParse<ListResponse>(json);
            if (list == null) return null;

            var entries = new List<LeaderboardEntry>();
            if (list.items == null) return entries;

            foreach (ListItem item in list.items)
            {
                ScoreMetadata meta = string.IsNullOrEmpty(item.metadata) ? null : TryParse<ScoreMetadata>(item.metadata);
                string name = meta != null && !string.IsNullOrEmpty(meta.name) ? meta.name
                            : item.player != null && !string.IsNullOrEmpty(item.player.name) ? item.player.name
                            : "Player " + item.member_id;

                entries.Add(new LeaderboardEntry
                {
                    rank = item.rank,
                    memberId = item.member_id,
                    name = name,
                    score = item.score,
                    levels = meta != null ? meta.levels : 0,
                    stars = meta != null ? meta.stars : 0,
                    submittedUtcTicks = meta != null ? meta.ticks : 0
                });
            }
            return entries;
        }

        // ------------------------------------------------------------------ HTTP

        private IEnumerator SendAuthenticated(string method, string path, string json, Action<string, string> done)
        {
            if (!IsConnected)
            {
                string loginError = null;
                yield return Connect(r => loginError = r.Success ? null : r.Error);
                if (loginError != null) { done(null, loginError); yield break; }
            }

            bool expired = false;
            string response = null, error = null;
            yield return Send(method, path, json, true, (r, e) => { response = r; error = e; expired = e != null && e.Contains("401"); });

            if (expired)
            {
                // Session timed out: log in again once and retry.
                sessionToken = null;
                string loginError = null;
                yield return Connect(r => loginError = r.Success ? null : r.Error);
                if (loginError != null) { done(null, loginError); yield break; }
                yield return Send(method, path, json, true, (r, e) => { response = r; error = e; });
            }
            done(response, error);
        }

        private IEnumerator Send(string method, string path, string json, bool authenticated, Action<string, string> done)
        {
            using (var request = new UnityWebRequest(config.baseUrl.TrimEnd('/') + path, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                if (authenticated) request.SetRequestHeader("x-session-token", sessionToken);
                request.timeout = config.timeoutSeconds;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    done(request.downloadHandler.text, null);
                }
                else
                {
                    done(null, Describe(request));
                }
            }
        }

        private static string Describe(UnityWebRequest request)
        {
            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    return request.error != null && request.error.ToLowerInvariant().Contains("timeout")
                        ? "The server took too long to answer (timeout)."
                        : "No internet connection (or the server is unreachable).";
                case UnityWebRequest.Result.ProtocolError:
                    return $"Server error {request.responseCode}: {ShortBody(request.downloadHandler?.text)}";
                case UnityWebRequest.Result.DataProcessingError:
                    return "The response couldn't be read.";
                default:
                    return request.error ?? "Unknown network error.";
            }
        }

        private static string ShortBody(string body)
        {
            if (string.IsNullOrEmpty(body)) return "";
            return body.Length > 160 ? body.Substring(0, 160) + "..." : body;
        }

        private static T TryParse<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<T>(json); }
            catch (Exception) { return null; }
        }
    }
}
