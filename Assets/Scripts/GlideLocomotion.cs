using UnityEngine;

/// <summary>
/// Gliding locomotion for the OVRCameraRig (brief requirement: "gliding/flying metaphor").
///
/// - Left thumbstick  : glide in the direction the head is facing (strafe + forward/back).
/// - Right thumbstick : snap-turn left/right (comfortable, avoids smooth-rotation sickness).
/// - If allowFlying is ON, the glide follows the head pitch too (flying); otherwise movement
///   stays horizontal so participants cannot leave the floor.
///
/// SETUP: put this on the root "[BuildingBlock] Camera Rig" object and drag
/// CenterEyeAnchor into the "head" field (it is found automatically if left empty).
/// The Meta Quest has thumbsticks, not a touchpad, so the thumbstick plays the touchpad role.
/// </summary>
public class GlideLocomotion : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The headset camera (CenterEyeAnchor). Auto-found if left empty.")]
    [SerializeField] private Transform head;

    [Header("Gliding")]
    [SerializeField] private float speed = 2f;          // metres per second
    [SerializeField] private float deadZone = 0.15f;    // ignore tiny stick drift
    [Tooltip("ON = flying (follows head pitch). OFF = gliding on the floor plane.")]
    [SerializeField] private bool allowFlying = false;

    [Header("Snap turn")]
    [SerializeField] private float snapAngle = 45f;
    [SerializeField] private float snapThreshold = 0.7f;

    [Header("Locking")]
    [Tooltip("Set false (e.g. during a trial) to freeze movement.")]
    public bool movementEnabled = true;

    private bool snapReady = true; // stick must return to centre before the next snap

    private void Awake()
    {
        if (head == null)
        {
            // OVRCameraRig names its head camera "CenterEyeAnchor"
            Transform found = transform.Find("TrackingSpace/CenterEyeAnchor");
            if (found != null) head = found;
        }
    }

    private void Update()
    {
        if (!movementEnabled || head == null) return;

        Glide();
        SnapTurn();
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
        transform.position += direction * speed * Time.deltaTime;
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
