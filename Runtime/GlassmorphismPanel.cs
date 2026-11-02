using UnityEngine;
using UnityEngine.UI;

namespace Tools.ScreenSystem.Scripts
{
	[RequireComponent(typeof(Image))]
	[RequireComponent(typeof(CanvasGroup))]
	public class GlassmorphismPanel : MonoBehaviour
	{
		private const string LOG_PREFIX = "[Glassmorphism]";

		[Header("Glass Settings")]
		[SerializeField]
		[Range(0.1f, 1f)]
		private float glassOpacity = 0.2f;

		[SerializeField]
		private Color glassColor = Color.white;

		[SerializeField]
		private bool useGradientBackground;

		[SerializeField]
		private Color gradientStartColor = new(1f, 1f, 1f, 0.2f);

		[SerializeField]
		private Color gradientEndColor = new(0.8f, 0.8f, 0.8f, 0.2f);

		[Header("Border Settings")]
		[SerializeField]
		private bool enableBorder;

		[SerializeField]
		private Image borderImage;

		[SerializeField]
		[Range(0.1f, 5f)]
		private float borderThickness = 2f;

		[SerializeField]
		private Color borderColor = new(1f, 1f, 1f, 0.8f);

		[Header("Shadow Settings")]
		[SerializeField]
		private bool enableShadow;

		[SerializeField]
		private Image shadowImage;

		[SerializeField]
		private Color shadowColor = new(0f, 0f, 0f, 0.3f);

		[SerializeField]
		private Vector2 shadowOffset = new(0f, -4f);

		private Image glassImage;
		private CanvasGroup canvasGroup;
		private RectTransform rectTransform;

		private void Awake()
		{
			InitializeComponents();
			ApplyGlassEffect();
		}

		private void InitializeComponents()
		{
			glassImage = GetComponent<Image>();
			canvasGroup = GetComponent<CanvasGroup>();
			rectTransform = GetComponent<RectTransform>();

			if (glassImage == null)
			{
				Debug.LogError($"{LOG_PREFIX} Image component is required");
				return;
			}

			glassImage.raycastTarget = true;
		}

		private void ApplyGlassEffect()
		{
			if (glassImage == null)
				return;

			Color panelColor = glassColor;
			panelColor.a = glassOpacity;
			glassImage.color = panelColor;

			ApplyBorderEffect();
			ApplyShadowEffect();

			Debug.Log($"{LOG_PREFIX} Glass effect applied with opacity {glassOpacity}");
		}

		private void ApplyBorderEffect()
		{
			if (!enableBorder || borderImage == null)
				return;

			borderImage.color = borderColor;
			RectTransform borderRect = borderImage.GetComponent<RectTransform>();
			if (borderRect != null)
			{
				borderRect.anchorMin = Vector2.zero;
				borderRect.anchorMax = Vector2.one;
				borderRect.offsetMin = new(-borderThickness, -borderThickness);
				borderRect.offsetMax = new(borderThickness, borderThickness);
			}

			borderImage.raycastTarget = false;
			Debug.Log($"{LOG_PREFIX} Border effect applied with thickness {borderThickness}");
		}

		private void ApplyShadowEffect()
		{
			if (!enableShadow || shadowImage == null)
				return;

			shadowImage.color = shadowColor;
			RectTransform shadowRect = shadowImage.GetComponent<RectTransform>();
			if (shadowRect != null)
			{
				shadowRect.anchoredPosition = shadowOffset;
				shadowRect.sizeDelta = new(20f, 20f);
			}

			shadowImage.raycastTarget = false;
			Debug.Log($"{LOG_PREFIX} Shadow effect applied");
		}

		public void SetGlassOpacity(float opacity)
		{
			glassOpacity = Mathf.Clamp01(opacity);
			if (glassImage != null)
			{
				Color panelColor = glassImage.color;
				panelColor.a = glassOpacity;
				glassImage.color = panelColor;
			}
		}

		public void SetGlassColor(Color color)
		{
			glassColor = color;
			if (glassImage != null)
			{
				Color panelColor = color;
				panelColor.a = glassOpacity;
				glassImage.color = panelColor;
			}
		}

		public void SetBorderColor(Color color)
		{
			borderColor = color;
			if (borderImage != null)
				borderImage.color = borderColor;
		}

		public void SetShadowColor(Color color)
		{
			shadowColor = color;
			if (shadowImage != null)
				shadowImage.color = shadowColor;
		}

		public float GetGlassOpacity()
			=> glassOpacity;

		public Color GetGlassColor()
			=> glassColor;
	}
}

