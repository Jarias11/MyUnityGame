using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Transform slotParent;
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Input")]
    [SerializeField] private bool useScrollWheel = true;
    [SerializeField] private bool useNumberKeys = true;

    [Tooltip("Scroll up usually moves left/previous. Turn this on if it feels backwards.")]
    [SerializeField] private bool invertScrollDirection = false;

    private readonly List<InventorySlotUI> hotbarSlotViews =
        new List<InventorySlotUI>();

    private void Start()
    {
        if (playerInventory == null)
            playerInventory = FindAnyObjectByType<PlayerInventory>();

        BuildSlots();

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += Refresh;
            playerInventory.OnHotbarSelectionChanged += RefreshSelection;

            playerInventory.SelectHotbarIndex(
                playerInventory.SelectedHotbarIndex);
        }

        Refresh();
    }

    private void Update()
    {
        HandleScrollWheelInput();
        HandleNumberKeyInput();
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= Refresh;
            playerInventory.OnHotbarSelectionChanged -= RefreshSelection;
        }
    }

    private void HandleScrollWheelInput()
    {
        if (!useScrollWheel)
            return;

        if (playerInventory == null)
            return;

        if (Mouse.current == null)
            return;

        float scrollY = Mouse.current.scroll.ReadValue().y;

        if (scrollY > 0f)
        {
            if (invertScrollDirection)
                playerInventory.SelectNextHotbarSlot();
            else
                playerInventory.SelectPreviousHotbarSlot();
        }
        else if (scrollY < 0f)
        {
            if (invertScrollDirection)
                playerInventory.SelectPreviousHotbarSlot();
            else
                playerInventory.SelectNextHotbarSlot();
        }
    }

    private void HandleNumberKeyInput()
    {
        if (!useNumberKeys)
            return;

        if (playerInventory == null)
            return;

        if (Keyboard.current == null)
            return;

        int maxNumber =
            Mathf.Min(playerInventory.HotbarSize, 9);

        for (int number = 1; number <= maxNumber; number++)
        {
            if (WasNumberPressed(number))
            {
                playerInventory.SelectHotbarIndex(number - 1);
                return;
            }
        }
    }

    private bool WasNumberPressed(int number)
    {
        Key key = number switch
        {
            1 => Key.Digit1,
            2 => Key.Digit2,
            3 => Key.Digit3,
            4 => Key.Digit4,
            5 => Key.Digit5,
            6 => Key.Digit6,
            7 => Key.Digit7,
            8 => Key.Digit8,
            9 => Key.Digit9,
            _ => Key.None
        };

        if (key == Key.None)
            return false;

        return Keyboard.current[key].wasPressedThisFrame;
    }

    private void BuildSlots()
    {
        if (playerInventory == null ||
            slotParent == null ||
            slotPrefab == null)
        {
            return;
        }

        ClearOldSlotViews();

        Canvas rootCanvas = GetRootCanvas();

        for (int i = 0; i < playerInventory.HotbarSize; i++)
        {
            InventorySlotUI newSlotView =
                Instantiate(slotPrefab, slotParent);

            newSlotView.Initialize(
                playerInventory,
                i,
                rootCanvas);

            hotbarSlotViews.Add(newSlotView);
        }
    }

    private void Refresh()
    {
        if (playerInventory == null)
            return;

        if (hotbarSlotViews.Count != playerInventory.HotbarSize)
            BuildSlots();

        for (int i = 0; i < hotbarSlotViews.Count; i++)
        {
            InventorySlot slot = playerInventory.GetSlot(i);
            bool isSelected =
                i == playerInventory.SelectedHotbarIndex;

            hotbarSlotViews[i].SetSlot(
                slot,
                i + 1,
                isSelected);
        }
    }

    private void RefreshSelection(int selectedIndex)
    {
        for (int i = 0; i < hotbarSlotViews.Count; i++)
        {
            hotbarSlotViews[i].SetSelected(i == selectedIndex);
        }
    }

    private void ClearOldSlotViews()
    {
        if (slotParent == null)
            return;

        for (int i = slotParent.childCount - 1; i >= 0; i--)
        {
            Destroy(slotParent.GetChild(i).gameObject);
        }

        hotbarSlotViews.Clear();
    }

    private Canvas GetRootCanvas()
    {
        return GetComponentInParent<Canvas>();
    }
}