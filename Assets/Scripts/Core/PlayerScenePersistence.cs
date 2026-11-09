using System;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        // Tự động dọn dẹp nếu người chơi quay về Menu hoặc bảng chọn Map
        if (scene.name.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0 ||
            scene.name.IndexOf("Choice", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Destroy(gameObject);
            return;
        }

        GameObject spawn = GameObject.Find("PlayerSpawn");

        if (spawn != null)
        {
            transform.position = spawn.transform.position;
            Debug.Log($"[PlayerScenePersistence] Đã đặt vị trí người chơi tại PlayerSpawn: {spawn.transform.position}", this);
        }
        else
        {
            Debug.LogWarning($"[PlayerScenePersistence] Không tìm thấy PlayerSpawn trong {scene.name}", this);
        }

        // Dọn dẹp nhân vật tĩnh khác có trong Scene để tránh trùng lặp
        var allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in allPlayers)
        {
            if (p != gameObject && p.scene == scene)
            {
                Destroy(p);
            }
        }

        // Cập nhật lại CinemachineCamera trong Scene mới theo dõi nhân vật này
        var vcam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null)
        {
            vcam.Target.TrackingTarget = transform;
        }
    }
}
