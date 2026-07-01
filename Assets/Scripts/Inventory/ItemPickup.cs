using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(EntityId))]
public class ItemPickup : MonoBehaviour
{
    [Header("Pickup Data")]
    [SerializeField] private ItemData itemData;

    [Min(1)]
    [SerializeField] private int quantity = 1;

    [Header("Pickup Settings")]
    [SerializeField] private bool pickupOnTouch = false;
    [SerializeField] private Key interactKey = Key.E;

    [Tooltip("Optional object like a small 'Press E' text above the item.")]
    [SerializeField] private GameObject pickupPrompt;

    [Header("Saving")]
    [SerializeField] private bool savePickupState = true;

    [Header("Visual Helper")]
    [Tooltip("If true, this will try to use the ItemData icon as this object's SpriteRenderer sprite.")]
    [SerializeField] private bool useItemIconAsSprite = true;

    private PlayerInventory nearbyInventory;
    private SpriteRenderer spriteRenderer;
    private EntityId entityId;

    private string SaveId
    {
        get
        {
            if (entityId == null)
                entityId = GetComponent<EntityId>();

            if (entityId == null)
                return "";

            return entityId.Id;
        }
    }

    private void Awake()
    {
        entityId = GetComponent<EntityId>();

        if (entityId != null)
            entityId.EnsureId();

        spriteRenderer = GetComponent<SpriteRenderer>();

        Collider2D pickupCollider = GetComponent<Collider2D>();
        pickupCollider.isTrigger = true;

        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);

        RefreshVisual();
    }

    private void Start()
    {
        LoadSavedPickupState();
    }

    private void Update()
    {
        if (pickupOnTouch)
            return;

        if (nearbyInventory == null)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[interactKey].wasPressedThisFrame)
        {
            TryPickup(nearbyInventory);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInventory inventory =
            other.GetComponentInParent<PlayerInventory>();

        if (inventory == null)
            return;

        nearbyInventory = inventory;

        if (pickupPrompt != null)
            pickupPrompt.SetActive(true);

        if (pickupOnTouch)
            TryPickup(inventory);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerInventory inventory =
            other.GetComponentInParent<PlayerInventory>();

        if (inventory == null)
            return;

        if (inventory == nearbyInventory)
        {
            nearbyInventory = null;

            if (pickupPrompt != null)
                pickupPrompt.SetActive(false);
        }
    }

    public void TryPickup(PlayerInventory inventory)
    {
        if (inventory == null)
            return;

        if (itemData == null)
        {
            Debug.LogWarning(
                name + " has no ItemData assigned.",
                this);

            return;
        }

        bool addedEverything =
            inventory.TryAddItem(
                itemData,
                quantity,
                out int remainingAmount);

        if (addedEverything)
        {
            MarkCollectedAndDestroy();
            return;
        }

        // Partial pickup: some fit, some stayed.
        if (remainingAmount < quantity)
        {
            quantity = remainingAmount;

            SaveRemainingQuantity();

            Debug.Log(
                "Picked up some " +
                itemData.DisplayName +
                ". " +
                quantity +
                " left on the ground.",
                this);
        }
        else
        {
            Debug.Log(
                "Inventory is full. Could not pick up " +
                itemData.DisplayName + ".",
                this);
        }
    }

    private void LoadSavedPickupState()
    {
        if (!savePickupState)
            return;

        if (string.IsNullOrWhiteSpace(SaveId))
            return;

        PickupSaveData pickupState;

        if (!SaveManager.TryGetPickupState(
                SaveId,
                out pickupState))
        {
            return;
        }

        if (pickupState.collected)
        {
            Destroy(gameObject);
            return;
        }

        if (pickupState.quantity > 0)
            quantity = pickupState.quantity;
    }

    private void MarkCollectedAndDestroy()
    {
        if (savePickupState &&
            !string.IsNullOrWhiteSpace(SaveId))
        {
            SaveManager.MarkPickupCollected(
                SaveId,
                itemData != null ? itemData.ItemId : "");
        }

        Destroy(gameObject);
    }

    private void SaveRemainingQuantity()
    {
        if (!savePickupState)
            return;

        if (string.IsNullOrWhiteSpace(SaveId))
            return;

        SaveManager.SavePickupState(
            SaveId,
            itemData != null ? itemData.ItemId : "",
            quantity,
            false);
    }

    private void RefreshVisual()
    {
        if (!useItemIconAsSprite)
            return;

        if (spriteRenderer == null)
            return;

        if (itemData == null)
            return;

        if (itemData.Icon == null)
            return;

        spriteRenderer.sprite = itemData.Icon;
    }

    private void Reset()
    {
        Collider2D pickupCollider = GetComponent<Collider2D>();

        if (pickupCollider != null)
            pickupCollider.isTrigger = true;

        EntityId id = GetComponent<EntityId>();

        if (id != null)
            id.EnsureId();
    }

    private void OnValidate()
    {
        quantity = Mathf.Max(1, quantity);
    }
}