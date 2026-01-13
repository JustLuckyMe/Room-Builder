using UnityEngine;

public class PlacementDebugHub : MonoBehaviour
{
    public PlacementController controller;
    public Vector2 margin = new Vector2(12f, 12f);
    public Vector2 size = new Vector2(420f, 280f);

    private GUIStyle _label;
    private GUIStyle _title;

    void OnGUI()
    {
        if (controller == null || !controller.debugMode) return;
        EnsureStyles();

        var d = controller.debug;
        Rect rect = new Rect(margin.x, margin.y, size.x, size.y);

        GUILayout.BeginArea(rect, "Placement Debug", _title);
        GUILayout.Space(6);

        GUILayout.Label($"<b>Valid Here</b>: {d.validHere}", _label);

        GUILayout.Space(4);
        GUILayout.Label($"<b>Surface</b>: hit={d.hasSurfaceHit}, tag={d.surfaceTag}, floor={d.isFloor}, wall={d.isWall}", _label);
        if (d.hasSurfaceHit)
            GUILayout.Label($"<b>Hit Point</b>: {Fmt(d.surfaceHitPoint)}", _label);

        GUILayout.Space(6);
        GUILayout.Label("<b>Stacking</b>:", _label);
        GUILayout.Label($"- Attempt: {d.isStackingAttempt}", _label);
        GUILayout.Label($"- Host: {(d.hostObject ? d.hostObject.name : "null")}", _label);
        GUILayout.Label($"- Host Box: {(d.hostBox ? d.hostBox.name : "null")}", _label);

        // New: face diagnostics (shown regardless of pass/fail)
        if (d.isStackingAttempt && d.hostBox != null)
        {
            GUILayout.Space(2);
            GUILayout.Label("<b>Face Diagnostics</b>:", _label);
            GUILayout.Label($"- hasHostHit: {d.hasHostHit}", _label);
            GUILayout.Label($"- hitFace: {d.hitFace}  (angle={d.hitFaceAngleDeg:F1}°)", _label);
            GUILayout.Label($"- allowedFace: {d.allowedFace}  (angle={d.allowedFaceAngleDeg:F1}°)", _label);
            GUILayout.Label($"- passedAllowedFaceTest: {d.passedAllowedFaceTest}", _label);

            if (d.atopSnappedPoint != Vector3.zero)
                GUILayout.Label($"- SnapPoint: {Fmt(d.atopSnappedPoint)}", _label);
        }

        GUILayout.Space(6);
        if (d.isFloor && d.floorCell.x != int.MinValue)
            GUILayout.Label($"<b>Floor Cell</b>: {d.floorCell}", _label);
        if (d.isWall && d.wallCell.x != int.MinValue)
            GUILayout.Label($"<b>Wall Cell</b>: {d.wallCell}", _label);

        if (d.occupiedCells != null && d.occupiedCells.Count > 0)
            GUILayout.Label($"<b>Occupied Cells</b>: {d.occupiedCells.Count}", _label);

        GUILayout.Space(6);
        GUILayout.Label($"Toggle: {controller.toggleDebugKey}  |  Gizmos in Scene view", _label);

        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        if (_label == null)
        {
            _label = new GUIStyle(GUI.skin.label);
            _label.fontSize = 12;
            _label.richText = true;
        }

        if (_title == null)
        {
            _title = new GUIStyle(GUI.skin.box);
            _title.fontSize = 14;
            _title.alignment = TextAnchor.MiddleLeft;
        }
    }

    private string Fmt(Vector3 v)
    {
        return $"{v.x:F3}, {v.y:F3}, {v.z:F3}";
    }
}
