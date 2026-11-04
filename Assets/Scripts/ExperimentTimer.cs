using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The experiment countdown: a small timer fixed to the bottom right of the participant's view, in every scene.
/// "Experiment duration" label on top, mm:ss below. It starts when the experiment starts (the first scene
/// that loads), keeps running through all the scenes, turns amber in the last minute and red at 00:00.
/// It only shows the time: nothing happens when it reaches zero.
///
/// NO SETUP NEEDED: it creates itself in every scene (a world-space Canvas fixed to the head camera).
/// To change the length of the experiment, set ExperimentTimer.DurationSeconds (default 5 minutes).
/// GameManager.ResetSession() restarts it; the ending scene stops it at "The End".
/// </summary>
public static class ExperimentTimer
{
    /// <summary>Length of the experiment, in seconds.</summary>
    public static float DurationSeconds = 5f * 60f;

    private static float startTime = -1f;
    private static float stoppedRemaining = -1f;
    private static float stoppedElapsed = -1f;

    /// <summary>Seconds since the experiment started (frozen once Stop() is called). The results page shows it.</summary>
    public static float ElapsedSeconds
    {
        get
        {
            if (stoppedElapsed >= 0f) return stoppedElapsed;
            if (startTime < 0f) return 0f;
            return Time.realtimeSinceStartup - startTime;
        }
    }

    public static float Remaining
    {
        get
        {
            if (stoppedRemaining >= 0f) return stoppedRemaining;
            if (startTime < 0f) return DurationSeconds;
            return Mathf.Max(0f, DurationSeconds - (Time.realtimeSinceStartup - startTime));
        }
    }

    public static void Restart()
    {
        startTime = Time.realtimeSinceStartup;
        stoppedRemaining = -1f;
        stoppedElapsed = -1f;
    }

    /// <summary>Freezes the timer at the time that is left.</summary>
    public static void Stop()
    {
        if (stoppedRemaining >= 0f) return;
        stoppedRemaining = Remaining;
        stoppedElapsed = ElapsedSeconds;
    }

    // statics survive between Play sessions when the editor does not reload the domain
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        startTime = -1f;
        stoppedRemaining = -1f;
        stoppedElapsed = -1f;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Begin()
    {
        if (startTime < 0f) Restart();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        BuildHud();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuildHud();
    }

    // the timer is rebuilt in every scene, as a child of that scene's head camera
    private static void BuildHud()
    {
        if (Object.FindFirstObjectByType<TimerHud>() != null) return;

        OVRCameraRig rig = Object.FindFirstObjectByType<OVRCameraRig>();
        Transform eye = rig != null ? rig.centerEyeAnchor : (Camera.main != null ? Camera.main.transform : null);
        if (eye == null) return;

        // 220 x 90 units at 0.0008 m per unit = 17.6 x 7.2 cm, 0.9 m in front, towards the bottom right
        GameObject root = new GameObject("TimerHud", typeof(RectTransform));
        root.transform.SetParent(eye, false);
        // at the very bottom of the view: 90 % of the way down to the lower edge of the camera's field of view
        Camera cam = eye.GetComponent<Camera>();
        float halfFov = (cam != null && cam.fieldOfView > 20f ? cam.fieldOfView : 90f) * 0.5f * Mathf.Deg2Rad;
        float bottom = -Mathf.Tan(halfFov) * 0.9f * 0.9f;
        root.transform.localPosition = new Vector3(0.4f, bottom, 0.9f);
        root.transform.localRotation = Quaternion.identity;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220f, 90f);
        rect.localScale = Vector3.one * 0.0008f;

        // dark, see-through background, drawn on top of the scene (never hidden behind a wall or an object)
        GameObject back = new GameObject("Background", typeof(RectTransform));
        back.transform.SetParent(root.transform, false);
        RectTransform backRect = back.GetComponent<RectTransform>();
        backRect.sizeDelta = new Vector2(220f, 90f);
        Image image = back.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.45f);
        image.raycastTarget = false;
        image.material = new Material(Canvas.GetDefaultCanvasMaterial());
        AlwaysOnTop(image.material, 3999);

        TextMeshProUGUI label = MakeText(root.transform, "Label", "Experiment duration", new Vector2(0f, 24f),
                                         new Vector2(210f, 28f), 15f, new Color32(0xB8, 0xF5, 0xFF, 255));
        TextMeshProUGUI time = MakeText(root.transform, "Time", "05:00", new Vector2(0f, -14f),
                                        new Vector2(210f, 50f), 34f, Color.white);

        TimerHud hud = root.AddComponent<TimerHud>();
        hud.time = time;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name, string text, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        AlwaysOnTop(label.fontMaterial, 4000);
        return label;
    }

    // UI materials depth-test by default, so a wall in front would hide the timer: draw it regardless of depth, last
    private static void AlwaysOnTop(Material material, int renderQueue)
    {
        material.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
        material.renderQueue = renderQueue;
    }
}

/// <summary>Updates the timer text every frame (created by ExperimentTimer).</summary>
public class TimerHud : MonoBehaviour
{
    [HideInInspector] public TMP_Text time;

    private int lastSeconds = -1;

    private void Update()
    {
        if (time == null) return;

        float remaining = ExperimentTimer.Remaining;
        int seconds = Mathf.CeilToInt(remaining);
        if (seconds == lastSeconds) return;
        lastSeconds = seconds;

        time.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        time.color = seconds <= 0 ? new Color(1f, 0.35f, 0.30f)
                   : seconds <= 60 ? new Color(1f, 0.78f, 0.30f)
                   : Color.white;
    }
}
