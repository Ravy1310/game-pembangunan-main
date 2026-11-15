using System.Collections.Generic;
using UnityEngine;

namespace CozyGlade.Building
{
    /// <summary>
    /// Generates a gable roof for a closed 4-corner WallData, and a pyramid roof
    /// for any other closed polygon.  All geometry is 100 % procedural mesh —
    /// no prefab pivot or import-axis assumptions are made.
    ///
    /// Tile prefabs are instantiated as visual dressing on top of the solid slope
    /// mesh; their orientation is derived from the slope surface geometry directly.
    /// </summary>
    public static class RoofGenerator
    {
        private const float DefaultPitchDeg = 35f;
        private const float DefaultOverhang = 0.3f;

        // ── Entry point ───────────────────────────────────────────────────────

        public static GameObject BuildRoof(
            WallData wall,
            Transform parent,
            GameObject tilePrefab,
            GameObject ridgeCapPrefab,
            Material roofMat,
            Material wallMat)
        {
            if (wall == null || !wall.closed || wall.points == null || wall.points.Count < 3)
                return null;

            var root = new GameObject($"Roof_{wall.id}");
            root.transform.SetParent(parent, false);

            float pitch    = wall.roof != null ? wall.roof.pitch    : DefaultPitchDeg;
            float overhang = wall.roof != null ? wall.roof.overhang : DefaultOverhang;

            if (wall.points.Count == 4)
                BuildGableRoof(root.transform, wall, pitch, overhang, tilePrefab, roofMat, wallMat);
            else
                BuildPyramidRoof(root.transform, wall, pitch, overhang, roofMat);

            return root;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GABLE ROOF
        // ══════════════════════════════════════════════════════════════════════

        private static void BuildGableRoof(
            Transform root,
            WallData wall,
            float pitchDeg,
            float overhang,
            GameObject tilePrefab,
            Material roofMat,
            Material wallMat)
        {
            // ------------------------------------------------------------------
            // 1.  Extract the axis-aligned bounding box of the footprint.
            //     WallTool always produces a rectangle aligned to world X/Z axes,
            //     so we can work directly in min/max coordinates.
            // ------------------------------------------------------------------
            float minX = wall.points[0].x, maxX = minX;
            float minZ = wall.points[0].z, maxZ = minZ;
            foreach (var p in wall.points)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.z < minZ) minZ = p.z;
                if (p.z > maxZ) maxZ = p.z;
            }

            float W = maxX - minX;   // world width  (X direction)
            float D = maxZ - minZ;   // world depth  (Z direction)
            float H = wall.height;

            float pitchRad = pitchDeg * Mathf.Deg2Rad;
            float yEave    = H;

            // ------------------------------------------------------------------
            // 2.  Ridge orientation.
            //     Ridge runs along the LONGER axis.
            //     halfShort = half of the shorter dimension = horizontal run to ridge.
            // ------------------------------------------------------------------
            float halfShort;
            // Ridge vertex positions
            Vector3 rA, rB;      // left-end and right-end of ridge
            // Eave corner positions (at yEave, pushed out by overhang)
            Vector3 efL, efR;    // front-eave left/right
            Vector3 ebL, ebR;    // back-eave  left/right

            if (W >= D)
            {
                // Ridge runs East-West (along X).
                // "Front" = -Z side,  "Back" = +Z side.
                halfShort = D * 0.5f;
                float yR  = yEave + halfShort * Mathf.Tan(pitchRad);

                rA  = new Vector3(minX, yR, minZ + halfShort);
                rB  = new Vector3(maxX, yR, minZ + halfShort);

                efL = new Vector3(minX, yEave, minZ - overhang);
                efR = new Vector3(maxX, yEave, minZ - overhang);
                ebL = new Vector3(minX, yEave, maxZ + overhang);
                ebR = new Vector3(maxX, yEave, maxZ + overhang);
            }
            else
            {
                // Ridge runs North-South (along Z).
                // "Front" = -X side,  "Back" = +X side.
                halfShort = W * 0.5f;
                float yR  = yEave + halfShort * Mathf.Tan(pitchRad);

                rA  = new Vector3(minX + halfShort, yR, minZ);
                rB  = new Vector3(minX + halfShort, yR, maxZ);

                efL = new Vector3(minX - overhang, yEave, minZ);
                efR = new Vector3(minX - overhang, yEave, maxZ);
                ebL = new Vector3(maxX + overhang, yEave, minZ);
                ebR = new Vector3(maxX + overhang, yEave, maxZ);
            }

