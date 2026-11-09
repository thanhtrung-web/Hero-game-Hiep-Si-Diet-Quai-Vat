using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("Portal Settings")]
    [SerializeField] private string targetSceneName = "Map5_Phase2";
    [SerializeField] private float transitionDelay = 0.3f;

    [Header("Safety & Behavior")]
    [Tooltip("Thời gian chờ tối thiểu sau khi scene load trước khi cổng kích hoạt, tránh bị loop nếu vừa spawn ngay trên cổng")]
    [SerializeField] private float activateDelay = 2.0f;
    [Tooltip("Nếu true, cổng này chỉ là cổng xuất hiện (một chiều), không dịch chuyển người chơi đi đâu cả")]
    [SerializeField] private bool isOneWay = false;

    private bool isLoading;
    private float sceneLoadedTime;

    public string TargetSceneName
    {
        get => targetSceneName;
        set => targetSceneName = value;
    }

    public bool IsOneWay
    {
        get => isOneWay;
        set => isOneWay = value;
    }

    private void Awake()
    {
        sceneLoadedTime = Time.time;
    }

    private void OnEnable()
    {
        sceneLoadedTime = Time.time;
        isLoading = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTriggerPortal(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTriggerPortal(other);
    }

    private void TryTriggerPortal(Collider2D other)
    {
        if (isOneWay) return;
        // Chỉ hoãn kích hoạt nếu Scene vừa mới nạp (ngăn người chơi vừa load scene đã bị dịch chuyển ngược lại)
        if (Time.timeSinceLevelLoad < activateDelay) return;
        if (isLoading) return;

        bool isPlayer = other.CompareTag("Player")
                     || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"))
                     || other.GetComponentInParent<KnightController>() != null
                     || other.GetComponentInParent<PlayerHealth>() != null;

        if (!isPlayer) return;

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"[ScenePortal] targetSceneName chưa được thiết lập trên {gameObject.name}!");
            return;
        }

        isLoading = true;
        Debug.Log($"🌀 [ScenePortal] Người chơi đã bước vào cổng! Đang chuyển đến scene: '{targetSceneName}'...");

        if (transitionDelay > 0f)
        {
            StartCoroutine(LoadSceneWithDelay());
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private IEnumerator LoadSceneWithDelay()
    {
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(targetSceneName);
    }
}
