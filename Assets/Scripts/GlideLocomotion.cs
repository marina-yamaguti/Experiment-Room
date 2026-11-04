using UnityEngine;

public class GlideLocomotion : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The headset camera (CenterEyeAnchor). Auto-found if left empty.")]
    [SerializeField] private Transform head;

    [Header("Gliding")]
    [SerializeField] private float speed = 2f;   
    [SerializeField] private float deadZone = 0.15f;    
    [Tooltip("ON = flying (follows head pitch). OFF = gliding on the floor plane.")]
    [SerializeField] private bool allowFlying = false;

    [Header("Snap turn")]
    [SerializeField] private float snapAngle = 45f;
    [SerializeField] private float snapThreshold = 0.7f;

    [Header("Locking")]
    [Tooltip("Set false (e.g. during a trial) to freeze movement.")]
    public bool movementEnabled = true;

    [Header("Staying inside the room")]
    [Tooltip("Keeps the participant inside the room walls (found by their name, wall_...) and stops them walking through tables and doors.")]
    [SerializeField] private bool keepInsideRoom = true;
    [Tooltip("Wall thickness in metres (Sweet Home 3D default is 7.5 cm).")]
    [SerializeField] private float wallThickness = 0.075f;
    [Tooltip("How close the head may get to a wall or to furniture, in metres.")]
    [SerializeField] private float bodyRadius = 0.35f;

    private bool snapReady = true; // stick must return to centre before the next snap
    private bool hasLimits;
    private Vector2 limitMin, limitMax;   // allowed area for the head (x, z) inside the walls

    private void Awake()
    {
        if (head == null)
        {
            // OVRCameraRig names its head camera "CenterEyeAnchor"
            Transform found = transform.Find("TrackingSpace/CenterEyeAnchor");
            if (found != null) head = found;
        }
    }

    private void Start()
    {
        if (!keepInsideRoom) return;

        // the walls have no colliders, so the room is measured from the wall meshes
        bool found = false;
        Bounds walls = new Bounds();
        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.gameObject.name.StartsWith("wall_")) continue;
            if (!found) { walls = r.bounds; found = true; }
            else walls.Encapsulate(r.bounds);
        }
        if (!found) return;

        float shrink = wallThickness + bodyRadius;
        limitMin = new Vector2(walls.min.x + shrink, walls.min.z + shrink);
        limitMax = new Vector2(walls.max.x - shrink, walls.max.z - shrink);
        hasLimits = limitMax.x > limitMin.x && limitMax.y > limitMin.y;
    }

    private void Update()
    {
        if (!movementEnabled || head == null) return;

        Glide();
        SnapTurn();
        KeepInsideWalls();
    }

    // pushes the rig back so the head stays inside the walls
    private void KeepInsideWalls()
    {
        if (!hasLimits) return;

        Vector3 p = head.position;
        float x = Mathf.Clamp(p.x, limitMin.x, limitMax.x);
        float z = Mathf.Clamp(p.z, limitMin.y, limitMax.y);
        if (x != p.x || z != p.z) transform.position += new Vector3(x - p.x, 0f, z - p.z);
    }

    private bool BlockedAt(Vector3 headPosition)
    {
        float floor = transform.position.y;
        float radius = bodyRadius * 0.7f;
        Vector3 low = new Vector3(headPosition.x, floor + 0.35f, headPosition.z);
        Vector3 high = new Vector3(headPosition.x, floor + 1.5f, headPosition.z);
        return Physics.CheckCapsule(low, high, radius, ~(1 << 2), QueryTriggerInteraction.Ignore);
    }

    // the move that is allowed: all of it, or only the part along a wall / table edge (sliding), or none
    private Vector3 AllowedMove(Vector3 move)
    {
        if (!keepInsideRoom || BlockedAt(head.position)) return move;
        if (!BlockedAt(head.position + move)) return move;

        Vector3 alongX = new Vector3(move.x, 0f, 0f);
        Vector3 alongZ = new Vector3(0f, 0f, move.z);
        if (!BlockedAt(head.position + alongX)) return alongX;
        if (!BlockedAt(head.position + alongZ)) return alongZ;
        return Vector3.zero;
    }

    private void Glide()
    {
        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick); // left stick
        if (stick.magnitude < deadZone) return;

        Vector3 forward = head.forward;
        Vector3 right = head.right;

        if (!allowFlying)
        {
            // flatten onto the floor plane so looking down does not push you into the ground
            forward.y = 0f;
            right.y = 0f;
        }

        forward.Normalize();
        right.Normalize();

        Vector3 direction = forward * stick.y + right * stick.x;
        transform.position += AllowedMove(direction * speed * Time.deltaTime);
    }

    private void SnapTurn()
    {
        float x = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x; // right stick

        if (snapReady && Mathf.Abs(x) > snapThreshold)
        {
            // rotate around the head so the participant turns on the spot
            transform.RotateAround(head.position, Vector3.up, Mathf.Sign(x) * snapAngle);
            snapReady = false;
        }
        else if (Mathf.Abs(x) < 0.3f)
        {
            snapReady = true;
        }
    }
}
