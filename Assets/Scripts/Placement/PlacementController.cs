using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementController : MonoBehaviour
{
    #region inspector_refs_and_settings
    [Header("Refs")]
    public Camera cam;
    public FloorGridSystem grid;

    [Header("Masks")]
    public LayerMask buildSurfaceMask;
    public LayerMask placeableMask;

    [Header("Placement Indicator")]
    public Color validTint = new Color(0.6f, 1f, 0.6f, 0.85f);
    public Color invalidTint = new Color(1f, 0.5f, 0.5f, 0.85f);

    [Header("Optional")]
    public GridOverlay gridOverlay;

    [Header("Controls")]
    public KeyCode rotateCWKey = KeyCode.E;
    public KeyCode rotateCCWKey = KeyCode.Q;
    public KeyCode placeKey = KeyCode.Mouse0;
    public KeyCode cancelMouseKey = KeyCode.Mouse1;
    public KeyCode cancelKey = KeyCode.Escape;

    [Header("Tags")]
    public string floorTag = "Floor";
    public string wallTag = "Wall";

    [Header("Stacking On Top (Collider-face-based)")]
    public float atopCellSize = 0.25f;
    public float atopEdgePadding = 0.02f;
    public float atopYOffset = 0.001f;

    // Parenting support for stacked placement
    private PlaceableObject lastStackHost; // when stacking this frame, we parent to this host on commit
    private Transform origParent;          // original parent when repositioning existing items

    [Header("Stacking Face Filtering")]
    public bool requireHitOnAllowedFace = true;
    public float faceToleranceDegrees = 20f;

    [Header("Debug")]
    public bool debugMode = true;
    public KeyCode toggleDebugKey = KeyCode.F1;
    #endregion

    #region debug_state
    [System.Serializable]
    public class PlacementDebugState
    {
        public bool hasSurfaceHit;
        public Vector3 surfaceHitPoint;
        public string surfaceTag;
        public bool isFloor;
        public bool isWall;

        public bool isStackingAttempt;
        public PlaceableObject hostObject;
        public BoxCollider hostBox;

        public bool hasHostHit;
        public WallSide hitFace;
        public float hitFaceAngleDeg;
        public WallSide allowedFace;
        public float allowedFaceAngleDeg;
        public bool passedAllowedFaceTest;

        public Vector3 faceCenter;
        public Vector3 faceNormal;
        public Vector3 atopSnappedPoint;

        public bool validHere;
        public Vector2Int floorCell;
        public Vector2Int wallCell;

        public List<Vector2Int> occupiedCells = new List<Vector2Int>();
    }
    public PlacementDebugState debug = new PlacementDebugState();
    #endregion

    #region runtime_state
    private enum HoldState { None, NewInstance, RepositionExisting }

    private HoldState holdState = HoldState.None;
    private PlaceableObject held;
    private GameObject holdPrefab;

    private Vector2Int lastCell = new Vector2Int(int.MinValue, int.MinValue);
    private readonly List<Vector2Int> currentCells = new List<Vector2Int>();
    private bool validHere;

    private Vector3 origPos;
    private Quaternion origRot;
    private List<Vector2Int> origCells = new List<Vector2Int>();
    private int origLayer = -1;

    private Renderer[] heldRenderers;
    private readonly Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();
    private bool lastTintValid = false;
    private bool tintApplied = false;

    private WallGridSystem currentWallAnchor;
    private WallGridSystem origWallAnchor;

    private Quaternion holdStartRot;
    private bool preserveXZOnPickupOnce = false;
    #endregion

    #region unity_loop
    void Update()
    {
        if (Input.GetKeyDown(toggleDebugKey)) debugMode = !debugMode;

        if (holdState != HoldState.None && held != null)
        {
            if (Input.GetKeyDown(rotateCWKey)) Rotate(+1);
            if (Input.GetKeyDown(rotateCCWKey)) Rotate(-1);

            if (Input.GetKeyDown(cancelKey) || Input.GetKeyDown(cancelMouseKey))
            {
                CancelOperation();
                return;
            }

            UpdateFollowAndTint();

            if (Input.GetKeyDown(placeKey) && validHere)
            {
                CommitPlacement();
                return;
            }
        }
        else
        {
            if (Input.GetKeyDown(placeKey))
            {
                TryBeginRepositionUnderCursor();
            }
        }
    }
    #endregion

    #region public_api_pickup_and_reposition
    public void PickupPrefab(GameObject prefab)
    {
        if (!prefab) { Debug.LogWarning("PickupPrefab: prefab is null."); return; }

        ResetAllState();

        var go = Instantiate(prefab);
        go.name = prefab.name + "_Held";

        held = go.GetComponent<PlaceableObject>() ?? go.AddComponent<PlaceableObject>();
        holdPrefab = prefab;

        holdStartRot = held.transform.rotation;
        EnsurePlacementSpotExists(held);

        holdState = HoldState.NewInstance;
        origLayer = go.layer;

        CacheRenderersAndColors(go);
        ApplyTint(false);

        lastCell = new Vector2Int(int.MinValue, int.MinValue);
        if (gridOverlay) gridOverlay.SetVisible(true);
    }

    private void TryBeginRepositionUnderCursor()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        if (!cam) { Debug.LogWarning("PlacementController: Camera not assigned."); return; }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 400f, placeableMask))
            return;

        var p = hit.collider.GetComponentInParent<PlaceableObject>();
        if (!p) return;

        EnsurePlacementSpotExists(p);

        origPos = p.transform.position;
        origRot = p.transform.rotation;

        if (p.placementSurface == PlacementSurface.Floor)
        {
            Vector2Int anchor = p.GetAnchorCell(grid);
            origCells = p.GetOccupiedCells(anchor);
            grid.Free(origCells);
            origWallAnchor = null;
        }
        else
        {
            if (TryFindWallAnchorFor(p, out var wallAnchor, out var wallHit))
            {
                Vector2Int cell = wallAnchor.WorldToCell(p.GetSpotWorld());
                origCells = p.GetOccupiedCells(cell);
                wallAnchor.Free(origCells);
                origWallAnchor = wallAnchor;
            }
            else
            {
                origCells = new List<Vector2Int>();
                origWallAnchor = null;
            }
        }

        held = p;

        // remember and detach parent while moving
        origParent = p.transform.parent;
        p.transform.SetParent(null, true);

        holdStartRot = held.transform.rotation;
        holdPrefab = p.sourcePrefab != null ? p.sourcePrefab : p.gameObject;
        holdState = HoldState.RepositionExisting;

        origLayer = held.gameObject.layer;

        CacheRenderersAndColors(held.gameObject);
        ApplyTint(false);

        lastCell = new Vector2Int(int.MinValue, int.MinValue);
        currentWallAnchor = null;

        preserveXZOnPickupOnce = (p.placementSurface == PlacementSurface.Floor);

        if (gridOverlay) gridOverlay.SetVisible(true);
    }
    #endregion

    #region anchors_and_spot_utilities
    private bool TryFindWallAnchorFor(PlaceableObject p, out WallGridSystem anchor, out RaycastHit hit)
    {
        Vector3 origin = p.GetSpotWorld();
        Vector3 intoWallWorld = -p.transform.TransformDirection(p.GetWallSideLocal());
        Vector3 start = origin + intoWallWorld * 0.05f;

        if (Physics.Raycast(start, intoWallWorld, out hit, 3f, buildSurfaceMask))
        {
            anchor = hit.collider.GetComponentInParent<WallGridSystem>();
            if (anchor != null) return true;
        }

        Collider[] hits = Physics.OverlapSphere(origin, 0.2f, buildSurfaceMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var a = hits[i].GetComponentInParent<WallGridSystem>();
            if (a != null)
            {
                anchor = a;
                hit = default;
                return true;
            }
        }

        anchor = null;
        hit = default;
        return false;
    }

    private void EnsurePlacementSpotExists(PlaceableObject obj)
    {
        if (!obj) return;
        if (obj.PlacementSpot == null)
        {
            var spot = new GameObject("PlacementSpot");
            spot.transform.SetParent(obj.transform);
            spot.transform.localPosition = Vector3.zero;
            spot.transform.localRotation = Quaternion.identity;
            obj.PlacementSpot = spot;
        }
    }

    private float SurfaceYAt(float x, float z)
    {
        RaycastHit hit;
        Vector3 fromTop = new Vector3(x, grid.origin.y + 1000f, z);
        if (Physics.Raycast(fromTop, Vector3.down, out hit, 5000f, buildSurfaceMask))
            return hit.point.y;
        Vector3 fromBottom = new Vector3(x, grid.origin.y - 1000f, z);
        if (Physics.Raycast(fromBottom, Vector3.up, out hit, 5000f, buildSurfaceMask))
            return hit.point.y;
        return grid.origin.y;
    }
    #endregion

    #region host_detection_and_face_math
    private bool TryFindHostUnderCursor(out PlaceableObject host, out Vector3 hostPoint, out Vector3 hostNormal, out Collider hostCollider, float radius = 0.06f, float maxDist = 500f)
    {
        host = null;
        hostPoint = Vector3.zero;
        hostNormal = Vector3.up;
        hostCollider = null;

        if (!cam) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        RaycastHit[] sphereHits = Physics.SphereCastAll(ray, radius, maxDist, placeableMask, QueryTriggerInteraction.Ignore);
        if (sphereHits != null && sphereHits.Length > 0)
        {
            System.Array.Sort(sphereHits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < sphereHits.Length; i++)
            {
                var p = sphereHits[i].collider ? sphereHits[i].collider.GetComponentInParent<PlaceableObject>() : null;
                if (p != null && p.allowsPlacementOnTop)
                {
                    host = p;
                    hostPoint = sphereHits[i].point;
                    hostNormal = sphereHits[i].normal;
                    hostCollider = sphereHits[i].collider;
                    return true;
                }
            }
        }

        if (Physics.Raycast(ray, out var buildHit, maxDist, buildSurfaceMask, QueryTriggerInteraction.Ignore))
        {
            const float searchRadius = 0.15f;
            Collider[] near = Physics.OverlapSphere(buildHit.point + Vector3.up * 0.02f, searchRadius, placeableMask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            PlaceableObject best = null;
            Collider bestCol = null;

            for (int i = 0; i < near.Length; i++)
            {
                var p = near[i].GetComponentInParent<PlaceableObject>();
                if (p == null || !p.allowsPlacementOnTop) continue;

                float d = Vector3.SqrMagnitude(near[i].ClosestPoint(buildHit.point) - ray.origin);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                    bestCol = near[i];
                }
            }

            if (best != null)
            {
                host = best;
                hostCollider = bestCol;

                if (bestCol.Raycast(ray, out var forcedHit, maxDist))
                {
                    hostPoint = forcedHit.point;
                    hostNormal = forcedHit.normal;
                    return true;
                }

                Vector3 cp = bestCol.ClosestPoint(buildHit.point);
                hostPoint = cp;

                Vector3 center = bestCol.bounds.center;
                Vector3 n = (cp - center);
                hostNormal = n.sqrMagnitude > 1e-6f ? n.normalized : Vector3.up;

                return true;
            }
        }

        return false;
    }

    private bool TryGetColliderFace(BoxCollider bc, WallSide face,
                                    out Vector3 faceCenter,
                                    out Vector3 uAxis, out Vector3 vAxis,
                                    out float halfU, out float halfV,
                                    out Vector3 normalOut)
    {
        faceCenter = uAxis = vAxis = normalOut = Vector3.zero;
        halfU = halfV = 0f;
        if (!bc) return false;

        Transform t = bc.transform;

        Vector3 worldCenter = t.TransformPoint(bc.center);
        Vector3 localExtents = 0.5f * bc.size;
        Vector3 absScale = new Vector3(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y), Mathf.Abs(t.lossyScale.z));
        Vector3 wExt = Vector3.Scale(localExtents, absScale);

        Vector3 r = t.right.normalized;
        Vector3 u = t.up.normalized;
        Vector3 f = t.forward.normalized;

        switch (face)
        {
            case WallSide.Up: normalOut = u; uAxis = r; vAxis = f; halfU = wExt.x; halfV = wExt.z; faceCenter = worldCenter + u * wExt.y; break;
            case WallSide.Down: normalOut = -u; uAxis = r; vAxis = f; halfU = wExt.x; halfV = wExt.z; faceCenter = worldCenter - u * wExt.y; break;
            case WallSide.Forward: normalOut = f; uAxis = r; vAxis = u; halfU = wExt.x; halfV = wExt.y; faceCenter = worldCenter + f * wExt.z; break;
            case WallSide.Back: normalOut = -f; uAxis = r; vAxis = u; halfU = wExt.x; halfV = wExt.y; faceCenter = worldCenter - f * wExt.z; break;
            case WallSide.Right: normalOut = r; uAxis = f; vAxis = u; halfU = wExt.z; halfV = wExt.y; faceCenter = worldCenter + r * wExt.x; break;
            case WallSide.Left: normalOut = -r; uAxis = f; vAxis = u; halfU = wExt.z; halfV = wExt.y; faceCenter = worldCenter - r * wExt.x; break;
            default: return false;
        }
        return true;
    }

    private bool SnapOnColliderFace(BoxCollider bc, Vector3 worldPoint, WallSide face, float cellSize, float edgePadding,
                                    out Vector3 snappedWorld, out Vector3 faceNormal, out Vector3 faceCenterOut)
    {
        snappedWorld = Vector3.zero;
        faceNormal = Vector3.up;
        faceCenterOut = Vector3.zero;

        if (!TryGetColliderFace(bc, face, out var center, out var uAxis, out var vAxis, out var halfU, out var halfV, out var n))
            return false;

        faceCenterOut = center;
        faceNormal = n;

        Vector3 fromCenter = worldPoint - center;
        float height = Vector3.Dot(fromCenter, n);
        Vector3 onPlane = worldPoint - n * height;

        Vector3 d = onPlane - center;
        float u = Vector3.Dot(d, uAxis);
        float v = Vector3.Dot(d, vAxis);

        float maxU = Mathf.Max(0f, halfU - edgePadding);
        float maxV = Mathf.Max(0f, halfV - edgePadding);
        u = Mathf.Clamp(u, -maxU, maxU);
        v = Mathf.Clamp(v, -maxV, maxV);

        if (cellSize > 0.0001f)
        {
            u = Mathf.Round(u / cellSize) * cellSize;
            v = Mathf.Round(v / cellSize) * cellSize;
        }

        snappedWorld = center + uAxis * u + vAxis * v;
        return true;
    }

    private bool TryGetHitFace(BoxCollider bc, Vector3 contactNormal, out WallSide faceHit, out float angleDeg)
    {
        faceHit = WallSide.Up;
        angleDeg = 180f;
        if (bc == null) return false;

        Transform t = bc.transform;
        Vector3 n = contactNormal.normalized;

        (Vector3 dir, WallSide face)[] candidates = new (Vector3, WallSide)[]
        {
            ( t.up,      WallSide.Up ),
            ( -t.up,     WallSide.Down ),
            ( t.right,   WallSide.Right ),
            ( -t.right,  WallSide.Left ),
            ( t.forward, WallSide.Forward ),
            ( -t.forward,WallSide.Back )
        };

        float best = 999f;
        WallSide bestFace = WallSide.Up;

        for (int i = 0; i < candidates.Length; i++)
        {
            float a = Vector3.Angle(n, candidates[i].dir);
            if (a < best)
            {
                best = a;
                bestFace = candidates[i].face;
            }
        }

        faceHit = bestFace;
        angleDeg = best;
        return true;
    }
    #endregion

    #region follow_tint_and_validation
    private void UpdateFollowAndTint()
    {
        // reset debug
        if (debugMode)
        {
            debug.hasSurfaceHit = false;
            debug.surfaceHitPoint = Vector3.zero;
            debug.surfaceTag = "";
            debug.isFloor = debug.isWall = false;

            debug.isStackingAttempt = false;
            debug.hostObject = null;
            debug.hostBox = null;

            debug.hasHostHit = false;
            debug.hitFace = WallSide.Up;
            debug.hitFaceAngleDeg = 0f;
            debug.allowedFace = WallSide.Up;
            debug.allowedFaceAngleDeg = 0f;
            debug.passedAllowedFaceTest = false;

            debug.faceCenter = Vector3.zero;
            debug.faceNormal = Vector3.zero;
            debug.atopSnappedPoint = Vector3.zero;

            debug.validHere = false;
            debug.floorCell = new Vector2Int(int.MinValue, int.MinValue);
            debug.wallCell = new Vector2Int(int.MinValue, int.MinValue);
            debug.occupiedCells.Clear();
        }

        if (!RayToBuildSurface(out Vector3 hitPoint, out RaycastHit surfaceHit))
            return;

        if (debugMode)
        {
            debug.hasSurfaceHit = true;
            debug.surfaceHitPoint = hitPoint;
            debug.surfaceTag = surfaceHit.collider ? surfaceHit.collider.tag : "";
        }

        bool hitIsFloor = surfaceHit.collider != null && surfaceHit.collider.CompareTag(floorTag);
        bool hitIsWall = surfaceHit.collider != null && surfaceHit.collider.CompareTag(wallTag);
        if (debugMode) { debug.isFloor = hitIsFloor; debug.isWall = hitIsWall; }

        // FLOOR-placed objects
        if (held.placementSurface == PlacementSurface.Floor)
        {
            if (held.canBePlacedOnTopOfOthers &&
                TryFindHostUnderCursor(out var host, out var hostPoint, out var hostNormal, out var hitCol) &&
                host != null && host.allowsPlacementOnTop)
            {
                if (host == held)
                {
                    lastStackHost = null;
                    validHere = false;
                    if (!tintApplied || lastTintValid != false) ApplyTint(false);
                    if (debugMode) { debug.isStackingAttempt = true; debug.validHere = false; }
                    return;
                }

                if (debugMode) { debug.isStackingAttempt = true; debug.hostObject = host; }

                var hostBox = (hitCol as BoxCollider) ?? host.GetComponentInChildren<BoxCollider>();
                if (hostBox != null)
                {
                    if (TryGetHitFace(hostBox, hostNormal, out var faceHit, out var hitAngle))
                    {
                        if (debugMode)
                        {
                            debug.hasHostHit = true;
                            debug.hostBox = hostBox;
                            debug.hitFace = faceHit;
                            debug.hitFaceAngleDeg = hitAngle;
                            debug.allowedFace = host.stackingHostFace;
                        }

                        if (TryGetColliderFace(hostBox, host.stackingHostFace, out _, out _, out _, out _, out _, out var allowedNormal))
                        {
                            float allowedAngle = Vector3.Angle(hostNormal, allowedNormal);
                            if (debugMode) debug.allowedFaceAngleDeg = allowedAngle;
                        }

                        WallSide faceToUse = faceHit;
                        bool passedFilter = true;

                        if (requireHitOnAllowedFace)
                        {
                            if (TryGetColliderFace(hostBox, host.stackingHostFace, out _, out _, out _, out _, out _, out var expectedNormal))
                            {
                                float faceAlignAngle = Vector3.Angle(hostNormal, expectedNormal);
                                passedFilter = faceAlignAngle <= faceToleranceDegrees;
                                faceToUse = host.stackingHostFace;
                                if (debugMode) debug.passedAllowedFaceTest = passedFilter;
                            }
                            else
                            {
                                passedFilter = false;
                                if (debugMode) debug.passedAllowedFaceTest = false;
                            }
                        }
                        else
                        {
                            if (debugMode) debug.passedAllowedFaceTest = true;
                        }

                        if (!passedFilter)
                        {
                            lastStackHost = null;
                            validHere = false;
                            if (!tintApplied || lastTintValid != false) ApplyTint(false);
                            return;
                        }

                        if (SnapOnColliderFace(hostBox, hostPoint, faceToUse, atopCellSize, atopEdgePadding,
                                               out var facePoint, out var faceNormal, out var faceCenter))
                        {
                            if (debugMode)
                            {
                                debug.faceCenter = faceCenter;
                                debug.faceNormal = faceNormal;
                                debug.atopSnappedPoint = facePoint;
                            }

                            Vector3 finalPos = facePoint + faceNormal.normalized * atopYOffset;
                            held.SnapSpotToWorld(finalPos);

                            // mark that we stacked successfully this frame
                            lastStackHost = host;

                            currentCells.Clear(); // stacking does not consume floor cells
                            validHere = true;
                            if (debugMode) debug.validHere = true;

                            if (!tintApplied || validHere != lastTintValid) ApplyTint(true);
                            lastCell = new Vector2Int(int.MinValue, int.MinValue);
                            return;
                        }
                        else
                        {
                            lastStackHost = null;
                        }
                    }
                    else
                    {
                        lastStackHost = null;
                    }
                }
                else
                {
                    lastStackHost = null;
                }

                validHere = false;
                if (debugMode) debug.validHere = false;
                if (!tintApplied || lastTintValid != false) ApplyTint(false);
                return;
            }

            // Normal floor grid placement (no stacking)
            lastStackHost = null;

            if (!hitIsFloor)
            {
                validHere = false;
                if (debugMode) debug.validHere = false;
                if (!tintApplied || lastTintValid != false) ApplyTint(false);
                return;
            }

            if (holdState == HoldState.RepositionExisting && preserveXZOnPickupOnce)
            {
                Vector3 spot = held.GetSpotWorld();
                float y = SurfaceYAt(spot.x, spot.z);
                Vector3 targetWorld = new Vector3(spot.x, y, spot.z);
                Vector2Int targetCell = grid.WorldToCell(targetWorld);
                SnapToWorldCell(targetCell, targetWorld);

                if (debugMode)
                {
                    debug.floorCell = targetCell;
                    debug.validHere = validHere;
                    debug.occupiedCells.Clear();
                    debug.occupiedCells.AddRange(currentCells);
                }

                preserveXZOnPickupOnce = false;
                return;
            }

            Vector2Int targetCellFollow = grid.WorldToCell(hitPoint);
            Vector3 cellWorld = grid.CellToWorld(targetCellFollow);
            float yy = SurfaceYAt(cellWorld.x, cellWorld.z);
            Vector3 targetWorldFollow = new Vector3(cellWorld.x, yy, cellWorld.z);
            SnapToWorldCell(targetCellFollow, targetWorldFollow);

            if (debugMode)
            {
                debug.floorCell = targetCellFollow;
                debug.validHere = validHere;
                debug.occupiedCells.Clear();
                debug.occupiedCells.AddRange(currentCells);
            }
            return;
        }

        // WALL objects (no stacking)
        lastStackHost = null;

        if (!hitIsWall)
        {
            validHere = false;
            if (debugMode) debug.validHere = false;
            if (!tintApplied || lastTintValid != false) ApplyTint(false);
            return;
        }

        var anchor = surfaceHit.collider.GetComponentInParent<WallGridSystem>();
        if (!anchor)
        {
            validHere = false;
            if (debugMode) debug.validHere = false;
            if (!tintApplied || lastTintValid != false) ApplyTint(false);
            return;
        }
        currentWallAnchor = anchor;

        Vector2Int wallCell = anchor.WorldToCell(surfaceHit.point);
        Vector3 targetWorldWall = anchor.CellToWorld(wallCell);

        Vector3 sideLocal = held.GetWallSideLocal();
        Vector3 sideWorldAtStart = holdStartRot * sideLocal;
        Quaternion align = Quaternion.FromToRotation(sideWorldAtStart, -anchor.NormalN);
        held.transform.rotation = align * holdStartRot;

        held.SnapSpotToWorld(targetWorldWall);

        currentCells.Clear();
        currentCells.AddRange(held.GetOccupiedCells(wallCell));
        bool inBounds = anchor.InBounds(wallCell);
        validHere = inBounds && anchor.AreCellsFree(currentCells);

        if (debugMode)
        {
            debug.wallCell = wallCell;
            debug.validHere = validHere;
            debug.occupiedCells.Clear();
            debug.occupiedCells.AddRange(currentCells);
        }

        if (!tintApplied || validHere != lastTintValid) ApplyTint(validHere);
        lastCell = new Vector2Int(int.MinValue, int.MinValue);
    }

    private void SnapToWorldCell(Vector2Int targetCell, Vector3 targetWorld)
    {
        if (targetCell != lastCell || currentCells.Count == 0)
        {
            lastCell = targetCell;

            held.SnapSpotToWorld(targetWorld);

            currentCells.Clear();
            currentCells.AddRange(held.GetOccupiedCells(targetCell));
            validHere = grid.AreCellsFree(currentCells);

            if (debugMode)
            {
                debug.floorCell = targetCell;
                debug.validHere = validHere;
                debug.occupiedCells.Clear();
                debug.occupiedCells.AddRange(currentCells);
            }

            if (!tintApplied || validHere != lastTintValid)
                ApplyTint(validHere);
        }
        else
        {
            held.SnapSpotToWorld(targetWorld);
        }
    }

    private bool RayToBuildSurface(out Vector3 hitPoint, out RaycastHit hitInfo)
    {
        hitPoint = Vector3.zero;
        hitInfo = default;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;
        if (!cam) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hitInfo, 500f, buildSurfaceMask))
        {
            hitPoint = hitInfo.point;
            return true;
        }
        return false;
    }

    private void Rotate(int dir)
    {
        int stepsNow = Mathf.RoundToInt(held.transform.eulerAngles.y / 90f) + dir;
        held.SetRotationSteps(stepsNow);
        lastCell = new Vector2Int(int.MinValue, int.MinValue);
    }
    #endregion

    #region commit_cancel_reset
    private void CommitPlacement()
    {
        // Parent to host if we stacked this frame
        if (held != null && lastStackHost != null)
        {
            held.transform.SetParent(lastStackHost.transform, true); // keep world pose
        }

        if (held != null && currentCells.Count > 0)
        {
            if (held.placementSurface == PlacementSurface.Floor)
            {
                grid.Occupy(currentCells);
            }
            else if (currentWallAnchor != null)
            {
                currentWallAnchor.Occupy(currentCells);
            }
        }

        RestoreOriginalColors();

        if (gridOverlay) gridOverlay.SetVisible(false);
        holdState = HoldState.None;
        held = null;
        holdPrefab = null;
        currentCells.Clear();
        lastCell = new Vector2Int(int.MinValue, int.MinValue);
        validHere = false;
        origCells.Clear();
        origLayer = -1;
        heldRenderers = null;
        originalColors.Clear();
        tintApplied = false;
        currentWallAnchor = null;
        origWallAnchor = null;

        // clear parenting trackers
        lastStackHost = null;
        origParent = null;
    }

    private void CancelOperation()
    {
        if (holdState == HoldState.RepositionExisting && held != null)
        {
            held.transform.position = origPos;
            held.transform.rotation = origRot;
            RestoreOriginalColors();

            // restore original parent if we detached it
            if (origParent != null)
            {
                held.transform.SetParent(origParent, true);
            }

            if (origCells != null && origCells.Count > 0)
            {
                if (held.placementSurface == PlacementSurface.Floor)
                {
                    grid.Occupy(origCells);
                }
                else if (origWallAnchor != null)
                {
                    origWallAnchor.Occupy(origCells);
                }
            }
        }
        else if (holdState == HoldState.NewInstance && held != null)
        {
            Destroy(held.gameObject);
        }

        if (gridOverlay) gridOverlay.SetVisible(false);
        holdState = HoldState.None;
        held = null;
        holdPrefab = null;
        currentCells.Clear();
        lastCell = new Vector2Int(int.MinValue, int.MinValue);
        validHere = false;
        origCells.Clear();
        origLayer = -1;
        heldRenderers = null;
        originalColors.Clear();
        tintApplied = false;
        currentWallAnchor = null;
        origWallAnchor = null;

        // clear parenting trackers
        lastStackHost = null;
        origParent = null;
    }

    private void ResetAllState()
    {
        if (held != null)
        {
            if (holdState == HoldState.NewInstance)
            {
                Destroy(held.gameObject);
            }
            else if (holdState == HoldState.RepositionExisting)
            {
                held.transform.position = origPos;
                held.transform.rotation = origRot;
                RestoreOriginalColors();

                // restore original parent if we detached it
                if (origParent != null)
                {
                    held.transform.SetParent(origParent, true);
                }

                if (origCells != null && origCells.Count > 0)
                {
                    if (held.placementSurface == PlacementSurface.Floor)
                        grid.Occupy(origCells);
                    else if (origWallAnchor != null)
                        origWallAnchor.Occupy(origCells);
                }
            }
        }

        if (gridOverlay) gridOverlay.SetVisible(false);
        holdState = HoldState.None;
        held = null;
        holdPrefab = null;
        currentCells.Clear();
        lastCell = new Vector2Int(int.MinValue, int.MinValue);
        validHere = false;
        origCells.Clear();
        origLayer = -1;
        heldRenderers = null;
        originalColors.Clear();
        tintApplied = false;
        currentWallAnchor = null;
        origWallAnchor = null;

        // clear parenting trackers
        lastStackHost = null;
        origParent = null;
    }
    #endregion

    #region tint_and_material_cache
    private void CacheRenderersAndColors(GameObject go)
    {
        heldRenderers = go.GetComponentsInChildren<Renderer>(true);
        originalColors.Clear();
        if (heldRenderers == null) return;

        for (int r = 0; r < heldRenderers.Length; r++)
        {
            var rend = heldRenderers[r];
            if (rend == null) continue;

            var mats = rend.materials;
            Color[] cols = new Color[mats.Length];
            for (int i = 0; i < mats.Length; i++) cols[i] = mats[i].color;
            originalColors[rend] = cols;
        }
        tintApplied = false;
    }

    private void ApplyTint(bool valid)
    {
        if (heldRenderers == null) return;

        Color c = valid ? validTint : invalidTint;
        for (int r = 0; r < heldRenderers.Length; r++)
        {
            var rend = heldRenderers[r];
            if (rend == null) continue;

            var mats = rend.materials;
            for (int i = 0; i < mats.Length; i++) mats[i].color = c;
        }
        lastTintValid = valid;
        tintApplied = true;
    }

    private void RestoreOriginalColors()
    {
        if (heldRenderers == null) return;

        for (int r = 0; r < heldRenderers.Length; r++)
        {
            var rend = heldRenderers[r];
            if (rend == null) continue;

            if (!originalColors.TryGetValue(rend, out var cols)) continue;

            var mats = rend.materials;
            int count = Mathf.Min(cols.Length, mats.Length);
            for (int i = 0; i < count; i++) mats[i].color = cols[i];
        }
        tintApplied = false;
    }
    #endregion

    #region gizmos
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!debugMode || held == null) return;

        if (debug.hasSurfaceHit)
        {
            Gizmos.color = debug.validHere ? Color.green : Color.red;
            Gizmos.DrawSphere(debug.surfaceHitPoint, 0.03f);
        }

        if (debug.isStackingAttempt && debug.hostBox != null && debug.faceCenter != Vector3.zero)
        {
            var bc = debug.hostBox;
            WallSide face = debug.hitFace;

            if (TryGetColliderFace(bc, face, out var center, out var uAxis, out var vAxis, out var halfU, out var halfV, out var n))
            {
                Vector3 c1 = center + uAxis * (+halfU) + vAxis * (+halfV);
                Vector3 c2 = center + uAxis * (-halfU) + vAxis * (+halfV);
                Vector3 c3 = center + uAxis * (-halfU) + vAxis * (-halfV);
                Vector3 c4 = center + uAxis * (+halfU) + vAxis * (-halfV);

                Gizmos.color = new Color(0f, 0.6f, 1f, 0.6f);
                Gizmos.DrawLine(c1, c2);
                Gizmos.DrawLine(c2, c3);
                Gizmos.DrawLine(c3, c4);
                Gizmos.DrawLine(c4, c1);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(center, center + n * 0.2f);

                if (debug.atopSnappedPoint != Vector3.zero)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(debug.atopSnappedPoint, 0.025f);
                }
            }
        }
    }
#endif
    #endregion
}