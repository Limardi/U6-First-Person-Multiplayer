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

namespace LittleWitches.Visualization
{
    public static class ForestIslandVisualization
    {
        const string Root = "Assets/Visualization";
        const string Generated = Root + "/Generated/ForestIsland";
        const string ScenePath = Root + "/Scenes/04-Forest-Island.unity";
        const string Output = "output/unity-island";
        const string Forest = Root + "/Imported/PolygonNatureBiomes/PNB_Enchanted_Forest/Prefabs/";
        const string Kingdom = Root + "/Imported/PolygonFantasyKingdom/Prefabs/";
        const string Interiors = Root + "/Imported/SimpleFantasyInteriors/Prefabs/Props/";
        const int Width = 2048, HeightPixels = 1152, Seed = 61006;
        const int ShowcaseCameraCount = 7;
        static readonly Vector2 Home = new Vector2(-23, -24);
        static readonly Vector2 Guardian = new Vector2(-22, 24);
        static readonly Vector2 Cave = new Vector2(27, 24);
        static readonly Vector2 Meadow = new Vector2(16, -18);
        static readonly Vector2 Grove = new Vector2(-6, -3);
        static readonly HashSet<string> Used = new HashSet<string>();
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static readonly List<List<Vector2>> Trails = new List<List<Vector2>>();
        static readonly List<Vector2> TreePositions = new List<Vector2>();
        static Transform terrain, cottage, vegetation, landmarks, atmosphere, cameras;
        static Camera mainCamera;
        static Material surface, rockSurface, trailSurface, grassSurface;
        static readonly Vector2 CottageView = new Vector2(-11, -40);
        static readonly Vector2 GroveView = Grove + new Vector2(8, -8);
        static readonly Vector2 GuardianView = Guardian + new Vector2(5, -15);

        static class Random
        {
            static System.Random generator;
            public static void InitState(int seed) { generator = new System.Random(seed); }
            public static float value => (float)generator.NextDouble();
            public static int Range(int minimum, int maximum) { return generator.Next(minimum, maximum); }
            public static float Range(float minimum, float maximum) { return minimum + value * (maximum - minimum); }
            public static Vector2 insideUnitCircle
            {
                get
                {
                    float angle = value * Mathf.PI * 2, radius = Mathf.Sqrt(value);
                    return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
            }
        }

