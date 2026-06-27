using System;
using UnityEngine;

[Serializable]
public class GroundCellSaveData
{
    public int x;
    public int y;
    public int z;

    public GroundType groundType = GroundType.Grass;
    public int daysUnused = 0;

    public GroundCellSaveData()
    {
    }

    public GroundCellSaveData(
        Vector3Int position,
        GroundType groundType,
        int daysUnused)
    {
        x = position.x;
        y = position.y;
        z = position.z;
        this.groundType = groundType;
        this.daysUnused = Mathf.Max(0, daysUnused);
    }

    public Vector3Int GetPosition()
    {
        return new Vector3Int(x, y, z);
    }
}
