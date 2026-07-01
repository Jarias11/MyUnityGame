using System;
using UnityEngine;

[Serializable]
public class CropSaveData
{
    public string cropId;

    public int x;
    public int y;
    public int z;

    public int stageIndex;
    public int growthProgressInStage;

    public CropSaveData()
    {
    }

    public CropSaveData(
        string cropId,
        Vector3Int position,
        int stageIndex,
        int growthProgressInStage)
    {
        this.cropId = cropId;

        x = position.x;
        y = position.y;
        z = position.z;

        this.stageIndex = stageIndex;
        this.growthProgressInStage = growthProgressInStage;
    }

    public Vector3Int GetPosition()
    {
        return new Vector3Int(x, y, z);
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(cropId);
    }
}