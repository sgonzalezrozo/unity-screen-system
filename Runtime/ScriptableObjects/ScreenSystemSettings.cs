using UnityEngine;

namespace ScreenSystem.ScriptableObjects
{
    public class ScreenSystemSettings : ScriptableObject
    {
        [Header("UI Settings")]
        [Tooltip("The UI frameworks your screens use. Each layer's framework is detected from its own components; with UIToolkit only, Canvas Render Mode isn't applied.")]
        public UIFramework UIFramework = UIFramework.Both;

        [Tooltip("Render mode of every uGUI layer's Canvas. Screen Space - Overlay is the tested one; the blur behind modals assumes it.")]
        public RenderMode CanvasRenderMode = RenderMode.ScreenSpaceOverlay;

        [Header("References")]
        [Tooltip("The registry of every ScreenDefinition. Setup assigns it, and it updates itself when screens change.")]
        public ScreenRegistry ScreenRegistry;

        [Header("Blur Settings")]
        [Tooltip("How blurred the snapshot behind modals and alerts is (0-10, higher is blurrier). On Android and iOS, Mobile Blur Strength replaces it while Reduce Blur Quality On Mobile is on.")]
        [Range(0f, 10f)]
        public float BlurStrength = 3.0f;

        [Tooltip("Darkens and tints the blurred snapshot behind modals. Alpha is the strength: 0 keeps only the blur, 1 is solid color.")]
        public Color BlurTintColor = new Color(0f, 0f, 0f, 0.5f);

        [HideInInspector]
        [System.Obsolete("Has no effect: the blurred snapshot appears with the modal, without a fade. It will be removed.")]
        public float BlurFadeDuration = 0.2f;

        [Tooltip("Resolution of the blurred snapshot shown behind modals, as a fraction of the screen. Lower is cheaper and blurrier.")]
        [Range(0.1f, 1f)]
        public float BlurResolutionScale = 0.25f;

        [Tooltip("Color laid between a modal and an alert opened over it. The modal stays visible, so it's only darkened, not blurred.")]
        public Color AlertDimColor = new Color(0f, 0f, 0f, 0.5f);

        [Header("Back Navigation")]
        [Tooltip("Back (Escape or Android back) at the root screen opens the QuitConfirmation modal or quits. Turn it off while migrating from your own UI: back at the root then only raises NavigationEvents.OnBackAtRoot, and your code decides.")]
        public bool QuitOnBackAtRoot = true;

        [Header("Mobile Settings")]
        [Tooltip("On Android and iOS, use Mobile Blur Strength instead of Blur Strength.")]
        public bool ReduceBlurQualityOnMobile = true;

        [Tooltip("Blur strength on Android and iOS while Reduce Blur Quality On Mobile is on (0-10). Lower is cheaper.")]
        [Range(0f, 10f)]
        public float MobileBlurStrength = 1.5f;

        [Header("Generated Screen IDs")]
        [Tooltip("Write a ScreenIds class with one constant per screen, regenerated when ScreenDefinitions change, so GoTo(ScreenIds.ShopMain) is checked at compile time.")]
        public bool GenerateScreenIds = true;

        public const string DefaultScreenIdsPath = "Assets/ScreenSystem/ScreenIds.cs";

        [Tooltip("Where ScreenIds is written. If your code is in an assembly definition, put it inside that assembly's folder so the code can see it.")]
        public string ScreenIdsPath = DefaultScreenIdsPath;

        [HideInInspector]
        [System.Obsolete("Has no effect: informational logs are compiled in with the SCREEN_SYSTEM_VERBOSE scripting define symbol. It will be removed.")]
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
                                       "║     Tools → Screen System → Setup                          ║\n" +
                                       "║                                                            ║\n" +
                                       "║  This is a ONE-TIME setup (30 seconds)                     ║\n" +
                                       "╚════════════════════════════════════════════════════════════╝");
                    }
                }

                return _instance;
            }
        }

        public static bool IsValid()
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
