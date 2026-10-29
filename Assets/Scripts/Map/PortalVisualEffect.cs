using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PortalVisualEffect : MonoBehaviour
{
    [Header("1. Frame Animation (Optional)")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 10f;

    [Header("2. Floating & Pulsing")]
    [SerializeField] private bool enableFloating = true;
    [SerializeField] private float floatSpeed = 2.5f;
    [SerializeField] private float floatAmplitude = 0.15f;

    [SerializeField] private bool enablePulsing = true;
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float pulseScale = 0.08f;

    [Header("3. Rotation (if single sprite)")]
    [SerializeField] private bool enableRotation = false;
    [SerializeField] private float rotationSpeed = 30f;

    private SpriteRenderer sr;
    private Vector3 initialLocalPos;
    private Vector3 initialScale;
    private float frameTimer;
    private int currentFrameIndex;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        initialLocalPos = transform.localPosition;
        initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        initialLocalPos = transform.localPosition;
        initialScale = transform.localScale;
        frameTimer = 0f;
        currentFrameIndex = 0;
    }

    private void Update()
    {
        // 1. Frame Animation
        if (frames != null && frames.Length > 0 && sr != null)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / Mathf.Max(frameRate, 1f))
            {
                frameTimer = 0f;
                currentFrameIndex = (currentFrameIndex + 1) % frames.Length;
                if (frames[currentFrameIndex] != null)
                {
                    sr.sprite = frames[currentFrameIndex];
                }
            }
        }

        // 2. Floating
        if (enableFloating)
        {
            float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.localPosition = initialLocalPos + new Vector3(0, yOffset, 0);
        }

        // 3. Pulsing
        if (enablePulsing)
        {
            float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
            transform.localScale = initialScale * (1f + scaleOffset);
        }

        // 4. Rotation
        if (enableRotation)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }
}
