using UnityEngine;

namespace ScreenSystem.ScriptableObjects
{
    public class ScreenSystemSettings : ScriptableObject
    {
        [Header("UI Settings")]
        [Tooltip("Select the UI framework(s) used in the project (uGUI, UIToolkit, or Both)")]
        public UIFramework UIFramework = UIFramework.Both;

        [Tooltip("Render mode for the main canvas")]
        public RenderMode CanvasRenderMode = RenderMode.ScreenSpaceOverlay;

        [Header("References")]
        [Tooltip("Main screen registry containing all screens and features")]
        public ScreenRegistry ScreenRegistry;

        [Header("Blur Settings")]
        [Tooltip("Strength of Gaussian blur effect (0-10, higher = more blur)")]
        [Range(0f, 10f)]
        public float BlurStrength = 3.0f;

        [Tooltip("Darkens and tints the blurred snapshot behind modals. Alpha is the strength: 0 keeps only the blur, 1 is solid color.")]
        public Color BlurTintColor = new Color(0f, 0f, 0f, 0.5f);

        [Tooltip("Fade duration for blur show/hide animation in seconds")]
        [Range(0f, 1f)]
        public float BlurFadeDuration = 0.2f;

        [Tooltip("Resolution of the blurred snapshot shown behind modals, as a fraction of the screen. Lower is cheaper and blurrier.")]
        [Range(0.1f, 1f)]
        public float BlurResolutionScale = 0.25f;

        [Tooltip("Color laid between a modal and an alert opened over it. The modal stays visible, so it's only darkened, not blurred.")]
        public Color AlertDimColor = new Color(0f, 0f, 0f, 0.5f);

        [Header("Mobile Settings")]
        [Tooltip("Use reduced blur quality on mobile devices for better performance")]
        public bool ReduceBlurQualityOnMobile = true;

        [Tooltip("Blur strength for mobile devices (0-10, lower = better performance)")]
        [Range(0f, 10f)]
        public float MobileBlurStrength = 1.5f;

        [Header("Debug")]
        [Tooltip("Log level for screen system operations")]
        public LogLevel DefaultLogLevel = LogLevel.None;

        private static ScreenSystemSettings _instance;

        public static ScreenSystemSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ScreenSystemSettings>("Tools/Screen System/Scriptables/ScreenSystemSettings");

                    if (_instance == null)
                    {
                        Debug.LogError("╔════════════════════════════════════════════════════════════╗\n" +
                                       "║  SCREEN SYSTEM NOT SET UP                                  ║\n" +
                                       "╠════════════════════════════════════════════════════════════╣\n" +
                                       "║  ScreenSystemSettings not found!                           ║\n" +
                                       "║                                                            ║\n" +
                                       "║  REQUIRED ACTION:                                          ║\n" +
                                       "║     In Unity, click:                                       ║\n" +
                                       "║     Tools → Screen System → Setup Project                  ║\n" +
                                       "║                                                            ║\n" +
                                       "║  This is a ONE-TIME setup (30 seconds)                     ║\n" +
                                       "╚════════════════════════════════════════════════════════════╝");
                    }
                }

                return _instance;
            }
        }        public static bool IsValid()
        {
            ScreenSystemSettings settings = Instance;
            return settings != null &&
                   settings.ScreenRegistry != null;
        }

        public float GetEffectiveBlurStrength()
        {
            #if UNITY_ANDROID || UNITY_IOS
                if (ReduceBlurQualityOnMobile)
                    return MobileBlurStrength;
            #endif
            return BlurStrength;
        }
    }

    public enum LogLevel
    {
        None,
        Error,
        Warning,
        Info,
        Verbose
    }
}
