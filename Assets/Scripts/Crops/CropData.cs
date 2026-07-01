using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    fileName = "New Crop Data",
    menuName = "New Unity Game/Crops/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique save ID. Once this crop exists in a save file, do not rename this ID.")]
    [SerializeField] private string cropId = "new_crop";

    [SerializeField] private string displayName = "New Crop";

    [Header("Items")]
    [Tooltip("The seed bag item used to plant this crop.")]
    [SerializeField] private ItemData seedItem;

    [Tooltip("The crop item received when harvesting this crop.")]
    [SerializeField] private ItemData harvestItem;

    [Min(1)]
    [SerializeField] private int harvestAmount = 1;

    [Header("Growth")]
    [Tooltip("Stage 0, Stage 1, Stage 2, Stage 3. The last tile is the mature/harvestable crop.")]
    [SerializeField] private TileBase[] growthStageTiles = new TileBase[4];

    [Tooltip("How many watered nights are needed to advance by one stage.")]
    [Min(1)]
    [SerializeField] private int wateredDaysPerStage = 1;

    public string CropId => cropId;
    public string DisplayName => displayName;

    public ItemData SeedItem => seedItem;
    public ItemData HarvestItem => harvestItem;
    public int HarvestAmount => harvestAmount;

    public int WateredDaysPerStage => wateredDaysPerStage;

    public int GrowthStageCount
    {
        get
        {
            return growthStageTiles == null ? 0 : growthStageTiles.Length;
        }
    }

    public int MatureStageIndex
    {
        get
        {
            return Mathf.Max(0, GrowthStageCount - 1);
        }
    }

    public bool HasValidGrowthStages
    {
        get
        {
            return GrowthStageCount > 0;
        }
    }

    public TileBase GetTileForStage(int stageIndex)
    {
        if (growthStageTiles == null || growthStageTiles.Length == 0)
            return null;

        int safeStageIndex =
            Mathf.Clamp(stageIndex, 0, growthStageTiles.Length - 1);

        return growthStageTiles[safeStageIndex];
    }

    public bool IsMatureStage(int stageIndex)
    {
        if (!HasValidGrowthStages)
            return false;

        return stageIndex >= MatureStageIndex;
    }

    public bool UsesSeedItem(ItemData item)
    {
        return item != null && seedItem == item;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(cropId))
            cropId = MakeSafeId(name);

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        harvestAmount = Mathf.Max(1, harvestAmount);
        wateredDaysPerStage = Mathf.Max(1, wateredDaysPerStage);

        if (growthStageTiles == null || growthStageTiles.Length == 0)
            growthStageTiles = new TileBase[4];
    }

    private string MakeSafeId(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "new_crop";

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

        return string.IsNullOrWhiteSpace(result) ? "new_crop" : result;
    }
}