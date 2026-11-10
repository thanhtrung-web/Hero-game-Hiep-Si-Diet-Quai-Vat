using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Quản lý khởi tạo nhân vật người chơi (Knight hoặc Mage) dựa theo lựa chọn trong PlayerPrefs.
/// Tự động kiểm tra nếu đã có nhân vật từ Scene trước chuyển qua (DontDestroyOnLoad) thì giữ nguyên,
/// không spawn đè để bảo toàn lượng máu và thể lực hiện có.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    private const string SelectedCharacterKey = "SelectedCharacter";

    [Header("Character Prefabs")]
    [SerializeField] private GameObject magePrefab;
    [SerializeField] private GameObject knightPrefab;

    [Header("Spawn Point")]
    [SerializeField] private Transform spawnPoint;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    private void Start()
    {
        // 1. Nếu đã có nhân vật chuyển từ Scene trước sang (DontDestroyOnLoad), không spawn mới để giữ nguyên máu
        var existingPlayer = FindAnyObjectByType<PlayerHealth>();
        if (existingPlayer != null && existingPlayer.gameObject.scene.name == "DontDestroyOnLoad")
        {
            Debug.Log($"🛡️ [PlayerSpawner] Đã phát hiện nhân vật '{existingPlayer.gameObject.name}' từ Scene trước (DontDestroyOnLoad, Máu: {existingPlayer.CurrentHealth}/{existingPlayer.MaxHealth}). Giữ nguyên nhân vật.");

            if (cinemachineCamera != null)
            {
                cinemachineCamera.Target.TrackingTarget = existingPlayer.transform;
            }
            return;
        }

        // 2. Nếu spawnPoint chưa được gán thủ công, tự tìm đối tượng 'PlayerSpawn' trong Scene
        if (spawnPoint == null)
        {
            GameObject sp = GameObject.Find("PlayerSpawn");
            if (sp != null) spawnPoint = sp.transform;
        }

        // 3. Nếu cinemachineCamera chưa gán, tự tìm
        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        }

        SpawnSelectedCharacter();
    }

    private void SpawnSelectedCharacter()
    {
        int selectedCharacter = PlayerPrefs.GetInt(SelectedCharacterKey, 1);

        GameObject selectedPrefab;

        switch (selectedCharacter)
        {
            case 0:
                selectedPrefab = magePrefab;
                break;

            case 1:
                selectedPrefab = knightPrefab;
                break;

            default:
                selectedPrefab = knightPrefab;
                break;
        }

        if (selectedPrefab == null)
        {
            Debug.LogError("PlayerSpawner: Character Prefab chưa được gán!");
            return;
        }

        Vector3 position = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        GameObject player = Instantiate(
            selectedPrefab,
            position,
            Quaternion.identity
        );

        // Đảm bảo nhân vật luôn có PlayerScenePersistence để chuyển Scene không bị mất
        if (player.GetComponent<PlayerScenePersistence>() == null)
        {
            player.AddComponent<PlayerScenePersistence>();
        }

        Debug.Log("🎮 [PlayerSpawner] Đã spawn nhân vật: " + player.name + " tại vị trí: " + position);

        if (cinemachineCamera != null)
        {
            cinemachineCamera.Target.TrackingTarget = player.transform;
        }
    }
}
