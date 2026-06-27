using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Rule Tile with extra neighbor choices for Grass, Ground, Tilled, and Empty.
///
/// Requires Unity's 2D Tilemap Extras package.
/// Create assets through:
/// Assets > Create > New Unity Game > Terrain Rule Tile
/// </summary>
[CreateAssetMenu(
    fileName = "New Terrain Rule Tile",
    menuName = "New Unity Game/Tiles/Terrain Rule Tile")]
public class TerrainRuleTile : RuleTile<TerrainRuleTile.Neighbor>
{
    [Header("Terrain Identity")]
    [Tooltip("The terrain represented by this Rule Tile asset.")]
    public TerrainKind terrainKind = TerrainKind.Grass;

    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        // 0, 1, and 2 are already used by the normal Rule Tile options.
        public const int Grass = 3;
        public const int Ground = 4;
        public const int Tilled = 5;
        public const int Empty = 6;
        public const int AnyTerrain = 7;
    }

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        TerrainRuleTile otherTerrain = other as TerrainRuleTile;

        switch (neighbor)
        {
            case Neighbor.Grass:
                return otherTerrain != null &&
                       otherTerrain.terrainKind == TerrainKind.Grass;

            case Neighbor.Ground:
                return otherTerrain != null &&
                       otherTerrain.terrainKind == TerrainKind.Ground;

            case Neighbor.Tilled:
                return otherTerrain != null &&
                       otherTerrain.terrainKind == TerrainKind.Tilled;

            case Neighbor.Empty:
                return other == null;

            case Neighbor.AnyTerrain:
                return otherTerrain != null;

            default:
                return base.RuleMatch(neighbor, other);
        }
    }
}
