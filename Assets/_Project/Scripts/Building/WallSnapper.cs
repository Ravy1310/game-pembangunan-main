using System.Collections.Generic;
using UnityEngine;

namespace CozyGlade.Building
{
    public struct SnapResult
    {
        public Vector3 Position;
        public bool IsEndpointSnapped;
        public bool IsAngleSnapped;
        public bool IsClosingPolygon;
        public float SnappedAngle;
    }

    public static class WallSnapper
    {
        public const float DefaultSnapRadius = 0.5f;
        public const float LengthSnapStep = 0.25f;

        public static SnapResult Snap(
            Vector3 rawPos,
            Vector3? lastPoint,
            Vector3? currentPolylineStart,
            int currentPolylinePointCount,
            IReadOnlyList<WallData> existingWalls,
            string currentWallId,
            bool isFreehand,
            float snapRadius = DefaultSnapRadius)
        {
            var result = new SnapResult
            {
                Position = new Vector3(rawPos.x, 0f, rawPos.z),
                IsEndpointSnapped = false,
                IsAngleSnapped = false,
                IsClosingPolygon = false,
                SnappedAngle = 0f
            };

            // Holding Ctrl disables snapping completely
            if (isFreehand)
            {
                return result;
            }

            // 1. Check closing current polyline (polygon close)
            if (currentPolylineStart.HasValue && currentPolylinePointCount >= 3)
            {
                float distToStart = Vector3.Distance(rawPos, currentPolylineStart.Value);
                if (distToStart <= snapRadius)
                {
                    result.Position = currentPolylineStart.Value;
                    result.IsEndpointSnapped = true;
                    result.IsClosingPolygon = true;
                    return result;
                }
            }

            // 2. Check existing wall endpoints
            if (existingWalls != null)
            {
                float closestDist = snapRadius;
                Vector3 closestEndpoint = Vector3.zero;
                bool foundEndpoint = false;

                for (int i = 0; i < existingWalls.Count; i++)
                {
                    var wall = existingWalls[i];
                    if (wall == null || wall.id == currentWallId || wall.points.Count < 2) continue;

                    Vector3 pFirst = wall.points[0];
                    Vector3 pLast = wall.points[wall.points.Count - 1];

                    float dFirst = Vector3.Distance(rawPos, pFirst);
                    if (dFirst < closestDist)
                    {
                        closestDist = dFirst;
                        closestEndpoint = pFirst;
                        foundEndpoint = true;
                    }

                    if (!wall.closed)
                    {
                        float dLast = Vector3.Distance(rawPos, pLast);
                        if (dLast < closestDist)
                        {
                            closestDist = dLast;
                            closestEndpoint = pLast;
                            foundEndpoint = true;
                        }
                    }
                }

                if (foundEndpoint)
                {
                    result.Position = closestEndpoint;
                    result.IsEndpointSnapped = true;
                    return result;
                }
            }

            // 3. Angle & Length Snapping relative to last point (15° increments, 0.25m increments)
            if (lastPoint.HasValue)
            {
                Vector3 delta = result.Position - lastPoint.Value;
                float dist = delta.magnitude;

                if (dist >= 0.1f)
                {
                    // Snap angle to multiples of 15°
                    float angleDeg = Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;
                    if (angleDeg < 0f) angleDeg += 360f;

                    float snappedAngle = Mathf.Round(angleDeg / 15f) * 15f;

                    // Snap length to multiples of 0.25m
                    float snappedLength = Mathf.Round(dist / LengthSnapStep) * LengthSnapStep;
                    if (snappedLength < LengthSnapStep) snappedLength = LengthSnapStep;

                    float cos = Mathf.Cos(snappedAngle * Mathf.Deg2Rad);
                    float sin = Mathf.Sin(snappedAngle * Mathf.Deg2Rad);

                    result.Position = lastPoint.Value + new Vector3(cos, 0f, sin) * snappedLength;
                    result.IsAngleSnapped = true;
                    result.SnappedAngle = snappedAngle;
                }
            }

            return result;
        }
    }
}
