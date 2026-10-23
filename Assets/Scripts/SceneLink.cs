using UnityEngine;

/// <summary>
/// Loads a scene when the object is selected with the ray pointer.
/// Used on the Hub doors (target = RoomA / RoomB / RoomC) and on a "back to hub" button.
///
/// SETUP: needs a PointerTarget on the same object (added automatically) and a collider
/// on the object or its children.
/// </summary>
[RequireComponent(typeof(PointerTarget))]
public class SceneLink : MonoBehaviour
{
    [Tooltip("Exact scene name, e.g. RoomA, RoomB, RoomC or Hub.")]
    [SerializeField] private string targetScene = "RoomA";

    [Tooltip("Tick on Hub doors: a room that is already completed cannot be entered again.")]
    [SerializeField] private bool lockWhenCompleted = true;

    private PointerTarget target;

    private void Awake()
    {
        target = GetComponent<PointerTarget>();
        target.onSelected.AddListener(Go);
    }

    private void Start()
    {
        if (lockWhenCompleted && GameManager.Instance != null && GameManager.Instance.IsRoomCompleted(targetScene))
            target.interactable = false;
    }

    private void Go()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadScene(targetScene);
    }
}
