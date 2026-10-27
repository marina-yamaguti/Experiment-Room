using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// The "visual disruption" of the change-blindness study: for about 0.1 s the whole view turns
/// grey. A Screen Space Overlay canvas does not render in a headset, so this is a grey quad
/// parented to the head camera, 30 cm in front of the eyes.
/// Created and used by TrialManager; nothing to set up.
/// </summary>
public class GreyFlicker : MonoBehaviour
{
    private GameObject quad;

    public void Init(Transform head)
    {
        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "FlickerQuad";
        Destroy(quad.GetComponent<Collider>());

        quad.transform.SetParent(head, false);
        quad.transform.localPosition = new Vector3(0f, 0f, 0.3f);
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(2f, 2f, 1f); // large enough to cover the whole field of view

        Renderer r = quad.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.material = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.5f, 0.5f, 0.5f, 1f) };

        quad.SetActive(false);
    }

    /// <summary>
    /// Covers the view, waits two frames so the grey is really on screen, runs whileCovered
    /// (the change is applied there, unseen), keeps the grey for "seconds", then uncovers.
    /// </summary>
    public IEnumerator Play(float seconds, Action whileCovered)
    {
        quad.SetActive(true);
        yield return null;
        yield return null;

        if (whileCovered != null) whileCovered();

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds));
        quad.SetActive(false);
    }
}
