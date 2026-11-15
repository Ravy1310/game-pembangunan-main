using UnityEngine;
using CozyGlade.Input;

namespace CozyGlade.CameraControls
{
    public class CameraRig : MonoBehaviour
    {
#pragma warning disable 0649
        [Header("References")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform pivotTarget;

        [Header("Orbit Settings")]
        [SerializeField] private float orbitSpeed = 0.25f;
        [SerializeField] private float minPitch = 15f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float defaultPitch = 45f;
        [SerializeField] private float defaultYaw = 45f;

        [Header("Pan Settings")]
        [SerializeField] private float basePanSpeed = 0.015f;
        [SerializeField] private Vector2 panBounds = new Vector2(40f, 40f);

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSensitivity = 3f;
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 45f;
        [SerializeField] private float defaultDistance = 18f;

        [Header("Smoothing")]
        [SerializeField] private float smoothTime     = 0.08f;
        [SerializeField] private float zoomSmoothTime = 0.06f;

        // Target state
        private Vector3 _targetPivot;
        private float _targetYaw;
        private float _targetPitch;
        private float _targetDistance;

        // Current smoothed state
        private Vector3 _currentPivot;
        private float _currentYaw;
        private float _currentPitch;
        private float _currentDistance;

        // SmoothDamp velocities (zero allocation)
        private Vector3 _pivotVelocity;
        private float _yawVelocity;
        private float _pitchVelocity;
        private float _distanceVelocity;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponentInChildren<Camera>();
                if (targetCamera == null)
                {
                    targetCamera = Camera.main;
                }
            }

            _targetPivot = pivotTarget != null ? pivotTarget.position : Vector3.zero;
            _targetYaw = defaultYaw;
            _targetPitch = defaultPitch;
            _targetDistance = defaultDistance;

            _currentPivot = _targetPivot;
            _currentYaw = _targetYaw;
            _currentPitch = _targetPitch;
            _currentDistance = _targetDistance;

            UpdateCameraTransformImmediate();
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.OnPointerDelta += HandlePointerDelta;
                inputReader.OnScroll += HandleScroll;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.OnPointerDelta -= HandlePointerDelta;
                inputReader.OnScroll -= HandleScroll;
            }
        }

        public void SetInputReader(InputReader reader)
        {
            if (inputReader != null)
            {
                inputReader.OnPointerDelta -= HandlePointerDelta;
                inputReader.OnScroll -= HandleScroll;
            }

            inputReader = reader;

            if (inputReader != null && isActiveAndEnabled)
            {
                inputReader.OnPointerDelta += HandlePointerDelta;
                inputReader.OnScroll += HandleScroll;
            }
        }

        private void HandlePointerDelta(Vector2 delta)
        {
            if (inputReader == null) return;

            bool isOrbiting = (inputReader.IsRightClickPressed || inputReader.IsMiddleClickPressed) && !inputReader.IsPanModifierPressed;
            bool isPanning = (inputReader.IsRightClickPressed || inputReader.IsMiddleClickPressed) && inputReader.IsPanModifierPressed;

            if (isOrbiting)
            {
                _targetYaw += delta.x * orbitSpeed;
                _targetPitch = Mathf.Clamp(_targetPitch - delta.y * orbitSpeed, minPitch, maxPitch);
            }
            else if (isPanning)
            {
                // Pan horizontally relative to camera azimuth
                Quaternion horizontalRot = Quaternion.Euler(0f, _currentYaw, 0f);
                Vector3 right = horizontalRot * Vector3.right;
                Vector3 forward = horizontalRot * Vector3.forward;

                float speedFactor = basePanSpeed * (_currentDistance / 15f);
                Vector3 move = (-right * delta.x - forward * delta.y) * speedFactor;

                _targetPivot += move;
                _targetPivot.x = Mathf.Clamp(_targetPivot.x, -panBounds.x, panBounds.x);
                _targetPivot.z = Mathf.Clamp(_targetPivot.z, -panBounds.y, panBounds.y);
            }
        }

        private void HandleScroll(float scrollDelta)
        {
            // Zoom speed proporsional dengan jarak saat ini — semakin jauh semakin cepat
            float speedFactor = _targetDistance * 0.1f;
            float zoomChange  = -scrollDelta * zoomSensitivity * speedFactor;
            _targetDistance   = Mathf.Clamp(_targetDistance + zoomChange, minDistance, maxDistance);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Smooth position & rotation (Zero GC alloc)
            _currentPivot    = Vector3.SmoothDamp(_currentPivot, _targetPivot, ref _pivotVelocity, smoothTime);
            _currentYaw      = Mathf.SmoothDampAngle(_currentYaw, _targetYaw, ref _yawVelocity, smoothTime);
            _currentPitch    = Mathf.SmoothDamp(_currentPitch, _targetPitch, ref _pitchVelocity, smoothTime);
            // Zoom pakai smoothTime terpisah agar lebih responsif
            _currentDistance = Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _distanceVelocity, zoomSmoothTime);

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            Quaternion rot = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Vector3 pos = _currentPivot - rot * Vector3.forward * _currentDistance;

            if (targetCamera != null)
            {
                targetCamera.transform.position = pos;
                targetCamera.transform.rotation = rot;
            }
            else
            {
                transform.position = pos;
                transform.rotation = rot;
            }

            if (pivotTarget != null)
            {
                pivotTarget.position = _currentPivot;
            }
        }

        public void UpdateCameraTransformImmediate()
        {
            _currentPivot = _targetPivot;
            _currentYaw = _targetYaw;
            _currentPitch = _targetPitch;
            _currentDistance = _targetDistance;
            ApplyTransform();
        }

        public void FocusOn(Vector3 point, float? distance = null)
        {
            _targetPivot = point;
            _targetPivot.y = 0f;
            if (distance.HasValue)
            {
                _targetDistance = Mathf.Clamp(distance.Value, minDistance, maxDistance);
            }
        }
#pragma warning restore 0649
    }
}
