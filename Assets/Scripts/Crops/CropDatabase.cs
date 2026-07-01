using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Crop Database",
    menuName = "New Unity Game/Crops/Crop Database")]
public class CropDatabase : ScriptableObject
{
    [SerializeField] private List<CropData> crops =
        new List<CropData>();

    private Dictionary<string, CropData> cropsById;
    private Dictionary<ItemData, CropData> cropsBySeedItem;

    private void OnEnable()
    {
        BuildLookup();
    }

    public bool TryGetCrop(string cropId, out CropData crop)
    {
        crop = null;

        if (string.IsNullOrWhiteSpace(cropId))
            return false;

        EnsureLookup();

        return cropsById.TryGetValue(cropId, out crop);
    }

    public bool TryGetCropForSeed(
        ItemData seedItem,
        out CropData crop)
    {
        crop = null;

        if (seedItem == null)
            return false;

        EnsureLookup();

        return cropsBySeedItem.TryGetValue(seedItem, out crop);
    }

    [ContextMenu("Rebuild Crop Lookup")]
    public void BuildLookup()
    {
        cropsById = new Dictionary<string, CropData>();
        cropsBySeedItem = new Dictionary<ItemData, CropData>();

        if (crops == null)
            crops = new List<CropData>();

        for (int i = 0; i < crops.Count; i++)
        {
            CropData crop = crops[i];

            if (crop == null)
                continue;

            if (string.IsNullOrWhiteSpace(crop.CropId))
            {
                Debug.LogWarning(
                    "CropDatabase found a crop with an empty CropId.",
                    crop);

                continue;
            }

            if (cropsById.ContainsKey(crop.CropId))
            {
                Debug.LogWarning(
                    "Duplicate CropId found in CropDatabase: " +
                    crop.CropId +
                    ". The first crop will be used.",
                    crop);
            }
            else
            {
                cropsById.Add(crop.CropId, crop);
            }

            if (crop.SeedItem == null)
            {
                Debug.LogWarning(
                    "CropDatabase crop has no seed item assigned: " +
                    crop.DisplayName,
                    crop);

                continue;
            }

            if (cropsBySeedItem.ContainsKey(crop.SeedItem))
            {
                Debug.LogWarning(
                    "Two crops use the same seed item: " +
                    crop.SeedItem.DisplayName +
                    ". The first crop will be used.",
                    crop);
            }
            else
            {
                cropsBySeedItem.Add(crop.SeedItem, crop);
            }
        }
    }

    private void EnsureLookup()
    {
        if (cropsById == null || cropsBySeedItem == null)
            BuildLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (crops == null)
            crops = new List<CropData>();
    }
#endif
}