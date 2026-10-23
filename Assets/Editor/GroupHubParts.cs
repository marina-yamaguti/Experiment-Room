#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click helper for the Hub scene. The Sweet Home 3D export names every door part with
/// a running number (frame_37, ..._door_45 ...), and Unity lists them in no useful order.
/// This groups them under Door_A / Door_B / Door_C by that number:
///   28-36 -> Door_B (blue)   37-45 -> Door_A (orange)   46-54 -> Door_C (green)
///
/// USE: with the Hub scene open, menu  Tools > Hub > Group door parts.
/// Safe to run twice (parts already moved are not touched again). Undo with Cmd+Z.
/// </summary>
public static class GroupHubParts
{
    [MenuItem("Tools/Hub/Group door parts")]
    private static void GroupDoorParts()
    {
        GameObject hub = GameObject.Find("Hub");
        if (hub == null)
        {
            Debug.LogError("GroupHubParts: no object called 'Hub' in the open scene.");
            return;
        }

        // Parts of a model instance cannot be moved around, so unpack it first.
        if (PrefabUtility.IsPartOfPrefabInstance(hub))
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(hub);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            Debug.Log("GroupHubParts: unpacked '" + root.name + "'.");
        }

        Transform doorA = FindOrCreate("Door_A", hub.transform);
        Transform doorB = FindOrCreate("Door_B", hub.transform);
        Transform doorC = FindOrCreate("Door_C", hub.transform);

        // copy first: we change the hierarchy while looping
        List<Transform> children = new List<Transform>();
        foreach (Transform child in hub.transform) children.Add(child);

        int moved = 0;
        foreach (Transform child in children)
        {
            Match m = Regex.Match(child.name, @"(\d+)$");
            if (!m.Success) continue;

            int n = int.Parse(m.Groups[1].Value);
            Transform target = null;
            if (n >= 28 && n <= 36) target = doorB;
            else if (n >= 37 && n <= 45) target = doorA;
            else if (n >= 46 && n <= 54) target = doorC;
            if (target == null) continue;

            Undo.SetTransformParent(child, target, "Group door parts"); // keeps world position
            if (child.parent == target) moved++;
            else Debug.LogWarning("GroupHubParts: could not move '" + child.name + "'.", child);
        }

        Debug.Log("GroupHubParts: moved " + moved + " parts (27 expected on the first run). Door_A=" + doorA.childCount + ", Door_B=" + doorB.childCount + ", Door_C=" + doorC.childCount);
    }

    private static Transform FindOrCreate(string objectName, Transform parent)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null) return existing.transform;

        GameObject created = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(created, "Create " + objectName);
        created.transform.SetParent(parent, false);
        return created.transform;
    }
}
#endif
