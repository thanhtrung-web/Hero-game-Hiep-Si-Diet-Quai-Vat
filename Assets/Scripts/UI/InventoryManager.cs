using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public List<Item> items = new List<Item>(); // Danh sách chứa vật phẩm
    public Transform slotContainer;             // Kéo đối tượng SlotContainer vào đây
    public GameObject slotPrefab;               // Kéo Prefab InventorySlot vào đây

    void Start()
    {
        RenderInventory();
    }

    // Hàm cập nhật và hiển thị vật phẩm lên màn hình
    public void RenderInventory()
    {
        // Xóa các ô cũ để vẽ lại
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        // Tạo số lượng ô tương ứng với số vật phẩm đang có
        foreach (Item item in items)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            InventorySlotController slotController = newSlot.GetComponent<InventorySlotController>();

            if (slotController != null)
            {
                slotController.AddItem(item);
            }
        }
    }

    // Hàm dùng để nhặt/thêm đồ vào balo
    public void AddItemToInventory(Item item)
    {
        items.Add(item);
        RenderInventory(); // Cập nhật lại giao diện
    }
}
