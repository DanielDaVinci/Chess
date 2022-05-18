using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoardSaver", menuName = "Create Board Saver", order = 1)]
public class DataSaver : ScriptableObject
{
    public int Rows;
    public int Columns;
    public List<GameObject> Fields = new List<GameObject>();

    public GameObject this[int i, int j]
    {
        get
        {
            if (Fields != null && (i >= 0 && i < Rows) && (j >= 0 && j < Columns))
                return Fields[i * Columns + j];
            else
                return null;
        }
        set
        {
            if (Fields != null && (i >= 0 && i < Rows) && (j >= 0 && j < Columns))
                Fields[i * Columns + j] = value;
        }
    }
}
