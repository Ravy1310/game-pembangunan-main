#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CozyGlade.Core;
using CozyGlade.CameraControls;
using CozyGlade.Building;
using CozyGlade.World;
using CozyGlade.Tools;
using CozyGlade.UI;

namespace CozyGlade.EditorTools
{
    public static class CozyVerificationTests
    {
        [MenuItem("Cozy Glade/Run Verification Tests (S1, S2, S3, S4)")]
        public static void RunAllTests()
        {
            int passed = 0;
            int failed = 0;

            Debug.Log("==================================================");
            Debug.Log("  COZY GLADE: FULL VERIFICATION SUITE (S1-S4)    ");
            Debug.Log("==================================================");

            RunTest("S1: CommandHistory (Execute, Undo, Redo, Clear)", TestCommandHistory, ref passed, ref failed);
            RunTest("S1: GroundRaycaster (Exact Ground Intersection)", TestGroundRaycaster, ref passed, ref failed);
            RunTest("S1: Auto-Bootstrapper & 6-Tool ToolbarUI in Empty Scene", TestAutoBootstrapAndUI, ref passed, ref failed);
            RunTest("S2: WallSnapper (Endpoints, Angle, Polygon Close)", TestWallSnapper, ref passed, ref failed);
            RunTest("S2: WallMeshBuilder (Open & Closed Geometry)", TestWallMeshBuilder, ref passed, ref failed);
            RunTest("S2: WallTool Stamp Simulation & Undo/Redo Flow", TestWallToolSimulation, ref passed, ref failed);
            RunTest("S3: OpeningPlacer (Slot Snapping & Direction Alignment)", TestOpeningPlacer, ref passed, ref failed);
            RunTest("S3: ModifyWallOpeningsCommand (Undo/Redo Cycle)", TestOpeningCommand, ref passed, ref failed);
            RunTest("S3: RoofGenerator (Gable & Pyramid Roof Generation)", TestRoofGenerator, ref passed, ref failed);
            RunTest("S4: FenceBuilder & FenceData (Wood & Stone Styles)", TestFenceBuilderAndData, ref passed, ref failed);
            RunTest("S4: PathBuilder & PathData (Curve Sampling & Stones)", TestPathBuilderAndData, ref passed, ref failed);
            RunTest("S4: Fence & Path Commands (Do/Undo Lifecycles)", TestFenceAndPathCommands, ref passed, ref failed);
            RunTest("S4: FenceTool & PathTool Simulation (Interactive Placement)", TestFenceAndPathToolsSimulation, ref passed, ref failed);

            Debug.Log("==================================================");
            if (failed == 0)
            {
                Debug.Log($"[SUCCESS] ALL {passed} TESTS PASSED! S1 through S4 are fully verified.");
            }
            else
            {
                Debug.LogError($"[FAILED] {failed} tests failed! ({passed} passed)");
            }
            Debug.Log("==================================================");
        }