            // ------------------------------------------------------------------
            // 3.  Material
            // ------------------------------------------------------------------
            Material mat = roofMat != null ? roofMat
                : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

            // ------------------------------------------------------------------
            // 4.  Slope meshes
            //     Front slope: efL(BL), efR(BR), rB(TR), rA(TL)
            //     Back  slope: ebR(BL), ebL(BR), rA(TR), rB(TL)
            // ------------------------------------------------------------------
            BuildQuadMesh(root, "Slope_Front", efL, efR, rB, rA, mat);
            BuildQuadMesh(root, "Slope_Back",  ebR, ebL, rA, rB, mat);

            // ------------------------------------------------------------------
            // 5.  Tile dressing
            //     Convention for TileSlope(bl, br, tr, tl):
            //       bl = eave-left,   br = eave-right   (bottom of slope)
            //       tr = ridge-right, tl = ridge-left   (top  of slope)
            //     slopeRight = (br-bl): points left→right viewed from outside.
            //     slopeUp3D  = midpoint(tl,tr) - midpoint(bl,br): always uphill.
            //
            //     Ridge runs East-West (W>=D):
            //       rA is at minX (left end of ridge), rB is at maxX (right end).
            //       Front slope eave: efL(minX,-Z)→efR(maxX,-Z), ridge: rA(minX)→rB(maxX)
            //       Back  slope eave: ebL(minX,+Z)→ebR(maxX,+Z), ridge: rA(minX)→rB(maxX)
            //
            //     Ridge runs North-South (D>W):
            //       rA is at minZ (left end of ridge), rB is at maxZ (right end).
            //       Front slope eave: efL(minX,minZ)→efR(minX,maxZ), ridge: rA→rB
            //       Back  slope eave: ebL(maxX,minZ)→ebR(maxX,maxZ), ridge: rA→rB
            // ------------------------------------------------------------------
            if (tilePrefab != null)
            {
                if (W >= D)
                {
                    // Ridge runs East-West (along X).
                    // rA = minX end (left), rB = maxX end (right).
                    // Front slope: eave at -Z, uphill toward ridge.
                    TileSlope(root, tilePrefab, efL, efR, rB, rA, "Front");
                    // Back slope:  eave at +Z, uphill toward ridge.
                    // ebL is at minX (left), ebR is at maxX (right) — same X order as front.
                    TileSlope(root, tilePrefab, ebL, ebR, rB, rA, "Back");
                }
                else
                {
                    // Ridge runs North-South (along Z).
                    // rA = minZ end, rB = maxZ end.
                    // Front slope: eave at -X side (efL minZ, efR maxZ).
                    TileSlope(root, tilePrefab, efL, efR, rB, rA, "Front");
                    // Back slope:  eave at +X side (ebR minZ, ebL maxZ).
                    TileSlope(root, tilePrefab, ebR, ebL, rA, rB, "Back");
                }
            }

            // ------------------------------------------------------------------
            // 6.  Ridge cap
            // ------------------------------------------------------------------
            BuildRidgeCap(root, rA, rB, roofMat);

