using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The light control panel: a world-space Canvas (Image + TextMeshPro UI elements) on a stand
/// next to the table. The ray pointer presses its buttons (each has a collider and a PointerTarget)
/// and RoomLight changes the lighting of the scene.
///
/// Built by Tools > End > Set up ending scene. This script finds its parts by name:
/// ToggleButton, DimButton, BrightButton, StatusLabel, BarFill.
/// </summary>
public class LightPanel : MonoBehaviour
{
    private RoomLight roomLight;
    private TMP_Text status;
    private RectTransform barFill;

    private void Start()
    {
        roomLight = FindFirstObjectByType<RoomLight>();
        if (roomLight == null)
        {
            Debug.LogError("LightPanel: no RoomLight in the scene (run Tools > End > Set up ending scene).", this);
            return;
        }

        status = Find("StatusLabel").GetComponent<TMP_Text>();
        barFill = Find("BarFill").GetComponent<RectTransform>();

        Wire("ToggleButton", roomLight.Toggle);
        Wire("DimButton", roomLight.Dimmer);
        Wire("BrightButton", roomLight.Brighter);

        roomLight.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (roomLight != null) roomLight.Changed -= Refresh;
    }

    private void Refresh()
    {
        status.text = roomLight.IsOn
            ? "Light ON  -  " + Mathf.RoundToInt(roomLight.Level / 4f * 100f) + "%"
            : "Light OFF";

        // the bar shows the chosen level (not the temporary automatic dimming)
        float fraction = roomLight.IsOn ? roomLight.Level / (float)RoomLight.MaxLevel : 0f;
        barFill.anchorMax = new Vector2(Mathf.Max(fraction, 0.001f), barFill.anchorMax.y);
    }

    // a button: lights up under the ray, runs the action when selected
    private void Wire(string buttonName, System.Action action)
    {
        Transform button = Find(buttonName);
        PointerTarget target = button.GetComponent<PointerTarget>();
        Image image = button.GetComponent<Image>();
        Color normal = image.color;
        Color hover = Color.Lerp(normal, Color.white, 0.35f);

        target.onHoverEnter.AddListener(() => image.color = hover);
        target.onHoverExit.AddListener(() => image.color = normal);
        target.onSelected.AddListener(() => action());
    }

    private Transform Find(string objectName)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == objectName) return t;

        Debug.LogError("LightPanel: part '" + objectName + "' not found.", this);
        return transform;
    }
}
