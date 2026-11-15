using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CozyGlade.Building;

namespace CozyGlade.World
{
    public class WorldView : MonoBehaviour
    {
        public static WorldView Instance { get; private set; }

        [Header("Materials")]
        [SerializeField] private Material wallPlasterMaterial;
        [SerializeField] private Material wallStoneMaterial;
        [SerializeField] private Material ghostValidMaterial;
        [SerializeField] private Material ghostInvalidMaterial;
        [SerializeField] private Material roofTileMaterial;

        [Header("Opening Prefabs")]
        [SerializeField] private GameObject wallDoorPrefab;
        [SerializeField] private GameObject wallWindow1Prefab;
        [SerializeField] private GameObject wallWindow2Prefab;

        [Header("Opening Fill Materials")]
        [SerializeField] private Material glassMaterial;
        [SerializeField] private Material doorPanelMaterial;

        [Header("Roof Prefabs")]
        [SerializeField] private GameObject roofTileRedPrefab;
        [SerializeField] private GameObject roofTileBluePrefab;
        [SerializeField] private GameObject roofThatchPrefab;
        [SerializeField] private GameObject roofRidgeCapPrefab;

        [Header("Fence Prefabs")]
        [SerializeField] private GameObject fencePostPrefab;
        [SerializeField] private GameObject fenceRailPrefab;
        [SerializeField] private GameObject fenceStonePrefab;
        [SerializeField] private GameObject fenceGatePrefab;

        [Header("Path Prefabs")]
        [SerializeField] private GameObject[] pathStonePrefabs;
        [SerializeField] private GameObject pathCobblePrefab;
        [SerializeField] private Material roadStoneMaterial;

        [Header("Audio")]
        [SerializeField] private AudioClip[] wallPlaceClips;
        [SerializeField] private AudioClip[] roofPopClips;
        [SerializeField] private AudioClip[] propPlaceClips;
        [SerializeField] private AudioClip[] eraseClips;
        [SerializeField] private AudioClip undoClip;

        [Header("FX")]
        [SerializeField] private GameObject buildPuffPrefab;
        [SerializeField] private GameObject sparklePlacePrefab;

        private readonly Dictionary<string, GameObject> _wallGameObjects = new();
        private readonly Dictionary<string, GameObject> _fenceGameObjects = new();
        private readonly Dictionary<string, GameObject> _pathGameObjects = new();
        private readonly Dictionary<string, GameObject> _propGameObjects = new();

        private GameObject _ghostGameObject;
        private MeshFilter _ghostMeshFilter;
        private MeshRenderer _ghostMeshRenderer;

        private GameObject _fenceGhostGo;
        private GameObject _pathGhostGo;

