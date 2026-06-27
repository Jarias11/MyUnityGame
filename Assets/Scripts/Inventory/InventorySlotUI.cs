using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text hotbarNumberText;
    [SerializeField] private GameObject selectedFrame;

    [Header("Drag Settings")]
    [SerializeField] private bool canDrag = true;

    private PlayerInventory playerInventory;
    private int slotIndex = -1;
    private Canvas rootCanvas;

    private GameObject dragIconObject;
    private RectTransform dragIconRect;

    public void Initialize(PlayerInventory inventory, int index, Canvas canvas)
    {
        playerInventory = inventory;
        slotIndex = index;
        rootCanvas = canvas;
    }

    private void Awake()
    {
        // This helps stop the selected frame from covering/tinting the icon.
        if (selectedFrame != null)
            selectedFrame.transform.SetAsFirstSibling();

        // The icon itself should not block drop detection.
        if (iconImage != null)
            iconImage.raycastTarget = false;

        if (quantityText != null)
            quantityText.raycastTarget = false;

        if (hotbarNumberText != null)
            hotbarNumberText.raycastTarget = false;
    }

    public void SetSlot(InventorySlot slot, int hotbarNumber = -1, bool isSelected = false)
    {
        SetHotbarNumber(hotbarNumber);
        SetSelected(isSelected);

        if (slot == null || slot.IsEmpty)
        {
            Clear();
            return;
        }

        if (iconImage != null)
        {
            iconImage.enabled = slot.Item.Icon != null;
            iconImage.sprite = slot.Item.Icon;
            iconImage.preserveAspect = true;
        }

        if (quantityText != null)
        {
            bool shouldShowQuantity = slot.Item.Stackable && slot.Quantity > 1;
            quantityText.text = shouldShowQuantity ? slot.Quantity.ToString() : "";
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedFrame != null)
            selectedFrame.SetActive(isSelected);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;

        if (playerInventory == null)
            return;

        InventorySlot slot = playerInventory.GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty)
            return;

        CreateDragIcon(slot.Item.Icon, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIconRect == null)
            return;

        dragIconRect.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DestroyDragIcon();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (playerInventory == null)
            return;

        InventorySlotUI draggedSlot = eventData.pointerDrag.GetComponentInParent<InventorySlotUI>();

        if (draggedSlot == null)
            return;

        if (draggedSlot == this)
            return;

        playerInventory.MoveSlot(draggedSlot.slotIndex, slotIndex);
    }

    private void CreateDragIcon(Sprite sprite, PointerEventData eventData)
    {
        if (rootCanvas == null || sprite == null)
            return;

        dragIconObject = new GameObject("Dragged Item Icon");
        dragIconObject.transform.SetParent(rootCanvas.transform, false);
        dragIconObject.transform.SetAsLastSibling();

        dragIconRect = dragIconObject.AddComponent<RectTransform>();
        dragIconRect.sizeDelta = new Vector2(64f, 64f);
        dragIconRect.position = eventData.position;

        Image dragImage = dragIconObject.AddComponent<Image>();
        dragImage.sprite = sprite;
        dragImage.preserveAspect = true;

        // Very important: the dragged icon should not block the slot underneath it.
        dragImage.raycastTarget = false;
    }

    private void DestroyDragIcon()
    {
        if (dragIconObject != null)
            Destroy(dragIconObject);

        dragIconObject = null;
        dragIconRect = null;
    }

    private void SetHotbarNumber(int hotbarNumber)
    {
        if (hotbarNumberText == null)
            return;

        hotbarNumberText.text = hotbarNumber > 0 ? hotbarNumber.ToString() : "";
    }

    private void Clear()
    {
        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }

        if (quantityText != null)
            quantityText.text = "";
    }
}