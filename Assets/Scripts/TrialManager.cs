using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs the 4 trials of one room. For every trial:
///   trigger to start -> study the table (countdown) -> grey flicker, the change is applied
///   unseen -> point at the object that changed -> rate confidence 0-5 -> log to CSV.
/// After the last trial the room is marked completed and the participant returns to the Hub.
///
/// SETUP: lives on the "Trial" object created by Tools > Room > Group experiment room parts,
/// next to ONE ChangeApplier (ColorChange in Room A). Set Room Id to this room's scene name.
/// The objects are found automatically (every RoomObject in the scene).
/// </summary>
public class TrialManager : MonoBehaviour
{
    [Header("Room")]
    [Tooltip("Scene name of this room: RoomA, RoomB or RoomC.")]
    [SerializeField] private string roomId = "RoomA";
    [SerializeField] private int trialsPerRoom = 4;

    [Header("Timing")]
    [SerializeField] private int studySeconds = 5;
    [SerializeField] private float flickerSeconds = 0.1f;

    [Header("Floating text")]
    [Tooltip("Tick this if the floating text looks mirrored.")]
    [SerializeField] private bool flipFacing = true;
    [Tooltip("Size of the instructions above the table (1 = a line about 11 cm tall). Raise it if they are hard to read.")]
    [SerializeField] private float instructionTextSize = 0.7f;
    [Tooltip("Multiplies the text size on the confidence panel (1 = default).")]
    [SerializeField] private float panelTextScale = 1f;

    private ChangeApplier applier;
    private RoomObject[] roomObjects;
    private GlideLocomotion locomotion;
    private Transform viewer;
    private RoomHud hud;
    private ConfidencePanel panel;
    private GreyFlicker flicker;

    private RoomObject selectedObject;
    private bool acceptingSelection;

    private void Start()
    {
        applier = GetComponent<ChangeApplier>();
        roomObjects = FindObjectsByType<RoomObject>(FindObjectsSortMode.None);

        if (applier == null || roomObjects.Length == 0)
        {
            Debug.LogError("TrialManager: needs a ChangeApplier on this object and RoomObjects in the scene.", this);
            return;
        }

        locomotion = FindFirstObjectByType<GlideLocomotion>();

        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        viewer = rig != null ? rig.centerEyeAnchor : Camera.main.transform;

        hud = gameObject.AddComponent<RoomHud>();
        hud.Init(roomObjects, viewer, flipFacing, instructionTextSize);

        panel = gameObject.AddComponent<ConfidencePanel>();
        panel.Init(viewer, flipFacing, panelTextScale);

        flicker = gameObject.AddComponent<GreyFlicker>();
        flicker.Init(viewer);

        foreach (RoomObject o in roomObjects)
        {
            RoomObject captured = o;
            o.Target.onSelected.AddListener(() => OnObjectSelected(captured));
        }

        StartCoroutine(RunRoom());
    }

    private void OnObjectSelected(RoomObject chosen)
    {
        if (!acceptingSelection) return; // ignore pointing outside the "which object changed?" phase
        selectedObject = chosen;
        acceptingSelection = false;
    }

    private IEnumerator RunRoom()
    {
        // random order, every object at most once per room
        List<RoomObject> order = new List<RoomObject>(roomObjects);
        Shuffle(order);
        int trials = Mathf.Min(trialsPerRoom, order.Count);

        hud.Show("Welcome\nStand here and look at the table");
        yield return new WaitForSeconds(2.5f);

        for (int i = 0; i < trials; i++)
        {
            RoomObject target = order[i];

            // 1. wait for the participant
            hud.Show("Trial " + (i + 1) + " of " + trials + "\nPress the trigger when you are ready");
            yield return WaitForTrigger();

            // 2. study phase with countdown
            SetMovement(false);
            for (int s = studySeconds; s > 0; s--)
            {
                hud.Show("Study the table\n" + s);
                yield return new WaitForSeconds(1f);
            }

            // 3. grey flicker; the change is applied while the view is covered
            yield return flicker.Play(flickerSeconds, () => applier.Apply(target));

            // 4. which object changed?
            hud.Show("Which object changed?\nPoint at it and press the trigger");
            selectedObject = null;
            acceptingSelection = true;
            float startTime = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => selectedObject != null);
            int reactionMs = Mathf.RoundToInt((Time.realtimeSinceStartup - startTime) * 1000f);
            RoomObject chosen = selectedObject;

            // 5. confidence 0-5
            hud.Show("");
            int confidence = -1;
            yield return panel.Ask(c => confidence = c);

            // 6. log, put the object back, unlock movement
            TrialLogger.Append(GameManager.Instance.participantId, roomId, i + 1, applier.ChangeType,
                               target.objectId, chosen.objectId, chosen == target, reactionMs, confidence);
            target.ResetToOriginal();
            SetMovement(true);
        }

        hud.Show("Thank you!\nReturning to the hub...");
        GameManager.Instance.MarkRoomCompleted(roomId);
        yield return new WaitForSeconds(3f);
        GameManager.Instance.LoadHub();
    }

    // waits for a fresh press of the right trigger (or A)
    private IEnumerator WaitForTrigger()
    {
        yield return null; // skip the frame of any press that is still being handled
        while (!(OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) || OVRInput.GetDown(OVRInput.Button.One)))
            yield return null;
    }

    private void SetMovement(bool allowed)
    {
        if (locomotion != null) locomotion.movementEnabled = allowed;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