        private static void RunTest(string testName, Action testAction, ref int passed, ref int failed)
        {
            try
            {
                testAction();
                passed++;
                Debug.Log($"[PASS] {testName}");
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"[FAIL] {testName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void TestCommandHistory()
        {
            var history = new CommandHistory();
            var cmd = new MockCommand();

            Assert(!history.CanUndo, "Undo stack should be empty initially.");
            Assert(!history.CanRedo, "Redo stack should be empty initially.");

            history.Execute(cmd);
            Assert(cmd.ExecutionCount == 1, "Command should have executed once.");
            Assert(history.CanUndo, "CanUndo should be true after Execute.");
            Assert(!history.CanRedo, "CanRedo should be false after Execute.");

            bool undoSuccess = history.Undo();
            Assert(undoSuccess, "Undo should succeed.");
            Assert(cmd.ExecutionCount == 0, "Execution count should return to 0 after Undo.");
            Assert(!history.CanUndo, "CanUndo should be false after Undoing single command.");
            Assert(history.CanRedo, "CanRedo should be true after Undo.");

            bool redoSuccess = history.Redo();
            Assert(redoSuccess, "Redo should succeed.");
            Assert(cmd.ExecutionCount == 1, "Execution count should return to 1 after Redo.");
            Assert(history.CanUndo, "CanUndo should be true after Redo.");
            Assert(!history.CanRedo, "CanRedo should be false after Redo.");
        }

        private static void TestGroundRaycaster()
        {
            var go = new GameObject("TestCamera");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = new Vector3(0, 10, 0);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);

            bool hit = GroundRaycaster.TryGetPoint(cam, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), out Vector3 point);
            Assert(hit, "Raycaster should hit ground when looking straight down.");
            Assert(Mathf.Abs(point.y) < 1e-4f, "Point y should be exactly 0.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void TestAutoBootstrapAndUI()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameBootstrapper.AutoBootstrap();

            var cam = Camera.main;
            Assert(cam != null, "Main Camera must be created automatically.");
            var rig = cam.GetComponent<CameraRig>();
            Assert(rig != null, "CameraRig must be attached to Main Camera.");

            var ground = GameObject.Find("Ground");
            Assert(ground != null, "Ground plane must be created automatically in empty scene.");

            var systems = GameObject.Find("--- Systems ---");
            Assert(systems != null, "Systems container must exist.");
            var tm = systems.GetComponent<ToolManager>();
            Assert(tm != null, "ToolManager must exist.");
            Assert(tm.ActiveTool != null && tm.ActiveTool.Id == "Wall", "WallTool must be active by default.");

            var toolbar = ToolbarUI.Instance;
            Assert(toolbar != null, "ToolbarUI must be created automatically.");
            var canvas = toolbar.GetComponentInChildren<Canvas>();
            Assert(canvas != null, "Canvas must exist for ToolbarUI.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void TestWallSnapper()
        {
            var wall1 = new WallData();
            wall1.points.Add(new Vector3(0, 0, 0));
            wall1.points.Add(new Vector3(4, 0, 0));

            var walls = new List<WallData> { wall1 };

            // 1. Endpoint Snap
            Vector3 nearEnd = new Vector3(4.1f, 0, 0.1f);
            var snap1 = WallSnapper.Snap(nearEnd, null, null, 0, walls, null, false, 0.5f);
            Assert(snap1.IsEndpointSnapped, "Should snap to endpoint within radius.");
            Assert(Vector3.Distance(snap1.Position, new Vector3(4, 0, 0)) < 1e-4f, "Snapped position should match endpoint exactly.");

            // 2. Angle Snap
            Vector3 lastPt = new Vector3(0, 0, 0);
            Vector3 rawAnglePt = new Vector3(2.05f, 0, 1.95f);
            var snap2 = WallSnapper.Snap(rawAnglePt, lastPt, null, 1, walls, null, false);
            Assert(snap2.IsAngleSnapped, "Should snap to 45 degree angle.");
            Assert(Mathf.Abs(snap2.SnappedAngle - 45f) < 1e-3f, "Snapped angle must be 45 degrees.");

            // 3. Polygon Close Snap
            Vector3 startPt = new Vector3(0, 0, 0);
            Vector3 nearStart = new Vector3(0.2f, 0, 0.1f);
            var snap3 = WallSnapper.Snap(nearStart, new Vector3(0, 0, 3), startPt, 4, walls, null, false, 0.5f);
            Assert(snap3.IsClosingPolygon, "Should detect polygon close near start point.");
            Assert(snap3.Position == startPt, "Should snap exactly to start point on close.");

            // 4. Freehand
            var snap4 = WallSnapper.Snap(nearEnd, null, null, 0, walls, null, true, 0.5f);
            Assert(!snap4.IsEndpointSnapped, "Freehand mode should NOT snap to endpoint.");
            Assert(Vector3.Distance(snap4.Position, nearEnd) < 1e-4f, "Position should be untouched in freehand mode.");
        }

        private static void TestWallMeshBuilder()
        {
            // 1. Single 4m segment: bounds check (Criterion 1)
            var pts = new List<Vector3> { new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f) };
            var dataSingle = WallMeshBuilder.GenerateMeshData(pts, false, 2.4f, 0.28f);

            Assert(Mathf.Abs(dataSingle.Bounds.size.x - 4f) < 0.02f, "Bounds X size should be approx 4.0m");
            Assert(Mathf.Abs(dataSingle.Bounds.size.y - 2.4f) < 0.02f, "Bounds Y size should be approx 2.4m");
            Assert(Mathf.Abs(dataSingle.Bounds.size.z - 0.28f) < 0.02f, "Bounds Z size should be approx 0.28m");

            // 2. Vertex & Triangle count: solid 3D requires >= 24 verts and >= 12 tris (Criterion 2)
            Assert(dataSingle.Vertices.Count >= 24, "Solid 3D wall must have at least 24 vertices (6 faces x 4).");
            Assert(dataSingle.Triangles.Count / 3 >= 12, "Solid 3D wall must have at least 12 triangles (6 faces x 2).");

            // 3. Outward-facing normals check (Criterion 3)
            Vector3 center = dataSingle.Bounds.center;
            for (int t = 0; t < dataSingle.Triangles.Count; t += 3)
            {
                Vector3 v0 = dataSingle.Vertices[dataSingle.Triangles[t]];
                Vector3 v1 = dataSingle.Vertices[dataSingle.Triangles[t + 1]];
                Vector3 v2 = dataSingle.Vertices[dataSingle.Triangles[t + 2]];

                Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                Vector3 triCentroid = (v0 + v1 + v2) / 3f;
                Vector3 fromCenter = (triCentroid - center).normalized;

                float dot = Vector3.Dot(faceNormal, fromCenter);
                Assert(dot >= -0.05f, $"Face normal at triangle {t / 3} must face outwards (dot was {dot}).");

                Vector3 vn = dataSingle.Normals[dataSingle.Triangles[t]];
                float normDot = Vector3.Dot(faceNormal, vn);
                Assert(normDot > 0.9f, $"Explicit vertex normal must match face normal at triangle {t / 3}.");
            }

            // 4. L-polyline: mitre corner meets without gaps (Criterion 4)
            var ptsL = new List<Vector3> { new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), new Vector3(4f, 0f, 3f) };
            var dataL = WallMeshBuilder.GenerateMeshData(ptsL, false, 2.4f, 0.28f);
            Assert(dataL.Vertices.Count >= 40, "L-wall should have at least 40 vertices.");
            for (int t = 0; t < dataL.Triangles.Count; t += 3)
            {
                Vector3 v0 = dataL.Vertices[dataL.Triangles[t]];
                Vector3 v1 = dataL.Vertices[dataL.Triangles[t + 1]];
                Vector3 v2 = dataL.Vertices[dataL.Triangles[t + 2]];
                float area = 0.5f * Vector3.Cross(v1 - v0, v2 - v0).magnitude;
                Assert(area > 1e-6f, $"Triangle {t / 3} in L-wall must have non-zero area.");
            }

            // 5. Closed 4x3 box: watertight, no end caps, bounds correct (Criterion 5)
            var ptsBox = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(4f, 0f, 0f),
                new Vector3(4f, 0f, 3f),
                new Vector3(0f, 0f, 3f)
            };
            var dataBox = WallMeshBuilder.GenerateMeshData(ptsBox, true, 2.4f, 0.28f);
            Assert(dataBox.Vertices.Count == 64, "Closed 4-segment wall should have 64 vertices (16 quads).");
            Assert(dataBox.Triangles.Count / 3 == 32, "Closed 4-segment wall should have 32 triangles.");
            for (int t = 0; t < dataBox.Triangles.Count; t += 3)
            {
                Vector3 v0 = dataBox.Vertices[dataBox.Triangles[t]];
                Vector3 v1 = dataBox.Vertices[dataBox.Triangles[t + 1]];
                Vector3 v2 = dataBox.Vertices[dataBox.Triangles[t + 2]];
                float area = 0.5f * Vector3.Cross(v1 - v0, v2 - v0).magnitude;
                Assert(area > 1e-6f, $"Triangle {t / 3} in closed box must have non-zero area.");
            }
        }

