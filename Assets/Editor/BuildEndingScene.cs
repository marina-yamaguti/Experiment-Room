using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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
        foreach (string n in new[] { "EndTable", "EndFloor", "Book", "Pen", "Ending", "LightPanel", "LightPanelStand", "RoomLight", "FanSound", "RoomGapPatches" })
        {
            GameObject old = GameObject.Find(n);
            while (old != null)
            {
                Object.DestroyImmediate(old);
                old = GameObject.Find(n);
            }
        }

        int doors = LockDoors();
        int patches = SealRoomGaps.Seal(); // closes the crack between the left wall and the floor / ceiling

        // the room's size, measured before anything is added (to keep the light panel inside the room)
        Bounds room = new Bounds(rig.transform.position, Vector3.zero);
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) room.Encapsulate(r.bounds);

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

        BuildLightPanel(rig, tablePos, flat, room, wood);
        AddFanSound();

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
        Debug.Log("BuildEndingScene: sealed " + patches + " gap(s), locked " + doors + " door(s), built the table with book and pen, added the Ending object. Save the scene (Cmd+S).");
    }

    // the fan's sound (same settings as in the Hub: looping, 3D, fades out over 8 m)
    private static void AddFanSound()
    {
        FanSpinner fan = Object.FindFirstObjectByType<FanSpinner>();
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/fan.wav");
        if (fan == null || clip == null)
        {
            Debug.LogWarning("BuildEndingScene: no FanSpinner in this scene or no Assets/Audio/fan.wav, fan sound not added.");
            return;
        }

        // the fan's pivot sits at the scene origin, so the sound goes to the centre of the fan itself
        Vector3 position = fan.transform.position;
        if (fan.hub != null)
        {
            position = fan.hub.bounds.center;
        }
        else
        {
            Renderer[] parts = fan.GetComponentsInChildren<Renderer>();
            if (parts.Length > 0)
            {
                Bounds b = parts[0].bounds;
                foreach (Renderer r in parts) b.Encapsulate(r.bounds);
                position = b.center;
            }
        }

        GameObject go = new GameObject("FanSound");
        go.transform.position = position;
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = true;
        source.volume = 1f;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = 8f;
    }

    // ---- light control panel: world-space Canvas on a stand, plus the RoomLight it controls ----------

    private static void BuildLightPanel(OVRCameraRig rig, Vector3 tablePos, Vector3 flat, Bounds room, Material wood)
    {
        GameObject lightObject = new GameObject("RoomLight");
        lightObject.AddComponent<RoomLight>();

        // beside the table, on whichever side stays inside the room (right side first)
        Vector3 right = Vector3.Cross(Vector3.up, flat);
        Vector3 position = tablePos + right * 1.2f - flat * 0.1f;
        if (!InsideRoom(room, position)) position = tablePos - right * 1.2f - flat * 0.1f;
        position.y = 1.35f;

        Vector3 awayFromRig = position - rig.transform.position;
        awayFromRig.y = 0f;
        Quaternion rotation = Quaternion.LookRotation(awayFromRig.normalized); // a canvas reads correctly looking along its +Z

        GameObject panel = new GameObject("LightPanel");
        panel.transform.SetPositionAndRotation(position, rotation);
        panel.AddComponent<LightPanel>();

        // the canvas: 400 x 300 units, 0.002 m per unit = 0.8 x 0.6 m
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
        canvasObject.transform.SetParent(panel.transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400f, 300f);
        canvasRect.localScale = Vector3.one * 0.002f;

        Color cyan = new Color32(0xB8, 0xF5, 0xFF, 255);
        MakeImage(canvasRect, "Background", Vector2.zero, new Vector2(400f, 300f), new Color(0.04f, 0.05f, 0.07f, 0.92f));
        MakeText(canvasRect, "Title", "ROOM LIGHT", new Vector2(0f, 112f), new Vector2(360f, 50f), 38f, cyan);
        MakeText(canvasRect, "StatusLabel", "Light ON  -  100%", new Vector2(0f, 66f), new Vector2(360f, 40f), 28f, Color.white);

        // the level bar: a track with a fill inside it; LightPanel changes how far the fill stretches
        RectTransform track = MakeImage(canvasRect, "BarTrack", new Vector2(0f, 28f), new Vector2(300f, 16f), new Color(0.25f, 0.27f, 0.30f));
        RectTransform fill = MakeImage(track, "BarFill", Vector2.zero, Vector2.zero, new Color(1f, 0.85f, 0.35f));
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(1f, 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        MakeButton(canvasRect, "ToggleButton", "Switch ON / OFF", new Vector2(0f, -35f), new Vector2(300f, 60f), new Color(0.20f, 0.45f, 0.35f));
        MakeButton(canvasRect, "DimButton", "Dim  -", new Vector2(-80f, -108f), new Vector2(140f, 60f), new Color(0.25f, 0.30f, 0.42f));
        MakeButton(canvasRect, "BrightButton", "Bright  +", new Vector2(80f, -108f), new Vector2(140f, 60f), new Color(0.50f, 0.40f, 0.15f));

        // a post and a base, so the panel is not floating in the air
        GameObject stand = new GameObject("LightPanelStand");
        stand.transform.SetPositionAndRotation(new Vector3(position.x, 0f, position.z), rotation);
        AddPart(stand.transform, "Post", new Vector3(0f, 0.5f, 0.04f), new Vector3(0.06f, 1.0f, 0.04f), wood, true);
        AddPart(stand.transform, "Base", new Vector3(0f, 0.015f, 0.04f), new Vector3(0.45f, 0.03f, 0.35f), wood, true); // solid: the participant cannot walk through the stand
    }

    private static bool InsideRoom(Bounds room, Vector3 p)
    {
        return p.x > room.min.x + 0.5f && p.x < room.max.x - 0.5f && p.z > room.min.z + 0.5f && p.z < room.max.z - 0.5f;
    }

    private static RectTransform MakeImage(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = go.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private static TextMeshProUGUI MakeText(RectTransform parent, string name, string text, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    // a button: an Image with a label, plus a 3D collider and a PointerTarget so the ray pointer can press it
    private static void MakeButton(RectTransform parent, string name, string text, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = MakeImage(parent, name, position, size, color);
        MakeText(rect, "Label", text, Vector2.zero, size, 30f, Color.white);

        BoxCollider collider = rect.gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(size.x, size.y, 30f); // canvas units: 30 units = 6 cm deep
        rect.gameObject.AddComponent<PointerTarget>();
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
