using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerTileInteractor : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerInventory playerInventory;
	[SerializeField] private Transform playerTransform;
	[SerializeField] private Camera worldCamera;

	[Header("Tilemaps")]
	[Tooltip("The tilemap that contains your grass, ground, and tilled tiles.")]
	[SerializeField] private Tilemap groundTilemap;

	[Tooltip("Optional tilemap used only to draw the hover selector.")]
	[SerializeField] private Tilemap hoverTilemap;

	[Header("Tile Selector")]
	[Tooltip("A simple transparent/outline tile that shows which cell the player is hovering.")]
	[SerializeField] private TileBase hoverTile;

	[Tooltip("If true, the hover tile stays on the last valid in-range tile when the mouse leaves range.")]
	[SerializeField] private bool keepLastHoverWhenOutOfRange = true;

	[Header("Ground Tiles")]
	[Tooltip("Tiles that count as grass. The shovel can turn these into ground.")]
	[SerializeField] private TileBase[] grassTiles;

	[Tooltip("The tile placed when grass becomes ground.")]
	[SerializeField] private TileBase groundTile;

	[Tooltip("Tiles that count as normal ground. The hoe can turn these into tilled ground.")]
	[SerializeField] private TileBase[] groundTiles;

	[Tooltip("The tile placed when ground becomes tilled.")]
	[SerializeField] private TileBase tilledTile;

	[Header("Interaction Settings")]
	[SerializeField] private float interactionRange = 2.5f;

	private Vector3Int hoveredCell;
	private Vector3Int highlightedCell;
	private Vector3Int lastUsedCellWhileHolding;

	private bool hasValidHoveredCell;
	private bool hasHighlightedCell;
	private bool hasUsedCellWhileHolding;

	private void Awake()
	{
		if (playerInventory == null)
			playerInventory = GetComponent<PlayerInventory>();

		if (playerTransform == null)
			playerTransform = transform;

		if (worldCamera == null)
			worldCamera = Camera.main;
	}

	private void Update()
	{
		UpdateHoveredCell();
		UpdateHoverVisual();
		HandleToolInput();
	}

	private void UpdateHoveredCell()
	{
		hasValidHoveredCell = false;

		if (worldCamera == null || groundTilemap == null || Mouse.current == null)
			return;

		Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

		Vector3 screenPosition = new Vector3(
			mouseScreenPosition.x,
			mouseScreenPosition.y,
			Mathf.Abs(worldCamera.transform.position.z)
		);

		Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
		worldPosition.z = 0f;

		Vector3Int mouseCell = groundTilemap.WorldToCell(worldPosition);

		TileBase tileAtMouse = groundTilemap.GetTile(mouseCell);

		if (tileAtMouse == null)
			return;

		if (!IsCellInRange(mouseCell))
			return;

		hoveredCell = mouseCell;
		hasValidHoveredCell = true;
	}

	private void UpdateHoverVisual()
	{
		if (hoverTilemap == null || hoverTile == null)
			return;

		if (!hasValidHoveredCell)
		{
			if (!keepLastHoverWhenOutOfRange)
				ClearHoverVisual();

			return;
		}

		if (hasHighlightedCell && highlightedCell != hoveredCell)
		{
			hoverTilemap.SetTile(highlightedCell, null);
		}

		hoverTilemap.SetTile(hoveredCell, hoverTile);
		highlightedCell = hoveredCell;
		hasHighlightedCell = true;
	}

	private void ClearHoverVisual()
	{
		if (!hasHighlightedCell || hoverTilemap == null)
			return;

		hoverTilemap.SetTile(highlightedCell, null);
		hasHighlightedCell = false;
	}

	private void HandleToolInput()
	{
		if (Mouse.current == null)
			return;

		if (Mouse.current.rightButton.isPressed)
		{
			TryUseSelectedToolWhileHolding();
		}
		else
		{
			hasUsedCellWhileHolding = false;
		}
	}

	private void TryUseSelectedToolWhileHolding()
	{
		if (!hasValidHoveredCell)
			return;

		if (hasUsedCellWhileHolding && lastUsedCellWhileHolding == hoveredCell)
			return;

		bool usedTool = TryUseSelectedToolOnCell(hoveredCell);

		lastUsedCellWhileHolding = hoveredCell;
		hasUsedCellWhileHolding = true;
	}

	private bool TryUseSelectedToolOnCell(Vector3Int targetCell)
	{
		if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
			return false;

		if (playerInventory == null)
			return false;

		if (!playerInventory.TryGetSelectedItem(out ItemData selectedItem))
			return false;

		if (selectedItem == null)
			return false;

		if (!selectedItem.CanBeUsedFromHotbar)
			return false;

		if (selectedItem.UseType != ItemUseType.ToolAction)
			return false;

		TileBase currentTile = groundTilemap.GetTile(targetCell);

		if (currentTile == null)
			return false;

		switch (selectedItem.ToolType)
		{
			case ToolType.Shovel:
				return TryUseShovel(targetCell, currentTile);

			case ToolType.Hoe:
				return TryUseHoe(targetCell, currentTile);
		}

		return false;
	}

	private bool TryUseShovel(Vector3Int targetCell, TileBase currentTile)
	{
		if (groundTile == null)
			return false;

		if (!TileIsInList(currentTile, grassTiles))
			return false;

		SetGroundTile(targetCell, groundTile);
		return true;
	}

	private bool TryUseHoe(Vector3Int targetCell, TileBase currentTile)
	{
		if (tilledTile == null)
			return false;

		if (!TileIsInList(currentTile, groundTiles))
			return false;

		SetGroundTile(targetCell, tilledTile);
		return true;
	}

	private void SetGroundTile(Vector3Int cell, TileBase newTile)
	{
		groundTilemap.SetTile(cell, newTile);
		RefreshCellAndNeighbors(cell);
	}

	private void RefreshCellAndNeighbors(Vector3Int cell)
	{
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				Vector3Int neighborCell = new Vector3Int(cell.x + x, cell.y + y, cell.z);
				groundTilemap.RefreshTile(neighborCell);
			}
		}
	}

	private bool IsCellInRange(Vector3Int cell)
	{
		Vector3 cellCenter = groundTilemap.GetCellCenterWorld(cell);
		float distance = Vector2.Distance(playerTransform.position, cellCenter);

		return distance <= interactionRange;
	}

	private bool TileIsInList(TileBase tile, TileBase[] tileList)
	{
		if (tile == null || tileList == null)
			return false;

		for (int i = 0; i < tileList.Length; i++)
		{
			if (tileList[i] == tile)
				return true;
		}

		return false;
	}
}