using System.Collections.Generic;
using UnityEngine;

public class FloorGridSystem : MonoBehaviour
{
    [Header("Grid")]
    public Vector2Int gridSize = new Vector2Int(16, 12);
    public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;

    private HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector3 local = world - origin;
        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.z / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        float x = cell.x * cellSize;
        float z = cell.y * cellSize;
        return origin + new Vector3(x, 0f, z);
    }

    public bool InBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < gridSize.x && cell.y < gridSize.y;
    }

    public bool IsFree(Vector2Int cell)
    {
        return InBounds(cell) && !occupied.Contains(cell);
    }

    public bool AreCellsFree(List<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (!IsFree(cells[i])) return false;
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
        for (int x = 0; x <= gridSize.x; x++)
        {
            Vector3 a = CellToWorld(new Vector2Int(x, 0));
            Vector3 b = CellToWorld(new Vector2Int(x, gridSize.y));
            Gizmos.DrawLine(a, b);
        }
        for (int y = 0; y <= gridSize.y; y++)
        {
            Vector3 a = CellToWorld(new Vector2Int(0, y));
            Vector3 b = CellToWorld(new Vector2Int(gridSize.x, y));
            Gizmos.DrawLine(a, b);
        }
    }
}