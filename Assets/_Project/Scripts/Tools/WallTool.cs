using UnityEngine;
using UnityEngine.EventSystems;
using CozyGlade.Building;
using CozyGlade.World;
using CozyGlade.Core;
using CozyGlade.Input;

namespace CozyGlade.Tools
{
    /// <summary>
    /// House Placement Tool — menggunakan prefab low-poly house dari Hodaart.
    ///
    /// Cara pakai:
    ///   • Ghost preview (hijau transparan) mengikuti kursor.
    ///   • Klik kiri  → tempatkan rumah di posisi tersebut.
    ///   • R          → putar rumah 90° sebelum ditempatkan.
    ///   • Scroll     → ganti model rumah (House 01 s/d House 10).
    ///   • Ctrl       → matikan snapping grid.
    ///   • Undo/Redo  → Ctrl+Z / Ctrl+Y.
    /// </summary>
    public class WallTool : MonoBehaviour, ITool
    {
        public string Id => "Wall";

        // ── Inspector / Serialized ─────────────────────────────────────────────
        [Header("House Prefabs (isi dari Assets/Hodaart/.../Prefabs)")]
        [SerializeField] private GameObject[] housePrefabs;

        [Header("Ghost Material (M_Ghost_Valid)")]
        [SerializeField] private Material ghostMaterial;

        [Header("Snapping")]
        [SerializeField] private float gridStep = 1f;

        // ── Runtime State ──────────────────────────────────────────────────────
        private int         _selectedIndex;   // which house prefab is active
        private float       _rotationY;       // current Y rotation in degrees
        private Vector3     _hoverPos;

        private GameObject  _ghostRoot;       // root of ghost preview hierarchy

        // ── References ────────────────────────────────────────────────────────
        private WorldData   _worldData;
        private ToolManager _toolManager;
        private InputReader _inputReader;

        // ── Properties ────────────────────────────────────────────────────────
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetIndex(value);
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            _toolManager = GetComponent<ToolManager>() ?? FindAnyObjectByType<ToolManager>();
        }

        // ── ITool Init ────────────────────────────────────────────────────────
        public void Initialize(WorldData worldData, ToolManager toolManager)
        {
            _worldData   = worldData;
            _toolManager = toolManager;
            SetInputReader(toolManager != null ? toolManager.InputReader : null);

            // Auto-load prefabs if not assigned in Inspector
            if (housePrefabs == null || housePrefabs.Length == 0)
                LoadPrefabsRuntime();

            // Auto-load ghost material if not assigned
            if (ghostMaterial == null)
                ghostMaterial = LoadGhostMaterial();
        }

        public void SetInputReader(InputReader inputReader)
        {
            if (_inputReader == inputReader) return;

            if (_inputReader != null)
            {
                _inputReader.OnScroll           -= HandleScroll;
                _inputReader.OnRightClickPressed -= HandleRightClick;
            }

            _inputReader = inputReader;

            if (_inputReader != null)
            {
                _inputReader.OnScroll           += HandleScroll;
                _inputReader.OnRightClickPressed += HandleRightClick;
            }
        }

        private void OnDestroy()
        {
            SetInputReader(null);
            DestroyGhost();
        }

        // ── ITool Events ──────────────────────────────────────────────────────
        public void OnEnter()
        {
            RebuildGhost();
        }

        public void OnExit()
        {
            DestroyGhost();
        }

        public void OnCancel()
        {
            // nothing to cancel in stamp mode
        }

        /// <summary>Ghost mengikuti kursor setiap frame.</summary>
        public void Tick(Vector3 pointerWorld)
        {
            _hoverPos = pointerWorld;

            if (_ghostRoot == null) RebuildGhost();

            if (_ghostRoot != null)
            {
                bool ctrl   = _toolManager != null && _toolManager.IsFreehandModifierPressed;
                Vector3 pos = SnapToGrid(pointerWorld, ctrl);
                _ghostRoot.transform.position = pos;
                _ghostRoot.transform.rotation = Quaternion.Euler(0f, _rotationY, 0f);
                _ghostRoot.SetActive(true);
            }
        }

