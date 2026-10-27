using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// The 0-5 confidence rating: six large buttons on a black box floating in front of the
/// participant, selected with the ray pointer (each button is a PointerTarget).
/// Built in code, so nothing to set up.
///
/// Size rule of thumb for 3D TextMeshPro: font size 1 is a text line about 11 cm tall
/// and about 5 cm per character.
/// </summary>
public class ConfidencePanel : MonoBehaviour
{
    private static readonly Color IdleColor = new Color(0.20f, 0.24f, 0.30f);
    private static readonly Color HoverColor = new Color(0.15f, 0.65f, 0.45f);

    private const float ButtonSize = 0.18f;    // large targets are easier to point at
    private const float ButtonSpacing = 0.22f;

    private Transform root;
    private Transform viewer;
    private bool flipFacing;
    private int choice = -1;

    public void Init(Transform viewerTransform, bool flip, float textScale)
    {
        viewer = viewerTransform;
        flipFacing = flip;
        float front = flip ? -1f : 1f; // the side of a button that faces the participant

        root = new GameObject("ConfidencePanel").transform;
        root.SetParent(transform, false);

        // black box behind everything
        GameObject box = WorldTextBox.CreateBackground(root, flip, 0.05f);
        WorldTextBox.Fit(box, new Vector2(1.5f, 0.76f));

        AddLabel("How confident are you?", new Vector3(0f, 0.25f, 0f), 0.5f * textScale,
                 WorldTextBox.LightCyan, TextAlignmentOptions.Center, new Vector2(1.4f, 0.15f));
        AddLabel("0 = pure guess", new Vector3(-0.35f, -0.25f, 0f), 0.35f * textScale,
                 WorldTextBox.LightCyan, TextAlignmentOptions.Left, new Vector2(0.7f, 0.1f));
        AddLabel("5 = completely sure", new Vector3(0.35f, -0.25f, 0f), 0.35f * textScale,
                 WorldTextBox.LightCyan, TextAlignmentOptions.Right, new Vector2(0.7f, 0.1f));

        for (int i = 0; i <= 5; i++)
        {
            int value = i; // own copy for the click handler
            float x = (i - 2.5f) * ButtonSpacing;

            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube); // comes with a BoxCollider
            button.name = "Confidence_" + i;
            button.transform.SetParent(root, false);
            button.transform.localPosition = new Vector3(x, 0f, 0f);
            button.transform.localScale = new Vector3(ButtonSize, ButtonSize, 0.04f);

            Renderer r = button.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.material.color = IdleColor;

            PointerTarget target = button.AddComponent<PointerTarget>();
            target.onHoverEnter.AddListener(() => r.material.color = HoverColor);
            target.onHoverExit.AddListener(() => r.material.color = IdleColor);
            target.onSelected.AddListener(() => choice = value);

            AddLabel(i.ToString(), new Vector3(x, 0f, 0.03f * front), 0.9f * textScale, Color.white,
                     TextAlignmentOptions.Center, new Vector2(ButtonSize, ButtonSize));
        }

        root.gameObject.SetActive(false);
    }

    /// <summary>Shows the panel, waits for a button to be selected, hides it, returns 0-5.</summary>
    public IEnumerator Ask(Action<int> done)
    {
        Place();
        choice = -1;
        root.gameObject.SetActive(true);

        yield return null; // so the press that selected the object cannot also select a button
        yield return new WaitUntil(() => choice >= 0);

        root.gameObject.SetActive(false);
        done(choice);
    }

    private void AddLabel(string message, Vector3 localPosition, float size, Color color,
                          TextAlignmentOptions alignment, Vector2 area)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(root, false);
        go.transform.localPosition = localPosition;

        TextMeshPro label = go.AddComponent<TextMeshPro>();
        label.text = message;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.rectTransform.sizeDelta = area;
    }

    // 1 m in front of where the participant is looking, a little below eye level
    private void Place()
    {
        Vector3 forward = viewer.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 position = viewer.position + forward * 1.0f;
        position.y = viewer.position.y - 0.3f;
        root.position = position;

        Vector3 toViewer = viewer.position - position;
        toViewer.y = 0f;
        root.rotation = Quaternion.LookRotation(flipFacing ? -toViewer : toViewer);
    }
}
