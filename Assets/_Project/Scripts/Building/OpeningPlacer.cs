using System.Collections.Generic;
using UnityEngine;

namespace CozyGlade.Building
{
    public struct OpeningPlacementResult
    {
        public bool Success;
        public int SegmentIndex;
        public float T;
        public Vector3 SlotCenter;
        public Vector3 PrefabPosition;
        public Quaternion PrefabRotation;
    }

    public static class OpeningPlacer
    {
        public const float OpeningWidth = 1.0f; // 1 meter modular width

        public static OpeningPlacementResult CalculatePlacement(WallData wall, Vector3 hitPoint)
        {
            var result = new OpeningPlacementResult { Success = false };

            if (wall == null || wall.points == null || wall.points.Count < 2)
            {
                return result;
            }

            int n = wall.points.Count;
            int segCount = wall.closed ? n : (n - 1);

            int bestSeg = -1;
            float bestDistSq = float.MaxValue;
            float bestT = 0f;
            Vector3 bestProj = Vector3.zero;

            Vector3 hitGround = new Vector3(hitPoint.x, 0f, hitPoint.z);

            for (int i = 0; i < segCount; i++)
            {
                int next = (i + 1) % n;
                Vector3 a = wall.points[i];
                Vector3 b = wall.points[next];

                Vector3 ab = b - a;
                float segLen = ab.magnitude;
                if (segLen < OpeningWidth) continue; // Segment too short for a 1m opening

                float t = Mathf.Clamp01(Vector3.Dot(hitGround - a, ab) / (segLen * segLen));
                Vector3 proj = a + ab * t;
                float distSq = (hitGround - proj).sqrMagnitude;

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestSeg = i;
                    bestT = t;
                    bestProj = proj;
                }
            }

            if (bestSeg < 0 || bestDistSq > 4.0f) // Max 2m away from wall
            {
                return result;
            }

            // Snap to 1m slot along the segment
            int nextIdx = (bestSeg + 1) % n;
            Vector3 pA = wall.points[bestSeg];
            Vector3 pB = wall.points[nextIdx];
            Vector3 dir = (pB - pA).normalized;
            float length = Vector3.Distance(pA, pB);

            float currentDist = bestT * length;
            // Align center to closest integer meter from start + 0.5m
            float halfW = OpeningWidth * 0.5f;
            float snappedCenterDist = Mathf.Round(currentDist - halfW) + halfW;
            snappedCenterDist = Mathf.Clamp(snappedCenterDist, halfW, length - halfW);

            float snappedT = snappedCenterDist / length;
            Vector3 center = pA + dir * snappedCenterDist;

            // Prefab pivot is left-bottom, so position is center - halfW * dir
            Vector3 prefabPos = pA + dir * (snappedCenterDist - halfW);

            // Rotation rule (Section 3.1): +X local along segment, +Z local outward
            float angleY = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - 90f;
            Quaternion rot = Quaternion.Euler(0f, angleY, 0f);

            result.Success = true;
            result.SegmentIndex = bestSeg;
            result.T = snappedT;
            result.SlotCenter = center;
            result.PrefabPosition = prefabPos;
            result.PrefabRotation = rot;

            return result;
        }

        public static bool TryAddOrCycleOpening(WallData wall, int segment, float t, OpeningType type)
        {
            if (wall == null) return false;

            // Check if opening already exists nearby
            int existingIdx = -1;
            for (int i = 0; i < wall.openings.Count; i++)
            {
                var op = wall.openings[i];
                if (op.segment == segment && Mathf.Abs(op.t - t) < 0.2f)
                {
                    existingIdx = i;
                    break;
                }
            }

            if (existingIdx >= 0)
            {
                // Cycle: Window1 -> Window2 -> Door -> Remove
                var existing = wall.openings[existingIdx];
                if (existing.type == OpeningType.Window1)
                {
                    existing.type = OpeningType.Window2;
                }
                else if (existing.type == OpeningType.Window2)
                {
                    existing.type = OpeningType.Door;
                }
                else
                {
                    wall.openings.RemoveAt(existingIdx);
                }
            }
            else
            {
                wall.openings.Add(new OpeningData
                {
                    segment = segment,
                    t = t,
                    type = type
                });
            }

            return true;
        }
    }
}
