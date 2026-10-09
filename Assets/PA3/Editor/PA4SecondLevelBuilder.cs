using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-time builder for the PA4 second forest level. It duplicates the PA3
/// gameplay scene, then edits only the duplicate and the build-scene list.
/// </summary>
[InitializeOnLoad]
internal static class PA4SecondLevelBuilder
{
    private const string SourceScenePath = "Assets/Scenes/PA3_VerticalSlice.unity";
    private const string TargetScenePath = "Assets/Scenes/SEGUNDO NIVEL.unity";
    private const string OneShotMarker = "Assets/PA3/Editor/BUILD_SECOND_LEVEL_ONCE.txt";
    private const string RockPrefabPath = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Generic_Rock_01.prefab";
    private const string PortalPrefabPath = "Assets/PA3/Prefabs/PA3_ExitPortal.prefab";
    private const string CrystalPrefabPath = "Assets/PA3/Prefabs/EnergyCrystal.prefab";

    static PA4SecondLevelBuilder()
    {
        EditorApplication.delayCall += RunRequestedBuild;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("PA4/Generar segundo nivel de bosque")]
    private static void BuildFromMenu()
    {
        BuildSecondLevel();
    }

    private static void RunRequestedBuild()
    {
        string markerPath = FullProjectPath(OneShotMarker);
        if (!File.Exists(markerPath)) return;
        if (BuildSecondLevel())
        {
            File.Delete(markerPath);
            AssetDatabase.Refresh();
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += RunRequestedBuild;
    }

    private static bool BuildSecondLevel()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("PA4: sal del modo Play antes de generar el segundo nivel.");
            return false;
        }

        string sourceFullPath = FullProjectPath(SourceScenePath);
        string targetFullPath = FullProjectPath(TargetScenePath);
        if (!File.Exists(sourceFullPath) || !File.Exists(targetFullPath))
        {
            Debug.LogError("PA4: no se encontró la escena base o la escena SEGUNDO NIVEL.");
            return false;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loaded = SceneManager.GetSceneAt(i);
            if (loaded.isDirty)
            {
                Debug.LogError("PA4: hay una escena con cambios sin guardar. Guarda o cierra esos cambios y vuelve a ejecutar PA4/Generar segundo nivel de bosque.");
                return false;
            }
        }

        string backupDirectory = FullProjectPath("Temp/PA4-LevelBackups");
        Directory.CreateDirectory(backupDirectory);
        string backupPath = Path.Combine(backupDirectory, "SEGUNDO NIVEL antes de generar.unity");
        File.Copy(targetFullPath, backupPath, true);

        Scene sourceScene = SceneManager.GetSceneByPath(SourceScenePath);
        bool openedSourceForBuild = !sourceScene.IsValid() || !sourceScene.isLoaded;
        if (openedSourceForBuild)
            sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

        Scene existingTarget = SceneManager.GetSceneByPath(TargetScenePath);
        if (existingTarget.IsValid() && existingTarget.isLoaded)
            EditorSceneManager.CloseScene(existingTarget, true);

        Scene level = default;
        try
        {
            // Copying the saved scene asset preserves every serialized reference
            // among its player, camera, HUD, terrain, crystals, and lighting.
            File.Copy(sourceFullPath, targetFullPath, true);
            AssetDatabase.ImportAsset(TargetScenePath, ImportAssetOptions.ForceSynchronousImport);
            level = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(level);

            Debug.Log("PA4: escena duplicada cargada; raíces: " + string.Join(", ", level.GetRootGameObjects().Select(go => go.name)));
            Terrain terrain = FindInScene<Terrain>(level).FirstOrDefault();
            PlayerController3D player = FindInScene<PlayerController3D>(level).FirstOrDefault();
            if (terrain == null || terrain.terrainData == null || player == null)
                throw new InvalidOperationException("La escena base debe contener Terrain y PlayerController3D.");

            const int platformCount = 8;
            Vector3[] route = new Vector3[platformCount];
            Vector3 origin = player.transform.position;
            float previousTop = float.NegativeInfinity;
            for (int i = 0; i < platformCount; i++)
            {
                float xOffset = i * 2.25f;
                float zOffset = i * 2.35f + ((i % 2 == 0) ? 0f : 1.0f);
                Vector3 point = origin + new Vector3(xOffset, 0f, zOffset);
                point.x = Mathf.Clamp(point.x, terrain.transform.position.x + 4f,
                    terrain.transform.position.x + terrain.terrainData.size.x - 4f);
                point.z = Mathf.Clamp(point.z, terrain.transform.position.z + 4f,
                    terrain.transform.position.z + terrain.terrainData.size.z - 4f);

                float ground = terrain.SampleHeight(point) + terrain.transform.position.y;
                float top = ground + 0.68f;
                if (i > 0) top = Mathf.Clamp(top, previousTop - 0.42f, previousTop + 0.42f);
                top = Mathf.Max(top, ground + 0.24f);
                previousTop = top;
                route[i] = new Vector3(point.x, top, point.z);
            }

            GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RockPrefabPath);
            if (rockPrefab == null) throw new FileNotFoundException("No se encontró el prefab de roca reutilizable.", RockPrefabPath);

