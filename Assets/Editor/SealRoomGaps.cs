using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Room > Seal gaps between walls and floor/ceiling
/// The Sweet Home 3D export can leave a crack where the floor and the ceiling stop a few millimetres
/// before a wall (the light from outside shows through it). This adds thin patches, with the same
/// material as the floor / ceiling, over every such crack in the open scene.
/// Safe to run again (it removes its own patches first). The ending setup tool runs it too.
/// </summary>
public static class SealRoomGaps
{
    private const string PatchRoot = "RoomGapPatches";

    [MenuItem("Tools/Room/Seal gaps between walls and floor-ceiling")]
    public static void SealMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Seal gaps", "Stop Play mode first.", "OK");
            return;
        }

        string scene = EditorSceneManager.GetActiveScene().name;
        if (scene == "Hub" || scene == "RoomB" || scene == "RoomC")
        {
            if (!EditorUtility.DisplayDialog("Seal gaps",
                "\"" + scene + "\" is a teammate's scene. Change it anyway?", "Yes", "Cancel")) return;
        }

        int count = Seal();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("SealRoomGaps: added " + count + " patch(es). Save the scene (Cmd+S).");
    }

    /// <returns>The number of patches added.</returns>
    public static int Seal()
    {
        GameObject old = GameObject.Find(PatchRoot);
        while (old != null)
        {
            Object.DestroyImmediate(old);
            old = GameObject.Find(PatchRoot);
        }

        // the walls (wall_...) and the floor / ceiling surfaces (room_...) of the Sweet Home 3D model
        bool anyWall = false;
        Bounds walls = new Bounds();
        System.Collections.Generic.List<Renderer> surfaces = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            string n = r.gameObject.name;
            if (n.StartsWith("wall_"))
            {
                if (!anyWall) { walls = r.bounds; anyWall = true; }
                else walls.Encapsulate(r.bounds);
            }
            else if (n.StartsWith("room_")) surfaces.Add(r);
        }
        if (!anyWall || surfaces.Count == 0) return 0;

        GameObject root = new GameObject(PatchRoot);
        int count = 0;
        const float overlap = 0.02f;   // the patch reaches 2 cm under the existing surface
        const float eps = 0.002f;      // gaps smaller than 2 mm are ignored

        foreach (Renderer surface in surfaces)
        {
            Bounds b = surface.bounds;
            bool ceiling = b.center.y > walls.center.y;
            // 1 mm inside the room, so the patch never fights with the surface for the same pixels
            float y = b.center.y + (ceiling ? -0.001f : 0.001f);

            if (walls.min.x < b.min.x - eps) count += Patch(root.transform, surface, ceiling, y, walls.min.x, b.min.x + overlap, walls.min.z, walls.max.z);
            if (walls.max.x > b.max.x + eps) count += Patch(root.transform, surface, ceiling, y, b.max.x - overlap, walls.max.x, walls.min.z, walls.max.z);
            if (walls.min.z < b.min.z - eps) count += Patch(root.transform, surface, ceiling, y, walls.min.x, walls.max.x, walls.min.z, b.min.z + overlap);
            if (walls.max.z > b.max.z + eps) count += Patch(root.transform, surface, ceiling, y, walls.min.x, walls.max.x, b.max.z - overlap, walls.max.z);
        }

        if (count == 0) Object.DestroyImmediate(root);
        return count;
    }

    private static int Patch(Transform parent, Renderer surface, bool ceiling, float y, float x0, float x1, float z0, float z1)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Patch_" + surface.gameObject.name + (ceiling ? "_ceiling" : "_floor");
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, true);

        quad.transform.position = new Vector3((x0 + x1) / 2f, y, (z0 + z1) / 2f);
        // a Quad's visible side faces -Z: turn it to face down for a ceiling, up for a floor
        quad.transform.rotation = Quaternion.Euler(ceiling ? -90f : 90f, 0f, 0f);
        quad.transform.localScale = new Vector3(x1 - x0, z1 - z0, 1f);

        Renderer r = quad.GetComponent<Renderer>();
        r.sharedMaterial = surface.sharedMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided; // blocks the light from both sides
        return 1;
    }
}