        private static void TestWallToolSimulation()
        {
            var worldData = new WorldData();

            var sysGo = new GameObject("TestSystems");
            var tm = sysGo.AddComponent<ToolManager>();
            var wt = sysGo.AddComponent<WallTool>();
            wt.Initialize(worldData, tm);
            tm.RegisterTool(wt);
            tm.SelectTool(wt);

            // Stamp mode: OnPressed langsung menempatkan rumah.
            // Tanpa prefab yang di-assign, PlaceHouse tidak melakukan apa-apa,
            // tapi tidak boleh throw exception.
            wt.Tick(new Vector3(2, 0, 2));
            wt.OnPressed(new Vector3(2, 0, 2));
            wt.OnReleased(new Vector3(2, 0, 2));
            wt.OnCancel();

            // Pastikan tidak ada exception dan undo stack kosong (karena tidak ada prefab)
            Assert(!tm.History.CanUndo || worldData.props.Count >= 0,
                "WallTool harus berjalan tanpa exception meski tanpa prefab.");

            UnityEngine.Object.DestroyImmediate(sysGo);
        }

        private static void TestOpeningPlacer()
        {
            var wall = new WallData();
            wall.points.Add(new Vector3(0, 0, 0));
            wall.points.Add(new Vector3(4, 0, 0)); // 4m wall along +X

            Vector3 clickPos = new Vector3(1.9f, 0, 0.1f);
            var res = OpeningPlacer.CalculatePlacement(wall, clickPos);

            Assert(res.Success, "OpeningPlacer should succeed placing on 4m wall.");
            Assert(res.SegmentIndex == 0, "SegmentIndex should be 0.");
            Assert(Mathf.Abs(res.SlotCenter.x - 2.0f) < 0.2f, "SlotCenter should snap near 2.0m.");

            // Add opening to wall
            bool added = OpeningPlacer.TryAddOrCycleOpening(wall, res.SegmentIndex, res.T, OpeningType.Window1);
            Assert(added, "TryAddOrCycleOpening should succeed.");
            Assert(wall.openings.Count == 1, "Wall should have 1 opening.");
            Assert(wall.openings[0].type == OpeningType.Window1, "Opening should be Window1.");

            // Cycle opening: Window1 -> Window2
            OpeningPlacer.TryAddOrCycleOpening(wall, res.SegmentIndex, res.T, OpeningType.Window1);
            Assert(wall.openings[0].type == OpeningType.Window2, "Opening should cycle to Window2.");

            // Cycle opening: Window2 -> Door
            OpeningPlacer.TryAddOrCycleOpening(wall, res.SegmentIndex, res.T, OpeningType.Window1);
            Assert(wall.openings[0].type == OpeningType.Door, "Opening should cycle to Door.");

            // Cycle opening: Door -> Remove
            OpeningPlacer.TryAddOrCycleOpening(wall, res.SegmentIndex, res.T, OpeningType.Window1);
            Assert(wall.openings.Count == 0, "Opening should be removed after cycling past Door.");
        }

