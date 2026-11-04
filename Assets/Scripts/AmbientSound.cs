using UnityEngine;

/// <summary>
/// Background music that loops for as long as the scene is open: a soft, slow elevator-music
/// loop (electric piano, bass, brushed hi-hat). Put it on any object in a scene (Hub, a room...).
/// EndingManager adds one by itself.
/// The sound is Assets/Resources/Audio/elevator_music.wav; to use another one, put a .wav in
/// that folder and type its name (without .wav) in Clip Name.
/// </summary>
public class AmbientSound : MonoBehaviour
{
    [Tooltip("File name (without .wav) in Assets/Resources/Audio.")]
    [SerializeField] private string clipName = "elevator_music";
    [SerializeField, Range(0f, 1f)] private float volume = 0.3f;
    [Tooltip("0 = same in both ears everywhere, 1 = comes from this object (3D).")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0f;

    private void Start()
    {
        AudioClip clip = SoundPlayer.Load(clipName);
        if (clip == null) return;

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 20f;
        source.Play();
    }
}
