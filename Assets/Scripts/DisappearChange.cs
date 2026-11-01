using UnityEngine;

/// <summary>
/// Room C: the chosen object disappears. The participant can still point 
/// at the empty spot where the object used to be to answer.
/// </summary>
public class DisappearChange : ChangeApplier
{
    public override string ChangeType { get { return "Disappearance"; } }

    public override void Apply(RoomObject target)
    {
        // Hide the object but the collider stays
        target.SetVisible(false);
    }
}