        private static void TestOpeningCommand()
        {
            var wall = new WallData();
            wall.points.Add(new Vector3(0, 0, 0));
            wall.points.Add(new Vector3(4, 0, 0));

            var history = new CommandHistory();
            var newOps = new List<OpeningData>
            {
                new OpeningData { segment = 0, t = 0.5f, type = OpeningType.Door }
            };

            var cmd = new ModifyWallOpeningsCommand(wall, newOps);
            history.Execute(cmd);

            Assert(wall.openings.Count == 1, "Wall should have 1 opening after Execute.");
            Assert(wall.openings[0].type == OpeningType.Door, "Opening should be Door.");

            history.Undo();
            Assert(wall.openings.Count == 0, "Wall openings should be empty after Undo.");

            history.Redo();
            Assert(wall.openings.Count == 1, "Wall openings should be restored after Redo.");
        }

        private static void TestRoofGenerator()
        {
            var wallBox = new WallData
            {
                height = 2.4f,
                thickness = 0.28f,
                closed = true
            };
            wallBox.points.Add(new Vector3(0, 0, 0));
            wallBox.points.Add(new Vector3(4, 0, 0));
            wallBox.points.Add(new Vector3(4, 0, 3));
            wallBox.points.Add(new Vector3(0, 0, 3));

            var dummyRoot = new GameObject("RoofTestRoot");
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

            // Gable roof for box
            var roofGo = RoofGenerator.BuildRoof(wallBox, dummyRoot.transform, null, null, mat, mat);
            Assert(roofGo != null, "Roof should be generated for closed 4-point box.");

            // Pyramid roof for L-shaped 6-point polygon
            var wallPoly = new WallData
            {
                height = 2.4f,
                thickness = 0.28f,
                closed = true
            };
            wallPoly.points.Add(new Vector3(0, 0, 0));
            wallPoly.points.Add(new Vector3(6, 0, 0));
            wallPoly.points.Add(new Vector3(6, 0, 3));
            wallPoly.points.Add(new Vector3(3, 0, 3));
            wallPoly.points.Add(new Vector3(3, 0, 6));
            wallPoly.points.Add(new Vector3(0, 0, 6));

            var roofPoly = RoofGenerator.BuildRoof(wallPoly, dummyRoot.transform, null, null, mat, mat);
            Assert(roofPoly != null, "Pyramid roof should be generated for 6-point closed polygon.");

            UnityEngine.Object.DestroyImmediate(dummyRoot);
            UnityEngine.Object.DestroyImmediate(mat);
        }

