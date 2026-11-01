using UnityEngine;

/// <summary>
/// Room C: the chosen object disappears and is replaced by a look-alike in the same spot,
/// to avoid having an empty space that would make the ansswer obvious.
/// The participant points at the replacement to answer.
/// </summary>
public class DisappearChange : ChangeApplier
{
    public override string ChangeType { get { return "Disappear"; } }

    public override void Apply(RoomObject target)
    {
        // Hide the object but the collider stays
        target.SetVisible(false);

        // Show the look-alike
        if (target.replacement != null) 
        {
            foreach (Renderer r in target.replacement.GetComponentsInChildren<Renderer>())
                r.enabled = true;
            target.replacement.SetActive(true);
        }
        else Debug.LogWarning("DisappearChange: no replacement listed for '" + target.objectId + "'.", this);
    }
}
