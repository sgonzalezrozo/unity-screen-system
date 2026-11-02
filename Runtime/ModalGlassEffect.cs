using UnityEngine;
using UnityEngine.UI;

namespace ScreenSystem
{
	public class ModalGlassEffect : MonoBehaviour
	{
		private const string LOG_PREFIX = "[ModalGlass]";

		[Header("Glass Container")]
		[SerializeField]
		private Image mainPanel;

		[SerializeField]
		[Range(0.15f, 0.3f)]
		private float panelOpacity = 0.2f;

		[Header("Border")]
		[SerializeField]
		private Image borderImage;

		[SerializeField]
		private bool createBorderIfMissing = true;

		[Header("Shadow")]
		[SerializeField]
		private bool enableDropShadow = true;

		[SerializeField]
		private Vector2 shadowOffset = new(0f, -8f);

		private GlassmorphismPanel glassmorphismPanel;

		private void Awake()
		{
			InitializeGlassEffect();
		}

		private void InitializeGlassEffect()
		{
			if (mainPanel == null)
			{
				mainPanel = GetComponent<Image>();
				if (mainPanel == null)
					mainPanel = GetComponentInChildren<Image>();
			}

			if (mainPanel == null)
			{
				Debug.LogError($"{LOG_PREFIX} No Image component found on modal");
				return;
			}

			CreateOrGetGlassmorphismPanel();
			SetupBorder();
			SetupShadow();

			Debug.Log($"{LOG_PREFIX} Modal glass effect initialized");
		}

		private void CreateOrGetGlassmorphismPanel()
		{
			glassmorphismPanel = GetComponent<GlassmorphismPanel>();

			if (glassmorphismPanel == null)
				glassmorphismPanel = gameObject.AddComponent<GlassmorphismPanel>();

			glassmorphismPanel.SetGlassOpacity(panelOpacity);
		}

		private void SetupBorder()
		{
			if (borderImage == null && createBorderIfMissing)
			{
				Transform existingBorder = transform.Find("Border");
				if (existingBorder != null)
				{
					borderImage = existingBorder.GetComponent<Image>();
				}
				else
				{
					GameObject borderObj = new("Border");
					borderImage = borderObj.AddComponent<Image>();
					borderObj.AddComponent<CanvasGroup>();

					RectTransform borderRect = borderObj.GetComponent<RectTransform>();
					borderRect.SetParent(transform, false);
					borderRect.anchorMin = Vector2.zero;
					borderRect.anchorMax = Vector2.one;
					borderRect.offsetMin = Vector2.zero;
					borderRect.offsetMax = Vector2.zero;

					borderImage.color = new Color(1f, 1f, 1f, 0.8f);
					borderImage.raycastTarget = false;

					Debug.Log($"{LOG_PREFIX} Created border Image");
				}
			}

			if (borderImage != null && glassmorphismPanel != null)
			{
				Color borderColor = new(1f, 1f, 1f, 0.8f);
				glassmorphismPanel.SetBorderColor(borderColor);
			}
		}

		private void SetupShadow()
		{
			if (!enableDropShadow)
				return;

			Shadow shadow = GetComponent<Shadow>();
			if (shadow == null)
				shadow = gameObject.AddComponent<Shadow>();

			shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
			shadow.effectDistance = shadowOffset;

			Debug.Log($"{LOG_PREFIX} Drop shadow configured");
		}

		public void SetModalOpacity(float opacity)
		{
			panelOpacity = Mathf.Clamp(opacity, 0.1f, 1f);
			if (glassmorphismPanel != null)
				glassmorphismPanel.SetGlassOpacity(panelOpacity);
		}

		public void SetModalGlassColor(Color color)
		{
			if (glassmorphismPanel != null)
				glassmorphismPanel.SetGlassColor(color);
		}

		public float GetModalOpacity()
			=> panelOpacity;
	}
}

