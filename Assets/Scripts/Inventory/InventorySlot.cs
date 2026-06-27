using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    [SerializeField] private ItemData item;
    [SerializeField] private int quantity;

    public ItemData Item => item;
    public int Quantity => quantity;

    public bool IsEmpty => item == null || quantity <= 0;

    public bool IsFull
    {
        get
        {
            if (IsEmpty)
                return false;

            return quantity >= item.MaxStackSize;
        }
    }

    public InventorySlot()
    {
        Clear();
    }

    public InventorySlot(ItemData startingItem, int startingQuantity)
    {
        Set(startingItem, startingQuantity);
    }

    public bool CanAcceptItem(ItemData newItem)
    {
        if (newItem == null)
            return false;

        if (IsEmpty)
            return true;

        if (item != newItem)
            return false;

        if (!item.Stackable)
            return false;

        return quantity < item.MaxStackSize;
    }

    public int GetSpaceFor(ItemData newItem)
    {
        if (newItem == null)
            return 0;

        if (IsEmpty)
            return newItem.MaxStackSize;

        if (item != newItem)
            return 0;

        if (!item.Stackable)
            return 0;

        return item.MaxStackSize - quantity;
    }

    public int AddAmount(ItemData newItem, int amount)
    {
        if (newItem == null || amount <= 0)
            return 0;

        if (!CanAcceptItem(newItem))
            return 0;

        if (IsEmpty)
        {
            item = newItem;
            quantity = 0;
        }

        int amountThatFits = Mathf.Min(amount, item.MaxStackSize - quantity);
        quantity += amountThatFits;

        return amountThatFits;
    }

    public int RemoveAmount(int amount)
    {
        if (IsEmpty || amount <= 0)
            return 0;

        int amountToRemove = Mathf.Min(amount, quantity);
        quantity -= amountToRemove;

        if (quantity <= 0)
            Clear();

        return amountToRemove;
    }

    public void Set(ItemData newItem, int newQuantity)
    {
        if (newItem == null || newQuantity <= 0)
        {
            Clear();
            return;
        }

        item = newItem;
        quantity = Mathf.Clamp(newQuantity, 1, newItem.MaxStackSize);
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
    }

    public InventorySlot Clone()
    {
        return new InventorySlot(item, quantity);
    }
}