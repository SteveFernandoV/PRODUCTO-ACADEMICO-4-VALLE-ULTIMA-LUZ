using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Repositions only the third-level collectibles and guardians over sampled terrain.</summary>
internal static class PA4ThirdLevelCrystalPlacement
{
    const string ScenePath = "Assets/Scenes/TERCER NIVEL.unity";

    [MenuItem("PA4/Corregir cristales del tercer nivel sobre el terreno")]
    static void PlaceCrystalsAndGuardians()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("PA4: detén Play antes de corregir los cristales.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("PA4: abre TERCER NIVEL antes de corregir sus cristales.");
            return;
        }

        Terrain terrain = Find<Terrain>(scene).FirstOrDefault(t => t != null && t.terrainData != null);
        PlayerController3D player = Find<PlayerController3D>(scene).FirstOrDefault(p => p != null);
        PA3GameManager manager = Find<PA3GameManager>(scene).FirstOrDefault(m => m != null);
        Collectible[] crystals = Find<Collectible>(scene).Where(c => c != null)
            .OrderBy(c => c.name, StringComparer.Ordinal).ToArray();
        EnemyChaser[] guardians = Find<EnemyChaser>(scene).Where(g => g != null)
            .OrderBy(g => g.name, StringComparer.Ordinal).ToArray();

        if (terrain == null || player == null || manager == null || crystals.Length != 6 || guardians.Length != 6)
        {
            Debug.LogError($"PA4: se detuvo sin guardar; encontrados terreno={terrain != null}, jugador={player != null}, administrador={manager != null}, cristales={crystals.Length}, guardianes={guardians.Length}.");
            return;
        }

        Vector3 start = player.transform.position;
        // Place crystals on a 70 m ring, with spacing to avoid stacking them together.
        const float radius = 70f;
        Vector2[] offsets =
        {
            new Vector2(0f, -radius), new Vector2(radius * 0.8660254f, -radius * 0.5f),
            new Vector2(radius * 0.8660254f, radius * 0.5f), new Vector2(0f, radius),
            new Vector2(-radius * 0.8660254f, radius * 0.5f), new Vector2(-radius * 0.8660254f, -radius * 0.5f)
        };

        for (int i = 0; i < crystals.Length; i++)
        {
            Vector3 point = new Vector3(start.x + offsets[i].x, 0f, start.z + offsets[i].y);
            point = GroundPoint(terrain, point, 1.0f);
            crystals[i].transform.position = point;
            crystals[i].gameObject.SetActive(true);

            float angle = (i * 137.5f + 35f) * Mathf.Deg2Rad;
            Vector3 enemyPoint = point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 7f;
            guardians[i].transform.position = GroundPoint(terrain, enemyPoint, 0.05f);
            guardians[i].target = player.transform;
        }

        manager.totalCollectibles = 6;
        manager.collected = 0;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("PA4: se guardaron 6 cristales sobre la altura real del terreno, a 70 m del inicio, y 6 guardianes a 7 m de cada cristal.");
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
