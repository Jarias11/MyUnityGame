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

    [Header("Tile Selector")]
    [SerializeField] private Tilemap hoverTilemap;
    [SerializeField] private TileBase hoverTile;

    [Tooltip("If true, the hover tile stays on the last valid in-range tile when the mouse leaves range.")]
    [SerializeField] private bool keepLastHoverWhenOutOfRange = true;

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

    private void OnDisable()
    {
        ClearHoverVisual();
    }

    private void UpdateHoveredCell()
    {
        hasValidHoveredCell = false;

        GroundManager groundManager = GroundManager.Instance;

        if (groundManager == null || !groundManager.IsInitialized)
            return;

        if (worldCamera == null || Mouse.current == null)
            return;

        Vector2 mouseScreenPosition =
            Mouse.current.position.ReadValue();

        Vector3 screenPosition = new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            Mathf.Abs(worldCamera.transform.position.z));

        Vector3 worldPosition =
            worldCamera.ScreenToWorldPoint(screenPosition);

        worldPosition.z = 0f;

        Vector3Int mouseCell =
            groundManager.WorldToCell(worldPosition);

        if (!groundManager.HasGroundCell(mouseCell))
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

        if (hasUsedCellWhileHolding &&
            lastUsedCellWhileHolding == hoveredCell)
        {
            return;
        }

        TryUseSelectedToolOnCell(hoveredCell);

        lastUsedCellWhileHolding = hoveredCell;
        hasUsedCellWhileHolding = true;
    }

    private bool TryUseSelectedToolOnCell(Vector3Int targetCell)
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        if (playerInventory == null)
            return false;

        if (!playerInventory.TryGetSelectedItem(
                out ItemData selectedItem))
        {
            return false;
        }

        if (selectedItem == null)
            return false;

        if (!selectedItem.CanBeUsedFromHotbar)
            return false;

        if (selectedItem.UseType != ItemUseType.ToolAction)
            return false;

        GroundManager groundManager = GroundManager.Instance;

        if (groundManager == null || !groundManager.IsInitialized)
            return false;

        GroundType currentGroundType;

        if (!groundManager.TryGetGroundType(
                targetCell,
                out currentGroundType))
        {
            return false;
        }

        switch (selectedItem.ToolType)
        {
            case ToolType.Shovel:
                return TryUseShovel(
                    groundManager,
                    targetCell,
                    currentGroundType);

            case ToolType.Hoe:
                return TryUseHoe(
                    groundManager,
                    targetCell,
                    currentGroundType);

            case ToolType.WateringCan:
                return TryUseWateringCan(
                    groundManager,
                    targetCell,
                    currentGroundType);
        }

        return false;
    }

    private bool TryUseShovel(
        GroundManager groundManager,
        Vector3Int targetCell,
        GroundType currentGroundType)
    {
        if (currentGroundType != GroundType.Grass)
            return false;

        return groundManager.TrySetGroundType(
            targetCell,
            GroundType.Ground,
            true);
    }

    private bool TryUseHoe(
        GroundManager groundManager,
        Vector3Int targetCell,
        GroundType currentGroundType)
    {
        if (currentGroundType != GroundType.Ground)
            return false;

        return groundManager.TrySetGroundType(
            targetCell,
            GroundType.TilledDry,
            true);
    }

    private bool TryUseWateringCan(
        GroundManager groundManager,
        Vector3Int targetCell,
        GroundType currentGroundType)
    {
        if (currentGroundType != GroundType.TilledDry)
            return false;

        // Crops should not block watering, but buildings/rocks still should.
        return groundManager.TrySetGroundType(
            targetCell,
            GroundType.TilledWatered,
            true,
            true);
    }

    private bool IsCellInRange(Vector3Int cell)
    {
        GroundManager groundManager = GroundManager.Instance;

        if (groundManager == null || playerTransform == null)
            return false;

        Vector3 cellCenter =
            groundManager.GetCellCenterWorld(cell);

        float distance =
            Vector2.Distance(
                playerTransform.position,
                cellCenter);

        return distance <= interactionRange;
    }
}