using UnityEngine;
using UnityEngine.Tilemaps;

public class BackgroundSize : MonoBehaviour
{
    void Start()
    {
        TilemapRenderer[] renderers =
            GetComponentsInChildren<TilemapRenderer>();

        Bounds totalBounds = renderers[0].bounds;

        foreach (TilemapRenderer renderer in renderers)
        {
            totalBounds.Encapsulate(renderer.bounds);
        }

        Debug.Log("Background Width: " + totalBounds.size.x);
        Debug.Log("Background Height: " + totalBounds.size.y);
        Debug.Log("Left: " + totalBounds.min.x);
        Debug.Log("Right: " + totalBounds.max.x);
    }
}