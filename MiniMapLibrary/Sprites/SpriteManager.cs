using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MiniMapLibrary
{
    public sealed class SpriteManager : ISpriteManager
    {
        private sealed class Request
        {
            public readonly string Path;
            public readonly Action<AsyncOperationHandle<Sprite>> Completed;
            public AsyncOperationHandle<Sprite> Handle;
            public Sprite Sprite;
            public bool Finished;

            public Request(SpriteManager owner, string path)
            {
                Path = path;
                Completed = operation => owner.Complete(this, operation);
            }
        }

        private readonly Dictionary<string, Request> requests = new(StringComparer.Ordinal);
        private readonly ILogger logger;
        private readonly bool asynchronous;
        private enum Shape { Diamond, Arrow, Dot }

        private readonly Sprite[] generated = new Sprite[3];
        private bool disposed;

        public int Revision { get; private set; }

        // MiniMapMod loads asynchronously and redraws icons when Revision changes.
        public SpriteManager(ILogger logger) : this(logger, false) { }

        public SpriteManager(ILogger logger, bool asynchronous)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.asynchronous = asynchronous;
        }

        public Sprite GetSprite(InteractableKind type) => type == InteractableKind.none ? null : GetOrCache(Settings.GetSetting(type).IconPath);

        public void PreloadConfiguredIcons()
        {
            GetOrCache(Settings.Icons.Default);
            GetOrCache(Settings.Icons.Arrow);
            foreach (InteractibleSetting setting in Settings.InteractibleSettings.Values)
            {
                if (setting.Enabled)
                    GetOrCache(setting.IconPath);
            }
        }

        public Sprite GetOrCache(string Path)
        {
            string path = Path;
            if (disposed)
                throw new ObjectDisposedException(nameof(SpriteManager));
            if (string.IsNullOrWhiteSpace(path))
                path = Settings.Icons.Default;
            if (path == Settings.Icons.Dot)
                return Generated(Shape.Dot);
            if (requests.TryGetValue(path, out Request request))
                return request.Sprite ? request.Sprite : GetFallback(path);

            request = new Request(this, path);
            requests.Add(path, request);
            try
            {
                // Configs store the old Resources paths; map them to Addressables keys.
                string key = RoR2.LegacyResourcesAPI.GetPathGuidString(path) ?? path;
                request.Handle = Addressables.LoadAssetAsync<Sprite>(key);
                if (!asynchronous && !request.Handle.IsDone)
                    request.Handle.WaitForCompletion();

                if (request.Handle.IsDone)
                    Complete(request, request.Handle);
                else
                    request.Handle.Completed += request.Completed;
            }
            catch (Exception exception)
            {
                request.Finished = true;
                logger.LogException(exception, $"Could not request minimap icon '{path}'. Using a fallback.");
            }
            return request.Sprite ? request.Sprite : GetFallback(path);
        }

        private void Complete(Request request, AsyncOperationHandle<Sprite> operation)
        {
            if (disposed)
                return;
            request.Finished = true;
            if (operation.Status == AsyncOperationStatus.Succeeded && operation.Result)
                request.Sprite = operation.Result;
            else
            {
                logger.LogWarning($"Minimap icon '{request.Path}' was unavailable; using a fallback. This path will not be retried during this session.");
                if (operation.OperationException != null)
                    logger.LogException(operation.OperationException, $"Minimap sprite load failed: {request.Path}");
            }
            Revision++;
        }

        public string DescribeRequests()
        {
            int loaded = 0, pending = 0, failed = 0;
            foreach (Request request in requests.Values)
            {
                if (request.Sprite) loaded++;
                else if (request.Finished) failed++;
                else pending++;
            }
            return $"requests={requests.Count} loaded={loaded} pending={pending} failed={failed} generatedFallback={(bool)generated[(int)Shape.Diamond]} revision={Revision}";
        }

        private Sprite GetFallback(string path)
        {
            if (path == Settings.Icons.Arrow)
                return Generated(Shape.Arrow);
            if (requests.TryGetValue(Settings.Icons.Default, out Request defaultRequest) && defaultRequest.Sprite)
                return defaultRequest.Sprite;
            return Generated(Shape.Diamond);
        }

        private Sprite Generated(Shape shape)
        {
            ref Sprite sprite = ref generated[(int)shape];
            if (!sprite)
                sprite = CreateSprite(shape);
            return sprite;
        }

        private static Sprite CreateSprite(Shape shape)
        {
            const int size = 16;
            const float center = (size - 1) * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center, dy = Math.Abs(y - center);
                    float coverage = shape switch
                    {
                        Shape.Arrow => x >= dy && x <= 12f ? 1f : 0f,
                        // Soft edge so the dot stays round when scaled down to a few pixels.
                        Shape.Dot => Math.Clamp(center + 0.5f - (float)Math.Sqrt(dx * dx + dy * dy), 0f, 1f),
                        _ => Math.Abs(dx) + dy <= center ? 1f : 0f
                    };
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(coverage * 255f));
                }
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "MiniMap." + shape,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            foreach (Request request in requests.Values)
            {
                if (!request.Handle.IsValid())
                    continue;
                request.Handle.Completed -= request.Completed;
                Addressables.Release(request.Handle);
            }
            requests.Clear();
            // Loaded sprites belong to Addressables; only our generated ones are destroyed here.
            for (int i = 0; i < generated.Length; i++)
            {
                if (!generated[i])
                    continue;
                UnityEngine.Object.Destroy(generated[i].texture);
                UnityEngine.Object.Destroy(generated[i]);
                generated[i] = null;
            }
        }
    }
}
