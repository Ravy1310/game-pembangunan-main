# 🤖 README_AGENT.md: Instruksi untuk AI Agent (Cozy Glade)

Dokumen ini ditujukan untuk **AI agent** (Claude Code, Cursor, Copilot, dll.) yang akan membuat **logika game** *Cozy Glade*, sandbox bangunan 3D santai ala Tiny Glade, memakai aset yang sudah tersedia di proyek.

> Tip: simpan salinan file ini sebagai `CLAUDE.md` (Claude Code) atau `AGENTS.md` (agent lain) di root proyek agar otomatis terbaca.

---

## 0. Cara Memulai (WAJIB dibaca urut)

1. Baca `README.md`: konsep, scope MVP, arsitektur, roadmap fase.
2. Baca `ASSETS.md`: palet, shader, daftar aset (☑ = sudah ada).
3. Baca `README_ASSETS.md`: cara setup aset dan catatan penting.
4. Baca bagian **Kontrak Aset** (bagian 3) di dokumen ini sebelum menulis kode yang menyentuh prefab/material.
5. Kerjakan tugas di bagian **Daftar Tugas** (bagian 6) **satu per satu, berurutan**. Jangan lompat.

### Prompt pembuka (salin ke agent)
```
Baca README_AGENT.md, README.md, ASSETS.md, dan README_ASSETS.md.
Lalu kerjakan Tugas T0 dan T1 dari README_AGENT.md.
Setelah selesai: baca Console Unity, perbaiki semua error, jalankan acceptance
criteria tugas tersebut, lalu laporkan hasil dan tunggu persetujuan sebelum T2.
```

---

## 1. Ringkasan Proyek

| Item | Nilai |
|---|---|
| Genre | Sandbox bangunan 3D, tanpa skor/musuh/fail state |
| Engine | Unity 2022.3 LTS atau Unity 6, **URP** |
| Input | **Input System** package (bukan `UnityEngine.Input` lama) |
| Kamera | Cinemachine (orbit/pan/zoom) |
| Gaya | Cozy low-poly, warna hangat, bayangan lembut |
| Pilar | Tanpa tekanan · menggambar (bukan menaruh) · otomatis & pintar · hidup & hangat |

## 2. Aturan Kerja Agent

**Lakukan:**
- Kerja **satu tugas per iterasi**, commit Git setelah tiap tugas lolos (`git commit -m "T3: wall tool"`).
- Setelah menulis/mengubah script, **baca Console Unity** (lewat MCP jika tersedia: `Unity_ReadConsole`) dan perbaiki error/warning sebelum lanjut.
- Pisahkan **data** dari **visual**: state game disimpan sebagai data murni (`WorldData`); mesh/GameObject dibangun ulang dari data.
- Semua aksi pemain yang mengubah dunia **harus lewat `ICommand`** supaya bisa di-undo/redo.
- Pakai namespace `CozyGlade.*` dan struktur folder pada bagian 4.
- Gunakan `[SerializeField] private` dan ScriptableObject untuk konfigurasi. Tidak ada angka ajaib tersebar; taruh di `GameConfig`.
- Tulis kode kecil dan terbaca. Tambahkan komentar singkat untuk logika matematis (offset poligon, snap, dsb).
- Jika ragu pada keputusan desain, pilih opsi **paling sederhana** yang memenuhi acceptance criteria, lalu catat di `docs/DECISIONS.md`.

**Jangan:**
- ❌ Mengedit/menghapus file di `Assets/_Project/Art/`, `Audio/` (aset). Kalau butuh variasi, buat prefab variant atau material baru.
- ❌ Mengubah isi `Scripts/Editor/CozyAssetSetup.cs` kecuali diminta. Jika ada bug di sana, laporkan.
- ❌ Memakai `FindObjectOfType` di `Update`, alokasi (`new List`, LINQ, string) per frame, atau `GameObject.Find` di runtime.
- ❌ Menambah fitur di luar scope MVP (multiplayer, bangunan bertingkat, terrain sculpting, interior).
- ❌ Menambah package berbayar atau dependensi baru tanpa izin. Package yang diizinkan: Input System, Cinemachine, Splines, TextMeshPro, DOTween (opsional).
- ❌ Menambah timer, skor, atau fail state. Game ini harus santai.

