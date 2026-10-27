using UnityEngine;

/// <summary>
/// Base class for the three kinds of change (brief: one change type per room).
/// ColorChange (Room A), PositionChange (Room B) and DisappearChange (Room C) all derive from it.
///
/// TEAMMATES: to build your room, write a class that extends ChangeApplier, put it on the
/// "Trial" object instead of ColorChange, and implement ChangeType and Apply.
/// You never need to undo the change: RoomObject.ResetToOriginal() restores position,
/// materials and visibility after every trial.
///
/// Room C note: to make an object disappear use RoomObject.SetVisible(false), NOT SetActive(false),
/// so its collider stays and the participant can still point at the spot where it was.
/// </summary>
public abstract class ChangeApplier : MonoBehaviour
{
    /// <summary>Written to the CSV, e.g. "Color", "Position", "Disappearance".</summary>
    public abstract string ChangeType { get; }

    /// <summary>Applies the change to the chosen object. Called while the screen is grey.</summary>
    public abstract void Apply(RoomObject target);
}
