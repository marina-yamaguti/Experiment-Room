using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The spotlight above a Hub door. It is lit while the room behind the door is still to do and goes
/// out once the room is completed, so the participant can see which doors are left.
/// When the participant comes back to the Hub after finishing a room, that door's light fades out
/// (after a short delay) instead of simply being off.
///
/// SETUP: created by Tools > Hub > Add door spotlights, on the Light that is a child of Door_A / B / C.
/// The room is read from the SceneLink of the door.
/// </summary>
[RequireComponent(typeof(Light))]
public class DoorSpotlight : MonoBehaviour
{
    [Tooltip("Seconds after the Hub opens before a just-finished door's light starts to go out.")]
    [SerializeField] private float fadeDelay = 0.5f;
    [SerializeField] private float fadeSeconds = 1.5f;

    // rooms whose light has already been shown going out (so it is only animated once)
    private static readonly HashSet<string> shownOff = new HashSet<string>();

    private Light spot;
    private float fullIntensity;

    private void Start()
    {
        spot = GetComponent<Light>();
        fullIntensity = spot.intensity;

        SceneLink link = GetComponentInParent<SceneLink>();
        string room = link != null ? link.TargetScene : null;
        bool done = room != null && GameManager.Instance != null && GameManager.Instance.IsRoomCompleted(room);

        if (!done)
        {
            if (room != null) shownOff.Remove(room); // a new session: lit again
            return;
        }

        if (shownOff.Contains(room))
        {
            spot.enabled = false;           // finished earlier: simply off
            return;
        }

        shownOff.Add(room);
        StartCoroutine(FadeOut());          // just finished: let the participant see it go out
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(fadeDelay);
        for (float t = 0f; t < 1f; t += Time.deltaTime / fadeSeconds)
        {
            spot.intensity = Mathf.Lerp(fullIntensity, 0f, t);
            yield return null;
        }
        spot.enabled = false;
        spot.intensity = fullIntensity;
    }
}
