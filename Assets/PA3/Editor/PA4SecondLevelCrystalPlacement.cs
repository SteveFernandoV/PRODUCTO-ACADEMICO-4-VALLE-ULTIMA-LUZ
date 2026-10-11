using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Places six second-level crystals and three nearby guardians.</summary>
internal static class PA4SecondLevelCrystalPlacement
{
    const string ScenePath = "Assets/Scenes/SEGUNDO NIVEL.unity";
    const string CrystalPrefabPath = "Assets/PA3/Prefabs/EnergyCrystal.prefab";
    const string GuardianPrefabPath = "Assets/PA3/Prefabs/GuardianEnemy.prefab";

    [MenuItem("PA4/Distribuir cristales y guardianes del segundo nivel")]
    static void PlaceCrystalsAndGuardians()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("PA4: detén Play antes de distribuir los cristales del segundo nivel.");
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene openScene = SceneManager.GetSceneAt(i);
            if (openScene.isDirty)
            {
                Debug.LogError("PA4: hay cambios sin guardar en una escena abierta. Guarda primero; el segundo nivel no se modificó.");
                return;
            }
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool closeAfterSave = !scene.IsValid() || !scene.isLoaded;
        if (closeAfterSave)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            Terrain terrain = Find<Terrain>(scene).FirstOrDefault(t => t != null && t.terrainData != null);
            PlayerController3D player = Find<PlayerController3D>(scene).FirstOrDefault(p => p != null);
            PA3GameManager manager = Find<PA3GameManager>(scene).FirstOrDefault(m => m != null);
            List<Collectible> crystals = Find<Collectible>(scene).Where(c => c != null)
                .OrderBy(c => c.name, StringComparer.Ordinal).ToList();
            List<EnemyChaser> guardians = Find<EnemyChaser>(scene).Where(g => g != null)
                .OrderBy(g => g.name, StringComparer.Ordinal).ToList();

            GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefabPath);
            GameObject guardianPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuardianPrefabPath);
            if (terrain == null || player == null || manager == null || crystalPrefab == null || guardianPrefab == null)
            {
                Debug.LogError($"PA4: se detuvo sin guardar; terreno={terrain != null}, jugador={player != null}, administrador={manager != null}, prefabCristal={crystalPrefab != null}, prefabGuardian={guardianPrefab != null}.");
                return;
            }

            while (crystals.Count < 6)
            {
                GameObject created = (GameObject)PrefabUtility.InstantiatePrefab(crystalPrefab, scene);
                Collectible collectible = created.GetComponent<Collectible>();
                if (collectible == null) throw new InvalidOperationException("El prefab EnergyCrystal no tiene Collectible en la raíz.");
                crystals.Add(collectible);
            }
            while (guardians.Count < 3)
            {
                GameObject created = (GameObject)PrefabUtility.InstantiatePrefab(guardianPrefab, scene);
                EnemyChaser chaser = created.GetComponent<EnemyChaser>();
                if (chaser == null) throw new InvalidOperationException("El prefab GuardianEnemy no tiene EnemyChaser en la raíz.");
                guardians.Add(chaser);
            }
            crystals = crystals.OrderBy(c => c.name, StringComparer.Ordinal).ToList();
            guardians = guardians.OrderBy(g => g.name, StringComparer.Ordinal).ToList();

            Vector3 start = player.transform.position;
            const float radius = 70f;
            float startAngle = UnityEngine.Random.Range(0f, 360f);
            Vector3[] positions = new Vector3[6];
            for (int i = 0; i < positions.Length; i++)
            {
                float angle = (startAngle + i * 60f) * Mathf.Deg2Rad;
                Vector3 point = new Vector3(start.x + Mathf.Cos(angle) * radius, 0f,
                                            start.z + Mathf.Sin(angle) * radius);
                positions[i] = GroundPoint(terrain, point, 1f);
                float actualDistance = Vector2.Distance(new Vector2(start.x, start.z),
                    new Vector2(positions[i].x, positions[i].z));
                if (Mathf.Abs(actualDistance - radius) > 0.5f)
                {
                    Debug.LogError($"PA4: se detuvo sin guardar; el terreno no permite colocar el cristal {i + 1} a 70 m dentro de sus límites.");
                    return;
                }
            }

            for (int i = 0; i < crystals.Count; i++)
            {
                bool active = i < positions.Length;
                crystals[i].gameObject.SetActive(active);
                if (!active) continue;
                crystals[i].name = $"Cristal Nivel 2 {i + 1:00}";
                crystals[i].transform.position = positions[i];
            }

            foreach (EnemyChaser guardian in guardians)
                guardian.gameObject.SetActive(false);

            int[] crystalIndices = Enumerable.Range(0, positions.Length).OrderBy(_ => UnityEngine.Random.value).Take(3).ToArray();
            int[] guardianIndices = Enumerable.Range(0, guardians.Count).OrderBy(_ => UnityEngine.Random.value).Take(3).ToArray();
            for (int i = 0; i < 3; i++)
            {
                EnemyChaser guardian = guardians[guardianIndices[i]];
                int crystalIndex = crystalIndices[i];
                Vector3 crystalPosition = positions[crystalIndex];
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 nearbyPoint = crystalPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 7f;
                guardian.transform.position = GroundPoint(terrain, nearbyPoint, 0.05f);
                guardian.target = player.transform;
                guardian.name = $"Guardián Nivel 2 {i + 1:00}";
                guardian.gameObject.SetActive(true);
            }

            manager.totalCollectibles = 6;
            manager.collected = 0;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("PA4: segundo nivel guardado; exactamente 6 cristales activos a 70 m del inicio y 3 guardianes activos junto a cristales elegidos al azar.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (closeAfterSave && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    static Vector3 GroundPoint(Terrain terrain, Vector3 point, float heightOffset)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        point.x = Mathf.Clamp(point.x, origin.x + 5f, origin.x + size.x - 5f);
        point.z = Mathf.Clamp(point.z, origin.z + 5f, origin.z + size.z - 5f);
        point.y = terrain.SampleHeight(point) + origin.y + heightOffset;
        return point;
    }

    static T[] Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .Where(root => root != null)
        .SelectMany(root => root.GetComponentsInChildren<T>(true))
        .Where(component => component != null && component.gameObject.scene == scene)
        .ToArray();
}
