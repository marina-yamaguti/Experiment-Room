

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Lives for the whole session (DontDestroyOnLoad). Keeps the participant ID and which rooms
/// are finished, and is the only place that loads scenes, so Hub and rooms stay decoupled.
///
/// NO SETUP NEEDED: it creates itself before the first scene loads, so a teammate can press
/// Play directly in RoomB or RoomC and GameManager.Instance still exists.
///
/// Scenes you load must be listed in File > Build Profiles > Scene List.
///  audio is automatically loaded (DoorSound and AmbientMusic)
/// </summary>

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int TotalRooms = 3;

    [Header("Scenes")]
    [SerializeField] private string hubScene = "Hub";
    [SerializeField] private string endScene = "End";

    [Header("Audio")]
    private AudioSource doorAudioSource;
    private AudioSource musicAudioSource;

    private AudioClip doorSound;
    private AudioClip ambientMusic;

    [Header("Session")]
    public int participantId = 1;

    private readonly HashSet<string> completedRooms = new HashSet<string>();

    public int CompletedCount => completedRooms.Count;
    public bool AllRoomsCompleted => completedRooms.Count >= TotalRooms;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Door sound
        doorSound = Resources.Load<AudioClip>("Audio/DoorSound");

        doorAudioSource = gameObject.AddComponent<AudioSource>();
        doorAudioSource.playOnAwake = false;
        doorAudioSource.spatialBlend = 0f;

        // Ambient music
        ambientMusic = Resources.Load<AudioClip>("Audio/AmbientMusic");

        musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = true;
        musicAudioSource.spatialBlend = 0f;
        musicAudioSource.volume = 0.12f;

        if (ambientMusic != null)
        {
            musicAudioSource.clip = ambientMusic;
            musicAudioSource.Play();
        }
        else
        {
            Debug.LogWarning(
                "GameManager: AmbientMusic not found in Resources/Audio."
            );
        }

        if (doorSound == null)
        {
            Debug.LogWarning(
                "GameManager: DoorSound not found in Resources/Audio."
            );
        }
    }

    public bool IsRoomCompleted(string roomScene)
    {
        return completedRooms.Contains(roomScene);
    }

    public void MarkRoomCompleted(string roomScene)
    {
        if (completedRooms.Add(roomScene))
        {
            Debug.Log(
                "GameManager: " + roomScene +
                " completed (" + CompletedCount + "/" + TotalRooms + ")."
            );
        }
    }

    public void LoadSceneWithSound(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning(
                "GameManager: scene '" + sceneName +
                "' is not in the Build Profiles scene list."
            );
            return;
        }

        StartCoroutine(LoadSceneWithSoundCoroutine(sceneName));
    }

    private IEnumerator LoadSceneWithSoundCoroutine(string sceneName)
    {
        if (doorAudioSource != null && doorSound != null)
        {
            doorAudioSource.PlayOneShot(doorSound);
            yield return new WaitForSecondsRealtime(doorSound.length);
        }

        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning(
                "GameManager: scene '" + sceneName +
                "' is not in the Build Profiles scene list."
            );
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Back to the Hub after a room. Once all three rooms are done, the next stop is the
    /// ending (book, pen and questionnaire) instead.
    /// </summary>
    public void LoadHub()
    {
        if (AllRoomsCompleted && Application.CanStreamedLevelBeLoaded(endScene))
        {
            LoadScene(endScene);
            return;
        }
        LoadScene(hubScene);
    }

    public void ResetSession()
    {
        completedRooms.Clear();
        TrialLogger.ClearSession();
        ExperimentTimer.Restart();
        LoadHub();
    }
}