            // ------------------------------------------------------------------
            // 7.  Gable end walls
            // ------------------------------------------------------------------
            if (W >= D)
            {
                // Left gable  (at X=minX)
                Vector3 glF = new Vector3(minX, yEave, minZ);
                Vector3 glB = new Vector3(minX, yEave, maxZ);
                BuildTriMesh(root, "Gable_L_F", glF, glB, rA, wallMat);
                BuildTriMesh(root, "Gable_L_B", glB, glF, rA, wallMat);
                // Right gable (at X=maxX)
                Vector3 grF = new Vector3(maxX, yEave, minZ);
                Vector3 grB = new Vector3(maxX, yEave, maxZ);
                BuildTriMesh(root, "Gable_R_F", grB, grF, rB, wallMat);
                BuildTriMesh(root, "Gable_R_B", grF, grB, rB, wallMat);
            }
            else
            {
                // Left gable  (at Z=minZ)
                Vector3 glF = new Vector3(minX, yEave, minZ);
                Vector3 glB = new Vector3(maxX, yEave, minZ);
                BuildTriMesh(root, "Gable_L_F", glF, glB, rA, wallMat);
                BuildTriMesh(root, "Gable_L_B", glB, glF, rA, wallMat);
                // Right gable (at Z=maxZ)
                Vector3 grF = new Vector3(minX, yEave, maxZ);
                Vector3 grB = new Vector3(maxX, yEave, maxZ);
                BuildTriMesh(root, "Gable_R_F", grB, grF, rB, wallMat);
                BuildTriMesh(root, "Gable_R_B", grF, grB, rB, wallMat);
            }
        }

        // ── Quad mesh (4 vertices, 2 triangles) ───────────────────────────────
        // bl=bottom-left, br=bottom-right, tr=top-right, tl=top-left
        // Winding: 0,1,2 / 0,2,3  → normal faces upward/outward

