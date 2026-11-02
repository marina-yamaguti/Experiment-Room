using UnityEngine;

/// <summary>
/// Spins the ceiling fan around its Y axis.
/// Attach to the fan object in the Hub scene. The speed is a public field, editable in the Inspector.
/// </summary>
public class FanSpinner : MonoBehaviour
{
    // Rotation speed
    public float speed = 180f;

    void Update()
    {
        // To make the speed the same on any frame rate
        transform.Rotate(0f, speed * Time.deltaTime, 0f);
    }
}