using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CozyGlade.Building;

namespace CozyGlade.Tests
{
    [TestFixture]
    public class WallMeshBuilderTests
    {
        [Test]
        public void Test01_SingleSegment_HasCorrectBounds()
        {
            var pts = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(4f, 0f, 0f)
            };

            var data = WallMeshBuilder.GenerateMeshData(pts, closed: false, height: 2.4f, thickness: 0.28f);

            Assert.AreEqual(4f, data.Bounds.size.x, 0.02f, "Bounds X size should be approx 4.0m");
            Assert.AreEqual(2.4f, data.Bounds.size.y, 0.02f, "Bounds Y size should be approx 2.4m");
            Assert.AreEqual(0.28f, data.Bounds.size.z, 0.02f, "Bounds Z size should be approx 0.28m");
        }

        [Test]
        public void Test02_SingleSegment_HasAtLeast24VerticesAnd12Triangles()
        {
            var pts = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(4f, 0f, 0f)
            };

            var data = WallMeshBuilder.GenerateMeshData(pts, closed: false, height: 2.4f, thickness: 0.28f);

            Assert.GreaterOrEqual(data.Vertices.Count, 24, "Solid 3D wall must have at least 24 vertices (6 faces x 4).");
            Assert.GreaterOrEqual(data.Triangles.Count / 3, 12, "Solid 3D wall must have at least 12 triangles (6 faces x 2).");
        }

        [Test]
        public void Test03_AllTriangles_NormalsFaceOutward()
        {
            var pts = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(4f, 0f, 0f)
            };

            var data = WallMeshBuilder.GenerateMeshData(pts, closed: false, height: 2.4f, thickness: 0.28f);
            Vector3 meshCentroid = data.Bounds.center;

            for (int t = 0; t < data.Triangles.Count; t += 3)
            {
                int i0 = data.Triangles[t];
                int i1 = data.Triangles[t + 1];
                int i2 = data.Triangles[t + 2];

                Vector3 v0 = data.Vertices[i0];
                Vector3 v1 = data.Vertices[i1];
                Vector3 v2 = data.Vertices[i2];

                Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                Vector3 triCentroid = (v0 + v1 + v2) / 3f;

                // For an outward-facing normal on a convex box, the dot product with
                // vector from mesh center to triangle center must be non-negative
                Vector3 fromCenter = (triCentroid - meshCentroid).normalized;
                float dot = Vector3.Dot(faceNormal, fromCenter);
                Assert.GreaterOrEqual(dot, -0.05f, $"Face normal at triangle {t/3} must face outwards (dot was {dot}).");

                // Explicit normal on vertex should point in the same direction as face normal
                Vector3 vn = data.Normals[i0];
                float normDot = Vector3.Dot(faceNormal, vn);
                Assert.Greater(normDot, 0.9f, $"Explicit vertex normal must match face normal at triangle {t/3}.");
            }
        }

        [Test]
        public void Test04_LPolyline_MitreJoint_NoGaps()
        {
            // L-shape with 90 degree corner at (4, 0, 0)
            var pts = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(4f, 0f, 0f),
                new Vector3(4f, 0f, 3f)
            };

            float thickness = 0.28f;
            var data = WallMeshBuilder.GenerateMeshData(pts, closed: false, height: 2.4f, thickness: thickness);

            // Two segments = at least 48 vertices, 24 triangles
            Assert.GreaterOrEqual(data.Vertices.Count, 40, "L-wall should have at least 40 vertices.");

            // Verify bounds enclose both arms plus thickness
            Assert.AreEqual(4f + thickness * 0.5f, data.Bounds.max.x, 0.05f, "Max X should include arm plus mitre offset.");
            Assert.AreEqual(3f, data.Bounds.max.z, 0.05f, "Max Z should match second arm end.");
            Assert.AreEqual(2.4f, data.Bounds.size.y, 0.01f, "Height should be 2.4m.");

            // Check that no triangle has zero area
            for (int t = 0; t < data.Triangles.Count; t += 3)
            {
                Vector3 v0 = data.Vertices[data.Triangles[t]];
                Vector3 v1 = data.Vertices[data.Triangles[t + 1]];
                Vector3 v2 = data.Vertices[data.Triangles[t + 2]];

                float area = 0.5f * Vector3.Cross(v1 - v0, v2 - v0).magnitude;
                Assert.Greater(area, 1e-6f, $"Triangle {t/3} in L-wall must have non-zero area.");
            }
        }

        [Test]
        public void Test05_ClosedBox4x3_HasCorrectBoundsAndNoDegenerateTriangles()
        {
            // 4x3 closed box (rectangle)
            var pts = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(4f, 0f, 0f),
                new Vector3(4f, 0f, 3f),
                new Vector3(0f, 0f, 3f)
            };

            float thickness = 0.28f;
            var data = WallMeshBuilder.GenerateMeshData(pts, closed: true, height: 2.4f, thickness: thickness);

            // 4 segments in closed loop: Sisi A, B, Atas, Bawah per segment = 4 x 4 = 16 quads = 32 triangles = 64 vertices
            Assert.AreEqual(64, data.Vertices.Count, "Closed 4-segment wall should have 64 vertices (16 quads).");
            Assert.AreEqual(32, data.Triangles.Count / 3, "Closed 4-segment wall should have 32 triangles.");

            // Verify non-zero area for every triangle (no degenerate geometry)
            for (int t = 0; t < data.Triangles.Count; t += 3)
            {
                Vector3 v0 = data.Vertices[data.Triangles[t]];
                Vector3 v1 = data.Vertices[data.Triangles[t + 1]];
                Vector3 v2 = data.Vertices[data.Triangles[t + 2]];

                float area = 0.5f * Vector3.Cross(v1 - v0, v2 - v0).magnitude;
                Assert.Greater(area, 1e-6f, $"Triangle {t/3} in closed box must have non-zero area.");
            }

            // Bounds must be approx 4 + thickness by 3 + thickness by 2.4
            Assert.AreEqual(4f + thickness, data.Bounds.size.x, 0.05f, "Outer X dimension should be 4m + thickness.");
            Assert.AreEqual(3f + thickness, data.Bounds.size.z, 0.05f, "Outer Z dimension should be 3m + thickness.");
            Assert.AreEqual(2.4f, data.Bounds.size.y, 0.01f, "Height should be 2.4m.");
        }
    }
}
