using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int saveVersion = 3;

    // Time
    public int seconds = 0;
    public int minutes = 0;
    public int hours = 6;

    // Day 1 starts on Sunday in Spring.
    public int totalDays = 1;

    // Player
    public bool hasPlayerPosition = false;
    public string currentScene = "";
    public float playerX = 0f;
    public float playerY = 0f;
    public float playerZ = 0f;

    // Inventory / Hotbar
    public int selectedHotbarIndex = 0;
    public List<InventorySlotSaveData> inventorySlots =
        new List<InventorySlotSaveData>();

    // Only cells changed from the hand-painted starting Tilemap are saved.
    public List<GroundCellSaveData> groundChanges =
        new List<GroundCellSaveData>();

    // Scene pickups that were collected or partially collected.
    public List<PickupSaveData> pickups =
        new List<PickupSaveData>();

    public void EnsureValidCollections()
    {
        if (inventorySlots == null)
            inventorySlots = new List<InventorySlotSaveData>();

        if (groundChanges == null)
            groundChanges = new List<GroundCellSaveData>();

        if (pickups == null)
            pickups = new List<PickupSaveData>();
    }
}

[Serializable]
public class InventorySlotSaveData
{
    public string itemId;
    public int quantity;

    public InventorySlotSaveData()
    {
    }

    public InventorySlotSaveData(string itemId, int quantity)
    {
        this.itemId = itemId;
        this.quantity = quantity;
    }

    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(itemId) || quantity <= 0;
    }
}

[Serializable]
public class PickupSaveData
{
    public string entityId;
    public string itemId;
    public int quantity;
    public bool collected;

    public PickupSaveData()
    {
    }

    public PickupSaveData(
        string entityId,
        string itemId,
        int quantity,
        bool collected)
    {
        this.entityId = entityId;
        this.itemId = itemId;
        this.quantity = quantity;
        this.collected = collected;
    }
}