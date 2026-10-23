using UnityEngine;

/// <summary>
/// Ray-cast pointer from a controller. Draws a line, finds the PointerTarget under it
/// and selects it when the trigger is pressed (brief: "ray-cast manipulation").
///
/// SETUP: add to the right controller anchor:
///   [BuildingBlock] Camera Rig > TrackingSpace > RightHandAnchor
/// Nothing to assign. Right index trigger selects (also what the simulator's click sends).
/// </summary>
public class RayPointer : MonoBehaviour
{
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private OVRInput.Button selectButton = OVRInput.Button.SecondaryIndexTrigger; // right trigger
    [SerializeField] private float lineWidth = 0.004f;
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color hoverColor = new Color(0.2f, 1f, 0.4f, 1f);
    [Tooltip("Prints what the ray hits and which buttons are pressed. Turn off once everything works.")]
    [SerializeField] private bool debugLog = true;

    private LineRenderer line;
    private PointerTarget current;

    private void Awake()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.widthMultiplier = lineWidth;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = new Material(Shader.Find("Sprites/Default"));
    }

    private void Update()
    {
        Vector3 origin = transform.position;
        Vector3 end = origin + transform.forward * maxDistance;
        PointerTarget target = null;

        RaycastHit hit;
        bool hitSomething = Physics.Raycast(origin, transform.forward, out hit, maxDistance, ~0, QueryTriggerInteraction.Ignore);
        if (hitSomething)
        {
            end = hit.point;
            target = hit.collider.GetComponentInParent<PointerTarget>();
            if (target != null && !target.interactable) target = null;
        }

        if (debugLog && Time.frameCount % 60 == 0)
            Debug.Log("RayPointer: origin " + origin.ToString("F2") + ", forward " + transform.forward.ToString("F2") + ", hit " + (hitSomething ? hit.collider.name : "nothing") + ", target " + (target != null ? target.name : "none"));

        if (target != current)
        {
            if (current != null) current.HoverExit();
            current = target;
            if (current != null) current.HoverEnter();
        }

        if (debugLog)
        {
            if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) Debug.Log("RayPointer: right trigger pressed (target " + (current != null ? current.name : "none") + ")");
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) Debug.Log("RayPointer: LEFT trigger pressed");
            if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger)) Debug.Log("RayPointer: right grip pressed");
            if (OVRInput.GetDown(OVRInput.Button.One)) Debug.Log("RayPointer: button A pressed");
        }

        // Right trigger selects; the A button works too (handy when a simulator maps only one of them).
        bool pressed = OVRInput.GetDown(selectButton) || OVRInput.GetDown(OVRInput.Button.One);
        if (current != null && pressed)
        {
            if (debugLog) Debug.Log("RayPointer: selecting " + current.name);
            current.Select();
        }

        Color c = current != null ? hoverColor : idleColor;
        line.startColor = c;
        line.endColor = c;
        line.SetPosition(0, origin);
        line.SetPosition(1, end);
    }
}
