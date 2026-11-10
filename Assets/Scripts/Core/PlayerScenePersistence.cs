using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý việc lưu giữ và dịch chuyển nhân vật người chơi (Knight / Mage) xuyên suốt các Scene (DontDestroyOnLoad).
/// Đảm bảo giữ nguyên lượng máu (PlayerHealth), thể lực và trang bị khi đi qua Cổng dịch chuyển (ScenePortal).
/// Khi sang Scene mới, tự động di chuyển nhân vật về vị trí PlayerSpawn và gán lại camera theo dõi.
/// </summary>
public class PlayerScenePersistence : MonoBehaviour
{
    private static PlayerScenePersistence instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Tự động dọn dẹp nếu người chơi quay về Menu hoặc bảng chọn Map
        if (scene.name.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0 ||
            scene.name.IndexOf("Choice", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Destroy(gameObject);
            return;
        }

        // 2. Dịch chuyển nhân vật đến điểm xuất phát PlayerSpawn của Scene mới
        GameObject spawn = GameObject.Find("PlayerSpawn");

        if (spawn != null)
        {
            transform.position = spawn.transform.position;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.position = spawn.transform.position;
            }

            Debug.Log($"🛡️ [PlayerScenePersistence] Đã đặt vị trí người chơi '{gameObject.name}' tại PlayerSpawn: {spawn.transform.position} trong scene '{scene.name}'", this);
        }
        else
        {
            Debug.LogWarning($"⚠️ [PlayerScenePersistence] Không tìm thấy PlayerSpawn trong scene '{scene.name}'", this);
        }

        // 3. Dọn dẹp nhân vật tĩnh khác có trong Scene để tránh trùng lặp
        var allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in allPlayers)
        {
            if (p != gameObject && p.scene == scene)
            {
                Destroy(p);
            }
        }

        // 4. Cập nhật lại CinemachineCamera trong Scene mới để bám theo nhân vật
        var vcam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null)
        {
            vcam.Target.TrackingTarget = transform;
            Debug.Log($"🎥 [PlayerScenePersistence] CinemachineCamera đã liên kết mục tiêu theo dõi: {gameObject.name}");
        }


    }
}
