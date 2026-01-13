using System.Collections.Generic;
using UnityEngine;

public class WallGridSystem : MonoBehaviour
{
    public enum PlaneMode { XY, YZ, ZX } // which local plane defines the wall surface

    [Header("Grid")]
    public int columns = 16;     // X (horizontal on wall)
    public int rows = 10;        // Y (vertical on wall)
    public float cellSize = 1f;

    [Header("Orientation")]
    public PlaneMode plane = PlaneMode.XY; // choose which local plane is the wall
    public bool invertNormal = false;      // flip outwards direction if needed

    [Header("Origin (local)")]
    public Vector2 originOffset = Vector2.zero; // lower-left in the selected plane

    [Header("Snapping")]
    public bool constrainToAxis = true;
    public bool clampToBounds = true;
    public float wallForwardOffset = 0.01f; // push out to avoid z-fighting

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoOutline = new Color(0.8f, 0.9f, 1f, 0.9f);
    public Color gizmoLines = new Color(0.8f, 0.9f, 1f, 0.25f);

    private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    // Axes chosen from transform based on plane mode
    public Vector3 RightN
    {
        get
        {
            switch (plane)
            {
                case PlaneMode.XY: return transform.right;   // +X
                case PlaneMode.YZ: return transform.forward; // +Z
                case PlaneMode.ZX: return transform.up;      // +Y
            }
            return transform.right;
        }
    }

    public Vector3 UpN
    {
        get
        {
            switch (plane)
            {
                case PlaneMode.XY: return transform.up;      // +Y
                case PlaneMode.YZ: return transform.up;      // +Y
                case PlaneMode.ZX: return transform.forward; // +Z
            }
            return transform.up;
        }
    }

    public Vector3 NormalN
    {
        get
        {
            Vector3 n;
            switch (plane)
            {
                case PlaneMode.XY: n = transform.forward; break; // +Z
                case PlaneMode.YZ: n = transform.right; break; // +X
                case PlaneMode.ZX: n = transform.up; break; // +Y
                default: n = transform.forward; break;
            }
            return invertNormal ? -n : n;
        }
    }

    private Vector3 OriginWorld =>
        // originOffset.x goes along RightN, originOffset.y along UpN
        transform.position + RightN * originOffset.x + UpN * originOffset.y;

    // -------- Mapping --------

    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector3 d = world - OriginWorld;
        float u = Vector3.Dot(d, RightN);
        float v = Vector3.Dot(d, UpN);
        int cx = Mathf.RoundToInt(u / cellSize);
        int cy = Mathf.RoundToInt(v / cellSize);
        return new Vector2Int(cx, cy);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return OriginWorld
             + RightN * (cell.x * cellSize)
             + UpN * (cell.y * cellSize)
             + NormalN * wallForwardOffset;
    }

    public bool InBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < columns && cell.y < rows;
    }

    public bool IsFree(Vector2Int cell) => InBounds(cell) && !occupied.Contains(cell);

    public bool AreCellsFree(List<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++)
            if (!IsFree(cells[i])) return false;
        return true;
    }

    public void Occupy(List<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++) occupied.Add(cells[i]);
    }

    public void Free(List<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++) occupied.Remove(cells[i]);
    }

    // -------- Axis constraint --------

    public Vector2Int ConstrainToAxis(Vector2Int startCell, Vector2Int rawCell)
    {
        int dx = rawCell.x - startCell.x;
        int dy = rawCell.y - startCell.y;

        Vector2Int c = (Mathf.Abs(dx) >= Mathf.Abs(dy))
            ? new Vector2Int(rawCell.x, startCell.y)
            : new Vector2Int(startCell.x, rawCell.y);

        if (clampToBounds)
        {
            c.x = Mathf.Clamp(c.x, 0, columns - 1);
            c.y = Mathf.Clamp(c.y, 0, rows - 1);
        }
        return c;
    }

    public Vector2Int WorldToConstrainedCell(Vector3 world, Vector2Int startCell)
    {
        var raw = WorldToCell(world);
        return constrainToAxis ? ConstrainToAxis(startCell, raw) : raw;
    }

    public Vector3 WorldToSnappedWorld(Vector3 world, Vector2Int startCell)
    {
        var c = WorldToConstrainedCell(world, startCell);
        return CellToWorld(c);
    }

    // -------- Optional helpers --------

    // Call this if you have a surface normal and want to orient the grid to it quickly.
    public void AlignToSurface(Vector3 outwardNormal, Vector3 verticalHint)
    {
        // pick an up that isn't parallel to the normal
        Vector3 up = Vector3.Dot(outwardNormal.normalized, verticalHint.normalized) > 0.99f
            ? Vector3.Cross(outwardNormal, Vector3.right).normalized
            : verticalHint.normalized;

        transform.rotation = Quaternion.LookRotation(outwardNormal, up);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector3 o = OriginWorld + NormalN * wallForwardOffset;
        Vector3 rx = RightN * cellSize;
        Vector3 ry = UpN * cellSize;

        // outline
        Gizmos.color = gizmoOutline;
        Vector3 a = o;
        Vector3 b = o + rx * (columns - 1);
        Vector3 c = b + ry * (rows - 1);
        Vector3 d = o + ry * (rows - 1);
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);

        // grid lines
        Gizmos.color = gizmoLines;
        for (int x = 0; x < columns; x++)
        {
            Vector3 s = o + rx * x;
            Vector3 e = s + ry * (rows - 1);
            Gizmos.DrawLine(s, e);
        }
        for (int y = 0; y < rows; y++)
        {
            Vector3 s = o + ry * y;
            Vector3 e = s + rx * (columns - 1);
            Gizmos.DrawLine(s, e);
        }
    }
}
