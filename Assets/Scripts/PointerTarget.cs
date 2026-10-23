using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Put this on anything the ray pointer can hover and select (a door, a table object, a
/// confidence button). The object (or one of its children) needs a Collider.
/// React to it by adding scripts that listen to the events, or wire them in the Inspector.
/// </summary>
public class PointerTarget : MonoBehaviour
{
    [Tooltip("Untick to make the object ignore the pointer (e.g. a locked door).")]
    public bool interactable = true;

    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;
    public UnityEvent onSelected;

    public void HoverEnter() { onHoverEnter.Invoke(); }
    public void HoverExit() { onHoverExit.Invoke(); }
    public void Select() { onSelected.Invoke(); }
}