            for (int i = 0; i < platformCount; i++)
            {
                CreateRockStep(level, rockPrefab, route[i], i == platformCount - 1);
            }

            // Preserve the existing six collectible objects and their components;
            // only reposition them onto the new stepping-stone route.
            var crystals = FindInScene<Collectible>(level).OrderBy(c => c.name, StringComparer.Ordinal).ToList();
            GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefabPath);
            while (crystals.Count < 6)
            {
                GameObject created = (GameObject)PrefabUtility.InstantiatePrefab(crystalPrefab, level);
                created.name = "EnergyCrystal PA4 " + (crystals.Count + 1).ToString("00");
                crystals.Add(created.GetComponent<Collectible>());
            }
            for (int i = 0; i < crystals.Count; i++)
            {
                if (i < 6)
                {
                    crystals[i].gameObject.SetActive(true);
                    crystals[i].transform.position = route[i + 1] + Vector3.up * 0.08f;
                    crystals[i].name = "Cristal PA4 " + (i + 1).ToString("00");
                }
                else crystals[i].gameObject.SetActive(false);
            }

            GameObject spawn = FindInScene<Transform>(level)
                .Select(t => t.gameObject)
                .FirstOrDefault(go => go.name == "PlayerSpawn" || go.name == "DefaultSpawnPoint");
            Vector3 startFeet = route[0];
            player.transform.SetPositionAndRotation(startFeet + Vector3.up * 0.08f, Quaternion.LookRotation(Vector3.right));
            if (spawn != null)
            {
                spawn.transform.SetPositionAndRotation(startFeet, player.transform.rotation);
                player.spawnPoint = spawn.transform;
            }
            else player.spawnPoint = null;

            PA3GameManager manager = FindInScene<PA3GameManager>(level).FirstOrDefault();
            if (manager != null)
            {
                manager.totalCollectibles = 6;
                manager.collected = 0;
            }

            GameObject portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
            if (portalPrefab == null) throw new FileNotFoundException("No se encontró el prefab del portal de salida.", PortalPrefabPath);
            GameObject portal = (GameObject)PrefabUtility.InstantiatePrefab(portalPrefab, level);
            portal.name = "Portal salida - Segundo Nivel";
            portal.transform.SetPositionAndRotation(route[platformCount - 1], Quaternion.LookRotation(Vector3.right));

            foreach (UnityEngine.UI.Text label in FindInScene<UnityEngine.UI.Text>(level))
            {
                if (label.name == "TitleText") label.text = "SEGUNDO NIVEL · CRISTALES DEL BOSQUE";
                else if (label.name == "ObjectiveText") label.text = "00";
            }

            AddSceneToBuildSettings();
            EditorSceneManager.MarkSceneDirty(level);
            EditorSceneManager.SaveScene(level);
            SceneManager.SetActiveScene(level);
            if (openedSourceForBuild && sourceScene.IsValid() && sourceScene.isLoaded)
                EditorSceneManager.CloseScene(sourceScene, true);
            SceneManager.SetActiveScene(level);
            Debug.Log("PA4: SEGUNDO NIVEL generado desde PA3: bosque existente, 8 plataformas de roca, 6 cristales, jugador, HUD y portal. Copia de seguridad del borrador anterior: " + backupPath);
            return true;
        }
        catch (Exception exception)
        {
            if (level.IsValid() && level.isLoaded)
                EditorSceneManager.CloseScene(level, true);
            File.Copy(backupPath, targetFullPath, true);
            AssetDatabase.ImportAsset(TargetScenePath, ImportAssetOptions.ForceSynchronousImport);
            if (openedSourceForBuild && sourceScene.IsValid() && sourceScene.isLoaded)
            {
                SceneManager.SetActiveScene(sourceScene);
                EditorSceneManager.CloseScene(sourceScene, true);
            }
            if (!SceneManager.GetSceneByPath(TargetScenePath).isLoaded)
                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);
            Debug.LogException(exception);
            return false;
        }
    }

    private static void CreateRockStep(Scene scene, GameObject rockPrefab, Vector3 topPosition, bool broadLanding)
    {
        GameObject platform = new GameObject(broadLanding ? "Roca - llegada al portal" : "Roca de salto");
        SceneManager.MoveGameObjectToScene(platform, scene);
        platform.transform.position = topPosition - Vector3.up * 0.34f;

        BoxCollider collider = platform.AddComponent<BoxCollider>();
        collider.size = broadLanding ? new Vector3(3.8f, 0.68f, 3.8f) : new Vector3(2.15f, 0.68f, 2.15f);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, scene);
        visual.transform.SetParent(platform.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        visual.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        visual.transform.localScale = broadLanding ? new Vector3(1.65f, 0.62f, 1.65f) : new Vector3(1.18f, 0.58f, 1.18f);
    }

    private static void AddSceneToBuildSettings()
    {
        string guid = AssetDatabase.AssetPathToGUID(TargetScenePath);
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        if (current.Any(scene => scene.path == TargetScenePath)) return;
        EditorBuildSettings.scenes = current.Concat(new[] { new EditorBuildSettingsScene(TargetScenePath, true) }).ToArray();
    }

    private static T[] FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return Array.Empty<T>();
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .ToArray();
    }

    private static string FullProjectPath(string relativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