---

## 3. Kontrak Aset (WAJIB dipatuhi)

Semua prefab ada di `Assets/_Project/Prefabs/<Kategori>/<nama>.prefab` (dibuat oleh menu **Cozy Glade > Setup ALL**). Satuan: **1 unit = 1 meter**. Arah depan model = **+Z**.

### 3.1 Dinding & bangunan (`Prefabs/Walls`, `Roofs`)
| Prefab | Ukuran | Pivot | Catatan |
|---|---|---|---|
| `wall_stone_01..03`, `wall_plaster_01` | lebar 1 × tinggi 2,4 × tebal 0,28 | **kiri-bawah** (x 0..1), tengah tebal | memanjang ke **+X lokal** |
| `wall_window_01`, `wall_window_02` | sama | sama | jendela otomatis (slot) |
| `wall_door_01` | sama | sama | pintu |
| `wall_corner_01` | 0,34 × 2,4 × 0,34 | **tengah-bawah** | penutup sudut |
| `wall_base_stone` | 1 × 0,4 × 0,38 | kiri-bawah | fondasi opsional |
| `roof_tile_red`, `roof_tile_blue`, `roof_thatch` | lebar 1, panjang miring ±1,3 | **tepi talang (eave)** | sudah miring 35° ke atas ke arah +Z lokal. **Skala Z** untuk menyesuaikan lebar bangunan |
| `roof_ridge_cap` | panjang 1 (sumbu +X) | ujung kiri | taruh di punggung atap |

**Aturan rotasi modul dinding** (terbukti di `BuildShowcase`):
- Memanjang ke **+X** dunia: rotasi Y = `0`, posisi = titik awal.
- Memanjang ke **+Z** dunia: rotasi Y = `-90` (pos = titik awal).
- Memanjang ke **−X** dunia: rotasi Y = `180`, posisi = titik **akhir**.
- Memanjang ke **−Z** dunia: rotasi Y = `90`, posisi = titik **akhir**.
- Sisi luar (depan) dinding menghadap **+Z lokal**.

**Rumus atap pelana** (rumah panjang X, lebar Z `D` meter, tinggi dinding `H = 2.4`):
- Pitch `θ = 35°`, jarak horizontal eave → punggung `run = D/2 + overhang(0.3)`.
- Skala Z panel = `run / (1.3 * cos θ)`. Tinggi punggung = `H + run * tan θ`.
- Panel depan: pos `(x, H, -overhang)`, rotY 0. Panel belakang: pos `(x+1, H, D+overhang)`, rotY 180.
- Bubungan: `roof_ridge_cap` tiap 1 m di `(x, H + run*tan θ + 0.05, D/2)`.

### 3.2 Pagar, jalan, alam, props
| Prefab | Pivot | Catatan |
|---|---|---|
| `fence_post_01` | tengah-bawah | tinggi ±1 m, jarak antar tiang 1 m |
| `fence_rail_01` | **kiri**, panjang 1 m sepanjang +X | skala X untuk jarak lain |
| `fence_stone_01` | kiri-bawah | lebar 1 m |
| `fence_gate_01` | kiri-bawah | lebar 1 m |
| `path_stone_01..04` | tengah-bawah | batu pijakan ±0,5 m, acak rotasi Y |
| `path_cobble_01` | kiri-bawah (0..1) | ubin 1×1 m |
| `path_edge_01` | kiri | 1 m sepanjang +X |
| `path_bridge_01` | kiri-bawah | 1,2 × 3 m, memanjang +Z |
| `tree_*`, `plant_*`, `rock_*`, `prop_*`, `creature_*`, `water_pond_01` | **tengah-bawah** | skala/rotasi acak ±10% saat ditaruh |
| `plant_ivy_wall` | kiri-bawah | tempel di sisi dinding (z ≈ 0,17) |
| `prop_chimney_01`, `prop_door_step`, `prop_flowerbox`, `prop_window_shutter` | tengah | aksesori dinding/atap |
| `prop_lantern_hang` | titik pasang di dinding | lengan memanjang +X |
| `fx_butterfly` | tengah | punya `ButterflyFlap`; tambah `SimpleWander` (flying) |

