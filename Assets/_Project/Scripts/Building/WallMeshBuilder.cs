using System.Collections.Generic;
using UnityEngine;

namespace CozyGlade.Building
{
    public class WallMeshData
    {
        public readonly List<Vector3> Vertices = new();
        public readonly List<Vector3> Normals = new();
        public readonly List<Vector2> UVs = new();
        public readonly List<int> Triangles = new();
        public Bounds Bounds;

        public void Clear()
        {
            Vertices.Clear();
            Normals.Clear();
            UVs.Clear();
            Triangles.Clear();
        }

        public void ComputeBounds()
        {
            if (Vertices.Count == 0)
            {
                Bounds = new Bounds(Vector3.zero, Vector3.zero);
                return;
            }

            Vector3 min = Vertices[0];
            Vector3 max = Vertices[0];
            for (int i = 1; i < Vertices.Count; i++)
            {
                min = Vector3.Min(min, Vertices[i]);
                max = Vector3.Max(max, Vertices[i]);
            }
            Bounds = new Bounds((min + max) * 0.5f, max - min);
        }

        public Mesh ToMesh(string name = "WallMesh", Mesh reuse = null)
        {
            var mesh = reuse != null ? reuse : new Mesh();
            mesh.Clear();
            mesh.name = name;

            if (Vertices.Count == 0) return mesh;

            mesh.indexFormat = Vertices.Count > 65000 
                ? UnityEngine.Rendering.IndexFormat.UInt32 
                : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, UVs);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
            Bounds = mesh.bounds;

