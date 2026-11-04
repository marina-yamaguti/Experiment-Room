using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > End > Set up ending scene
/// Run it in the "End" scene (a copy of the Hub). It
///   - locks the three doors,
///   - adds an invisible floor collider so objects do not fall through the room,
///   - builds a table with a book and a pen (physics + ray pickup) in front of the camera rig,
///   - adds the "Ending" object that runs pick-up -> paper questionnaire -> thank you.
/// Safe to run again: it removes what it created before and rebuilds it. It refuses to run in
/// any scene not called "End", so the Hub and the rooms cannot be changed by mistake.
/// </summary>
public static class BuildEndingScene
{
    private const string MaterialFolder = "Assets/Environment/End";

    [MenuItem("Tools/End/Set up ending scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Set up ending scene", "Stop Play mode first.", "OK");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "End")
        {
            EditorUtility.DisplayDialog("Set up ending scene",
                "This tool only runs in the scene called \"End\" (the active scene is \"" + scene.name + "\").\n" +
                "Duplicate the Hub scene, name it End and open it.", "OK");
            return;
        }

        OVRCameraRig rig = Object.FindFirstObjectByType<OVRCameraRig>();
        if (rig == null)
        {
            EditorUtility.DisplayDialog("Set up ending scene", "No OVRCameraRig (Camera Rig) found in this scene.", "OK");
            return;
        }

        // start clean, so the tool can be run again
        foreach (string n in new[] { "EndTable", "EndFloor", "Book", "Pen", "Ending" })
        {
            GameObject old = GameObject.Find(n);
            while (old != null)
            {
                Object.DestroyImmediate(old);
                old = GameObject.Find(n);
            }
        }

        int doors = LockDoors();

        // everything is placed relative to where the participant starts
        Vector3 flat = rig.transform.forward;
        flat.y = 0f;
        flat.Normalize();
        Quaternion facing = Quaternion.LookRotation(flat);
        Vector3 tablePos = rig.transform.position + flat * 1.3f;
        tablePos.y = 0f;

        // invisible floor
        GameObject floor = new GameObject("EndFloor");
        floor.transform.position = new Vector3(rig.transform.position.x, -0.1f, rig.transform.position.z);
        BoxCollider floorCollider = floor.AddComponent<BoxCollider>();
        floorCollider.size = new Vector3(30f, 0.2f, 30f);

        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Environment", "End");
        Material wood = MakeMaterial("EndWood", new Color(0.55f, 0.38f, 0.22f));
        Material cover = MakeMaterial("EndBookCover", new Color(0.55f, 0.10f, 0.12f));
        Material pages = MakeMaterial("EndBookPages", new Color(0.94f, 0.91f, 0.82f));
        Material ink = MakeMaterial("EndPen", new Color(0.08f, 0.15f, 0.55f));

        // table: top at 0.75 m, one collider on the top only (the table is static, it never moves)
        GameObject table = new GameObject("EndTable");
        table.transform.SetPositionAndRotation(tablePos, facing);
        AddPart(table.transform, "Top", new Vector3(0f, 0.73f, 0f), new Vector3(1.2f, 0.04f, 0.7f), wood, true);
        foreach (Vector2 leg in new[] { new Vector2(-0.55f, -0.3f), new Vector2(0.55f, -0.3f), new Vector2(-0.55f, 0.3f), new Vector2(0.55f, 0.3f) })
            AddPart(table.transform, "Leg", new Vector3(leg.x, 0.365f, leg.y), new Vector3(0.05f, 0.73f, 0.05f), wood, false);

        float top = 0.75f;

        // book: a red cover with a lighter block of pages
        GameObject bookObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bookObject.name = "Book";
        bookObject.transform.SetPositionAndRotation(tablePos + facing * new Vector3(-0.25f, top + 0.0155f, 0f),
                                                    facing * Quaternion.Euler(0f, -15f, 0f));
        bookObject.transform.localScale = new Vector3(0.15f, 0.03f, 0.21f);
        bookObject.GetComponent<Renderer>().sharedMaterial = cover;
        GameObject pageBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pageBlock.name = "Pages";
        Object.DestroyImmediate(pageBlock.GetComponent<Collider>());
        pageBlock.transform.SetParent(bookObject.transform, false);
        pageBlock.transform.localScale = new Vector3(0.92f, 0.80f, 1.01f);
        pageBlock.GetComponent<Renderer>().sharedMaterial = pages;
        GrabbableProp bookProp = MakeGrabbable(bookObject, 0.5f);

        // pen: a thin cylinder lying on the table; its collider is thicker than the pen so it is easy to point at
        GameObject penObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        penObject.name = "Pen";
        penObject.transform.SetPositionAndRotation(tablePos + facing * new Vector3(0.25f, top + 0.03f, 0.05f),
                                                   facing * Quaternion.Euler(0f, 20f, 90f));
        penObject.transform.localScale = new Vector3(0.014f, 0.08f, 0.014f);
        penObject.GetComponent<Renderer>().sharedMaterial = ink;
        Object.DestroyImmediate(penObject.GetComponent<Collider>());
        BoxCollider penCollider = penObject.AddComponent<BoxCollider>();
        penCollider.size = new Vector3(2.5f, 2f, 2.5f);
        GrabbableProp penProp = MakeGrabbable(penObject, 0.05f);

        // the manager
        GameObject ending = new GameObject("Ending");
        EndingManager manager = ending.AddComponent<EndingManager>();
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("book").objectReferenceValue = bookProp;
        so.FindProperty("pen").objectReferenceValue = penProp;
        so.FindProperty("tableRoot").objectReferenceValue = table.transform;
        so.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("BuildEndingScene: locked " + doors + " door(s), built the table with book and pen, added the Ending object. Save the scene (Cmd+S).");
    }

    private static int LockDoors()
    {
        int count = 0;
        foreach (SceneLink link in Object.FindObjectsByType<SceneLink>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            PointerTarget target = link.GetComponent<PointerTarget>();
            if (target == null) continue;
            SerializedObject so = new SerializedObject(target);
            so.FindProperty("interactable").boolValue = false;
            so.ApplyModifiedProperties();
            count++;
        }
        return count;
    }

    private static GrabbableProp MakeGrabbable(GameObject go, float mass)
    {
        Rigidbody body = go.AddComponent<Rigidbody>();
        body.mass = mass;
        go.AddComponent<PointerTarget>();
        return go.AddComponent<GrabbableProp>();
    }

    private static void AddPart(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider) Object.DestroyImmediate(part.GetComponent<Collider>());
    }

    // materials must be real assets, otherwise they are lost when the scene is saved and reloaded
    private static Material MakeMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }
}
