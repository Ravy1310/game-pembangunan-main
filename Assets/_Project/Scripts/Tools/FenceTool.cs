using System;
using System.Collections.Generic;
using UnityEngine;
using CozyGlade.Building;
using CozyGlade.World;
using CozyGlade.Core;
using CozyGlade.Input;

namespace CozyGlade.Tools
{
    public class FenceTool : MonoBehaviour, ITool
    {
        public string Id => "Fence";

        [Header("Settings")]
        [SerializeField] private FenceStyle style = FenceStyle.WoodPostRail;
        [SerializeField] private float minCommitDistance = 0.5f;

        private readonly List<Vector3> _currentPoints = new();
        private bool _isDrawing;
        private bool _isDragging;
        private bool _isClosed;
        private Vector3 _startDragPos;
        private Vector3 _lastHoverPos;

        private WorldData _worldData;
        private ToolManager _toolManager;
        private InputReader _inputReader;

        public FenceStyle Style
        {
            get => style;
            set => style = value;
        }

        public bool IsDrawing => _isDrawing;

        private void Awake()
        {
            _toolManager = GetComponent<ToolManager>() ?? FindAnyObjectByType<ToolManager>();
        }

        public void Initialize(WorldData worldData, ToolManager toolManager)
        {
            _worldData = worldData;
            _toolManager = toolManager;
            SetInputReader(toolManager != null ? toolManager.InputReader : null);
        }

        private void SetInputReader(InputReader inputReader)
        {
            if (_inputReader == inputReader) return;

            if (_inputReader != null)
            {
                _inputReader.OnConfirm -= HandleConfirm;
                _inputReader.OnRightClickPressed -= HandleRightClick;
            }

            _inputReader = inputReader;

            if (_inputReader != null)
            {
                _inputReader.OnConfirm += HandleConfirm;
                _inputReader.OnRightClickPressed += HandleRightClick;
            }
        }

        private void OnDestroy()
        {
            SetInputReader(null);
        }

        public void OnEnter()
        {
            ResetDrawing();
        }

        public void OnExit()
        {
            ResetDrawing();
        }

        public void Tick(Vector3 pointerWorld)
        {
            _lastHoverPos = pointerWorld;

            if (!_isDrawing || _currentPoints.Count == 0)
            {
                if (WorldView.Instance != null)
                {
                    WorldView.Instance.HidePreviewFence();
                }
                return;
            }

            bool isCtrlHeld = _toolManager != null && _toolManager.IsFreehandModifierPressed;

            Vector3? lastPt = _currentPoints.Count > 0 ? _currentPoints[_currentPoints.Count - 1] : null;
            Vector3? firstPt = _currentPoints.Count > 0 ? _currentPoints[0] : null;

            IReadOnlyList<WallData> existingWalls = _worldData != null ? _worldData.walls : null;

            SnapResult snap = WallSnapper.Snap(
                pointerWorld,
                lastPt,
                firstPt,
                _currentPoints.Count,
                existingWalls,
                null,
                isCtrlHeld);

            var previewFence = new FenceData
            {
                style = style,
                closed = snap.IsClosingPolygon
            };

            for (int i = 0; i < _currentPoints.Count; i++)
            {
                previewFence.points.Add(_currentPoints[i]);
            }

            if (!snap.IsClosingPolygon)
            {
                if (Vector3.Distance(_currentPoints[_currentPoints.Count - 1], snap.Position) > 0.05f)
                {
                    previewFence.points.Add(snap.Position);
                }
            }

            bool isValid = previewFence.points.Count >= 2;
            if (WorldView.Instance != null)
            {
                WorldView.Instance.SetPreviewFence(previewFence, isValid);
            }
        }

        public void OnPressed(Vector3 pointerWorld)
        {
            bool isCtrlHeld = _toolManager != null && _toolManager.IsFreehandModifierPressed;
            IReadOnlyList<WallData> existingWalls = _worldData != null ? _worldData.walls : null;

            if (!_isDrawing)
            {
                SnapResult snap = WallSnapper.Snap(
                    pointerWorld,
                    null,
                    null,
                    0,
                    existingWalls,
                    null,
                    isCtrlHeld);

                _currentPoints.Clear();
                _currentPoints.Add(snap.Position);
                _startDragPos = snap.Position;
                _isDrawing = true;
                _isDragging = true;
                _isClosed = false;
            }
            else
            {
                Vector3 lastPt = _currentPoints[_currentPoints.Count - 1];
                Vector3 firstPt = _currentPoints[0];

                SnapResult snap = WallSnapper.Snap(
                    pointerWorld,
                    lastPt,
                    firstPt,
                    _currentPoints.Count,
                    existingWalls,
                    null,
                    isCtrlHeld);

                if (snap.IsClosingPolygon)
                {
                    _isClosed = true;
                    CommitFence();
                }
                else if (Vector3.Distance(lastPt, snap.Position) >= 0.2f)
                {
                    _currentPoints.Add(snap.Position);
                }
            }
        }

        public void OnReleased(Vector3 pointerWorld)
        {
            if (!_isDrawing) return;

            bool isCtrlHeld = _toolManager != null && _toolManager.IsFreehandModifierPressed;
            IReadOnlyList<WallData> existingWalls = _worldData != null ? _worldData.walls : null;

            Vector3 lastPt = _currentPoints[_currentPoints.Count - 1];
            Vector3 firstPt = _currentPoints[0];

            SnapResult snap = WallSnapper.Snap(
                pointerWorld,
                lastPt,
                firstPt,
                _currentPoints.Count,
                existingWalls,
                null,
                isCtrlHeld);

            if (_isDragging)
            {
                _isDragging = false;
                float dragDist = Vector3.Distance(_startDragPos, snap.Position);

                if (dragDist >= minCommitDistance)
                {
                    if (snap.IsClosingPolygon)
                    {
                        _isClosed = true;
                    }
                    else if (Vector3.Distance(_currentPoints[_currentPoints.Count - 1], snap.Position) >= 0.2f)
                    {
                        _currentPoints.Add(snap.Position);
                    }
                    CommitFence();
                }
            }
        }

        public void CommitFence()
        {
            if (_currentPoints.Count >= 2)
            {
                var fence = new FenceData
                {
                    style = style,
                    closed = _isClosed
                };

                for (int i = 0; i < _currentPoints.Count; i++)
                {
                    fence.points.Add(_currentPoints[i]);
                }

                if (_worldData != null)
                {
                    var cmd = new BuildFenceCommand(_worldData, WorldView.Instance, fence);
                    if (_toolManager != null)
                    {
                        _toolManager.History.Execute(cmd);
                    }
                    else
                    {
                        cmd.Do();
                    }
                }
            }

            ResetDrawing();
        }

        public void OnCancel()
        {
            ResetDrawing();
        }

        private void ResetDrawing()
        {
            _currentPoints.Clear();
            _isDrawing = false;
            _isDragging = false;
            _isClosed = false;

            if (WorldView.Instance != null)
            {
                WorldView.Instance.HidePreviewFence();
            }
        }

        private void HandleConfirm()
        {
            if (_isDrawing)
            {
                CommitFence();
            }
        }

        private void HandleRightClick(Vector2 _)
        {
            if (_isDrawing)
            {
                CommitFence();
            }
        }
    }
}
