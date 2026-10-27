using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Room A: the chosen object changes colour. Each object has its own target colour, picked so
/// that no object ever ends up with the same colour as another one on the table.
/// The values are editable in the Inspector (list "New Colors").
/// </summary>
public class ColorChange : ChangeApplier
{
    [System.Serializable]
    public class Entry
    {
        public string objectId;
        public Color color;
    }

    [SerializeField]
    private List<Entry> newColors = new List<Entry>
    {
        new Entry { objectId = "Mug",   color = new Color32(0x00, 0xA3, 0x9A, 255) }, // orange -> teal
        new Entry { objectId = "Book",  color = new Color32(0xD7, 0x26, 0x3D, 255) }, // azure  -> red
        new Entry { objectId = "Lamp",  color = new Color32(0xF2, 0xC5, 0x00, 255) }, // purple -> yellow
        new Entry { objectId = "Vase",  color = new Color32(0x7B, 0x4B, 0x2A, 255) }, // green  -> brown
        new Entry { objectId = "Frame", color = new Color32(0x33, 0x33, 0x33, 255) }, // pink   -> charcoal
    };

    public override string ChangeType { get { return "Color"; } }

    public override void Apply(RoomObject target)
    {
        Color color = Color.magenta; // visible fallback if an object has no entry
        bool found = false;
        foreach (Entry e in newColors)
        {
            if (e.objectId != target.objectId) continue;
            color = e.color;
            found = true;
            break;
        }
        if (!found) Debug.LogWarning("ColorChange: no colour listed for '" + target.objectId + "'.", this);

        foreach (Renderer r in target.Renderers)
        {
            // r.materials gives this renderer its own copies, so other objects are not affected
            foreach (Material m in r.materials)
            {
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color); // URP Lit
                if (m.HasProperty("_Color")) m.SetColor("_Color", color);         // built-in shaders
            }
        }
    }
}
