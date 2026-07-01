using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [SerializeField] private List<ItemData> items =
        new List<ItemData>();

    private Dictionary<string, ItemData> itemsById;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "A second ItemDatabase was found and will be ignored.",
                this);

            return;
        }

        Instance = this;
        BuildLookup();
    }

    public bool TryGetItem(string itemId, out ItemData item)
    {
        item = null;

        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        EnsureLookup();

        return itemsById.TryGetValue(itemId, out item);
    }

    public ItemData GetItem(string itemId)
    {
        ItemData item;
        TryGetItem(itemId, out item);
        return item;
    }

    [ContextMenu("Rebuild Item Lookup")]
    public void BuildLookup()
    {
        itemsById = new Dictionary<string, ItemData>();

        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];

            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(item.ItemId))
            {
                Debug.LogWarning(
                    "ItemDatabase found an item with an empty ItemId.",
                    item);

                continue;
            }

            if (itemsById.ContainsKey(item.ItemId))
            {
                Debug.LogWarning(
                    "Duplicate ItemId found in ItemDatabase: " +
                    item.ItemId +
                    ". The first item will be used.",
                    item);

                continue;
            }

            itemsById.Add(item.ItemId, item);
        }
    }

    private void EnsureLookup()
    {
        if (itemsById == null)
            BuildLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (items == null)
            items = new List<ItemData>();
    }
#endif
}