        public Material WallPlasterMaterial => wallPlasterMaterial;
        public Material WallStoneMaterial => wallStoneMaterial;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadDefaultAssetsIfMissing();
            CreateGhostObject();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void LoadDefaultAssetsIfMissing()
        {
#if UNITY_EDITOR
            if (wallPlasterMaterial == null)
                wallPlasterMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Wall_Plaster.mat");
            if (wallStoneMaterial == null)
                wallStoneMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Wall_Stone.mat");
            if (ghostValidMaterial == null)
                ghostValidMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Ghost_Valid.mat");
            if (ghostInvalidMaterial == null)
                ghostInvalidMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Ghost_Invalid.mat");
            if (roofTileMaterial == null)
                roofTileMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Roof_Tile.mat");

            if (wallDoorPrefab == null)
                wallDoorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Walls/wall_door_01.prefab");
            if (wallWindow1Prefab == null)
                wallWindow1Prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Walls/wall_window_01.prefab");
            if (wallWindow2Prefab == null)
                wallWindow2Prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Walls/wall_window_02.prefab");

            if (glassMaterial == null)
                glassMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Glass.mat");
            if (doorPanelMaterial == null)
                doorPanelMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_DoorPanel.mat");

            if (roofTileRedPrefab == null)
                roofTileRedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Roofs/roof_tile_red.prefab");
            if (roofTileBluePrefab == null)
                roofTileBluePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Roofs/roof_tile_blue.prefab");
            if (roofThatchPrefab == null)
                roofThatchPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Roofs/roof_thatch.prefab");
            if (roofRidgeCapPrefab == null)
                roofRidgeCapPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Roofs/roof_ridge_cap.prefab");

            if (fencePostPrefab == null)
                fencePostPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Fences/fence_post_01.prefab");
            if (fenceRailPrefab == null)
                fenceRailPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Fences/fence_rail_01.prefab");
            if (fenceStonePrefab == null)
                fenceStonePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Fences/fence_stone_01.prefab");
            if (fenceGatePrefab == null)
                fenceGatePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Fences/fence_gate_01.prefab");

            if (pathStonePrefabs == null || pathStonePrefabs.Length == 0)
            {
                var stones = new List<GameObject>();
                for (int i = 1; i <= 4; i++)
                {
                    var p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Paths/path_stone_0{i}.prefab");
                    if (p != null) stones.Add(p);
                }
                pathStonePrefabs = stones.ToArray();
            }

            if (pathCobblePrefab == null)
                pathCobblePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Paths/path_cobble_01.prefab");

            if (roadStoneMaterial == null)
                roadStoneMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Road_Stone.mat");

            if (buildPuffPrefab == null)
                buildPuffPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FX/fx_build_puff.prefab");
            if (sparklePlacePrefab == null)
                sparklePlacePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FX/fx_sparkle_place.prefab");

            if (wallPlaceClips == null || wallPlaceClips.Length == 0)
            {
                var clips = new List<AudioClip>();
                foreach (var letter in new[] { "a", "b", "c" })
                {
                    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Audio/SFX/sfx_wall_place_{letter}.wav");
                    if (clip != null) clips.Add(clip);
                }
                wallPlaceClips = clips.ToArray();
            }

            if (roofPopClips == null || roofPopClips.Length == 0)
            {
                var clips = new List<AudioClip>();
                foreach (var letter in new[] { "a", "b", "c" })
                {
                    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Audio/SFX/sfx_roof_pop_{letter}.wav");
                    if (clip != null) clips.Add(clip);
                }
                roofPopClips = clips.ToArray();
            }

            if (propPlaceClips == null || propPlaceClips.Length == 0)
            {
                var clips = new List<AudioClip>();
                foreach (var letter in new[] { "a", "b", "c" })
                {
                    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Audio/SFX/sfx_prop_place_{letter}.wav");
                    if (clip != null) clips.Add(clip);
                }
                propPlaceClips = clips.ToArray();
            }

            if (eraseClips == null || eraseClips.Length == 0)
            {
                var clips = new List<AudioClip>();
                foreach (var letter in new[] { "a", "b", "c" })
                {
                    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Audio/SFX/sfx_erase_{letter}.wav");
                    if (clip != null) clips.Add(clip);
                }
                eraseClips = clips.ToArray();
            }

            if (undoClip == null)
                undoClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/sfx_undo.wav");
#endif
            // Procedural fallback if materials were not found
            if (wallPlasterMaterial == null)
            {
                wallPlasterMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                wallPlasterMaterial.color = new Color(0.92f, 0.88f, 0.82f);
            }
            if (wallStoneMaterial == null)
            {
                wallStoneMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                wallStoneMaterial.color = new Color(0.65f, 0.65f, 0.65f);
            }
            if (ghostValidMaterial == null)
            {
                ghostValidMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                ghostValidMaterial.color = new Color(0.45f, 0.85f, 0.5f, 0.5f);
            }
            if (ghostInvalidMaterial == null)
            {
                ghostInvalidMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                ghostInvalidMaterial.color = new Color(0.92f, 0.38f, 0.33f, 0.5f);
            }
            if (roofTileMaterial == null)
            {
                roofTileMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                roofTileMaterial.color = new Color(0.72f, 0.35f, 0.28f);
            }

            // Procedural fallback for opening fill materials
            EnsureOpeningFillMaterials();
        }

