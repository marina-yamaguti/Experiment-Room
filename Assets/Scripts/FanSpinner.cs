using UnityEngine;

/// <summary>
/// Spins the ceiling fan around its Y axis.
/// Attach to the fan object (parent) in the Hub scene. The speed is editable  
///  we rotate around the centre of the hub instead of around the object's own pivot. (Sweet Home 3D objects have its pivot at the scene origin,)
/// </summary>
public class FanSpinner : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second. A negative value spins the other way.")]
    public float speed = 180f;

    [Tooltip("The round hub or motor in the middle of the fan. Its centre is the rotation point.")]
    public Renderer hub;

    private Vector3 center;

    void Start()
    {
        if (hub != null)
        {
            center = hub.bounds.center;
        }
        else
        {
            // Fallback: rotate around this object's own pivot 
            center = transform.position;
            Debug.LogWarning("FanSpinner: no hub assigned, the fan may not spin around its centre.", this);
        }
    }

    void Update()
    {
        // Time.deltaTime keeps the speed the same on any frame rate.
        transform.RotateAround(center, Vector3.up, speed * Time.deltaTime);
    }
}