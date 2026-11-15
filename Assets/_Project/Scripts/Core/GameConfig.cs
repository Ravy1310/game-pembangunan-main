using UnityEngine;

namespace CozyGlade.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Cozy Glade/Core/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Wall Configuration")]
        [SerializeField] private float defaultWallHeight = 2.4f;
        [SerializeField] private float minWallHeight = 1.5f;
        [SerializeField] private float maxWallHeight = 4.0f;
        [SerializeField] private float defaultWallThickness = 0.28f;
        [SerializeField] private float snapRadius = 0.5f;
        [SerializeField] private float closePolygonThreshold = 0.5f;

        [Header("Roof Configuration")]
        [SerializeField] private float defaultRoofPitch = 35f;
        [SerializeField] private float defaultRoofOverhang = 0.3f;

        [Header("Fence & Path Configuration")]
        [SerializeField] private float fencePostInterval = 1.0f;
        [SerializeField] private float pathStoneInterval = 0.55f;

        [Header("Camera Configuration")]
        [SerializeField] private float cameraMinPitch = 15f;
        [SerializeField] private float cameraMaxPitch = 80f;
        [SerializeField] private float cameraMinDistance = 4f;
        [SerializeField] private float cameraMaxDistance = 45f;
        [SerializeField] private float cameraDefaultDistance = 18f;
        [SerializeField] private float cameraSmoothTime = 0.08f;

        public float DefaultWallHeight => defaultWallHeight;
        public float MinWallHeight => minWallHeight;
        public float MaxWallHeight => maxWallHeight;
        public float DefaultWallThickness => defaultWallThickness;
        public float SnapRadius => snapRadius;
        public float ClosePolygonThreshold => closePolygonThreshold;

        public float DefaultRoofPitch => defaultRoofPitch;
        public float DefaultRoofOverhang => defaultRoofOverhang;

        public float FencePostInterval => fencePostInterval;
        public float PathStoneInterval => pathStoneInterval;

        public float CameraMinPitch => cameraMinPitch;
        public float CameraMaxPitch => cameraMaxPitch;
        public float CameraMinDistance => cameraMinDistance;
        public float CameraMaxDistance => cameraMaxDistance;
        public float CameraDefaultDistance => cameraDefaultDistance;
        public float CameraSmoothTime => cameraSmoothTime;
    }
}
