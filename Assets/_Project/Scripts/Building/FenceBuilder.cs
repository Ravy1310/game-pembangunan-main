using System.Collections.Generic;
using UnityEngine;

namespace CozyGlade.Building
{
    public static class FenceBuilder
    {
        public static GameObject BuildFence(
            FenceData fence,
            Transform parent,
            GameObject postPrefab,
            GameObject railPrefab,
            GameObject stonePrefab,
            Material fallbackMat = null)
        {
            if (fence == null || fence.points == null || fence.points.Count < 2)
            {
                return null;
            }

            var rootGo = new GameObject($"Fence_{fence.id}");
            if (parent != null)
            {
                rootGo.transform.SetParent(parent, false);
            }
            rootGo.transform.position = Vector3.zero;

            int n = fence.points.Count;
            int segmentCount = fence.closed ? n : n - 1;

            if (fence.style == FenceStyle.WoodPostRail)
            {
                BuildWoodFence(fence, rootGo.transform, segmentCount, postPrefab, railPrefab, fallbackMat);
            }
            else
            {
                BuildStoneFence(fence, rootGo.transform, segmentCount, stonePrefab, fallbackMat);
            }

            return rootGo;
        }

        private static void BuildWoodFence(
            FenceData fence,
            Transform rootTransform,
            int segmentCount,
            GameObject postPrefab,
            GameObject railPrefab,
            Material fallbackMat)
        {
            int n = fence.points.Count;
            var placedPostPositions = new List<Vector3>();

            for (int seg = 0; seg < segmentCount; seg++)
            {
                Vector3 pA = fence.points[seg];
                Vector3 pB = fence.points[(seg + 1) % n];

                float dist = Vector3.Distance(pA, pB);
                if (dist < 0.05f) continue;

                Vector3 dir = (pB - pA).normalized;
                int subdivisions = Mathf.Max(1, Mathf.RoundToInt(dist / 1.0f));
                float subLen = dist / subdivisions;

                float angleY = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - 90f;
                Quaternion rot = Quaternion.Euler(0f, angleY, 0f);

                for (int s = 0; s < subdivisions; s++)
                {
                    Vector3 subA = pA + dir * (s * subLen);
                    Vector3 subB = pA + dir * ((s + 1) * subLen);

                    // Place post at subA if not already placed nearby
                    if (!HasPostNear(placedPostPositions, subA, 0.1f))
                    {
                        SpawnPost(subA, rootTransform, postPrefab, fallbackMat);
                        placedPostPositions.Add(subA);
                    }

                    // Place rail spanning from subA to subB
                    SpawnRail(subA, rot, subLen, rootTransform, railPrefab, fallbackMat);

                    // If last segment and last subdivision of open fence, place closing post at subB
                    if (seg == segmentCount - 1 && s == subdivisions - 1 && !fence.closed)
                    {
                        if (!HasPostNear(placedPostPositions, subB, 0.1f))
                        {
                            SpawnPost(subB, rootTransform, postPrefab, fallbackMat);
                            placedPostPositions.Add(subB);
                        }
                    }
                }
            }
        }

        private static void BuildStoneFence(
            FenceData fence,
            Transform rootTransform,
            int segmentCount,
            GameObject stonePrefab,
            Material fallbackMat)
        {
            int n = fence.points.Count;

            for (int seg = 0; seg < segmentCount; seg++)
            {
                Vector3 pA = fence.points[seg];
                Vector3 pB = fence.points[(seg + 1) % n];

                float dist = Vector3.Distance(pA, pB);
                if (dist < 0.05f) continue;

                Vector3 dir = (pB - pA).normalized;
                int subdivisions = Mathf.Max(1, Mathf.RoundToInt(dist / 1.0f));
                float subLen = dist / subdivisions;

                float angleY = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - 90f;
                Quaternion rot = Quaternion.Euler(0f, angleY, 0f);

                for (int s = 0; s < subdivisions; s++)
                {
                    Vector3 subA = pA + dir * (s * subLen);
                    SpawnStoneSegment(subA, rot, subLen, rootTransform, stonePrefab, fallbackMat);
                }
            }
        }

        private static bool HasPostNear(List<Vector3> placed, Vector3 target, float radius)
        {
            float rSq = radius * radius;
            for (int i = 0; i < placed.Count; i++)
            {
                if ((placed[i] - target).sqrMagnitude <= rSq) return true;
            }
            return false;
        }

        private static GameObject SpawnPost(Vector3 pos, Transform parent, GameObject prefab, Material fallbackMat)
        {
            GameObject inst;
            if (prefab != null)
            {
                inst = Object.Instantiate(prefab, pos, Quaternion.identity, parent);
            }
            else
            {
                inst = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                inst.transform.SetParent(parent, false);
                inst.transform.position = pos + Vector3.up * 0.5f;
                inst.transform.localScale = new Vector3(0.12f, 0.5f, 0.12f);
                if (fallbackMat != null) inst.GetComponent<Renderer>().sharedMaterial = fallbackMat;
            }
            inst.name = "Fence_Post";
            return inst;
        }

        private static GameObject SpawnRail(Vector3 pos, Quaternion rot, float length, Transform parent, GameObject prefab, Material fallbackMat)
        {
            GameObject inst;
            if (prefab != null)
            {
                inst = Object.Instantiate(prefab, pos, rot, parent);
                inst.transform.localScale = new Vector3(length, 1f, 1f);
            }
            else
            {
                inst = GameObject.CreatePrimitive(PrimitiveType.Cube);
                inst.transform.SetParent(parent, false);
                Vector3 mid = pos + (rot * Vector3.right) * (length * 0.5f) + Vector3.up * 0.5f;
                inst.transform.position = mid;
                inst.transform.rotation = rot;
                inst.transform.localScale = new Vector3(length, 0.1f, 0.08f);
                if (fallbackMat != null) inst.GetComponent<Renderer>().sharedMaterial = fallbackMat;
            }
            inst.name = "Fence_Rail";
            return inst;
        }

        private static GameObject SpawnStoneSegment(Vector3 pos, Quaternion rot, float length, Transform parent, GameObject prefab, Material fallbackMat)
        {
            GameObject inst;
            if (prefab != null)
            {
                inst = Object.Instantiate(prefab, pos, rot, parent);
                inst.transform.localScale = new Vector3(length, 1f, 1f);
            }
            else
            {
                inst = GameObject.CreatePrimitive(PrimitiveType.Cube);
                inst.transform.SetParent(parent, false);
                Vector3 mid = pos + (rot * Vector3.right) * (length * 0.5f) + Vector3.up * 0.4f;
                inst.transform.position = mid;
                inst.transform.rotation = rot;
                inst.transform.localScale = new Vector3(length, 0.8f, 0.3f);
                if (fallbackMat != null) inst.GetComponent<Renderer>().sharedMaterial = fallbackMat;
            }
            inst.name = "Fence_Stone";
            return inst;
        }
    }
}
