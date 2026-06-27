using System;
using System.Text;
using UnityEngine;

[Flags]
public enum ItemCategory
{
    None = 0,

    Tool = 1 << 0,
    Resource = 1 << 1,
    Food = 1 << 2,
    Armor = 1 << 3,
    Machine = 1 << 4,
    PowerUp = 1 << 5,
    Seed = 1 << 6,
    Weapon = 1 << 7,
    Furniture = 1 << 8,
    Quest = 1 << 9,
    Currency = 1 << 10,
    Material = 1 << 11,
    Placeable = 1 << 12,
    Consumable = 1 << 13,
    Misc = 1 << 14
}

public enum ToolType
{
    None,
    Hoe,
    Axe,
    Pickaxe,
    WateringCan,
    FishingRod,
    Hammer,
    Sickle,
    Shovel
}

public enum EquipmentSlot
{
    None,
    Head,
    Chest,
    Legs,
    Feet,
    Hands,
    Accessory,
    Weapon,
    Shield
}

public enum ItemUseType
{
    None,
    ToolAction,
    Eat,
    Equip,
    Place,
    Consume,
    Open
}

[CreateAssetMenu(fileName = "New Item Data", menuName = "New Unity Game/Items/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique save ID. Once this item exists in a save file, do not rename this ID.")]
    [SerializeField] private string itemId = "new_item";

    [SerializeField] private string displayName = "New Item";

    [TextArea(2, 5)]
    [SerializeField] private string description;

    [Header("Visuals")]
    [SerializeField] private Sprite icon;

    [Tooltip("Optional prefab used if this item is dropped into the world.")]
    [SerializeField] private GameObject worldDropPrefab;

    [Tooltip("Optional prefab used if this item can be placed, like a machine, chest, furnace, or furniture.")]
    [SerializeField] private GameObject placeablePrefab;

    [Header("Classification")]
    [SerializeField] private ItemCategory categories = ItemCategory.Misc;
    [SerializeField] private ToolType toolType = ToolType.None;
    [SerializeField] private EquipmentSlot equipmentSlot = EquipmentSlot.None;
    [SerializeField] private ItemUseType useType = ItemUseType.None;

    [Header("Inventory")]
    [SerializeField] private bool stackable = true;

    [Min(1)]
    [SerializeField] private int maxStackSize = 99;

    [SerializeField] private bool canBeDropped = true;
    [SerializeField] private bool canBeUsedFromHotbar = true;

    [Header("Item Values")]
    [Min(0)]
    [SerializeField] private int sellValue = 0;

    [Header("Food / Consumable Stats")]
    [Min(0)]
    [SerializeField] private int healthRestore = 0;

    [Min(0)]
    [SerializeField] private int staminaRestore = 0;

    [Header("Tool / Equipment / Power Stats")]
    [Min(0)]
    [SerializeField] private int power = 0;

    [Min(0f)]
    [SerializeField] private float effectDuration = 0f;

    [Header("Future Expansion")]
    [Tooltip("Optional labels like: ore, wood, crop, cooked, rare, magical, fuel, metal.")]
    [SerializeField] private string[] tags;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public string Description => description;

    public Sprite Icon => icon;
    public GameObject WorldDropPrefab => worldDropPrefab;
    public GameObject PlaceablePrefab => placeablePrefab;

    public ItemCategory Categories => categories;
    public ToolType ToolType => toolType;
    public EquipmentSlot EquipmentSlot => equipmentSlot;
    public ItemUseType UseType => useType;

    public bool Stackable => stackable;
    public int MaxStackSize => stackable ? maxStackSize : 1;

    public bool CanBeDropped => canBeDropped;
    public bool CanBeUsedFromHotbar => canBeUsedFromHotbar;

    public int SellValue => sellValue;
    public int HealthRestore => healthRestore;
    public int StaminaRestore => staminaRestore;
    public int Power => power;
    public float EffectDuration => effectDuration;

    public string[] Tags => tags;

    public bool HasCategory(ItemCategory category)
    {
        return (categories & category) == category;
    }

    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || tags == null)
            return false;

        for (int i = 0; i < tags.Length; i++)
        {
            if (string.Equals(tags[i], tag, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemId))
            itemId = MakeSafeId(name);

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        maxStackSize = Mathf.Max(1, maxStackSize);

        if (!stackable)
            maxStackSize = 1;

        if (categories == ItemCategory.None)
            categories = ItemCategory.Misc;
    }

    private string MakeSafeId(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "new_item";

        StringBuilder builder = new StringBuilder();

        foreach (char c in source)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
            else if (c == ' ' || c == '-' || c == '_')
            {
                builder.Append('_');
            }
        }

        string result = builder.ToString().Trim('_');

        return string.IsNullOrWhiteSpace(result) ? "new_item" : result;
    }
}