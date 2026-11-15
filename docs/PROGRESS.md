# Progress Log Cozy Glade

## [T0] Verifikasi Setup (Selesai)
- **Status:** Selesai
- **Dependensi Package:** Universal Render Pipeline, Input System, Cinemachine, Splines, TextMeshPro.
- **Struktur Folder:** Diselaraskan ke `Assets/_Project/`.
- **Scene & Profil:** `PP_Cozy.asset` dan `Sandbox.unity` disiapkan.

---

## [T1] Input & Kamera (Selesai)
- **Status:** Selesai
- **Input System:** `CozyInput.inputactions` dan `InputReader.cs` terimplementasi.
- **Ground Raycaster:** `GroundRaycaster.cs` ($y = 0$, zero GC).
- **Camera Rig:** `CameraRig.cs` orbit, pan, zoom damping mulus.

---

## [Slice S1] Auto-Bootstrap & Toolbar UI (Selesai)
- **Status:** Selesai & Terverifikasi di Play Mode pada AssetShowcase dan scene kosong.
- **Auto-Bootstrapper:**
  - `GameBootstrapper.cs` berjalan otomatis pada Play Mode di scene manapun (`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`).
  - Menjamin ketersediaan Ground plane 60×60m, Directional Light hangat, dan Ambient/Fog.
  - Menyiapkan `Main Camera` dan `CameraRig` secara otomatis jika belum ada.
  - Menginisialisasi `WorldData`, `WorldView`, `ToolManager`, dan `InputReader`.
- **Toolbar UI:**
  - `ToolbarUI.cs` di-instantiate otomatis di Canvas ScreenSpaceOverlay (1920×1080 scaler).
  - Menyediakan tombol tool: **Wall [1]**, **Opening**, **Fence [2]**, **Path [3]**, **Prop [4]**, **Erase [5]**.
  - Menyediakan tombol aksi: **Undo [Ctrl+Z]**, **Redo [Ctrl+Y]**.
  - Tombol dilengkapi ikon 9-slice, status highlight/glow saat aktif, dan terhubung langsung ke `ToolManager`.

---

## [Slice S2 & S3] Dinding, Openings (Pintu & Jendela) & Atap Otomatis (Selesai)
- **Status:** Selesai
- **Wall & Openings:**
  - `WallData.cs`, `OpeningPlacer.cs`, `ModifyWallOpeningsCommand.cs`, `OpeningTool.cs`.
  - Pintu (`wall_door_01`) dan Jendela (`wall_window_01`, `wall_window_02`) menempel presisi di dinding, siklus klik untuk mengganti atau menghapus.
- **Roof Generator:**
  - `RoofGenerator.cs`: Gable roof untuk kotak 4-sisi dan Pyramid/Tent roof untuk poligon tertutup lainnya.
  - Skala panel eave-to-ridge otomatis sesuai rumus pitch 35°, ridge cap, material Red Tile, Blue Tile, Thatch, audio `sfx_roof_pop_*`.

---

## [Slice S4] Pagar & Jalan (Fences & Paths) (Selesai)
- **Status:** Selesai
- **Fence System:**
  - `FenceData.cs`, `FenceBuilder.cs`, `BuildFenceCommand.cs`, `FenceTool.cs`.
  - Wood Post & Rail: `fence_post_01` tiap node + subdivisi interval ~1m, `fence_rail_01` diskalakan sumbu X sesuai jarak, rotasi miter/arah segmen.
  - Stone Fence: `fence_stone_01`.
- **Path System:**
  - `PathData.cs`, `PathBuilder.cs`, `BuildPathCommand.cs`, `PathTool.cs`.
  - Stepping Stones: interpolasi polyline 0.6m, variasi `path_stone_01..04`, rotasi Y acak (0..360°), jitter lateral ±0.15m, jitter skala 0.85–1.15x.
  - Cobblestone: `path_cobble_01`.
- **Audio & Undo/Redo:**
  - Terhubung ke `CommandHistory` dengan `sfx_prop_place_*`, `sfx_erase_*`, dan partikel puff/sparkle.

---

