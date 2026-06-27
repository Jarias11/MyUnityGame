using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryPanelUI : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerInventory playerInventory;
	[SerializeField] private GameObject panelRoot;
	[SerializeField] private Transform slotParent;
	[SerializeField] private InventorySlotUI slotPrefab;

	[Header("Input")]
	[SerializeField] private Key toggleInventoryKey = Key.I;

	[Header("Startup")]
	[SerializeField] private bool startHidden = true;

	private readonly List<InventorySlotUI> slotViews = new List<InventorySlotUI>();
	private bool isOpen;

	private void Start()
	{
		if (playerInventory == null)
			playerInventory = FindAnyObjectByType<PlayerInventory>();

		if (panelRoot == null)
			panelRoot = gameObject;

		BuildSlots();

		if (playerInventory != null)
			playerInventory.OnInventoryChanged += Refresh;

		SetOpen(!startHidden);
		Refresh();
	}

	private void Update()
	{
		if (Keyboard.current == null)
			return;

		if (Keyboard.current[toggleInventoryKey].wasPressedThisFrame)
		{
			Toggle();
		}
	}

	private void OnDestroy()
	{
		if (playerInventory != null)
			playerInventory.OnInventoryChanged -= Refresh;
	}

	public void Toggle()
	{
		SetOpen(!isOpen);
	}

	public void SetOpen(bool open)
	{
		isOpen = open;

		if (panelRoot != null)
			panelRoot.SetActive(isOpen);

		if (isOpen)
			Refresh();
	}

	private void BuildSlots()
	{
		if (playerInventory == null || slotParent == null || slotPrefab == null)
			return;

		ClearOldSlotViews();

		for (int i = 0; i < playerInventory.InventorySize; i++)
		{
			InventorySlotUI newSlotView = Instantiate(slotPrefab, slotParent);
			newSlotView.Initialize(playerInventory, i, GetRootCanvas());
			slotViews.Add(newSlotView);
		}
	}

	private void Refresh()
	{
		if (playerInventory == null)
			return;

		if (slotViews.Count != playerInventory.InventorySize)
			BuildSlots();

		for (int i = 0; i < slotViews.Count; i++)
		{
			InventorySlot slot = playerInventory.GetSlot(i);

			// Full inventory does not need hotbar numbers or selected border.
			slotViews[i].SetSlot(slot);
		}
	}

	private void ClearOldSlotViews()
	{
		for (int i = slotParent.childCount - 1; i >= 0; i--)
		{
			Destroy(slotParent.GetChild(i).gameObject);
		}

		slotViews.Clear();
	}
	private Canvas GetRootCanvas()
	{
		Canvas canvas = GetComponentInParent<Canvas>();

		if (canvas == null && panelRoot != null)
			canvas = panelRoot.GetComponentInParent<Canvas>();

		return canvas;
	}
}