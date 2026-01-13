using UnityEngine;

public class TopPlaceAreaOverlay : MonoBehaviour
{
    [Header("Host")]
    public PlaceableObject host;          // object that can accept items on top
    public bool attachToHost = true;      // keep overlay positioned/rotated on host top

    [Header("Grid")]
    public bool useHostGridSize = true;   // if true, uses host.size.x / host.size.y for cols/rows
    public int columns = 3;               // used if useHostGridSize == false
    public int rows = 2;                  // used if useHostGridSize == false
    public float edgePadding = 0.02f;     // shrink the usable rect inward (meters)
    public float yOffset = 0.01f;         // lift overlay a bit to avoid z-fighting

    [Header("Visuals")]
    public Color lineColor = new Color(1f, 1f, 1f, 0.25f);
    public float lineWidth = 0.02f;       // thickness of each grid line (meters)
    public bool startVisible = true;
    public KeyCode toggleKey = KeyCode.T; // quick toggle at runtime (optional)

    private MeshFilter mf;
    private MeshRenderer mr;
    private Mesh mesh;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
        {
            Debug.LogWarning("TopPlaceAreaOverlay: assign an Unlit/Transparent material for best results.");
        }
    }

    void OnEnable()
    {
        if (!host) host = GetComponentInParent<PlaceableObject>();
        if (!host)
        {
            Debug.LogError("TopPlaceAreaOverlay: No host PlaceableObject assigned.");
            enabled = false;
            return;
        }

        Rebuild();
        SetVisible(startVisible);
    }

    void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            SetVisible(!mr.enabled);

        if (attachToHost)
            FollowHostTopPlane();
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

    private void EnsureMesh()
    {
        if (mesh == null)
        {
            mesh = new Mesh { name = "TopPlaceAreaOverlayMesh" };
        }
        else
        {
            mesh.Clear();
        }
        if (!mf) mf = GetComponent<MeshFilter>();
        if (!mr) mr = GetComponent<MeshRenderer>();
    }

    public void Rebuild()
    {
        EnsureMesh();

        int gx = useHostGridSize ? Mathf.Max(1, host.size.x) : Mathf.Max(1, columns);
        int gy = useHostGridSize ? Mathf.Max(1, host.size.y) : Mathf.Max(1, rows);

        // Get top plane basis from the host
        Vector3 centerW, xAxisW, zAxisW, normalW;
        float width, depth;
        ComputeTopPlane(out centerW, out xAxisW, out zAxisW, out normalW, out width, out depth);

        // shrink by padding
        float w = Mathf.Max(0f, width - edgePadding * 2f);
        float d = Mathf.Max(0f, depth - edgePadding * 2f);

        // Align the overlay so:
        //   local X == xAxisW, local Z == zAxisW, local Y == normalW
        transform.position = centerW + normalW * yOffset;
        transform.rotation = Quaternion.LookRotation(zAxisW, normalW);

        // Build grid quads in LOCAL space around origin (X/Z plane)
        int verticalLines = gx + 1;
        int horizontalLines = gy + 1;
        int totalLines = verticalLines + horizontalLines;

        Vector3[] verts = new Vector3[totalLines * 4];
        int[] tris = new int[totalLines * 6];
        Color[] cols = new Color[verts.Length];

        int vi = 0, ti = 0;
        float halfW = Mathf.Max(0.0001f, lineWidth * 0.5f);

        // Vertical lines (parallel to local Z)
        for (int x = 0; x <= gx; x++)
        {
            float t = gx == 0 ? 0f : (float)x / gx;
            float px = Mathf.Lerp(-w * 0.5f, w * 0.5f, t);
            Vector3 a = new Vector3(px, 0f, -d * 0.5f);
            Vector3 b = new Vector3(px, 0f, d * 0.5f);

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

            vi += 4; ti += 6;
        }

        // Horizontal lines (parallel to local X)
        for (int y = 0; y <= gy; y++)
        {
            float t = gy == 0 ? 0f : (float)y / gy;
            float pz = Mathf.Lerp(-d * 0.5f, d * 0.5f, t);

            Vector3 a = new Vector3(-w * 0.5f, 0f, pz);
            Vector3 b = new Vector3(w * 0.5f, 0f, pz);

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

            vi += 4; ti += 6;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.colors = cols;
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        if (mr && mr.sharedMaterial) mr.sharedMaterial.color = lineColor;
    }

    private void FollowHostTopPlane()
    {
        Vector3 centerW, xAxisW, zAxisW, normalW;
        float width, depth;
        ComputeTopPlane(out centerW, out xAxisW, out zAxisW, out normalW, out width, out depth);

        transform.position = centerW + normalW * yOffset;
        transform.rotation = Quaternion.LookRotation(zAxisW, normalW);
    }

    /// <summary>
    /// Derive top center/axes/size using BoxCollider if present; else from bounds.
    /// Axes come from the host’s transform so rotation is respected.
    /// </summary>
    private void ComputeTopPlane(out Vector3 centerW, out Vector3 xAxisW, out Vector3 zAxisW, out Vector3 normalW,
                                 out float width, out float depth)
    {
        xAxisW = host.transform.right.normalized;   // along host local +X across the top
        zAxisW = host.transform.forward.normalized; // along host local +Z across the top
        normalW = host.transform.up.normalized;     // host's “top” normal

        // Prefer BoxCollider for exact top face
        BoxCollider bc = host.GetComponentInChildren<BoxCollider>();
        if (bc != null)
        {
            Transform t = bc.transform;

            // world-space half-size, respecting lossyScale
            Vector3 s = Vector3.Scale(bc.size, new Vector3(Mathf.Abs(t.lossyScale.x),
                                                           Mathf.Abs(t.lossyScale.y),
                                                           Mathf.Abs(t.lossyScale.z)));
            width = Mathf.Max(0.001f, s.x);
            depth = Mathf.Max(0.001f, s.z);

            // top face center in world
            Vector3 topLocal = bc.center + new Vector3(0f, bc.size.y * 0.5f, 0f);
            centerW = t.TransformPoint(topLocal);
            return;
        }

        // Fallback: use renderer/collider bounds (axis-aligned), then project/average top corners
        Bounds b;
        Collider anyCol = host.GetComponentInChildren<Collider>();
        if (anyCol != null) b = anyCol.bounds;
        else
        {
            Renderer r = host.GetComponentInChildren<Renderer>();
            if (r == null)
            {
                // last-resort defaults
                centerW = host.transform.position;
                width = depth = 0.5f;
                return;
            }
            b = r.bounds;
        }

        // 4 top corners of AABB
        float yTop = b.max.y;
        Vector3 p1 = new Vector3(b.min.x, yTop, b.min.z);
        Vector3 p2 = new Vector3(b.max.x, yTop, b.min.z);
        Vector3 p3 = new Vector3(b.max.x, yTop, b.max.z);
        Vector3 p4 = new Vector3(b.min.x, yTop, b.max.z);

        // Width/depth measured along the host’s X and Z axes
        float minX = Mathf.Min(Vector3.Dot(p1, xAxisW), Vector3.Dot(p2, xAxisW),
                               Vector3.Dot(p3, xAxisW), Vector3.Dot(p4, xAxisW));
        float maxX = Mathf.Max(Vector3.Dot(p1, xAxisW), Vector3.Dot(p2, xAxisW),
                               Vector3.Dot(p3, xAxisW), Vector3.Dot(p4, xAxisW));

        float minZ = Mathf.Min(Vector3.Dot(p1, zAxisW), Vector3.Dot(p2, zAxisW),
                               Vector3.Dot(p3, zAxisW), Vector3.Dot(p4, zAxisW));
        float maxZ = Mathf.Max(Vector3.Dot(p1, zAxisW), Vector3.Dot(p2, zAxisW),
                               Vector3.Dot(p3, zAxisW), Vector3.Dot(p4, zAxisW));

        width = Mathf.Max(0.001f, maxX - minX);
        depth = Mathf.Max(0.001f, maxZ - minZ);

        // Center is just the average of the four top corners
        centerW = 0.25f * (p1 + p2 + p3 + p4);
    }
}
