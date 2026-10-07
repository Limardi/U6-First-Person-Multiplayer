using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LittleWitches.Visualization
{
    // Staged art scenes, deliberately independent of the multiplayer gameplay scene.
    public static class LittleWitchesVisualization
    {
        const string Root = "Assets/Visualization";
        const string Output = "output/unity-visualization";
        const int Width = 2048, Height = 1152;
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static readonly List<string> UsedAssets = new List<string>();
        static string[] prefabPaths;
        static Transform sceneRoot;
        static Camera camera;
        static int meshIndex;
        static bool batch;
        static Material wood, stone, green, teal, amber, cream, dark;

        [MenuItem("Little Witches/Build And Render Storyboard")]
        public static void BuildAndRender()
        {
            batch = Application.isBatchMode;
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Directory.CreateDirectory(Output);
                Directory.CreateDirectory(Root + "/Generated/Materials");
                Directory.CreateDirectory(Root + "/Generated/Meshes");
                Directory.CreateDirectory(Root + "/Generated/Icons");
                Directory.CreateDirectory(Root + "/Scenes");
                AssetDatabase.Refresh();
                prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/Imported" })
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
                ConvertImportedMaterials();
                wood = Mat("Timber", "654531"); stone = Mat("Warm Stone", "78847A");
                green = Mat("Moss", "60854A"); teal = Mat("Teal Magic", "39D4C0", 1.2f);
                amber = Mat("Amber Magic", "FFA938", 1.1f); cream = Mat("Linen", "E6DCC0");
                dark = Mat("Iron", "303C39");
                for (int shot = 0; shot < 3; shot++)
                {
                    Begin(shot);
                    if (shot == 0) Forest(); else if (shot == 1) Cottage(); else Marsh();
                    Viewmodel(shot);
                    Hud(shot);
                    AssetDatabase.SaveAssets();
                    string name = new[] { "01-Exploration", "02-Crafting-Brewing", "03-Frog-King" }[shot];
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), Root + "/Scenes/" + name + ".unity");
                    Render(name);
                    Debug.Log("VISUALIZATION_RENDERED " + Output + "/" + name + ".png");
                }
                File.WriteAllLines(Output + "/used-prefabs.txt", UsedAssets.Distinct().OrderBy(p => p));
                AssetDatabase.SaveAssets();
                Debug.Log("VISUALIZATION_COMPLETE: three 2048x1152 Unity renders.");
                if (batch) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (batch) EditorApplication.Exit(1);
                else throw;
            }
        }

        static void ConvertImportedMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Root + "/Imported" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                var leafTexture = SavedTexture(material, "_LeafTex");
                if (leafTexture)
                {
                    var trunkTexture = SavedTexture(material, "_TunkTex");
                    material.shader = Shader.Find("Little Witches/Vegetation Preview");
                    material.SetTexture("_LeafTex", leafTexture);
                    material.SetTexture("_TunkTex", trunkTexture ? trunkTexture : leafTexture);
                    EditorUtility.SetDirty(material);
                    continue;
                }
                if (!material || material.shader == shader) continue;
                Texture texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
                if (!texture && material.HasProperty("_MainTex")) texture = material.GetTexture("_MainTex");
                Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                bool cutout = material.name.IndexOf("leaf", StringComparison.OrdinalIgnoreCase) >= 0
                    || material.name.IndexOf("foliage", StringComparison.OrdinalIgnoreCase) >= 0;
                material.shader = shader;
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", 0.12f);
                material.SetFloat("_Cull", 0);
                if (cutout) { material.SetFloat("_AlphaClip", 1); material.EnableKeyword("_ALPHATEST_ON"); }
                EditorUtility.SetDirty(material);
            }
        }

        static Texture SavedTexture(Material material, string key)
        {
            var serialized = new SerializedObject(material);
            var textures = serialized.FindProperty("m_SavedProperties.m_TexEnvs");
            for (int i = 0; i < textures.arraySize; i++)
            {
                var pair = textures.GetArrayElementAtIndex(i);
                if (pair.FindPropertyRelative("first").stringValue == key)
                    return pair.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
            }
            return null;
        }

        static Material Mat(string name, string hex, float glow = 0)
        {
            if (Materials.TryGetValue(name, out Material cached)) return cached;
            string path = Root + "/Generated/Materials/" + name.Replace(' ', '-') + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", 0.16f);
            if (glow > 0) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * glow); }
            Materials[name] = material;
            return material;
        }

        static void Begin(int shot)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            sceneRoot = new GameObject("Little Witches - Staged Visualization").transform;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.57f, 0.68f, 0.71f);
            RenderSettings.ambientEquatorColor = new Color(0.47f, 0.52f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.28f, 0.2f);
            RenderSettings.fog = shot != 1;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 32; RenderSettings.fogEndDistance = 105;
            RenderSettings.fogColor = new Color(0.68f, 0.81f, 0.83f);
            var sun = new GameObject("Warm Afternoon Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1, 0.85f, 0.63f);
            sun.intensity = shot == 1 ? 1.1f : 2.2f;
            sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(42, -35, 0);
            RenderSettings.sun = sun;
            camera = new GameObject("First Person Storyboard Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.fieldOfView = 73; camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 180; camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.Skybox;
            string skyPath = Root + "/Generated/Materials/Island-Sky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (!sky) { sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(sky, skyPath); }
            sky.SetFloat("_SunSize", 0.03f); sky.SetFloat("_AtmosphereThickness", 0.7f);
            sky.SetColor("_SkyTint", new Color(0.55f, 0.64f, 0.71f)); sky.SetFloat("_Exposure", 0.9f);
            RenderSettings.skybox = sky;
            camera.backgroundColor = new Color(0.63f, 0.78f, 0.84f);
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camera.gameObject.AddComponent<VisualizationFlyCamera>();
            var volume = new GameObject("Art Direction").AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(); bloom.threshold.Override(1.15f); bloom.intensity.Override(0.16f);
            var tone = profile.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
            var color = profile.Add<ColorAdjustments>(); color.postExposure.Override(0.15f); color.saturation.Override(5);
            string path = Root + "/Generated/Shot-" + shot + "-Volume.asset";
            var previous = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (previous) { UnityEngine.Object.DestroyImmediate(profile); profile = previous; }
            else { AssetDatabase.CreateAsset(profile, path); foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile); }
            volume.sharedProfile = profile;
        }

        static void Aim(Vector3 position, Vector3 target)
        { camera.transform.position = position; camera.transform.LookAt(target); }

        static GameObject Prefab(string key, Vector3 position, float height, float yaw = 0, Transform parent = null)
        {
            bool interior = key.StartsWith("SM_Prop_") && (key.Contains("Table") || key.Contains("Shelf") || key.Contains("Book") || key.Contains("Fireplace"));
            string path = (interior ? prefabPaths.FirstOrDefault(p => p.Contains("SimpleFantasyInteriors") && Path.GetFileNameWithoutExtension(p) == key) : null)
                ?? prefabPaths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).Equals(key, StringComparison.OrdinalIgnoreCase))
                ?? prefabPaths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
            if (path == null) { Debug.LogWarning("Missing visualization prefab: " + key); return null; }
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var item = (GameObject)PrefabUtility.InstantiatePrefab(source);
            item.transform.SetParent(parent ? parent : sceneRoot, false);
            item.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Bounds bounds = BoundsOf(item);
            if (height > 0 && bounds.size.y > 0.001f)
            {
                float extent = bounds.size.y < Mathf.Max(bounds.size.x, bounds.size.z) * 0.05f
                    ? Mathf.Max(bounds.size.x, bounds.size.z) : bounds.size.y;
                item.transform.localScale *= height / extent;
            }
            bounds = BoundsOf(item);
            item.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (var animator in item.GetComponentsInChildren<Animator>()) animator.enabled = false;
            UsedAssets.Add(path);
            return item;
        }

        static Bounds BoundsOf(GameObject item)
        {
            var renderers = item.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(item.transform.position, Vector3.one);
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            var item = GameObject.CreatePrimitive(type); item.name = name;
            item.transform.SetParent(parent ? parent : sceneRoot, false);
            item.transform.localPosition = position; item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
            return item;
        }

        static GameObject MeshObject(string name, List<Vector3> vertices, List<int> triangles, Material material, Transform parent = null)
        {
            // Separate triangle vertices create the faceted surfaces used throughout the custom props.
            var flat = new List<Vector3>();
            foreach (int triangle in triangles) flat.Add(vertices[triangle]);
            var mesh = new Mesh { name = name };
            mesh.SetVertices(flat); mesh.SetTriangles(Enumerable.Range(0, flat.Count).ToArray(), 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path = Root + "/Generated/Meshes/" + (meshIndex++) + "-" + name.Replace(' ', '-') + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved) { UnityEngine.Object.DestroyImmediate(mesh); mesh = saved; } else AssetDatabase.CreateAsset(mesh, path);
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            item.transform.SetParent(parent ? parent : sceneRoot, false);
            item.GetComponent<MeshFilter>().sharedMesh = mesh; item.GetComponent<MeshRenderer>().sharedMaterial = material;
            return item;
        }

        static GameObject Facet(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            const int sides = 12, rings = 7;
            for (int y = 0; y <= rings; y++) for (int x = 0; x <= sides; x++)
            {
                float latitude = Mathf.PI * y / rings, angle = 2 * Mathf.PI * x / sides;
                vertices.Add(new Vector3(Mathf.Sin(latitude) * Mathf.Cos(angle), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(angle)) * 0.5f);
            }
            for (int y = 0; y < rings; y++) for (int x = 0; x < sides; x++)
            { int i = y * (sides + 1) + x; triangles.AddRange(new[] { i, i + 1, i + sides + 1, i + 1, i + sides + 2, i + sides + 1 }); }
            var item = MeshObject(name, vertices, triangles, material, parent);
            item.transform.localPosition = position; item.transform.localScale = scale; return item;
        }

        static GameObject Cone(string name, Vector3 position, float radius, float height, Material material, Transform parent = null)
        {
            var vertices = new List<Vector3> { new Vector3(0, height, 0), Vector3.zero };
            var triangles = new List<int>(); const int sides = 9;
            for (int i = 0; i < sides; i++) vertices.Add(new Vector3(Mathf.Cos(i * Mathf.PI * 2 / sides) * radius, 0, Mathf.Sin(i * Mathf.PI * 2 / sides) * radius));
            for (int i = 0; i < sides; i++) { int a = i + 2, b = (i + 1) % sides + 2; triangles.AddRange(new[] { 0, b, a, 1, a, b }); }
            var item = MeshObject(name, vertices, triangles, material, parent); item.transform.localPosition = position; return item;
        }

        static void Beam(string name, Vector3 a, Vector3 b, float radius, Material material, Transform parent = null)
        {
            var beam = Primitive(name, PrimitiveType.Cylinder, (a + b) / 2, new Vector3(radius * 2, (b - a).magnitude / 2, radius * 2), material, parent);
            beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        }

        static void Island(Vector3 center, Vector3 scale)
        {
            Facet("Floating Island Stone", center + Vector3.down * scale.y * 0.55f, scale, Mat("Cliff", "6D766E"));
            var top = Primitive("Mossy Island Ground", PrimitiveType.Cylinder, center + Vector3.down * 0.25f, new Vector3(scale.x, 0.35f, scale.z), green);
            top.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        static void Forest()
        {
            Aim(new Vector3(0, 1.7f, -8), new Vector3(0.6f, 1.5f, 14));
            Island(new Vector3(0, -0.3f, 9), new Vector3(43, 15, 52));
            var pathVertices = new List<Vector3>(); var pathTriangles = new List<int>();
            for (int i = 0; i < 22; i++)
            {
                float z = -9 + i * 2.1f, x = Mathf.Sin(i * 0.48f) * 0.8f, halfWidth = 1.7f + Mathf.Sin(i * 1.7f) * 0.25f;
                pathVertices.Add(new Vector3(x - halfWidth, -0.015f, z));
                pathVertices.Add(new Vector3(x + halfWidth, -0.015f, z));
                if (i > 0) { int v = i * 2; pathTriangles.AddRange(new[] { v - 2, v, v - 1, v - 1, v, v + 1 }); }
            }
            MeshObject("Winding Dirt Trail", pathVertices, pathTriangles, Mat("Trail", "B6A174"));
            UnityEngine.Random.InitState(312);
            for (int i = 0; i < 48; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * UnityEngine.Random.Range(4.8f, 19);
                float z = UnityEngine.Random.Range(-4, 33);
                if ((x < -4 && z > 5 && z < 15) || (x > 4 && z > 8 && z < 20)) continue;
                Prefab("SM_Env_Tree_Medium_01", new Vector3(x, -0.3f, z), UnityEngine.Random.Range(7, 12), UnityEngine.Random.Range(0, 360));
                if (i % 3 == 0) Prefab("SM_Env_Fern_01", new Vector3(x * 0.7f, -0.25f, z), 0.8f, i * 35);
            }
            for (int i = 0; i < 45; i++)
            {
                Vector3 p = new Vector3((i % 2 == 0 ? -1 : 1) * UnityEngine.Random.Range(2.3f, 10), -0.3f, UnityEngine.Random.Range(-5, 23));
                Prefab(i % 3 == 0 ? "SM_Env_Rock_Pebbles_01" : "SM_Env_Ground_Grass_01", p, i % 3 == 0 ? 0.7f : 0.45f, i * 32);
            }
            for (int i = 0; i < 5; i++) Prefab("SM_Env_Mushroom_01", new Vector3(-1.9f - i * 0.23f, -0.25f, -4.6f + i * 0.32f), 0.32f + i * 0.06f, i * 55);
            Prefab("SM_Env_Log_01", new Vector3(-3.7f, -0.3f, -2), 0.8f, 30);
            CottageExterior(new Vector3(-8, -0.3f, 10));
            Cave(new Vector3(7.5f, -0.3f, 13));
            Witch(new Vector3(-2.4f, -0.3f, 2), 165, false);
            Witch(new Vector3(4.8f, -0.3f, 7), 25, true);
            Island(new Vector3(-7, 1, 57), new Vector3(18, 13, 17));
            Tower(new Vector3(-7, 1.1f, 57), 1);
            Clouds();
        }

        static void CottageExterior(Vector3 p)
        {
            if (Prefab("SM_Bld_Preset_House_01_A_Optimized", p, 6.3f, 160)) return;
            var root = new GameObject("Grandpa's Cottage").transform; root.SetParent(sceneRoot); root.position = p;
            Primitive("Plaster", PrimitiveType.Cube, new Vector3(0, 1.8f, 0), new Vector3(5, 3.6f, 4), Mat("Cottage Plaster", "DCCCA6"), root);
            for (int side = -1; side <= 1; side += 2)
            {
                var roof = Primitive("Steep Roof", PrimitiveType.Cube, new Vector3(side * 1.35f, 4.05f, 0), new Vector3(3.1f, 0.25f, 5), Mat("Roof", "4C615B"), root);
                roof.transform.localRotation = Quaternion.Euler(0, 0, side * -37);
                Primitive("Corner Beam", PrimitiveType.Cube, new Vector3(side * 2.3f, 1.8f, -2.05f), new Vector3(0.23f, 3.7f, 0.23f), wood, root);
                Primitive("Warm Cottage Window", PrimitiveType.Cube, new Vector3(side * 1.4f, 1.9f, -2.04f), new Vector3(0.85f, 1.1f, 0.08f), amber, root);
            }
            Primitive("Door", PrimitiveType.Cube, new Vector3(0, 1.1f, -2.1f), new Vector3(1, 2.2f, 0.2f), wood, root);
            Primitive("Lintel", PrimitiveType.Cube, new Vector3(0, 3, -2.1f), new Vector3(5, 0.25f, 0.25f), wood, root);
            Primitive("Chimney", PrimitiveType.Cube, new Vector3(1.2f, 4.5f, 0.8f), new Vector3(0.6f, 2, 0.6f), stone, root);
        }

        static void Cave(Vector3 p)
        {
            Facet("Cave Darkness", p + new Vector3(0, 1.7f, 0.5f), new Vector3(4.5f, 4.1f, 0.4f), Mat("Cave Shadow", "172B2A"));
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.PI * i / 6;
                Facet("Cave Rock", p + new Vector3(Mathf.Cos(a) * 2.6f, Mathf.Sin(a) * 3.3f + 0.6f, 0), new Vector3(2.4f, 2.8f, 2.3f), stone);
            }
            for (int i = 0; i < 4; i++)
                Cone("Moon Ore", p + new Vector3(-0.8f + i * 0.6f, 0, -0.45f), 0.3f, 0.6f + i * 0.21f, teal);
            Point("Ore Glow", p + new Vector3(0, 1.2f, -0.7f), new Color(0.2f, 1, 0.85f), 25, 5);
        }

        static void Tower(Vector3 p, float scale)
        {
            if (Prefab("SM_Bld_Preset_Tower_01_Optimized", p, 8 * scale, 180)) return;
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("Ruined Tower Wall", PrimitiveType.Cube, p + new Vector3(side * 2.1f, 3, 0) * scale, new Vector3(1.3f, 6, 3.5f) * scale, stone);
                Primitive("Broken Battlement", PrimitiveType.Cube, p + new Vector3(side * 2.1f, 6.5f, 0.8f) * scale, new Vector3(1.3f, 1, 1) * scale, stone);
            }
            Primitive("Tower Lintel", PrimitiveType.Cube, p + new Vector3(0, 5.7f, 0) * scale, new Vector3(4.3f, 0.7f, 3.4f) * scale, stone);
        }

        static void Clouds()
        {
            var cloud = Mat("Cloud", "CFDFDB");
            for (int i = 0; i < 24; i++)
                Facet("Cloudbank", new Vector3(-50 + i * 4.5f, -5 + Mathf.Sin(i) * 2, 48 + (i % 4) * 10), new Vector3(14, 5, 10), cloud);
        }

        static void Cottage()
        {
            Aim(new Vector3(0, 1.72f, -4.8f), new Vector3(0, 1.3f, 2.8f));
            camera.backgroundColor = new Color(0.3f, 0.32f, 0.28f);
            for (int x = -5; x <= 5; x++) for (int z = -6; z <= 6; z++)
                Primitive("Stone Floor Tile", PrimitiveType.Cube, new Vector3(x, -0.12f, z), new Vector3(0.97f, 0.18f, 0.97f), stone);
            var plaster = Mat("Interior Plaster", "A59979");
            Primitive("Back Wall", PrimitiveType.Cube, new Vector3(0, 2.1f, 6), new Vector3(11, 4.2f, 0.3f), plaster);
            Primitive("Timber Ceiling", PrimitiveType.Cube, new Vector3(0, 4.4f, 0), new Vector3(11.5f, 0.3f, 12.5f), Mat("Ceiling Timber", "78664C"));
            Primitive("Left Wall", PrimitiveType.Cube, new Vector3(-5.5f, 2.1f, 0), new Vector3(0.3f, 4.2f, 12), plaster);
            Primitive("Right Lower Wall", PrimitiveType.Cube, new Vector3(5.5f, 0.6f, 0), new Vector3(0.3f, 1.2f, 12), plaster);
            Primitive("Right Upper Wall", PrimitiveType.Cube, new Vector3(5.5f, 3.8f, 0), new Vector3(0.3f, 0.8f, 12), plaster);
            for (int z = -5; z <= 6; z += 3)
            {
                Primitive("Exposed Roof Beam", PrimitiveType.Cube, new Vector3(0, 4.1f, z), new Vector3(11, 0.25f, 0.3f), wood);
                Primitive("Window Post", PrimitiveType.Cube, new Vector3(5.4f, 2.2f, z), new Vector3(0.25f, 4.2f, 0.25f), wood);
            }
            Primitive("Window Sill", PrimitiveType.Cube, new Vector3(5.4f, 1.2f, 0), new Vector3(0.5f, 0.2f, 12), wood);
            for (int i = 0; i < 5; i++) Prefab("SM_Env_Tree_Medium_01", new Vector3(10 + i * 3, -0.3f, -3 + i * 3), 8, i * 30);
            Prefab("SM_Prop_Fireplace_01", new Vector3(0, 0, 5.6f), 3.3f, 180);
            Prefab("SM_Prop_Bookshelf_01", new Vector3(-4, 0, 5.5f), 2.8f, 180);
            Prefab("SM_Prop_Bookshelf_03", new Vector3(3.4f, 0, 5.6f), 2.5f, 180);
            Prefab("SM_Prop_Table_03", new Vector3(-3.1f, 0, 1), 1.0f);
            Prefab("SM_Prop_Potion_Table_01", new Vector3(3.1f, 0, 1.8f), 1.05f, 0);
            Witch(new Vector3(-3.2f, 0, 2.4f), 160, false);
            Witch(new Vector3(3.2f, 0, 3), 205, true);
            for (int i = 0; i < 4; i++)
            {
                Bottle(new Vector3(2.5f + i * 0.4f, 1.07f, 1.5f), i % 2 == 0 ? Mat("Healing", "CF6456", 0.2f) : amber, 0.3f);
                Primitive("Gathered Timber", PrimitiveType.Cylinder, new Vector3(-3.8f + i * 0.3f, 1.15f, 0.7f), new Vector3(0.17f, 0.4f, 0.17f), wood).transform.rotation = Quaternion.Euler(90, 0, 0);
            }
            Prefab("SM_Prop_Book_Open_01", new Vector3(-1.5f, 0.92f, -1.2f), 0.15f, 155);
            Prefab("SM_Prop_Table_01", new Vector3(-2.3f, 0, -0.4f), 0.92f);
            Cauldron(new Vector3(0.2f, 0, -1.2f));
            Point("Cauldron Glow", new Vector3(0.2f, 1.4f, -1.2f), new Color(0.18f, 1, 0.8f), 4, 3);
            Point("Firelight", new Vector3(0, 1, 4.6f), new Color(1, 0.48f, 0.15f), 9, 6);
            Point("Window Fill", new Vector3(4, 2.5f, 0), new Color(1, 0.9f, 0.7f), 24, 8);
        }

        static void Cauldron(Vector3 p)
        {
            Facet("Iron Cauldron", p + Vector3.up * 0.65f, new Vector3(1.7f, 1.2f, 1.7f), dark);
            Primitive("Teal Brew", PrimitiveType.Cylinder, p + Vector3.up * 1.02f, new Vector3(1.45f, 0.025f, 1.45f), teal);
            Ring("Cauldron Lip", p + Vector3.up * 1.04f, 0.78f, 0.055f, dark);
            for (int i = 0; i < 8; i++) Facet("Brew Bubble", p + new Vector3(Mathf.Cos(i * 2) * 0.4f, 1.06f + (i % 3) * 0.035f, Mathf.Sin(i * 2) * 0.4f), Vector3.one * (0.07f + i * 0.008f), teal);
            for (int i = 0; i < 3; i++)
            { float a = i * Mathf.PI * 2 / 3; Beam("Cauldron Foot", p + new Vector3(Mathf.Cos(a) * 0.5f, 0.4f, Mathf.Sin(a) * 0.5f), p + new Vector3(Mathf.Cos(a) * 0.7f, 0.05f, Mathf.Sin(a) * 0.7f), 0.08f, dark); }
        }

        static void Marsh()
        {
            Aim(new Vector3(0, 1.7f, -7), new Vector3(0, 2.1f, 12));
            Island(new Vector3(0, -0.5f, 10), new Vector3(52, 10, 58));
            Primitive("Marsh Water", PrimitiveType.Cylinder, new Vector3(0, -0.21f, 10), new Vector3(48, 0.03f, 54), Mat("Marsh Water", "487D71", 0.04f));
            for (int x = -5; x <= 5; x++) for (int z = -3; z <= 9; z++)
            {
                if (Math.Abs(x) == 5 && z % 3 == 0) continue;
                var tile = Primitive("Ancient Arena Paving", PrimitiveType.Cube, new Vector3(x * 1.8f, -0.07f, z * 1.8f), new Vector3(1.72f, 0.25f, 1.72f), stone);
                tile.transform.rotation = Quaternion.Euler(0, ((x + z) % 3) * 1.2f, 0);
            }
            for (int i = 0; i < 10; i++)
            {
                float x = i % 2 == 0 ? -11 : 11, z = i * 3.2f - 2;
                Prefab("Ruins", new Vector3(x, 0, z), i % 3 == 0 ? 5 : 2.8f, i * 60);
                Prefab("SM_Env_Tree_Medium_01", new Vector3(x * 1.4f, -0.5f, z + 3), 9, i * 31);
                Prefab("Fern", new Vector3(x * 0.9f, -0.15f, z), 0.8f, i * 43);
            }
            Tower(new Vector3(0, 0, 25), 1.6f);
            Cone("Heartseed Pedestal Crystal", new Vector3(0, 1, 25), 0.48f, 1.6f, teal);
            Ring("Frog Leap Landing Warning", new Vector3(0, 0.09f, 9), 3.8f, 0.085f, amber);
            Frog(new Vector3(0, 2.1f, 9));
            Witch(new Vector3(-4.5f, 0, 4.5f), 25, false);
            Witch(new Vector3(4.6f, 0, 3.8f), -35, true);
            Beam("Readable Wand Spell", new Vector3(-4.3f, 1.1f, 5.1f), new Vector3(-1.4f, 3.4f, 8.4f), 0.025f, teal);
            for (int i = 0; i < 16; i++)
            {
                float x = i % 2 == 0 ? -11.5f : 11.5f, z = -4 + i * 1.8f;
                Primitive("Lily Pad", PrimitiveType.Cylinder, new Vector3(x, -0.13f, z), new Vector3(0.7f, 0.012f, 0.6f), green);
            }
            Clouds();
        }

        static void Frog(Vector3 position)
        {
            var root = new GameObject("Custom Frog King - Airborne Leap").transform;
            root.SetParent(sceneRoot); root.position = position;
            var frogGreen = Mat("Frog Skin", "779847"); var belly = Mat("Frog Belly", "C4CE84");
            var eye = Mat("Frog Eye", "D5D883"); var mouth = Mat("Frog Mouth", "3F3430");
            Facet("Body", new Vector3(0, 1.7f, 0), new Vector3(4.7f, 3.6f, 3.6f), frogGreen, root);
            Facet("Belly", new Vector3(0, 1.15f, -1.35f), new Vector3(3.1f, 2.2f, 0.9f), belly, root);
            Facet("Open Croak", new Vector3(0, 2.4f, -1.65f), new Vector3(2.2f, 1.1f, 0.35f), mouth, root);
            Facet("Tongue", new Vector3(0, 2.1f, -1.8f), new Vector3(1.3f, 0.25f, 0.25f), Mat("Frog Tongue", "C68179"), root);
            for (int side = -1; side <= 1; side += 2)
            {
                Facet("Eye Socket", new Vector3(side * 1.05f, 3.15f, -0.9f), new Vector3(1.3f, 1.4f, 1.2f), frogGreen, root);
                Facet("Golden Eye", new Vector3(side * 1.05f, 3.3f, -1.39f), new Vector3(0.88f, 0.88f, 0.55f), eye, root);
                Facet("Pupil", new Vector3(side * 1.05f, 3.32f, -1.63f), new Vector3(0.28f, 0.5f, 0.1f), dark, root);
                Facet("Powerful Hind Leg", new Vector3(side * 2.05f, 0.7f, 0.5f), new Vector3(2.1f, 1.6f, 2), frogGreen, root);
                Beam("Hind Shin", new Vector3(side * 2.2f, 0.5f, 0.6f), new Vector3(side * 2.6f, -0.6f, -0.3f), 0.3f, frogGreen, root);
                Facet("Webbed Hind Foot", new Vector3(side * 2.65f, -0.6f, -0.65f), new Vector3(1.4f, 0.27f, 1.1f), frogGreen, root);
                Beam("Forelimb", new Vector3(side * 1.7f, 2, -0.4f), new Vector3(side * 3.15f, 2.9f, -0.75f), 0.22f, frogGreen, root);
                Facet("Forefoot", new Vector3(side * 3.2f, 3, -0.8f), new Vector3(0.8f, 0.7f, 0.4f), frogGreen, root);
                for (int finger = 0; finger < 3; finger++) Beam("Webbed Finger", new Vector3(side * 3.25f, 3, -0.8f), new Vector3(side * (3.65f + finger * 0.12f), 2.85f + finger * 0.36f, -0.9f), 0.09f, frogGreen, root);
            }
            var gold = Mat("Crown Gold", "E5B14E");
            Ring("Crown Band", new Vector3(0, 3.65f, 0), 0.8f, 0.12f, gold, root);
            for (int i = 0; i < 6; i++)
            { float a = i * Mathf.PI / 3; Cone("Crown Point", new Vector3(Mathf.Cos(a) * 0.7f, 3.65f, Mathf.Sin(a) * 0.7f), 0.22f, 0.68f, gold, root); }
            Cone("Heartseed In Crown", new Vector3(0, 3.65f, -0.65f), 0.25f, 0.9f, teal, root);
        }

        static void Witch(Vector3 p, float yaw, bool tealHat)
        {
            var witch = Prefab("Character_Female_Witch_FixedScale", p, 1.9f, yaw);
            if (!witch) return;
            // Use pack geometry, with a separate oversized hat to establish cooperative silhouettes.
            var hat = tealHat ? Mat("Teammate Teal", "397C78") : Mat("Teammate Ochre", "C49A3B");
            Primitive("Team Hat Brim", PrimitiveType.Cylinder, p + Vector3.up * 1.83f, new Vector3(1.35f, 0.025f, 1.35f), hat);
            Cone("Team Pointed Hat", p + Vector3.up * 1.86f, 0.43f, 0.67f, hat);
            foreach (var transform in witch.GetComponentsInChildren<Transform>())
            {
                string name = transform.name.ToLowerInvariant();
                if (name == "shoulder_l" || name == "shoulder_r")
                {
                    Vector3 desired = witch.transform.TransformDirection(new Vector3(name.EndsWith("_l") ? -0.25f : 0.25f, -0.65f, 0.45f));
                    var elbow = transform.Cast<Transform>().FirstOrDefault(child => child.name.IndexOf("elbow", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (elbow) transform.rotation = Quaternion.FromToRotation(elbow.position - transform.position, desired) * transform.rotation;
                }
            }
            var hand = witch.GetComponentsInChildren<Transform>().FirstOrDefault(bone => bone.name == "Hand_R");
            Bottle(hand ? hand.position : p + Vector3.up, tealHat ? Mat("Healing", "CF6456", 0.2f) : amber, 0.22f);
        }

        static void Bottle(Vector3 position, Material liquid, float size, Transform parent = null)
        {
            Facet("Faceted Potion", position + Vector3.up * size * 0.5f, new Vector3(size * 0.75f, size, size * 0.75f), liquid, parent);
            Primitive("Potion Neck", PrimitiveType.Cylinder, position + Vector3.up * size * 1.02f, new Vector3(size * 0.3f, size * 0.13f, size * 0.3f), cream, parent);
            Primitive("Cork", PrimitiveType.Cylinder, position + Vector3.up * size * 1.23f, new Vector3(size * 0.23f, size * 0.08f, size * 0.23f), wood, parent);
        }

        static void Ring(string name, Vector3 p, float radius, float thickness, Material material, Transform parent = null)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            const int sides = 64;
            for (int i = 0; i <= sides; i++)
            { float a = i * Mathf.PI * 2 / sides; vertices.Add(new Vector3(Mathf.Cos(a) * (radius - thickness), 0, Mathf.Sin(a) * (radius - thickness))); vertices.Add(new Vector3(Mathf.Cos(a) * (radius + thickness), 0, Mathf.Sin(a) * (radius + thickness))); }
            for (int i = 0; i < sides; i++) { int v = i * 2; triangles.AddRange(new[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 }); }
            var item = MeshObject(name, vertices, triangles, material, parent); item.transform.localPosition = p;
        }

        static void Point(string name, Vector3 p, Color color, float intensity, float range)
        { var light = new GameObject(name).AddComponent<Light>(); light.type = LightType.Point; light.transform.position = p; light.color = color; light.intensity = intensity; light.range = range; }

        static void Viewmodel(int shot)
        {
            var root = new GameObject("First Person Hands And Equipment").transform;
            root.SetParent(camera.transform, false);
            root.localPosition = new Vector3(0, -0.24f, 0.5f);
            var sleeve = Mat("Player Moss Sleeve", "536F46"); var skin = Mat("Player Hands", "CBAB88");
            Beam("Right Sleeve", new Vector3(0.4f, -0.48f, 0.58f), new Vector3(0.28f, -0.29f, 0.79f), 0.09f, sleeve, root);
            Facet("Right Hand", new Vector3(0.26f, -0.25f, 0.83f), new Vector3(0.17f, 0.13f, 0.18f), skin, root);
            Beam("Left Sleeve", new Vector3(-0.45f, -0.48f, 0.6f), new Vector3(-0.31f, -0.32f, 0.81f), 0.09f, sleeve, root);
            Facet("Left Hand", new Vector3(-0.29f, -0.29f, 0.85f), new Vector3(0.16f, 0.12f, 0.18f), skin, root);
            if (shot == 0)
            {
                Beam("Gathering Tool Handle", new Vector3(0.24f, -0.3f, 0.85f), new Vector3(0.18f, 0.14f, 0.92f), 0.025f, wood, root);
                Facet("Stone Gathering Head", new Vector3(0.18f, 0.13f, 0.92f), new Vector3(0.24f, 0.18f, 0.12f), stone, root);
            }
            else if (shot == 1)
            {
                for (int i = 0; i < 5; i++)
                { var leaf = Facet("Held Herb Leaf", new Vector3(0.24f + Mathf.Sin(i * 2) * 0.04f, -0.17f + i * 0.035f, 0.88f), new Vector3(0.1f, 0.045f, 0.05f), green, root); leaf.transform.localRotation = Quaternion.Euler(0, 0, i * 50); }
            }
            else
            {
                Bottle(new Vector3(0.25f, -0.22f, 0.86f), amber, 0.18f, root);
                Beam("Offhand Branch Wand", new Vector3(-0.28f, -0.28f, 0.88f), new Vector3(-0.16f, 0.02f, 1.03f), 0.015f, wood, root);
                Cone("Wand Crystal", new Vector3(-0.16f, 0.02f, 1.03f), 0.025f, 0.08f, teal, root);
            }
        }

        static void Hud(int shot)
        {
            var go = new GameObject("Consistent Six Slot Gameplay HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 0.12f; canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            Rect("Crosshair Horizontal", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 3), new Color(0.98f, 0.95f, 0.82f));
            Rect("Crosshair Vertical", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3, 16), new Color(0.98f, 0.95f, 0.82f));
            int active = shot == 0 ? 0 : shot == 1 ? 2 : 5;
            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = new Vector2((i - 2.5f) * 77, 64);
                Rect("Slot Frame", canvas.transform, new Vector2(0.5f, 0), offset, new Vector2(72, 72), i == active ? new Color(0.92f, 0.83f, 0.57f) : new Color(0.2f, 0.21f, 0.17f, 0.93f));
                Rect("Slot", canvas.transform, new Vector2(0.5f, 0), offset, new Vector2(64, 64), new Color(0.15f, 0.17f, 0.14f, 0.95f));
                var icon = Rect("Equipment Icon", canvas.transform, new Vector2(0.5f, 0), offset, new Vector2(52, 52), Color.white);
                icon.sprite = Icon(i);
            }
            var hearts = new GameObject("Health", typeof(RectTransform), typeof(Text)); hearts.transform.SetParent(canvas.transform, false);
            var text = hearts.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "\u2665  \u2665  \u2665"; text.fontSize = 43; text.color = new Color(0.88f, 0.34f, 0.3f);
            var rt = hearts.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(0, 0); rt.anchoredPosition = new Vector2(40, 38); rt.sizeDelta = new Vector2(250, 60);
            if (shot != 2) return;
            Rect("Boss Track", canvas.transform, new Vector2(0.5f, 1), new Vector2(0, -74), new Vector2(640, 18), new Color(0.16f, 0.19f, 0.15f));
            Rect("Boss Remaining Health", canvas.transform, new Vector2(0.5f, 1), new Vector2(-107, -74), new Vector2(426, 12), new Color(0.79f, 0.32f, 0.23f));
            var label = new GameObject("Frog King", typeof(RectTransform), typeof(Text)); label.transform.SetParent(canvas.transform, false);
            var bossText = label.GetComponent<Text>(); bossText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); bossText.text = "Frog King"; bossText.fontSize = 30; bossText.alignment = TextAnchor.MiddleCenter; bossText.color = new Color(0.98f, 0.95f, 0.82f);
            rt = label.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1); rt.anchoredPosition = new Vector2(0, -40); rt.sizeDelta = new Vector2(400, 45);
        }

        static Image Rect(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image)); item.transform.SetParent(parent, false);
            var rt = item.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = offset; rt.sizeDelta = size;
            var image = item.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }

        static Sprite Icon(int type)
        {
            string path = Root + "/Generated/Icons/Hotbar-" + type + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                texture.SetPixels(new Color[64 * 64]);
                Action<int, int, int, Color> disc = (x, y, radius, color) =>
                { for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++) if (dx * dx + dy * dy <= radius * radius && x + dx >= 0 && x + dx < 64 && y + dy >= 0 && y + dy < 64) texture.SetPixel(x + dx, y + dy, color); };
                Action<int, int, int, int, int, Color> line = (x, y, ex, ey, radius, color) =>
                { for (int i = 0; i <= 60; i++) disc(Mathf.RoundToInt(Mathf.Lerp(x, ex, i / 60f)), Mathf.RoundToInt(Mathf.Lerp(y, ey, i / 60f)), radius, color); };
                var brown = new Color(0.56f, 0.36f, 0.2f); var mint = new Color(0.25f, 0.8f, 0.7f);
                if (type == 0 || type == 1)
                { line(16, 10, 43, 49, 4, brown); if (type == 0) { disc(40, 45, 11, new Color(0.64f, 0.67f, 0.58f)); line(31, 45, 53, 48, 7, new Color(0.69f, 0.7f, 0.63f)); } else disc(44, 51, 8, mint); }
                else if (type == 2)
                { line(25, 9, 37, 49, 2, mint); for (int i = 0; i < 4; i++) { disc(24 - i, 20 + i * 9, 7, new Color(0.4f, 0.7f, 0.3f)); disc(39 + i, 25 + i * 8, 7, new Color(0.6f, 0.8f, 0.38f)); } }
                else if (type == 3)
                { line(32, 12, 32, 32, 7, new Color(0.86f, 0.79f, 0.6f)); disc(32, 39, 19, new Color(0.82f, 0.4f, 0.25f)); disc(24, 42, 3, Color.white); disc(39, 47, 3, Color.white); }
                else
                { var liquid = type == 4 ? new Color(0.86f, 0.3f, 0.3f) : new Color(1, 0.66f, 0.21f); disc(32, 25, 18, new Color(0.83f, 0.9f, 0.8f)); disc(32, 24, 14, liquid); line(32, 38, 32, 48, 6, new Color(0.83f, 0.9f, 0.8f)); line(29, 52, 35, 52, 4, brown); }
                texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        static void Render(string name)
        {
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            RenderPipeline.SubmitRenderRequest(camera, request);
            var prior = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = prior; camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
