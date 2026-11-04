using System;
using UnityEngine;

/// <summary>
/// An object the participant can pick up with the ray pointer and put down again.
///   - point at it and press the trigger: it jumps into the hand (held in front of the controller)
///   - press the trigger again: it is released and falls / is thrown (real physics)
/// Only one object can be held at a time. If it falls out of the room it goes back to where it started.
///
/// SETUP: created by Tools > End > Set up ending scene (Rigidbody, collider and PointerTarget included).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PointerTarget))]
public class GrabbableProp : MonoBehaviour
{
    [Tooltip("How far in front of the controller the object is held, in metres.")]
    [SerializeField] private float holdDistance = 0.45f;
    [Tooltip("Rotation of the object relative to the controller while held.")]
    [SerializeField] private Vector3 holdEulerAngles = Vector3.zero;
    [SerializeField] private float maxThrowSpeed = 4f;

    public bool HasBeenPickedUp { get; private set; }
    public bool IsHeld { get { return held == this; } }

    private static GrabbableProp held;
    private static int lastDropFrame = -1;

    private Rigidbody body;
    private PointerTarget target;
    private Transform hand;
    private Transform originalParent;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Transform[] allTransforms;
    private int[] originalLayers;
    private int pickFrame = -1;
    private Vector3 lastHandPosition;
    private Vector3 handVelocity;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // small objects must not tunnel through the table
        target = GetComponent<PointerTarget>();
        target.onSelected.AddListener(OnSelected);

        originalParent = transform.parent;
        startPosition = transform.position;
        startRotation = transform.rotation;

        allTransforms = GetComponentsInChildren<Transform>(true);
        originalLayers = new int[allTransforms.Length];
        for (int i = 0; i < allTransforms.Length; i++) originalLayers[i] = allTransforms[i].gameObject.layer;
    }

    private void Start()
    {
        RayPointer pointer = FindFirstObjectByType<RayPointer>();
        if (pointer != null) hand = pointer.transform;
        else Debug.LogError("GrabbableProp: no RayPointer in the scene, the object cannot be picked up.", this);
    }

    private void OnSelected()
    {
        if (hand == null || held != null) return;           // already holding something
        if (Time.frameCount == lastDropFrame) return;       // the same press just dropped another object
        PickUp();
    }

    private void PickUp()
    {
        SoundPlayer.Play("pickup", transform.position, 0.9f, SoundPitch);
        held = this;
        HasBeenPickedUp = true;
        pickFrame = Time.frameCount;

        body.isKinematic = true;
        SetLayer(2); // Ignore Raycast: the pointer ray passes through the held object

        transform.SetParent(hand, false);
        transform.localPosition = new Vector3(0f, 0f, holdDistance);
        transform.localRotation = Quaternion.Euler(holdEulerAngles);

        lastHandPosition = hand.position;
        handVelocity = Vector3.zero;
    }

    // small light objects (the pen) sound higher than heavy ones (the book)
    private float SoundPitch { get { return body.mass < 0.1f ? 1.5f : 1f; } }

    private float lastImpactTime;

    // a knock when the object lands on the table or the floor (not for the first moments, while it settles)
    private void OnCollisionEnter(Collision collision)
    {
        if (Time.timeSinceLevelLoad < 1.5f || Time.time - lastImpactTime < 0.15f) return;

        float speed = collision.relativeVelocity.magnitude;
        if (speed < 0.5f) return;

        lastImpactTime = Time.time;
        SoundPlayer.Play("impact", transform.position, Mathf.Clamp(speed / 3f, 0.2f, 1f), SoundPitch);
    }

    private void Drop()
    {
        held = null;
        lastDropFrame = Time.frameCount;

        transform.SetParent(originalParent, true);
        SetLayer(-1);
        body.isKinematic = false;
        body.linearVelocity = Vector3.ClampMagnitude(handVelocity, maxThrowSpeed);
    }

    /// <summary>Puts the object down if it is in the hand (used before the questionnaire).</summary>
    public void ReleaseIfHeld()
    {
        if (held == this) Drop();
    }

    /// <summary>Puts the object back where it started on the table (used before the questionnaire).</summary>
    public void PutBack()
    {
        ResetToStart();
    }

    /// <summary>Turns the pointer on or off for this object.</summary>
    public void SetInteractable(bool value)
    {
        target.interactable = value;
    }

    private void Update()
    {
        if (held == this)
        {
            Vector3 v = (hand.position - lastHandPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            handVelocity = Vector3.Lerp(handVelocity, v, 0.5f);
            lastHandPosition = hand.position;

            bool pressed = OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) || OVRInput.GetDown(OVRInput.Button.One);
            if (pressed && Time.frameCount != pickFrame) Drop();
        }

        if (transform.position.y < -3f) ResetToStart();
    }

    private void ResetToStart()
    {
        if (held == this) held = null;
        transform.SetParent(originalParent, true);
        SetLayer(-1);
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(startPosition, startRotation);
    }

    // layer -1 = restore the original layers
    private void SetLayer(int layer)
    {
        for (int i = 0; i < allTransforms.Length; i++)
            allTransforms[i].gameObject.layer = layer < 0 ? originalLayers[i] : layer;
    }
}
