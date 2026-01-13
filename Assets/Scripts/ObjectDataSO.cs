using UnityEngine;

[CreateAssetMenu(fileName = "ObjectData", menuName = "Object/Object Data")]
public class ObjectDataSO : ScriptableObject
{
    public Type ObjectType;

    public SeatingSubType Seating;
    public SurfacesSubType Surfaces;
    public StorageSubType Storage;
    public BedsSubType Beds;
    public LightingSubType Lighting;
    public AppliancesSubType Appliances;
    public DecorSubType Decor;

    public StyleType Style;

    public ColorType colorType;
    public SingleColor singleColor;
    public MultiColor multiColor;
}