using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridOverlay : MonoBehaviour
{
    [Header("Refs")]
    public FloorGridSystem grid;

    [Header("Visuals")]
    public Color lineColor = new Color(1f, 1f, 1f, 0.25f);
    public float lineWidth = 0.02f;
    public float yOffset = 0.01f;
    public bool startVisible = true;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.G;

    MeshFilter mf;
    MeshRenderer mr;
    Mesh mesh;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
        {
            Debug.LogWarning("GridOverlay: assign an Unlit material for best results.");
        }
    }

    void OnEnable()
    {
        if (grid == null)
        {
            Debug.LogError("GridOverlay: GridSystem reference not set.");
            enabled = false;
            return;
        }
        Rebuild();
        SetVisible(startVisible);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetVisible(!mr.enabled);
        }
    }

    public void SetVisible(bool v)
    {
        if (mr) mr.enabled = v;
    }

    public void SetColor(Color c)
    {
        lineColor = c;
        if (mr && mr.sharedMaterial) mr.sharedMaterial.color = lineColor;
    }

    public void Rebuild()
    {
        if (mesh != null) mesh.Clear();
        else mesh = new Mesh { name = "GridOverlayMesh" };

        float cs = grid.cellSize;
        int gx = grid.gridSize.x;
        int gy = grid.gridSize.y;
        Vector3 origin = grid.origin + new Vector3(0f, yOffset, 0f);

        // Build thin quad strips for each grid line (horizontal and vertical)
        int verticalLines = gx + 1;
        int horizontalLines = gy + 1;
        int totalLines = verticalLines + horizontalLines;

        // 4 verts per line, 6 indices per line (two triangles)
        Vector3[] verts = new Vector3[totalLines * 4];
        int[] tris = new int[totalLines * 6];
        Color[] cols = new Color[verts.Length];

        int vi = 0;
        int ti = 0;

        float halfW = Mathf.Max(0.0001f, lineWidth * 0.5f);

        // Vertical lines (along +Z)
        for (int x = 0; x <= gx; x++)
        {
            float wx = x * cs;
            Vector3 a = origin + new Vector3(wx, 0f, 0f);
            Vector3 b = origin + new Vector3(wx, 0f, gy * cs);

            // Build a quad centered on the line, thickness in X
            Vector3 left = new Vector3(-halfW, 0f, 0f);
            Vector3 right = new Vector3(halfW, 0f, 0f);

            verts[vi + 0] = a + left;
            verts[vi + 1] = a + right;
            verts[vi + 2] = b + right;
            verts[vi + 3] = b + left;

            tris[ti + 0] = vi + 0;
            tris[ti + 1] = vi + 2;
            tris[ti + 2] = vi + 1;
            tris[ti + 3] = vi + 0;
            tris[ti + 4] = vi + 3;
            tris[ti + 5] = vi + 2;

            cols[vi + 0] = lineColor;
            cols[vi + 1] = lineColor;
            cols[vi + 2] = lineColor;
            cols[vi + 3] = lineColor;

            vi += 4;
            ti += 6;
        }

        // Horizontal lines (along +X)
        for (int y = 0; y <= gy; y++)
        {
            float wz = y * cs;
            Vector3 a = origin + new Vector3(0f, 0f, wz);
            Vector3 b = origin + new Vector3(gx * cs, 0f, wz);

            Vector3 back = new Vector3(0f, 0f, -halfW);
            Vector3 fwd = new Vector3(0f, 0f, halfW);

            verts[vi + 0] = a + back;
            verts[vi + 1] = a + fwd;
            verts[vi + 2] = b + fwd;
            verts[vi + 3] = b + back;

            tris[ti + 0] = vi + 0;
            tris[ti + 1] = vi + 2;
            tris[ti + 2] = vi + 1;
            tris[ti + 3] = vi + 0;
            tris[ti + 4] = vi + 3;
            tris[ti + 5] = vi + 2;

            cols[vi + 0] = lineColor;
            cols[vi + 1] = lineColor;
            cols[vi + 2] = lineColor;
            cols[vi + 3] = lineColor;

            vi += 4;
            ti += 6;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.colors = cols;
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;

        // ensure renderer color matches desired tint
        if (mr == null) mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial != null) mr.sharedMaterial.color = lineColor;

        // place overlay at grid origin
        transform.position = grid.origin;
        transform.rotation = Quaternion.identity;
    }
}