        /// <summary>
        /// Creates glass and door-panel materials in code if they were not assigned in the inspector.
        /// Uses the project's Cozy/ToonLit shader when available so fills match the art style.
        /// </summary>
        private void EnsureOpeningFillMaterials()
        {
            if (glassMaterial == null)
            {
                var shader = Shader.Find("Cozy/ToonLit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                glassMaterial = new Material(shader);
                glassMaterial.name = "M_Glass_Runtime";
                glassMaterial.SetColor("_BaseColor", new Color(0.62f, 0.82f, 0.92f, 1f));
                glassMaterial.SetColor("_ShadowColor", new Color(0.45f, 0.6f, 0.72f, 1f));
                glassMaterial.SetFloat("_Cull", 0f); // double-sided
            }

            if (doorPanelMaterial == null)
            {
                var shader = Shader.Find("Cozy/ToonLit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                doorPanelMaterial = new Material(shader);
                doorPanelMaterial.name = "M_DoorPanel_Runtime";
                doorPanelMaterial.SetColor("_BaseColor", new Color(0.52f, 0.34f, 0.2f, 1f));
                doorPanelMaterial.SetFloat("_Cull", 0f); // double-sided
            }
        }

        // Cached fill meshes keyed by OpeningType to avoid creating new Mesh objects every placement.
        private static readonly Dictionary<OpeningType, (Mesh front, Mesh back)> _fillMeshCache = new();

        /// <summary>
        /// Creates a flat quad mesh in the local XY plane (normal +Z) at the given z offset.
        /// </summary>
        private static Mesh CreateQuadMesh(float minX, float maxX, float minY, float maxY, float z, string name)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new Vector3[]
            {
                new Vector3(minX, minY, z),
                new Vector3(maxX, minY, z),
                new Vector3(maxX, maxY, z),
                new Vector3(minX, maxY, z),
            };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.normals = new Vector3[]
            {
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
            };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Returns cached front/back fill meshes for the given opening type, creating them on first use.
        /// Two quads are placed at z=+0.15 and z=-0.15 so the fill is visible from both sides
        /// even though the wall mesh is currently solid (no hole cut).
        /// </summary>
        private static (Mesh front, Mesh back) GetFillMeshes(OpeningType type)
        {
            if (_fillMeshCache.TryGetValue(type, out var cached))
                return cached;

            float minX, maxX, minY, maxY;
            switch (type)
            {
                case OpeningType.Window1:
                    // Posts x=0..-0.22 & x=-0.78..-1.0; sill y=0.28; lintel y=1.85
                    minX = -0.22f; maxX = -0.78f; minY = 0.30f; maxY = 1.83f;
                    break;
                case OpeningType.Window2:
                    // Posts x=0..-0.27 & x=-0.73..-1.0; sill y=0.28; lintel y=1.68
                    minX = -0.27f; maxX = -0.73f; minY = 0.30f; maxY = 1.66f;
                    break;
                default: // Door
                    // Posts x=0..-0.14 & x=-0.86..-1.0; threshold y=0.28; lintel y=1.88
                    minX = -0.14f; maxX = -0.86f; minY = 0.30f; maxY = 1.86f;
                    break;
            }

            var front = CreateQuadMesh(minX, maxX, minY, maxY, 0.15f, $"{type}_FillFront");
            var back = CreateQuadMesh(minX, maxX, minY, maxY, -0.15f, $"{type}_FillBack");
            var entry = (front, back);
            _fillMeshCache[type] = entry;
            return entry;
        }

        /// <summary>
        /// Adds glass panes (windows) or a door panel as child objects of an instantiated opening prefab.
        /// </summary>
        private void AddOpeningFill(OpeningType type, Transform parent)
        {
            Material fillMat = (type == OpeningType.Door) ? doorPanelMaterial : glassMaterial;
            if (fillMat == null) return;

            var (frontMesh, backMesh) = GetFillMeshes(type);
            CreateFillChild($"{type}_FillFront", frontMesh, fillMat, parent);
            CreateFillChild($"{type}_FillBack", backMesh, fillMat, parent);
        }

        private static void CreateFillChild(string name, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        private void CreateGhostObject()
        {
            if (_ghostGameObject != null) return;

            _ghostGameObject = new GameObject("--- Wall Ghost Preview ---");
            _ghostGameObject.transform.SetParent(transform);
            _ghostMeshFilter = _ghostGameObject.AddComponent<MeshFilter>();
            _ghostMeshRenderer = _ghostGameObject.AddComponent<MeshRenderer>();
            _ghostMeshRenderer.sharedMaterial = ghostValidMaterial;
            _ghostGameObject.SetActive(false);
        }

        // ================= WALLS =================
        public GameObject SpawnWallVisual(WallData wall, bool animate = true)
        {
            if (wall == null || wall.points == null || wall.points.Count < 2) return null;

            DestroyWallVisual(wall.id);

            var go = new GameObject($"Wall_{wall.id}");
            go.transform.SetParent(transform);
            go.transform.position = Vector3.zero;

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();

            Material mat = (wall.style == WallStyle.Stone) ? wallStoneMaterial : wallPlasterMaterial;
            mr.sharedMaterial = mat;

            Mesh mesh = WallMeshBuilder.BuildWallMesh(wall, true);
            mf.sharedMesh = mesh;

            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;

            SpawnOpenings(wall, go.transform);

            if (wall.closed && wall.points.Count >= 3)
            {
                GameObject tilePrefab = roofTileRedPrefab;
                if (wall.roof != null)
                {
                    if (wall.roof.material == RoofMaterial.BlueTile) tilePrefab = roofTileBluePrefab;
                    else if (wall.roof.material == RoofMaterial.Thatch) tilePrefab = roofThatchPrefab;
                }

                RoofGenerator.BuildRoof(wall, go.transform, tilePrefab, roofRidgeCapPrefab, roofTileMaterial, mat);
                if (animate)
                {
                    PlayRoofPopSfx(wall.points[0]);
                }
            }

            _wallGameObjects[wall.id] = go;

            if (animate)
            {
                StartCoroutine(AnimatePop(go.transform));
                PlayPlaceSfx(wall.points[wall.points.Count - 1]);
                SpawnPuff(wall.points[wall.points.Count - 1]);
            }

            return go;
        }

        private void SpawnOpenings(WallData wall, Transform parent)
        {
            if (wall.openings == null || wall.openings.Count == 0) return;

            int n = wall.points.Count;

            for (int i = 0; i < wall.openings.Count; i++)
            {
                var op = wall.openings[i];
                if (op.segment < 0 || op.segment >= n) continue;

                int nextIdx = (op.segment + 1) % n;
                Vector3 pA = wall.points[op.segment];
                Vector3 pB = wall.points[nextIdx];
                Vector3 dir = (pB - pA).normalized;
                float segLen = Vector3.Distance(pA, pB);

                float centerDist = op.t * segLen;
                float halfW = OpeningPlacer.OpeningWidth * 0.5f;
                Vector3 prefabPos = pA + dir * (centerDist - halfW);

                float angleY = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - 90f;
                Quaternion rot = Quaternion.Euler(0f, angleY, 0f);

                GameObject prefab = wallWindow1Prefab;
                if (op.type == OpeningType.Door) prefab = wallDoorPrefab;
                else if (op.type == OpeningType.Window2) prefab = wallWindow2Prefab;

                if (prefab != null)
                {
                    var inst = Instantiate(prefab, prefabPos, rot, parent);
                    inst.name = $"Opening_{op.type}_{i}";
                    AddOpeningFill(op.type, inst.transform);
                }
            }
        }

        public bool DestroyWallVisual(string wallId, bool playSfx = false)
        {
            if (_wallGameObjects.TryGetValue(wallId, out var go))
            {
                if (go != null)
                {
                    Vector3 pos = go.transform.position;
                    if (playSfx)
                    {
                        PlayUndoSfx(pos);
                        SpawnPuff(pos);
                    }
                    Destroy(go);
                }
                _wallGameObjects.Remove(wallId);
                return true;
            }
            return false;
        }

        public void SetPreviewWall(WallData previewWall, bool isValid)
        {
            if (previewWall == null || previewWall.points == null || previewWall.points.Count < 2)
            {
                if (_ghostGameObject != null) _ghostGameObject.SetActive(false);
                return;
            }

            if (_ghostGameObject == null) CreateGhostObject();

            Mesh mesh = WallMeshBuilder.BuildWallMesh(previewWall, false, _ghostMeshFilter.sharedMesh);
            _ghostMeshFilter.sharedMesh = mesh;
            _ghostMeshRenderer.sharedMaterial = isValid ? ghostValidMaterial : ghostInvalidMaterial;
            _ghostGameObject.SetActive(true);
        }

        public void HidePreview()
        {
            if (_ghostGameObject != null)
            {
                _ghostGameObject.SetActive(false);
            }
        }

        // ================= FENCES =================
        public GameObject SpawnFenceVisual(FenceData fence, bool animate = true)
        {
            if (fence == null || fence.points == null || fence.points.Count < 2) return null;

            DestroyFenceVisual(fence.id);

            var go = FenceBuilder.BuildFence(fence, transform, fencePostPrefab, fenceRailPrefab, fenceStonePrefab, wallStoneMaterial);
            if (go != null)
            {
                _fenceGameObjects[fence.id] = go;

                if (animate)
                {
                    StartCoroutine(AnimatePop(go.transform));
                    PlayPropPlaceSfx(fence.points[fence.points.Count - 1]);
                    SpawnSparkle(fence.points[fence.points.Count - 1]);
                }
            }
            return go;
        }

        public bool DestroyFenceVisual(string fenceId, bool playSfx = false)
        {
            if (_fenceGameObjects.TryGetValue(fenceId, out var go))
            {
                if (go != null)
                {
                    Vector3 pos = go.transform.position;
                    if (playSfx)
                    {
                        PlayEraseSfx(pos);
                        SpawnPuff(pos);
                    }
                    Destroy(go);
                }
                _fenceGameObjects.Remove(fenceId);
                return true;
            }
            return false;
        }

        public void SetPreviewFence(FenceData previewFence, bool isValid)
        {
            if (previewFence == null || previewFence.points == null || previewFence.points.Count < 2)
            {
                HidePreviewFence();
                return;
            }

            if (_fenceGhostGo != null)
            {
                Destroy(_fenceGhostGo);
            }

            _fenceGhostGo = FenceBuilder.BuildFence(
                previewFence,
                transform,
                fencePostPrefab,
                fenceRailPrefab,
                fenceStonePrefab,
                isValid ? ghostValidMaterial : ghostInvalidMaterial);

            if (_fenceGhostGo != null)
            {
                _fenceGhostGo.name = "--- Fence Ghost Preview ---";
                var renderers = _fenceGhostGo.GetComponentsInChildren<Renderer>();
                Material ghostMat = isValid ? ghostValidMaterial : ghostInvalidMaterial;
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].sharedMaterial = ghostMat;
                }
            }
        }

        public void HidePreviewFence()
        {
            if (_fenceGhostGo != null)
            {
                Destroy(_fenceGhostGo);
                _fenceGhostGo = null;
            }
        }

        // ================= PATHS =================
        public GameObject SpawnPathVisual(PathData path, bool animate = true)
        {
            if (path == null || path.points == null || path.points.Count < 2) return null;

            DestroyPathVisual(path.id);

            var go = PathBuilder.BuildPath(path, transform, pathStonePrefabs, pathCobblePrefab, wallStoneMaterial, roadStoneMaterial);
            if (go != null)
            {
                _pathGameObjects[path.id] = go;

                if (animate)
                {
                    StartCoroutine(AnimatePop(go.transform));
                    PlayPropPlaceSfx(path.points[path.points.Count - 1]);
                    SpawnSparkle(path.points[path.points.Count - 1]);
                }
            }
            return go;
        }

        public bool DestroyPathVisual(string pathId, bool playSfx = false)
        {
            if (_pathGameObjects.TryGetValue(pathId, out var go))
            {
                if (go != null)
                {
                    Vector3 pos = go.transform.position;
                    if (playSfx)
                    {
                        PlayEraseSfx(pos);
                        SpawnPuff(pos);
                    }
                    Destroy(go);
                }
                _pathGameObjects.Remove(pathId);
                return true;
            }
            return false;
        }

        public void SetPreviewPath(PathData previewPath, bool isValid)
        {
            if (previewPath == null || previewPath.points == null || previewPath.points.Count < 2)
            {
                HidePreviewPath();
                return;
            }

            if (_pathGhostGo != null)
            {
                Destroy(_pathGhostGo);
            }

            _pathGhostGo = PathBuilder.BuildPath(
                previewPath,
                transform,
                pathStonePrefabs,
                pathCobblePrefab,
                isValid ? ghostValidMaterial : ghostInvalidMaterial,
                roadStoneMaterial);

            if (_pathGhostGo != null)
            {
                _pathGhostGo.name = "--- Path Ghost Preview ---";
                var renderers = _pathGhostGo.GetComponentsInChildren<Renderer>();
                Material ghostMat = isValid ? ghostValidMaterial : ghostInvalidMaterial;
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].sharedMaterial = ghostMat;
                }
            }
        }

