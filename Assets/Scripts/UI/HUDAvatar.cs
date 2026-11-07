using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HUDAvatar : MonoBehaviour
{
    [SerializeField] private Sprite mageAvatar;
    [SerializeField] private Sprite knightAvatar;

    // PHẢI trùng đúng key mà ChoiceMapCharacterSelection / PlayerSpawner đang dùng
    private const string CharacterKey = "SelectedCharacter";

    private void Start()
    {
        Image avatar = GetComponent<Image>();
        int selected = PlayerPrefs.GetInt(CharacterKey, 0); // 0 = Mage, 1 = Knight
        avatar.sprite = selected == 1 ? knightAvatar : mageAvatar;
    }
}