using UnityEngine;
using UnityEngine.UI;

public class InventorySlotController : MonoBehaviour
{
    public Image itemIcon;
    private Item currentItem;

    public void AddItem(Item newItem)
    {
        currentItem = newItem;
        itemIcon.sprite = newItem.icon;
        itemIcon.enabled = true; // Hiện ảnh lên
    }

    public void ClearSlot()
    {
        currentItem = null;
        itemIcon.sprite = null;
        itemIcon.enabled = false; // Ẩn ảnh đi
    }
}
