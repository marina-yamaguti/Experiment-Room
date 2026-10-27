using UnityEngine;

/// <summary>
/// Shared look of the floating text in the rooms: a black box at 70% opacity behind the text,
/// text in white or light cyan. Used by RoomHud and ConfidencePanel.
/// </summary>
public static class WorldTextBox
{
    public static readonly Color BoxColor = new Color(0f, 0f, 0f, 0.7f);
    public static readonly Color LightCyan = new Color32(0xB8, 0xF5, 0xFF, 255);

    /// <summary>
    /// A flat black box, "distanceBehind" metres behind the text as seen by the participant.
    /// flipFacing says which side the participant is on (same value as TrialManager.flipFacing).
    /// </summary>
    public static GameObject CreateBackground(Transform parent, bool flipFacing, float distanceBehind)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "TextBackground";
        Object.Destroy(quad.GetComponent<Collider>()); // must not block the ray pointer

        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = new Vector3(0f, 0f, flipFacing ? distanceBehind : -distanceBehind);

        Renderer r = quad.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.material = new Material(Shader.Find("Sprites/Default")) { color = BoxColor };

        return quad;
    }

    public static void Fit(GameObject background, Vector2 size)
    {
        background.transform.localScale = new Vector3(size.x, size.y, 1f);
    }
}
