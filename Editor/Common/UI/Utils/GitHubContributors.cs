using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Loads the repository's GitHub contributors and their avatars for the Preferences page.
    /// The list is cached in SessionState to stay under the unauthenticated API limit
    /// (60 requests/hour per IP). Failures are silent and retried on the next page open.
    /// </summary>
    internal static class GitHubContributors
    {
        [Serializable]
        internal sealed class Contributor
        {
            public string login;
            public long id;
            public string type;
        }

        [Serializable]
        private sealed class Payload
        {
            public Contributor[] items;
        }

        private const string ApiUrl =
            "https://api.github.com/repos/"
            + GitHubSection.Repository
            + "/contributors?per_page=100";
        private const string CacheKey = AvatarCompressorPreferences.PrefsPrefix + "contributors";
        private const int TimeoutSeconds = 15;

        // Twice the 32pt grid cell so avatars stay sharp on HiDPI displays.
        private const int AvatarPixels = 64;

        // \z, not $: $ also matches before a trailing newline.
        private static readonly Regex LoginPattern = new Regex(@"^[A-Za-z0-9][A-Za-z0-9-]{0,38}\z");
        private static readonly Dictionary<long, Texture2D> Avatars =
            new Dictionary<long, Texture2D>();
        private static readonly List<(
            UnityWebRequest Request,
            Action<UnityWebRequest> OnDone
        )> Pending = new List<(UnityWebRequest, Action<UnityWebRequest>)>();

        internal static event Action Changed;

        internal static bool IsLoading { get; private set; }

        internal static IReadOnlyList<Contributor> Contributors { get; private set; } =
            Array.Empty<Contributor>();

        static GitHubContributors()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Release;
        }

        internal static void EnsureLoaded()
        {
            // Batch test runs draw Preferences too and must stay off the network.
            if (Application.isBatchMode || IsLoading || Contributors.Count > 0)
                return;

            var cached = Parse(SessionState.GetString(CacheKey, string.Empty));
            if (cached.Length > 0)
            {
                Complete(cached);
                return;
            }

            var request = UnityWebRequest.Get(ApiUrl);
            request.timeout = TimeoutSeconds;
            request.SetRequestHeader("Accept", "application/vnd.github+json");
            request.SetRequestHeader("User-Agent", "avatar-compressor");
            Send(request, OnListReceived);
            IsLoading = true;
        }

        internal static Texture2D GetAvatar(long id)
        {
            return Avatars.TryGetValue(id, out var texture) ? texture : null;
        }

        // From the Parse-validated login, never the payload's html_url.
        internal static string ProfileUrl(string login)
        {
            return "https://github.com/" + login;
        }

        internal static Contributor[] Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<Contributor>();

            Payload payload;
            try
            {
                // JsonUtility cannot read a top-level array.
                payload = JsonUtility.FromJson<Payload>("{\"items\":" + json + "}");
            }
            catch (ArgumentException)
            {
                return Array.Empty<Contributor>();
            }

            return payload?.items == null
                ? Array.Empty<Contributor>()
                : payload
                    .items.Where(c =>
                        c != null
                        && c.type == "User"
                        && c.id > 0
                        && c.login != null
                        && LoginPattern.IsMatch(c.login)
                    )
                    .ToArray();
        }

        private static void OnListReceived(UnityWebRequest request)
        {
            IsLoading = false;
            string body =
                request.result == UnityWebRequest.Result.Success
                    ? request.downloadHandler.text
                    : null;
            var contributors = Parse(body);
            if (contributors.Length == 0)
            {
                Changed?.Invoke();
                return;
            }

            SessionState.SetString(CacheKey, body);
            Complete(contributors);
        }

        private static void Complete(Contributor[] contributors)
        {
            Contributors = contributors;
            foreach (var contributor in contributors)
            {
                long id = contributor.id;
                var request = UnityWebRequestTexture.GetTexture(
                    $"https://avatars.githubusercontent.com/u/{id}?s={AvatarPixels}",
                    true
                );
                request.timeout = TimeoutSeconds;
                Send(request, response => OnAvatarReceived(id, response));
            }
            Changed?.Invoke();
        }

        private static void OnAvatarReceived(long id, UnityWebRequest request)
        {
            if (request.result != UnityWebRequest.Result.Success)
                return;

            Texture2D texture;
            try
            {
                texture = DownloadHandlerTexture.GetContent(request);
            }
            catch (Exception)
            {
                return;
            }
            if (texture == null)
                return;

            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            Avatars[id] = texture;
            Changed?.Invoke();
        }

        // Polled: AsyncOperation.completed is not relied on outside play mode.
        private static void Send(UnityWebRequest request, Action<UnityWebRequest> onDone)
        {
            request.SendWebRequest();
            Pending.Add((request, onDone));
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            // Collect before calling back: the list callback queues avatar requests.
            List<(UnityWebRequest Request, Action<UnityWebRequest> OnDone)> finished = null;
            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                if (!Pending[i].Request.isDone)
                    continue;
                (finished ??= new List<(UnityWebRequest, Action<UnityWebRequest>)>()).Add(
                    Pending[i]
                );
                Pending.RemoveAt(i);
            }
            if (Pending.Count == 0)
                EditorApplication.update -= Poll;
            if (finished == null)
                return;

            // These already left Pending, so one throwing callback must not strand the rest.
            foreach (var (request, onDone) in finished)
            {
                try
                {
                    onDone(request);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    request.Dispose();
                }
            }
        }

        private static void Release()
        {
            EditorApplication.update -= Poll;
            foreach (var (request, _) in Pending)
                request.Dispose();
            Pending.Clear();

            foreach (var texture in Avatars.Values)
            {
                if (texture != null)
                    Object.DestroyImmediate(texture);
            }
            Avatars.Clear();
        }
    }
}