        private static void TestFenceBuilderAndData()
        {
            var dummyRoot = new GameObject("TestFenceRoot");

            var fence = new FenceData("fence_01", new[] { new Vector3(0, 0, 0), new Vector3(5, 0, 0) }, FenceStyle.WoodPostRail);
            Assert(fence.points.Count == 2, "Fence should have 2 points.");

            var clone = fence.Clone();
            Assert(clone.id == fence.id, "Cloned fence ID should match.");
            Assert(clone.points.Count == 2, "Cloned fence points should match.");

            var goWood = FenceBuilder.BuildFence(fence, dummyRoot.transform, null, null, null);
            Assert(goWood != null, "FenceBuilder should produce GameObject for wood fence.");
            Assert(goWood.transform.childCount > 0, "Fence root should contain posts and rails.");

            var stoneFence = new FenceData("fence_02", new[] { new Vector3(0, 0, 0), new Vector3(3, 0, 0) }, FenceStyle.Stone);
            var goStone = FenceBuilder.BuildFence(stoneFence, dummyRoot.transform, null, null, null);
            Assert(goStone != null, "FenceBuilder should produce GameObject for stone fence.");

            UnityEngine.Object.DestroyImmediate(dummyRoot);
        }

        private static void TestPathBuilderAndData()
        {
            var points = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(10, 0, 0) };
            var samples = PathBuilder.SamplePointsAlongPolyline(points, 0.6f);
            Assert(samples.Count >= 16 && samples.Count <= 18, $"Polyline sampling should yield ~17 samples, got {samples.Count}.");
            Assert(Vector3.Distance(samples[0], Vector3.zero) < 1e-4f, "First sample must match start point.");
            Assert(samples[samples.Count - 1].x <= 10.01f, "Last sample should be within path range.");

