using System.Collections.Generic;
using UnityEngine;
using CozyGlade.Building;
using CozyGlade.World;
using CozyGlade.Core;

namespace CozyGlade.Tools
{
    public class OpeningTool : MonoBehaviour, ITool
    {
        public string Id => "Opening";

        [SerializeField] private OpeningType openingType = OpeningType.Window1;

        private WorldData _worldData;
        private ToolManager _toolManager;
        private GameObject _previewObject;

        public OpeningType CurrentType
        {
            get => openingType;
            set => openingType = value;
        }

        public void Initialize(WorldData worldData, ToolManager toolManager)
        {
            _worldData = worldData;
            _toolManager = toolManager;
        }

        public void OnEnter()
        {
            EnsurePreview();
            _previewObject?.SetActive(false);
        }

        public void OnExit()
        {
            _previewObject?.SetActive(false);
        }

        private void EnsurePreview()
        {
            if (_previewObject != null) return;

            _previewObject = new GameObject("OpeningGhostPreview");
            _previewObject.transform.SetParent(transform);
            var mf = _previewObject.AddComponent<MeshFilter>();
            var mr = _previewObject.AddComponent<MeshRenderer>();

#if UNITY_EDITOR
            var ghostMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Ghost_Valid.mat");
            if (ghostMat != null) mr.sharedMaterial = ghostMat;
#endif
            // Simple 1x2.4x0.28m box mesh for preview
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mf.sharedMesh = box.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(box);
            _previewObject.transform.localScale = new Vector3(1f, 2.4f, 0.28f);
            _previewObject.SetActive(false);
        }

        public void Tick(Vector3 pointerWorld)
        {
            if (_worldData == null || _worldData.walls.Count == 0)
            {
                _previewObject?.SetActive(false);
                return;
            }

            WallData bestWall = null;
            OpeningPlacementResult bestResult = default;
            float closestDist = float.MaxValue;

            for (int i = 0; i < _worldData.walls.Count; i++)
            {
                var wall = _worldData.walls[i];
                var res = OpeningPlacer.CalculatePlacement(wall, pointerWorld);
                if (res.Success)
                {
                    float d = Vector3.Distance(pointerWorld, res.SlotCenter);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        bestWall = wall;
                        bestResult = res;
                    }
                }
            }

            if (bestWall != null && bestResult.Success)
            {
                EnsurePreview();
                if (_previewObject != null)
                {
                    // Center the 1m box along the segment
                    _previewObject.transform.position = bestResult.SlotCenter + Vector3.up * 1.2f;
                    _previewObject.transform.rotation = bestResult.PrefabRotation;
                    _previewObject.SetActive(true);
                }
            }
            else
            {
                _previewObject?.SetActive(false);
            }
        }

        public void OnPressed(Vector3 pointerWorld)
        {
            if (_worldData == null || _worldData.walls.Count == 0) return;

            WallData bestWall = null;
            OpeningPlacementResult bestResult = default;
            float closestDist = float.MaxValue;

            for (int i = 0; i < _worldData.walls.Count; i++)
            {
                var wall = _worldData.walls[i];
                var res = OpeningPlacer.CalculatePlacement(wall, pointerWorld);
                if (res.Success)
                {
                    float d = Vector3.Distance(pointerWorld, res.SlotCenter);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        bestWall = wall;
                        bestResult = res;
                    }
                }
            }

            if (bestWall != null && bestResult.Success)
            {
                // Create copy of openings list
                var newOpenings = new List<OpeningData>();
                for (int i = 0; i < bestWall.openings.Count; i++)
                {
                    var op = bestWall.openings[i];
                    newOpenings.Add(new OpeningData { segment = op.segment, t = op.t, type = op.type });
                }

                // Cycle or add opening
                int existingIdx = -1;
                for (int i = 0; i < newOpenings.Count; i++)
                {
                    if (newOpenings[i].segment == bestResult.SegmentIndex && Mathf.Abs(newOpenings[i].t - bestResult.T) < 0.2f)
                    {
                        existingIdx = i;
                        break;
                    }
                }

                if (existingIdx >= 0)
                {
                    var ex = newOpenings[existingIdx];
                    if (ex.type == OpeningType.Window1) ex.type = OpeningType.Window2;
                    else if (ex.type == OpeningType.Window2) ex.type = OpeningType.Door;
                    else newOpenings.RemoveAt(existingIdx);
                }
                else
                {
                    newOpenings.Add(new OpeningData
                    {
                        segment = bestResult.SegmentIndex,
                        t = bestResult.T,
                        type = openingType
                    });
                }

                var cmd = new ModifyWallOpeningsCommand(bestWall, newOpenings);
                if (_toolManager != null)
                {
                    _toolManager.History.Execute(cmd);
                }
                else
                {
                    cmd.Do();
                }

                WorldView.Instance?.PlayPlaceSfx(bestResult.SlotCenter);
            }
        }

        public void OnReleased(Vector3 pointerWorld) { }

        public void OnCancel()
        {
            _previewObject?.SetActive(false);
        }
    }
}
