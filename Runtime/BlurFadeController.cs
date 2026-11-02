using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScreenSystem
{
    public class BlurFadeController
    {
        private readonly CanvasGroup canvasGroup;
        private float fadeDuration;
        private CancellationTokenSource animationCts;

        public BlurFadeController(CanvasGroup canvasGroup, float fadeDuration)
        {
            this.canvasGroup = canvasGroup;
            this.fadeDuration = fadeDuration;
        }

        public void SetFadeDuration(float duration)
        {
            fadeDuration = duration;
        }

        public async Task FadeAsync(float startAlpha, float endAlpha, CancellationToken ct = default)
        {
            animationCts?.Cancel();
            animationCts?.Dispose();
            animationCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            if (canvasGroup == null || fadeDuration <= 0f)
            {
                canvasGroup.alpha = endAlpha;
                return;
            }

            await PerformFade(startAlpha, endAlpha, animationCts.Token);
        }

        private async Task PerformFade(float startAlpha, float endAlpha, CancellationToken ct)
        {
            canvasGroup.alpha = startAlpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                await Task.Yield();
            }

            canvasGroup.alpha = endAlpha;
        }

        public void SetAlphaImmediate(float alpha)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }

        public void CancelAnimation()
        {
            animationCts?.Cancel();
            animationCts?.Dispose();
            animationCts = null;
        }

        public void Cleanup()
        {
            CancelAnimation();
        }
    }
}

