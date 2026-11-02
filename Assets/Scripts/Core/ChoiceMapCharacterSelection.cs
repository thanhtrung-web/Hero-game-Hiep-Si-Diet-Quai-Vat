using UnityEngine;
using UnityEngine.UI;

public class ChoiceMapCharacterSelection : MonoBehaviour
{
    private const string SelectedCharacterKey = "SelectedCharacter";

    [Header("Panels")]
    [SerializeField] private GameObject mapButtonsPanel;
    [SerializeField] private GameObject characterPanel;

    [Header("Selection Highlights")]
    [SerializeField] private Outline mageOutline;
    [SerializeField] private Outline knightOutline;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;

    [Header("Scene Loader")]
    [SerializeField] private MainMenuController mainMenu;

    // 0 = Mage, 1 = Knight
    private int selectedCharacter = -1;

    // 1-5 = Map 1..5, -1 = chưa chọn
    private int selectedMap = -1;

    private void Start()
    {
        selectedCharacter = PlayerPrefs.GetInt(SelectedCharacterKey, -1);

        if (mainMenu == null)
            mainMenu = FindFirstObjectByType<MainMenuController>();

        // Bước 1: chọn map trước
        ShowMapPanel();
        RefreshSelection();
    }

    // Gắn vào OnClick của nút map. Truyền số map: 1, 2, 3, 4 hoặc 5.
    public void SelectMap(int mapNumber)
    {
        selectedMap = mapNumber;

        // Bước 2: chọn nhân vật
        if (mapButtonsPanel != null)
            mapButtonsPanel.SetActive(false);

        if (characterPanel != null)
            characterPanel.SetActive(true);

        RefreshSelection();
    }

    // Nút "Quay lại" trong panel chọn nhân vật -> về lại chọn map
    public void BackToMapSelection()
    {
        selectedMap = -1;
        ShowMapPanel();
        RefreshSelection();
    }

    public void SelectMage()
    {
        selectedCharacter = 0;
        RefreshSelection();
    }

    public void SelectKnight()
    {
        selectedCharacter = 1;
        RefreshSelection();
    }

    public void ConfirmSelection()
    {
        if (selectedMap == -1)
        {
            Debug.LogWarning("Hãy chọn map trước.");
            return;
        }

        if (selectedCharacter == -1)
        {
            Debug.LogWarning("Hãy chọn Mage hoặc Knight trước.");
            return;
        }

        PlayerPrefs.SetInt(SelectedCharacterKey, selectedCharacter);
        PlayerPrefs.Save();

        if (mainMenu == null)
        {
            Debug.LogError("ChoiceMapCharacterSelection: Chưa có MainMenuController trong scene để load map.", this);
            return;
        }

        mainMenu.LoadMapByNumber(selectedMap);
    }

    private void ShowMapPanel()
    {
        if (mapButtonsPanel != null)
            mapButtonsPanel.SetActive(true);

        if (characterPanel != null)
            characterPanel.SetActive(false);
    }

    private void RefreshSelection()
    {
        if (mageOutline != null)
            mageOutline.enabled = selectedCharacter == 0;

        if (knightOutline != null)
            knightOutline.enabled = selectedCharacter == 1;

        if (confirmButton != null)
            confirmButton.interactable = selectedCharacter != -1 && selectedMap != -1;
    }
}