## [Perbaikan] Dinding Solid 3D + Drag UX + Ikon Toolbar (`perbaikan.md`) (Selesai)
- **Status:** Selesai & Lolos 100% Seluruh Acceptance Criteria (EditMode Tests + 4 Screenshots)
- **1. Solid 3D `WallMeshBuilder`:**
  - Ditulis ulang total sesuai spesifikasi di `perbaikan.md`.
  - Setiap segmen $i \to j$ menghasilkan 4 quad permukaan flat-shaded solid (Sisi A $+nrm$, Sisi B $-nrm$, Sisi Atas $+Y$, Sisi Bawah $-Y$) plus 2 quad penutup ujung (Start $-dir$, End $+dir$) untuk poligon terbuka.
  - Sambungan sudut mitre presisi: $m = (n_0 + n_1).normalized \cdot (half / \max(Dot(m, n_0), 0.3))$, baik untuk polyline terbuka maupun closed polygon.
  - Quad helper dengan automatic winding verification (`Vector3.Dot(Vector3.Cross(b - a, c - a), expected) > 0`) memastikan seluruh sisi memiliki normal luar dan tidak ter-cull oleh `Cull Back`.
  - Alokasi nol (zero-GC) untuk preview drag melalui parameter `reuse: mesh`.
- **2. Preview Solid Saat Drag:**
  - Ghost preview menggunakan builder yang sama (`WallMeshBuilder.BuildWallMesh(previewWall, false, reuse: _ghostMeshFilter.sharedMesh)`).
  - Menggunakan material `M_Ghost_Valid` (hijau) dan otomatis beralih ke `M_Ghost_Invalid` (merah) jika segmen < 0.5m (terlalu pendek).
- **3. Perbaikan UX Drag:**
  - Angle snapping default ke kelipatan 15° (dengan 0°, 45°, 90° sebagai sumbu utama).
  - Length snapping ke kelipatan 0.25m.
  - Endpoint snapping radius 0.5m ke ujung dinding lain dan penutupan poligon (< 0.5m ke titik awal).
  - Tahan `Ctrl` = mode freehand tanpa snap.
  - Minimum panjang 0.5m: drag lebih pendek otomatis dibatalkan/direset, mencegah dinding sampah/0-length.
  - `EventSystem.current.IsPointerOverGameObject()` mencegah interaksi drag dimulai saat kursor berada di atas UI.
- **4. Perbaikan Ikon Toolbar:**
  - Semua meta file tekstur UI (`Assets/_Project/Art/UI/Icons/`, `Panels/`) dikonfigurasi ke `textureType: 8` (Sprite 2D/UI) dengan `alphaIsTransparency: 1` dan `enableMipMap: 0`.
  - `ui_panel_9slice` border: (36, 50, 36, 36), `ui_button_*_9slice` border: (32, 40, 32, 30).
  - `ToolbarUI.cs` mengimplementasikan fallback loader aman (`LoadSpriteSafe`) yang mampu membaca PNG disk langsung via `Texture2D` + `Sprite.Create` jika belum di-import oleh AssetDatabase.
  - Tombol berukuran 72×72, child `Icon` berukuran 44×44 dengan `preserveAspect = true` dan `color = Color.white` (tidak lagi cokelat polos).
  - Label teks di bawah ikon: "Wall", "Open", "Fence", "Path", "Prop", "Erase", "Undo", "Redo".
  - Tool aktif memiliki highlight glow dan membesar (1.08x).
- **5. Hasil Pengujian EditMode (`Tests/EditMode/WallMeshBuilderTests.cs`):**
  - `Test01_SingleSegment_HasCorrectBounds`: **PASSED** (size ≈ 4.0m × 2.4m × 0.28m).
  - `Test02_SingleSegment_HasAtLeast24VerticesAnd12Triangles`: **PASSED** (24 vertices, 12 triangles).
  - `Test03_AllTriangles_NormalsFaceOutward`: **PASSED** (semua normal outward, vertex normal cocok).
  - `Test04_LPolyline_MitreJoint_NoGaps`: **PASSED** (sambungan mitre 90° menyatu tanpa celah).
  - `Test05_ClosedBox4x3_HasCorrectBoundsAndNoDegenerateTriangles`: **PASSED** (64 vertices, 32 triangles, 0 degenerate).
- **6. Hasil Screenshot Play Mode (`docs/screenshots/`):**
  - `01_wall_top_view.png`: Dinding tampak sebagai garis bertebal 0.28m dengan sudut tersambung rapi.
  - `02_wall_side_perspective.png`: Tebal dinding solid, sisi atas rata, tidak transparan dari sudut manapun.
  - `03_wall_building_corner.png`: Bangunan persegi 4×3m menyambung kokoh dan solid.
  - `04_toolbar_ui_icons.png`: Seluruh ikon UI tampil tajam dan presisi dengan panel krem 9-slice.
