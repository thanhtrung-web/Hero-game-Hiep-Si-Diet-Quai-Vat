using UnityEngine;

/// <summary>
/// Put this on the same GameObject as the door Animator. Call its public methods
/// from Animation Events to switch the doorway collider at the right animation frame.
/// </summary>
public class DoorAnimationEvents : MonoBehaviour
{
    [SerializeField] private Collider2D doorBlocker;

    private void Awake()
    {
        if (doorBlocker != null)
            doorBlocker.enabled = true;
    }

    // Add an Animation Event when the opening door has cleared the passage.
    public void DoorPassageOpen()
    {
        if (doorBlocker != null)
            doorBlocker.enabled = false;
    }

    // Add an Animation Event near the start of the closing animation.
    public void DoorPassageClosed()
    {
        if (doorBlocker != null)
            doorBlocker.enabled = true;
    }
}