        /// <summary>Klik kiri → stamp rumah di posisi snapped.</summary>
        public void OnPressed(Vector3 pointerWorld)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            bool ctrl   = _toolManager != null && _toolManager.IsFreehandModifierPressed;
            Vector3 pos = SnapToGrid(pointerWorld, ctrl);
            PlaceHouse(pos, _rotationY);
        }

        public void OnReleased(Vector3 pointerWorld) { }

        // ── Placement ─────────────────────────────────────────────────────────
        private void PlaceHouse(Vector3 pos, float rotY)
        {
            if (housePrefabs == null || housePrefabs.Length == 0) return;
            var prefab = housePrefabs[_selectedIndex];
            if (prefab == null) return;

            var propData = new PropData(
                System.Guid.NewGuid().ToString("N"),
                prefab.name,
                pos,
                Quaternion.Euler(0f, rotY, 0f),
                Vector3.one);

            if (_worldData != null)
            {
                var cmd = new BuildHouseCommand(_worldData, propData, prefab);
                if (_toolManager != null)
                    _toolManager.History.Execute(cmd);
                else
                    cmd.Do();
            }
            else
            {
                // fallback: langsung spawn tanpa undo
                var go = Instantiate(prefab, pos, Quaternion.Euler(0f, rotY, 0f));
                go.name = $"House_{prefab.name}";
            }
        }

        // ── Ghost ─────────────────────────────────────────────────────────────
        private void RebuildGhost()
        {
            DestroyGhost();

            if (housePrefabs == null || housePrefabs.Length == 0) return;
            var prefab = housePrefabs[_selectedIndex];
            if (prefab == null) return;

            // Instantiate a copy, apply ghost material to all renderers
            _ghostRoot = Instantiate(prefab);
            _ghostRoot.name = "--- House Ghost ---";

            // Disable colliders so ghost doesn't interfere with raycasts
            foreach (var col in _ghostRoot.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            // Replace all materials with ghost (semi-transparent green)
            if (ghostMaterial != null)
            {
                foreach (var rend in _ghostRoot.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = new Material[rend.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = ghostMaterial;
                    rend.sharedMaterials = mats;
                }
            }

            _ghostRoot.SetActive(false);
        }

        private void DestroyGhost()
        {
            if (_ghostRoot != null)
            {
                Destroy(_ghostRoot);
                _ghostRoot = null;
            }
        }

        // ── Index / Rotation helpers ──────────────────────────────────────────
        private void SetIndex(int idx)
        {
            if (housePrefabs == null || housePrefabs.Length == 0) return;
            _selectedIndex = ((idx % housePrefabs.Length) + housePrefabs.Length) % housePrefabs.Length;
            RebuildGhost();
        }

        // ── Input handlers ────────────────────────────────────────────────────
        private void HandleScroll(float delta)
        {
            // Scroll naik = next house, scroll turun = prev house
            if (delta > 0f) SetIndex(_selectedIndex + 1);
            else if (delta < 0f) SetIndex(_selectedIndex - 1);
        }

        private void HandleRightClick(Vector2 screenPos)
        {
            // Kanan klik = putar 90°
            _rotationY = (_rotationY + 90f) % 360f;
        }

        // ── Snapping ──────────────────────────────────────────────────────────
        private Vector3 SnapToGrid(Vector3 raw, bool freehand)
        {
            if (freehand || gridStep <= 0f)
                return new Vector3(raw.x, 0f, raw.z);

            float x = Mathf.Round(raw.x / gridStep) * gridStep;
            float z = Mathf.Round(raw.z / gridStep) * gridStep;
            return new Vector3(x, 0f, z);
        }

        // ── Runtime asset loading ─────────────────────────────────────────────
        private void LoadPrefabsRuntime()
        {
            // Load dari Resources jika tersedia, fallback ke kosong
            var loaded = Resources.LoadAll<GameObject>("Hodaart/Prefabs");
            if (loaded != null && loaded.Length > 0)
            {
                housePrefabs = loaded;
                return;
            }

#if UNITY_EDITOR
            // Di Editor, load langsung via AssetDatabase
            var guids = UnityEditor.AssetDatabase.FindAssets(
                "t:Prefab House",
                new[] { "Assets/Hodaart" });

            var list = new System.Collections.Generic.List<GameObject>();
            foreach (var g in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var prefab  = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) list.Add(prefab);
            }

            // Sort by name agar urutan House 01, 02, ... konsisten
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            housePrefabs = list.ToArray();

            if (housePrefabs.Length == 0)
                Debug.LogWarning("[WallTool] Tidak ada prefab House di Assets/Hodaart. " +
                                 "Assign housePrefabs di Inspector secara manual.");
            else
                Debug.Log($"[WallTool] Loaded {housePrefabs.Length} house prefabs dari Hodaart.");
#endif
        }

        private static Material LoadGhostMaterial()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Art/Materials/M_Ghost_Valid.mat");
#else
            return null;
#endif
        }

        // ── Update (keyboard shortcuts) ───────────────────────────────────────
        private void Update()
        {
            // R → putar 90°
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                _rotationY = (_rotationY + 90f) % 360f;
            }

            // [ / ] → ganti house
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftBracket))
                SetIndex(_selectedIndex - 1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightBracket))
                SetIndex(_selectedIndex + 1);
        }
    }
}
