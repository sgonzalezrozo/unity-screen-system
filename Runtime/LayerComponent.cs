using System;
using System.Threading;
using System.Threading.Tasks;
using Tools.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Tools.ScreenSystem.Scripts
{
    public class LayerComponent : MonoBehaviour
    {
        public event Action<GameObject> OnContentLoaded;
        public event Action OnContentUnloaded;

        [Header("Content Loading")] [SerializeField]
        private string contentPrefabName;

        [SerializeField] private LayerContentLifecycle.LoadTiming loadTiming = LayerContentLifecycle.LoadTiming.OnFirstShow;

        internal IPrefabInstantiator injectedInstantiator;

        private bool isShared;
        private UIFramework framework;

        // Flag to suppress child component initialization until after injection
        private bool isInitializationSuppressed = true;

        // Current layer being initialized (for child components to check)
        private static LayerComponent currentInitializingLayer;

        private CanvasController canvasController;
        private UIDocumentController uiDocumentController;
        private ILayerUIView uiView;
        private ILayerLifecycle lifecycle;
        private SharedLayerManager sharedLayerManager;
        private LayerContentLifecycle contentLifecycle;
        private ILayerContentLoader contentLoader;

        private IScreenShow[] cachedShowComponents;
        private IScreenHide[] cachedHideComponents;

        public string LayerName
            => gameObject.name;

        public bool IsShared
            => isShared;

        public Canvas Canvas
            => canvasController?.Canvas;

        public GraphicRaycaster GraphicRaycaster
            => canvasController?.GraphicRaycaster;

        public string CurrentScreenId
            => sharedLayerManager.CurrentScreenId;

        public int CurrentZOrder
            => sharedLayerManager.CurrentZOrder;

        public bool HasContent
            => contentLifecycle?.HasContent ?? false;

        public bool IsLoadingContent
            => contentLifecycle?.IsLoadingContent ?? false;

        public int AssignmentCount
            => sharedLayerManager.AssignmentCount;

        public bool IsBeingUsed
            => sharedLayerManager.IsBeingUsed;

        public bool IsCanvasEnabled
            => canvasController?.IsCanvasEnabled ?? false;

        public UIFramework Framework
            => framework;

        public bool IsInitializationSuppressed
            => isInitializationSuppressed;

        public static bool IsCurrentLayerInitializationSuppressed()
        {
            return currentInitializingLayer != null && currentInitializingLayer.isInitializationSuppressed;
        }

        public void AllowInitialization()
        {
            isInitializationSuppressed = false;

            IScreenShow[] showComponents = gameObject.GetComponentsInChildren<IScreenShow>(includeInactive: true);
            for (int i = 0; i < showComponents.Length; i++)
            {
                MonoBehaviour component = showComponents[i] as MonoBehaviour;
                if (component == null)
                    continue;

                component.SendMessage("OnInitializationAllowed", SendMessageOptions.DontRequireReceiver);
            }
        }

        public void SetContentName(string prefabName)
        {
            contentPrefabName = prefabName;
        }

#if UNITY_EDITOR
        void Reset()
        {
            CanvasController tempController = new(gameObject);
            tempController.ConfigureForEditor();

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        void OnValidate()
        {
            Canvas canvasComponent = GetComponent<Canvas>();
            if (canvasComponent != null && !canvasComponent.overrideSorting)
            {
                canvasComponent.overrideSorting = true;
                UnityEditor.EditorUtility.SetDirty(gameObject);
            }
        }
#endif

        private void Awake()
        {
            currentInitializingLayer = this;
            sharedLayerManager = new SharedLayerManager();

            Canvas canvas = GetComponent<Canvas>();
            UIDocument uiDocument = GetComponent<UIDocument>();

            if (uiDocument != null)
            {
                framework = UIFramework.UIToolkit;
                InitializeUIToolkit(uiDocument);
            }
            else if (canvas != null)
            {
                framework = UIFramework.uGUI;
                InitializeuGUI(canvas);
            }
            else
            {
                framework = UIFramework.UIToolkit;
                InitializeUIToolkit(null);
            }

            cachedShowComponents = gameObject.GetComponentsInChildren<IScreenShow>(includeInactive: true);
            cachedHideComponents = gameObject.GetComponentsInChildren<IScreenHide>(includeInactive: true);

#if DEBUG_LAYER_COMPONENT
            Debug.Log($"[LayerComponent] '{gameObject.name}' cached {cachedShowComponents.Length} IScreenShow components and {cachedHideComponents.Length} IScreenHide components");

            for (int i = 0; i < cachedShowComponents.Length; i++)
                Debug.Log($"[LayerComponent]   - Show[{i}]: {cachedShowComponents[i]?.GetType().Name ?? "NULL"}");

            for (int i = 0; i < cachedHideComponents.Length; i++)
                Debug.Log($"[LayerComponent]   - Hide[{i}]: {cachedHideComponents[i]?.GetType().Name ?? "NULL"}");
#endif

            EnsureComponents();
        }

        private void InitializeuGUI(Canvas canvas)
        {
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            canvasController = new CanvasController(gameObject);
            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            uiView = new LayerUIView(canvas, raycaster);
            lifecycle = new LayerLifecycleManager(uiView, sharedLayerManager);

            if (injectedInstantiator == null)
            {
#if DEBUG_LAYER_COMPONENT
                Debug.LogWarning($"[LayerComponent] '{gameObject.name}' - Using NullPrefabInstantiator (standard Unity Instantiate). " +
                              "FastIoC DI not injected, which is expected if IoC is not configured for this project.");
#endif
                injectedInstantiator = new NullPrefabInstantiator();
            }

            contentLoader = new LayerContentLoader(transform, contentPrefabName, injectedInstantiator);
            contentLifecycle = new LayerContentLifecycle(contentLoader, contentPrefabName, loadTiming);

            contentLoader.OnContentLoaded += HandleContentLoaded;
            contentLoader.OnContentUnloaded += HandleContentUnloaded;
        }

        private void InitializeUIToolkit(UIDocument uiDocument)
        {
            if (uiDocument != null)
            {
                uiDocumentController = new UIDocumentController(gameObject);
                uiView = new LayerUIView_UIToolkit(uiDocument);
            }
            else
            {
                Debug.Log($"[LayerComponent] Bare shell '{gameObject.name}' - UIDocument will be provided by content");
                uiDocumentController = null;
                uiView = null;
            }

            lifecycle = new LayerLifecycleManager(uiView, sharedLayerManager);

            LayerContentLoader_UIToolkit contentLoaderUIToolkit = new(uiDocument, contentPrefabName, transform);
            contentLifecycle = new LayerContentLifecycle(contentLoaderUIToolkit, contentPrefabName, loadTiming);
            contentLoader = contentLoaderUIToolkit;

            contentLoader.OnContentLoaded += HandleContentLoaded;
            contentLoader.OnContentUnloaded += HandleContentUnloaded;
        }

        private void HandleContentLoaded(GameObject content)
        {
            OnContentLoaded?.Invoke(content);
        }

        private void HandleContentUnloaded()
        {
            OnContentUnloaded?.Invoke();
        }

        private void EnsureComponents()
        {
            if (framework != UIFramework.uGUI)
                return;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
                Debug.LogWarning($"[LayerComponent] Added missing GraphicRaycaster to {gameObject.name}");
            }

            if (GetComponent<Canvas>() != null)
                return;

            Canvas c = gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
        }

        public void SetNavigationToken(CancellationToken ct)
        {
            contentLifecycle?.SetNavigationToken(ct);
        }

        void OnEnable()
        {
            contentLifecycle?.HandleOnEnable(this);
        }

        void OnDestroy()
        {
            if (contentLoader != null)
            {
                contentLoader.OnContentLoaded -= HandleContentLoaded;
                contentLoader.OnContentUnloaded -= HandleContentUnloaded;
            }

            if (currentInitializingLayer == this)
                currentInitializingLayer = null;

            contentLifecycle?.Cleanup();
        }

        public void AssignToScreen(string screenId, int zOrder, int layerOrder, string readableName = null)
        {
            lifecycle.Assign(screenId, zOrder, layerOrder, readableName ?? "");

            canvasController?.ApplyZOrder(zOrder);
            uiDocumentController?.ApplyZOrder(zOrder);
        }

        public void UnassignFromScreen(string screenId)
        {
            lifecycle.Unassign(screenId);
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (framework == UIFramework.UIToolkit)
                ShowUIToolkit();
            else
            {
                NotifyScreenLifecycleComponents(isShown: true);
                lifecycle.Show();
                canvasController?.Show();
            }
        }

        private void ShowUIToolkit()
        {
            if (uiDocumentController != null)
            {
                UIDocument uiDocument = uiDocumentController.UIDocument;
                if (uiDocument && !uiDocument.enabled)
                    uiDocument.enabled = true;
            }

            NotifyScreenLifecycleComponents(isShown: true);
            lifecycle.Show();
            uiDocumentController?.Show();
        }

        public void Hide()
        {
            NotifyScreenLifecycleComponents(isShown: false);

            lifecycle.Hide();

            if (framework == UIFramework.uGUI)
                canvasController?.Hide();
            else if (framework == UIFramework.UIToolkit)
                uiDocumentController?.Hide();
        }

        public async Task LoadContentAsync(CancellationToken ct = default)
        {
            await contentLifecycle.LoadContentAsync(ct);
        }

        public async Task WaitForLoadingCompleteAsync(CancellationToken ct = default)
        {
            await contentLifecycle.WaitForLoadingCompleteAsync(ct);
        }

        public bool DestroyContent()
        {
            return contentLifecycle?.DestroyContent() ?? false;
        }

        private void NotifyScreenLifecycleComponents(bool isShown)
        {
            try
            {
                if (isShown)
                {
                    // Use only cached components, never recache after Awake
                    for (int i = 0; i < cachedShowComponents.Length; i++)
                    {
                        if (cachedShowComponents[i] == null)
                            continue;

                        try
                        {
                            cachedShowComponents[i].OnScreenShown();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[LayerComponent] {cachedShowComponents[i].GetType().Name}.OnScreenShown() failed: {ex.Message}\n{ex.StackTrace}", gameObject);
                        }
                    }
                }
                else
                {
                    // Use only cached components, never recache after Awake
                    for (int i = 0; i < cachedHideComponents.Length; i++)
                    {
                        if (cachedHideComponents[i] == null)
                            continue;

                        try
                        {
                            cachedHideComponents[i].OnScreenHidden();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[LayerComponent] {cachedHideComponents[i].GetType().Name}.OnScreenHidden() failed: {ex.Message}\n{ex.StackTrace}", gameObject);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LayerComponent] Lifecycle notification failed: {ex.Message}\n{ex.StackTrace}", gameObject);
            }
        }
    }
}
