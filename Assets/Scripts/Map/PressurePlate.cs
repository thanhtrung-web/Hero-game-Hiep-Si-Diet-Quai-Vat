using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Opens the door once when an accepted object (such as a rock) overlaps this plate.
/// Put this on the plate and set the plate's Collider2D to Is Trigger.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string[] acceptedTags = { "Crate" };
    [SerializeField] private Color pressedColor = Color.green;

    private readonly HashSet<Collider2D> acceptedObjects = new HashSet<Collider2D>();
    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private bool doorOpened;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (doorAnimator != null)
            doorAnimator.enabled = false;

        SetPressed(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsAccepted(other))
            return;

        acceptedObjects.Add(other);
        if (acceptedObjects.Count > 0)
            OpenDoorOnce();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        acceptedObjects.Remove(other);
    }

    private void OnDisable()
    {
        acceptedObjects.Clear();
        // The door remains open once triggered, even if the rock later leaves.
    }

    private bool IsAccepted(Collider2D other)
    {
        if (acceptedTags == null)
            return false;

        foreach (string acceptedTag in acceptedTags)
        {
            // Compare the tag string directly so a missing project tag does not throw.
            if (!string.IsNullOrEmpty(acceptedTag) && other.tag == acceptedTag)
                return true;
        }

        return false;
    }

    private void SetPressed(bool pressed)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = pressed ? pressedColor : originalColor;
    }

    private void OpenDoorOnce()
    {
        if (doorOpened)
            return;

        doorOpened = true;
        SetPressed(true);
        if (doorAnimator != null)
        {
            doorAnimator.enabled = true;
            doorAnimator.Play("DoorAni", 0, 0f);
        }
    }
}
