using UnityEngine;
using UnityEngine.UI;

namespace Tools.ScreenSystem.Scripts
{
	public class BlurEffectRenderer
	{
		private readonly Image blurImage;
		private Color tintColor;

		public Material BlurMaterial => null;

		public BlurEffectRenderer(Image blurImage, float initialStrength, Color initialTint)
		{
			this.blurImage = blurImage;
			this.tintColor = initialTint;
		}

		public void Initialize()
		{
			Debug.Log("[BlurEffectRenderer] Using URP blur - no shader initialization needed");
			ApplySettings();
		}

		public void ApplySettings()
		{
			if (blurImage != null)
			{
				blurImage.color = tintColor;
				blurImage.raycastTarget = false;
			}
		}

		public void SetBlurStrength(float strength)
		{
		}

		public void SetBlurRadius(float radius)
		{
		}

		public void SetBlurIntensity(float intensity)
		{
		}

		public void SetTintColor(Color color)
		{
			tintColor = color;
			if (blurImage != null)
				blurImage.color = tintColor;
		}

		public void Cleanup()
		{
		}
	}
}
