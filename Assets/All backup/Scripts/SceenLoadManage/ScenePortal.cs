using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [SerializeField] private string targetSceneName;
    private bool isLoading;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Portal chạm: {other.name}, tag = {other.tag}");

        if (isLoading || !other.CompareTag("Player"))
            return;

        isLoading = true;
        Debug.Log($"Load scene: {targetSceneName}");
        SceneManager.LoadScene(targetSceneName);
    }
}