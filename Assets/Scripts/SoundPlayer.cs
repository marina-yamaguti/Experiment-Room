using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the short sound effects (pickup, impact, page_flip) as 3D sounds at a position.
/// The clips are .wav files in Assets/Resources/Audio, found by file name, so there is nothing
/// to assign in the Inspector. To use another sound, replace the file and keep its name.
/// </summary>
public static class SoundPlayer
{
    private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    public static AudioClip Load(string clipName)
    {
        AudioClip clip;
        if (cache.TryGetValue(clipName, out clip) && clip != null) return clip;

        clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip == null) Debug.LogWarning("SoundPlayer: no clip 'Audio/" + clipName + "' in a Resources folder.");
        cache[clipName] = clip;
        return clip;
    }

    /// <param name="spatialBlend">1 = fully 3D (comes from the position), 0 = plain stereo.</param>
    public static void Play(string clipName, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
    {
        AudioClip clip = Load(clipName);
        if (clip == null) return;

        GameObject go = new GameObject("Sound_" + clipName);
        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = 15f;
        source.Play();

        Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
    }
}
