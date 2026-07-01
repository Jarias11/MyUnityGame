using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-50)]
public class CropManager : MonoBehaviour
{
    public static CropManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Tilemap cropTilemap;
    [SerializeField] private CropDatabase cropDatabase;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Saving")]
    [SerializeField] private bool saveOnDisable = true;

    public bool IsInitialized { get; private set; }

    private sealed class PlantedCropRuntime
    {
        public Vector3Int Position;
        public CropData CropData;
        public int StageIndex;
        public int GrowthProgressInStage;

        public PlantedCropRuntime(
            Vector3Int position,
            CropData cropData,
            int stageIndex,
            int growthProgressInStage)
        {
            Position = position;
            CropData = cropData;
            StageIndex = stageIndex;
            GrowthProgressInStage = growthProgressInStage;
        }
    }

    private readonly Dictionary<Vector3Int, PlantedCropRuntime> plantedCrops =
        new Dictionary<Vector3Int, PlantedCropRuntime>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "A second CropManager was found and will be destroyed.",
                this);

            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameTime.BeforeDayChanged += HandleBeforeDayChanged;
    }

    private void Start()
    {
        InitializeCrops();
    }

    private void OnDisable()
    {
        GameTime.BeforeDayChanged -= HandleBeforeDayChanged;

        if (Instance == this)
        {
            if (saveOnDisable && IsInitialized)
                SaveCropsToSaveManager();

            Instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        if (Instance == this && IsInitialized)
            SaveCropsToSaveManager();
    }

    public void InitializeCrops()
    {
        if (IsInitialized)
            return;

        if (cropTilemap == null)
        {
            Debug.LogError(
                "CropManager needs a Crop Tilemap reference.",
                this);

            return;
        }

        if (cropDatabase == null)
        {
            Debug.LogError(
                "CropManager needs a CropDatabase asset.",
                this);

            return;
        }

        ClearRuntimeCrops();
        LoadSavedCrops();

        IsInitialized = true;

        Debug.Log(
            "CropManager initialized " + plantedCrops.Count +
            " planted crops.",
            this);
    }

    public bool TryPlantSeed(
        Vector3Int position,
        ItemData seedItem,
        PlayerInventory inventory)
    {
        if (!IsInitialized)
            return false;

        if (seedItem == null)
            return false;

        if (plantedCrops.ContainsKey(position))
            return false;

        CropData cropData;

        if (!cropDatabase.TryGetCropForSeed(seedItem, out cropData))
            return false;

        if (cropData == null || !cropData.HasValidGrowthStages)
            return false;

        GroundManager groundManager = GroundManager.Instance;

        if (groundManager == null || !groundManager.IsInitialized)
            return false;

        GroundType groundType;

        if (!groundManager.TryGetGroundType(position, out groundType))
            return false;

        if (groundType != GroundType.TilledDry &&
            groundType != GroundType.TilledWatered)
        {
            return false;
        }

        if (!groundManager.CanModifyCell(position))
            return false;

        inventory = ResolveInventory(inventory);

        if (inventory == null)
            return false;

        InventorySlot selectedSlot = inventory.SelectedHotbarSlot;

        if (selectedSlot == null ||
            selectedSlot.IsEmpty ||
            selectedSlot.Item != seedItem)
        {
            return false;
        }

        if (!inventory.RemoveFromSlot(inventory.SelectedHotbarIndex, 1))
            return false;

        PlantedCropRuntime crop =
            new PlantedCropRuntime(
                position,
                cropData,
                0,
                0);

        plantedCrops[position] = crop;

        groundManager.RegisterCropCell(position);
        groundManager.MarkCellUsed(position);

        DrawCrop(crop);
        SaveCropsToSaveManager();

        return true;
    }

    public bool TryHarvestCrop(
        Vector3Int position,
        PlayerInventory inventory)
    {
        if (!IsInitialized)
            return false;

        PlantedCropRuntime crop;

        if (!plantedCrops.TryGetValue(position, out crop))
            return false;

        if (crop == null || crop.CropData == null)
            return false;

        if (!crop.CropData.IsMatureStage(crop.StageIndex))
            return false;

        inventory = ResolveInventory(inventory);

        if (inventory == null)
            return false;

        ItemData harvestItem = crop.CropData.HarvestItem;
        int harvestAmount = crop.CropData.HarvestAmount;

        if (harvestItem == null || harvestAmount <= 0)
            return false;

        if (!CanInventoryFitItem(inventory, harvestItem, harvestAmount))
        {
            Debug.Log(
                "Inventory has no room for " +
                harvestAmount + "x " +
                harvestItem.DisplayName + ".",
                this);

            return false;
        }

        if (!inventory.AddItem(harvestItem, harvestAmount))
            return false;

        RemoveCrop(position, true);
        SaveCropsToSaveManager();

        return true;
    }

    public bool HasCropAt(Vector3Int position)
    {
        return plantedCrops.ContainsKey(position);
    }

    public bool IsCropMature(Vector3Int position)
    {
        PlantedCropRuntime crop;

        if (!plantedCrops.TryGetValue(position, out crop))
            return false;

        if (crop == null || crop.CropData == null)
            return false;

        return crop.CropData.IsMatureStage(crop.StageIndex);
    }

    private PlayerInventory ResolveInventory(PlayerInventory inventory)
    {
        if (inventory != null)
            return inventory;

        if (playerInventory != null)
            return playerInventory;

        playerInventory = FindAnyObjectByType<PlayerInventory>();
        return playerInventory;
    }

    private bool CanInventoryFitItem(
        PlayerInventory inventory,
        ItemData item,
        int amount)
    {
        if (inventory == null || item == null || amount <= 0)
            return false;

        int remainingAmount = amount;
        IReadOnlyList<InventorySlot> slots = inventory.Slots;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot == null)
                continue;

            remainingAmount -= slot.GetSpaceFor(item);

            if (remainingAmount <= 0)
                return true;
        }

        return false;
    }

    private void HandleBeforeDayChanged(int nextTotalDay)
    {
        if (!IsInitialized)
            return;

        ProcessCropGrowthForNewDay();
    }

    public void ProcessCropGrowthForNewDay()
    {
        GroundManager groundManager = GroundManager.Instance;

        if (groundManager == null || !groundManager.IsInitialized)
            return;

        bool anythingChanged = false;

        foreach (PlantedCropRuntime crop in plantedCrops.Values)
        {
            if (crop == null || crop.CropData == null)
                continue;

            if (crop.CropData.IsMatureStage(crop.StageIndex))
                continue;

            GroundType groundType;

            if (!groundManager.TryGetGroundType(
                    crop.Position,
                    out groundType))
            {
                continue;
            }

            if (groundType != GroundType.TilledWatered)
                continue;

            crop.GrowthProgressInStage++;

            int daysNeeded =
                Mathf.Max(1, crop.CropData.WateredDaysPerStage);

            if (crop.GrowthProgressInStage >= daysNeeded)
            {
                crop.GrowthProgressInStage = 0;

                crop.StageIndex =
                    Mathf.Min(
                        crop.StageIndex + 1,
                        crop.CropData.MatureStageIndex);

                DrawCrop(crop);
            }

            anythingChanged = true;
        }

        if (anythingChanged)
            SaveCropsToSaveManager();
    }

    private void LoadSavedCrops()
    {
        GameSaveData saveData = SaveManager.LoadGame();

        if (saveData.crops == null)
            return;

        GroundManager groundManager = GroundManager.Instance;

        if (groundManager == null || !groundManager.IsInitialized)
        {
            Debug.LogWarning(
                "CropManager could not load crops because GroundManager is not initialized.",
                this);

            return;
        }

        for (int i = 0; i < saveData.crops.Count; i++)
        {
            CropSaveData savedCrop = saveData.crops[i];

            if (savedCrop == null || !savedCrop.IsValid())
                continue;

            Vector3Int position = savedCrop.GetPosition();

            if (plantedCrops.ContainsKey(position))
                continue;

            if (!groundManager.HasGroundCell(position))
            {
                Debug.LogWarning(
                    "A saved crop at " + position +
                    " does not exist on the current ground map. Skipping it.",
                    this);

                continue;
            }

            GroundType groundType;

            if (!groundManager.TryGetGroundType(position, out groundType))
                continue;

            if (groundType != GroundType.TilledDry &&
                groundType != GroundType.TilledWatered)
            {
                Debug.LogWarning(
                    "A saved crop at " + position +
                    " is not on tilled soil. Skipping it.",
                    this);

                continue;
            }

            CropData cropData;

            if (!cropDatabase.TryGetCrop(savedCrop.cropId, out cropData))
            {
                Debug.LogWarning(
                    "Saved crop could not be found in CropDatabase: " +
                    savedCrop.cropId,
                    this);

                continue;
            }

            if (cropData == null || !cropData.HasValidGrowthStages)
                continue;

            int safeStageIndex =
                Mathf.Clamp(
                    savedCrop.stageIndex,
                    0,
                    cropData.MatureStageIndex);

            int safeGrowthProgress =
                Mathf.Max(0, savedCrop.growthProgressInStage);

            PlantedCropRuntime crop =
                new PlantedCropRuntime(
                    position,
                    cropData,
                    safeStageIndex,
                    safeGrowthProgress);

            plantedCrops[position] = crop;

            groundManager.RegisterCropCell(position);
            DrawCrop(crop);
        }
    }

    public void SaveCropsToSaveManager()
    {
        if (!IsInitialized)
            return;

        List<CropSaveData> savedCrops =
            new List<CropSaveData>();

        foreach (PlantedCropRuntime crop in plantedCrops.Values)
        {
            if (crop == null || crop.CropData == null)
                continue;

            savedCrops.Add(
                new CropSaveData(
                    crop.CropData.CropId,
                    crop.Position,
                    crop.StageIndex,
                    crop.GrowthProgressInStage));
        }

        savedCrops.Sort(CompareSavedCrops);
        SaveManager.SaveCrops(savedCrops);
    }

    private static int CompareSavedCrops(
        CropSaveData a,
        CropSaveData b)
    {
        int yComparison = a.y.CompareTo(b.y);

        if (yComparison != 0)
            return yComparison;

        int xComparison = a.x.CompareTo(b.x);

        if (xComparison != 0)
            return xComparison;

        return a.z.CompareTo(b.z);
    }

    private void DrawCrop(PlantedCropRuntime crop)
    {
        if (crop == null || crop.CropData == null)
            return;

        TileBase tile =
            crop.CropData.GetTileForStage(crop.StageIndex);

        cropTilemap.SetTile(crop.Position, tile);
    }

    private void RemoveCrop(
        Vector3Int position,
        bool unregisterFromGround)
    {
        plantedCrops.Remove(position);
        cropTilemap.SetTile(position, null);

        if (unregisterFromGround)
        {
            GroundManager groundManager = GroundManager.Instance;

            if (groundManager != null)
                groundManager.UnregisterCropCell(position);
        }
    }

    private void ClearRuntimeCrops()
    {
        GroundManager groundManager = GroundManager.Instance;

        if (groundManager != null)
        {
            foreach (Vector3Int position in plantedCrops.Keys)
            {
                groundManager.UnregisterCropCell(position);
            }
        }

        plantedCrops.Clear();

        if (cropTilemap != null)
            cropTilemap.ClearAllTiles();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Print Crop Summary")]
    private void PrintCropSummary()
    {
        Debug.Log(
            "Planted crops: " + plantedCrops.Count,
            this);
    }
#endif
}