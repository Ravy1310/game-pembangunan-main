#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using CozyGlade.Fx;

namespace CozyGlade.EditorTools
{
    // ------------------------------------------------------------------ import settings
    public class CozyAssetPostprocessor : AssetPostprocessor
    {
        const string Root = "Assets/_Project/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root + "Art/Models/")) return;
            var m = (ModelImporter)assetImporter;
            m.materialImportMode = ModelImporterMaterialImportMode.None;
            m.globalScale = 1f;
            m.importCameras = false; m.importLights = false; m.importBlendShapes = false;
            m.meshCompression = ModelImporterMeshCompression.Off;
            m.isReadable = false;
            m.importNormals = ModelImporterNormals.Import;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root + "Art/")) return;
            var t = (TextureImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);

            if (assetPath.Contains("/Art/UI/"))
            {
                if (assetPath.Contains("/Cursors/")) { t.textureType = TextureImporterType.Cursor; t.mipmapEnabled = false; return; }
                t.textureType = TextureImporterType.Sprite;
                t.spriteImportMode = SpriteImportMode.Single;
                t.mipmapEnabled = false; t.alphaIsTransparency = true; t.spritePixelsPerUnit = 100;
                if (name == "ui_panel_9slice") t.spriteBorder = new Vector4(36, 50, 36, 36);          // L,B,R,T
                else if (name.StartsWith("ui_button_")) t.spriteBorder = new Vector4(32, 40, 32, 30);
                return;
            }
            if (name.StartsWith("T_Palette"))
            {
                t.filterMode = FilterMode.Point; t.mipmapEnabled = false; t.wrapMode = TextureWrapMode.Clamp;
                t.textureCompression = TextureImporterCompression.Uncompressed; return;
            }
            if (assetPath.Contains("/Textures/VFX/"))
            {
                t.alphaIsTransparency = true; t.mipmapEnabled = true; t.wrapMode = TextureWrapMode.Clamp; return;
            }
            if (name.StartsWith("T_"))
            {
                t.wrapMode = TextureWrapMode.Repeat; t.mipmapEnabled = true; t.filterMode = FilterMode.Bilinear; t.anisoLevel = 4;
            }
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root + "Audio/")) return;
            var a = (AudioImporter)assetImporter;
            var s = a.defaultSampleSettings;
            bool sfx = assetPath.Contains("/SFX/");
            s.loadType = sfx ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = sfx ? 0.7f : 0.6f;
            a.defaultSampleSettings = s;
            a.loadInBackground = !sfx;
        }
    }

    // ------------------------------------------------------------------ menu tools
    public static class CozyAssetSetup
    {
        const string P = "Assets/_Project/";
        const string MatDir = P + "Art/Materials/";
        const string PrefabDir = P + "Prefabs/";

        [MenuItem("Cozy Glade/Setup ALL (Materials + Prefabs + FX + Post FX)")]
        public static void SetupAll()
        {
            AssetDatabase.Refresh();
            BuildMaterials();
            BuildPrefabs();
            BuildFx();
            BuildPostProcessProfile();
            Debug.Log("[Cozy Glade] Setup complete. Try: Cozy Glade > Build Showcase Scene");
        }

        // ---------------- helpers
        static void Ensure(string dir)
        {
            if (AssetDatabase.IsValidFolder(dir.TrimEnd('/'))) return;
            string parent = Path.GetDirectoryName(dir.TrimEnd('/')).Replace('\\', '/');
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(dir.TrimEnd('/')));
        }
        static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
        static Texture2D Tex(string rel) => Load<Texture2D>(P + "Art/Textures/" + rel);

        static Material Mat(string name, string shaderName, Action<Material> setup)
        {
            Ensure(MatDir);
            var shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogError("[Cozy Glade] Shader not found: " + shaderName + " (check the Console for shader compile errors)"); return null; }
            string path = MatDir + name + ".mat";
            var m = Load<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            else m.shader = shader;
            setup(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Toon(Material m, Texture2D baseMap, Color? color = null, bool worldUV = false, float uvScale = 0.5f)
        {
            if (baseMap) m.SetTexture("_BaseMap", baseMap);
            m.SetColor("_BaseColor", color ?? Color.white);
            m.SetFloat("_WorldUV", worldUV ? 1 : 0);
            if (worldUV) m.EnableKeyword("_WORLDUV_ON"); else m.DisableKeyword("_WORLDUV_ON");
            m.SetFloat("_WorldUVScale", uvScale);
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

        // ---------------- materials
        [MenuItem("Cozy Glade/1) Build Materials")]
        public static void BuildMaterials()
        {
            var pal = Tex("T_Palette.png"); var emi = Tex("T_Palette_Emission.png");
            Action<Material, float> palette = (m, wind) =>
            {
                Toon(m, pal);
                m.SetTexture("_EmissionMap", emi); m.SetFloat("_UseEmission", 1); m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);          // set to white*intensity at night (DayNight script)
                m.SetFloat("_WindStrength", wind);
            };
            Mat("M_Palette", "Cozy/ToonLit", m => palette(m, 0f));
            Mat("M_Palette_Wind", "Cozy/ToonLit", m => palette(m, 0.07f));
            Mat("M_Wall_Stone", "Cozy/ToonLit", m => Toon(m, Tex("T_Wall_Stone.png"), null, true, 0.5f));
            Mat("M_Wall_Plaster", "Cozy/ToonLit", m => Toon(m, Tex("T_Wall_Plaster.png"), null, true, 0.4f));
            Mat("M_Roof_Tile", "Cozy/ToonLit", m => Toon(m, Tex("T_Roof_Tile.png"), null, true, 0.55f));
            Mat("M_Wood", "Cozy/ToonLit", m => Toon(m, Tex("T_Wood.png"), null, true, 0.8f));
            Mat("M_Grass_Ground", "Cozy/ToonLit", m => { Toon(m, null, Hex("A8D672")); m.SetFloat("_RimStrength", 0f); });
            Mat("M_Grass", "Cozy/Grass", m => { });
            Mat("M_Water", "Cozy/Water", m => { });
            Mat("M_Ghost_Valid", "Cozy/Ghost", m => m.SetColor("_Color", new Color(0.45f, 0.85f, 0.5f, 0.45f)));
            Mat("M_Ghost_Invalid", "Cozy/Ghost", m => m.SetColor("_Color", new Color(0.92f, 0.38f, 0.33f, 0.45f)));
            Mat("M_Outline_Highlight", "Cozy/Ghost", m => { m.SetColor("_Color", new Color(1f, 0.9f, 0.5f, 0.55f)); m.SetFloat("_FresnelPower", 1.5f); m.SetFloat("_FresnelBoost", 0.5f); });

            Action<string, string, Color, bool> fx = (n, tex, tint, add) => Mat(n, "Cozy/ParticleSoft", m =>
            {
                m.SetTexture("_MainTex", Tex("VFX/" + tex)); m.SetColor("_TintColor", tint); m.SetFloat("_DstBlend", add ? 1 : 10);
            });
            fx("M_FX_Smoke", "fx_smoke_puff.png", new Color(1, 1, 1, 1), false);
            fx("M_FX_Leaf", "fx_leaf.png", Color.white, false);
            fx("M_FX_Sparkle", "fx_sparkle.png", Color.white, true);
            fx("M_FX_Dust", "fx_dust_mote.png", Color.white, true);
            fx("M_FX_Glow", "fx_firefly_glow.png", Color.white, true);
            AssetDatabase.SaveAssets();
            Debug.Log("[Cozy Glade] Materials built.");
        }

        // ---------------- prefabs
        static Material M(string n) => Load<Material>(MatDir + n + ".mat");

        static Material PickMaterial(string root, string node)
        {
            if (node.Contains("_water")) return M("M_Water");
            if (root.StartsWith("plant_grass")) return M("M_Grass");
            if (root.StartsWith("tree_") || root.StartsWith("plant_") || root == "fx_butterfly") return M("M_Palette_Wind");
            return M("M_Palette");
        }

        [MenuItem("Cozy Glade/2) Build Prefabs From Models")]
        public static void BuildPrefabs()
        {
            if (M("M_Palette") == null) BuildMaterials();
            var guids = AssetDatabase.FindAssets("t:Model", new[] { P + "Art/Models" });
            int n = 0;
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.EndsWith(".obj")) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                string folder = Path.GetFileName(Path.GetDirectoryName(path));
                var model = Load<GameObject>(path);
                if (model == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                inst.name = name;

                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = PickMaterial(name, r.gameObject.name);
                    if (name.StartsWith("plant_grass")) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                bool collide = !(name.StartsWith("plant_grass") || name.StartsWith("fx_") || name.StartsWith("path_stone") || name == "path_edge_01" || name.StartsWith("plant_flower") || name == "plant_ivy_wall");
                if (collide)
                {
                    var rs = inst.GetComponentsInChildren<Renderer>();
                    if (rs.Length > 0)
                    {
                        Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                        var bc = inst.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size;
                    }
                }
                if (name == "fx_butterfly") inst.AddComponent<ButterflyFlap>();
                if (name == "creature_sheep_01") { var w = inst.AddComponent<SimpleWander>(); w.speed = 0.35f; w.radius = 3f; }
                if (name == "prop_lantern_01" || name == "prop_lantern_hang" || name == "prop_campfire_01")
                    AddLight(inst, name == "prop_campfire_01" ? new Vector3(0, 0.5f, 0) : new Vector3(name == "prop_lantern_hang" ? 0.38f : 0, name == "prop_lantern_hang" ? -0.15f : 1.5f, 0),
                             name == "prop_campfire_01" ? Hex("F28C38") : Hex("FFC773"));

                string dir = PrefabDir + folder; Ensure(dir);
                PrefabUtility.SaveAsPrefabAsset(inst, dir + "/" + name + ".prefab");
                UnityEngine.Object.DestroyImmediate(inst);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Cozy Glade] Prefabs built: " + n);
        }

        static void AddLight(GameObject parent, Vector3 localPos, Color color)
        {
            var go = new GameObject("PointLight"); go.transform.SetParent(parent.transform, false); go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.range = 6f; l.intensity = 1.6f;
            l.shadows = LightShadows.None; l.enabled = false;   // turn on at night (DayNight script)
        }

        // ---------------- particle prefabs
        static ParticleSystem NewPs(string name, Transform parent, string mat)
        {
            var go = new GameObject(name); if (parent) go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = M(mat); r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }
        static Gradient Grad(Color a, Color b, float[] alphas)
        {
            var g = new Gradient();
            var ck = new[] { new GradientColorKey(a, 0), new GradientColorKey(b, 1) };
            var ak = new GradientAlphaKey[alphas.Length];
            for (int i = 0; i < alphas.Length; i++) ak[i] = new GradientAlphaKey(alphas[i], alphas.Length == 1 ? 0 : i / (float)(alphas.Length - 1));
            g.SetKeys(ck, ak); return g;
        }
        static void Save(GameObject go) { Ensure(PrefabDir + "FX"); PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "FX/" + go.name + ".prefab"); UnityEngine.Object.DestroyImmediate(go); }

        [MenuItem("Cozy Glade/3) Build FX Prefabs")]
        public static void BuildFx()
        {
            if (M("M_FX_Smoke") == null) BuildMaterials();

            // build puff (+ leaves)
            {
                var ps = NewPs("fx_build_puff", null, "M_FX_Smoke");
                var main = ps.main; main.duration = 0.8f; main.loop = false; main.startLifetime = 0.7f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f); main.startColor = new Color(0.85f, 0.75f, 0.6f, 0.75f); main.gravityModifier = -0.05f;
                main.stopAction = ParticleSystemStopAction.Destroy; main.maxParticles = 30;
                var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.35f;
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Grad(Color.white, Color.white, new[] { 0.8f, 0.4f, 0f });
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.5f, 1, 1.4f));
                var leaves = NewPs("Leaves", ps.transform, "M_FX_Leaf");
                var lm = leaves.main; lm.duration = 0.8f; lm.loop = false; lm.startLifetime = 1.1f; lm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
                lm.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.18f); lm.startColor = new ParticleSystem.MinMaxGradient(Hex("6FA85B"), Hex("C4E68B")); lm.gravityModifier = 0.6f;
                lm.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
                var le = leaves.emission; le.rateOverTime = 0; le.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });
                var ls = leaves.shape; ls.shapeType = ParticleSystemShapeType.Hemisphere; ls.radius = 0.2f;
                var lr = leaves.rotationOverLifetime; lr.enabled = true; lr.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
                ps.Play(); Save(ps.gameObject);
            }
            // chimney smoke
            {
                var ps = NewPs("fx_chimney_smoke", null, "M_FX_Smoke");
                var main = ps.main; main.loop = true; main.startLifetime = 3.8f; main.startSpeed = 0.55f; main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
                main.startColor = new Color(0.96f, 0.94f, 0.9f, 0.55f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 40;
                main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
                var em = ps.emission; em.rateOverTime = 4f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8f; sh.radius = 0.1f; sh.rotation = new Vector3(-90, 0, 0);
                var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
                v.x = new ParticleSystem.MinMaxCurve(0.3f); v.y = new ParticleSystem.MinMaxCurve(0f); v.z = new ParticleSystem.MinMaxCurve(0f);
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Grad(Color.white, Color.white, new[] { 0f, 0.8f, 0.5f, 0f });
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 2.4f));
                ps.Play(); Save(ps.gameObject);
            }
            // falling leaves
            {
                var ps = NewPs("fx_leaves_fall", null, "M_FX_Leaf");
                var main = ps.main; main.loop = true; main.startLifetime = 9f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
                main.startColor = new ParticleSystem.MinMaxGradient(Hex("8DBF65"), Hex("F5D561")); main.gravityModifier = 0.06f; main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f); main.maxParticles = 60;
                var em = ps.emission; em.rateOverTime = 2f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(20, 0.2f, 20); ps.transform.localPosition = new Vector3(0, 8, 0);
                var nz = ps.noise; nz.enabled = true; nz.strength = 0.6f; nz.frequency = 0.3f;
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
                ps.Play(); Save(ps.gameObject);
            }
            // dust motes in light
            {
                var ps = NewPs("fx_dust_motes", null, "M_FX_Dust");
                var main = ps.main; main.loop = true; main.startLifetime = 8f; main.startSpeed = 0.05f; main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
                main.startColor = new Color(1f, 0.95f, 0.8f, 0.6f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 80;
                var em = ps.emission; em.rateOverTime = 7f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(8, 4, 8);
                var nz = ps.noise; nz.enabled = true; nz.strength = 0.25f; nz.frequency = 0.25f;
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Grad(Color.white, Color.white, new[] { 0f, 1f, 1f, 0f });
                ps.Play(); Save(ps.gameObject);
            }
            // placement sparkle
            {
                var ps = NewPs("fx_sparkle_place", null, "M_FX_Sparkle");
                var main = ps.main; main.duration = 0.6f; main.loop = false; main.startLifetime = 0.7f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f); main.startColor = new Color(1f, 0.93f, 0.6f, 1f); main.stopAction = ParticleSystemStopAction.Destroy; main.maxParticles = 20;
                var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
                var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1f, 1, 0f));
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Grad(Color.white, Color.white, new[] { 1f, 1f, 0f });
                ps.Play(); Save(ps.gameObject);
            }
            // fireflies
            {
                var ps = NewPs("fx_fireflies", null, "M_FX_Glow");
                var main = ps.main; main.loop = true; main.startLifetime = 6f; main.startSpeed = 0.1f; main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
                main.startColor = new Color(1f, 0.95f, 0.55f, 1f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 40;
                var em = ps.emission; em.rateOverTime = 5f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(10, 2, 10); ps.transform.localPosition = new Vector3(0, 1.2f, 0);
                var nz = ps.noise; nz.enabled = true; nz.strength = 1f; nz.frequency = 0.4f;
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Grad(Color.white, Color.white, new[] { 0f, 1f, 0.3f, 1f, 0.2f, 1f, 0f });
                ps.Play(); Save(ps.gameObject);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Cozy Glade] FX prefabs built.");
        }

        // ---------------- post processing
        [MenuItem("Cozy Glade/4) Build Post-Processing Profile")]
        public static VolumeProfile BuildPostProcessProfile()
        {
            Ensure(P + "Settings");
            string path = P + "Settings/PP_Cozy.asset";
            var existing = Load<VolumeProfile>(path);
            if (existing != null) return existing;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);
            Action<VolumeComponent> sub = c => { c.name = c.GetType().Name; AssetDatabase.AddObjectToAsset(c, p); };

            var bloom = p.Add<Bloom>(true); bloom.threshold.Override(0.9f); bloom.intensity.Override(0.35f); bloom.scatter.Override(0.7f); bloom.tint.Override(new Color(1f, 0.93f, 0.8f)); sub(bloom);
            var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(0.1f); ca.contrast.Override(5f); ca.saturation.Override(10f); ca.colorFilter.Override(new Color(1f, 0.97f, 0.92f)); sub(ca);
            var vg = p.Add<Vignette>(true); vg.intensity.Override(0.2f); vg.smoothness.Override(0.5f); sub(vg);
            var tm = p.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral); sub(tm);
            EditorUtility.SetDirty(p); AssetDatabase.SaveAssets();
            return p;
        }

        // ---------------- showcase scene
        [MenuItem("Cozy Glade/5) Build Showcase Scene")]
        public static void BuildShowcase()
        {
            SetupAll();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Func<string, GameObject> pf = n =>
            {
                var g = AssetDatabase.FindAssets(n + " t:Prefab", new[] { PrefabDir });
                foreach (var id in g) { var path = AssetDatabase.GUIDToAssetPath(id); if (Path.GetFileNameWithoutExtension(path) == n) return Load<GameObject>(path); }
                Debug.LogWarning("Prefab not found: " + n); return null;
            };
            Func<string, Vector3, float, GameObject> put = (n, pos, rotY) =>
            {
                var src = pf(n); if (src == null) return null;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(src); go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, rotY, 0); return go;
            };

            // ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "Ground"; ground.transform.localScale = new Vector3(6, 1, 6);
            ground.GetComponent<Renderer>().sharedMaterial = M("M_Grass_Ground");

            // light + ambience
            var lightGo = GameObject.Find("Directional Light");
            if (lightGo)
            {
                var l = lightGo.GetComponent<Light>(); l.color = Hex("FFF1D6"); l.intensity = 1.25f; l.shadows = LightShadows.Soft; l.shadowStrength = 0.9f;
                lightGo.transform.rotation = Quaternion.Euler(48, -35, 0);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.8f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Hex("CFE8F2"); RenderSettings.fogStartDistance = 25; RenderSettings.fogEndDistance = 90;

            // house 4 x 3 m
            string[] front = { "wall_plaster_01", "wall_window_01", "wall_door_01", "wall_window_02" };
            for (int i = 0; i < 4; i++) put(front[i], new Vector3(i, 0, 0), 0);
            for (int i = 0; i < 4; i++) put(i == 1 ? "wall_window_01" : "wall_plaster_01", new Vector3(i + 1, 0, 3), 180);
            for (int i = 0; i < 3; i++) { put(i == 1 ? "wall_window_02" : "wall_plaster_01", new Vector3(0, 0, i), -90); put("wall_plaster_01", new Vector3(4, 0, i + 1), 90); }
            foreach (var c in new[] { new Vector2(0, 0), new Vector2(4, 0), new Vector2(0, 3), new Vector2(4, 3) }) put("wall_corner_01", new Vector3(c.x, 0, c.y), 0);
            for (int i = 0; i < 4; i++)
            {
                var a = put("roof_tile_red", new Vector3(i - 0.0f, 2.4f, -0.3f), 0); if (a) a.transform.localScale = new Vector3(1, 1, 1.7f);
                var b = put("roof_tile_red", new Vector3(i + 1f, 2.4f, 3.3f), 180); if (b) b.transform.localScale = new Vector3(1, 1, 1.7f);
                put("roof_ridge_cap", new Vector3(i, 3.68f, 1.5f), 0);
            }
            put("prop_chimney_01", new Vector3(3.3f, 2.7f, 1.9f), 0);
            put("prop_door_step", new Vector3(2.5f, 0, -0.3f), 0);
            put("prop_flowerbox", new Vector3(1.5f, 0.95f, -0.25f), 0);
            put("prop_window_shutter", new Vector3(1.5f, 1.4f, -0.2f), 0);
            put("prop_lantern_hang", new Vector3(3.15f, 1.9f, -0.2f), 0);

            // fence + path
            for (int i = -6; i <= 8; i++) { put("fence_post_01", new Vector3(i, 0, -6), 0); if (i < 8) put("fence_rail_01", new Vector3(i, 0, -6), 0); }
            put("fence_gate_01", new Vector3(2f, 0, -6), 0);
            for (int i = 0; i < 9; i++) put("path_stone_0" + (1 + i % 4), new Vector3(2.5f + Mathf.Sin(i) * 0.4f, 0.01f, -0.9f - i * 0.55f), i * 37);

            // garden
            var rng = new System.Random(5);
            Func<double, double, float> R = (a, b) => (float)(a + (b - a) * rng.NextDouble());
            string[] trees = { "tree_oak_01", "tree_oak_02", "tree_oak_03", "tree_pine_01", "tree_birch_01", "tree_fruit_01" };
            for (int i = 0; i < 12; i++) { float ang = R(0, 6.28); float r = R(11, 24); put(trees[i % trees.Length], new Vector3(2 + Mathf.Cos(ang) * r, 0, 1.5f + Mathf.Sin(ang) * r), R(0, 360)); }
            for (int i = 0; i < 26; i++) put("plant_flower_0" + (1 + i % 4), new Vector3(R(-5, 9), 0, R(-5, -1.2)), R(0, 360));
            for (int i = 0; i < 8; i++) put("plant_bush_0" + (1 + i % 2), new Vector3(R(-4, 8), 0, R(4, 8)), R(0, 360));
            for (int i = 0; i < 160; i++) put("plant_grass_tuft", new Vector3(R(-12, 14), 0, R(-10, 12)), R(0, 360));
            for (int i = 0; i < 6; i++) put("rock_small_0" + (1 + i % 3), new Vector3(R(-8, 12), 0, R(-9, 9)), R(0, 360));
            put("rock_big_01", new Vector3(-7, 0, 5), 20); put("rock_big_02", new Vector3(11, 0, 3), 80);
            put("plant_mushroom_01", new Vector3(-6.2f, 0, 3.8f), 0);
            put("water_pond_01", new Vector3(8, 0, 6), 0); put("plant_reed_01", new Vector3(6.4f, 0, 6.3f), 0);
            put("prop_bench_01", new Vector3(6, 0, 1.2f), 200); put("prop_barrel_01", new Vector3(-0.8f, 0, 0.3f), 0); put("prop_crate_01", new Vector3(-1.3f, 0, 0.6f), 20);
            put("prop_haybale_01", new Vector3(-3, 0, 4), 30); put("prop_cart_01", new Vector3(-3.5f, 0, 1.5f), 70); put("prop_well_01", new Vector3(-3, 0, 7), 0);
            put("prop_signpost_01", new Vector3(3.6f, 0, -4), 15); put("prop_flag_01", new Vector3(-1, 0, 3.5f), 0); put("prop_lantern_01", new Vector3(1.2f, 0, -2f), 0);
            put("prop_campfire_01", new Vector3(5.5f, 0, -3f), 0);
            put("creature_sheep_01", new Vector3(-6, 0, 0), 90); put("creature_sheep_01", new Vector3(-5, 0, -1.5f), 40);
            put("creature_cat_01", new Vector3(6.1f, 0.4f, 1.2f), 160);
            var bf = put("fx_butterfly", new Vector3(1, 1.2f, -3), 0); if (bf) { var w = bf.AddComponent<SimpleWander>(); w.flying = true; w.speed = 0.8f; w.radius = 3; }
            put("fx_chimney_smoke", new Vector3(3.3f, 4.25f, 1.9f), 0);
            put("fx_dust_motes", new Vector3(2, 2, -2), 0); put("fx_leaves_fall", Vector3.zero, 0);

            // camera + post processing
            var cam = Camera.main;
            if (cam)
            {
                cam.transform.position = new Vector3(9, 5.5f, -11); cam.transform.LookAt(new Vector3(1.5f, 1.2f, 1)); cam.fieldOfView = 38; cam.backgroundColor = Hex("BFE3F2");
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }
            var vol = new GameObject("PostFX").AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = BuildPostProcessProfile();

            Ensure(P + "Scenes");
            EditorSceneManager.SaveScene(scene, P + "Scenes/AssetShowcase.unity");
            Debug.Log("[Cozy Glade] Showcase scene saved: " + P + "Scenes/AssetShowcase.unity");
        }
    }
}
#endif
