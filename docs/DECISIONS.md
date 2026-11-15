# Architecture & Design Decisions

Catatan keputusan desain dan arsitektur untuk proyek Cozy Glade.

---

## D-001: Lokasi Root Aset di Assets/_Project
- **Konteks:** File `Art`, `Audio`, `Prefabs`, `Scripts` diekstrak langsung di bawah `Assets/`, padahal `CozyAssetSetup.cs` dan dokumen `readme.md` menetapkan kontrak `Assets/_Project/`.
- **Keputusan:** Memindahkan `Art`, `Audio`, `Prefabs`, `Scripts` ke dalam `Assets/_Project/` agar sesuai dengan kontrak `CozyAssetSetup.cs` dan standar arsitektur proyek.

---

## D-002: Modular Openings via 1m Prefab Slots dan Procedural Cutout
- **Konteks:** Pintu dan jendela berukuran presisi 1m x 2.4m x 0.28m (`wall_door_01`, `wall_window_01`, `wall_window_02`).
- **Keputusan:** Mengikuti saran MVP di `readme.md`: Dinding prosedural memotong slot selebar 1m di lokasi opening, lalu menaruh prefab modul pintu/jendela di slot tersebut dengan orientasi yang pas mengikuti arah segmen. Undo/Redo dan serialisasi menyimpan `OpeningData` di `WallData`.

---

## D-003: Atap Otomatis Pelana (Gable) dan Piramida (Pyramid)
- **Konteks:** Dinding tertutup (`closed = true`) harus otomatis memunculkan atap pelana untuk ruangan persegi panjang atau piramida untuk poligon sembarang.
- **Keputusan:** Mengimplementasikan `RoofGenerator` yang mendeteksi orientasi sumbu poligon. Pada atap pelana, menggunakan modul prefab `roof_tile_red/blue/thatch` dengan skala Z sesuai rumus `run / (1.3 * cos 35°)` dan `roof_ridge_cap` di bubungan. Ujung segitiga pelana diisi mesh prosedural gable wall bermaterial dinding.
