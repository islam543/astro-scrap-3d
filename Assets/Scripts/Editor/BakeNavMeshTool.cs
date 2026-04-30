using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class BakeNavMeshTool
{
    [MenuItem("Tools/Bake NavMesh Now")]
    public static void BakeNow()
    {
        // Find or create a NavMeshSurface on the Environment
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();

        if (surface == null)
        {
            // Add to scene root
            GameObject navHost = new GameObject("NavMeshSurface");
            surface = navHost.AddComponent<NavMeshSurface>();
            Debug.Log("[BakeNavMesh] Created new NavMeshSurface GameObject.");
        }

        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
        Debug.Log("[BakeNavMesh] NavMesh baked successfully!");
    }
}
