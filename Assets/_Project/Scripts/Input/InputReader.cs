using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozyGlade.Input
{
    [CreateAssetMenu(fileName = "InputReader", menuName = "Cozy Glade/Input/Input Reader")]
    public class InputReader : ScriptableObject
    {
#pragma warning disable 0649
        [SerializeField] private InputActionAsset inputAsset;
#pragma warning restore 0649

        private InputActionMap _gameplayMap;
        private InputAction _pointerAction;
        private InputAction _pointerDeltaAction;
        private InputAction _clickAction;
        private InputAction _rightClickAction;
        private InputAction _middleClickAction;
        private InputAction _scrollAction;
        private InputAction _panModifierAction;
        private InputAction _cancelAction;
        private InputAction _confirmAction;
        private InputAction _undoAction;
        private InputAction _redoAction;
        private InputAction _freehandModifierAction;
        private InputAction[] _toolSlotActions;

        public Vector2 PointerPosition { get; private set; }
        public Vector2 PointerDelta { get; private set; }
        public bool IsFreehandModifierPressed { get; private set; }
        public bool IsClickPressed { get; private set; }
        public bool IsRightClickPressed { get; private set; }
        public bool IsMiddleClickPressed { get; private set; }
        public bool IsPanModifierPressed { get; private set; }

        public event Action<Vector2> OnPointerMoved;
        public event Action<Vector2> OnPointerDelta;
        public event Action<Vector2> OnClickPressed;
        public event Action<Vector2> OnClickReleased;
        public event Action<Vector2> OnRightClickPressed;
        public event Action<Vector2> OnRightClickReleased;
        public event Action<Vector2> OnMiddleClickPressed;
        public event Action<Vector2> OnMiddleClickReleased;
        public event Action<float> OnScroll;
        public event Action OnCancel;
        public event Action OnConfirm;
        public event Action OnUndo;
        public event Action OnRedo;
        public event Action<int> OnToolSlotSelected;

        public void Initialize(InputActionAsset asset = null)
        {
            if (asset != null)
            {
                inputAsset = asset;
            }

            if (inputAsset == null)
            {
                Debug.LogWarning("[InputReader] InputActionAsset is null. Call Initialize(asset) or assign in inspector.");
                return;
            }

            _gameplayMap = inputAsset.FindActionMap("Gameplay");
            if (_gameplayMap == null)
            {
                Debug.LogError("[InputReader] 'Gameplay' action map not found.");
                return;
            }

            _pointerAction = _gameplayMap.FindAction("Pointer");
            _pointerDeltaAction = _gameplayMap.FindAction("PointerDelta");
            _clickAction = _gameplayMap.FindAction("Click");
            _rightClickAction = _gameplayMap.FindAction("RightClick");
            _middleClickAction = _gameplayMap.FindAction("MiddleClick");
            _scrollAction = _gameplayMap.FindAction("Scroll");
            _panModifierAction = _gameplayMap.FindAction("PanModifier");
            _cancelAction = _gameplayMap.FindAction("Cancel");
            _confirmAction = _gameplayMap.FindAction("Confirm");
            _undoAction = _gameplayMap.FindAction("Undo");
            _redoAction = _gameplayMap.FindAction("Redo");
            _freehandModifierAction = _gameplayMap.FindAction("FreehandModifier");
            if (_freehandModifierAction == null)
            {
                _freehandModifierAction = new InputAction("FreehandModifier", InputActionType.Button);
                _freehandModifierAction.AddBinding("<Keyboard>/leftCtrl");
                _freehandModifierAction.AddBinding("<Keyboard>/rightCtrl");
            }

            _toolSlotActions = new InputAction[5];
            for (int i = 0; i < 5; i++)
            {
                _toolSlotActions[i] = _gameplayMap.FindAction($"ToolSlot{i + 1}");
            }

            RegisterCallbacks();
            EnableInput();
        }

        public void EnableInput()
        {
            _gameplayMap?.Enable();
            if (_freehandModifierAction != null && _freehandModifierAction.actionMap == null)
            {
                _freehandModifierAction.Enable();
            }
        }

        public void DisableInput()
        {
            _gameplayMap?.Disable();
            if (_freehandModifierAction != null && _freehandModifierAction.actionMap == null)
            {
                _freehandModifierAction.Disable();
            }
            IsFreehandModifierPressed = false;
        }

        private void RegisterCallbacks()
        {
            if (_pointerAction != null)
            {
                _pointerAction.performed += ctx =>
                {
                    PointerPosition = ctx.ReadValue<Vector2>();
                    OnPointerMoved?.Invoke(PointerPosition);
                };
            }

            if (_pointerDeltaAction != null)
            {
                _pointerDeltaAction.performed += ctx =>
                {
                    PointerDelta = ctx.ReadValue<Vector2>();
                    OnPointerDelta?.Invoke(PointerDelta);
                };
            }

            if (_clickAction != null)
            {
                _clickAction.started += _ =>
                {
                    IsClickPressed = true;
                    OnClickPressed?.Invoke(ReadPointerPosition());
                };
                _clickAction.canceled += _ =>
                {
                    IsClickPressed = false;
                    OnClickReleased?.Invoke(ReadPointerPosition());
                };
            }

            if (_rightClickAction != null)
            {
                _rightClickAction.started += _ =>
                {
                    IsRightClickPressed = true;
                    OnRightClickPressed?.Invoke(ReadPointerPosition());
                };
                _rightClickAction.canceled += _ =>
                {
                    IsRightClickPressed = false;
                    OnRightClickReleased?.Invoke(ReadPointerPosition());
                };
            }

            if (_middleClickAction != null)
            {
                _middleClickAction.started += _ =>
                {
                    IsMiddleClickPressed = true;
                    OnMiddleClickPressed?.Invoke(ReadPointerPosition());
                };
                _middleClickAction.canceled += _ =>
                {
                    IsMiddleClickPressed = false;
                    OnMiddleClickReleased?.Invoke(ReadPointerPosition());
                };
            }

            if (_scrollAction != null)
            {
                _scrollAction.performed += ctx =>
                {
                    Vector2 scroll = ctx.ReadValue<Vector2>();
                    if (Mathf.Abs(scroll.y) > 0.001f)
                    {
                        OnScroll?.Invoke(scroll.y);
                    }
                };
            }

            if (_panModifierAction != null)
            {
                _panModifierAction.started += _ => IsPanModifierPressed = true;
                _panModifierAction.canceled += _ => IsPanModifierPressed = false;
            }

            if (_cancelAction != null)
            {
                _cancelAction.performed += _ => OnCancel?.Invoke();
            }

            if (_confirmAction != null)
            {
                _confirmAction.performed += _ => OnConfirm?.Invoke();
            }

            if (_undoAction != null)
            {
                _undoAction.performed += _ => OnUndo?.Invoke();
            }

            if (_redoAction != null)
            {
                _redoAction.performed += _ => OnRedo?.Invoke();
            }

            if (_freehandModifierAction != null)
            {
                _freehandModifierAction.started += _ => IsFreehandModifierPressed = true;
                _freehandModifierAction.canceled += _ => IsFreehandModifierPressed = false;
            }

            if (_toolSlotActions != null)
            {
                for (int i = 0; i < _toolSlotActions.Length; i++)
                {
                    int slotIndex = i;
                    var action = _toolSlotActions[i];
                    if (action != null)
                    {
                        action.performed += _ => OnToolSlotSelected?.Invoke(slotIndex);
                    }
                }
            }
        }

        private Vector2 ReadPointerPosition()
        {
            if (Pointer.current != null)
            {
                PointerPosition = Pointer.current.position.ReadValue();
            }

            return PointerPosition;
        }

        private void OnDisable()
        {
            DisableInput();
        }
    }
}
