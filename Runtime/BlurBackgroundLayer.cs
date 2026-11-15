using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenSystem
{
    /// <summary>
    /// The backdrop behind modals and alerts. It has two parts, each on its own Canvas so each can sit at its own
    /// sorting order:
    /// - Snapshot: a blurred copy of the screen taken just before a modal opens. It replaces the fullscreen screen,
    ///   which then stops rendering, so a modal draws over one quad instead of a whole screen.
    /// - Dim: a flat color between a modal and an alert opened over it. The modal stays visible, so it isn't blurred.
    /// Both parts stay active and only toggle their Canvas, like screen layers. Created by Tools > Screen System > Setup.
    /// </summary>
    public class BlurBackgroundLayer : MonoBehaviour
    {
        private const int BLUR_PASSES = 2;

        [SerializeField] private Shader blurShader;
        [SerializeField] private Canvas snapshotCanvas;
        [SerializeField] private RawImage snapshotImage;
        [SerializeField] private Canvas dimCanvas;
        [SerializeField] private Image dimImage;

        private Material blurMaterial;
        private RenderTexture blurred;

        public bool IsSnapshotVisible
            => snapshotCanvas != null && snapshotCanvas.enabled;

        public bool IsDimVisible
            => dimCanvas != null && dimCanvas.enabled;

        /// <summary>Last snapshot cost on the main thread, in milliseconds. Read by the benchmark.</summary>
        public double LastSnapshotMs { get; private set; }

        /// <summary>Sets the references. Called by Setup when it builds the backdrop.</summary>
        public void Configure(Shader shader, Canvas snapshot, RawImage snapshotRawImage, Canvas dim, Image dimColorImage)
        {
            blurShader = shader;
            snapshotCanvas = snapshot;
            snapshotImage = snapshotRawImage;
            dimCanvas = dim;
            dimImage = dimColorImage;
        }

        private void Awake()
        {
            if (snapshotCanvas != null)
                snapshotCanvas.enabled = false;
            if (dimCanvas != null)
                dimCanvas.enabled = false;
        }

        /// <summary>
        /// Captures what's on screen at the end of this frame and blurs it at low resolution. Call it before the modal
        /// becomes visible, so the capture holds only the screen underneath.
        /// </summary>
        public Task CaptureAsync(CancellationToken ct = default)
        {
            // Batch mode doesn't render, and WaitForEndOfFrame never comes there. Without a snapshot, the screen
            // underneath keeps rendering instead.
            if (blurShader == null || snapshotImage == null || Application.isBatchMode)
                return Task.CompletedTask;

            TaskCompletionSource<bool> done = new();
            StartCoroutine(CaptureAtEndOfFrame(done, ct));
            return done.Task;
        }

        private IEnumerator CaptureAtEndOfFrame(TaskCompletionSource<bool> done, CancellationToken ct)
        {
            yield return new WaitForEndOfFrame();

            if (ct.IsCancellationRequested)
            {
                done.TrySetCanceled(ct);
                yield break;
            }

            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            using (ScreenSystemMarkers.Snapshot.Auto())
                Capture();
            LastSnapshotMs = watch.Elapsed.TotalMilliseconds;
            done.TrySetResult(true);
        }

        private void Capture()
        {
            ScreenSystemSettings settings = ScreenSystemSettings.Instance;
            float scale = settings != null ? settings.BlurResolutionScale : 0.25f;
            float strength = settings != null ? settings.GetEffectiveBlurStrength() : 3f;

            int width = Mathf.Max(16, Mathf.RoundToInt(Screen.width * scale));
            int height = Mathf.Max(16, Mathf.RoundToInt(Screen.height * scale));
            EnsureTarget(width, height);
            if (blurMaterial == null)
                blurMaterial = new Material(blurShader) { hideFlags = HideFlags.HideAndDontSave };

            RenderTexture full = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            RenderTexture temp = RenderTexture.GetTemporary(width, height, 0);
            try
            {
                ScreenCapture.CaptureScreenshotIntoRenderTexture(full);

                // The capture is upside down on graphics APIs whose texture origin is at the top.
                if (SystemInfo.graphicsUVStartsAtTop)
                    Graphics.Blit(full, blurred, new Vector2(1f, -1f), new Vector2(0f, 1f));
                else
                    Graphics.Blit(full, blurred);

                blurMaterial.SetFloat("_Spread", Mathf.Max(0.5f, strength));
                for (int pass = 0; pass < BLUR_PASSES; pass++)
                {
                    blurMaterial.SetVector("_Direction", new Vector4(1f, 0f, 0f, 0f));
                    Graphics.Blit(blurred, temp, blurMaterial);
                    blurMaterial.SetVector("_Direction", new Vector4(0f, 1f, 0f, 0f));
                    Graphics.Blit(temp, blurred, blurMaterial);
                }
            }
            finally
            {
                RenderTexture.ReleaseTemporary(full);
                RenderTexture.ReleaseTemporary(temp);
            }

            snapshotImage.texture = blurred;
            snapshotImage.color = settings != null ? Color.Lerp(Color.white, settings.BlurTintColor, settings.BlurTintColor.a) : Color.white;
        }

        private void EnsureTarget(int width, int height)
        {
            if (blurred != null && blurred.width == width && blurred.height == height)
                return;

            ReleaseTarget();
            blurred = new RenderTexture(width, height, 0, RenderTextureFormat.Default) { name = "ScreenSystem Snapshot", filterMode = FilterMode.Bilinear };
            blurred.Create();
        }

        public void ShowSnapshot(int sortingOrder)
        {
            if (snapshotCanvas == null || blurred == null)
                return;

            snapshotCanvas.overrideSorting = true;
            snapshotCanvas.sortingOrder = sortingOrder;
            snapshotCanvas.enabled = true;
        }

        public void HideSnapshot()
        {
            if (snapshotCanvas != null)
                snapshotCanvas.enabled = false;
        }

        public void ShowDim(int sortingOrder)
        {
            if (dimCanvas == null)
                return;

            ScreenSystemSettings settings = ScreenSystemSettings.Instance;
            if (dimImage != null && settings != null)
                dimImage.color = settings.AlertDimColor;

            dimCanvas.overrideSorting = true;
            dimCanvas.sortingOrder = sortingOrder;
            dimCanvas.enabled = true;
        }

        public void HideDim()
        {
            if (dimCanvas != null)
                dimCanvas.enabled = false;
        }

        private void ReleaseTarget()
        {
            if (blurred == null)
                return;

            if (snapshotImage != null && snapshotImage.texture == blurred)
                snapshotImage.texture = null;

            blurred.Release();
            Destroy(blurred);
            blurred = null;
        }

        private void OnDestroy()
        {
            ReleaseTarget();
            if (blurMaterial != null)
                Destroy(blurMaterial);
        }
    }
}
