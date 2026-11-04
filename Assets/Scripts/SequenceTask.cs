using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sequence memory task used in every room. After the participant sees a sequence of buttons light up, 
/// then has to repeat it. There are 3 trials. Each trial is longer and faster than the one before.
/// After each trial the is a confidence rating..
/// During the sequence, there are some hidden changes that are not announced. 2 different random 
/// table objects change , one in trial 2 and one in trial 3.
/// </summary>
public class SequenceTask : MonoBehaviour
{
    [SerializeField] private string roomId = "Room";   
    [SerializeField] private ChangeApplier applier;     // the Trial object of this room
    [SerializeField] private SequenceButton[] buttons;
    [SerializeField] private Renderer plate;            // the plate under the buttons

    
    private readonly int[] sequenceLengths = { 3, 4, 5 };
    private readonly float[] flashDurations = { 0.7f, 0.55f, 0.4f };

    public bool AcceptingInput { get; private set; }

    private readonly List<int> sequence = new List<int>();
    private int nextInput;      // how many buttons the participant has clicked in this trial
    private bool trialOver;
    private bool trialSuccess;
    private RoomHud hud;

    private void Start()
    {
        StartCoroutine(RunTask());
    }

    private IEnumerator RunTask()
    {
        // floating text above the plate
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        Transform viewer = rig != null ? rig.centerEyeAnchor : Camera.main.transform;
        RoomObject[] roomObjects = FindObjectsByType<RoomObject>(FindObjectsSortMode.None);
        hud = gameObject.AddComponent<RoomHud>();
        hud.Init(plate.bounds, viewer, true, 0.7f); 

        ConfidencePanel panel = gameObject.AddComponent<ConfidencePanel>();
        panel.Init(viewer, true, 1f);

        // pick 2 different random objects
        int object1 = Random.Range(0, roomObjects.Length);
        int object2 = (object1 + Random.Range(1, roomObjects.Length)) % roomObjects.Length; 
        // one entry per trial
        RoomObject[] changeTargets = { null, roomObjects[object1], roomObjects[object2] }; // one entry per trial

        hud.Show("Walk to the table\nPress the trigger when ready");
        yield return WaitForTrigger();

        int score = 0;
        for (int t = 0; t < 3; t++)
        {
            // countdown
            for (int s = 3; s > 0; s--)
            {
                hud.Show("Trial " + (t + 1) + " of 3\nWatch carefully... " + s);
                yield return new WaitForSeconds(1f);
            }

            // new random sequence
            sequence.Clear();
            for (int i = 0; i < sequenceLengths[t]; i++)
                sequence.Add(Random.Range(0, buttons.Length));

            // the flash during which this trial's change happens
            int changeFlash = Random.Range(0, sequence.Count);

            // light the buttons one by one
            hud.Show("Watch");
            for (int i = 0; i < sequence.Count; i++)
            {
                buttons[sequence[i]].LightOn();
                // hidden change 
                if (changeTargets[t] != null && i == changeFlash)
                {
                    applier.Apply(changeTargets[t]); 
                    Debug.Log(roomId + ": " + changeTargets[t].objectId + " changed (" + applier.ChangeType + ") in trial " + (t + 1));
                }
                yield return new WaitForSeconds(flashDurations[t]);
                buttons[sequence[i]].LightOff();
                yield return new WaitForSeconds(0.2f);
            }

            // the participant clicks as many buttons as the sequence is long
            hud.Show("Your turn!\nRepeat the sequence");
            nextInput = 0;
            trialSuccess = true; 
            trialOver = false;
            float answerStart = Time.time; // measure how long the participant takes to answer
            AcceptingInput = true;
            yield return new WaitUntil(() => trialOver);
            int answerMs = Mathf.RoundToInt((Time.time - answerStart) * 1000f);

            if (trialSuccess) score++;

            // confidence after each trial
            hud.Show("How sure are you that you\nrepeated this sequence correctly?");
            yield return new WaitForSeconds(2f);
            hud.Show(""); // hide the text so it does not overlap the confidence panel
            int confidence = -1;
            yield return panel.Ask(c => confidence = c);

            // save the trial (used by the results page in the End scene)
            bool changed = changeTargets[t] != null;
            TrialLogger.Append(GameManager.Instance.participantId, roomId, t + 1,
                               changed ? applier.ChangeType : "None",
                               changed ? changeTargets[t].objectId : "none",
                               "sequence", trialSuccess, answerMs, confidence);
        }

        hud.Show("Done!\nReturning to the hub...");
        Debug.Log(roomId + ": score " + score + "/3");

        GameManager.Instance.MarkRoomCompleted(roomId);
        yield return new WaitForSeconds(3f);
        GameManager.Instance.LoadHub();
    }

    /// Called by a SequenceButton when it's clicked
    public void ButtonPressed(int index)
    {
        if (!AcceptingInput) return;

        if (index != sequence[nextInput]) trialSuccess = false;
        nextInput++;

        // the trial ends once the participant has clicked as many buttons as the sequence length
        if (nextInput == sequence.Count)
        {
            trialOver = true;
            AcceptingInput = false;
        }
    }

    // waits for a fresh press of the right trigger, same as TrialManager
    private IEnumerator WaitForTrigger()
    {
        yield return null;
        while (!(OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) || OVRInput.GetDown(OVRInput.Button.One)))
            yield return null;
    }
}