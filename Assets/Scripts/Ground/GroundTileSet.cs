using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Connects gameplay ground states to the Tile assets that render them.
///
/// TilledDry and TilledWatered deliberately use the same Tile.
/// GroundManager darkens the watered cells with a per-cell tint.
/// </summary>
[CreateAssetMenu(
    fileName = "Ground Tile Set",
    menuName = "New Unity Game/Ground/Ground Tile Set")]
public class GroundTileSet : ScriptableObject
{
    [Header("Main Tiles")]
    [SerializeField] private TileBase grassTile;
    [SerializeField] private TileBase groundTile;
    [SerializeField] private TileBase tilledTile;

    [Header("Optional Recognized Variants")]
    [Tooltip(
        "Add any extra Grass Tile assets already painted on the starting map. " +
        "You do not need to add sprites used internally by one Rule Tile.")]
    [SerializeField] private List<TileBase> grassAliases = new List<TileBase>();

    [Tooltip(
        "Add any extra Ground Tile assets already painted on the starting map. " +
        "You do not need to add sprites used internally by one Rule Tile.")]
    [SerializeField] private List<TileBase> groundAliases = new List<TileBase>();

    [Tooltip(
        "Add any extra Tilled Tile assets already painted on the starting map. " +
        "You do not need to add sprites used internally by one Rule Tile.")]
    [SerializeField] private List<TileBase> tilledAliases = new List<TileBase>();

    [Header("Per-Cell Tint")]
    [SerializeField] private Color normalTint = Color.white;

    [Tooltip(
        "Multiplied over the normal tilled sprite. Lower RGB values make it darker. " +
        "A value around 0.65 for R, G, and B is a useful starting point.")]
    [SerializeField] private Color wateredTint =
        new Color(0.65f, 0.65f, 0.65f, 1f);

    public TileBase GetTile(GroundType groundType)
    {
        switch (groundType)
        {
            case GroundType.Grass:
                return grassTile;

            case GroundType.Ground:
                return groundTile;

            case GroundType.TilledDry:
            case GroundType.TilledWatered:
                return tilledTile;

            default:
                return null;
        }
    }

    public Color GetTint(GroundType groundType)
    {
        return groundType == GroundType.TilledWatered
            ? wateredTint
            : normalTint;
    }

    public bool TryGetGroundType(TileBase tile, out GroundType groundType)
    {
        if (Matches(tile, grassTile, grassAliases))
        {
            groundType = GroundType.Grass;
            return true;
        }

        if (Matches(tile, groundTile, groundAliases))
        {
            groundType = GroundType.Ground;
            return true;
        }

        if (Matches(tile, tilledTile, tilledAliases))
        {
            // A tilled tile painted in the editor starts dry.
            groundType = GroundType.TilledDry;
            return true;
        }

        groundType = default;
        return false;
    }

    private static bool Matches(
        TileBase candidate,
        TileBase primary,
        List<TileBase> aliases)
    {
        if (candidate == null)
            return false;

        if (candidate == primary)
            return true;

        return aliases != null && aliases.Contains(candidate);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        normalTint.a = 1f;
        wateredTint.a = 1f;
    }
#endif
}
