using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper that places a sign (3D TextMeshPro) above a door and turns it to face into the room.
///
/// The Sweet Home 3D export has no usable pivot points, so instead of typing coordinates by
/// hand we read the door frame's bounds and place the sign from those.
///
/// SETUP: put this on the sign object (e.g. Sign_A), assign:
///   - Door Frame  : the frame_## object of that door (any renderer of the door works)
///   - Inside Room : something standing inside the room, e.g. the Camera Rig
/// Then right-click the component header > "Snap above door". Do this once per sign;
/// the result is saved in the scene.
/// </summary>
public class DoorSign : MonoBehaviour
{
    [Header("References")]
    [Tooltip("A renderer of the door, e.g. the frame_## object of that door.")]
    [SerializeField] private Renderer doorFrame;
    [Tooltip("Anything inside the room (the Camera Rig). The sign faces this side of the wall.")]
    [SerializeField] private Transform insideRoom;

    [Header("Placement")]
    [SerializeField] private float heightAboveDoor = 0.4f;   // metres above the top of the frame
    [SerializeField] private float distanceFromWall = 0.03f; // keeps the text from z-fighting with the wall
    [Tooltip("Tick this if the text shows mirrored, then run Snap above door again.")]
    [SerializeField] private bool flipFacing = false;

    [ContextMenu("Snap above door")]
    private void SnapAboveDoor()
    {
        if (doorFrame == null || insideRoom == null)
        {
            Debug.LogWarning("DoorSign: assign Door Frame and Inside Room first.", this);
            return;
        }

#if UNITY_EDITOR
        Undo.RecordObject(transform, "Snap sign above door");
#endif

        Bounds b = doorFrame.bounds;

        // The doors sit in a wall that runs along the X axis, so the wall normal is +Z or -Z.
        // "side" is +1 if the room is on the +Z side of the door, -1 otherwise.
        float side = Mathf.Sign(insideRoom.position.z - b.center.z);
        float wallZ = side > 0f ? b.max.z : b.min.z;

        transform.position = new Vector3(
            b.center.x,
            b.max.y + heightAboveDoor,
            wallZ + side * distanceFromWall);

        // Text reads correctly when seen from the side the sign faces away from.
        // If a sign shows mirrored text, tick "Flip Facing" and snap again.
        float facing = flipFacing ? -side : side;
        transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, facing), Vector3.up);
    }
}
