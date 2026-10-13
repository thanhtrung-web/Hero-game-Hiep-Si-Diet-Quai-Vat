using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerScenePersistence : MonoBehaviour
{
    private static PlayerScenePersistence instance;

    private void Awake()
    {
       Debug.Log("Awake chạy", this);
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
        Debug.Log("OnEnable: đã huỷ đăng ký sceneLoaded");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        Debug.Log("OnDisable: đã đăng ký sceneLoaded");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"OnSceneLoaded: {scene.name}", this);

        GameObject spawn = GameObject.Find("PlayerSpawn");

        if (spawn != null)
        {
            transform.position = spawn.transform.position;
            Debug.Log($"Spawn tại {spawn.transform.position}", this);
        }
        else
        {
            Debug.LogWarning($"Không tìm thấy PlayerSpawn trong {scene.name}", this);
        }
    }
}