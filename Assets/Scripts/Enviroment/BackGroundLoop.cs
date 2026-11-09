using UnityEngine;

public class BackgroundLoop : MonoBehaviour
{
    [SerializeField] private Transform[] tiles;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float loopAhead = 20f;

    private void Start()
    {
        if (tiles == null || tiles.Length < 3 || targetCamera == null)
        {
            Debug.LogError(
                "Cần gán 3 nhóm nền và Main Camera vào BackgroundLoopManager.",
                this
            );
            enabled = false;
            return;
        }

        foreach (Transform tile in tiles)
        {
            if (tile == null ||
                tile.GetComponentsInChildren<SpriteRenderer>().Length == 0)
            {
                Debug.LogError(
                    "Mỗi ô Tiles phải là nhóm cha có SpriteRenderer con.",
                    this
                );
                enabled = false;
                return;
            }
        }
    }

    private void LateUpdate()
    {
        Transform leftTile = tiles[0];
        Transform rightTile = tiles[0];

        Bounds leftBounds = GetBounds(leftTile);
        Bounds rightBounds = leftBounds;

        foreach (Transform tile in tiles)
        {
            Bounds bounds = GetBounds(tile);

            if (bounds.min.x < leftBounds.min.x)
            {
                leftTile = tile;
                leftBounds = bounds;
            }

            if (bounds.max.x > rightBounds.max.x)
            {
                rightTile = tile;
                rightBounds = bounds;
            }
        }

        float halfCameraWidth =
            targetCamera.orthographicSize * targetCamera.aspect;

        float cameraLeft =
            targetCamera.transform.position.x - halfCameraWidth;

        float cameraRight =
            targetCamera.transform.position.x + halfCameraWidth;

        // Chuẩn bị nền phía trước khi camera còn cách mép 20 units.
        // Chỉ chuyển nhóm đã ra khỏi màn hình.
        if (cameraRight >= rightBounds.max.x - loopAhead
            && leftBounds.max.x < cameraLeft)
        {
            leftTile.position += Vector3.right
                * (rightBounds.max.x - leftBounds.min.x);

            return;
        }

        // Xử lý tương tự khi camera đi sang trái.
        if (cameraLeft <= leftBounds.min.x + loopAhead
            && rightBounds.min.x > cameraRight)
        {
            rightTile.position += Vector3.right
                * (leftBounds.min.x - rightBounds.max.x);
        }
    }

    private Bounds GetBounds(Transform tile)
    {
        SpriteRenderer[] sprites =
            tile.GetComponentsInChildren<SpriteRenderer>();

        Bounds bounds = sprites[0].bounds;

        for (int i = 1; i < sprites.Length; i++)
        {
            bounds.Encapsulate(sprites[i].bounds);
        }

        return bounds;
    }
}