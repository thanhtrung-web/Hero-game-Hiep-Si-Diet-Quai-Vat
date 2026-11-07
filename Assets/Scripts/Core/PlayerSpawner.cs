using Unity.Cinemachine;
using UnityEngine;

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
        int selected = PlayerPrefs.GetInt("SelectedCharacter", -1);

        Debug.Log("=== PLAYER SPAWNER START ===");
        Debug.Log("SelectedCharacter = " + selected);
        Debug.Log("Mage Prefab = " + magePrefab);
        Debug.Log("Knight Prefab = " + knightPrefab);
        Debug.Log("Spawn Point = " + spawnPoint);

        SpawnSelectedCharacter();
    }

    private void SpawnSelectedCharacter()
    {
        int selectedCharacter =
            PlayerPrefs.GetInt(SelectedCharacterKey, 1);

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

        Debug.Log("Spawned Player = " + player.name);
        Debug.Log("Player Transform = " + player.transform);
        Debug.Log("Camera ref = " + cinemachineCamera);

        if (cinemachineCamera != null)
        {
            cinemachineCamera.Target.TrackingTarget = player.transform;

            Debug.Log(
                "Camera Target sau khi gán = " +
                cinemachineCamera.Target.TrackingTarget
            );
        }
    }
}