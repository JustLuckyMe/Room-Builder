using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ObjectDataSO))]
public class ObjectDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty typeProp = serializedObject.FindProperty("ObjectType");

        SerializedProperty seatingProp = serializedObject.FindProperty("Seating");
        SerializedProperty surfacesProp = serializedObject.FindProperty("Surfaces");
        SerializedProperty storageProp = serializedObject.FindProperty("Storage");
        SerializedProperty bedsProp = serializedObject.FindProperty("Beds");
        SerializedProperty lightingProp = serializedObject.FindProperty("Lighting");
        SerializedProperty appliancesProp = serializedObject.FindProperty("Appliances");
        SerializedProperty decorProp = serializedObject.FindProperty("Decor");

        // Style
        SerializedProperty styleProp = serializedObject.FindProperty("Style");

        // Color
        SerializedProperty colorTypeProp = serializedObject.FindProperty("colorType");
        SerializedProperty singleColorProp = serializedObject.FindProperty("singleColor");
        SerializedProperty multiColorProp = serializedObject.FindProperty("multiColor");

        // Type
        EditorGUILayout.PropertyField(typeProp);

        // Subtype based on Type
        Type t = (Type)typeProp.enumValueIndex;

        switch (t)
        {
            case Type.Seating:
                EditorGUILayout.PropertyField(seatingProp);
                break;

            case Type.Surfaces:
                EditorGUILayout.PropertyField(surfacesProp);
                break;

            case Type.Storage:
                EditorGUILayout.PropertyField(storageProp);
                break;

            case Type.Beds:
                EditorGUILayout.PropertyField(bedsProp);
                break;

            case Type.Lighting:
                EditorGUILayout.PropertyField(lightingProp);
                break;

            case Type.Appliances:
                EditorGUILayout.PropertyField(appliancesProp);
                break;

            case Type.Decor:
                EditorGUILayout.PropertyField(decorProp);
                break;
        }

        // Style (flags)
        EditorGUILayout.PropertyField(styleProp);

        // Color
        EditorGUILayout.PropertyField(colorTypeProp);

        ColorType c = (ColorType)colorTypeProp.enumValueIndex;
        switch (c)
        {
            case ColorType.SingleColor:
                EditorGUILayout.PropertyField(singleColorProp);
                break;

            case ColorType.MultiColor:
                EditorGUILayout.PropertyField(multiColorProp);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
