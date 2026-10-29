using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("Portal Settings")]
    [SerializeField] private string targetSceneName = "Map5_Phase2";
    [SerializeField] private float transitionDelay = 0.3f;

    private bool isLoading;

    public string TargetSceneName
    {
        get => targetSceneName;
        set => targetSceneName = value;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
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
