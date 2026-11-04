using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Hub > Add door spotlights
/// Puts a small wall lamp above each Hub door (Door_A, Door_B, Door_C) with a spotlight pointing at the
/// door. DoorSpotlight turns it off once that room is completed. Run it in the Hub scene.
/// Safe to run again: it removes the lamps it made before.
/// </summary>
public static class AddDoorSpotlights
{
    private static readonly string[] OwnNames = { "DoorSpot", "DoorSpotFixture", "DoorSpotArm" };

    [MenuItem("Tools/Hub/Add door spotlights")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Door spotlights", "Stop Play mode first.", "OK");
            return;
        }

        SceneLink[] links = Object.FindObjectsByType<SceneLink>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (links.Length == 0)
        {
            EditorUtility.DisplayDialog("Door spotlights", "No doors (SceneLink) in this scene. Open the Hub scene.", "OK");
            return;
        }

        // the middle of the room, to know which side of a door is the inside
        Bounds floor = new Bounds();
        bool haveFloor = false;
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.gameObject.name.StartsWith("room_")) continue;
            if (!haveFloor) { floor = r.bounds; haveFloor = true; }
            else floor.Encapsulate(r.bounds);
        }
        if (!haveFloor)
        {
            EditorUtility.DisplayDialog("Door spotlights", "Could not find the floor (room_...) to tell which side of the doors is the inside.", "OK");
            return;
        }

        Material metal = MakeMaterial("DoorSpotMetal", new Color(0.12f, 0.12f, 0.13f));
        int added = 0;

        foreach (SceneLink link in links)
        {
            Transform door = link.transform;
            if (!door.name.StartsWith("Door_")) continue;

            // remove the lamp from an earlier run
            for (int i = door.childCount - 1; i >= 0; i--)
                foreach (string own in OwnNames)
                    if (door.GetChild(i).name == own) { Object.DestroyImmediate(door.GetChild(i).gameObject); break; }

            Renderer[] parts = door.GetComponentsInChildren<Renderer>();
            if (parts.Length == 0) continue;
            Bounds b = parts[0].bounds;
            foreach (Renderer r in parts) b.Encapsulate(r.bounds);

            // which way the door faces: towards the middle of the room, along the nearest axis
            Vector3 toRoom = floor.center - b.center;
            toRoom.y = 0f;
            Vector3 normal = Mathf.Abs(toRoom.x) > Mathf.Abs(toRoom.z)
                ? new Vector3(Mathf.Sign(toRoom.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(toRoom.z));

            // the lamp: above the door and its sign (60 cm over the door frame), 0.45 m out from the wall,
            // aimed at the middle of the door
            Vector3 wallPoint = new Vector3(b.center.x, b.max.y + 0.6f, b.center.z);
            Vector3 lampPosition = wallPoint + normal * 0.45f;
            Vector3 aim = new Vector3(b.center.x, b.min.y + b.size.y * 0.45f, b.center.z);
            Vector3 direction = (aim - lampPosition).normalized;

            // spotlight
            GameObject spot = new GameObject("DoorSpot");
            spot.transform.SetParent(door, true);
            spot.transform.position = lampPosition;
            spot.transform.rotation = Quaternion.LookRotation(direction);
            Light light = spot.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.93f, 0.8f);
            light.intensity = 5f;
            light.range = 3.5f;
            light.spotAngle = 70f;
            light.innerSpotAngle = 35f;
            light.shadows = LightShadows.None;   // cheaper, important on a Quest
            spot.AddComponent<DoorSpotlight>();

            // small lamp body, along the light direction
            GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fixture.name = "DoorSpotFixture";
            Object.DestroyImmediate(fixture.GetComponent<Collider>());   // never blocks the ray or the participant
            fixture.transform.SetParent(door, true);
            fixture.transform.position = lampPosition - direction * 0.02f;
            fixture.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
            fixture.transform.localScale = new Vector3(0.14f, 0.04f, 0.14f);
            fixture.GetComponent<Renderer>().sharedMaterial = metal;

            // arm holding it to the wall
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arm.name = "DoorSpotArm";
            Object.DestroyImmediate(arm.GetComponent<Collider>());
            arm.transform.SetParent(door, true);
            Vector3 armStart = wallPoint;
            Vector3 armEnd = lampPosition;
            arm.transform.position = (armStart + armEnd) / 2f;
            arm.transform.rotation = Quaternion.LookRotation(armEnd - armStart) * Quaternion.Euler(90f, 0f, 0f);
            arm.transform.localScale = new Vector3(0.02f, (armEnd - armStart).magnitude / 2f, 0.02f);
            arm.GetComponent<Renderer>().sharedMaterial = metal;

            added++;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("AddDoorSpotlights: added " + added + " door spotlight(s). Save the scene (Cmd+S).");
    }

    // materials must be real assets, otherwise they are lost when the scene is saved and reloaded
    private static Material MakeMaterial(string name, Color color)
    {
        string path = "Assets/Environment/Hub/" + name + ".mat";
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