        public void HidePreviewPath()
        {
            if (_pathGhostGo != null)
            {
                Destroy(_pathGhostGo);
                _pathGhostGo = null;
            }
        }

        // ================= PROPS (FORWARD SUPPORT FOR T7) =================
        public GameObject SpawnPropVisual(PropData prop, GameObject prefab, bool animate = true)
        {
            if (prop == null) return null;
            DestroyPropVisual(prop.id);

            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, prop.position, prop.rotation, transform);
                go.transform.localScale = prop.scale;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(transform, false);
                go.transform.position = prop.position;
                go.transform.rotation = prop.rotation;
                go.transform.localScale = prop.scale;
            }
            go.name = $"Prop_{prop.prefabName}_{prop.id}";

            _propGameObjects[prop.id] = go;

            if (animate)
            {
                StartCoroutine(AnimatePop(go.transform));
                PlayPropPlaceSfx(prop.position);
                SpawnSparkle(prop.position);
            }

            return go;
        }

        public bool DestroyPropVisual(string propId, bool playSfx = false)
        {
            if (_propGameObjects.TryGetValue(propId, out var go))
            {
                if (go != null)
                {
                    Vector3 pos = go.transform.position;
                    if (playSfx)
                    {
                        PlayEraseSfx(pos);
                        SpawnPuff(pos);
                    }
                    Destroy(go);
                }
                _propGameObjects.Remove(propId);
                return true;
            }
            return false;
        }

        // ================= ANIMATION & AUDIO =================
        private IEnumerator AnimatePop(Transform targetT)
        {
            if (targetT == null) yield break;

            Vector3 origScale = targetT.localScale;
            float duration = 0.22f;
            float elapsed = 0f;

            targetT.localScale = new Vector3(origScale.x, 0.05f, origScale.z);

            while (elapsed < duration)
            {
                if (targetT == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float scaleY = Mathf.Sin(t * Mathf.PI * 0.5f);
                if (t > 0.6f)
                {
                    scaleY += Mathf.Sin((t - 0.6f) / 0.4f * Mathf.PI) * 0.08f;
                }

                targetT.localScale = new Vector3(origScale.x, Mathf.Max(0.05f, origScale.y * scaleY), origScale.z);
                yield return null;
            }

            if (targetT != null)
            {
                targetT.localScale = origScale;
            }
        }

        public void PlayPlaceSfx(Vector3 pos)
        {
            if (wallPlaceClips != null && wallPlaceClips.Length > 0)
            {
                var clip = wallPlaceClips[Random.Range(0, wallPlaceClips.Length)];
                if (clip != null) PlayClipWithPitch(clip, pos, 0.95f, 1.05f);
            }
        }

        public void PlayRoofPopSfx(Vector3 pos)
        {
            if (roofPopClips != null && roofPopClips.Length > 0)
            {
                var clip = roofPopClips[Random.Range(0, roofPopClips.Length)];
                if (clip != null) PlayClipWithPitch(clip, pos, 0.95f, 1.05f);
            }
        }

        public void PlayPropPlaceSfx(Vector3 pos)
        {
            if (propPlaceClips != null && propPlaceClips.Length > 0)
            {
                var clip = propPlaceClips[Random.Range(0, propPlaceClips.Length)];
                if (clip != null) PlayClipWithPitch(clip, pos, 0.95f, 1.05f);
            }
        }

        public void PlayEraseSfx(Vector3 pos)
        {
            if (eraseClips != null && eraseClips.Length > 0)
            {
                var clip = eraseClips[Random.Range(0, eraseClips.Length)];
                if (clip != null) PlayClipWithPitch(clip, pos, 0.95f, 1.05f);
            }
        }

        public void PlayUndoSfx(Vector3 pos)
        {
            if (undoClip != null)
            {
                PlayClipWithPitch(undoClip, pos, 0.95f, 1.05f);
            }
        }

        private void PlayClipWithPitch(AudioClip clip, Vector3 pos, float minPitch, float maxPitch)
        {
            var go = new GameObject("TempAudio");
            go.transform.position = pos;
            var a = go.AddComponent<AudioSource>();
            a.clip = clip;
            a.pitch = Random.Range(minPitch, maxPitch);
            a.volume = 0.85f;
            a.spatialBlend = 0f;
            a.Play();
            Destroy(go, clip.length / Mathf.Max(0.1f, a.pitch) + 0.1f);
        }

        private void SpawnPuff(Vector3 pos)
        {
            if (buildPuffPrefab != null)
            {
                var puff = Instantiate(buildPuffPrefab, pos, Quaternion.identity);
                Destroy(puff, 2f);
            }
        }

        private void SpawnSparkle(Vector3 pos)
        {
            if (sparklePlacePrefab != null)
            {
                var fx = Instantiate(sparklePlacePrefab, pos, Quaternion.identity);
                Destroy(fx, 2f);
            }
        }
    }
}
