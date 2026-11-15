using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using CozyGlade.Core;
using CozyGlade.Tools;

namespace CozyGlade.UI
{
    public class ToolbarUI : MonoBehaviour
    {
        public static ToolbarUI Instance { get; private set; }

        public static ToolbarUI Create(ToolManager toolManager)
        {
            var go = new GameObject("--- ToolbarUI ---");
            var ui = go.AddComponent<ToolbarUI>();
            ui.Initialize(toolManager);
            return ui;
        }

        private class CozyButtonView
        {
            public Button button;
            public RectTransform rect;
            public Image bgImage;
            public Image iconImage;
            public Image highlight;
            public Text labelText;
        }

        private ToolManager _toolManager;
        private readonly Dictionary<string, CozyButtonView> _toolButtons = new();
        private Button _undoButton;
        private Button _redoButton;

        private Sprite _panelSprite;
        private Sprite _btnNormalSprite;
        private Sprite _btnHoverSprite;
        private Sprite _btnPressedSprite;
        private readonly Dictionary<string, Sprite> _iconSprites = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ensure any canvas under ToolbarUI is strictly ScreenSpaceOverlay
            var canvases = GetComponentsInChildren<Canvas>(true);
            foreach (var c in canvases)
            {
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 100;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(ToolManager toolManager)
        {
            _toolManager = toolManager;

            LoadUiSprites();
            BuildCanvasAndToolbar();

            if (_toolManager != null)
            {
                _toolManager.OnToolChanged += HandleToolChanged;
                _toolManager.History.OnHistoryChanged += UpdateUndoRedoState;
                if (_toolManager.ActiveTool != null)
                {
                    HandleToolChanged(_toolManager.ActiveTool);
                }
            }

            UpdateUndoRedoState();
        }

        private void LoadUiSprites()
        {
            _panelSprite = LoadSpriteSafe("Assets/_Project/Art/UI/Panels/ui_panel_9slice.png", new Vector4(36, 50, 36, 36));
            _btnNormalSprite = LoadSpriteSafe("Assets/_Project/Art/UI/Panels/ui_button_normal_9slice.png", new Vector4(32, 40, 32, 30));
            _btnHoverSprite = LoadSpriteSafe("Assets/_Project/Art/UI/Panels/ui_button_hover_9slice.png", new Vector4(32, 40, 32, 30));
            _btnPressedSprite = LoadSpriteSafe("Assets/_Project/Art/UI/Panels/ui_button_pressed_9slice.png", new Vector4(32, 40, 32, 30));

            string[] icons = { "wall", "fence", "path", "prop", "erase", "undo", "redo" };
            foreach (var icon in icons)
            {
                var s = LoadSpriteSafe($"Assets/_Project/Art/UI/Icons/ui_icon_{icon}.png");
                if (s != null)
                {
                    _iconSprites[icon] = s;
                }
                else
                {
                    Debug.LogWarning($"[ToolbarUI] Could not load sprite for icon: ui_icon_{icon}.png");
                }
            }

            if (!_iconSprites.ContainsKey("road") && _iconSprites.TryGetValue("path", out var pathIcon))
            {
                _iconSprites["road"] = pathIcon;
            }
        }

        private Sprite LoadSpriteSafe(string assetPath, Vector4? border = null)
        {
#if UNITY_EDITOR
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null) return sprite;
#endif
            // Standalone or runtime fallback: load PNG directly from disk
            string fullPath = Path.Combine(Application.dataPath, assetPath.StartsWith("Assets/") ? assetPath.Substring(7) : assetPath);
            if (File.Exists(fullPath))
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    Vector4 b = border ?? Vector4.zero;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, b);
                }
            }
            return null;
        }

        private void BuildCanvasAndToolbar()
        {
            // Ensure EventSystem with InputSystemUIInputModule
            var es = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                es = esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }

            var legacyModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (legacyModule != null)
            {
                Destroy(legacyModule);
            }

            if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Ensure Canvas
            var canvasGo = new GameObject("Canvas_CozyUI");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Main Toolbar Container (bottom center)
            var barGo = new GameObject("ToolbarPanel");
            barGo.transform.SetParent(canvasGo.transform, false);

            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0f);
            barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 25f);
            barRect.sizeDelta = new Vector2(760f, 96f);

            var barImg = barGo.AddComponent<Image>();
            if (_panelSprite != null)
            {
                barImg.sprite = _panelSprite;
                barImg.type = Image.Type.Sliced;
            }
            barImg.color = new Color(0.97f, 0.93f, 0.85f, 0.96f); // Cozy Cream #F7EDD8

            var layout = barGo.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 1. Tool Buttons: Wall, Road, Fence, Path, Prop, Erase
            string[] toolIds = { "Wall", "Road", "Fence", "Path", "Prop", "Erase" };
            string[] toolIcons = { "wall", "road", "fence", "path", "prop", "erase" };
            string[] toolLabels = { "Wall", "Road", "Fence", "Path", "Prop", "Erase" };

            for (int i = 0; i < toolIds.Length; i++)
            {
                string id = toolIds[i];
                string iconKey = toolIcons[i];
                string label = toolLabels[i];

                var btnObj = CreateCozyButton(barGo.transform, id, iconKey, label);
                _toolButtons[id] = btnObj;

                btnObj.button.onClick.AddListener(() =>
                {
                    if (_toolManager != null)
                    {
                        _toolManager.SelectToolById(id);
                    }
                });
            }

            // Separator line
            var sepGo = new GameObject("Separator");
            sepGo.transform.SetParent(barGo.transform, false);
            var sepRect = sepGo.AddComponent<RectTransform>();
            sepRect.sizeDelta = new Vector2(2f, 54f);
            var sepImg = sepGo.AddComponent<Image>();
            sepImg.color = new Color(0.55f, 0.42f, 0.32f, 0.4f);

            // 2. Undo / Redo Buttons
            var undoObj = CreateCozyButton(barGo.transform, "Undo", "undo", "Undo");
            _undoButton = undoObj.button;
            _undoButton.onClick.AddListener(() => _toolManager?.History.Undo());

            var redoObj = CreateCozyButton(barGo.transform, "Redo", "redo", "Redo");
            _redoButton = redoObj.button;
            _redoButton.onClick.AddListener(() => _toolManager?.History.Redo());
        }

        private CozyButtonView CreateCozyButton(Transform parent, string name, string iconKey, string labelText)
        {
            var btnGo = new GameObject($"Btn_{name}");
            btnGo.transform.SetParent(parent, false);

            var rect = btnGo.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(72f, 72f);

            var bgImg = btnGo.AddComponent<Image>();
            if (_btnNormalSprite != null)
            {
                bgImg.sprite = _btnNormalSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = Color.white;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            if (_btnNormalSprite != null && _btnHoverSprite != null && _btnPressedSprite != null)
            {
                btn.transition = Selectable.Transition.SpriteSwap;
                var ss = new SpriteState
                {
                    highlightedSprite = _btnHoverSprite,
                    pressedSprite = _btnPressedSprite,
                    selectedSprite = _btnHoverSprite
                };
                btn.spriteState = ss;
            }
            else
            {
                btn.transition = Selectable.Transition.ColorTint;
                var cb = btn.colors;
                cb.highlightedColor = new Color(1f, 0.95f, 0.85f);
                cb.pressedColor = new Color(0.85f, 0.8f, 0.7f);
                btn.colors = cb;
            }

            // Highlight ring (active tool state glow)
            var hlGo = new GameObject("ActiveGlow");
            hlGo.transform.SetParent(btnGo.transform, false);
            var hlRect = hlGo.AddComponent<RectTransform>();
            hlRect.anchorMin = Vector2.zero;
            hlRect.anchorMax = Vector2.one;
            hlRect.sizeDelta = new Vector2(6f, 6f);
            var hlImg = hlGo.AddComponent<Image>();
            if (_btnHoverSprite != null)
            {
                hlImg.sprite = _btnHoverSprite;
                hlImg.type = Image.Type.Sliced;
            }
            hlImg.color = new Color(0.96f, 0.65f, 0.28f, 0.85f); // Warm gold glow
            hlGo.SetActive(false);

            // Separate child Icon Image (size 44x44, preserveAspect = true, color = Color.white)
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(btnGo.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 7f);
            iconRect.sizeDelta = new Vector2(44f, 44f);

            var iconImg = iconGo.AddComponent<Image>();
            iconImg.preserveAspect = true;
            if (_iconSprites.TryGetValue(iconKey, out var iconSprite) && iconSprite != null)
            {
                iconImg.sprite = iconSprite;
                iconImg.color = Color.white;
            }
            else
            {
                iconImg.color = Color.clear;
            }

            // Label text below icon
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = new Vector2(0f, 6f);
            textRect.sizeDelta = new Vector2(0f, 16f);

            var text = textGo.AddComponent<Text>();
            text.text = labelText;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") 
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                        ?? Font.CreateDynamicFontFromOSFont("Arial", 11)
                        ?? Font.CreateDynamicFontFromOSFont("Segoe UI", 11);
            text.fontSize = 11;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.35f, 0.24f, 0.17f, 1f); // Cozy brown #5A3E2B

            return new CozyButtonView
            {
                button = btn,
                rect = rect,
                bgImage = bgImg,
                iconImage = iconImg,
                highlight = hlImg,
                labelText = text
            };
        }

        private void HandleToolChanged(ITool activeTool)
        {
            if (activeTool == null) return;

            foreach (var kvp in _toolButtons)
            {
                bool isActive = kvp.Key.Equals(activeTool.Id, System.StringComparison.OrdinalIgnoreCase);
                var view = kvp.Value;
                if (view != null)
                {
                    if (view.highlight != null)
                    {
                        view.highlight.gameObject.SetActive(isActive);
                    }

                    // Active tool button pops up slightly (1.08x scale)
                    if (view.rect != null)
                    {
                        view.rect.localScale = isActive ? Vector3.one * 1.08f : Vector3.one;
                    }

                    if (view.bgImage != null && _btnNormalSprite != null && _btnHoverSprite != null)
                    {
                        view.bgImage.sprite = isActive ? _btnHoverSprite : _btnNormalSprite;
                    }
                }
            }
        }

        private void UpdateUndoRedoState()
        {
            if (_toolManager == null) return;

            if (_undoButton != null)
            {
                _undoButton.interactable = _toolManager.History.CanUndo;
            }
            if (_redoButton != null)
            {
                _redoButton.interactable = _toolManager.History.CanRedo;
            }
        }
    }
}
