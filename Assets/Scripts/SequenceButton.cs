using UnityEngine;

/// <summary>
/// One of the 4 buttons of the sequence memory task.
/// Based on RemoteButton from XR Lab 2: pressing moves the button down and it slides back up
/// (Lerp in FixedUpdate). In VR there is no OnMouseDown/OnMouseUp, so the press comes from the
/// ray pointer (PointerTarget.onSelected) and the button releases itself after a short delay.
/// SETUP: on a button with a collider and a PointerTarget; drag the SequenceTask, the two
/// materials and the child Point Light into the fields.
/// </summary>
[RequireComponent(typeof(PointerTarget))]
public class SequenceButton : MonoBehaviour
{
    [Tooltip("Number of this button in the task (0 to 3).")]
    public int index;
    [SerializeField] private SequenceTask task;

    [SerializeField] private float pressDepth = 0.005f;
    [SerializeField] private float pressSpeed = 10f;
    [SerializeField] private float releaseDelay = 0.15f;
    [SerializeField] private float releaseTime = -1f;

    [SerializeField] private Material offMaterial;
    [SerializeField] private Material onMaterial;
    [SerializeField] private Light buttonLight;

    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Renderer rend;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        GetComponent<PointerTarget>().onSelected.AddListener(OnSelected);
    }

    private void Start()
    {
        // store the initial position of the button
        originalPosition = transform.localPosition;
        targetPosition = originalPosition;
        LightOff();
    }

    // smooth press/release movement
    private void FixedUpdate()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * pressSpeed);
    }

    private void OnSelected()
    {
        // clicks only count during the "your turn" phase
        if (task != null && task.AcceptingInput)
        {
            Press();
            task.ButtonPressed(index);
        }
    }

    // go down and light up
    private void Press()
    {
        targetPosition = originalPosition + Vector3.down * pressDepth;
        LightOn();
        releaseTime = Time.time + releaseDelay;
    }

    // come back up and turn light off
    private void Release()
    {
        targetPosition = originalPosition;
        LightOff();
    }

    public void LightOn()
    {
        rend.sharedMaterial = onMaterial;
        if (buttonLight != null) buttonLight.enabled = true;
    }

    public void LightOff()
    {
        rend.sharedMaterial = offMaterial;
        if (buttonLight != null) buttonLight.enabled = false;
    }

    private void Update()
    {
        if (releaseTime > 0f && Time.time >= releaseTime)
        {
            targetPosition = originalPosition;
            LightOff();
            releaseTime = -1f; 
        }
    }
}