            return mesh;
        }
    }

    public static class WallMeshBuilder
    {
        private const float MinPointDistance = 0.01f;

        /// <summary>
        /// Generates the raw solid 3D mesh data for a WallData object.
        /// </summary>
        public static WallMeshData GenerateMeshData(WallData data, bool addNoise = false)
        {
            if (data == null || data.points == null || data.points.Count < 2) return new WallMeshData();
            return GenerateMeshData(data.points, data.closed, data.height, data.thickness, 0.5f);
        }

        /// <summary>
        /// Generates the raw solid 3D mesh data (Vertices, Normals, UVs, Triangles) for a wall polyline.
        /// </summary>
        public static WallMeshData GenerateMeshData(
            IReadOnlyList<Vector3> rawPts,
            bool closed,
            float height = 2.4f,
            float thickness = 0.28f,
            float uvScale = 0.5f)
        {
            var data = new WallMeshData();
            if (rawPts == null || rawPts.Count < 2) return data;

            // 1. Clean points: remove duplicates/too-close points
            var pts = CleanPoints(rawPts, closed);
            int n = pts.Count;
            if (n < 2) return data;

            bool isClosed = closed && n >= 3;
            float half = thickness * 0.5f;

            // 2. Compute mitre offset points L (left side) and R (right side) per node
            var L = new Vector3[n];
            var R = new Vector3[n];

            for (int i = 0; i < n; i++)
            {
                bool hasPrev = isClosed || i > 0;
                bool hasNext = isClosed || i < n - 1;
                Vector3 p = Flat(pts[i]);

                Vector3 n0 = Vector3.zero;
                Vector3 n1 = Vector3.zero;

                if (hasPrev)
                {
                    n0 = LeftNormal(Flat(pts[(i - 1 + n) % n]), p);
                }
                if (hasNext)
                {
                    n1 = LeftNormal(p, Flat(pts[(i + 1) % n]));
                }

                Vector3 m;
                if (hasPrev && hasNext)
                {
                    m = n0 + n1;
                    m = m.sqrMagnitude < 1e-6f ? n0 : m.normalized;
                    float d = Mathf.Max(Vector3.Dot(m, n0), 0.3f);
                    m *= (half / d);
                }
                else
                {
                    m = (hasPrev ? n0 : n1) * half;
                }

                L[i] = p + m;
                R[i] = p - m;
            }

            // 3. Build solid faces per segment
            int segs = isClosed ? n : n - 1;
            float dist = 0f;
            Vector3 up = Vector3.up * height;
            float vH = height * uvScale;

            for (int s = 0; s < segs; s++)
            {
                int i = s;
                int j = (s + 1) % n;

                Vector3 a = Flat(pts[i]);
                Vector3 b = Flat(pts[j]);
                Vector3 dir = (b - a).normalized;
                Vector3 nrm = LeftNormal(a, b);
                float len = Vector3.Distance(a, b);

                float u0 = dist * uvScale;
                float u1 = (dist + len) * uvScale;

                // Sisi A (kiri segmen, normal = +nrm)
                Quad(data.Vertices, data.Normals, data.UVs, data.Triangles,
                    L[i], L[j], L[j] + up, L[i] + up, nrm,
                    new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u1, vH), new Vector2(u0, vH));

                // Sisi B (kanan segmen, normal = -nrm)
                Quad(data.Vertices, data.Normals, data.UVs, data.Triangles,
                    R[j], R[i], R[i] + up, R[j] + up, -nrm,
                    new Vector2(u1, 0f), new Vector2(u0, 0f), new Vector2(u0, vH), new Vector2(u1, vH));

                // Sisi Atas (normal = +Y)
                Vector3 a1 = L[i] + up;
                Vector3 b1 = L[j] + up;
                Vector3 c1 = R[j] + up;
                Vector3 d1 = R[i] + up;
                Quad(data.Vertices, data.Normals, data.UVs, data.Triangles,
                    a1, b1, c1, d1, Vector3.up,
                    XZ(a1, uvScale), XZ(b1, uvScale), XZ(c1, uvScale), XZ(d1, uvScale));

                // Sisi Bawah (normal = -Y) -> menutup dasar sehingga solid tertutup sempurna
                Quad(data.Vertices, data.Normals, data.UVs, data.Triangles,
                    R[i], R[j], L[j], L[i], -Vector3.up,
                    XZ(R[i], uvScale), XZ(R[j], uvScale), XZ(L[j], uvScale), XZ(L[i], uvScale));

                // Tutup Ujung (hanya untuk polyline terbuka)
                if (!isClosed && s == 0)
                {
                    // Ujung titik awal: normal = -dir
                    Quad(data.Vertices, data.Normals, data.UVs, data.Triangles,
                        R[i], L[i], L[i] + up, R[i] + up, -dir,
                        Vector2.zero, new Vector2(thickness * uvScale, 0f),
                        new Vector2(thickness * uvScale, vH), new Vector2(0f, vH));
                }

                if (!isClosed && s == segs - 1)
                {
                    // Ujung titik akhir: normal = +dir
                    Quad(data.Vertices, data.Normals, data.UVs, data.Triangles,
                        L[j], R[j], R[j] + up, L[j] + up, dir,
                        Vector2.zero, new Vector2(thickness * uvScale, 0f),
                        new Vector2(thickness * uvScale, vH), new Vector2(0f, vH));
                }

                dist += len;
            }

            data.ComputeBounds();
            return data;
        }

        /// <summary>
        /// Builds a solid 3D Unity Mesh conforming to the specification in perbaikan.md.
        /// Reuses the provided mesh if non-null, avoiding per-frame GC allocations.
        /// </summary>
        public static Mesh Build(
            IReadOnlyList<Vector3> pts,
            bool closed,
            float height = 2.4f,
            float thickness = 0.28f,
            float uvScale = 0.5f,
            Mesh reuse = null)
        {
            var data = GenerateMeshData(pts, closed, height, thickness, uvScale);
            return data.ToMesh(reuse != null ? reuse.name : "WallSolidMesh", reuse);
        }

        public static Mesh BuildWallMesh(WallData data, bool addNoise = false, Mesh reuse = null)
        {
            if (data == null || data.points == null || data.points.Count < 2)
            {
                var empty = reuse != null ? reuse : new Mesh();
                empty.Clear();
                return empty;
            }

            return Build(data.points, data.closed, data.height, data.thickness, 0.5f, reuse);
        }

        private static List<Vector3> CleanPoints(IReadOnlyList<Vector3> rawPoints, bool closed)
        {
            var clean = new List<Vector3>();
            for (int i = 0; i < rawPoints.Count; i++)
            {
                Vector3 p = Flat(rawPoints[i]);
                if (clean.Count == 0 || Vector3.Distance(clean[clean.Count - 1], p) >= MinPointDistance)
                {
                    clean.Add(p);
                }
            }

            if (closed && clean.Count >= 3)
            {
                if (Vector3.Distance(clean[0], clean[clean.Count - 1]) < MinPointDistance)
                {
                    clean.RemoveAt(clean.Count - 1);
                }
            }

            return clean;
        }

        private static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0f, p.z);

        private static Vector3 LeftNormal(Vector3 a, Vector3 b)
        {
            Vector3 d = (b - a).normalized;
            return new Vector3(-d.z, 0f, d.x);
        }

        private static Vector2 XZ(Vector3 p, float s) => new Vector2(p.x * s, p.z * s);

        /// <summary>
        /// Creates a quad with explicit normal and automatic winding verification
        /// to ensure front-facing orientation matches the expected normal.
        /// </summary>
        private static void Quad(
            List<Vector3> V, List<Vector3> N, List<Vector2> U, List<int> T,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 expected,
            Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            int o = V.Count;
            V.Add(a); V.Add(b); V.Add(c); V.Add(d);
            N.Add(expected); N.Add(expected); N.Add(expected); N.Add(expected);
            U.Add(ua); U.Add(ub); U.Add(uc); U.Add(ud);

            bool ok = Vector3.Dot(Vector3.Cross(b - a, c - a), expected) > 0f;
            if (ok)
            {
                T.Add(o);
                T.Add(o + 1);
                T.Add(o + 2);

                T.Add(o);
                T.Add(o + 2);
                T.Add(o + 3);
            }
            else
            {
                T.Add(o);
                T.Add(o + 2);
                T.Add(o + 1);

                T.Add(o);
                T.Add(o + 3);
                T.Add(o + 2);
            }
        }
    }
}
