using UnityEngine;

namespace CozyGlade.CameraControls
{
    public static class GroundRaycaster
    {
        private const float PlaneY = 0f;
        private const float Epsilon = 1e-5f;

        /// <summary>
        /// Projects a screen position onto the horizontal plane y = 0.
        /// Zero GC allocations per frame.
        /// </summary>
        public static bool TryGetPoint(Camera camera, Vector2 screenPos, out Vector3 point)
        {
            point = Vector3.zero;
            if (camera == null) return false;

            Ray ray = camera.ScreenPointToRay(screenPos);
            if (Mathf.Abs(ray.direction.y) < Epsilon)
            {
                return false;
            }

            float t = (PlaneY - ray.origin.y) / ray.direction.y;
            if (t < 0f)
            {
                return false;
            }

            point = ray.origin + ray.direction * t;
            point.y = PlaneY;
            return true;
        }

        public static bool TryGetPoint(Camera camera, Vector2 screenPos, float groundHeight, out Vector3 point)
        {
            point = Vector3.zero;
            if (camera == null) return false;

            Ray ray = camera.ScreenPointToRay(screenPos);
            if (Mathf.Abs(ray.direction.y) < Epsilon)
            {
                return false;
            }

            float t = (groundHeight - ray.origin.y) / ray.direction.y;
            if (t < 0f)
            {
                return false;
            }

            point = ray.origin + ray.direction * t;
            point.y = groundHeight;
            return true;
        }
    }
}
