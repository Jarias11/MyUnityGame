using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int saveVersion = 2;

    // Time
    public int seconds = 0;
    public int minutes = 0;
    public int hours = 6;

    // Day 1 starts on Sunday in Spring.
    public int totalDays = 1;

    // Only cells changed from the hand-painted starting Tilemap are saved.
    public List<GroundCellSaveData> groundChanges =
        new List<GroundCellSaveData>();

    public void EnsureValidCollections()
    {
        if (groundChanges == null)
            groundChanges = new List<GroundCellSaveData>();
    }

    // Later:
    // public float playerX;
    // public float playerY;
    // public string currentScene;
    // public int money;
    // public List<string> inventoryItems;
}
