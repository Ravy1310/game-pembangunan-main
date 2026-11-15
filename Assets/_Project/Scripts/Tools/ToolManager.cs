using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using CozyGlade.Input;
using CozyGlade.CameraControls;
using CozyGlade.Core;

namespace CozyGlade.Tools
{
    public class ToolManager : MonoBehaviour
    {
#pragma warning disable 0649
        [Header("References")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private Camera mainCamera;
#pragma warning restore 0649

        private readonly List<ITool> _tools = new();
        private ITool _activeTool;
        private Vector3 _currentPointerWorld;
        private bool _hasValidPointerWorld;
        private EventSystem _pointerEventSystem;
        private PointerEventData _pointerEventData;
        private readonly List<RaycastResult> _uiRaycastResults = new();

        public CommandHistory History { get; } = new();
        public ITool ActiveTool => _activeTool;
        public IReadOnlyList<ITool> Tools => _tools;
        public InputReader InputReader => inputReader;
        public bool IsFreehandModifierPressed => inputReader != null && inputReader.IsFreehandModifierPressed;
        public Vector3 CurrentPointerWorld => _currentPointerWorld;
        public bool HasValidPointerWorld => _hasValidPointerWorld;

        public event Action<ITool> OnToolChanged;

        private bool _isSubscribed;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        public void SetInputReader(InputReader reader)
        {
            if (inputReader == reader) return;

            UnsubscribeInput();
            inputReader = reader;
            SubscribeInput();
        }

        private void SubscribeInput()
        {
            if (_isSubscribed || !isActiveAndEnabled || inputReader == null) return;

            inputReader.OnPointerMoved += HandlePointerMoved;
            inputReader.OnClickPressed += HandleClickPressed;
            inputReader.OnClickReleased += HandleClickReleased;
            inputReader.OnCancel += HandleCancel;
            inputReader.OnUndo += HandleUndo;
            inputReader.OnRedo += HandleRedo;
            inputReader.OnToolSlotSelected += SelectToolByIndex;
            _isSubscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!_isSubscribed || inputReader == null) return;

            inputReader.OnPointerMoved -= HandlePointerMoved;
            inputReader.OnClickPressed -= HandleClickPressed;
            inputReader.OnClickReleased -= HandleClickReleased;
            inputReader.OnCancel -= HandleCancel;
            inputReader.OnUndo -= HandleUndo;
            inputReader.OnRedo -= HandleRedo;
            inputReader.OnToolSlotSelected -= SelectToolByIndex;
            _isSubscribed = false;
        }

        public void RegisterTool(ITool tool)
        {
            if (tool == null || _tools.Contains(tool)) return;
            _tools.Add(tool);

            if (_activeTool == null)
            {
                SelectTool(tool);
            }
        }

        public void SelectTool(ITool tool)
        {
            if (_activeTool == tool) return;

            _activeTool?.OnExit();
            _activeTool = tool;
            _activeTool?.OnEnter();

            if (_hasValidPointerWorld)
            {
                _activeTool?.Tick(_currentPointerWorld);
            }

            OnToolChanged?.Invoke(_activeTool);
        }

        public void SelectToolByIndex(int index)
        {
            if (index >= 0 && index < _tools.Count)
            {
                SelectTool(_tools[index]);
            }
        }

        public void SelectToolById(string id)
        {
            for (int i = 0; i < _tools.Count; i++)
            {
                if (_tools[i].Id == id)
                {
                    SelectTool(_tools[i]);
                    return;
                }
            }
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            if (inputReader != null)
            {
                _hasValidPointerWorld = GroundRaycaster.TryGetPoint(mainCamera, inputReader.PointerPosition, out _currentPointerWorld);
                if (_hasValidPointerWorld && _activeTool != null)
                {
                    _activeTool.Tick(_currentPointerWorld);
                }
            }
        }

        private void HandlePointerMoved(Vector2 screenPos)
        {
            if (mainCamera == null) return;
            _hasValidPointerWorld = GroundRaycaster.TryGetPoint(mainCamera, screenPos, out _currentPointerWorld);
            if (_hasValidPointerWorld && _activeTool != null)
            {
                _activeTool.Tick(_currentPointerWorld);
            }
        }

        private void HandleClickPressed(Vector2 screenPos)
        {
            if (_activeTool == null) return;

            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                if (_pointerEventSystem != eventSystem)
                {
                    _pointerEventSystem = eventSystem;
                    _pointerEventData = new PointerEventData(eventSystem);
                }

                _pointerEventData.Reset();
                _pointerEventData.position = screenPos;
                _uiRaycastResults.Clear();
                eventSystem.RaycastAll(_pointerEventData, _uiRaycastResults);
                if (_uiRaycastResults.Count > 0) return;
            }

            if (mainCamera != null &&
                GroundRaycaster.TryGetPoint(mainCamera, screenPos, out _currentPointerWorld))
            {
                _hasValidPointerWorld = true;
                _activeTool.OnPressed(_currentPointerWorld);
            }
        }

        private void HandleClickReleased(Vector2 screenPos)
        {
            if (_activeTool != null && mainCamera != null &&
                GroundRaycaster.TryGetPoint(mainCamera, screenPos, out _currentPointerWorld))
            {
                _hasValidPointerWorld = true;
                _activeTool.OnReleased(_currentPointerWorld);
            }
        }

        private void HandleCancel()
        {
            _activeTool?.OnCancel();
        }

        private void HandleUndo()
        {
            History.Undo();
        }

        private void HandleRedo()
        {
            History.Redo();
        }
    }
}
