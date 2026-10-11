using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Creates a separate third level from the saved second-level forest.</summary>
[InitializeOnLoad]
internal static class PA4ThirdLevelBuilder
{
    const string SourceScenePath = "Assets/Scenes/SEGUNDO NIVEL.unity";
    const string TargetScenePath = "Assets/Scenes/TERCER NIVEL.unity";
    const string RunMarkerPath = "Assets/PA3/Editor/BUILD_THIRD_LEVEL_ONCE.txt";
    const string GuardianPrefabPath = "Assets/PA3/Prefabs/GuardianEnemy.prefab";
    const string CrystalPrefabPath = "Assets/PA3/Prefabs/EnergyCrystal.prefab";

    static PA4ThirdLevelBuilder()
    {
        EditorApplication.delayCall += RunRequestedBuild;
    }

    [MenuItem("PA4/Generar tercer nivel de bosque")]
    static void BuildFromMenu() => BuildThirdLevel();

    static void RunRequestedBuild()
    {
        string marker = FullPath(RunMarkerPath);
        if (!File.Exists(marker)) return;
        if (BuildThirdLevel())
        {
            File.Delete(marker);
            AssetDatabase.Refresh();
        }
    }

    static bool BuildThirdLevel()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("PA4: detén Play antes de generar el tercer nivel.");
            return false;
        }

        string sourceFullPath = FullPath(SourceScenePath);
        string targetFullPath = FullPath(TargetScenePath);
        if (!File.Exists(sourceFullPath))
        {
            Debug.LogError("PA4: no se encontró SEGUNDO NIVEL; no se modificó ninguna escena.");
            return false;
        }
        if (File.Exists(targetFullPath))
        {
            Debug.LogError("PA4: TERCER NIVEL ya existe; se conserva intacto y no se sobrescribió.");
            return false;
        }
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene openScene = SceneManager.GetSceneAt(i);
            if (openScene.isDirty)
            {
                Debug.LogError("PA4: hay cambios sin guardar en una escena abierta. Guarda primero; no se modificó ninguna escena.");
                return false;
            }
        }

        Scene level = default;
        try
        {
            // Copy the saved level, then edit only the new scene asset.
            File.Copy(sourceFullPath, targetFullPath);
            AssetDatabase.ImportAsset(TargetScenePath, ImportAssetOptions.ForceSynchronousImport);
            level = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(level);

            Terrain terrain = FindInScene<Terrain>(level).FirstOrDefault(t => t.terrainData != null);
            PlayerController3D player = FindInScene<PlayerController3D>(level).FirstOrDefault();
            PA3GameManager manager = FindInScene<PA3GameManager>(level).FirstOrDefault();
            ExitTrigger exit = FindInScene<ExitTrigger>(level).FirstOrDefault();
            GameObject guardianPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuardianPrefabPath);
            if (terrain == null || player == null || manager == null || exit == null || guardianPrefab == null)
                throw new InvalidOperationException("La escena base necesita Terrain, jugador, administrador, portal y prefab GuardianEnemy.");

            Vector3 terrainOrigin = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            Vector3[] fractions =
            {
                new Vector3(0.14f, 0f, 0.15f), new Vector3(0.50f, 0f, 0.12f),
                new Vector3(0.86f, 0f, 0.24f), new Vector3(0.18f, 0f, 0.72f),
                new Vector3(0.52f, 0f, 0.62f), new Vector3(0.84f, 0f, 0.80f)
            };
            Vector3[] gemPositions = fractions.Select(f => GroundPoint(terrain,
                new Vector3(terrainOrigin.x + terrainSize.x * f.x, terrainOrigin.y,
                            terrainOrigin.z + terrainSize.z * f.z), 0.95f)).ToArray();

            // Keep the current spawn location but ensure the six stations cover distinct areas.
            Vector3 startPosition = player.transform.position;
            if (gemPositions.Any(p => Vector3.Distance(new Vector2(p.x, p.z), new Vector2(startPosition.x, startPosition.z)) < 18f))
                Debug.LogWarning("PA4: una zona de cristales queda relativamente cerca del punto de inicio; se mantienen las seis zonas distribuidas por el bosque.");

            List<Collectible> crystals = FindInScene<Collectible>(level)
                .OrderBy(c => c.name, StringComparer.Ordinal).ToList();
            GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefabPath);
            if (crystalPrefab == null)
                throw new FileNotFoundException("No se encontró el prefab reutilizable de cristal.", CrystalPrefabPath);
            while (crystals.Count < 6)
            {
                GameObject created = (GameObject)PrefabUtility.InstantiatePrefab(crystalPrefab, level);
                created.name = "Cristal Nivel 3 " + (crystals.Count + 1).ToString("00");
                crystals.Add(created.GetComponent<Collectible>());
            }
            for (int i = 0; i < crystals.Count; i++)
            {
                if (i < 6)
                {
                    crystals[i].gameObject.SetActive(true);
                    crystals[i].name = "Cristal Nivel 3 " + (i + 1).ToString("00");
                    crystals[i].transform.position = gemPositions[i];
                }
                else crystals[i].gameObject.SetActive(false);
            }

            // Remove only the copied scene's existing guardians, then place one beside each gem.
            foreach (EnemyChaser oldGuardian in FindInScene<EnemyChaser>(level))
                UnityEngine.Object.DestroyImmediate(oldGuardian.gameObject);
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 137.5f + 35f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 8f;
                Vector3 guardianPosition = GroundPoint(terrain, gemPositions[i] + offset, 0.05f);
                GameObject guardianObject = (GameObject)PrefabUtility.InstantiatePrefab(guardianPrefab, level);
                guardianObject.name = "Guardián Nivel 3 " + (i + 1).ToString("00");
                guardianObject.transform.position = guardianPosition;
                EnemyChaser chaser = guardianObject.GetComponent<EnemyChaser>();
                chaser.target = player.transform;
                chaser.detectionRange = 24f;
                chaser.patrolRadius = 5f;
            }

            manager.totalCollectibles = 6;
            manager.collected = 0;
            Vector3 portalPosition = GroundPoint(terrain, gemPositions[5] + new Vector3(13f, 0f, 8f), 0f);
            exit.transform.root.SetPositionAndRotation(portalPosition, Quaternion.LookRotation(Vector3.right));
            player.spawnPoint = FindInScene<Transform>(level)
                .FirstOrDefault(t => t.name == "PlayerSpawn" || t.name == "DefaultSpawnPoint");
            if (player.spawnPoint != null)
                player.transform.SetPositionAndRotation(player.spawnPoint.position, player.spawnPoint.rotation);

            foreach (UnityEngine.UI.Text label in FindInScene<UnityEngine.UI.Text>(level))
            {
                if (label.name == "TitleText") label.text = "TERCER NIVEL · EL ÚLTIMO RESPLANDOR";
                else if (label.name == "ObjectiveText") label.text = "00";
            }

            AddSceneToBuildSettings();
            EditorSceneManager.MarkSceneDirty(level);
            EditorSceneManager.SaveScene(level);
            SceneManager.SetActiveScene(level);
            Debug.Log("PA4: TERCER NIVEL creado en una escena nueva. Bosque preservado, 6 cristales en distintas zonas, 6 guardianes próximos, portal final y cierre musical al completarlo.");
            return true;
        }
        catch (Exception exception)
        {
            if (level.IsValid() && level.isLoaded) EditorSceneManager.CloseScene(level, true);
            if (File.Exists(targetFullPath)) File.Delete(targetFullPath);
            string meta = targetFullPath + ".meta";
            if (File.Exists(meta)) File.Delete(meta);
            AssetDatabase.Refresh();
            Debug.LogException(exception);
            return false;
        }
    }

    static Vector3 GroundPoint(Terrain terrain, Vector3 point, float heightOffset)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        point.x = Mathf.Clamp(point.x, origin.x + 4f, origin.x + size.x - 4f);
        point.z = Mathf.Clamp(point.z, origin.z + 4f, origin.z + size.z - 4f);
        point.y = terrain.SampleHeight(point) + origin.y + heightOffset;
        return point;
    }

    static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        if (current.Any(scene => scene.path == TargetScenePath)) return;
        EditorBuildSettings.scenes = current.Concat(new[] { new EditorBuildSettingsScene(TargetScenePath, true) }).ToArray();
    }

    static T[] FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return Array.Empty<T>();
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    }

    static string FullPath(string relativePath) => Path.Combine(
        Directory.GetParent(Application.dataPath).FullName,
        relativePath.Replace('/', Path.DirectorySeparatorChar));
}