        private static void BuildQuadMesh(
            Transform parent, string name,
            Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl,
            Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh   = MakeQuad(bl, br, tr, tl, name);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static Mesh MakeQuad(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, string name)
        {
            var m = new Mesh { name = name };
            m.vertices  = new[] { bl, br, tr, tl };
            m.uv        = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
            m.triangles = new[] { 0,1,2, 0,2,3 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // ── Triangle mesh ─────────────────────────────────────────────────────

        private static void BuildTriMesh(
            Transform parent, string name,
            Vector3 a, Vector3 b, Vector3 apex, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var m = new Mesh { name = name };
            m.vertices  = new[] { a, b, apex };
            m.uv        = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,1) };
            m.triangles = new[] { 0,1,2 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh   = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // ── Ridge cap (triangular prism) ──────────────────────────────────────

        private static void BuildRidgeCap(
            Transform parent, Vector3 rA, Vector3 rB,
            Material mat)
        {
            const float halfW  = 0.14f;
            const float capH   = 0.11f;

            Vector3 ridgeDir = (rB - rA).normalized;
            if (ridgeDir.sqrMagnitude < 0.001f) ridgeDir = Vector3.right;
            Vector3 perp = Vector3.Cross(ridgeDir, Vector3.up).normalized;
            if (perp.sqrMagnitude < 0.01f) perp = Vector3.right;

            // 6 verts: left-foot/right-foot/peak at each end (A and B)
            Vector3 lfA = rA - perp * halfW;
            Vector3 rfA = rA + perp * halfW;
            Vector3 pkA = rA + Vector3.up * capH;

            Vector3 lfB = rB - perp * halfW;
            Vector3 rfB = rB + perp * halfW;
            Vector3 pkB = rB + Vector3.up * capH;

            var verts = new List<Vector3>();
            var uvs   = new List<Vector2>();
            var tris  = new List<int>();

            // Helper: add a quad (CCW winding when viewed from outside)
            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int o = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                uvs.Add(V2(0,0)); uvs.Add(V2(1,0)); uvs.Add(V2(1,1)); uvs.Add(V2(0,1));
                tris.Add(o); tris.Add(o+1); tris.Add(o+2);
                tris.Add(o); tris.Add(o+2); tris.Add(o+3);
            }
            void AddTri(Vector3 a, Vector3 b, Vector3 c)
            {
                int o = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                uvs.Add(V2(0,0)); uvs.Add(V2(1,0)); uvs.Add(V2(0.5f,1));
                tris.Add(o); tris.Add(o+1); tris.Add(o+2);
            }

            // Left  slope face  (lfA→lfB→pkB→pkA)
            AddQuad(lfA, lfB, pkB, pkA);
            // Right slope face  (rfB→rfA→pkA→pkB)
            AddQuad(rfB, rfA, pkA, pkB);
            // End caps
            AddTri(rfA, lfA, pkA);
            AddTri(lfB, rfB, pkB);

            var mesh = new Mesh { name = "RidgeCap" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("RidgeCap");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh   = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Vector2 V2(float x, float y) => new Vector2(x, y);

        // ── Tile prefab dressing ──────────────────────────────────────────────
        // Tile OBJ analysis (roof_tile_red.obj at identity transform):
        //   X: -1.01 → +0.01  (width 1.02m, pivot at maxX ≈ 0, body extends -X)
        //   Y: -0.059 → +0.719
        //   Z:  0.006 → 1.094  (depth 1.088m, pivot at minZ ≈ 0, body extends +Z)
        //   Top-face normal: (0, 0.5736, 0.8192) = 35° pitch, so tile is ALREADY
        //   built-in at 35° pitch in OBJ space.
        //
        // At identity the tile sits as if on the FRONT slope of a house facing +Z:
        //   tile local +Z  = up the slope (world +Z tilted up)
        //   tile local -X  = along eave left→right (world -X direction)
        //   tile local +Y  = outward normal from slope
        //
        // To place tiles on an arbitrary slope we build a rotation matrix whose
        // columns are the desired world-space basis for each tile local axis:
        //   col0 (+X axis) = -slopeRight   (tile body extends -X, so pivot is on the right)
        //   col1 (+Y axis) = slopeNormal   (outward normal)
        //   col2 (+Z axis) = slopeUp3D     (uphill)
        //
        // TileSlope args: (eaveLeft, eaveRight, ridgeRight, ridgeLeft)
        //   bl=eave-left, br=eave-right (bottom), tr=ridge-right, tl=ridge-left (top)

        private static void TileSlope(
            Transform parent, GameObject prefab,
            Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl,
            string label)
        {
            const float nativeW = 1.02f;    // tile extent along local +X (but body is -X)
            const float nativeD = 1.0879f;  // tile extent along local +Z (uphill)

            Vector3 eaveMid  = (bl + br) * 0.5f;
            Vector3 ridgeMid = (tl + tr) * 0.5f;

            // slopeRight: left→right along eave  (world direction)
            Vector3 slopeRight = (br - bl).normalized;
            // slopeUp3D: eave→ridge along slope surface
            Vector3 slopeUp3D  = (ridgeMid - eaveMid).normalized;

            if (slopeRight.sqrMagnitude < 0.001f || slopeUp3D.sqrMagnitude < 0.001f) return;

            float slopeWidth  = Vector3.Distance(bl, br);
            float slopeHeight = Vector3.Distance(eaveMid, ridgeMid);
            if (slopeWidth < 0.01f || slopeHeight < 0.01f) return;

            // Outward normal of slope surface
            Vector3 slopeNormal = Vector3.Cross(slopeRight, slopeUp3D).normalized;
            if (slopeNormal.sqrMagnitude < 0.001f) return;

            // Build rotation: tile local axes → world directions
            //   local +X → -slopeRight  (tile extends -X from pivot, so pivot is at right edge)
            //   local +Y → slopeNormal  (outward)
            //   local +Z → slopeUp3D    (uphill)
            // Quaternion from explicit 3×3 matrix (column = desired world axis)
            Vector3 axisX = -slopeRight;
            Vector3 axisY = slopeNormal;
            Vector3 axisZ = slopeUp3D;
            Quaternion rot = Quaternion.LookRotation(axisZ, axisY);

            // LookRotation sets forward=axisZ and up≈axisY, but doesn't guarantee
            // right=-slopeRight. We correct by using FromToRotation on the resulting
            // right vector. Actually, build from matrix directly:
            rot = QuatFromMatrix(axisX, axisY, axisZ);

            int   cols   = Mathf.Max(1, Mathf.RoundToInt(slopeWidth  / nativeW));
            int   rows   = Mathf.Max(1, Mathf.RoundToInt(slopeHeight / nativeD));
            float scaleX = (slopeWidth  / cols)  / nativeW;
            float scaleZ = (slopeHeight / rows)   / nativeD;

            for (int r = 0; r < rows; r++)
            {
                float vBot = (float)r / rows;

                for (int c = 0; c < cols; c++)
                {
                    // Pivot is at bottom-RIGHT corner of each cell (tile body goes -X).
                    // uRight = right edge of cell c
                    float uRight = (float)(c + 1) / cols;

                    Vector3 eavePt   = Vector3.Lerp(bl, br,  uRight);
                    Vector3 ridgePt  = Vector3.Lerp(tl, tr,  uRight);
                    Vector3 pivotPos = Vector3.Lerp(eavePt, ridgePt, vBot);

                    var tile = Object.Instantiate(prefab, pivotPos, rot, parent);
                    tile.name = $"Tile_{label}_{r}_{c}";
                    tile.transform.localScale = new Vector3(scaleX, 1f, scaleZ);
                }
            }
        }

        // Build a Quaternion from three orthonormal world-space column vectors
        // representing the destination of local +X, +Y, +Z respectively.
        private static Quaternion QuatFromMatrix(Vector3 right, Vector3 up, Vector3 forward)
        {
            // Orthonormalise to be safe
            forward = forward.normalized;
            right   = Vector3.Cross(up.normalized, forward).normalized;
            up      = Vector3.Cross(forward, right);

            float m00 = right.x,   m10 = right.y,   m20 = right.z;
            float m01 = up.x,      m11 = up.y,      m21 = up.z;
            float m02 = forward.x, m12 = forward.y, m22 = forward.z;

            float trace = m00 + m11 + m22;
            float qw, qx, qy, qz;

            if (trace > 0f)
            {
                float s = 0.5f / Mathf.Sqrt(trace + 1f);
                qw = 0.25f / s;
                qx = (m21 - m12) * s;
                qy = (m02 - m20) * s;
                qz = (m10 - m01) * s;
            }
            else if (m00 > m11 && m00 > m22)
            {
                float s = 2f * Mathf.Sqrt(1f + m00 - m11 - m22);
                qw = (m21 - m12) / s;
                qx = 0.25f * s;
                qy = (m01 + m10) / s;
                qz = (m02 + m20) / s;
            }
            else if (m11 > m22)
            {
                float s = 2f * Mathf.Sqrt(1f + m11 - m00 - m22);
                qw = (m02 - m20) / s;
                qx = (m01 + m10) / s;
                qy = 0.25f * s;
                qz = (m12 + m21) / s;
            }
            else
            {
                float s = 2f * Mathf.Sqrt(1f + m22 - m00 - m11);
                qw = (m10 - m01) / s;
                qx = (m02 + m20) / s;
                qy = (m12 + m21) / s;
                qz = 0.25f * s;
            }

            return new Quaternion(qx, qy, qz, qw);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PYRAMID ROOF  (non-rectangular footprint)
        // ══════════════════════════════════════════════════════════════════════

        private static void BuildPyramidRoof(
            Transform root,
            WallData wall,
            float pitchDeg,
            float overhang,
            Material mat)
        {
            int n = wall.points.Count;

            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < n; i++) centroid.x += wall.points[i].x;
            for (int i = 0; i < n; i++) centroid.z += wall.points[i].z;
            centroid /= n;

            float avgR = 0f;
            for (int i = 0; i < n; i++)
                avgR += Vector2.Distance(
                    new Vector2(wall.points[i].x, wall.points[i].z),
                    new Vector2(centroid.x, centroid.z));
            avgR /= n;

            float pitchRad = pitchDeg * Mathf.Deg2Rad;
            float apexY    = wall.height + (avgR + overhang) * Mathf.Tan(pitchRad);
            Vector3 apex   = new Vector3(centroid.x, apexY, centroid.z);

            var eave = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float ex = wall.points[i].x, ez = wall.points[i].z;
                float dx = ex - centroid.x,  dz = ez - centroid.z;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                if (len > 0.001f) { dx /= len; dz /= len; }
                eave[i] = new Vector3(ex + dx * overhang, wall.height, ez + dz * overhang);
            }

            var verts = new List<Vector3>();
            var uvs   = new List<Vector2>();
            var tris  = new List<int>();

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                int o = verts.Count;
                verts.Add(eave[i]); verts.Add(eave[j]); verts.Add(apex);
                uvs.Add(V2(0,0)); uvs.Add(V2(1,0)); uvs.Add(V2(0.5f,1));
                tris.Add(o); tris.Add(o+1); tris.Add(o+2);
            }

            var mesh = new Mesh { name = "PyramidRoof" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("PyramidRoof");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh   = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial =
                mat != null ? mat : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        }
    }
}
