using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class MossyForestBlockoutBuilder
{
    private const string ForestRootName = "Mossy Forest Blockout";
    private const string GeneratedFolder = "Assets/Generated/MossyForest";
    private const float ForestSize = 64f;
    private const float TrailWidth = 3.2f;
    private const int GroundResolution = 100;

    [MenuItem("Walking Simulator/Build Mossy Forest Blockout")]
    private static void BuildForest()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Open a scene before building the forest blockout.");
            return;
        }

        Material forestFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/ForestFloor.mat");
        Material muddyPath = AssetDatabase.LoadAssetAtPath<Material>("Assets/MuddyPath.mat");
        Material bark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bark.mat");
        Material texturedBark = CreateWeatheredBarkMaterial();
        Material needles = AssetDatabase.LoadAssetAtPath<Material>("Assets/Needles.mat");
        GameObject broadleafPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tree_01.prefab");

        if (forestFloor == null || muddyPath == null || bark == null || needles == null || broadleafPrefab == null)
        {
            Debug.LogError("The forest builder could not find one or more existing forest materials or Tree_01.prefab.");
            return;
        }

        EnsureGeneratedFolder();
        DeactivatePrototypeObjects();

        GameObject oldForest = GameObject.Find(ForestRootName);
        if (oldForest != null)
        {
            Undo.DestroyObjectImmediate(oldForest);
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build mossy forest blockout");
        GameObject forestRoot = new GameObject(ForestRootName);
        Undo.RegisterCreatedObjectUndo(forestRoot, "Build mossy forest blockout");

        Mesh groundMesh = SaveMesh("Ground.asset", CreateGroundMesh());
        Mesh trailMesh = SaveMesh("Trail.asset", CreateTrailMesh());
        Mesh firCanopyMesh = SaveMesh("LowPolyFirCanopy.asset", CreateFirCanopyMesh());
        Mesh firNeedleDetailMesh = SaveMesh("FirNeedleDetail.asset", CreateFirNeedleDetailMesh());
        Mesh grassDetailMesh = SaveMesh("GroundGrassDetail.asset", CreateGroundGrassDetailMesh());

        CreateMeshSurface(forestRoot.transform, "Rolling Mossy Ground", groundMesh, forestFloor, true);
        CreateMeshSurface(forestRoot.transform, "Winding Muddy Trail", trailMesh, muddyPath, false);
        CreateMeshSurface(forestRoot.transform, "Mossy Grass and Leaf Litter", grassDetailMesh, needles, false);
        CreateForestTrees(forestRoot.transform, firCanopyMesh, firNeedleDetailMesh, texturedBark, needles, broadleafPrefab);
        CreateFallenLogs(forestRoot.transform, bark);
        CreateFernPatches(forestRoot.transform, needles);
        SetForestAtmosphere();
        CreateLightRain(forestRoot.transform);

        Selection.activeGameObject = forestRoot;
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Built a walkable, hilly forest blockout with a winding trail and varied trees.");
    }

    private static Material CreateWeatheredBarkMaterial()
    {
        const string texturePath = GeneratedFolder + "/WeatheredBarkTexture.asset";
        const string materialPath = GeneratedFolder + "/WeatheredBark.mat";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            const int size = 128;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            texture.name = "Weathered Bark with Moss";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            System.Random random = new System.Random(221122);
            float[] crackX = new float[18];
            for (int i = 0; i < crackX.Length; i++) crackX[i] = (float)random.NextDouble();
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float grain = Mathf.PerlinNoise(u * 42f + 7f, v * 8f + 2f) * 0.24f
                            + Mathf.PerlinNoise(u * 13f, v * 23f) * 0.18f;
                float fissure = 0f;
                foreach (float crack in crackX)
                {
                    float wanderingCrack = crack + Mathf.Sin(v * 18f + crack * 50f) * 0.008f;
                    fissure = Mathf.Max(fissure, 1f - Mathf.Clamp01(Mathf.Abs(u - wanderingCrack) * 210f));
                }
                float mossNoise = Mathf.PerlinNoise(u * 17f + 16f, v * 13f + 4f);
                bool moss = mossNoise > 0.64f && (Mathf.PerlinNoise(u * 5f, v * 7f) > 0.42f);
                float shade = Mathf.Clamp(0.57f + grain - fissure * 0.34f, 0.22f, 0.91f);
                Color barkPixel = new Color(0.26f * shade, 0.16f * shade, 0.095f * shade, 1f);
                if (moss)
                {
                    float mossShade = Mathf.Clamp(0.65f + grain * 0.5f, 0.4f, 0.9f);
                    barkPixel = Color.Lerp(barkPixel, new Color(0.20f * mossShade, 0.28f * mossShade, 0.10f * mossShade, 1f), 0.85f);
                }
                texture.SetPixel(x, y, barkPixel);
            }
            texture.Apply();
            AssetDatabase.CreateAsset(texture, texturePath);
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = "Weathered Bark" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateLightRain(Transform forestRoot)
    {
        Transform weather = forestRoot.Find("Weather");
        if (weather == null)
        {
            weather = new GameObject("Weather").transform;
            weather.SetParent(forestRoot, false);
        }
        Transform previous = weather.Find("Light Rain");
        if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
        GameObject rain = new GameObject("Light Rain");
        rain.transform.SetParent(weather, false);
        ParticleSystem particles = rain.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.maxParticles = 400;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.008f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(0.18f, 0.28f);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(0.008f);
        main.startColor = new Color(0.55f, 0.60f, 0.62f, 0.25f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.5f;
        var emission = particles.emission;
        emission.rateOverTime = 55f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12f, 0.15f, 12f);
        rain.AddComponent<WeatherRainFollow>();
        var renderer = rain.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 1.2f;
        renderer.velocityScale = 0.025f;
        rain.transform.position = new Vector3(0f, 8f, 0f);
        renderer.sharedMaterial = CreateRainMaterial();
    }

    private static Material CreateRainMaterial()
    {
        const string materialPath = GeneratedFolder + "/RainGrey.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("Could not find a URP particle shader for the rain material.");
                return null;
            }
            material = new Material(shader) { name = "Soft Grey Rain" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        Color rainColor = new Color(0.55f, 0.60f, 0.62f, 0.3f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", rainColor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", rainColor);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_SrcBlendAlpha")) material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (material.HasProperty("_DstBlendAlpha")) material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureGeneratedFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }

        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            AssetDatabase.CreateFolder("Assets/Generated", "MossyForest");
        }
    }

    private static void DeactivatePrototypeObjects()
    {
        string[] namesToHide =
        {
            "Plane", "ForestPath", "ForestPath2", "ForestPathCurve ",
            "TetsRamp", "TestWall", "TestStep", "Tree_01", "Tree_01 (1)"
        };

        foreach (string objectName in namesToHide)
        {
            GameObject prototype = GameObject.Find(objectName);
            if (prototype != null && prototype.activeSelf)
            {
                Undo.RecordObject(prototype, "Hide prototype environment object");
                prototype.SetActive(false);
            }
        }
    }

    private static Mesh CreateGroundMesh()
    {
        int side = GroundResolution + 1;
        Vector3[] vertices = new Vector3[side * side];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[GroundResolution * GroundResolution * 6];
        float halfSize = ForestSize * 0.5f;
        int triangleIndex = 0;

        for (int zIndex = 0; zIndex <= GroundResolution; zIndex++)
        {
            float z = Mathf.Lerp(-halfSize, halfSize, zIndex / (float)GroundResolution);
            for (int xIndex = 0; xIndex <= GroundResolution; xIndex++)
            {
                float x = Mathf.Lerp(-halfSize, halfSize, xIndex / (float)GroundResolution);
                int vertexIndex = zIndex * side + xIndex;
                vertices[vertexIndex] = new Vector3(x, GroundHeight(x, z), z);
                uv[vertexIndex] = new Vector2(x / 8f, z / 8f);

                if (xIndex == GroundResolution || zIndex == GroundResolution)
                {
                    continue;
                }

                int lowerLeft = vertexIndex;
                int lowerRight = vertexIndex + 1;
                int upperLeft = vertexIndex + side;
                int upperRight = upperLeft + 1;

                triangles[triangleIndex++] = lowerLeft;
                triangles[triangleIndex++] = upperLeft;
                triangles[triangleIndex++] = lowerRight;
                triangles[triangleIndex++] = lowerRight;
                triangles[triangleIndex++] = upperLeft;
                triangles[triangleIndex++] = upperRight;
            }
        }

        Mesh mesh = new Mesh { name = "Rolling Mossy Ground" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateTrailMesh()
    {
        const int segments = 180;
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6];
        float halfSize = ForestSize * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float z = Mathf.Lerp(-halfSize, halfSize, i / (float)segments);
            float centerX = TrailCenterX(z);
            float leftX = centerX - TrailWidth * 0.5f;
            float rightX = centerX + TrailWidth * 0.5f;
            int vertex = i * 2;

            vertices[vertex] = new Vector3(leftX, GroundHeight(leftX, z) + 0.045f, z);
            vertices[vertex + 1] = new Vector3(rightX, GroundHeight(rightX, z) + 0.045f, z);
            uv[vertex] = new Vector2(0f, i / 12f);
            uv[vertex + 1] = new Vector2(1f, i / 12f);

            if (i == segments)
            {
                continue;
            }

            int triangle = i * 6;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 2;
            triangles[triangle + 2] = vertex + 1;
            triangles[triangle + 3] = vertex + 1;
            triangles[triangle + 4] = vertex + 2;
            triangles[triangle + 5] = vertex + 3;
        }

        Mesh mesh = new Mesh { name = "Winding Muddy Trail" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateFirCanopyMesh()
    {
        const int sides = 9;
        float[] baseHeights = { 1.8f, 3.0f, 4.2f, 5.3f, 6.3f };
        float[] heights = { 2.8f, 2.7f, 2.55f, 2.4f, 2.25f };
        float[] radii = { 1.9f, 1.55f, 1.26f, 0.97f, 0.67f };
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        for (int tier = 0; tier < baseHeights.Length; tier++)
        {
            int firstVertex = vertices.Count;
            float rotation = (tier % 2) * 0.16f;
            for (int side = 0; side < sides; side++)
            {
                float angle = 2f * Mathf.PI * side / sides + rotation;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radii[tier], baseHeights[tier], Mathf.Sin(angle) * radii[tier]));
                uv.Add(new Vector2(side / (float)sides, tier / (float)baseHeights.Length));
            }

            int apex = vertices.Count;
            vertices.Add(new Vector3(0f, baseHeights[tier] + heights[tier], 0f));
            uv.Add(new Vector2(0.5f, 1f));

            for (int side = 0; side < sides; side++)
            {
                int current = firstVertex + side;
                int next = firstVertex + (side + 1) % sides;
                triangles.Add(current);
                triangles.Add(apex);
                triangles.Add(next);
            }

            int baseCenter = vertices.Count;
            vertices.Add(new Vector3(0f, baseHeights[tier], 0f));
            uv.Add(new Vector2(0.5f, 0.5f));
            for (int side = 0; side < sides; side++)
            {
                triangles.Add(baseCenter);
                triangles.Add(firstVertex + (side + 1) % sides);
                triangles.Add(firstVertex + side);
            }
        }

        Mesh mesh = new Mesh { name = "Layered Low-Poly Fir Canopy" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Grass blades and needle sprigs are double sided: each uses the same three vertices twice with opposite winding.
    // RecalculateNormals() adds the two opposite face normals together, which cancels to zero and gives broken
    // lighting (blown out white in HDR + Bloom). Using straight up normals gives soft, stable foliage shading.
    private static void SetUpwardNormals(Mesh mesh, int vertexCount)
    {
        Vector3[] normals = new Vector3[vertexCount];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.up;
        }
        mesh.SetNormals(normals);
    }

    private static Mesh CreateFirNeedleDetailMesh()
    {
        float[] baseHeights = { 1.8f, 3.0f, 4.2f, 5.3f, 6.3f };
        float[] radii = { 1.9f, 1.55f, 1.26f, 0.97f, 0.67f };
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        const int sides = 9;
        for (int tier = 0; tier < baseHeights.Length; tier++)
        for (int side = 0; side < sides; side++)
        {
            float angle = 2f * Mathf.PI * (side + 0.5f) / sides + (tier % 2) * 0.16f;
            Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 center = outward * (radii[tier] * 0.72f) + Vector3.up * (baseHeights[tier] + 0.38f);
            for (int sprig = -1; sprig <= 1; sprig++)
            {
                Vector3 sideAxis = Quaternion.AngleAxis(sprig * 22f, Vector3.up) * outward;
                Vector3 root = center + sideAxis * (sprig * 0.09f);
                Vector3 tip = root + sideAxis * 0.38f + Vector3.down * 0.36f;
                Vector3 left = root - sideAxis * 0.035f + Vector3.up * 0.015f;
                Vector3 right = root + sideAxis * 0.035f + Vector3.up * 0.015f;
                int first = vertices.Count;
                vertices.Add(left);
                vertices.Add(tip);
                vertices.Add(right);
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
                triangles.Add(first + 2);
                triangles.Add(first + 1);
                triangles.Add(first);
            }
        }
        Mesh mesh = new Mesh { name = "Coastal Fir Needle Sprigs" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        SetUpwardNormals(mesh, vertices.Count);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateGroundGrassDetailMesh()
    {
        const int clumps = 420;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        System.Random random = new System.Random(221126);
        int placed = 0;
        int attempts = 0;
        while (placed < clumps && attempts < clumps * 8)
        {
            attempts++;
            float x = NextRange(random, -30f, 30f);
            float z = NextRange(random, -30f, 30f);
            if (Mathf.Abs(x - TrailCenterX(z)) < 2.6f || new Vector2(x, z).magnitude < 5.7f) continue;
            Vector3 basePoint = new Vector3(x, GroundHeight(x, z) + 0.035f, z);
            int blades = 4 + random.Next(0, 4);
            for (int blade = 0; blade < blades; blade++)
            {
                float angle = NextRange(random, 0f, Mathf.PI * 2f);
                float length = NextRange(random, 0.24f, 0.52f);
                float width = NextRange(random, 0.045f, 0.085f);
                Vector3 forward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 side = new Vector3(-forward.z, 0f, forward.x) * width;
                Vector3 origin = basePoint + forward * NextRange(random, 0f, 0.16f);
                Vector3 tip = origin + forward * length + Vector3.up * length;
                int first = vertices.Count;
                vertices.Add(origin - side);
                vertices.Add(tip);
                vertices.Add(origin + side);
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
                triangles.Add(first + 2);
                triangles.Add(first + 1);
                triangles.Add(first);
            }
            placed++;
        }
        Mesh mesh = new Mesh { name = "Mossy Ground Grass Blades" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        SetUpwardNormals(mesh, vertices.Count);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh SaveMesh(string fileName, Mesh generatedMesh)
    {
        string path = GeneratedFolder + "/" + fileName;
        Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (savedMesh == null)
        {
            AssetDatabase.CreateAsset(generatedMesh, path);
            return generatedMesh;
        }

        EditorUtility.CopySerialized(generatedMesh, savedMesh);
        savedMesh.name = generatedMesh.name;
        UnityEngine.Object.DestroyImmediate(generatedMesh);
        EditorUtility.SetDirty(savedMesh);
        return savedMesh;
    }

    private static void CreateMeshSurface(Transform parent, string objectName, Mesh mesh, Material material, bool addCollider)
    {
        GameObject surface = new GameObject(objectName);
        surface.transform.SetParent(parent, false);
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        surface.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (addCollider)
        {
            surface.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
    }

    private static void CreateForestTrees(Transform parent, Mesh canopyMesh, Mesh needleDetailMesh, Material bark, Material needles, GameObject broadleafPrefab)
    {
        Transform trees = new GameObject("Coastal Fir and Hemlock Grove").transform;
        trees.SetParent(parent, false);
        System.Random random = new System.Random(281122);
        List<Vector3> placed = new List<Vector3>();
        int targetTrees = 96;
        int attempts = 0;

        while (placed.Count < targetTrees && attempts < 2500)
        {
            attempts++;
            float x = NextRange(random, -30f, 30f);
            float z = NextRange(random, -30f, 30f);
            if (Mathf.Abs(x - TrailCenterX(z)) < 4.2f || new Vector2(x, z).magnitude < 5.5f)
            {
                continue;
            }

            bool tooClose = false;
            foreach (Vector3 existing in placed)
            {
                if ((new Vector2(x, z) - new Vector2(existing.x, existing.z)).sqrMagnitude < 13f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
            {
                continue;
            }

            float scale = NextRange(random, 0.82f, 1.28f);
            float groundY = GroundHeight(x, z);
            placed.Add(new Vector3(x, groundY, z));

            if (placed.Count % 7 == 0)
            {
                GameObject broadleaf = PrefabUtility.InstantiatePrefab(broadleafPrefab) as GameObject;
                if (broadleaf != null)
                {
                    broadleaf.name = "Mossy Broadleaf";
                    broadleaf.transform.SetParent(trees, true);
                    broadleaf.transform.position = new Vector3(x, groundY + 7.14158f * scale, z);
                    broadleaf.transform.rotation = Quaternion.Euler(0f, NextRange(random, 0f, 360f), 0f);
                    broadleaf.transform.localScale = Vector3.one * scale;
                }
            }
            else
            {
                CreateFir(trees, x, groundY, z, scale, NextRange(random, 0f, 360f), canopyMesh, needleDetailMesh, bark, needles);
            }
        }
    }

    private static void CreateFir(Transform parent, float x, float groundY, float z, float scale, float yaw, Mesh canopyMesh, Mesh needleDetailMesh, Material bark, Material needles)
    {
        GameObject tree = new GameObject("Coastal Fir");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = new Vector3(x, groundY, z);
        tree.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        tree.transform.localScale = Vector3.one * scale;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Bark Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 4f, 0f);
        trunk.transform.localScale = new Vector3(0.34f, 4f, 0.34f);
        trunk.GetComponent<MeshRenderer>().sharedMaterial = bark;
        UnityEngine.Object.DestroyImmediate(trunk.GetComponent<Collider>());

        GameObject canopy = new GameObject("Layered Needle Canopy");
        canopy.transform.SetParent(tree.transform, false);
        canopy.AddComponent<MeshFilter>().sharedMesh = canopyMesh;
        MeshRenderer canopyRenderer = canopy.AddComponent<MeshRenderer>();
        canopyRenderer.sharedMaterial = needles;
        MaterialPropertyBlock canopyTint = new MaterialPropertyBlock();
        float canopyVariation = Mathf.PerlinNoise(x * 0.37f + 12f, z * 0.37f + 21f);
        Color needlesTint = Color.Lerp(new Color(0.57f, 0.69f, 0.52f), new Color(0.83f, 0.91f, 0.70f), canopyVariation);
        canopyRenderer.GetPropertyBlock(canopyTint);
        canopyTint.SetColor("_BaseColor", needlesTint);
        canopyTint.SetColor("_Color", needlesTint);
        canopyRenderer.SetPropertyBlock(canopyTint);

        GameObject needleSprigs = new GameObject("Individual Needle Sprigs");
        needleSprigs.transform.SetParent(tree.transform, false);
        needleSprigs.AddComponent<MeshFilter>().sharedMesh = needleDetailMesh;
        MeshRenderer sprigRenderer = needleSprigs.AddComponent<MeshRenderer>();
        sprigRenderer.sharedMaterial = needles;
        MaterialPropertyBlock sprigTint = new MaterialPropertyBlock();
        Color sprigColor = Color.Lerp(new Color(0.42f, 0.58f, 0.38f), new Color(0.64f, 0.76f, 0.50f), canopyVariation);
        sprigTint.SetColor("_BaseColor", sprigColor);
        sprigTint.SetColor("_Color", sprigColor);
        sprigRenderer.SetPropertyBlock(sprigTint);

        CapsuleCollider trunkCollider = tree.AddComponent<CapsuleCollider>();
        trunkCollider.center = new Vector3(0f, 4f, 0f);
        trunkCollider.radius = 0.17f;
        trunkCollider.height = 8f;
    }

    private static void CreateFallenLogs(Transform parent, Material bark)
    {
        Transform logs = new GameObject("Mossy Fallen Logs").transform;
        logs.SetParent(parent, false);
        System.Random random = new System.Random(91022);
        int created = 0;
        int attempts = 0;

        while (created < 13 && attempts < 300)
        {
            attempts++;
            float x = NextRange(random, -28f, 28f);
            float z = NextRange(random, -28f, 28f);
            if (Mathf.Abs(x - TrailCenterX(z)) < 5.3f)
            {
                continue;
            }

            float length = NextRange(random, 2.4f, 5.5f);
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "Mossy Fallen Log";
            log.transform.SetParent(logs, false);
            log.transform.position = new Vector3(x, GroundHeight(x, z) + 0.19f, z);
            log.transform.rotation = Quaternion.Euler(90f, NextRange(random, 0f, 360f), NextRange(random, -8f, 8f));
            log.transform.localScale = new Vector3(0.34f, length * 0.5f, 0.34f);
            log.GetComponent<MeshRenderer>().sharedMaterial = bark;
            UnityEngine.Object.DestroyImmediate(log.GetComponent<Collider>());
            created++;
        }
    }

    private static void CreateFernPatches(Transform parent, Material needles)
    {
        Transform patches = new GameObject("Forest Fern Patches").transform;
        patches.SetParent(parent, false);
        System.Random random = new System.Random(6122);
        int created = 0;
        int attempts = 0;

        while (created < 28 && attempts < 800)
        {
            attempts++;
            float x = NextRange(random, -30f, 30f);
            float z = NextRange(random, -30f, 30f);
            if (Mathf.Abs(x - TrailCenterX(z)) < 2.5f)
            {
                continue;
            }

            Transform patch = new GameObject("Fern Cluster").transform;
            patch.SetParent(patches, false);
            patch.position = new Vector3(x, GroundHeight(x, z), z);

            for (int frond = 0; frond < 5; frond++)
            {
                float angle = frond * 72f + NextRange(random, -18f, 18f);
                float lean = NextRange(random, 26f, 48f);
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                leaf.name = "Fern Frond";
                leaf.transform.SetParent(patch, false);
                leaf.transform.localPosition = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 0.18f, 0.3f, Mathf.Cos(angle * Mathf.Deg2Rad) * 0.18f);
                leaf.transform.localRotation = Quaternion.Euler(lean, angle, 0f);
                leaf.transform.localScale = new Vector3(0.11f, NextRange(random, 0.36f, 0.54f), 0.09f);
                leaf.GetComponent<MeshRenderer>().sharedMaterial = needles;
                UnityEngine.Object.DestroyImmediate(leaf.GetComponent<Collider>());
            }

            created++;
        }
    }

    private static void SetForestAtmosphere()
    {
        RenderSettings.fog = false;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.38f, 0.43f, 0.41f);
        RenderSettings.fogDensity = 0.009f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.31f, 0.36f, 0.33f);
        RenderSettings.ambientEquatorColor = new Color(0.23f, 0.27f, 0.22f);
        RenderSettings.ambientGroundColor = new Color(0.12f, 0.15f, 0.12f);

        const string skyPath = GeneratedFolder + "/OvercastSky.mat";
        Material overcastSky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (overcastSky != null)
        {
            overcastSky.SetColor("_SkyTint", new Color(0.42f, 0.48f, 0.49f));
            overcastSky.SetColor("_GroundColor", new Color(0.23f, 0.28f, 0.27f));
            overcastSky.SetFloat("_AtmosphereThickness", 1.45f);
            overcastSky.SetFloat("_Exposure", 0.3f);
            overcastSky.SetFloat("_SunSize", 0.001f);
            RenderSettings.skybox = overcastSky;
            EditorUtility.SetDirty(overcastSky);
        }

        Camera forestCamera = Camera.main;
        if (forestCamera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject != null) forestCamera = cameraObject.GetComponent<Camera>();
        }
        if (forestCamera != null)
        {
            forestCamera.clearFlags = CameraClearFlags.SolidColor;
            forestCamera.backgroundColor = new Color(0.18f, 0.22f, 0.21f);
        }

        GameObject directionalLight = GameObject.Find("Directional Light");
        if (directionalLight != null)
        {
            Light light = directionalLight.GetComponent<Light>();
            if (light != null)
            {
                light.color = new Color(0.82f, 0.86f, 0.76f);
                light.intensity = 0.85f;
            }
        }
    }

    private static float GroundHeight(float x, float z)
    {
        float pathDistance = Mathf.Abs(x - TrailCenterX(z));
        float blendDistance = Mathf.Max(0f, pathDistance - TrailWidth * 0.5f);
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(blendDistance / 3.5f));
        float broadNoise = (Mathf.PerlinNoise((x + 83f) * 0.038f, (z + 57f) * 0.038f) - 0.5f) * 4.6f;
        float detailNoise = (Mathf.PerlinNoise((x - 21f) * 0.105f, (z + 132f) * 0.105f) - 0.5f) * 0.65f;
        float ridge = Mathf.Sin(z * 0.105f + x * 0.035f) * 0.72f + Mathf.Sin(x * 0.13f - z * 0.045f) * 0.38f;
        float naturalHeight = broadNoise + detailNoise + ridge;
        return Mathf.Lerp(TrailHeight(z), TrailHeight(z) + naturalHeight, blend);
    }

    private static float TrailCenterX(float z)
    {
        return Mathf.Sin(z * 0.083f) * 6.8f + Mathf.Sin(z * 0.205f) * 1.15f;
    }

    private static float TrailHeight(float z)
    {
        return Mathf.Sin(z * 0.11f) * 0.62f + Mathf.Sin(z * 0.23f) * 0.18f;
    }

    private static float NextRange(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
}
