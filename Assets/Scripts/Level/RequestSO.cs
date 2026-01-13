using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Request", menuName = "Levels/Request")]
public class RequestSO : ScriptableObject
{
    public string requestName;
    public Type furnitureType;

    public StyleType furnitureStyle;
    public ColorType colorType;
    public Color color;
}
