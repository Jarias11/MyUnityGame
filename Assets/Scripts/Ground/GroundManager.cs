using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Owns the gameplay state for every recognized cell on the painted Ground Tilemap.
///
/// The Tilemap painted in the Scene is the starting map.
/// Save data contains only cells that changed from that starting map.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GroundManager : MonoBehaviour
{
    public static GroundManager Instance { get; private set; }

    [Header("Required References")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private GroundTileSet groundTileSet;

    [Header("Optional Edit Restrictions")]
    [Tooltip(
        "Optional hidden marker Tilemap. Any cell containing any marker Tile " +
        "is considered permanently non-editable.")]
    [SerializeField] private Tilemap nonEditableMarkerTilemap;

    [SerializeField] private bool hideRestrictionMarkersDuringPlay = true;

    [Header("Unused Tilled Soil")]
    [SerializeField] private bool revertUnusedTilledSoil = true;

    [Min(1)]
    [SerializeField] private int unusedDaysBeforeReturningToGround = 3;

    [Header("Saving")]
    [SerializeField] private bool saveOnDisable = true;

    public bool IsInitialized { get; private set; }

    private sealed class GroundCellRuntime
    {
        public Vector3Int Position;
        public GroundType InitialType;
        public GroundType CurrentType;
        public bool BaseEditable;
        public int DaysUnused;

        public GroundCellRuntime(
            Vector3Int position,
            GroundType initialType,
            bool baseEditable)
        {
            Position = position;
            InitialType = initialType;
            CurrentType = initialType;
            BaseEditable = baseEditable;
            DaysUnused = 0;
        }
    }

    private readonly Dictionary<Vector3Int, GroundCellRuntime> cells =
        new Dictionary<Vector3Int, GroundCellRuntime>();

    private readonly Dictionary<Vector3Int, int> blockerCounts =
        new Dictionary<Vector3Int, int>();

    private readonly Dictionary<EntityId, HashSet<Vector3Int>> blockerCellsById =
        new Dictionary<EntityId, HashSet<Vector3Int>>();

    private readonly HashSet<Vector3Int> cropOccupiedCells =
        new HashSet<Vector3Int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "A second GroundManager was found and will be destroyed.",
                this);

            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameTime.DayChanged += HandleDayChanged;
    }

    private void Start()
    {
        InitializeGround();
    }

    private void OnDisable()
    {
        GameTime.DayChanged -= HandleDayChanged;

        if (Instance == this)
        {
            if (saveOnDisable && IsInitialized)
                SaveGroundChanges();

            Instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        if (Instance == this && IsInitialized)
            SaveGroundChanges();
    }

    public void InitializeGround()
    {
        if (IsInitialized)
            return;

        if (groundTilemap == null)
        {
            Debug.LogError(
                "GroundManager needs a Ground Tilemap reference.",
                this);
            return;
        }

        if (groundTileSet == null)
        {
            Debug.LogError(
                "GroundManager needs a GroundTileSet asset.",
                this);
            return;
        }

        cells.Clear();
        blockerCounts.Clear();
        blockerCellsById.Clear();
        cropOccupiedCells.Clear();

        ScanPaintedStartingMap();

        if (hideRestrictionMarkersDuringPlay &&
            nonEditableMarkerTilemap != null)
        {
            TilemapRenderer markerRenderer =
                nonEditableMarkerTilemap.GetComponent<TilemapRenderer>();

            if (markerRenderer != null)
                markerRenderer.enabled = false;
        }

        LoadSavedGroundChanges();

        IsInitialized = true;

        Debug.Log(
            "GroundManager initialized " + cells.Count +
            " recognized ground cells.",
            this);
    }

    private void ScanPaintedStartingMap()
    {
        int unrecognizedTileCount = 0;
        BoundsInt bounds = groundTilemap.cellBounds;

        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            TileBase paintedTile = groundTilemap.GetTile(position);

            if (paintedTile == null)
                continue;

            GroundType startingType;

            if (!groundTileSet.TryGetGroundType(
                    paintedTile,
                    out startingType))
            {
                unrecognizedTileCount++;
                continue;
            }

            bool baseEditable =
                nonEditableMarkerTilemap == null ||
                !nonEditableMarkerTilemap.HasTile(position);

            GroundCellRuntime cell = new GroundCellRuntime(
                position,
                startingType,
                baseEditable);

            cells[position] = cell;

            // Do not replace the painted Tile here.
            // This preserves any hand-painted starting variation.
            ApplyTint(position, startingType);
        }

        if (unrecognizedTileCount > 0)
        {
            Debug.LogWarning(
                "GroundManager ignored " + unrecognizedTileCount +
                " painted cells because their Tile assets were not listed " +
                "in the GroundTileSet.",
                this);
        }

        if (cells.Count == 0)
        {
            Debug.LogWarning(
                "GroundManager found no recognized ground cells. " +
                "Paint the Ground Tilemap and assign its Tile assets " +
                "to the GroundTileSet.",
                this);
        }
    }

    private void LoadSavedGroundChanges()
    {
        GameSaveData saveData = SaveManager.LoadGame();

        if (saveData.groundChanges == null)
            return;

        foreach (GroundCellSaveData savedCell in saveData.groundChanges)
        {
            if (savedCell == null)
                continue;

            Vector3Int position = savedCell.GetPosition();
            GroundCellRuntime runtimeCell;

            if (!cells.TryGetValue(position, out runtimeCell))
            {
                Debug.LogWarning(
                    "A saved ground change at " + position +
                    " does not exist on the currently painted map. " +
                    "The saved entry was skipped.",
                    this);

                continue;
            }

            runtimeCell.CurrentType = savedCell.groundType;
            runtimeCell.DaysUnused =
                Mathf.Max(0, savedCell.daysUnused);

            DrawRuntimeCell(runtimeCell);
        }
    }

    public void SaveGroundChanges()
    {
        if (!IsInitialized)
            return;

        List<GroundCellSaveData> changes =
            new List<GroundCellSaveData>();

        foreach (GroundCellRuntime cell in cells.Values)
        {
            bool typeChanged =
                cell.CurrentType != cell.InitialType;

            bool hasRuntimeData =
                cell.DaysUnused > 0;

            if (!typeChanged && !hasRuntimeData)
                continue;

            changes.Add(
                new GroundCellSaveData(
                    cell.Position,
                    cell.CurrentType,
                    cell.DaysUnused));
        }

        changes.Sort(CompareSavedCells);
        SaveManager.SaveGroundChanges(changes);
    }

    private static int CompareSavedCells(
        GroundCellSaveData a,
        GroundCellSaveData b)
    {
        int yComparison = a.y.CompareTo(b.y);

        if (yComparison != 0)
            return yComparison;

        int xComparison = a.x.CompareTo(b.x);

        if (xComparison != 0)
            return xComparison;

        return a.z.CompareTo(b.z);
    }

    public bool HasGroundCell(Vector3Int position)
    {
        return cells.ContainsKey(position);
    }

    public bool TryGetGroundType(
        Vector3Int position,
        out GroundType groundType)
    {
        GroundCellRuntime cell;

        if (cells.TryGetValue(position, out cell))
        {
            groundType = cell.CurrentType;
            return true;
        }

        groundType = default;
        return false;
    }

    public bool CanModifyCell(Vector3Int position)
    {
        GroundCellRuntime cell;

        if (!cells.TryGetValue(position, out cell))
            return false;

        if (!cell.BaseEditable)
            return false;

        int blockerCount;

        if (blockerCounts.TryGetValue(
                position,
                out blockerCount) &&
            blockerCount > 0)
        {
            return false;
        }

        if (cropOccupiedCells.Contains(position))
            return false;

        return true;
    }

    /// <summary>
    /// Ready for the future tool controller.
    /// No player-input code calls this yet.
    /// </summary>
    public bool TrySetGroundType(
        Vector3Int position,
        GroundType newType,
        bool requireEditable = true)
    {
        GroundCellRuntime cell;

        if (!cells.TryGetValue(position, out cell))
            return false;

        if (requireEditable && !CanModifyCell(position))
            return false;

        if (cell.CurrentType == newType)
        {
            MarkCellUsed(position);
            return false;
        }

        cell.CurrentType = newType;
        cell.DaysUnused = 0;

        DrawRuntimeCell(cell);
        SaveGroundChanges();

        return true;
    }

    public void MarkCellUsed(Vector3Int position)
    {
        GroundCellRuntime cell;

        if (!cells.TryGetValue(position, out cell))
            return;

        if (cell.DaysUnused == 0)
            return;

        cell.DaysUnused = 0;
        SaveGroundChanges();
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return groundTilemap.WorldToCell(worldPosition);
    }

    public Vector3 GetCellCenterWorld(Vector3Int position)
    {
        return groundTilemap.GetCellCenterWorld(position);
    }

    public List<Vector3Int> GetCellsOverlappingBounds(
        Bounds worldBounds)
    {
        List<Vector3Int> result = new List<Vector3Int>();

        const float inset = 0.0001f;

        Vector3 minWorld = new Vector3(
            worldBounds.min.x + inset,
            worldBounds.min.y + inset,
            worldBounds.center.z);

        Vector3 maxWorld = new Vector3(
            worldBounds.max.x - inset,
            worldBounds.max.y - inset,
            worldBounds.center.z);

        Vector3Int first = groundTilemap.WorldToCell(minWorld);
        Vector3Int last = groundTilemap.WorldToCell(maxWorld);

        int minX = Mathf.Min(first.x, last.x);
        int maxX = Mathf.Max(first.x, last.x);
        int minY = Mathf.Min(first.y, last.y);
        int maxY = Mathf.Max(first.y, last.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector3Int position =
                    new Vector3Int(x, y, first.z);

                if (cells.ContainsKey(position))
                    result.Add(position);
            }
        }

        return result;
    }

    public void RegisterBlocker(
        EntityId blockerId,
        IEnumerable<Vector3Int> positions)
    {
        UnregisterBlocker(blockerId);

        HashSet<Vector3Int> registeredPositions =
            new HashSet<Vector3Int>();

        foreach (Vector3Int position in positions)
        {
            if (!cells.ContainsKey(position))
                continue;

            registeredPositions.Add(position);

            int count = 0;
            blockerCounts.TryGetValue(position, out count);
            blockerCounts[position] = count + 1;
        }

        blockerCellsById[blockerId] = registeredPositions;
    }

    public void UnregisterBlocker(EntityId blockerId)
    {
        HashSet<Vector3Int> registeredPositions;

        if (!blockerCellsById.TryGetValue(
                blockerId,
                out registeredPositions))
        {
            return;
        }

        foreach (Vector3Int position in registeredPositions)
        {
            int count;

            if (!blockerCounts.TryGetValue(position, out count))
                continue;

            count--;

            if (count <= 0)
                blockerCounts.Remove(position);
            else
                blockerCounts[position] = count;
        }

        blockerCellsById.Remove(blockerId);
    }

    /// <summary>
    /// Future Crop components can register their occupied cells here.
    /// This prevents unused tilled soil from reverting under a crop.
    /// </summary>
    public void RegisterCropCell(Vector3Int position)
    {
        cropOccupiedCells.Add(position);
    }

    public void UnregisterCropCell(Vector3Int position)
    {
        cropOccupiedCells.Remove(position);
    }

    private void HandleDayChanged(int newTotalDay)
    {
        if (!IsInitialized)
            return;

        ProcessNewDay();
    }

    public void ProcessNewDay()
    {
        bool anythingChanged = false;
        int revertAfterDays =
            Mathf.Max(1, unusedDaysBeforeReturningToGround);

        foreach (GroundCellRuntime cell in cells.Values)
        {
            if (cell.CurrentType == GroundType.TilledWatered)
            {
                cell.CurrentType = GroundType.TilledDry;
                cell.DaysUnused = 0;
                DrawRuntimeCell(cell);
                anythingChanged = true;
                continue;
            }

            if (cell.CurrentType != GroundType.TilledDry)
                continue;

            if (cropOccupiedCells.Contains(cell.Position))
            {
                cell.DaysUnused = 0;
                continue;
            }

            cell.DaysUnused++;
            anythingChanged = true;

            if (revertUnusedTilledSoil &&
                cell.DaysUnused >= revertAfterDays)
            {
                cell.CurrentType = GroundType.Ground;
                cell.DaysUnused = 0;
                DrawRuntimeCell(cell);
            }
        }

        if (anythingChanged)
            SaveGroundChanges();
    }

    private void DrawRuntimeCell(GroundCellRuntime cell)
    {
        TileBase tile =
            groundTileSet.GetTile(cell.CurrentType);

        if (tile == null)
        {
            Debug.LogError(
                "GroundTileSet has no Tile assigned for " +
                cell.CurrentType + ".",
                groundTileSet);
            return;
        }

        groundTilemap.SetTile(cell.Position, tile);
        ApplyTint(cell.Position, cell.CurrentType);
        RefreshCellAndNeighbors(cell.Position);
    }

    private void ApplyTint(
        Vector3Int position,
        GroundType groundType)
    {
        // Some Tile assets lock their color by default.
        groundTilemap.RemoveTileFlags(
            position,
            TileFlags.LockColor);

        groundTilemap.SetColor(
            position,
            groundTileSet.GetTint(groundType));
    }

    private void RefreshCellAndNeighbors(Vector3Int center)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                groundTilemap.RefreshTile(
                    center + new Vector3Int(x, y, 0));
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Print Ground Summary")]
    private void PrintGroundSummary()
    {
        Debug.Log(
            "Ground cells: " + cells.Count +
            ", blockers: " + blockerCellsById.Count +
            ", crop cells: " + cropOccupiedCells.Count,
            this);
    }
#endif
}