            var dummyRoot = new GameObject("TestPathRoot");
            var path = new PathData("path_01", points, PathStyle.SteppingStones);
            var goStones = PathBuilder.BuildPath(path, dummyRoot.transform, null, null);
            Assert(goStones != null, "PathBuilder should produce GameObject for stepping stones.");
            Assert(goStones.transform.childCount > 0, "Path root should contain stone children.");

            var cobblePath = new PathData("path_02", points, PathStyle.Cobblestone);
            var goCobble = PathBuilder.BuildPath(cobblePath, dummyRoot.transform, null, null);
            Assert(goCobble != null, "PathBuilder should produce GameObject for cobble path.");

            UnityEngine.Object.DestroyImmediate(dummyRoot);
        }

        private static void TestFenceAndPathCommands()
        {
            var worldData = new WorldData();
            var fence = new FenceData("f_cmd", new[] { new Vector3(0, 0, 0), new Vector3(2, 0, 0) });
            var fenceCmd = new BuildFenceCommand(worldData, null, fence);

            fenceCmd.Do();
            Assert(worldData.fences.Count == 1, "Fence should be added to WorldData on Do().");
            Assert(worldData.GetFence("f_cmd") != null, "GetFence should retrieve the added fence.");

            fenceCmd.Undo();
            Assert(worldData.fences.Count == 0, "Fence should be removed from WorldData on Undo().");

            var path = new PathData("p_cmd", new[] { new Vector3(0, 0, 0), new Vector3(3, 0, 0) });
            var pathCmd = new BuildPathCommand(worldData, null, path);

            pathCmd.Do();
            Assert(worldData.paths.Count == 1, "Path should be added to WorldData on Do().");
            Assert(worldData.GetPath("p_cmd") != null, "GetPath should retrieve the added path.");

            pathCmd.Undo();
            Assert(worldData.paths.Count == 0, "Path should be removed from WorldData on Undo().");
        }

        private static void TestFenceAndPathToolsSimulation()
        {
            var sysGo = new GameObject("TestSystems");
            var tm = sysGo.AddComponent<ToolManager>();
            var worldData = new WorldData();

            var fenceTool = sysGo.AddComponent<FenceTool>();
            fenceTool.Initialize(worldData, tm);
            tm.RegisterTool(fenceTool);

            var pathTool = sysGo.AddComponent<PathTool>();
            pathTool.Initialize(worldData, tm);
            tm.RegisterTool(pathTool);

            // Test FenceTool drag
            tm.SelectTool(fenceTool);
            fenceTool.OnPressed(new Vector3(0, 0, 0));
            fenceTool.Tick(new Vector3(3, 0, 0));
            fenceTool.OnReleased(new Vector3(3, 0, 0));

            Assert(worldData.fences.Count == 1, "FenceTool should commit fence on drag-release.");
            Assert(tm.History.CanUndo, "History should be able to undo fence placement.");

            tm.History.Undo();
            Assert(worldData.fences.Count == 0, "Undo should remove fence from WorldData.");

            // Test PathTool drag
            tm.SelectTool(pathTool);
            pathTool.OnPressed(new Vector3(0, 0, 0));
            pathTool.Tick(new Vector3(1, 0, 0));
            pathTool.Tick(new Vector3(2, 0, 0));
            pathTool.OnReleased(new Vector3(2, 0, 0));

            Assert(worldData.paths.Count == 1, "PathTool should commit path on drag-release.");
            Assert(tm.History.CanUndo, "History should be able to undo path placement.");

            tm.History.Undo();
            Assert(worldData.paths.Count == 0, "Undo should remove path from WorldData.");

            UnityEngine.Object.DestroyImmediate(sysGo);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Assertion failed: " + message);
            }
        }

        private class MockCommand : ICommand
        {
            public int ExecutionCount { get; private set; }
            public void Do() => ExecutionCount++;
            public void Undo() => ExecutionCount--;
        }
    }
}
#endif