### 3.3 Material
Folder `Assets/_Project/Art/Materials/` (dibuat menu Setup):

| Material | Dipakai untuk |
|---|---|
| `M_Palette`, `M_Palette_Wind` | semua prop/model berpalet. `_Wind` untuk pohon/tanaman |
| `M_Wall_Stone`, `M_Wall_Plaster`, `M_Roof_Tile`, `M_Wood` | **mesh prosedural** (dinding/atap/pagar hasil gambar). UV world-space, `_WorldUVScale` bisa diatur |
| `M_Grass_Ground` | tanah. `M_Grass` | rumput instancing (`plant_grass_blade`) |
| `M_Water` | air kolam |
| `M_Ghost_Valid` / `M_Ghost_Invalid` | preview penempatan (hijau / merah) |
| `M_Outline_Highlight` | highlight saat hover tool Hapus |
| `M_FX_*` | partikel |

**Malam hari:** set `_EmissionColor` pada `M_Palette` & `M_Palette_Wind` ke putih × intensitas (jendela & lentera menyala), dan aktifkan komponen `Light` anak pada `prop_lantern_01`, `prop_lantern_hang`, `prop_campfire_01` (default `enabled = false`).

### 3.4 Audio (`Assets/_Project/Audio`)
- Musik: `mus_main_theme` (menu), `mus_day_loop`, `mus_night_loop`.
- Ambience: `amb_birds_day`, `amb_wind_soft`, `amb_crickets_night` (semua loop).
- SFX punya **3 varian** `_a/_b/_c`: `sfx_wall_place_*`, `sfx_roof_pop_*`, `sfx_prop_place_*`, `sfx_erase_*`. Putar acak satu varian + pitch ±5%.
- SFX tunggal: `sfx_ui_click`, `sfx_ui_hover`, `sfx_undo`, `sfx_camera_shutter`.

### 3.5 FX prefab (`Prefabs/FX`)
`fx_build_puff` (one-shot, auto-destroy), `fx_sparkle_place` (one-shot), `fx_chimney_smoke` (loop, taruh di atas cerobong), `fx_leaves_fall`, `fx_dust_motes`, `fx_fireflies` (aktif malam).

### 3.6 UI sprite (`Art/UI`)
Ikon tool: `ui_icon_wall/fence/path/prop/erase`, `ui_icon_undo/redo/time/moon/camera`. Panel/tombol 9-slice: `ui_panel_9slice`, `ui_button_normal/hover/pressed_9slice`. Kursor: `ui_cursor_default/dot/hand/eraser`. Logo: `ui_logo`. Palet UI: krem `#F7EDD8`, teks cokelat `#5A3E2B`. Font: Fredoka/Nunito (unduh sendiri).

---

## 4. Struktur Kode yang Harus Dibuat

```
Assets/_Project/Scripts/
├─ Core/       GameConfig.cs, GameManager.cs, CommandHistory.cs, ICommand.cs, SaveSystem.cs, ServiceLocator-lite (opsional)
├─ Input/      InputReader.cs (+ CozyInput.inputactions di Settings/)
├─ Tools/      ITool.cs, ToolManager.cs, WallTool.cs, FenceTool.cs, PathTool.cs, PropTool.cs, EraseTool.cs, OpeningTool.cs
├─ Building/   WallData.cs, WallMeshBuilder.cs, WallSnapper.cs, RoofGenerator.cs, OpeningPlacer.cs, FenceBuilder.cs, PathBuilder.cs
├─ World/      WorldData.cs, WorldView.cs (data → GameObject), TerrainController.cs, DayNightController.cs, GrassRenderer.cs, AmbientLife.cs
├─ Camera/     CameraRig.cs
├─ Audio/      AudioManager.cs
└─ UI/         ToolbarUI.cs, PropertiesPanel.cs, PauseMenu.cs, PhotoMode.cs
```
Namespace: `CozyGlade.Core`, `CozyGlade.Tools`, `CozyGlade.Building`, `CozyGlade.World`, dst. Buat **asmdef** per modul bila proyek membesar (opsional).

