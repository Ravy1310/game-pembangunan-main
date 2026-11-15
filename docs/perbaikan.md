# 🛠️ perbaikan.md: Dinding Masih Seperti Papan Tipis (Bukan 3D)

> Konteks: game sudah jalan (bootstrap + toolbar muncul), tapi hasil drag dinding **bukan bangunan 3D**. Dinding tampak seperti **papan/garis datar tipis**, arahnya berantakan, tanpa tebal, tanpa sisi atas, dan sudutnya tidak menyambung. Toolbar juga hanya berisi **kotak cokelat polos tanpa ikon**.
> Aset (`Art/`, `Audio/`, shader, material) **sudah benar. JANGAN diubah.** Perbaikan hanya di kode logika/UI.

---

## 🔴 PROMPT (salin ke agent)

```
Baca perbaikan.md sampai habis. Hasil drag dinding saat ini hanya berupa papan
tipis (quad datar), bukan dinding 3D bertebal. Toolbar juga tidak menampilkan ikon.

Perbaiki secara berurutan:
 1) WallMeshBuilder: tulis ulang agar menghasilkan SOLID 3D (sisi A, sisi B, sisi
    atas, tutup ujung, sambungan sudut mitre) sesuai spesifikasi di perbaikan.md.
 2) Preview saat drag harus memakai builder yang SAMA (solid, bukan garis).
 3) Perbaiki UX drag (snap sudut, panjang minimum, tidak menumpuk).
 4) Perbaiki ikon toolbar (sprite harus tampil).
Jangan ubah aset. Setelah itu verifikasi sendiri di Play Mode: ambil screenshot
dari 3 sudut (atas, samping, sudut bangunan) dan jalankan tes EditMode yang
ada di perbaikan.md. Jangan nyatakan selesai sebelum semua Acceptance Criteria
terpenuhi dan kamu melampirkan bukti.
```

---

## 1. Diagnosis (apa yang terlihat di screenshot)

| Gejala | Penyebab paling mungkin |
|---|---|
| Dinding tampak seperti papan datar | Mesh hanya **1 quad per segmen** (tanpa ketebalan / tanpa sisi lain / tanpa sisi atas) |
| Dari beberapa sudut dinding **hilang/transparan** | Quad satu sisi + `Cull Back` → sisi belakang tidak dirender |
| Ujung dan sudut tidak menyambung, ada celah | Tidak ada **tutup ujung** dan **join mitre** antar segmen |
| Arah dinding acak/miring | Tidak ada **snap sudut**, titik langsung mengikuti mouse |
| Bayangan berupa jajar genjang tipis | Efek samping mesh tipis (akan hilang setelah solid) |
| Dinding polos tanpa tekstur | UV belum diisi / material belum `M_Wall_Plaster`/`M_Wall_Stone` dengan UV world-space |
| Toolbar kotak cokelat tanpa ikon | `Image.sprite` tidak terisi (sprite tidak ter-load dari `GameAssets`), atau `Image.color` gelap menutupi ikon |

---

## 2. Spesifikasi `WallMeshBuilder` (WAJIB solid 3D)

**Input:** polyline titik di bidang XZ (`y = 0`), flag `closed`, `height` (default 2,4), `thickness` (default 0,28).
**Output:** satu `Mesh` berisi **solid tertutup** (kecuali dasar yang tidak terlihat).

Untuk setiap titik `i` hitung dua titik offset **mitre**: `L_i` (sisi A) dan `R_i` (sisi B), berjarak `thickness/2` dari garis tengah.

Untuk setiap segmen `i → j` buat **4 permukaan** (masing-masing quad dengan vertex sendiri agar flat-shaded):

| Permukaan | Titik | Normal |
|---|---|---|
| Sisi A | `L_i(0), L_j(0), L_j(H), L_i(H)` | `+nrm` (kiri segmen) |
| Sisi B | `R_i(0), R_j(0), R_j(H), R_i(H)` | `−nrm` |
| **Atas** | `L_i(H), L_j(H), R_j(H), R_i(H)` | `+Y` |
| Tutup ujung (hanya ujung **terbuka**) | `L,R` di titik awal / akhir | `∓dir` |

