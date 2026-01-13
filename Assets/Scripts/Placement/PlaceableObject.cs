using System.Collections.Generic;
using UnityEngine;

public enum PivotMode { BottomLeft, Center }
public enum PlacementSurface { Floor, Wall }
public enum WallSide { Forward, Back, Right, Left, Up, Down }

public class PlaceableObject : MonoBehaviour
{
    [Header("Grid Footprint")]
    public Vector2Int size = new Vector2Int(1, 1);
    public PivotMode pivot = PivotMode.BottomLeft;

    [Header("Placement Rules")]
    public PlacementSurface placementSurface = PlacementSurface.Floor;
    public GameObject PlacementSpot;

    [Header("Stacking (as held object)")]
    public bool canBePlacedOnTopOfOthers = false;

    [Header("Stacking (as host object)")]
    public bool allowsPlacementOnTop = false;
    public WallSide stackingHostFace = WallSide.Up; // which face of THIS object accepts stacks

    [Header("Wall Mount")]
    public WallSide wallSide = WallSide.Back;

    public GameObject sourcePrefab;

    [Header("Visuals (optional)")]
    public Renderer[] colorable;

    private int rotationSteps;

    public Vector3 GetWallSideLocal()
    {
        switch (wallSide)
        {
            case WallSide.Forward: return Vector3.forward;
            case WallSide.Back: return -Vector3.forward;
            case WallSide.Right: return Vector3.right;
            case WallSide.Left: return -Vector3.right;
            case WallSide.Up: return Vector3.up;
            case WallSide.Down: return -Vector3.up;
        }
        return -Vector3.forward;
    }

    public void SetGhostTint(bool valid, Color validColor, Color invalidColor)
    {
        if (colorable == null) return;
        Color c = valid ? validColor : invalidColor;
        for (int i = 0; i < colorable.Length; i++)
        {
            if (!colorable[i]) continue;
            var mats = colorable[i].materials;
            for (int m = 0; m < mats.Length; m++) mats[m].color = c;
        }
    }

    public void SetRotationSteps(int steps)
    {
        rotationSteps = ((steps % 4) + 4) % 4;
        Vector3 e = transform.eulerAngles;
        e.y = rotationSteps * 90f;
        transform.eulerAngles = e;
    }

    public Vector2Int GetRotatedSize()
    {
        return (rotationSteps % 2 == 0) ? size : new Vector2Int(size.y, size.x);
    }

    public Vector3 GetSpotWorld()
    {
        return PlacementSpot ? PlacementSpot.transform.position : transform.position;
    }

    public Vector2Int GetAnchorCell(FloorGridSystem grid)
    {
        return grid.WorldToCell(GetSpotWorld());
    }

    public void SnapSpotToWorld(Vector3 targetWorld)
    {
        Vector3 spot = GetSpotWorld();
        Vector3 delta = targetWorld - spot;
        transform.position += delta;
    }

    public List<Vector2Int> GetOccupiedCells(Vector2Int anchorCell)
    {
        Vector2Int s = GetRotatedSize();
        Vector2Int baseCell = anchorCell;

        if (pivot == PivotMode.Center)
            baseCell = new Vector2Int(anchorCell.x - (s.x / 2), anchorCell.y - (s.y / 2));

        var cells = new List<Vector2Int>(s.x * s.y);
        for (int x = 0; x < s.x; x++)
        {
            for (int y = 0; y < s.y; y++)
            {
                Vector2Int local = new Vector2Int(x, y);
                Vector2Int rot = RotateLocal(local, s, rotationSteps);
                cells.Add(new Vector2Int(baseCell.x + rot.x, baseCell.y + rot.y));
            }
        }
        return cells;
    }

    private Vector2Int RotateLocal(Vector2Int c, Vector2Int sizeBefore, int steps)
    {
        steps = ((steps % 4) + 4) % 4;
        switch (steps)
        {
            case 0: return c;
            case 1: return new Vector2Int(sizeBefore.y - 1 - c.y, c.x);
            case 2: return new Vector2Int(sizeBefore.x - 1 - c.x, sizeBefore.y - 1 - c.y);
            case 3: return new Vector2Int(c.y, sizeBefore.x - 1 - c.x);
        }
        return c;
    }
}
