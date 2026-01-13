using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Levels", menuName = "Levels/Level")]
public class LevelSO : ScriptableObject
{
    public int level;
    public string LevelName = "";

    //public CustomerSO customer;
}
