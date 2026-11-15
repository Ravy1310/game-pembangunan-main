using System;
using System.Collections.Generic;
using UnityEngine;
using CozyGlade.Building;
using CozyGlade.World;
using CozyGlade.Core;
using CozyGlade.Input;

namespace CozyGlade.Tools
{
    public class PathTool : MonoBehaviour, ITool
    {
        public string Id => "Path";

        [Header("Settings")]
        [SerializeField] private PathStyle style = PathStyle.SteppingStones;
        [SerializeField] private float sampleInterval = 0.5f;

        private readonly List<Vector3> _currentPoints = new();
        private bool _isDrawing;
        private bool _isDragging;
        private Vector3 _lastHoverPos;

        private WorldData _worldData;
        private ToolManager _toolManager;
        private InputReader _inputReader;

        public PathStyle Style
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

            if (!_isDrawing)
            {
                if (WorldView.Instance != null)
                {
                    WorldView.Instance.HidePreviewPath();
                }
                return;
            }

            // If dragging, append new points as the pointer moves
            if (_isDragging && _currentPoints.Count > 0)
            {
                Vector3 lastPt = _currentPoints[_currentPoints.Count - 1];
                if (Vector3.Distance(lastPt, pointerWorld) >= sampleInterval)
                {
                    _currentPoints.Add(pointerWorld);
                }
            }

            var previewPath = new PathData
            {
                style = style
            };

            for (int i = 0; i < _currentPoints.Count; i++)
            {
                previewPath.points.Add(_currentPoints[i]);
            }

            if (!_isDragging && _currentPoints.Count > 0)
            {
                if (Vector3.Distance(_currentPoints[_currentPoints.Count - 1], pointerWorld) > 0.05f)
                {
                    previewPath.points.Add(pointerWorld);
                }
            }

            bool isValid = previewPath.points.Count >= 2;
            if (WorldView.Instance != null)
            {
                WorldView.Instance.SetPreviewPath(previewPath, isValid);
            }
        }

        public void OnPressed(Vector3 pointerWorld)
        {
            if (!_isDrawing)
            {
                _currentPoints.Clear();
                _currentPoints.Add(pointerWorld);
                _isDrawing = true;
                _isDragging = true;
            }
            else
            {
                Vector3 lastPt = _currentPoints[_currentPoints.Count - 1];
                if (Vector3.Distance(lastPt, pointerWorld) >= 0.2f)
                {
                    _currentPoints.Add(pointerWorld);
                }
            }
        }

        public void OnReleased(Vector3 pointerWorld)
        {
            if (!_isDrawing) return;

            if (_isDragging)
            {
                _isDragging = false;
                Vector3 lastPt = _currentPoints[_currentPoints.Count - 1];
                if (Vector3.Distance(lastPt, pointerWorld) >= 0.15f)
                {
                    _currentPoints.Add(pointerWorld);
                }

                // If user dragged a path with >= 2 points, commit immediately!
                if (_currentPoints.Count >= 2)
                {
                    CommitPath();
                }
            }
        }

        public void CommitPath()
        {
            if (_currentPoints.Count >= 2)
            {
                var path = new PathData
                {
                    style = style
                };

                for (int i = 0; i < _currentPoints.Count; i++)
                {
                    path.points.Add(_currentPoints[i]);
                }

                if (_worldData != null)
                {
                    var cmd = new BuildPathCommand(_worldData, WorldView.Instance, path);
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

            if (WorldView.Instance != null)
            {
                WorldView.Instance.HidePreviewPath();
            }
        }

        private void HandleConfirm()
        {
            if (_isDrawing)
            {
                CommitPath();
            }
        }

        private void HandleRightClick(Vector2 _)
        {
            if (_isDrawing)
            {
                CommitPath();
            }
        }
    }
}