### Alur data
```
InputReader → ToolManager → ITool aktif ─► ICommand ─► CommandHistory (undo/redo)
                                              │ mengubah
                                              ▼
                                         WorldData (WallData[], FenceData[], PathData[], PropData[])
                                              │ diamati oleh
                                              ▼
                                         WorldView → membangun mesh/prefab ──► SaveSystem (JSON)
```

---

## 5. Antarmuka Inti (implementasi awal yang diharapkan)

```csharp
public interface ITool
{
    string Id { get; }
    void OnEnter(); void OnExit();
    void Tick(Vector3 pointerWorld);          // preview tiap frame
    void OnPressed(Vector3 p); void OnReleased(Vector3 p);
    void OnCancel();
}
public interface ICommand { void Do(); void Undo(); }

[Serializable] public class WallData
{
    public string id; public List<Vector3> points = new(); public bool closed;
    public float height = 2.4f, thickness = 0.28f; public WallStyle style;   // Stone, Plaster
    public List<OpeningData> openings = new();                                 // pintu/jendela
    public RoofData roof;                                                      // null jika tidak ada
}
[Serializable] public class OpeningData { public int segment; public float t; public OpeningType type; } // t = 0..1 di segmen
[Serializable] public class RoofData { public RoofType type; public RoofMaterial material; public float pitch = 35f, overhang = 0.3f; }
```
`WorldData` harus bisa diserialisasi dengan `JsonUtility` (hindari `Dictionary`; pakai `List` + id).

---

## 6. Daftar Tugas (kerjakan berurutan)

Format: **Tujuan → Langkah → Acceptance Criteria (AC)**. Agent baru boleh lanjut jika semua AC lolos.

### T0: Verifikasi Setup (±30 menit)
- Cek paket terpasang: URP, Input System, Cinemachine, Splines. Jika belum, tambahkan lewat Package Manager.
- Jalankan **Cozy Glade > Setup ALL** dan **Build Showcase Scene**. Pastikan tidak ada error Console.
- Buat scene `Assets/_Project/Scenes/Sandbox.unity` (ground `M_Grass_Ground` 60×60 m, Directional Light hangat, Volume `PP_Cozy`).
- Buat `docs/DECISIONS.md` (kosong dengan judul).
- **AC:** Showcase scene tampil tanpa material magenta; `Sandbox.unity` ada; Console bersih.