        [MenuItem("Little Witches/Build And Render Forest Island")]
        public static void BuildAndRender()
        {
            bool batch = Application.isBatchMode;
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Used.Clear(); Materials.Clear(); Trails.Clear(); TreePositions.Clear();
                Random.InitState(Seed);
                Directory.CreateDirectory(Generated + "/Meshes");
                Directory.CreateDirectory(Generated + "/Materials");
                Directory.CreateDirectory(Output);
                AssetDatabase.Refresh();
                RequireShader("Little Witches/Island Surface");
                RequireShader("Little Witches/Island Sky");
                RequireShader("Little Witches/Island Cliff");
                RequireShader("Little Witches/Vegetation Preview");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("Little Witches - Floating Forest Island").transform;
                terrain = Group("01 Landform And Trails", root);
                cottage = Group("02 Grandpa's Cottage", root);
                vegetation = Group("03 Woodland And Habitats", root);
                landmarks = Group("04 Cave And Guardian Clearing", root);
                atmosphere = Group("05 Lighting And Clouds", root);
                cameras = Group("06 Showcase Cameras", root);
                surface = Material("Meadow", "FFFFFF", "Little Witches/Island Surface");
                rockSurface = Material("Island Stone", "FFFFFF", "Little Witches/Island Surface");
                trailSurface = Material("Trail", "FFFFFF", "Little Witches/Island Surface");
                grassSurface = Material("Grass Blades", "FFFFFF", "Little Witches/Island Surface");
                CreateTrails();
                CreateLandform();
                CreateCottage();
                CreateLandmarks();
                CreateWoodland();
                CreateBiomePockets();
                CreateTrailMarkers();
                CreateAtmosphere();
                CreateCameras();
                ValidateScene();
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath))
                    throw new IOException("Could not save forest island scene.");
                // Render from the reloaded scene so serialization problems cannot hide in the preview.
                EditorSceneManager.OpenScene(ScenePath);
                var savedRoot = GameObject.Find("Little Witches - Floating Forest Island");
                ValidateContents(savedRoot);
                var savedCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .OrderBy(c => c.name).ToArray();
                if (savedCameras.Length != ShowcaseCameraCount) throw new InvalidOperationException("Saved island scene lost its cameras.");
                foreach (var camera in savedCameras) Render(camera);
                File.WriteAllLines(Output + "/used-prefabs.txt", Used.OrderBy(p => p));
                File.WriteAllText(Output + "/scene-report.txt", "Scene: " + ScenePath + "\nSeed: " + Seed +
                    "\nCameras: " + savedCameras.Length + "\nDistinct source prefabs: " + Used.Count +
                    "\nPlacement fingerprint: " + PlacementFingerprint(savedRoot) +
                    "\nRender size: " + Width + " x " + HeightPixels +
                    "\nBiomes: sunny wildflower meadow, shaded mushroom grove, ancient guardian grove, rocky cave ridge" +
                    "\nSkybox: layered cloud sky, matching sun disc, warm horizon\nReload validation: passed\n");
                Debug.Log("FOREST_ISLAND_COMPLETE: scene reloaded and seven renders saved in " + Output);
                if (batch) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (batch) EditorApplication.Exit(1);
                else throw;
            }
        }

        static string PlacementFingerprint(GameObject root)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var records = new List<string>();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(item.gameObject)) continue;
                Vector3 p = item.position, r = item.eulerAngles, s = item.localScale;
                records.Add(item.name + "|" + string.Join(",", new[] { p.x, p.y, p.z, r.x, r.y, r.z, s.x, s.y, s.z }
                    .Select(v => v.ToString("R", culture))));
            }
            records.Sort(StringComparer.Ordinal);
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", records)))).Replace("-", "");
        }

        static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        static Shader RequireShader(string name)
        {
            var shader = Shader.Find(name);
            if (!shader || UnityEditor.ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Missing or invalid island shader: " + name);
            return shader;
        }

        static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color color); return color; }

        static Material Material(string name, string hex, string shader = "Universal Render Pipeline/Lit", float glow = 0)
        {
            if (Materials.TryGetValue(name, out var material)) return material;
            string path = Generated + "/Materials/" + name.Replace(' ', '-') + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(RequireShader(shader)); AssetDatabase.CreateAsset(material, path); }
            material.shader = RequireShader(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Hex(hex));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
            if (glow > 0) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", Hex(hex) * glow); }
            EditorUtility.SetDirty(material); Materials[name] = material;
            return material;
        }

        static float Boundary(float angle)
        { return 1 + 0.055f * Mathf.Sin(angle * 3 + 0.4f) + 0.035f * Mathf.Cos(angle * 5) + 0.022f * Mathf.Sin(angle * 9); }

        static Vector2 Outline(float angle, float radius = 1)
        { return new Vector2(Mathf.Cos(angle) * 49, Mathf.Sin(angle) * 59) * Boundary(angle) * radius; }

        static float Radius(Vector2 p)
        {
            float a = Mathf.Atan2(p.y / 59, p.x / 49);
            return new Vector2(p.x / 49, p.y / 59).magnitude / Boundary(a);
        }

        static float Hill(Vector2 p, Vector2 center, float spread)
        { return Mathf.Exp(-(p - center).sqrMagnitude / (spread * spread)); }

        static float GroundHeight(Vector2 p)
        {
            float h = 1.8f + 1.6f * Mathf.PerlinNoise(p.x * 0.043f + 37, p.y * 0.043f + 19);
            h += 9 * Hill(p, new Vector2(33, 35), 15) + 3.6f * Hill(p, new Vector2(-31, 35), 17);
            h += 0.6f * Mathf.Sin(p.y * 0.095f);
            h = Mathf.Lerp(h, 5.3f, 1 - Mathf.SmoothStep(0, 1, (Vector2.Distance(p, Home) - 9) / 10));
            h = Mathf.Lerp(h, 4.8f, 1 - Mathf.SmoothStep(0, 1, (Vector2.Distance(p, Guardian) - 8) / 6));
            // A level cave apron keeps the doorway and nearby trail connected to the ridge.
            h = Mathf.Lerp(h, 6.5f, 1 - Mathf.SmoothStep(0, 1, (Vector2.Distance(p, Cave) - 6) / 7));
            return h;
        }

        static Vector3 Ground(Vector2 p, float lift = 0) { return new Vector3(p.x, GroundHeight(p) + lift, p.y); }

        static GameObject MeshObject(string name, List<Vector3> vertices, List<int> triangles, List<Color> colors,
            Material material, Transform parent, bool faceted = false)
        {
            if (faceted)
            {
                var flatV = new List<Vector3>(); var flatC = new List<Color>();
                foreach (int i in triangles) { flatV.Add(vertices[i]); flatC.Add(colors[i]); }
                vertices = flatV; colors = flatC; triangles = Enumerable.Range(0, vertices.Count).ToList();
            }
            string path = Generated + "/Meshes/" + name.Replace(' ', '-') + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear(); mesh.name = name;
            mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            // Vertex colors bypass Unity's material color conversion in a linear-color project.
            mesh.SetColors(colors.Select(color => color.linear).ToList());
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        static void CreateLandform()
        {
            const int segments = 96, rings = 28;
            var v = new List<Vector3> { Ground(Vector2.zero) };
            var c = new List<Color> { MeadowColor(Vector2.zero) }; var t = new List<int>();
            for (int r = 1; r <= rings; r++) for (int i = 0; i < segments; i++)
            {
                Vector2 p = Outline(i * Mathf.PI * 2 / segments, r / (float)rings);
                v.Add(Ground(p)); c.Add(MeadowColor(p));
            }
            for (int i = 0; i < segments; i++) t.AddRange(new[] { 0, 1 + (i + 1) % segments, 1 + i });
            for (int r = 1; r < rings; r++) for (int i = 0; i < segments; i++)
            {
                int a = 1 + (r - 1) * segments + i, b = 1 + (r - 1) * segments + (i + 1) % segments;
                int d = a + segments, e = b + segments;
                t.AddRange(new[] { a, b, d, b, e, d });
            }
            MeshObject("Rolling Meadow", v, t, c, surface, terrain);
            v = new List<Vector3>(); c = new List<Color>(); t = new List<int>();
            float[] radii = { 1, 1.02f, 0.86f, 0.59f, 0.26f, 0.025f };
            float[] depths = { 0, -4, -13, -24, -34, -40 };
            for (int r = 0; r < radii.Length; r++) for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                Vector2 edge = Outline(a), p = Outline(a, radii[r]);
                if (r > 0) p *= 1 + 0.075f * Mathf.Sin(a * 7 + r * 0.6f) + 0.035f * Mathf.Cos(a * 13 - r);
                float y = r == 0 ? GroundHeight(edge) : depths[r] + 2.3f * Mathf.Sin(a * 5 + r);
                v.Add(new Vector3(p.x + r * 0.9f, y, p.y - r * 1.1f));
                Color shade = Color.Lerp(Hex("626978"), Hex("A1A59C"), Mathf.PerlinNoise(i * 0.17f, r * 0.6f));
                if (r == 0) shade = Color.Lerp(shade, MeadowColor(edge), 0.55f);
                c.Add(shade);
            }
            for (int r = 0; r < radii.Length - 1; r++) for (int i = 0; i < segments; i++)
            {
                int a = r * segments + i, b = r * segments + (i + 1) % segments;
                t.AddRange(new[] { a, b, a + segments, b, b + segments, a + segments });
            }
            int bottom = v.Count; v.Add(new Vector3(4.5f, -42, -5.5f)); c.Add(Hex("59675F"));
            for (int i = 0; i < segments; i++)
                t.AddRange(new[] { (radii.Length - 1) * segments + i, (radii.Length - 1) * segments + (i + 1) % segments, bottom });
            MeshObject("Tapered Floating Underside", v, t, c, rockSurface, terrain, true);
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2 / 24 + 0.045f * Mathf.Sin(i * 2.7f);
                Vector2 p = Outline(a, 0.995f);
                var cliff = Place(Forest + "SM_Env_Dirt_Cliff_" + (i % 4 + 1).ToString("00") + ".prefab", p, 4.5f + 0.5f * Mathf.Sin(i * 2.3f), -a * Mathf.Rad2Deg, terrain, -4.2f);
                cliff.name = "Forest Cliff Edge " + i.ToString("00");
                if (i % 8 == 0) Place(Forest + "SM_Env_Roots_Large_01.prefab", p * 0.99f, 1.5f, i * 31, terrain, -0.8f);
            }
            foreach (var trail in Trails) TrailMesh(trail, "Trail " + Trails.IndexOf(trail));
            Disc("Cottage Earth Terrace", Home, 8.7f, Hex("998D5C"), terrain);
            Disc("Guardian Moss Clearing", Guardian, 9.2f, Hex("788653"), landmarks);
        }

        static Color MeadowColor(Vector2 p)
        {
            float n = Mathf.PerlinNoise(p.x * 0.09f + 27, p.y * 0.09f + 51);
            Color color = Color.Lerp(Hex("526F40"), Hex("8D9D58"), n);
            color = Color.Lerp(color, Hex("A5AD69"), Hill(p, Meadow, 16) * 0.85f);
            color = Color.Lerp(color, Hex("3B6257"), Hill(p, Grove, 14) * 0.82f);
            color = Color.Lerp(color, Hex("637C61"), Hill(p, Guardian, 15) * 0.72f);
            return Color.Lerp(color, Hex("7B8861"), Hill(p, new Vector2(33, 35), 12) * 0.6f);
        }

        static void CreateTrails()
        {
            Trails.Add(Spline(new[] { Home + new Vector2(2, -8), new Vector2(-19, -35), new Vector2(-8, -32),
                new Vector2(-2, -21), new Vector2(4, -9), new Vector2(-2, 3), new Vector2(1, 14), new Vector2(15, 18), Cave + new Vector2(0, -3) }));
            Trails.Add(Spline(new[] { new Vector2(-2, 3), new Vector2(-12, 9), new Vector2(-17, 16), Guardian }));
            Trails.Add(Spline(new[] { new Vector2(4, -9), new Vector2(17, -13), new Vector2(30, -19), new Vector2(38, -20) }));
        }

        static List<Vector2> Spline(Vector2[] points)
        {
            var output = new List<Vector2>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 a = points[Mathf.Max(0, i - 1)], b = points[i], c = points[i + 1], d = points[Mathf.Min(points.Length - 1, i + 2)];
                for (int j = 0; j < 24; j++)
                {
                    float u = j / 24f;
                    output.Add(0.5f * ((2 * b) + (-a + c) * u + (2 * a - 5 * b + 4 * c - d) * u * u + (-a + 3 * b - 3 * c + d) * u * u * u));
                }
            }
            output.Add(points.Last()); return output;
        }

        static float TrailDistance(Vector2 p)
        {
            float best = float.MaxValue;
            foreach (var trail in Trails) foreach (var point in trail) best = Mathf.Min(best, (p - point).sqrMagnitude);
            return Mathf.Sqrt(best);
        }

        static bool BlocksShowcaseView(Vector2 p)
        {
            Vector2[] starts = { CottageView, GroveView, GuardianView };
            Vector2[] ends = { Home, Grove, Guardian + new Vector2(-1, 4) };
            for (int i = 0; i < starts.Length; i++)
            {
                Vector2 line = ends[i] - starts[i];
                float t = Mathf.Clamp01(Vector2.Dot(p - starts[i], line) / line.sqrMagnitude);
                if (Vector2.Distance(p, starts[i] + line * t) < 3.2f) return true;
            }
            return false;
        }

        static void TrailMesh(List<Vector2> points, string name)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var c = new List<Color>();
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 direction = (points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)]).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                float half = 1.25f + Mathf.Sin(i * 0.11f) * 0.23f;
                for (int side = 0; side < 5; side++)
                {
                    float across = side / 4f * 2 - 1;
                    Vector2 p = points[i] + normal * half * across;
                    v.Add(Ground(p, 0.10f));
                    c.Add(Color.Lerp(Hex("B3A274"), MeadowColor(p), Mathf.Pow(Mathf.Abs(across), 3) * 0.65f));
                    if (i > 0 && side > 0)
                    { int k = v.Count - 1; t.AddRange(new[] { k - 6, k - 5, k - 1, k - 5, k, k - 1 }); }
                }
            }
            MeshObject(name, v, t, c, trailSurface, terrain);
        }

        static void Disc(string name, Vector2 center, float radius, Color color, Transform parent)
        {
            var v = new List<Vector3> { Ground(center, 0.08f) }; var c = new List<Color> { color }; var t = new List<int>();
            const int segments = 64;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius * (1 + 0.08f * Mathf.Sin(a * 5));
                v.Add(Ground(p, 0.08f)); c.Add(Color.Lerp(color, MeadowColor(p), 0.5f));
                t.AddRange(new[] { 0, (i + 1) % segments + 1, i + 1 });
            }
            MeshObject(name, v, t, c, trailSurface, parent);
        }

        static GameObject Place(string path, Vector2 p, float extent, float yaw, Transform parent, float lift = 0)
        { return PlaceAt(path, Ground(p, lift), extent, yaw, parent); }

        static GameObject PlaceAt(string path, Vector3 position, float extent, float yaw, Transform parent)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!source) throw new FileNotFoundException("Required forest island prefab is missing: " + path);
            var item = (GameObject)PrefabUtility.InstantiatePrefab(source);
            item.transform.SetParent(parent, false);
            item.transform.position = Vector3.zero; item.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Bounds bounds = BoundsOf(item);
            float reference = bounds.size.y < Mathf.Max(bounds.size.x, bounds.size.z) * 0.15f ? Mathf.Max(bounds.size.x, bounds.size.z) : bounds.size.y;
            item.transform.localScale *= extent / Mathf.Max(reference, 0.001f);
            bounds = BoundsOf(item);
            item.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (var animator in item.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var renderer in item.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = SceneMaterial(mats[i]);
                renderer.sharedMaterials = mats;
            }
            Used.Add(path); return item;
        }

        static Bounds BoundsOf(GameObject item)
        {
            var renderers = item.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Prefab has no visible geometry: " + item.name);
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static Material SceneMaterial(Material source)
        {
            if (!source) throw new InvalidOperationException("Imported prefab has a missing material.");
            string key = "Source-" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            if (Materials.TryGetValue(key, out var cached)) return cached;
            string path = Generated + "/Materials/" + key + ".mat";
            var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!copy) { copy = new Material(source); AssetDatabase.CreateAsset(copy, path); }
            else copy.CopyPropertiesFromMaterial(source);
            Texture sides = SavedTexture(source, "_Sides"), top = SavedTexture(source, "_Top");
            if (sides && top)
            {
                copy.shader = RequireShader("Little Witches/Island Cliff");
                copy.SetTexture("_Sides", sides); copy.SetTexture("_Top", top);
                copy.SetColor("_BaseColor", Hex("CDD7C1"));
            }
            else if (source.shader.name != "Little Witches/Vegetation Preview")
            {
                var texture = SavedTexture(source, "_BaseMap") ?? SavedTexture(source, "_MainTex");
                copy.shader = RequireShader("Universal Render Pipeline/Lit");
                copy.SetTexture("_BaseMap", texture); copy.SetFloat("_Smoothness", 0.12f);
                copy.DisableKeyword("_EMISSION"); copy.SetColor("_EmissionColor", Color.black);
            }
            else
            {
                copy.SetColor("_AmbientFloor", Hex("82967D"));
                copy.SetFloat("_Translucency", 0.2f);
            }
            EditorUtility.SetDirty(copy); Materials[key] = copy; return copy;
        }

        static Texture SavedTexture(Material material, string key)
        {
            var textures = new SerializedObject(material).FindProperty("m_SavedProperties.m_TexEnvs");
            for (int i = 0; i < textures.arraySize; i++)
            {
                var pair = textures.GetArrayElementAtIndex(i);
                if (pair.FindPropertyRelative("first").stringValue == key)
                    return pair.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
            }
            return null;
        }

        static void CreateCottage()
        {
            Place(Kingdom + "Buildings/Preset_Houses/SM_Bld_Preset_House_01_A_Optimized.prefab", Home, 9.3f, 165, cottage);
            for (int i = 0; i < 3; i++)
            {
                Place(Kingdom + "Props/SM_Prop_Fence_01.prefab", Home + new Vector2(-7 + i * 3, -7), 1.15f, 0, cottage);
                Place(Kingdom + "Props/SM_Prop_Fence_01.prefab", Home + new Vector2(7, -4 + i * 3), 1.15f, 90, cottage);
            }
            Place(Kingdom + "Props/SM_Prop_Barrel_Stack_01.prefab", Home + new Vector2(-4.5f, -1), 1.35f, 50, cottage);
            for (int i = 0; i < 4; i++) Place(Forest + "SM_Env_Log_02.prefab", Home + new Vector2(-5.5f, -3 + i * 0.5f), 0.55f, 80, cottage);
            var table = Place(Interiors + "SM_Prop_KitchenTable_01.prefab", Home + new Vector2(5, -2), 1, 85, cottage);
            Bounds bounds = BoundsOf(table);
            PlaceAt(Interiors + "SM_Prop_Book_Open_01.prefab", new Vector3(bounds.center.x, bounds.max.y + 0.01f, bounds.center.z), 0.43f, 15, cottage);
            for (int i = 0; i < 3; i++) PlaceAt(Interiors + "SM_Prop_Potion_0" + (i + 1) + ".prefab",
                new Vector3(bounds.center.x + 0.5f, bounds.max.y, bounds.center.z + (i - 1) * 0.28f), 0.23f, i * 40, cottage);
            for (int row = 0; row < 3; row++)
            {
                Vector2 center = Home + new Vector2(5, 3 + row * 1.6f);
                var bed = Primitive("Ingredient Garden Bed " + row, PrimitiveType.Cube, Ground(center, 0.06f), new Vector3(3.5f, 0.14f, 1.1f), Material("Garden Soil", "68523A"), cottage);
                bed.transform.rotation = Quaternion.Euler(0, -15, 0);
                for (int j = 0; j < 5; j++)
                {
                    Vector2 p = center + new Vector2((j - 2) * 0.58f, 0);
                    Place(Kingdom + "Environments/SM_Env_Flowers_0" + (row + 1) + ".prefab", p, 0.48f, j * 61, cottage, 0.12f);
                }
            }
            Lantern(Home + new Vector2(3, -5), cottage);
            Lantern(Home + new Vector2(-5, -6), cottage);
            for (int i = 0; i < 15; i++)
            {
                float a = i * 2.4f;
                Vector2 p = Home + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(9, 12);
                Place(Kingdom + "Environments/SM_Env_Bush_Flowers_0" + (i % 3 + 1) + ".prefab", p, Random.Range(0.6f, 1), i * 17, cottage);
            }
        }

        static void Lantern(Vector2 p, Transform parent)
        {
            Place(Forest + "Props/SM_Prop_Lantern_Stand_01.prefab", p, 1.8f, 0, parent);
            PointLight("Warm Trail Lantern", Ground(p, 1.4f), Hex("FFC883"), 2, 4, parent);
        }

        static void CreateLandmarks()
        {
            Place(Forest + "SM_Env_Tree_Giant_02.prefab", Guardian + new Vector2(-1, 4), 26, 35, landmarks);
            Place(Forest + "SM_Env_Roots_Cluster_01.prefab", Guardian + new Vector2(-1, 5), 1.3f, 10, landmarks);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.1f;
                Place(Forest + "Props/SM_Prop_Ruins_0" + (i % 4 + 1) + ".prefab",
                    Guardian + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 8, Random.Range(1.4f, 2.7f), i * 57, landmarks);
            }
            float y = GroundHeight(Cave);
            Primitive("Cave Darkness", PrimitiveType.Cube, new Vector3(Cave.x, y + 3, Cave.y + 4), new Vector3(6.5f, 6, 0.2f), Material("Cave Interior", "172B2D"), landmarks);
            Primitive("Cave Floor", PrimitiveType.Cube, new Vector3(Cave.x, y + 0.02f, Cave.y + 1.5f), new Vector3(4.5f, 0.12f, 6), Material("Cave Floor", "64715C"), landmarks);
            for (int side = -1; side <= 1; side += 2)
            {
                Place(Kingdom + "Environments/SM_Env_Rock_01.prefab", Cave + new Vector2(side * 6.1f, 2), 3.8f, side * 25, landmarks, -0.4f);
                Place(Kingdom + "Environments/SM_Env_Rock_02.prefab", Cave + new Vector2(side * 4.8f, 0), 2.5f, side * 45, landmarks, -0.3f);
                Place(Forest + "SM_Env_Roots_02.prefab", Cave + new Vector2(side * 5.4f, 0), 0.9f, side * 80, landmarks);
            }
            CaveArch(y);
            for (int i = -1; i <= 1; i++)
                PlaceAt(Kingdom + "Environments/SM_Env_Rock_0" + (i + 2) + ".prefab",
                    new Vector3(Cave.x + i * 2.4f, y + 4.7f - Mathf.Abs(i) * 0.7f, Cave.y + 0.8f),
                    1.45f, 45 + i * 63, landmarks);
            for (int i = 0; i < 9; i++)
            {
                Vector2 p = Cave + new Vector2(Random.Range(-8, 9), Random.Range(6, 15));
                if (Mathf.Abs(p.x - Cave.x) < 5.5f && p.y < Cave.y + 12) continue;
                Place(Kingdom + "Environments/SM_Env_Rock_0" + (i % 4 + 1) + ".prefab", p, Random.Range(3, 6), i * 37, landmarks, -0.4f);
            }
            var crystalMat = Material("Moon Crystal", "69C3B1", glow: 0.55f);
            for (int i = 0; i < 4; i++)
            {
                var crystal = Place(Forest + "Props/SM_Prop_Crystal_0" + (i + 1) + ".prefab", Cave + new Vector2(1.7f + i * 0.45f, -0.6f + i * 0.4f), 0.8f + i * 0.18f, i * 55, landmarks);
                foreach (var renderer in crystal.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => crystalMat).ToArray();
            }
            PointLight("Cave Teal Glow", new Vector3(Cave.x, y + 1.8f, Cave.y + 2.5f), Hex("70D8C5"), 5, 7, landmarks);
            Lantern(Cave + new Vector2(-4, -3), landmarks);
            for (int i = 0; i < 8; i++)
                Place(Forest + "SM_Env_Mushroom_Small_Group_01.prefab", Cave + new Vector2(-5 + i * 0.4f, -1.5f - i * 0.4f), 0.4f, i * 60, landmarks);
            Group("Future Cave Interior Route", landmarks).position = Ground(Cave + new Vector2(0, 6));
            Group("Future Marsh Route", landmarks).position = Ground(new Vector2(38, -20));
        }

        static void CaveArch(float groundY)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var c = new List<Color>();
            const int segments = 10;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.PI * i / segments;
                float rough = 1 + 0.055f * Mathf.Sin(i * 5.3f);
                for (int layer = 0; layer < 2; layer++) for (int edge = 0; edge < 2; edge++)
                {
                    float width = edge == 0 ? 2.6f * (1 + 0.045f * Mathf.Sin(i * 2.8f)) : 4.0f * rough;
                    float height = edge == 0 ? 4.5f * (1 + 0.045f * Mathf.Cos(i * 3.7f)) : 6.0f * rough;
                    v.Add(new Vector3(Cave.x + Mathf.Cos(a) * width, groundY + Mathf.Sin(a) * height, Cave.y + layer * 2));
                    c.Add(Color.Lerp(Hex("677268"), Hex("899283"), i / (float)segments));
                }
                if (i > 0)
                {
                    int k = i * 4;
                    t.AddRange(new[] { k - 4, k, k - 3, k - 3, k, k + 1 });
                    t.AddRange(new[] { k - 3, k + 1, k - 1, k - 1, k + 1, k + 3 });
                    t.AddRange(new[] { k - 2, k + 2, k - 4, k - 4, k + 2, k });
                    t.AddRange(new[] { k - 1, k + 3, k - 2, k - 2, k + 3, k + 2 });
                }
            }
            MeshObject("Faceted Cave Arch", v, t, c, rockSurface, landmarks, true);
        }

        static void CreateWoodland()
        {
            string[] trees = { "SM_Env_Tree_Small_01", "SM_Env_Tree_Medium_01", "SM_Env_Tree_Medium_02", "SM_Env_Tree_Large_01", "SM_Env_Tree_Large_02" };
            for (float z = -48; z <= 48; z += 6.4f) for (float x = -43; x <= 43; x += 6.4f)
            {
                Vector2 p = new Vector2(x + Random.Range(-2.1f, 2.1f), z + Random.Range(-2.1f, 2.1f));
                if (Radius(p) > 0.92f || Vector2.Distance(p, Home) < 14 || Vector2.Distance(p, Guardian) < 12 || Vector2.Distance(p, Cave) < 12 || TrailDistance(p) < 3.8f) continue;
                if (p.x > 24 && p.y > 20 || Random.value < 0.13f) continue;
                if (Vector2.Distance(p, Meadow) < 12) continue;
                if (BlocksShowcaseView(p)) continue;
                int variant = Random.Range(0, trees.Length);
                float height = variant == 0 ? Random.Range(5, 8) : variant > 2 ? Random.Range(12, 17) : Random.Range(9, 13);
                Place(Forest + trees[variant] + ".prefab", p, height, Random.Range(0, 360), vegetation, -0.08f);
                TreePositions.Add(p);
                for (int j = 0; j < 3; j++)
                {
                    Vector2 fern = p + Random.insideUnitCircle * 2.8f;
                    if (TrailDistance(fern) > 2) Place(Forest + "SM_Env_Fern_0" + (j % 3 + 1) + ".prefab", fern, Random.Range(0.4f, 0.9f), j * 95, vegetation);
                }
                if (TreePositions.Count % 4 == 0) MushroomHabitat(p + new Vector2(2, -1));
            }
            // Hand-placed framing trees leave the cottage and trail cameras a readable opening.
            Place(Forest + "SM_Env_Tree_Large_01.prefab", new Vector2(-42, -26), 13, 45, vegetation);
            Place(Forest + "SM_Env_Tree_Medium_02.prefab", new Vector2(-8, -28), 12, 130, vegetation);
            MushroomHabitat(new Vector2(-3.5f, -17));
            MushroomHabitat(new Vector2(7.7f, -2));
            for (int i = 0; i < 90; i++)
            {
                Vector2 p = new Vector2(Random.Range(-43, 43), Random.Range(-49, 49));
                if (Radius(p) > 0.94f || TrailDistance(p) < 2 || Vector2.Distance(p, Home) < 9 || Vector2.Distance(p, Cave) < 7) continue;
                Place(Forest + "SM_Env_Rock_Pebbles_0" + (i % 6 + 1) + ".prefab", p, Random.Range(0.18f, 0.65f), i * 53, vegetation, -0.03f);
                if (i % 3 == 0) Place(Forest + "SM_Env_Ground_Grass_Round_01.prefab", p + Vector2.right, 1.3f, i * 21, vegetation, -0.07f);
            }
            for (int i = 0; i < 28; i++)
            {
                Vector2 p = new Vector2(12 + Random.Range(-5, 5), -16 + Random.Range(-4, 4));
                if (TrailDistance(p) > 1.9f) Place(Kingdom + "Environments/SM_Env_Flowers_0" + (i % 3 + 1) + ".prefab", p, Random.Range(0.3f, 0.6f), i * 31, vegetation);
            }
            GrassDetails();
        }

        static void CreateBiomePockets()
        {
            var meadow = Group("Sunny Wildflower Meadow", vegetation);
            var grove = Group("Shaded Mushroom Grove", vegetation);
            var ancient = Group("Ancient Guardian Grove", vegetation);
            // Clumps with open gaps read as a habitat rather than evenly scattered props.
            Vector2[] flowerBeds = { new Vector2(-6, -5), new Vector2(0, -7), new Vector2(6, -4),
                new Vector2(8, 2), new Vector2(-7, 3), new Vector2(2, 4) };
            for (int bed = 0; bed < flowerBeds.Length; bed++)
            {
                Vector2 center = Meadow + flowerBeds[bed];
                for (int i = 0; i < 22; i++)
                {
                    Vector2 p = center + Random.insideUnitCircle * 2.6f;
                    if (TrailDistance(p) < 1.8f) continue;
                    Place(Kingdom + "Environments/SM_Env_Flowers_" + new[] { 2, 7, 4, 8, 3, 9 }[bed].ToString("00") + ".prefab",
                        p, Random.Range(0.35f, 0.7f), Random.Range(0, 360), meadow);
                }
                Place(Kingdom + "Environments/SM_Env_Bush_Flowers_" + (bed % 6 + 1).ToString("00") + ".prefab",
                    center + flowerBeds[bed].normalized * 2, 1.1f, bed * 43, meadow);
            }

            Vector2[] caps = { new Vector2(-4, -2), new Vector2(-1, 3), new Vector2(3, 1),
                new Vector2(-5, 4), new Vector2(2, -4) };
            for (int i = 0; i < caps.Length; i++)
            {
                Vector2 p = Grove + caps[i];
                Place(Forest + "SM_Env_Mushroom_" + (i + 1).ToString("00") + ".prefab",
                    p, 1.8f + (i % 3) * 0.65f, i * 67, grove);
                Place(Forest + "SM_Env_Moss_Lumps_" + (i % 4 + 1).ToString("00") + ".prefab",
                    p + new Vector2(-0.8f, 0.3f), 1.4f, i * 35, grove);
            }
            Place(Forest + "SM_Env_Log_03.prefab", Grove + new Vector2(-2, 1), 0.85f, 115, grove);
            Place(Forest + "SM_Env_Log_04.prefab", Grove + new Vector2(4, 3), 0.75f, 20, grove);
            for (int i = 0; i < 65; i++)
            {
                Vector2 p = Grove + Random.insideUnitCircle * 8;
                if (TrailDistance(p) < 1.9f) continue;
                string prefab = i % 3 == 0 ? "SM_Env_Fern_" + (i % 3 + 1).ToString("00") :
                    "SM_Env_Mushroom_Small_Group_" + (i % 6 + 1).ToString("00");
                Place(Forest + prefab + ".prefab", p, Random.Range(0.3f, 0.75f), i * 37, grove);
            }
            Lantern(Grove + new Vector2(5, -1), grove);

            var crystalMat = Material("Grove Moon Crystal", "7BDAC4", glow: 0.7f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2 / 8;
                Vector2 p = Guardian + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 7.5f;
                if (TrailDistance(p) < 2.3f) continue;
                Place(Forest + "SM_Env_Roots_Cluster_" + (i % 3 + 1).ToString("00") + ".prefab",
                    p, 0.5f, i * 45, ancient);
                var crystal = Place(Forest + "Props/SM_Prop_Crystal_" + (i % 8 + 1).ToString("00") + ".prefab",
                    p + new Vector2(0.7f, -0.6f), Random.Range(0.65f, 1.2f), i * 55, ancient);
                foreach (var renderer in crystal.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => crystalMat).ToArray();
                Place(Forest + "SM_Env_Fern_02.prefab", p + new Vector2(-0.7f, 0.3f), 0.85f, i * 53, ancient);
            }
            PointLight("Guardian Root Glow", Ground(Guardian + new Vector2(-1, 4), 2), Hex("70D8C5"), 3, 9, ancient);
        }

        static void MushroomHabitat(Vector2 p)
        {
            Place(Forest + "SM_Env_Log_01.prefab", p, 0.6f, Random.Range(0, 360), vegetation);
            for (int i = 0; i < 7; i++)
            {
                Vector2 spot = p + Random.insideUnitCircle * 1.9f;
                if (TrailDistance(spot) > 1.55f) Place(Forest + "SM_Env_Mushroom_Small_0" + (i % 3 + 1) + ".prefab", spot, Random.Range(0.18f, 0.4f), i * 39, vegetation);
            }
        }

        static void CreateTrailMarkers()
        {
            var markers = Group("Trail Junctions And Wayfinding", landmarks);
            var meadowSign = Place(Kingdom + "Props/SM_Prop_Direction_Sign_01.prefab", new Vector2(1, -6.8f), 2.2f, 25, markers);
            meadowSign.name = "Meadow And Cottage Junction";
            var guardianSign = Place(Kingdom + "Props/SM_Prop_Direction_Sign_02.prefab", new Vector2(-4.6f, 5), 2.2f, -30, markers);
            guardianSign.name = "Guardian And Cave Junction";
            // The source signs use shop names baked into an atlas; blank wood suits the forest routes.
            var wood = Material("Trail Sign Wood", "8B6F4E");
            foreach (var sign in new[] { meadowSign, guardianSign })
                foreach (var renderer in sign.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => wood).ToArray();
            foreach (var p in new[] { new Vector2(-3, -23), new Vector2(2.7f, 11), new Vector2(17, 15), new Vector2(-16, 12) })
                Lantern(p, markers);
        }

        static void GrassDetails()
        {
            var v = new List<Vector3>(); var t = new List<int>(); var c = new List<Color>();
            for (int i = 0; i < 6300; i++)
            {
                Vector2 p = new Vector2(Random.Range(-48, 48), Random.Range(-58, 58));
                if (Radius(p) > 0.97f || TrailDistance(p) < 1.6f || Vector2.Distance(p, Home) < 8.8f || Vector2.Distance(p, Guardian) < 8 || Vector2.Distance(p, Cave) < 5) continue;
                int blades = Random.Range(3, 6);
                for (int j = 0; j < blades; j++)
                {
                    Vector2 spot = p + Random.insideUnitCircle * 0.35f;
                    Vector3 basePoint = Ground(spot, 0.015f);
                    float a = Random.Range(0, Mathf.PI * 2), h = Random.Range(0.16f, 0.5f), w = Random.Range(0.035f, 0.075f);
                    Vector3 side = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * w;
                    Vector3 tip = basePoint + new Vector3(Mathf.Sin(a) * 0.1f, h, Mathf.Cos(a) * 0.1f);
                    int k = v.Count; v.Add(basePoint - side); v.Add(tip); v.Add(basePoint + side);
                    Color shade = Color.Lerp(Hex("526B35"), Hex("ACB96A"), Random.value);
                    shade = Color.Lerp(shade, Hex("487567"), Hill(spot, Grove, 14) * 0.6f);
                    c.Add(shade * 0.8f); c.Add(shade); c.Add(shade * 0.8f);
                    t.AddRange(new[] { k, k + 1, k + 2, k + 2, k + 1, k });
                }
            }
            MeshObject("Meadow Grass Tufts", v, t, c, grassSurface, vegetation, true);
        }

        static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void PointLight(string name, Vector3 position, Color color, float intensity, float range, Transform parent)
        {
            var light = new GameObject(name).AddComponent<Light>(); light.transform.SetParent(parent, false);
            light.transform.position = position; light.type = LightType.Point; light.color = color;
            light.intensity = intensity; light.range = range;
        }

        static void CreateAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("C8D7D8"); RenderSettings.ambientEquatorColor = Hex("AEB4A0");
            RenderSettings.ambientGroundColor = Hex("7A8775");
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("B8CFC9"); RenderSettings.fogStartDistance = 165; RenderSettings.fogEndDistance = 550;
            var sun = new GameObject("Late Afternoon Sun").AddComponent<Light>(); sun.transform.SetParent(atmosphere, false);
            sun.type = LightType.Directional; sun.color = Hex("FFE9CA"); sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft; sun.shadowBias = 0.035f; sun.shadowNormalBias = 0.2f;
            sun.transform.rotation = Quaternion.Euler(38, -38, 0); RenderSettings.sun = sun;
            var sky = Material("Cloud Sky", "FFFFFF", "Little Witches/Island Sky");
            sky.SetColor("_Top", Hex("1976BD")); sky.SetColor("_Horizon", Hex("FFE2B7")); sky.SetColor("_Bottom", Hex("91B8CB"));
            sky.SetColor("_CloudColor", Hex("FFF1D9")); sky.SetColor("_CloudShadow", Hex("89B9D9"));
            sky.SetFloat("_CloudCoverage", 0.45f);
            Vector3 sunDirection = -sun.transform.forward;
            sky.SetVector("_SunDirection", new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 0));
            sky.SetColor("_SunColor", Hex("FFE2AC") * 2.5f);
            EditorUtility.SetDirty(sky); RenderSettings.skybox = sky;
            var volume = new GameObject("Island Color And Bloom").AddComponent<Volume>(); volume.transform.SetParent(atmosphere, false);
            volume.isGlobal = true;
            string profilePath = Generated + "/Island-Volume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (!profile) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, profilePath); }
            foreach (var old in profile.components.ToArray()) UnityEngine.Object.DestroyImmediate(old, true);
            profile.components.Clear();
            var bloom = profile.Add<Bloom>(); bloom.threshold.Override(1.2f); bloom.intensity.Override(0.12f);
            var tone = profile.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
            var color = profile.Add<ColorAdjustments>(); color.postExposure.Override(0.1f); color.saturation.Override(7);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile); volume.sharedProfile = profile;
            var cloudMat = Material("Clouds", "DBE8DC");
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2 / 12;
                Vector3 center = new Vector3(Mathf.Cos(a) * Random.Range(88, 130), Random.Range(-48, -40), Mathf.Sin(a) * Random.Range(95, 140));
                var group = Group("Cloud Bank " + i.ToString("00"), atmosphere);
                for (int j = 0; j < 5; j++)
                    Primitive("Cloud Lobe", PrimitiveType.Sphere, center + new Vector3((j - 2) * 8, Random.Range(-2, 2), Random.Range(-5, 5)),
                        new Vector3(Random.Range(19, 29), Random.Range(12, 20), Random.Range(17, 25)), cloudMat, group);
            }
        }

        static void CreateCameras()
        {
            mainCamera = Camera("01-Island-Overview", new Vector3(104, 57, -120), new Vector3(0, -8, 0), 44, true);
            Camera("02-Cottage-Approach", Ground(CottageView, 2), Ground(Home, 3.5f), 60, false);
            Camera("03-Forest-Trail", Ground(new Vector2(-0.7f, -20), 1.75f), Ground(new Vector2(0, 9), 2.9f), 66, false);
            Camera("04-Cave-Entrance", Ground(new Vector2(24.8f, 14.4f), 2), Ground(Cave, 2.5f), 64, false);
            Camera("05-Mushroom-Grove", Ground(GroveView, 1.9f), Ground(Grove + new Vector2(-2, 1), 1.7f), 63, false);
            Camera("06-Guardian-Grove", Ground(GuardianView, 5), Ground(Guardian + new Vector2(-1, 4), 12.5f), 82, false);
            Camera("07-Meadow-And-Sky", Ground(Meadow + new Vector2(-10, 6), 1.7f), Ground(Meadow + new Vector2(12, -15), 7), 70, false);
            mainCamera.tag = "MainCamera"; mainCamera.gameObject.AddComponent<VisualizationFlyCamera>();
            mainCamera.gameObject.AddComponent<AudioListener>();
        }

        static Camera Camera(string name, Vector3 position, Vector3 target, float fov, bool enabled)
        {
            var camera = new GameObject(name).AddComponent<Camera>(); camera.transform.SetParent(cameras, false);
            camera.transform.position = position; camera.transform.LookAt(target);
            camera.fieldOfView = fov; camera.nearClipPlane = 0.15f; camera.farClipPlane = 650;
            camera.allowHDR = true; camera.clearFlags = CameraClearFlags.Skybox; camera.enabled = enabled;
            var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            return camera;
        }

        static void ValidateScene()
        {
            ValidateContents(terrain.root.gameObject);
            if (!RenderSettings.skybox || ShaderUtil.ShaderHasError(RenderSettings.skybox.shader))
                throw new InvalidOperationException("Island skybox is missing or invalid.");
            if (cameras.GetComponentsInChildren<Camera>().Length != ShowcaseCameraCount)
                throw new InvalidOperationException("Island requires seven showcase cameras.");
            Debug.Log("FOREST_ISLAND_VALIDATED: " + TreePositions.Count + " woodland trees, " + Used.Count + " distinct source prefabs.");
        }

        static void ValidateContents(GameObject root)
        {
            if (!root) throw new InvalidOperationException("Missing forest island scene root.");
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (!material || !material.shader || ShaderUtil.ShaderHasError(material.shader))
                        throw new InvalidOperationException("Invalid material on island object " + renderer.name);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                if (!filter.sharedMesh) throw new InvalidOperationException("Missing mesh on " + filter.name);
        }

        static void Render(Camera camera)
        {
            var target = new RenderTexture(Width, HeightPixels, 24, RenderTextureFormat.ARGB32);
            Texture2D image = null; var previous = RenderTexture.active;
            try
            {
                target.Create(); camera.targetTexture = target;
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                // Warm the pipeline and camera-dependent LOD state before capturing the image.
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                image = new Texture2D(Width, HeightPixels, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, HeightPixels), 0, 0); image.Apply();
                File.WriteAllBytes(Output + "/" + camera.name + ".png", image.EncodeToPNG());
                Debug.Log("FOREST_ISLAND_RENDERED: " + camera.name);
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null; target.Release();
                UnityEngine.Object.DestroyImmediate(target); if (image) UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
