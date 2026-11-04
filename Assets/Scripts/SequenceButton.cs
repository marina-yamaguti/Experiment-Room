using UnityEngine;

/// <summary>
/// One of the buttons of the sequence memory task. Pressing moves the button down and it slides back up. 
/// It changes color and lights up when pressed.
/// </summary>
/// 
[RequireComponent(typeof(PointerTarget))]
public class SequenceButton : MonoBehaviour
{
    public int index;
    [SerializeField] private SequenceTask task;

    [SerializeField] private float pressDepth = 0.005f;
    [SerializeField] private float pressSpeed = 10f;
    [SerializeField] private float releaseDelay = 0.15f;

    [SerializeField] private Material offMaterial;
    [SerializeField] private Material onMaterial;
    [SerializeField] private Light buttonLight;

    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Renderer rend;
    private float releaseTime = -1f;

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
        // clicks only count when you have to repeat the sequence
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
            Release();
            releaseTime = -1f; 
        }
    }
}
