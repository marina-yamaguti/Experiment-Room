using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The ending of the experiment (scene "End", a copy of the Hub with locked doors):
///   pick up the book and the pen from the table -> paper questionnaire -> thank-you message.
/// Answers go to questionnaire.csv. The questions are editable in the Inspector (Questions list).
///
/// SETUP: created by Tools > End > Set up ending scene.
/// </summary>
public class EndingManager : MonoBehaviour
{
    [Header("Scene objects (filled by the setup tool)")]
    [SerializeField] private GrabbableProp book;
    [SerializeField] private GrabbableProp pen;
    [SerializeField] private Transform tableRoot;

    [Header("Floating text")]
    [Tooltip("Tick this if the floating text looks mirrored.")]
    [SerializeField] private bool flipFacing = true;
    [SerializeField] private float instructionTextSize = 0.7f;

    [Header("Questionnaire")]
    [Tooltip("Handwriting font (a .ttf imported into Unity, e.g. Assets/Fonts/PatrickHand-Regular.ttf). Empty = default font.")]
    [SerializeField] private Font handwritingFont;
    [Header("Questions (temporary, edit freely)")]
    [SerializeField] private List<QuizQuestion> questions = DefaultQuestions();

    private RoomHud hud;
    private PaperQuiz paper;
    private GlideLocomotion locomotion;
    private RoomLight roomLight;
    private Transform viewer;

    private static List<QuizQuestion> DefaultQuestions()
    {
        string[] rooms = { "Room A (colour changes)", "Room B (position changes)", "Room C (objects disappearing)", "No difference" };
        return new List<QuizQuestion>
        {
            new QuizQuestion { id = "easiest_room", text = "Which room did you find the EASIEST?", options = rooms },
            new QuizQuestion { id = "hardest_room", text = "Which room did you find the HARDEST?", options = rooms },
            new QuizQuestion
            {
                id = "easiest_change", text = "Which kind of change was the easiest to notice?",
                options = new[] { "A colour changing", "An object moving", "An object disappearing", "I did not notice any change" }
            },
            new QuizQuestion
            {
                id = "distraction", text = "How much did the button task distract you from the table?",
                options = new[] { "Not at all", "A little", "Quite a lot", "Completely" }
            },
            new QuizQuestion
            {
                id = "distraction_effect", text = "Did the distraction change how well you noticed the changes?",
                options = new[] { "Much harder to notice", "A little harder", "No difference", "Easier, I paid more attention" }
            },
        };
    }

    private void Start()
    {
        if (book == null || pen == null || tableRoot == null)
        {
            Debug.LogError("EndingManager: book, pen and table root must be set (run Tools > End > Set up ending scene).", this);
            return;
        }

        // the background music of the other scenes (played by GameManager) is silenced here,
        // the elevator music takes its place
        MuteBackgroundMusic();
        if (FindFirstObjectByType<AmbientSound>() == null) gameObject.AddComponent<AmbientSound>();

        locomotion = FindFirstObjectByType<GlideLocomotion>();
        roomLight = FindFirstObjectByType<RoomLight>();
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        viewer = rig != null ? rig.centerEyeAnchor : Camera.main.transform;

        Bounds tableBounds = new Bounds(tableRoot.position, Vector3.zero);
        foreach (Renderer r in tableRoot.GetComponentsInChildren<Renderer>())
            tableBounds.Encapsulate(r.bounds);

        hud = gameObject.AddComponent<RoomHud>();
        hud.Init(tableBounds, viewer, flipFacing, instructionTextSize);

        paper = gameObject.AddComponent<PaperQuiz>();
        paper.Init(viewer, flipFacing, LoadHandwritingFont());

        StartCoroutine(RunEnding());
    }

    // In the editor the font is found by itself if the field was left empty; in a build, assign it in the Inspector.
    private Font LoadHandwritingFont()
    {
#if UNITY_EDITOR
        if (handwritingFont == null)
        {
            // prefers Devonshire; otherwise the first font found in Assets/Fonts
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Font", new[] { "Assets/Fonts" }))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Font found = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(path);
                if (handwritingFont == null || path.Contains("Devonshire")) handwritingFont = found;
            }
        }
#endif
        return handwritingFont;
    }

    private readonly List<AudioSource> mutedMusic = new List<AudioSource>();

    private void MuteBackgroundMusic()
    {
        if (GameManager.Instance == null) return;
        foreach (AudioSource source in GameManager.Instance.GetComponents<AudioSource>())
        {
            if (!source.loop || source.mute) continue;
            source.mute = true;
            mutedMusic.Add(source);
        }
    }

    private void OnDestroy()
    {
        // give the music back when leaving this scene (GameManager outlives it)
        foreach (AudioSource source in mutedMusic)
            if (source != null) source.mute = false;
    }

    private IEnumerator RunEnding()
    {
        hud.Show("All three rooms are done!\nPick up the book and the pen\non the table");

        string last = "";
        while (!(book.HasBeenPickedUp && pen.HasBeenPickedUp))
        {
            string message = "All three rooms are done!\nPick up the book and the pen\non the table";
            if (book.HasBeenPickedUp) message = "Good! Now pick up the pen";
            else if (pen.HasBeenPickedUp) message = "Good! Now pick up the book";

            if (message != last) { hud.Show(message); last = message; }
            yield return null;
        }

        hud.Show("Thank you!\nA few last questions...");
        yield return new WaitForSeconds(1.5f);

        // put both objects back on the table and keep them out of the way during the questionnaire
        foreach (GrabbableProp prop in new[] { book, pen })
        {
            prop.PutBack();
            prop.SetInteractable(false);
        }
        hud.Show("");
        if (locomotion != null) locomotion.movementEnabled = false;

        int participant = GameManager.Instance != null ? GameManager.Instance.participantId : 0;

        // the room dims while the participant answers, and comes back for the results
        if (roomLight != null) roomLight.SetDim(0.45f);
        yield return paper.Run(questions, (q, answerIndex) =>
        {
            QuestionnaireLogger.Append(participant, q.id, q.text, answerIndex, q.options[answerIndex]);
        });
        if (roomLight != null) roomLight.SetDim(1f);

        // results page, then the participant turns the page: "The End". Movement stays locked, it is over.
        yield return paper.ShowResults(ResultsSummary.Compute(participant));
        yield return paper.TurnToEnd();
    }
}
