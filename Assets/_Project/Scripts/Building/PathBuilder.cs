using System.Collections.Generic;
using UnityEngine;

namespace CozyGlade.Building
{
    public static class PathBuilder
    {
        public static GameObject BuildPath(
            PathData path,
            Transform parent,
            GameObject[] stonePrefabs,
            GameObject cobblePrefab,
            Material fallbackMat = null,
            Material roadMaterial = null)
        {
            if (path == null || path.points == null || path.points.Count < 2)
            {
                return null;
            }

            var rootGo = new GameObject($"Path_{path.id}");
            if (parent != null)
            {
                rootGo.transform.SetParent(parent, false);
            }
            rootGo.transform.position = Vector3.zero;

            if (path.style == PathStyle.Cobblestone)
            {
                BuildCobblePath(path, rootGo.transform, cobblePrefab, fallbackMat);
            }
            else if (path.style == PathStyle.Road)
            {
                BuildRoad(path, rootGo.transform, roadMaterial != null ? roadMaterial : fallbackMat);
            }
            else
            {
                BuildSteppingStones(path, rootGo.transform, stonePrefabs, fallbackMat);
            }

            return rootGo;
        }

        private const float RoadTileMeters = 0.83f;

        private static void BuildRoad(PathData path, Transform rootTransform, Material roadMaterial)
        {
            var points = new List<Vector3>();
            for (int i = 0; i < path.points.Count; i++)
            {
                Vector3 p = path.points[i];
                p.y += 0.02f;

                if (points.Count > 0)
                {
                    Vector3 prev = points[points.Count - 1];
                    float dx = p.x - prev.x;
                    float dz = p.z - prev.z;
                    if (dx * dx + dz * dz < 0.0025f) continue;
                }

                points.Add(p);
            }

            if (points.Count < 2) return;

            float halfWidth = Mathf.Max(0.4f, path.width) * 0.5f;
            int n = points.Count;

            var verts = new Vector3[n * 2];
            var uvs = new Vector2[n * 2];
            var tris = new int[(n - 1) * 6];
            float travelled = 0f;

            for (int i = 0; i < n; i++)
            {
                Vector3 forward = i == 0 ? points[1] - points[0]
                                : i == n - 1 ? points[n - 1] - points[n - 2]
                                : points[i + 1] - points[i - 1];
                forward.y = 0f;
                if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;
                forward.Normalize();

                Vector3 side = new Vector3(-forward.z, 0f, forward.x);
                float spread = halfWidth;

                if (i > 0 && i < n - 1)
                {
                    Vector3 dirIn = points[i] - points[i - 1];
                    Vector3 dirOut = points[i + 1] - points[i];
                    dirIn.y = 0f;
                    dirOut.y = 0f;
                    dirIn.Normalize();
                    dirOut.Normalize();

                    Vector3 miter = dirIn + dirOut;
                    if (miter.sqrMagnitude > 1e-4f)
                    {
                        miter.Normalize();
                        float cos = Mathf.Clamp(Vector3.Dot(miter, dirOut), 0.5f, 1f);
                        spread = Mathf.Min(halfWidth / cos, halfWidth * 2f);
                        side = new Vector3(-miter.z, 0f, miter.x);
                    }
                }

                verts[i * 2] = points[i] + side * spread;
                verts[i * 2 + 1] = points[i] - side * spread;

                float v = travelled / RoadTileMeters;
                uvs[i * 2] = new Vector2(0f, v);
                uvs[i * 2 + 1] = new Vector2((halfWidth * 2f) / RoadTileMeters, v);

                if (i < n - 1)
                {
                    Vector3 delta = points[i + 1] - points[i];
                    delta.y = 0f;
                    travelled += delta.magnitude;
                }

                if (i < n - 1)
                {
                    int v0 = i * 2;
                    int t = i * 6;
                    tris[t] = v0;
                    tris[t + 1] = v0 + 2;
                    tris[t + 2] = v0 + 1;
                    tris[t + 3] = v0 + 1;
                    tris[t + 4] = v0 + 2;
                    tris[t + 5] = v0 + 3;
                }
            }

            var mesh = new Mesh { name = "RoadSurface" };
            if (verts.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();

            if (mesh.normals.Length > 0 && mesh.normals[0].y < 0f)
            {
                System.Array.Reverse(tris);
                mesh.triangles = tris;
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();

            var surface = new GameObject("RoadSurface");
            surface.transform.SetParent(rootTransform, false);

            var filter = surface.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = roadMaterial;

            var collider = surface.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }

        private static void BuildSteppingStones(
            PathData path,
            Transform rootTransform,
            GameObject[] stonePrefabs,
            Material fallbackMat)
        {
            List<Vector3> samples = SamplePointsAlongPolyline(path.points, 0.6f);
            if (samples.Count == 0) return;

            int seed = path.id.GetHashCode();
            var rng = new System.Random(seed);

            for (int i = 0; i < samples.Count; i++)
            {
                Vector3 center = samples[i];

                // Calculate tangent direction
                Vector3 tangent = Vector3.forward;
                if (samples.Count > 1)
                {
                    if (i < samples.Count - 1)
                    {
                        tangent = (samples[i + 1] - center).normalized;
                    }
                    else
                    {
                        tangent = (center - samples[i - 1]).normalized;
                    }
                }

                // Normal perpendicular to tangent on XZ plane
                Vector3 normal = new Vector3(-tangent.z, 0f, tangent.x);

                // Slight lateral jitter (-0.15m .. +0.15m)
                float lateralOffset = ((float)rng.NextDouble() * 2f - 1f) * 0.15f;
                Vector3 stonePos = center + normal * lateralOffset;
                stonePos.y = 0.015f; // prevent z-fighting with ground

                // Random rotation
                float rotY = (float)rng.NextDouble() * 360f;
                Quaternion rot = Quaternion.Euler(0f, rotY, 0f);

                // Random scale jitter (0.85 .. 1.15)
                float scaleJitter = 0.85f + (float)rng.NextDouble() * 0.3f;
                Vector3 scale = Vector3.one * scaleJitter;

                // Pick prefab
                GameObject prefab = null;
                if (stonePrefabs != null && stonePrefabs.Length > 0)
                {
                    int idx = rng.Next(stonePrefabs.Length);
                    prefab = stonePrefabs[idx];
                }

                SpawnSingleStone(stonePos, rot, scale, rootTransform, prefab, fallbackMat, i);
            }
        }

        private static void BuildCobblePath(
            PathData path,
            Transform rootTransform,
            GameObject cobblePrefab,
            Material fallbackMat)
        {
            List<Vector3> samples = SamplePointsAlongPolyline(path.points, 1.0f);
            for (int i = 0; i < samples.Count - 1; i++)
            {
                Vector3 pA = samples[i];
                Vector3 pB = samples[i + 1];
                Vector3 dir = (pB - pA).normalized;

                float angleY = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - 90f;
                Quaternion rot = Quaternion.Euler(0f, angleY, 0f);

                SpawnSingleCobble(pA, rot, rootTransform, cobblePrefab, fallbackMat, i);
            }
        }

        public static List<Vector3> SamplePointsAlongPolyline(List<Vector3> points, float stepSize)
        {
            var result = new List<Vector3>();
            if (points == null || points.Count < 2) return result;

            result.Add(points[0]);
            float accumulatedDist = 0f;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 pA = points[i];
                Vector3 pB = points[i + 1];
                float segLen = Vector3.Distance(pA, pB);
                if (segLen < 0.01f) continue;

                Vector3 dir = (pB - pA) / segLen;
                float currentSegDist = 0f;

                while (accumulatedDist + (segLen - currentSegDist) >= stepSize)
                {
                    float needed = stepSize - accumulatedDist;
                    currentSegDist += needed;
                    Vector3 samplePt = pA + dir * currentSegDist;
                    result.Add(samplePt);
                    accumulatedDist = 0f;
                }

                accumulatedDist += (segLen - currentSegDist);
            }

            return result;
        }

        private static GameObject SpawnSingleStone(
            Vector3 pos,
            Quaternion rot,
            Vector3 scale,
            Transform parent,
            GameObject prefab,
            Material fallbackMat,
            int index)
        {
            GameObject inst;
            if (prefab != null)
            {
                inst = Object.Instantiate(prefab, pos, rot, parent);
                inst.transform.localScale = scale;
            }
            else
            {
                inst = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                inst.transform.SetParent(parent, false);
                inst.transform.position = pos + Vector3.up * 0.02f;
                inst.transform.rotation = rot;
                inst.transform.localScale = new Vector3(0.5f * scale.x, 0.04f, 0.5f * scale.z);
                if (fallbackMat != null) inst.GetComponent<Renderer>().sharedMaterial = fallbackMat;
            }

            // Ensure collider for raycasting/erasing
            var col = inst.GetComponent<Collider>();
            if (col == null)
            {
                var box = inst.AddComponent<BoxCollider>();
                box.size = new Vector3(0.5f, 0.15f, 0.5f);
                box.center = new Vector3(0f, 0.05f, 0f);
            }

            inst.name = $"PathStone_{index}";
            return inst;
        }

        private static GameObject SpawnSingleCobble(
            Vector3 pos,
            Quaternion rot,
            Transform parent,
            GameObject prefab,
            Material fallbackMat,
            int index)
        {
            GameObject inst;
            if (prefab != null)
            {
                inst = Object.Instantiate(prefab, pos, rot, parent);
            }
            else
            {
                inst = GameObject.CreatePrimitive(PrimitiveType.Cube);
                inst.transform.SetParent(parent, false);
                inst.transform.position = pos + Vector3.up * 0.01f;
                inst.transform.rotation = rot;
                inst.transform.localScale = new Vector3(1f, 0.05f, 1f);
                if (fallbackMat != null) inst.GetComponent<Renderer>().sharedMaterial = fallbackMat;
            }

            var col = inst.GetComponent<Collider>();
            if (col == null)
            {
                var box = inst.AddComponent<BoxCollider>();
                box.size = new Vector3(1f, 0.1f, 1f);
                box.center = new Vector3(0.5f, 0f, 0.5f);
            }

            inst.name = $"PathCobble_{index}";
            return inst;
        }
    }
}
