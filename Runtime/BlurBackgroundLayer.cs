using System.Threading;
using System.Threading.Tasks;
using Tools.ScreenSystem.Scripts.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.ScreenSystem.Scripts
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasGroup))]
	public class BlurBackgroundLayer : MonoBehaviour
	{
		private const float DEFAULT_FADE_DURATION = 0.2f;
		private const float DEFAULT_BLUR_STRENGTH = 3.0f;

		[Header("Blur Settings")] [Tooltip("Strength of the blur effect (0-10, higher = more blur)")] [SerializeField] [Range(0f, 10f)]
		private float blurStrength = DEFAULT_BLUR_STRENGTH;

		[Tooltip("Tint color overlay (default: semi-transparent black)")] [SerializeField]
		private Color tintColor = new Color(0f, 0f, 0f, 0.4f);

		[Tooltip("Fade in/out duration in seconds")] [SerializeField]
		private float fadeDuration = DEFAULT_FADE_DURATION;

		[Header("Mobile Glassmorphism")]
		[Tooltip("Use fake glassmorphism (no blur) for mobile performance")]
		[SerializeField]
		private bool useFakeGlassmorphism = true;

		[Tooltip("Darken the background for better glass effect visibility")]
		[SerializeField]
		[Range(0f, 1f)]
		private float backgroundDarkness = 0.3f;

        [Header("References")] [Tooltip("Image component for blur material (auto-found if not set)")] [SerializeField]
        private Image blurImage;

        [Tooltip("CanvasGroup for fade animations (auto-found if not set)")] [SerializeField]
        private CanvasGroup canvasGroup;

        private BlurFadeController fadeController;
        private Canvas canvas;
        private Canvas parentCanvas;
        private bool isInitialized;

        public float BlurStrength
        {
            get => blurStrength;
            set
            {
                blurStrength = Mathf.Clamp(value, 0f, 10f);
                if (blurImage != null)
                    blurImage.color = new Color(blurImage.color.r, blurImage.color.g, blurImage.color.b, tintColor.a);
            }
        }

		public Color TintColor
		{
			get => tintColor;
			set
			{
				tintColor = value;
				if (blurImage != null)
					blurImage.color = tintColor;
			}
		}

		public bool UseFakeGlassmorphism
		{
			get => useFakeGlassmorphism;
			set => useFakeGlassmorphism = value;
		}

		public float BackgroundDarkness
		{
			get => backgroundDarkness;
			set
			{
				backgroundDarkness = Mathf.Clamp01(value);
				UpdateTintColorForGlassEffect();
			}
		}

        private void EnsureInitialized()
        {
            if (isInitialized)
                return;

            ScreenSystemSettings settings = ScreenSystemSettings.Instance;
            if (settings != null)
            {
                blurStrength = settings.GetEffectiveBlurStrength();
                tintColor = settings.BlurTintColor;
                fadeDuration = settings.BlurFadeDuration;
            }

            Transform blurImageTransform = transform.Find("BlurImage");
            if (blurImageTransform != null)
                blurImage = blurImageTransform.GetComponent<Image>();
            else
            {
                blurImage = GetComponentInChildren<Image>(true);
                Debug.LogWarning("[BlurBackgroundLayer] BlurImage child not found by name, using fallback search");
            }

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvas == null)
                canvas = GetComponent<Canvas>();

            parentCanvas = GetComponentInParent<Canvas>();

            SetupCanvasForBlur();
            SetupURPBlur();

            fadeController = new BlurFadeController(canvasGroup, fadeDuration);
            fadeController.SetAlphaImmediate(0f);

            isInitialized = true;
        }


        private void SetupCanvasForBlur()
        {
            if (canvas == null)
            {
                Debug.LogError("[BlurBackgroundLayer] No Canvas component found!");
                return;
            }

            canvas.overrideSorting = true;
            canvas.sortingLayerName = "Default";
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

		private void SetupURPBlur()
		{
			if (blurImage == null)
				return;

			UpdateTintColorForGlassEffect();
			blurImage.raycastTarget = false;
		}

		private void UpdateTintColorForGlassEffect()
		{
			if (blurImage == null)
				return;

			if (useFakeGlassmorphism)
			{
				Color glassBackgroundTint = new(
					tintColor.r * (1f - backgroundDarkness),
					tintColor.g * (1f - backgroundDarkness),
					tintColor.b * (1f - backgroundDarkness),
					tintColor.a
				);
				blurImage.color = glassBackgroundTint;
			}
			else
			{
				blurImage.color = tintColor;
			}
		}

        private void EnableURPBlur()
        {
            if (blurImage != null)
                blurImage.enabled = true;
            Debug.Log("[BlurBackgroundLayer] Blur overlay enabled");
        }

        private void DisableURPBlur()
        {
            if (blurImage != null)
                blurImage.enabled = false;
            Debug.Log("[BlurBackgroundLayer] Blur overlay disabled");
        }

        public async Task ShowAsync(CancellationToken ct = default)
        {
            EnsureInitialized();

            gameObject.SetActive(true);
            EnsureProperHierarchyPosition();

            canvas.enabled = true;
            EnableURPBlur();

            await fadeController.FadeAsync(0f, 1f, ct);
        }

        private void EnsureProperHierarchyPosition()
        {
            if (parentCanvas != null && parentCanvas.transform != transform.parent)
                transform.SetParent(parentCanvas.transform, false);

            transform.SetAsFirstSibling();
        }

        public async Task HideAsync(CancellationToken ct = default)
        {
            if (canvasGroup == null || fadeDuration <= 0f)
            {
                DisableURPBlur();
                gameObject.SetActive(false);
                return;
            }

            try
            {
                await fadeController.FadeAsync(1f, 0f, ct);
                DisableURPBlur();
                gameObject.SetActive(false);
            }
            catch (System.OperationCanceledException)
            {
                DisableURPBlur();
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            fadeController?.Cleanup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            if (blurImage != null)
                blurImage.color = tintColor;
        }

        private void Reset()
        {
            blurImage = GetComponent<Image>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (blurImage == null)
                blurImage = gameObject.AddComponent<Image>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            tintColor = new Color(0f, 0f, 0f, 0.3f);
            fadeDuration = DEFAULT_FADE_DURATION;

            if (blurImage != null)
            {
                blurImage.raycastTarget = true;
                blurImage.color = tintColor;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.name = "BlurBackground";
        }
#endif
    }
}

