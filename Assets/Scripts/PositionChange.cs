using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Room B: the chosen object is shifted by a small distance (about 10 cm) in a direction
/// chosen per object, so that it always stays on the table (otherwise we would have to add physics, and the object falling would make the change too obvious). 
/// Each object has its own offset editable in the Inspector, in metres and in world space.
/// The shift is undone after each trial by RoomObject.ResetToOriginal().
/// </summary>
public class PositionChange : ChangeApplier
{
    [System.Serializable]
    public class Entry
    {
        public string objectId;
        [Tooltip("Shift in metres, in world space.")]
        public Vector3 offset;
    }

    // Used when an object has no entry in the list.
    [SerializeField]
    private Vector3 defaultOffset = new Vector3(0f, 0f, 0.10f);

    [SerializeField]
    private List<Entry> offsets = new List<Entry>
    {
        new Entry { objectId = "Mug",   offset = new Vector3(0f, 0f, 0.10f) },
        new Entry { objectId = "Book",  offset = new Vector3(0f, 0f, 0.10f) },
        new Entry { objectId = "Lamp",  offset = new Vector3(0f, 0f, 0.10f) },
        new Entry { objectId = "Vase",  offset = new Vector3(0f, 0f, 0.10f) },
        new Entry { objectId = "Frame", offset = new Vector3(0f, 0f, 0.10f) },
    };

    public override string ChangeType { get { return "Position"; } }

    public override void Apply(RoomObject target)
    {
        Vector3 offset = defaultOffset;
        bool found = false;
        foreach (Entry e in offsets)
        {
            if (e.objectId != target.objectId) continue;
            offset = e.offset;
            found = true;
            break;
        }
        if (!found)
            Debug.LogWarning("PositionChange: no offset listed for '" + target.objectId + "', using the default.", this);

        // Moving the root keeps its collider with it, so the object stays selectable at its new spot.
        target.transform.position += offset;
    }
}