Aturan wajib:
- Winding harus benar: **front face = `Cross(b−a, c−a)` searah normal yang diharapkan** (Unity). Pakai helper yang otomatis membalik urutan jika salah.
- Join mitre: `miter = (n0 + n1).normalized`, panjang offset `= (thickness/2) / max(Dot(miter, n0), 0.3)` (batasi agar sudut tajam tidak meledak). Untuk polyline **closed**, titik pertama & terakhir juga di-mitre.
- Titik ujung terbuka memakai normal segmen langsung (tanpa mitre).
- UV: sisi memakai `u = jarak kumulatif sepanjang dinding × uvScale`, `v = y × uvScale`; sisi atas memakai `world XZ × uvScale` (cocok dengan `M_Wall_Plaster` yang `_WorldUV`).
- Normal diisi **eksplisit** (jangan andalkan `RecalculateNormals` untuk flat shading), `RecalculateBounds()`.
- Tambahkan `MeshCollider` (atau `BoxCollider` per segmen) untuk tool Hapus/Pintu.
- Material dinding **`Cull Back` sudah default**; pastikan mesh solid agar terlihat dari semua sisi, jangan "memperbaiki" dengan Cull Off.
- Reuse list/array (tanpa alokasi per frame) saat membangun preview.

### Implementasi acuan (belum teruji; sesuaikan bila perlu)
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CozyGlade.Building
{
    public static class WallMeshBuilder
    {
        public static Mesh Build(IReadOnlyList<Vector3> pts, bool closed, float height, float thickness,
                                 float uvScale = 0.5f, Mesh reuse = null)
        {
            int n = pts.Count;
            var mesh = reuse != null ? reuse : new Mesh();
            mesh.Clear();
            if (n < 2) return mesh;

            var V = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var T = new List<int>();
            float half = thickness * 0.5f;

            // --- offset (mitre) points per vertex
            var L = new Vector3[n]; var R = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                bool hasPrev = closed || i > 0, hasNext = closed || i < n - 1;
                Vector3 p = Flat(pts[i]);
                Vector3 n0 = Vector3.zero, n1 = Vector3.zero;
                if (hasPrev) n0 = LeftNormal(Flat(pts[(i - 1 + n) % n]), p);
                if (hasNext) n1 = LeftNormal(p, Flat(pts[(i + 1) % n]));
                Vector3 m;
                if (hasPrev && hasNext)
                {
                    m = (n0 + n1); m = m.sqrMagnitude < 1e-6f ? n0 : m.normalized;
                    float d = Mathf.Max(Vector3.Dot(m, n0), 0.3f);
                    m *= half / d;
                }
                else m = (hasPrev ? n0 : n1) * half;
                L[i] = p + m; R[i] = p - m;
            }

            int segs = closed ? n : n - 1;
            float dist = 0f;
            for (int s = 0; s < segs; s++)
            {
                int i = s, j = (s + 1) % n;
                Vector3 a = Flat(pts[i]), b = Flat(pts[j]);
                Vector3 dir = (b - a).normalized; Vector3 nrm = LeftNormal(a, b);
                float len = Vector3.Distance(a, b);
                Vector3 up = Vector3.up * height;
                float u0 = dist * uvScale, u1 = (dist + len) * uvScale, vH = height * uvScale;

                // sisi A (+nrm) dan sisi B (-nrm)
                Quad(V, N, U, T, L[i], L[j], L[j] + up, L[i] + up, nrm, new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, vH), new Vector2(u0, vH));
                Quad(V, N, U, T, R[i], R[j], R[j] + up, R[i] + up, -nrm, new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, vH), new Vector2(u0, vH));
                // sisi atas
                Vector3 a1 = L[i] + up, b1 = L[j] + up, c1 = R[j] + up, d1 = R[i] + up;
                Quad(V, N, U, T, a1, b1, c1, d1, Vector3.up, XZ(a1, uvScale), XZ(b1, uvScale), XZ(c1, uvScale), XZ(d1, uvScale));
                // tutup ujung (hanya dinding terbuka)
                if (!closed && s == 0)
                    Quad(V, N, U, T, R[i], L[i], L[i] + up, R[i] + up, -dir, Vector2.zero, new Vector2(thickness * uvScale, 0), new Vector2(thickness * uvScale, vH), new Vector2(0, vH));
                if (!closed && s == segs - 1)
                    Quad(V, N, U, T, L[j], R[j], R[j] + up, L[j] + up, dir, Vector2.zero, new Vector2(thickness * uvScale, 0), new Vector2(thickness * uvScale, vH), new Vector2(0, vH));
                dist += len;
            }

            mesh.indexFormat = V.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(V); mesh.SetNormals(N); mesh.SetUVs(0, U); mesh.SetTriangles(T, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0f, p.z);
        static Vector3 LeftNormal(Vector3 a, Vector3 b) { var d = (b - a).normalized; return new Vector3(-d.z, 0f, d.x); }
        static Vector2 XZ(Vector3 p, float s) => new Vector2(p.x * s, p.z * s);

        // quad dengan winding otomatis agar front-face searah 'expected'
        static void Quad(List<Vector3> V, List<Vector3> N, List<Vector2> U, List<int> T,
                         Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 expected,
                         Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            int o = V.Count;
            V.Add(a); V.Add(b); V.Add(c); V.Add(d);
            N.Add(expected); N.Add(expected); N.Add(expected); N.Add(expected);
            U.Add(ua); U.Add(ub); U.Add(uc); U.Add(ud);
            bool ok = Vector3.Dot(Vector3.Cross(b - a, c - a), expected) > 0f;
            if (ok) { T.Add(o); T.Add(o + 1); T.Add(o + 2); T.Add(o); T.Add(o + 2); T.Add(o + 3); }
            else    { T.Add(o); T.Add(o + 2); T.Add(o + 1); T.Add(o); T.Add(o + 3); T.Add(o + 2); }
        }
    }
}
```

> Jika ada detail yang tidak cocok dengan struktur proyek, agent boleh menyesuaikan, **tetapi semua ketentuan di tabel di atas tidak boleh dikurangi** (sisi A, sisi B, atas, tutup ujung, mitre, normal & winding benar).

---

## 3. Perbaikan Alur Drag (UX)

1. **Preview solid:** saat drag, tampilkan dinding **solid** memakai `WallMeshBuilder` yang sama dengan material `M_Ghost_Valid` (merah `M_Ghost_Invalid` bila terlalu pendek/menembus). Dilarang memakai `LineRenderer` atau quad datar sebagai preview.
2. **Update preview tanpa alokasi:** satu `MeshFilter` preview, panggil `Build(..., reuse: previewMesh)` tiap frame saat mouse bergerak.
3. **Snap default aktif:** sudut ke kelipatan **15°** (utama 0/45/90) terhadap segmen sebelumnya atau sumbu dunia, panjang ke kelipatan **0,25 m**, titik ke ujung dinding lain (radius 0,5 m). **Ctrl = bebas.** Tampilkan indikator snap kecil.
4. **Panjang minimum 0,5 m:** drag lebih pendek = batal (tidak membuat dinding sampah). Tampilkan label panjang (m).
5. **Rantai & penutupan:** tekan dekat ujung terakhir → lanjut rantai; titik akhir ≈ titik awal (< 0,5 m) → `closed = true` (siap atap).
6. **Satu `WallData` = satu rantai** (bukan satu GameObject per segmen): seluruh polyline dibangun jadi **satu mesh** sehingga sudutnya mitre. Saat rantai berubah, bangun ulang mesh-nya.
7. **Animasi muncul:** naikkan lewat **skala Y transform** (pivot di dasar, 0 → 1 dalam 0,25 dtk dengan overshoot kecil), **bukan** membangun ulang mesh tiap frame.
8. Mouse di atas UI → jangan mulai drag (`IsPointerOverGameObject`).

---

## 4. Perbaikan Ikon Toolbar (kotak cokelat polos)

Gejala: tombol terlihat sebagai kotak gelap tanpa ikon. Periksa dan perbaiki:

- [ ] `Image.sprite` ikon terisi dari `GameAssets` (`Assets.Sprite("ui_icon_wall")`, dst.). Log peringatan bila sprite `null`.
- [ ] Ikon dibuat sebagai **child `Image` terpisah** di dalam tombol (bukan memakai `Button.targetGraphic` sebagai ikon sekaligus latar), dengan `preserveAspect = true`, `color = Color.white`.
- [ ] Latar tombol memakai `ui_button_normal_9slice` (`Image.type = Sliced`), state hover/pressed lewat `SpriteState` atau swap sprite. **Jangan** mewarnai latar menjadi cokelat gelap.
- [ ] Panel toolbar memakai `ui_panel_9slice` (`Image.type = Sliced`).
- [ ] `GameAssets` sudah terisi sprite UI (jalankan **Cozy Glade > Populate GameAssets** bila ada) dan sprite berada di tipe `Sprite (2D and UI)`.
- [ ] Ukuran tombol ±72×72, ikon ±48×48, ada **label teks kecil** / tooltip nama tool.
- [ ] Tool aktif jelas: tombol sedikit membesar + latar `ui_button_hover_9slice`.

---

## 5. Acceptance Criteria (WAJIB lolos + sertakan bukti)

**Geometri (tes EditMode otomatis, tulis di `Tests/EditMode/WallMeshBuilderTests.cs`):**
1. Segmen tunggal 4 m, tebal 0,28, tinggi 2,4 → `mesh.bounds.size` ≈ `(4, 2.4, 0.28)` (toleransi 0,01).
2. Jumlah vertex ≥ 24 dan triangle ≥ 12 (A, B, atas, 2 tutup ujung).
3. **Semua normal menghadap keluar:** untuk tiap triangle, `Dot(faceNormal, centroidTriangle − centroidMesh)` pada sisi luar bernilai positif (atau normal sama dengan normal eksplisit).
4. Polyline L (2 segmen siku 90°): tidak ada celah di sudut (titik offset mitre di sudut bertemu; luas atas kontinu).
5. Polyline **closed** persegi 4×3: tanpa tutup ujung; bounds benar; tidak ada degenerate triangle (luas > 1e-6).

**Visual (Play Mode, lampirkan 3 screenshot):**
1. Dari **atas**: dinding terlihat sebagai garis bertebal dengan sudut tersambung rapi.
2. Dari **samping/perspektif**: terlihat jelas tebal dinding, sisi atas rata, tidak transparan dari sudut mana pun.
3. **Persegi tertutup** 4×3 m: keempat dinding menyambung dan dari luar tampak seperti bangunan solid (siap diberi atap).
4. Saat drag, **preview juga solid** (bukan garis).
5. Toolbar menampilkan **ikon** untuk Dinding, Pagar, Jalan, Dekor, Hapus, Undo, Redo.

**Fungsional:**
1. Drag dengan snap menghasilkan dinding rapi lurus 0°/45°/90°.
2. Ctrl+Z / Ctrl+Y bekerja untuk dinding baru.
3. FPS tetap ≥ 60 dengan 100 segmen; **tidak ada GC alloc** saat menggerakkan mouse di mode drag.
4. Console: 0 error.

---

## 6. Urutan Kerja

1. Tulis `WallMeshBuilder` + **tes EditMode** → jalankan, pastikan hijau.
2. Hubungkan ke `WallView`/`WorldView`: satu `WallData` → satu GameObject dengan `MeshFilter`, `MeshRenderer` (`M_Wall_Plaster`), `MeshCollider`.
3. Ganti preview drag ke mesh solid + material ghost.
4. Terapkan snap, panjang minimum, rantai, penutupan.
5. Perbaiki ikon toolbar.
6. Verifikasi Play Mode + 3 screenshot + laporan di `docs/PROGRESS.md`, lalu commit (`git commit -m "fix: solid 3D wall mesh + drag UX + toolbar icons"`).

**Dilarang:** mengubah aset, memakai `Cull Off` untuk menutupi mesh tipis, menyatakan selesai tanpa screenshot dan tes, atau membuat dinding dari quad/scale-cube per segmen tanpa join mitre (jika memakai cube per segmen, wajib ada cap sudut yang menutup celah dan hasilnya tetap harus lolos Acceptance Criteria).
