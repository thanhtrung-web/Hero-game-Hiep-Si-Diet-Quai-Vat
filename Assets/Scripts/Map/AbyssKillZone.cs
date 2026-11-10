using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý khu vực Vực Thẳm (Abyss / Bottomless Pit) trong các màn chơi đi cảnh (Map 5).
/// - Phát hiện tức thì khi Người chơi hoặc Quái vật rơi khỏi các bục nhảy xuống vực thẳm.
/// - Xử lý "rơi vực tử trận" ("cook"):
///   + Khóa vận tốc rơi tự do để tránh văng mất camera.
///   + Gây sát thương chí mạng kết liễu nhân vật ngay lập tức.
///   + Tự động kích hoạt bảng Thua trận (GameEndUI: "Nhân vật đã bị gục ngã - Máu đã rơi, hãy đứng lên và phục thù..").
/// - Hỗ trợ cả 2 cơ chế song song:
///   1. Trigger Collider 2D bao trọn đáy vực.
///   2. Fallback kiểm tra tọa độ Y mỗi frame (ngăn ngừa triệt để lỗi rơi xuyên sàn ở tốc độ cao).
/// - Tự động tiêu diệt quái vật (Skeleton, Mob) khi bị đánh văng xuống vực.
/// - Tự động tạo mảng tối vực thẳm (Abyss Darkness) che lấp khoảng trống xám xịt dưới sàn.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class AbyssKillZone : MonoBehaviour
{
    [Header("1. Cấu hình Rơi Vực (Fall Settings)")]
    [Tooltip("Ngưỡng tọa độ Y an toàn: nếu Người chơi rơi xuống thấp hơn mốc này sẽ kích hoạt chết ngay lập tức")]
    [SerializeField] private float fallThresholdY = -5.8f;

    [Tooltip("Kích hoạt kiểm tra tọa độ Y mỗi frame dự phòng (Fallback)")]
    [SerializeField] private bool enableYThresholdCheck = true;

    [Header("2. Âm thanh & Hiệu ứng")]
    [Tooltip("Âm thanh rơi xuống vực thẳm / tiếng gió rít đáy vực")]
    [SerializeField] private AudioClip abyssFallSound;
    [SerializeField] private AudioSource audioSource;

    [Header("3. Hiển thị Vực Thẳm (Visual Abyss)")]
    [Tooltip("Tự động tạo phông nền bóng tối vực thẳm phía dưới các bục nhảy")]
    [SerializeField] private bool createAbyssDarkness = true;

    private bool playerFallen = false;
    private Transform playerTransform;
    private PlayerHealth cachedPlayerHealth;

    public float FallThresholdY
    {
        get => fallThresholdY;
        set => fallThresholdY = value;
    }

    private void Awake()
    {
        var boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxCol.isTrigger = true;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        // Tự động nạp âm thanh gió rít rơi vực nếu chưa gán
        if (abyssFallSound == null)
        {
#if UNITY_EDITOR
            abyssFallSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Map3/25_Wind_01.wav");
#endif
        }

        if (createAbyssDarkness)
        {
            EnsureAbyssDarknessVisual();
        }
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (playerFallen) return;

        // Fallback kiểm tra tọa độ Y mỗi frame chống rơi xuyên collider khi tốc độ rơi quá lớn
        if (enableYThresholdCheck)
        {
            if (playerTransform == null)
            {
                FindPlayer();
            }

            if (playerTransform != null && playerTransform.position.y <= fallThresholdY)
            {
                HandlePlayerFall(playerTransform.gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (playerFallen) return;

        // 1. Kiểm tra Người chơi (Knight hoặc Mage)
        if (collision.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            HandlePlayerFall(collision.gameObject);
            return;
        }

        // 2. Kiểm tra Quái vật rơi xuống vực -> Tiêu diệt ngay để giải phóng tài nguyên
        EnemyHealth enemy = collision.GetComponentInParent<EnemyHealth>();
        if (enemy != null && !enemy.IsDead)
        {
            Debug.Log($"☠️ [AbyssKillZone] Quái vật {enemy.name} bị rơi xuống vực thẳm!");
            enemy.TakeDamage(9999);
            Destroy(enemy.gameObject, 0.5f);
            return;
        }
    }

    /// <summary>
    /// Xử lý chuỗi sự kiện khi Người chơi rơi vào vực thẳm ("cook").
    /// </summary>
    /// <param name="targetObj">GameObject của người chơi va chạm.</param>
    private void HandlePlayerFall(GameObject targetObj)
    {
        if (playerFallen) return;
        playerFallen = true;

        Debug.Log("💀 [AbyssKillZone] Người chơi đã rơi xuống vực thẳm! Kích hoạt tử trận...");

        // Phát âm thanh gió rít rơi vực
        if (abyssFallSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(abyssFallSound);
        }

        // Tìm PlayerHealth
        PlayerHealth health = targetObj.GetComponentInParent<PlayerHealth>();
        if (health == null && cachedPlayerHealth != null)
            health = cachedPlayerHealth;

        // Khóa vận tốc rơi để người chơi không bị văng mất dấu camera
        Rigidbody2D rb = targetObj.GetComponentInParent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        // Gây sát thương chí mạng kết liễu
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(9999);
        }

        // Tự động làm mờ nhẹ sprite nhân vật chìm dần vào bóng tối vực thẳm
        SpriteRenderer sr = targetObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            StartCoroutine(FadeOutPlayerSprite(sr));
        }

        // Đảm bảo kích hoạt bảng Game Over thất bại (ShowLose)
        StartCoroutine(TriggerGameOverDelayed());
    }

    /// <summary>
    /// Hiệu ứng mờ dần khi chìm sâu vào bóng tối vực thẳm.
    /// </summary>
    private IEnumerator FadeOutPlayerSprite(SpriteRenderer sr)
    {
        float elapsed = 0f;
        float duration = 0.5f;
        Color startCol = sr.color;

        while (elapsed < duration && sr != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            sr.color = Color.Lerp(startCol, new Color(0f, 0f, 0f, 0f), t);
            yield return null;
        }
    }

    /// <summary>
    /// Hiển thị bảng Thua trận (GameEndUI) sau khoảng trễ ngắn để tạo cảm giác chìm vào vực thẳm.
    /// </summary>
    private IEnumerator TriggerGameOverDelayed()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        GameEndUI gameEnd = FindAnyObjectByType<GameEndUI>(FindObjectsInactive.Include);
        if (gameEnd != null)
        {
            gameEnd.ShowLose();
        }
    }

    /// <summary>
    /// Tìm kiếm Người chơi trong Scene theo thứ tự ưu tiên: Tag "Player", KnightController, PlayerHealth.
    /// </summary>
    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerTransform = p.transform;
            cachedPlayerHealth = p.GetComponentInParent<PlayerHealth>();
            return;
        }

        var knight = FindAnyObjectByType<KnightController>();
        if (knight != null)
        {
            playerTransform = knight.transform;
            cachedPlayerHealth = knight.GetComponent<PlayerHealth>();
            return;
        }

        var ph = FindAnyObjectByType<PlayerHealth>();
        if (ph != null)
        {
            playerTransform = ph.transform;
            cachedPlayerHealth = ph;
        }
    }

    /// <summary>
    /// Tạo mảng tối vực thẳm (Abyss Darkness Visual) phía sau địa hình, lấp đầy khoảng xám xịt dưới sàn.
    /// </summary>
    private void EnsureAbyssDarknessVisual()
    {
        Transform existingDarkness = transform.Find("Abyss_Darkness_Layer");
        if (existingDarkness != null) return;

        GameObject darkObj = new GameObject("Abyss_Darkness_Layer");
        darkObj.transform.SetParent(transform, false);
        darkObj.transform.localPosition = new Vector3(0f, -8f, 0f);

        // Tạo Texture gradient tối dần từ miệng vực xuống đáy đen kịt
        Texture2D tex = new Texture2D(1, 64);
        for (int y = 0; y < 64; y++)
        {
            float t = (float)y / 63f; // 0 ở đáy, 1 ở đỉnh
            // Đỉnh (gần miệng vực): đen xám mờ (alpha 0.85). Đáy (sâu bên dưới): đen tuyền tuyệt đối
            float alpha = Mathf.Lerp(1.0f, 0.85f, t);
            Color col = new Color(0.04f, 0.03f, 0.06f, alpha);
            tex.SetPixel(0, y, col);
        }
        tex.Apply();

        Sprite abyssSprite = Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f), 1f);

        SpriteRenderer sr = darkObj.AddComponent<SpriteRenderer>();
        sr.sprite = abyssSprite;
        // SortingOrder = -5: Nằm sau các khối đất (order 0), nhưng nằm TRƯỚC nền xám mặc định của Camera
        sr.sortingOrder = -5;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(300f, 25f);
    }

    /// <summary>
    /// Tự động thiết lập Vực Thẳm khi nạp các màn chơi Map 5 (Runtime Automatic Injection).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoEnsureAbyssOnSceneLoad()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Map5")
        {
            EnsureAbyssInMap5Phase1();
        }
        else if (sceneName == "Map5_Phase2")
        {
            EnsureAbyssInMap5Phase2();
        }
    }

    /// <summary>
    /// Đảm bảo Vực Thẳm luôn tồn tại và hoạt động chuẩn mực trong Map 5 Phase 1.
    /// </summary>
    public static GameObject EnsureAbyssInMap5Phase1()
    {
        var existing = FindAnyObjectByType<AbyssKillZone>(FindObjectsInactive.Include);
        if (existing != null) return existing.gameObject;

        GameObject abyssObj = new GameObject("Abyss_KillZone");
        abyssObj.transform.position = new Vector3(20f, -5.8f, 0f);

        BoxCollider2D col = abyssObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(280f, 10f);
        col.offset = new Vector2(0f, -4f);

        AbyssKillZone zone = abyssObj.AddComponent<AbyssKillZone>();
        zone.fallThresholdY = -5.8f;
        zone.enableYThresholdCheck = true;

        Debug.Log("🌌 [AbyssKillZone] Đã tự động kích hoạt Vực Thẳm cho Map 5 Phase 1 tại ngưỡng Y: -5.8!");
        return abyssObj;
    }

    /// <summary>
    /// Đảm bảo Vực Thẳm tồn tại cho Map 5 Phase 2 (khu vực đáy hầm sâu).
    /// </summary>
    public static GameObject EnsureAbyssInMap5Phase2()
    {
        var existing = FindAnyObjectByType<AbyssKillZone>(FindObjectsInactive.Include);
        if (existing != null) return existing.gameObject;

        GameObject abyssObj = new GameObject("Abyss_KillZone");
        abyssObj.transform.position = new Vector3(40f, -36f, 0f);

        BoxCollider2D col = abyssObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(280f, 10f);
        col.offset = new Vector2(0f, -4f);

        AbyssKillZone zone = abyssObj.AddComponent<AbyssKillZone>();
        zone.fallThresholdY = -36f;
        zone.enableYThresholdCheck = true;

        Debug.Log("🌌 [AbyssKillZone] Đã tự động kích hoạt Vực Thẳm cho Map 5 Phase 2 tại ngưỡng Y: -36.0!");
        return abyssObj;
    }

    /// <summary>
    /// Vẽ vùng vực thẳm và đường ranh giới kích hoạt trong Scene Editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.1f, 0.1f, 0.5f);
        var boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            Gizmos.DrawCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
        }

        // Vẽ đường ranh giới Y Fallback
        if (enableYThresholdCheck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(-100f, fallThresholdY, 0f), new Vector3(200f, fallThresholdY, 0f));
        }
    }
}
