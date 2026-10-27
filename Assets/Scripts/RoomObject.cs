using UnityEngine;

/// <summary>
/// One of the five objects on the table (Obj_Mug, Obj_Book...). Remembers how the object
/// started, so every trial can restore it. The ray pointer selects it through PointerTarget.
/// Added to the Obj_* groups by Tools > Room > Group experiment room parts.
/// </summary>
[RequireComponent(typeof(PointerTarget))]
public class RoomObject : MonoBehaviour
{
    [Tooltip("Name written to the CSV. Filled from the object name (Obj_Mug -> Mug) when left empty.")]
    public string objectId;

    public PointerTarget Target { get; private set; }
    public Renderer[] Renderers { get; private set; }

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private Material[][] startMaterials;

    private void Awake()
    {
        if (string.IsNullOrEmpty(objectId))
            objectId = name.StartsWith("Obj_") ? name.Substring(4) : name;

        Target = GetComponent<PointerTarget>();
        Renderers = GetComponentsInChildren<Renderer>(true);

        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;

        startMaterials = new Material[Renderers.Length][];
        for (int i = 0; i < Renderers.Length; i++)
            startMaterials[i] = Renderers[i].sharedMaterials;
    }

    /// <summary>Shows or hides the object but keeps its collider, so it stays selectable.</summary>
    public void SetVisible(bool visible)
    {
        foreach (Renderer r in Renderers) r.enabled = visible;
    }

    /// <summary>Puts position, rotation, colours and visibility back as they were at the start.</summary>
    public void ResetToOriginal()
    {
        transform.localPosition = startLocalPosition;
        transform.localRotation = startLocalRotation;

        for (int i = 0; i < Renderers.Length; i++)
            Renderers[i].sharedMaterials = startMaterials[i];

        SetVisible(true);
    }

    /// <summary>World-space box around all the given objects.</summary>
    public static Bounds CombinedBounds(RoomObject[] objects)
    {
        Bounds bounds = new Bounds(objects[0].transform.position, Vector3.zero);
        foreach (RoomObject o in objects)
            foreach (Renderer r in o.Renderers)
                bounds.Encapsulate(r.bounds);
        return bounds;
    }
}
