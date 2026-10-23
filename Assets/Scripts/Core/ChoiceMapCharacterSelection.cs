using UnityEngine;
using UnityEngine.UI;

public class ChoiceMapCharacterSelection : MonoBehaviour
{
    private const string SelectedCharacterKey = "SelectedCharacter";

    [Header("Panels")]
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject mapButtonsPanel;

    [Header("Selection Highlights")]
    [SerializeField] private Outline mageOutline;
    [SerializeField] private Outline knightOutline;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;

    // 0 = Mage, 1 = Knight
    private int selectedCharacter = -1;

    private void Start()
    {
        selectedCharacter = PlayerPrefs.GetInt(SelectedCharacterKey, -1);

        if (characterPanel != null)
            characterPanel.SetActive(true);

        if (mapButtonsPanel != null)
            mapButtonsPanel.SetActive(false);

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
        if (selectedCharacter == -1)
        {
            Debug.LogWarning("Hãy chọn Mage hoặc Knight trước.");
            return;
        }

        PlayerPrefs.SetInt(SelectedCharacterKey, selectedCharacter);
        PlayerPrefs.Save();

        characterPanel.SetActive(false);
        mapButtonsPanel.SetActive(true);
    }

    private void RefreshSelection()
    {
        if (mageOutline != null)
            mageOutline.enabled = selectedCharacter == 0;

        if (knightOutline != null)
            knightOutline.enabled = selectedCharacter == 1;

        if (confirmButton != null)
            confirmButton.interactable = selectedCharacter != -1;
    }
}