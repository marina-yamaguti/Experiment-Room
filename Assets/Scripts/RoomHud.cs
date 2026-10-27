using TMPro;
using UnityEngine;

/// <summary>
/// The text floating above the table: instructions, countdown, questions (3D TextMeshPro) on a
/// black box that fits the text. Created and driven by TrialManager. Always turns to face the
/// participant.
/// </summary>
public class RoomHud : MonoBehaviour
{
    private const float MaxWidth = 1.5f;                       // metres before the text wraps
    private static readonly Vector2 Padding = new Vector2(0.16f, 0.10f);

    private TextMeshPro text;
    private GameObject background;
    private Transform anchor;   // the part that turns towards the participant
    private Transform viewer;
    private bool flipFacing;

    public void Init(RoomObject[] objects, Transform viewerTransform, bool flip, float fontSize)
    {
        viewer = viewerTransform;
        flipFacing = flip;

        Bounds table = RoomObject.CombinedBounds(objects);

        anchor = new GameObject("HudAnchor").transform;
        anchor.SetParent(transform, false);
        anchor.position = new Vector3(table.center.x, table.max.y + 0.4f, table.center.z);

        background = WorldTextBox.CreateBackground(anchor, flip, 0.01f);

        GameObject go = new GameObject("HudText");
        go.transform.SetParent(anchor, false);
        text = go.AddComponent<TextMeshPro>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = WorldTextBox.LightCyan;

        Show("");
    }

    public void Show(string message)
    {
        if (text == null) return;

        text.text = message;
        bool empty = string.IsNullOrEmpty(message);
        background.SetActive(!empty);
        if (empty) return;

        // size the text area and the box to the message
        Vector2 size = text.GetPreferredValues(message, MaxWidth, Mathf.Infinity);
        text.rectTransform.sizeDelta = size;
        WorldTextBox.Fit(background, size + Padding);
    }

    private void LateUpdate()
    {
        if (anchor == null || viewer == null) return;

        Vector3 toViewer = viewer.position - anchor.position;
        toViewer.y = 0f; // turn around the vertical axis only, so the text never tilts
        if (toViewer.sqrMagnitude < 0.0001f) return;

        anchor.rotation = Quaternion.LookRotation(flipFacing ? -toViewer : toViewer);
    }
}
