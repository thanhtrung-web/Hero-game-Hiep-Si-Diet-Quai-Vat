using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rào chắn / Phong ấn phòng Boss.
/// Ngăn người chơi đi vào phòng Boss cho tới khi tiêu diệt sạch toàn bộ quái vật theo yêu cầu.
/// Hỗ trợ cả danh sách quái chỉ định thủ công (như Map 1) và chế độ tự động đếm toàn bộ quái vật thường trong Scene (Map 5).
/// Có hiệu ứng nhấp nhô phát sáng (Pulse Glow), âm thanh va chạm / phá vỡ phong ấn, và hiển thị thông báo số quái còn lại khi người chơi chạm vào.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossBarrier : MonoBehaviour
{
    [Header("1. Điều kiện mở phong ấn")]
    [Tooltip("Tự động theo dõi toàn bộ quái vật thường trong Scene (bỏ qua Boss)")]
    [SerializeField] private bool autoTrackAllMonsters = true;

    [Tooltip("Danh sách quái con chỉ định thủ công (nếu muốn giới hạn quái cụ thể)")]
    [SerializeField] private EnemyHealth[] monsters;

    [Header("2. Hiệu ứng trực quan (Visual & Pulse)")]
    [Tooltip("Sprite hiển thị rào chắn")]
    [SerializeField] private SpriteRenderer barrierVisual;

    [Tooltip("Màu sắc của phong ấn (Mặc định: Tím ma thuật linh hồn)")]
    [SerializeField] private Color barrierColor = new Color(0.7f, 0.25f, 1f, 0.85f);

    [Tooltip("Tốc độ nhấp nhô phát sáng")]
    [SerializeField] private float pulseSpeed = 2.5f;

    [SerializeField] private float minAlpha = 0.45f;
    [SerializeField] private float maxAlpha = 0.95f;

    [Header("3. Âm thanh (Audio SFX)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip barrierHitSound;
    [SerializeField] private AudioClip barrierUnlockSound;

    [Header("4. Thông báo UI")]
    [SerializeField] private string barrierTitle = "Phong ấn phòng Boss";
    [SerializeField] private float messageCooldown = 1.8f;

    private Collider2D barrierCol;
    private bool barrierOpened = false;
    private float lastMessageTime = -10f;
    private readonly List<EnemyHealth> trackedMonsters = new List<EnemyHealth>();
    private int totalMonsterCount = 0;
    private TextMesh promptTextMesh;
    private Coroutine textRoutine;

    public bool IsOpened => barrierOpened;
    public int TotalMonsterCount => totalMonsterCount;
    public int RemainingMonsterCount => GetRemainingMonsterCount();
    public int DefeatedMonsterCount => Mathf.Max(0, totalMonsterCount - GetRemainingMonsterCount());

    private void Awake()
    {
        barrierCol = GetComponent<Collider2D>();
        if (barrierVisual == null) barrierVisual = GetComponent<SpriteRenderer>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        CreatePromptText();
        InitializeMonsterList();
    }

    private void Start()
    {
        if (trackedMonsters.Count == 0)
        {
            InitializeMonsterList();
        }
    }

    public void InitializeMonsterList()
    {
        trackedMonsters.Clear();

        // 1. Nếu có gán danh sách thủ công (như Map 1), ưu tiên danh sách đó
        if (monsters != null && monsters.Length > 0)
        {
            foreach (var m in monsters)
            {
                if (m != null && !trackedMonsters.Contains(m))
                    trackedMonsters.Add(m);
            }
        }

        // 2. Nếu bật tự động hoặc danh sách thủ công trống: tìm toàn bộ quái thường trong scene
        if (autoTrackAllMonsters || trackedMonsters.Count == 0)
        {
            var allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude);
            foreach (var enemy in allEnemies)
            {
                // Bỏ qua Boss, chỉ tính lính thường
                if (enemy != null && !enemy.IsBoss && !trackedMonsters.Contains(enemy))
                {
                    trackedMonsters.Add(enemy);
                }
            }
        }

        totalMonsterCount = trackedMonsters.Count;
        Debug.Log($"🛡️ [{gameObject.name}] Đã kích hoạt phong ấn! Cần tiêu diệt {GetRemainingMonsterCount()}/{totalMonsterCount} quái vật để mở cửa.");
    }

    private void Update()
    {
        if (barrierOpened) return;

        // Hiệu ứng nhấp nhô phát sáng huyền bí
        if (barrierVisual != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            Color c = barrierColor;
            c.a = alpha;
            barrierVisual.color = c;
        }

        // Kiểm tra xem quái đã bị tiêu diệt hết chưa
        if (GetRemainingMonsterCount() == 0)
        {
            OpenBarrier();
        }
    }

    /// <summary>
    /// Đếm số lượng quái vật còn sống cần tiêu diệt.
    /// </summary>
    public int GetRemainingMonsterCount()
    {
        int count = 0;
        for (int i = 0; i < trackedMonsters.Count; i++)
        {
            var m = trackedMonsters[i];
            if (m != null && !m.IsDead && m.gameObject.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryShowNotification(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryShowNotification(other);
    }

    private void TryShowNotification(Collider2D col)
    {
        if (barrierOpened) return;

        bool isPlayer = col.CompareTag("Player")
                     || col.GetComponentInParent<KnightController>() != null
                     || col.GetComponentInParent<PlayerHealth>() != null;

        if (!isPlayer) return;

        if (Time.time >= lastMessageTime + messageCooldown)
        {
            lastMessageTime = Time.time;
            int remaining = GetRemainingMonsterCount();

            if (barrierHitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(barrierHitSound);
            }

            ShowFloatingMessage($"🔒 {barrierTitle}!\nCòn lại {remaining} quái vật!");
            Debug.Log($"🔒 [BossBarrier] Người chơi chạm vào phong ấn! Còn lại {remaining} quái cần tiêu diệt.");
        }
    }

    /// <summary>
    /// Mở rào chắn: Phát âm thanh, làm mờ dần và tắt va chạm cho người chơi tiến vào.
    /// </summary>
    public void OpenBarrier()
    {
        if (barrierOpened) return;
        barrierOpened = true;

        Debug.Log($"✨ [{gameObject.name}] ĐÃ TIÊU DIỆT SẠCH QUÁI VẬT! PHONG ẤN PHÒNG BOSS ĐƯỢC GIẢI TRỪ!");
        StartCoroutine(DissolveAndOpenRoutine());
    }

    private IEnumerator DissolveAndOpenRoutine()
    {
        if (barrierUnlockSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(barrierUnlockSound);
        }

        ShowFloatingMessage("✨ Phong ấn đã bị phá vỡ!\nCửa phòng Đại Trùm đã mở!");

        if (barrierCol != null) barrierCol.enabled = false;

        // Hiệu ứng mờ dần trong 0.8 giây
        if (barrierVisual != null)
        {
            float elapsed = 0f;
            float duration = 0.8f;
            Color startColor = barrierVisual.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                barrierVisual.color = c;
                yield return null;
            }
        }

        gameObject.SetActive(false);
    }

    private void CreatePromptText()
    {
        GameObject textObj = new GameObject("BarrierPrompt");
        textObj.transform.SetParent(transform, false);
        textObj.transform.localPosition = new Vector3(0f, 2.2f, 0f);

        promptTextMesh = textObj.AddComponent<TextMesh>();
        promptTextMesh.alignment = TextAlignment.Center;
        promptTextMesh.anchor = TextAnchor.MiddleCenter;
        promptTextMesh.fontSize = 32;
        promptTextMesh.characterSize = 0.08f;
        promptTextMesh.fontStyle = FontStyle.Bold;
        promptTextMesh.color = new Color(1f, 0.9f, 0.4f, 0f); // Mặc định ẩn

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (defaultFont != null)
        {
            promptTextMesh.font = defaultFont;
            var mr = textObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (defaultFont.material != null) mr.sharedMaterial = defaultFont.material;
                mr.sortingOrder = 15;
            }
        }
    }

    private void ShowFloatingMessage(string msg)
    {
        if (promptTextMesh == null) return;
        promptTextMesh.text = msg;

        if (textRoutine != null) StopCoroutine(textRoutine);
        textRoutine = StartCoroutine(AnimateMessageRoutine());
    }

    private IEnumerator AnimateMessageRoutine()
    {
        if (promptTextMesh == null) yield break;

        Color start = new Color(1f, 0.95f, 0.5f, 1f);
        promptTextMesh.color = start;

        yield return new WaitForSeconds(1.8f);

        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            Color c = promptTextMesh.color;
            c.a = Mathf.Lerp(1f, 0f, elapsed / 0.6f);
            promptTextMesh.color = c;
            yield return null;
        }

        Color final = promptTextMesh.color;
        final.a = 0f;
        promptTextMesh.color = final;
    }
}
