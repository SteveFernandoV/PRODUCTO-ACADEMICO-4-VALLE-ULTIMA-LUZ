using UnityEngine;

/// <summary>
/// Rebuilds the Terrain collider at scene startup so Terrain-painted tree
/// colliders are recreated from the current tree prefab assets.
/// </summary>
public static class TerrainTreeColliderRefresh
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RefreshTerrainColliders()
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null) continue;

            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
            if (terrainCollider == null || !terrainCollider.enabled) continue;

            terrainCollider.enabled = false;
            terrainCollider.enabled = true;
        }
    }
}
