#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click helper for the experiment room (Room_A.obj from Sweet Home 3D).
/// The export is a flat list of parts with numbered names. This groups them into
/// Obj_Book, Obj_Mug, Obj_Lamp, Obj_Frame, Obj_Vase, Table and Door, fits a BoxCollider
/// around each object (so the ray pointer can select it) and adds a PointerTarget.
/// It also hides the extra "ground_1" plane that would flicker against the floor.
///
/// USE: drag Room_A.obj into the scene (object must be called "Room_A"), then
/// menu  Tools > Room > Group experiment room parts.   Safe to run twice; Cmd+Z undoes it.
/// </summary>
public static class GroupRoomParts
{
    private static readonly Dictionary<string, string[]> ObjectParts = new Dictionary<string, string[]>
    {
        { "Obj_Book",  new[] { "face_avant_45", "tranche_46", "face_arriere_47", "pages_48", "bas_tranche_49" } },
        { "Obj_Mug",   new[] { "Coffee_mug_Coffee_mug_White_ceramic_50" } },
        { "Obj_Lamp",  new[] { "Table_lamp_041_51", "Table_lamp_041_11132_52", "Table_lamp_041_12282_53" } },
        { "Obj_Frame", new[] { "54", "55", "56", "57" } },
        { "Obj_Vase",  new[] { "jar_Plane_58", "jar_Plane_324_59" } },
        { "Table",     new[] { "Outdoor_table_043_44" } },
    };

    [MenuItem("Tools/Room/Group experiment room parts")]
    private static void Run()
    {
        GameObject room = GameObject.Find("Room_A");
        if (room == null)
        {
            Debug.LogError("GroupRoomParts: no object called 'Room_A' in the open scene.");
            return;
        }

        // Parts of a model instance cannot be moved around, so unpack it first.
        if (PrefabUtility.IsPartOfPrefabInstance(room))
        {
            GameObject outer = PrefabUtility.GetOutermostPrefabInstanceRoot(room);
            PrefabUtility.UnpackPrefabInstance(outer, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            Debug.Log("GroupRoomParts: unpacked '" + outer.name + "'.");
        }

        Transform root = room.transform;

        Transform ground = root.Find("ground_1");
        if (ground != null) ground.gameObject.SetActive(false);

        // snapshot first: we change the hierarchy while looping
        List<Transform> children = new List<Transform>();
        foreach (Transform child in root) children.Add(child);

        foreach (KeyValuePair<string, string[]> entry in ObjectParts)
        {
            Transform group = FindOrCreateChild(root, entry.Key);
            int moved = 0;
            foreach (Transform child in children)
            {
                if (Array.IndexOf(entry.Value, child.name) < 0) continue;
                Undo.SetTransformParent(child, group, "Group room parts");
                moved++;
            }

            if (entry.Key.StartsWith("Obj_"))
            {
                FitBoxCollider(group);
                if (group.GetComponent<PointerTarget>() == null)
                    Undo.AddComponent<PointerTarget>(group.gameObject);
            }

            Debug.Log("GroupRoomParts: " + entry.Key + " <- " + moved + " parts (" + group.childCount + " inside now).");
        }

        // everything the door is made of (frame, hinges, handles, leaf)
        Transform door = FindOrCreateChild(root, "Door");
        int doorParts = 0;
        foreach (Transform child in children)
        {
            if (!child.name.StartsWith("sweethome3d_") && child.name != "frame_35") continue;
            Undo.SetTransformParent(child, door, "Group room parts");
            doorParts++;
        }
        Debug.Log("GroupRoomParts: Door <- " + doorParts + " parts (" + door.childCount + " inside now).");
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null) return existing;

        GameObject created = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(created, "Create " + childName);
        created.transform.SetParent(parent, false);
        return created.transform;
    }

    private static void FitBoxCollider(Transform group)
    {
        Renderer[] renderers = group.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

        BoxCollider box = group.GetComponent<BoxCollider>();
        if (box == null) box = Undo.AddComponent<BoxCollider>(group.gameObject);

        Vector3 s = group.lossyScale;
        Vector3 padded = bounds.size + Vector3.one * 0.03f; // 3 cm of slack makes pointing easier
        box.center = group.InverseTransformPoint(bounds.center);
        box.size = new Vector3(padded.x / Mathf.Abs(s.x), padded.y / Mathf.Abs(s.y), padded.z / Mathf.Abs(s.z));
    }
}
#endif
