using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class PrefabCreatorWindow : EditorWindow
{
    private ObjectField objectField;
    private TextField nameField;
    private Toggle customName;
    private ObjectField materialField;
    private IntegerField sizeXField;
    private IntegerField sizeYField;
    private EnumField pivotField;
    private EnumField surfaceField;
    private Toggle canBePlacedOnPlaceableToggle;
    private Toggle allowsObjectsOnTopToggle;
    private EnumField wallSideField;
    private Toggle addCollider;

    // style fields
    private EnumField objectType;
    private EnumField seating;
    private EnumField surfaces;
    private EnumField storage;
    private EnumField beds;
    private EnumField kitchen;
    private EnumField bathrom;
    private EnumField office;
    private EnumField decorative;
    private EnumField lighting;
    private EnumField appliances;

    private EnumFlagsField style;

    private EnumField colorType;
    private EnumField singleColor;
    private EnumFlagsField multiColor;

    // debugging fields
    private LayerField layerField;
    private TextField pathField;
    private Button pickPathBtn;
    private Button createBtn;
    private string savePath = "";

    // preview fields
    private IMGUIContainer previewIMGUIC;
    private PreviewRenderUtility previewUtil;
    private GameObject previewGO;
    private Bounds previewBounds;
    private Vector2 orbit = new Vector2(30f, -20f);
    private float distance = 3.0f;
    private float zoomSpeed = 0.5f;
    private float orbitSpeed = 0.5f;
    private Vector2 lastMouse;

    // grid settings
    private bool showPreviewGrid = true;
    private int gridLines = 10;
    private float gridSpacing = 0.25f;
    private float gridYOffset = 0.0f;
    private GameObject previewGridGO;
    private Material previewLineMat;


    [MenuItem("Window/Placeable Prefab Window", false, 13)]
    static void OpenWindow()
    {
        PrefabCreatorWindow win = GetWindow<PrefabCreatorWindow>();
        win.titleContent = new GUIContent("Object Prefab Window");
        win.minSize = new Vector2(750, 500);
    }

    private void OnEnable()
    {
        if (previewUtil != null) return;

        previewUtil = new PreviewRenderUtility();
        previewUtil.cameraFieldOfView = 30f;
        previewUtil.camera.nearClipPlane = 0.01f;
        previewUtil.camera.farClipPlane = 100f;

        // Key light
        var key = new GameObject("KeyLight");
        var kl = key.AddComponent<Light>();
        kl.type = LightType.Directional;
        kl.intensity = 1.2f;
        key.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
        previewUtil.AddSingleGO(key);

        // Fill light
        var fill = new GameObject("FillLight");
        var fl = fill.AddComponent<Light>();
        fl.type = LightType.Directional;
        fl.intensity = 0.8f;
        fill.transform.rotation = Quaternion.Euler(340f, 220f, 0f);
        previewUtil.AddSingleGO(fill);
    }

    private void OnDisable()
    {
        CleanupPreviewGO();
        previewUtil?.Cleanup();
        previewUtil = null;
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.style.backgroundColor = new Color(0.167f, 0.199f, 0.231f);
        
        VisualElement topContainer = new VisualElement();
        topContainer.style.flexDirection = FlexDirection.Column;
        topContainer.style.paddingLeft = 10;
        topContainer.style.backgroundColor = new Color(0.167f, 0.199f, 0.231f);
        root.Add(topContainer);

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.flexGrow = 1;
        row.style.paddingLeft = 8;
        row.style.paddingRight = 8;

        root.Add(row);

        VisualElement leftPanel = new VisualElement();
        leftPanel.style.flexDirection = FlexDirection.Column;
        leftPanel.style.flexGrow = 1;
        leftPanel.style.marginRight = 8;

        row.Add(leftPanel);

        VisualElement rightPanel = new VisualElement();
        rightPanel.style.flexDirection = FlexDirection.Column;
        rightPanel.style.width = 320;
        rightPanel.style.flexShrink = 0;

        row.Add(rightPanel);

        VisualElement bottomContainer = new VisualElement();
        bottomContainer.style.flexDirection = FlexDirection.Column;
        bottomContainer.style.paddingLeft = 10;
        bottomContainer.style.backgroundColor = new Color(0.167f, 0.199f, 0.231f);
        root.Add(bottomContainer);

        #region Title
        Label label = new Label("Object Prefab Creator! :D");
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.fontSize = 18;
        label.style.marginTop = 10;
        label.style.marginBottom = 10;
        topContainer.Add(label);
        #endregion

        #region Box
        VisualElement previewBox = new VisualElement();
        previewBox.style.height = 50;
        previewBox.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);
        previewBox.style.borderTopColor = Color.gray;
        previewBox.style.borderBottomColor = Color.gray;
        previewBox.style.borderLeftColor = Color.gray;
        previewBox.style.borderRightColor = Color.gray;
        previewBox.style.borderTopWidth = 1;
        previewBox.style.borderBottomWidth = 1;
        previewBox.style.borderLeftWidth = 1;
        previewBox.style.borderRightWidth = 1;
        previewBox.style.marginTop = 5;
        previewBox.style.marginRight = 12;
        previewBox.style.marginLeft = 12;
        previewBox.style.marginBottom = 12;
        topContainer.Add(previewBox);

        Label textBox = new Label("ur mama gae");
        textBox.style.unityTextAlign = TextAnchor.MiddleCenter;
        textBox.style.marginTop = 15;
        previewBox.Add(textBox);
        #endregion

        #region Object Preview
        previewIMGUIC = new IMGUIContainer(DrawPreviewGUI);
        previewIMGUIC.style.height = 220;
        previewIMGUIC.style.marginLeft = 8;
        previewIMGUIC.style.marginRight = 8;
        previewIMGUIC.style.marginBottom = 8;
        previewIMGUIC.style.borderTopWidth = 1;
        previewIMGUIC.style.borderBottomWidth = 1;
        previewIMGUIC.style.borderLeftWidth = 1;
        previewIMGUIC.style.borderRightWidth = 1;
        previewIMGUIC.style.borderTopColor = new Color(0.25f, 0.25f, 0.25f);
        previewIMGUIC.style.borderBottomColor = new Color(0.25f, 0.25f, 0.25f);
        previewIMGUIC.style.borderLeftColor = new Color(0.25f, 0.25f, 0.25f);
        previewIMGUIC.style.borderRightColor = new Color(0.25f, 0.25f, 0.25f);
        // mouse input
        previewIMGUIC.RegisterCallback<WheelEvent>(OnPreviewScroll);
        previewIMGUIC.RegisterCallback<MouseDownEvent>(OnPreviewMouseDown);
        previewIMGUIC.RegisterCallback<MouseMoveEvent>(OnPreviewMouseMove);
        rightPanel.Add(previewIMGUIC);
        #endregion

        #region Grid
        Toggle showGridToggle = new Toggle("Show Preview Grid") { value = showPreviewGrid };
        IntegerField gridLinesField = new IntegerField("Grid Lines") { value = gridLines };
        FloatField gridSpacingField = new FloatField("Grid Spacing") { value = gridSpacing };
        FloatField gridYOffsetField = new FloatField("Grid Y Offset") { value = gridYOffset };

        rightPanel.Add(showGridToggle);
        rightPanel.Add(gridLinesField);
        rightPanel.Add(gridSpacingField);
        rightPanel.Add(gridYOffsetField);

        showGridToggle.RegisterValueChangedCallback(evt =>
        {
            showPreviewGrid = evt.newValue;
            UpdatePreviewGridNow();
        });

        gridLinesField.RegisterValueChangedCallback(evt =>
        {
            gridLines = Mathf.Max(1, evt.newValue);
            UpdatePreviewGridNow();
        });

        gridSpacingField.RegisterValueChangedCallback(evt =>
        {
            gridSpacing = Mathf.Max(0.001f, evt.newValue);
            UpdatePreviewGridNow();
        });

        gridYOffsetField.RegisterValueChangedCallback(evt =>
        {
            gridYOffset = evt.newValue;
            UpdatePreviewGridNow();
        });
        #endregion

        // object name
        nameField = new TextField("Object Name")
        {
            value = "",
            isReadOnly = true
        };
        leftPanel.Add(nameField);

        // toggle
        customName = new Toggle("Custom Name") { value = false };
        leftPanel.Add(customName);

        // react to toggle changes
        customName.RegisterValueChangedCallback(evt =>
        {
            nameField.isReadOnly = !evt.newValue;
            nameField.SetEnabled(evt.newValue);
        });

        // source prefab
        objectField = new ObjectField("Object")
        {
            objectType = typeof(GameObject),
            allowSceneObjects = false
        };
        leftPanel.Add(objectField);

        objectField.RegisterValueChangedCallback(evt =>
        {
            var go = evt.newValue as GameObject;
            if (go != null)
                SetupPreviewFromObject(go);
            else
                CleanupPreviewGO();
        });

        // material
        materialField = new ObjectField("Material Override")
        {
            objectType = typeof(Material),
            allowSceneObjects = false
        };
        leftPanel.Add(materialField);

        materialField.RegisterValueChangedCallback(_ =>
        {
            RefreshPreviewMaterial();
        });

        // pivot dropdown
        pivotField = new EnumField("Pivot", PivotMode.Center);
        leftPanel.Add(pivotField);

        // size x/y
        sizeXField = new IntegerField("Grid Size X") { value = 1 };
        sizeYField = new IntegerField("Grid Size Y") { value = 1 };

        leftPanel.Add(sizeXField);
        leftPanel.Add(sizeYField);

        // surface
        surfaceField = new EnumField("Surface", PlacementSurface.Floor);
        leftPanel.Add(surfaceField);

        // wall placement
        wallSideField = new EnumField("Wall Side", WallSide.Back);
        wallSideField.style.display = DisplayStyle.None;
        leftPanel.Add(wallSideField);

        surfaceField.RegisterValueChangedCallback(evt =>
        {
            var sv = (PlacementSurface)evt.newValue;
            wallSideField.style.display = (sv == PlacementSurface.Wall) ? DisplayStyle.Flex : DisplayStyle.None;
        });

        // if the object can be placed on another object or have objects placed on them
        canBePlacedOnPlaceableToggle = new Toggle("Can be placed on top") { value = false };
        allowsObjectsOnTopToggle = new Toggle("Allows objects on top") { value = false };
        leftPanel.Add(canBePlacedOnPlaceableToggle);
        leftPanel.Add(allowsObjectsOnTopToggle);

        // collider
        addCollider = new Toggle("Add collider") { value = true };
        leftPanel.Add(addCollider);

        #region Style
        #region subtitle
        Label styleTitle = new Label("Metadata");
        styleTitle.style.unityTextAlign = TextAnchor.MiddleLeft;
        styleTitle.style.fontSize = 12;
        styleTitle.style.marginTop = 16;
        leftPanel.Add(styleTitle);
        #endregion
         
        objectType = new EnumField("Object Type", Type.Seating);
        seating = new EnumField("Seating Subtype", SeatingSubType.Chair);
        surfaces = new EnumField("Surfaces Subtype", SurfacesSubType.DiningTable);
        storage = new EnumField("Storage Subtype", StorageSubType.Cabinet);
        beds = new EnumField("Beds Subtype", BedsSubType.SingleBed);
        lighting = new EnumField("Lighting Subtype", LightingSubType.CeilingLight);
        appliances = new EnumField("Appliances Subtype", AppliancesSubType.BigAppliances);
        decorative = new EnumField("Decorative Subtype", DecorSubType.Plant);

        style = new EnumFlagsField("Style", StyleType.None);

        colorType = new EnumField("Color Type", ColorType.SingleColor);
        singleColor = new EnumField("Single Color", SingleColor.Red);
        multiColor = new EnumFlagsField("MultiColored", MultiColor.None);

        leftPanel.Add(objectType);
        leftPanel.Add(seating);
        leftPanel.Add(surfaces);
        leftPanel.Add(storage);
        leftPanel.Add(beds);
        leftPanel.Add(lighting);
        leftPanel.Add(appliances);
        leftPanel.Add(decorative);
        
        leftPanel.Add(style);

        leftPanel.Add(colorType);
        leftPanel.Add(singleColor);
        leftPanel.Add(multiColor);

        HideAllSubtypeFields();
        HideColorTypes();

        objectType.RegisterValueChangedCallback(evt =>
        {
            UpdateSubtypeVisibility((Type)evt.newValue);
        });

        colorType.RegisterValueChangedCallback(evt =>
        {
            UpdateColorVisibility((ColorType)evt.newValue);
        });

        UpdateSubtypeVisibility((Type)(Enum)objectType.value);
        UpdateColorVisibility((ColorType)(Enum)colorType.value);
        #endregion

        #region Debugging

        Foldout debugFoldout = new Foldout
        {
            text = "Debugging",
            value = false
        };
        leftPanel.Add(debugFoldout);

        #region layer
        layerField = new LayerField("Layer", 7);
        layerField.SetEnabled(false);
        debugFoldout.Add(layerField);
        #endregion

        #region Path Row
        VisualElement savePathRow = new VisualElement();
        savePathRow.style.flexDirection = FlexDirection.Row;
        savePathRow.style.alignItems = Align.Center;
        savePathRow.style.marginRight = 2;

        pathField = new TextField("Save Path");
        pathField.value = "Assets/_Prefabs/Objects";
        pathField.isReadOnly = true;
        pathField.style.flexGrow = 1;
        pathField.style.flexBasis = 0;
        pathField.style.minWidth = 0;
        pathField.labelElement.style.minWidth = 80;
        pathField.style.marginRight = 6;
        savePathRow.Add(pathField);

        #region Path Button
        pickPathBtn = new Button();
        pickPathBtn.name = "pickPathBtn";
        pickPathBtn.text = "Path...";
        pickPathBtn.clicked += OnPickButtonPathPressed;
        pickPathBtn.style.flexShrink = 0;
        pickPathBtn.style.width = 70;
        savePathRow.Add(pickPathBtn);
        #endregion

        debugFoldout.Add(savePathRow);
        #endregion
        #endregion

        #region Create Button
        createBtn = new Button();
        createBtn.name = "createButton";
        createBtn.text = "Create Prefab!";
        createBtn.style.alignSelf = Align.Center;
        createBtn.style.marginTop = 15;
        createBtn.style.width = 150;
        createBtn.clicked += OnCreateButtonPressed;
        leftPanel.Add(createBtn);
        #endregion
    }

    #region Preview Window
    private void SetupPreviewFromObject(GameObject prefab)
    {
        CleanupPreviewGO();
        if (prefab == null) return;

        // Instantiate
        previewGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (previewGO == null) previewGO = Instantiate(prefab);
        previewGO.hideFlags = HideFlags.HideAndDontSave;

        // Add to preview scene
        previewUtil.AddSingleGO(previewGO);

        // Compute bounds from colliders (fallback to renderers)
        previewBounds = GetColliderOrRendererBounds(previewGO);

        // Frame camera distances based on bounds
        float radius = Mathf.Max(0.01f, previewBounds.extents.magnitude);
        distance = Mathf.Max(0.5f, radius * 2.2f);

        UpdatePreviewGridNow();
    }

    private void CleanupPreviewGO()
    {
        if (previewGridGO != null)
        {
            DestroyImmediate(previewGridGO);
            previewGridGO = null;
        }

        if (previewGO != null)
        {
            DestroyImmediate(previewGO);
            previewGO = null;
        }
    }

    private Bounds GetColliderOrRendererBounds(GameObject go)
    {
        var cols = go.GetComponentsInChildren<Collider>(true);
        bool hasCol = false;
        Bounds b = new Bounds(go.transform.position, Vector3.zero);

        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null) continue;
            if (!hasCol) { b = cols[i].bounds; hasCol = true; }
            else b.Encapsulate(cols[i].bounds);
        }

        if (hasCol) return b;

        var rends = go.GetComponentsInChildren<Renderer>(true);
        bool hasR = false;
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] == null) continue;
            if (!hasR) { b = rends[i].bounds; hasR = true; }
            else b.Encapsulate(rends[i].bounds);
        }

        if (!hasR) b = new Bounds(go.transform.position, Vector3.one * 0.25f);
        return b;
    }

    private void BuildOrUpdatePreviewGrid(Bounds b)
    {
        if (previewGridGO != null)
        {
            DestroyImmediate(previewGridGO);
            previewGridGO = null;
        }

        EnsurePreviewLineMaterial();

        previewGridGO = new GameObject("PreviewGrid");
        previewGridGO.hideFlags = HideFlags.HideAndDontSave;

        var mf = previewGridGO.AddComponent<MeshFilter>();
        var mr = previewGridGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = previewLineMat;

        // Grid plane at bottom of collider bounds
        float y = b.min.y + gridYOffset;

        // Size grid around object footprint
        float sx = Mathf.Max(0.01f, b.size.x);
        float sz = Mathf.Max(0.01f, b.size.z);
        float halfX = Mathf.Max(sx * 0.6f, gridLines * gridSpacing);
        float halfZ = Mathf.Max(sz * 0.6f, gridLines * gridSpacing);

        int nx = Mathf.Clamp(Mathf.RoundToInt(halfX / gridSpacing), 2, 120);
        int nz = Mathf.Clamp(Mathf.RoundToInt(halfZ / gridSpacing), 2, 120);

        Vector3 center = new Vector3(b.center.x, y, b.center.z);

        // Build line mesh
        // Each line has 2 vertices. We'll generate (2*nx+1) + (2*nz+1) lines.
        int lineCount = (2 * nx + 1) + (2 * nz + 1);
        Vector3[] verts = new Vector3[lineCount * 2];
        int[] indices = new int[lineCount * 2];

        int vi = 0;

        // Lines parallel to X (varying Z)
        for (int i = -nz; i <= nz; i++)
        {
            float z = i * gridSpacing;
            verts[vi + 0] = center + new Vector3(-halfX, 0f, z);
            verts[vi + 1] = center + new Vector3(halfX, 0f, z);
            indices[vi + 0] = vi + 0;
            indices[vi + 1] = vi + 1;
            vi += 2;
        }

        // Lines parallel to Z (varying X)
        for (int i = -nx; i <= nx; i++)
        {
            float x = i * gridSpacing;
            verts[vi + 0] = center + new Vector3(x, 0f, -halfZ);
            verts[vi + 1] = center + new Vector3(x, 0f, halfZ);
            indices[vi + 0] = vi + 0;
            indices[vi + 1] = vi + 1;
            vi += 2;
        }

        Mesh m = new Mesh();
        m.name = "PreviewGridMesh";
        m.vertices = verts;
        m.SetIndices(indices, MeshTopology.Lines, 0);
        m.RecalculateBounds();

        mf.sharedMesh = m;

        previewUtil.AddSingleGO(previewGridGO);
    }

    private void EnsurePreviewLineMaterial()
    {
        if (previewLineMat != null) return;

        // Built-in editor line shader
        Shader s = Shader.Find("Hidden/Internal-Colored");
        previewLineMat = new Material(s);
        previewLineMat.hideFlags = HideFlags.HideAndDontSave;

        // Transparent-ish white
        previewLineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        previewLineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        previewLineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        previewLineMat.SetInt("_ZWrite", 0);
        previewLineMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
        previewLineMat.color = new Color(1f, 1f, 1f, 0.25f);
    }

    private void RefreshPreviewMaterial()
    {
        if (previewGO == null) return;
        var mat = materialField.value as Material;
        if (mat == null) return;

        var rends = previewGO.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
        Repaint();
    }

    private void DrawPreviewGUI()
    {
        var r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        EditorGUI.DrawRect(r, new Color(0.08f, 0.09f, 0.1f));

        if (previewUtil == null) return;

        previewUtil.BeginPreview(r, GUIStyle.none);

        // Orbit around bounds center
        Vector3 pivot = (previewGO != null) ? previewBounds.center : Vector3.zero;
        Quaternion camRot = Quaternion.Euler(orbit.y, orbit.x, 0f);
        Vector3 camPos = pivot + camRot * (Vector3.back * distance);

        var cam = previewUtil.camera;
        cam.transform.position = camPos;
        cam.transform.rotation = camRot;
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 1000f;

        previewUtil.Render();

        var tex = previewUtil.EndPreview();
        GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, false);
    }

    private void OnPreviewScroll(WheelEvent evt)
    {
        distance *= 1f + (-evt.delta.y) * 0.05f * zoomSpeed;
        distance = Mathf.Clamp(distance, 0.1f, 100f);
        evt.StopImmediatePropagation();
        Repaint();
    }

    private void OnPreviewMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 0 || evt.button == 1)
        {
            lastMouse = evt.mousePosition;
            previewIMGUIC.CaptureMouse();
            evt.StopImmediatePropagation();
        }
    }

    private void OnPreviewMouseMove(MouseMoveEvent evt)
    {
        if (!previewIMGUIC.HasMouseCapture()) return;

        Vector2 delta = evt.mousePosition - lastMouse;
        lastMouse = evt.mousePosition;

        if (Event.current != null && Event.current.button == 1)
        {
            // right mouse: zoom
            distance *= 1f + (-delta.y) * 0.01f * zoomSpeed;
        }
        else
        {
            // left mouse: orbit
            orbit.x += delta.x * orbitSpeed;
            orbit.y -= delta.y * orbitSpeed;
            orbit.y = Mathf.Clamp(orbit.y, -89f, 89f);
        }

        evt.StopImmediatePropagation();
        Repaint();
    }

    private void UpdatePreviewGridNow()
    {
        if (previewGO == null) return;

        // Always keep bounds up to date in case spacing/offset changed assumptions
        previewBounds = GetColliderOrRendererBounds(previewGO);

        if (showPreviewGrid)
        {
            BuildOrUpdatePreviewGrid(previewBounds);
        }
        else
        {
            if (previewGridGO != null)
            {
                DestroyImmediate(previewGridGO);
                previewGridGO = null;
            }
        }

        previewIMGUIC?.MarkDirtyRepaint();
        Repaint();
    }
    #endregion

    #region Field Control
    private void HideAllSubtypeFields()
    {
        seating.style.display = DisplayStyle.None;
        surfaces.style.display = DisplayStyle.None;
        storage.style.display = DisplayStyle.None;
        beds.style.display = DisplayStyle.None;
        lighting.style.display = DisplayStyle.None;
        appliances.style.display = DisplayStyle.None;
        decorative.style.display = DisplayStyle.None;
    }
    private void HideColorTypes()
    {
        singleColor.style.display = DisplayStyle.None;
        multiColor.style.display = DisplayStyle.None;
    }
    private void UpdateSubtypeVisibility(Type type)
    {
        HideAllSubtypeFields();

        switch (type)
        {
            case Type.Seating:
                seating.style.display = DisplayStyle.Flex;
                break;

            case Type.Surfaces:
                surfaces.style.display = DisplayStyle.Flex;
                break;

            case Type.Storage:
                storage.style.display = DisplayStyle.Flex;
                break;

            case Type.Beds:
                beds.style.display = DisplayStyle.Flex;
                break;

            case Type.Decor:
                decorative.style.display = DisplayStyle.Flex;
                break;

            case Type.Lighting:
                lighting.style.display = DisplayStyle.Flex;
                break;

            case Type.Appliances:
                appliances.style.display = DisplayStyle.Flex;
                break;
        }
    }
    private void UpdateColorVisibility(ColorType type)
    {
        HideColorTypes();

        switch (type)
        {
            case ColorType.SingleColor:
                singleColor.style.display = DisplayStyle.Flex;
                break;

            case ColorType.MultiColor:
                multiColor.style.display = DisplayStyle.Flex;
                break;
        }
    }
    #endregion
    private void OnPickButtonPathPressed()
    {
        Debug.Log("Reissuing new path...");
        string defaultName = !string.IsNullOrWhiteSpace(nameField.value)
            ? nameField.value.Trim()
            : "NewPlaceable";

        if (!defaultName.EndsWith(".prefab"))
            defaultName += ".prefab";

        string picked = EditorUtility.SaveFilePanelInProject(
            "Save Placeable Prefab",
            defaultName,
            "prefab",
            "Select where to save the prefab."
        );

        if (string.IsNullOrEmpty(picked))
            return;

        savePath = picked;

        if (pathField != null)
            pathField.value = System.IO.Path.GetDirectoryName(savePath).Replace("\\", "/");
    }
    private void OnCreateButtonPressed()
    {
        if (string.IsNullOrWhiteSpace(nameField.value))
        {
            EditorUtility.DisplayDialog("Missing Name", "Please enter a name for the prefab.", "OK");
            return;
        }
        if (objectField.value == null)
        {
            EditorUtility.DisplayDialog("Missing Object", "Please assign a GameObject before creating the prefab.", "OK");
            return;
        }

        var src = objectField.value as GameObject;
        var mat = materialField.value as Material;
        string baseName = nameField.value.Trim();
        int sx = Mathf.Max(1, sizeXField.value);
        int sy = Mathf.Max(1, sizeYField.value);
        var pivot = (PivotMode)pivotField.value;
        var surface = (PlacementSurface)surfaceField.value;

        string folderPath = string.IsNullOrWhiteSpace(pathField.value) ? "Assets" : pathField.value.Trim().Replace("\\", "/");
        string fullPath = string.IsNullOrEmpty(savePath)
            ? $"{folderPath}/{baseName}.prefab"
            : savePath;

        GameObject temp = null;
        try
        {
            temp = (GameObject)PrefabUtility.InstantiatePrefab(src);
            if (temp == null) temp = Instantiate(src);
            temp.name = baseName;

            if (!string.IsNullOrEmpty("Object"))
            {
                try
                {
                    temp.tag = "Object";
                }
                catch
                {
                    Debug.LogWarning("Tag 'Object' does not exist. Please create it in the Tags & Layers settings.");
                }
            }

            int placeableLayer = LayerMask.NameToLayer("Placeable");
            if (placeableLayer < 0) placeableLayer = layerField?.value ?? 0;

            SetLayerRecursive(temp, placeableLayer);

            if (mat != null)
            {
                var rends = temp.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                    r.sharedMaterials = mats;
                }
            }

            if (addCollider.value && temp.GetComponentInChildren<Collider>(true) == null)
            {
                if (!temp.TryGetComponent<BoxCollider>(out _)) temp.AddComponent<BoxCollider>();
            }

            var po = temp.GetComponent<PlaceableObject>();
            if (!po) po = temp.AddComponent<PlaceableObject>();
            po.size = new Vector2Int(sx, sy);
            po.pivot = pivot;
            po.placementSurface = surface;
            po.colorable = temp.GetComponentsInChildren<Renderer>(true);
            po.sourcePrefab = src;
            po.wallSide = (WallSide)wallSideField.value;

            po.canBePlacedOnTopOfOthers = canBePlacedOnPlaceableToggle.value;
            po.allowsPlacementOnTop = allowsObjectsOnTopToggle.value;

            // ensure child "PlacementSpot" exists and assign it
            Transform spotT = temp.transform.Find("PlacementSpot");
            GameObject spot;
            if (spotT != null)
            {
                spot = spotT.gameObject;
            }
            else
            {
                spot = new GameObject("PlacementSpot");
                spot.transform.SetParent(temp.transform);
                spot.transform.localPosition = Vector3.zero;
                spot.transform.localRotation = Quaternion.identity;
            }
            po.PlacementSpot = spot;


            var saved = PrefabUtility.SaveAsPrefabAsset(temp, fullPath, out bool success);
            if (!success || saved == null)
            {
                EditorUtility.DisplayDialog("Save Failed", $"Could not save prefab at:\n{fullPath}", "OK");
                return;
            }

            EditorGUIUtility.PingObject(saved);
            Debug.Log("Prefab created at: " + fullPath);
        }
        finally
        {
            if (temp) DestroyImmediate(temp);
            AssetDatabase.Refresh();
        }
    }
    private void SetLayerRecursive(GameObject go, int layer)
    {
        if (!go) return;
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursive(t.gameObject, layer);
    }

}