### T1: Input & Kamera
- Buat `CozyInput.inputactions` (Pointer, Click, RightClick, MiddleClick, Scroll, Cancel, Undo, Redo, Confirm, ToolSlots 1..5).
- `InputReader` (event C#, tanpa polling di banyak tempat).
- `CameraRig` + Cinemachine: klik tengah/kanan+drag = orbit, Shift+drag = pan, scroll = zoom (batas min/max, damping halus).
- `GroundRaycaster.TryGetPoint(cam, screenPos, out point)` (bidang y=0).
- **AC:** kamera mulus; kursor di tanah menghasilkan titik dunia yang benar; tidak ada GC alloc per frame (cek Profiler).

### T2: Tool System + Undo/Redo
- `ITool`, `ToolManager` (ganti tool via 1..5 & UI), `ICommand`, `CommandHistory` (Ctrl+Z / Ctrl+Y, tanpa batas).
- Tool dummy `PropTool` yang menaruh `tree_oak_01` saat klik, lewat `PlacePropCommand`.
- Preview ghost memakai `M_Ghost_Valid`/`M_Ghost_Invalid`.
- **AC:** klik menaruh pohon, Ctrl+Z menghapus, Ctrl+Y mengembalikan; `sfx_prop_place_*` terdengar; `fx_sparkle_place` muncul.

### T3: Tool Dinding (inti game)
- `WallData`, `WallTool`: klik = tambah titik, mouse = preview segmen, klik kanan/Enter = selesai, Esc = batal.
- `WallSnapper`: snap ke ujung dinding lain (radius 0,5 m) dan ke sudut 15°/45°/90° (tahan Ctrl = bebas). Tampilkan indikator snap.
- `WallMeshBuilder`: bangun mesh dari polyline (sisi kiri/kanan, atas, ujung), UV **world-space/panjang**, vertex noise kecil (±1 cm) agar terlihat handmade. Material `M_Wall_Plaster`/`M_Wall_Stone`.
- Animasi muncul: naik dari bawah 0,25 dtk dengan overshoot kecil. `sfx_wall_place_*` + `fx_build_puff`.
- Panel properti: tinggi (1,5–4 m), gaya (Stone/Plaster).
- **AC:** bisa menggambar dinding lurus berbentuk bebas; snap bekerja; undo/redo mulus; tidak ada celah/z-fighting di sudut; 60 FPS dengan 100 segmen.

### T4: Pintu & Jendela Otomatis
- `OpeningPlacer`: pada dinding, sediakan slot tiap ±2,5 m. `OpeningTool`: klik dinding → pasang `wall_door_01` / `wall_window_01/02` (snap ke tengah slot terdekat).
- Cara MVP (disarankan): bagi dinding menjadi **modul 1 m**; modul pembuka diganti prefab `wall_*` (tengah slot), sisanya mesh prosedural atau prefab `wall_plaster_01`. Dokumentasikan pilihan di `DECISIONS.md`.
- **AC:** pintu/jendela menempel presisi di dinding lurus; bisa dihapus/diganti; save/load menyimpan `OpeningData`.

### T5: Atap Otomatis
- Deteksi poligon tertutup (titik awal ≈ akhir < 0,5 m) → `closed = true`.
- `RoofGenerator`: (a) atap **piramida/tenda** (poligon → centroid + tinggi), (b) atap **pelana** untuk persegi panjang (sumbu terpanjang). Pakai `roof_tile_*` (panel prefab diskalakan, rumus bagian 3.1) atau mesh prosedural dengan `M_Roof_Tile`.
- Overhang 0,3 m, `roof_ridge_cap` di punggung. Toggle jenis atap + slider tinggi di panel properti.
- **AC:** menggambar kotak tertutup otomatis menghasilkan atap; mengubah dinding memperbarui atap; `sfx_roof_pop_*` terdengar. Jangan implementasikan *straight skeleton* penuh.

### T6: Pagar & Jalan
- `FenceTool`: gambar polyline; spawn `fence_post_01` tiap ±1 m dan `fence_rail_01` di antaranya (skala X).
- `PathTool`: gambar kurva (Splines atau Catmull-Rom); spawn `path_stone_01..04` dengan jarak, rotasi, dan skala acak. Tahan tombol = jalan panjang.
- **AC:** pagar mengikuti kurva rapi; jalan terlihat alami; undo/redo bekerja.

### T7: Dekorasi & Hapus
- `PropTool` dengan palet: pohon, bunga, semak, batu, lentera, bangku, dll. Skala/rotasi acak ±10%. Tahan klik + drag = "sikat" bunga/rumput.
- `EraseTool`: hover → highlight (`M_Outline_Highlight`) → klik hapus (`DeleteCommand`, `sfx_erase_*`).
- **AC:** semua kategori dapat ditaruh dan dihapus; highlight jelas; semua undoable.

### T8: Dunia Hidup
- `GrassRenderer`: `Graphics.RenderMeshInstanced` memakai `plant_grass_blade` + `M_Grass` (target 20–50 ribu helai, area sekitar kamera). Hindari rumput di bawah jalan/dinding (mask sederhana).
- `DayNightController`: 4 preset (Pagi/Siang/Sore/Malam) dengan lerp warna cahaya, intensitas, fog, ambient (nilai di `ASSETS.md` bagian 5). Malam: emission `M_Palette` + `Light` lentera + `fx_fireflies`.
- `AmbientLife`: kupu-kupu (`fx_butterfly`+`SimpleWander flying`), domba (`creature_sheep_01`), `fx_chimney_smoke` otomatis di `prop_chimney_01`.
- `AudioManager`: musik + ambience sesuai waktu (crossfade 2 dtk), SFX acak varian & pitch.
- **AC:** transisi waktu halus; suasana berubah jelas siang→malam; FPS ≥ 60 di PC menengah.

### T9: Save/Load & UI
- `SaveSystem`: JSON di `Application.persistentDataPath`, 3 slot, auto-save tiap 2 menit, versi skema (`version` field).
- UI (uGUI atau UI Toolkit, pilih satu dan catat di DECISIONS): `ToolbarUI` memakai ikon `ui_icon_*`, panel 9-slice, tombol 3 state, kursor per tool, `PauseMenu`, menu utama dengan `ui_logo`, pemilih waktu.
- `PhotoMode`: sembunyikan UI, kamera bebas, `sfx_camera_shutter`, simpan PNG.
- **AC:** simpan → tutup → muat menghasilkan dunia identik; semua tool bisa dipilih lewat UI dan angka 1–5.

### T10: Polish & Optimasi
- Batching/GPU instancing, gabungkan mesh statis, pool partikel/SFX, hindari alokasi per frame.
- Animasi pop/grow/settle pada semua penempatan; transisi UI lembut.
- Uji dengan 5 skenario (rumah kecil, desa, taman, jalan panjang, dunia penuh) dan catat FPS.
- **AC:** tidak ada error/warning Console; FPS stabil; build Windows berhasil.

---

## 7. Standar Kode & Kualitas

- C# 9, `private` secara default, `readonly` bila bisa, `nameof` untuk string, hindari `public` field kecuali data serializable.
- Event C# (`Action<>`) untuk komunikasi, bukan `SendMessage`.
- Cache komponen (`GetComponent` sekali di `Awake`), objek besar di-pool.
- Matematika geometri: gunakan `Vector2` (XZ) untuk perhitungan 2D, hindari akumulasi error float (gunakan toleransi `Mathf.Approximately`/epsilon 1e-4).
- Tulis **tes** (EditMode) untuk: `WallSnapper`, deteksi poligon tertutup, `CommandHistory`, serialisasi `WorldData`.
- Setiap tugas selesai → ringkas di `docs/PROGRESS.md` (apa dibuat, file utama, cara tes, masalah diketahui).

## 8. Definisi Selesai (per tugas)

- [ ] Kode compile tanpa error/warning.
- [ ] Semua AC tugas terpenuhi dan diuji di Editor (Play Mode).
- [ ] Undo/Redo & Save/Load tidak rusak.
- [ ] Console bersih; tidak ada GC alloc per frame di hot path.
- [ ] `docs/PROGRESS.md` diperbarui; commit Git dibuat.

## 9. Jika Ada Masalah

| Gejala | Tindakan |
|---|---|
| Material magenta | Shader `Cozy/*` error compile → baca Console, perbaiki shader / jalankan ulang **Setup ALL** |
| Menu **Cozy Glade** tidak muncul | Script Editor gagal compile (biasanya URP belum terpasang) |
| Prefab tidak ditemukan | Jalankan **Cozy Glade > Setup ALL**; cek path `Assets/_Project/Prefabs/` |
| Dinding/atap terbalik atau menembus | Periksa aturan rotasi & pivot di bagian 3.1 |
| Aset tidak cocok kebutuhan | **Jangan edit aset.** Catat di `docs/DECISIONS.md` dan tanya pengguna |

Jika ada requirement yang ambigu atau bertentangan dengan dokumen lain, **hentikan dan tanyakan** ke pengguna daripada menebak.
