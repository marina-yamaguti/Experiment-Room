using System;
using UnityEngine;

/// <summary>
/// Controls the lighting of the whole scene: every Light in it plus the ambient light, scaled together.
///   - Toggle(): on / off (off is not pitch black, a faint 8% stays so the room is still visible)
///   - Brighter() / Dimmer(): 6 steps, from 25% to 150% (step 4 = 100% = the scene as it was made)
///   - SetDim(factor): an extra automatic dimming on top (the ending scene dims the room during the questionnaire)
/// Changes fade smoothly. LightPanel (the wall panel) is the participant's way to use it.
/// </summary>
public class RoomLight : MonoBehaviour
{
    public const int MinLevel = 1;
    public const int MaxLevel = 6;
    private const int DefaultLevel = 4;       // 4/4 = 100%
    private const float OffBrightness = 0.08f;
    private const float FadeSpeed = 2f;       // brightness change per second

    [Tooltip("Lights to control. Leave empty to use every Light in the scene.")]
    [SerializeField] private Light[] lights;

    public bool IsOn { get; private set; }
    public int Level { get; private set; }
    /// <summary>Current brightness of the scene lights, 1 = as built.</summary>
    public float Percent { get { return (IsOn ? Level / (float)DefaultLevel : OffBrightness) * dimFactor; } }
    public event Action Changed;

    private float[] baseIntensity;
    private float baseAmbient;
    private float baseReflection;
    private float dimFactor = 1f;
    private float current = 1f;

    private void Awake()
    {
        IsOn = true;
        Level = DefaultLevel;
    }

    private void Start()
    {
        if (lights == null || lights.Length == 0)
            lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        baseIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i].intensity;
        baseAmbient = RenderSettings.ambientIntensity;
        baseReflection = RenderSettings.reflectionIntensity;
    }

    private void Update()
    {
        float target = Percent;
        if (Mathf.Approximately(current, target)) return;

        current = Mathf.MoveTowards(current, target, FadeSpeed * Time.deltaTime);
        Apply(current);
    }

    private void OnDestroy()
    {
        if (baseIntensity != null) Apply(1f); // leave the scene as it was made
    }

    private void Apply(float multiplier)
    {
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity = baseIntensity[i] * multiplier;
        RenderSettings.ambientIntensity = baseAmbient * multiplier;
        RenderSettings.reflectionIntensity = baseReflection * multiplier;
    }

    public void Toggle() { IsOn = !IsOn; Notify(); }
    public void Brighter() { Level = Mathf.Min(MaxLevel, Level + 1); IsOn = true; Notify(); }
    public void Dimmer() { Level = Mathf.Max(MinLevel, Level - 1); IsOn = true; Notify(); }

    /// <summary>Automatic dimming on top of the participant's setting (1 = none, 0.45 = 45% of it).</summary>
    public void SetDim(float factor) { dimFactor = Mathf.Clamp01(factor); Notify(); }

    private void Notify()
    {
        if (Changed != null) Changed();
    }
}
