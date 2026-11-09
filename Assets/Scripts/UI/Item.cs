using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;      // Tên v?t ph?m
    public Sprite icon;          // Hình ?nh hi?n th?
    public bool isStackable;     // Có th? c?ng d?n không (ví d?: bình máu)
}

