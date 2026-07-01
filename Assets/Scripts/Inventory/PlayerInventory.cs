using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    private class StartingItem
    {
        public ItemData item;

        [Min(1)]
        public int quantity = 1;
    }

    [Header("Inventory Settings")]
    [Min(1)]
    [SerializeField] private int inventorySize = 24;

    [Min(1)]
    [SerializeField] private int hotbarSize = 9;

    [SerializeField] private List<InventorySlot> inventorySlots =
        new List<InventorySlot>();

    [Header("Saving")]
    [SerializeField] private bool saveInventory = true;
    [SerializeField] private bool loadSavedInventoryOnAwake = true;
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Hotbar")]
    [SerializeField] private int selectedHotbarIndex = 0;

    [Header("Testing / Starting Items")]
    [Tooltip("Used when no saved inventory exists yet.")]
    [SerializeField] private bool addStartingItemsOnAwake = true;

    [SerializeField] private List<StartingItem> startingItems =
        new List<StartingItem>();

    private bool startingItemsAdded;
    private bool suppressSave;

    public event Action OnInventoryChanged;
    public event Action<int> OnHotbarSelectionChanged;

    public IReadOnlyList<InventorySlot> Slots => inventorySlots;

    public int InventorySize => inventorySize;
    public int HotbarSize => hotbarSize;
    public int SelectedHotbarIndex => selectedHotbarIndex;

    public InventorySlot SelectedHotbarSlot => GetSlot(selectedHotbarIndex);

    public ItemData SelectedItem
    {
        get
        {
            InventorySlot slot = SelectedHotbarSlot;
            return slot == null || slot.IsEmpty ? null : slot.Item;
        }
    }

    private void Awake()
    {
        suppressSave = true;

        EnsureInventorySize();

        selectedHotbarIndex =
            Mathf.Clamp(selectedHotbarIndex, 0, hotbarSize - 1);

        bool loadedSavedInventory = false;

        if (loadSavedInventoryOnAwake)
            loadedSavedInventory = TryLoadSavedInventory();

        if (!loadedSavedInventory && addStartingItemsOnAwake)
            AddStartingItems();

        selectedHotbarIndex =
            Mathf.Clamp(selectedHotbarIndex, 0, hotbarSize - 1);

        suppressSave = false;

        OnInventoryChanged?.Invoke();
        OnHotbarSelectionChanged?.Invoke(selectedHotbarIndex);

        if (saveInventory)
            SaveInventoryToSaveManager();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (saveInventory)
            SaveInventoryToSaveManager();
    }

    private void OnValidate()
    {
        inventorySize = Mathf.Max(1, inventorySize);
        hotbarSize = Mathf.Clamp(hotbarSize, 1, inventorySize);
        selectedHotbarIndex =
            Mathf.Clamp(selectedHotbarIndex, 0, hotbarSize - 1);

        EnsureInventorySize();
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        bool addedEverything =
            TryAddItem(item, amount, out int remainingAmount);

        if (!addedEverything && item != null)
        {
            Debug.Log(
                "Inventory could not fit " +
                remainingAmount + "x " +
                item.DisplayName + ".");
        }

        return addedEverything;
    }

    public bool TryAddItem(
        ItemData item,
        int amount,
        out int remainingAmount)
    {
        remainingAmount = amount;

        if (item == null || amount <= 0)
            return false;

        EnsureInventorySize();

        int originalRemainingAmount = remainingAmount;

        // 1. Fill existing stacks first.
        if (item.Stackable)
        {
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                if (remainingAmount <= 0)
                    break;

                InventorySlot slot = inventorySlots[i];

                if (!slot.IsEmpty && slot.Item == item)
                {
                    int added =
                        slot.AddAmount(item, remainingAmount);

                    remainingAmount -= added;
                }
            }
        }

        // 2. Put leftovers into empty slots.
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (remainingAmount <= 0)
                break;

            InventorySlot slot = inventorySlots[i];

            if (slot.IsEmpty)
            {
                int added =
                    slot.AddAmount(item, remainingAmount);

                remainingAmount -= added;
            }
        }

        if (remainingAmount != originalRemainingAmount)
            NotifyInventoryChanged();

        return remainingAmount <= 0;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (CountItem(item) < amount)
            return false;

        int remainingAmount = amount;

        // Remove from the back first so hotbar items are preserved when possible.
        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            if (remainingAmount <= 0)
                break;

            InventorySlot slot = inventorySlots[i];

            if (!slot.IsEmpty && slot.Item == item)
            {
                int removed =
                    slot.RemoveAmount(remainingAmount);

                remainingAmount -= removed;
            }
        }

        NotifyInventoryChanged();
        return true;
    }

    public bool RemoveFromSlot(int slotIndex, int amount = 1)
    {
        if (!IsValidSlotIndex(slotIndex) || amount <= 0)
            return false;

        InventorySlot slot = inventorySlots[slotIndex];

        if (slot.IsEmpty)
            return false;

        int removed = slot.RemoveAmount(amount);

        if (removed > 0)
        {
            NotifyInventoryChanged();
            return true;
        }

        return false;
    }

    public int CountItem(ItemData item)
    {
        if (item == null)
            return 0;

        int count = 0;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlot slot = inventorySlots[i];

            if (!slot.IsEmpty && slot.Item == item)
                count += slot.Quantity;
        }

        return count;
    }

    public bool HasItem(ItemData item, int amount = 1)
    {
        return CountItem(item) >= amount;
    }

    public bool MoveSlot(int fromIndex, int toIndex)
    {
        if (!IsValidSlotIndex(fromIndex) ||
            !IsValidSlotIndex(toIndex))
        {
            return false;
        }

        if (fromIndex == toIndex)
            return false;

        InventorySlot fromSlot = inventorySlots[fromIndex];
        InventorySlot toSlot = inventorySlots[toIndex];

        if (fromSlot.IsEmpty)
            return false;

        // Move into empty slot.
        if (toSlot.IsEmpty)
        {
            toSlot.Set(fromSlot.Item, fromSlot.Quantity);
            fromSlot.Clear();

            NotifyInventoryChanged();
            return true;
        }

        // Merge stacks if same item.
        if (toSlot.Item == fromSlot.Item &&
            toSlot.Item.Stackable)
        {
            int added =
                toSlot.AddAmount(
                    fromSlot.Item,
                    fromSlot.Quantity);

            if (added <= 0)
                return false;

            fromSlot.RemoveAmount(added);

            NotifyInventoryChanged();
            return true;
        }

        // Otherwise swap.
        SwapSlots(fromIndex, toIndex);
        return true;
    }

    public bool SwapSlots(int firstIndex, int secondIndex)
    {
        if (!IsValidSlotIndex(firstIndex) ||
            !IsValidSlotIndex(secondIndex))
        {
            return false;
        }

        if (firstIndex == secondIndex)
            return false;

        InventorySlot firstSlot = inventorySlots[firstIndex];
        InventorySlot secondSlot = inventorySlots[secondIndex];

        InventorySlot temp = firstSlot.Clone();

        firstSlot.Set(secondSlot.Item, secondSlot.Quantity);
        secondSlot.Set(temp.Item, temp.Quantity);

        NotifyInventoryChanged();
        return true;
    }

    public bool SelectHotbarIndex(int index)
    {
        if (index < 0 || index >= hotbarSize)
            return false;

        if (selectedHotbarIndex == index)
            return true;

        selectedHotbarIndex = index;
        NotifyHotbarSelectionChanged();

        return true;
    }

    public bool SelectNextHotbarSlot()
    {
        int nextIndex = selectedHotbarIndex + 1;

        if (nextIndex >= hotbarSize)
            nextIndex = 0;

        return SelectHotbarIndex(nextIndex);
    }

    public bool SelectPreviousHotbarSlot()
    {
        int previousIndex = selectedHotbarIndex - 1;

        if (previousIndex < 0)
            previousIndex = hotbarSize - 1;

        return SelectHotbarIndex(previousIndex);
    }

    public InventorySlot GetSlot(int index)
    {
        if (!IsValidSlotIndex(index))
            return null;

        return inventorySlots[index];
    }

    public bool TryGetSelectedItem(out ItemData item)
    {
        item = SelectedItem;
        return item != null;
    }

    public int FindFirstSlotContaining(ItemData item)
    {
        if (item == null)
            return -1;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlot slot = inventorySlots[i];

            if (!slot.IsEmpty && slot.Item == item)
                return i;
        }

        return -1;
    }

    public void ClearInventory()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            inventorySlots[i].Clear();
        }

        NotifyInventoryChanged();
    }

    public void SaveInventoryToSaveManager()
    {
        if (!saveInventory)
            return;

        List<InventorySlotSaveData> slotSaveData =
            new List<InventorySlotSaveData>();

        EnsureInventorySize();

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlot slot = inventorySlots[i];

            if (slot == null || slot.IsEmpty)
            {
                slotSaveData.Add(
                    new InventorySlotSaveData("", 0));
            }
            else
            {
                slotSaveData.Add(
                    new InventorySlotSaveData(
                        slot.Item.ItemId,
                        slot.Quantity));
            }
        }

        SaveManager.SaveInventory(
            slotSaveData,
            selectedHotbarIndex);
    }

    private bool TryLoadSavedInventory()
    {
        GameSaveData saveData = SaveManager.LoadGame();

        if (saveData.inventorySlots == null ||
            saveData.inventorySlots.Count == 0)
        {
            return false;
        }

        ItemDatabase database = ResolveItemDatabase();

        inventorySize =
            Mathf.Max(inventorySize, saveData.inventorySlots.Count);

        hotbarSize = Mathf.Clamp(hotbarSize, 1, inventorySize);

        EnsureInventorySize();

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            inventorySlots[i].Clear();
        }

        for (int i = 0; i < saveData.inventorySlots.Count; i++)
        {
            if (!IsValidSlotIndex(i))
                break;

            InventorySlotSaveData savedSlot =
                saveData.inventorySlots[i];

            if (savedSlot == null || savedSlot.IsEmpty())
            {
                inventorySlots[i].Clear();
                continue;
            }

            if (database == null)
            {
                Debug.LogWarning(
                    "Cannot load saved inventory because no ItemDatabase was found.",
                    this);

                continue;
            }

            ItemData item;

            if (!database.TryGetItem(savedSlot.itemId, out item))
            {
                Debug.LogWarning(
                    "Saved inventory item could not be found in ItemDatabase: " +
                    savedSlot.itemId,
                    this);

                continue;
            }

            inventorySlots[i].Set(item, savedSlot.quantity);
        }

        selectedHotbarIndex =
            Mathf.Clamp(
                saveData.selectedHotbarIndex,
                0,
                hotbarSize - 1);

        return true;
    }

    private ItemDatabase ResolveItemDatabase()
    {
        if (itemDatabase != null)
            return itemDatabase;

        if (ItemDatabase.Instance != null)
        {
            itemDatabase = ItemDatabase.Instance;
            return itemDatabase;
        }

        itemDatabase = FindAnyObjectByType<ItemDatabase>();
        return itemDatabase;
    }

    private bool IsValidSlotIndex(int index)
    {
        return index >= 0 && index < inventorySlots.Count;
    }

    private void EnsureInventorySize()
    {
        if (inventorySlots == null)
            inventorySlots = new List<InventorySlot>();

        while (inventorySlots.Count < inventorySize)
        {
            inventorySlots.Add(new InventorySlot());
        }

        while (inventorySlots.Count > inventorySize)
        {
            inventorySlots.RemoveAt(inventorySlots.Count - 1);
        }

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] == null)
                inventorySlots[i] = new InventorySlot();
        }
    }

    private void AddStartingItems()
    {
        if (startingItemsAdded)
            return;

        startingItemsAdded = true;

        for (int i = 0; i < startingItems.Count; i++)
        {
            StartingItem startingItem = startingItems[i];

            if (startingItem == null || startingItem.item == null)
                continue;

            AddItem(startingItem.item, startingItem.quantity);
        }
    }

    private void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();

        if (!suppressSave && saveInventory)
            SaveInventoryToSaveManager();
    }

    private void NotifyHotbarSelectionChanged()
    {
        OnHotbarSelectionChanged?.Invoke(selectedHotbarIndex);

        if (!suppressSave && saveInventory)
            SaveInventoryToSaveManager();
    }
}