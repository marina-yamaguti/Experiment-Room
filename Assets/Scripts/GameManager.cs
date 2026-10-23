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
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int TotalRooms = 3;

    [Header("Scenes (must be in the Build Profiles scene list)")]
    [SerializeField] private string hubScene = "Hub";

    [Header("Session")]
    public int participantId = 1;

    private readonly HashSet<string> completedRooms = new HashSet<string>();

    public int CompletedCount => completedRooms.Count;
    public bool AllRoomsCompleted => completedRooms.Count >= TotalRooms;

    // Creates the manager automatically, whichever scene is played first.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
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
    }

    public bool IsRoomCompleted(string roomScene)
    {
        return completedRooms.Contains(roomScene);
    }

    /// <summary>Call this when a room's 4 trials are done, then LoadHub().</summary>
    public void MarkRoomCompleted(string roomScene)
    {
        if (completedRooms.Add(roomScene))
            Debug.Log("GameManager: " + roomScene + " completed (" + CompletedCount + "/" + TotalRooms + ").");
    }

    public void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("GameManager: scene '" + sceneName + "' is not in the Build Profiles scene list (or not created yet).");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    public void LoadHub()
    {
        LoadScene(hubScene);
    }

    /// <summary>Start a fresh session for the next participant.</summary>
    public void ResetSession()
    {
        completedRooms.Clear();
        LoadHub();
    }
}
