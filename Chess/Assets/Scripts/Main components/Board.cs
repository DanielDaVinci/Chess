using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private GameObject fieldPrefab;
    [SerializeField] private Color      firstColor;
    [SerializeField] private Color      secondColor;
    [SerializeField] private Color      chooseColor;
    [SerializeField] private Color      targetColor;
    [SerializeField] private Color      targetEnemyColor;
    [SerializeField] private Vector2Int size;
    [Space(10)]
    [SerializeField] private DataSaver  data;

    public Side WhoMove { get; private set; } = Side.White;

    public Color FirstColor { get { return firstColor; } }
    public Color SecondColor { get { return secondColor; } }
    public Color ChooseColor { get { return chooseColor; } }
    public Color TargetColor { get { return targetColor; } }
    public Color TargetEnemyColor { get { return targetEnemyColor; } }

    public int Rows 
    { 
        get { return size.y; }
        private set { size.y = value; }
    }
    public int Columns 
    { 
        get { return size.x; }
        private set { size.x = value; }
    }
   
    public Field this[int i, int j]
    {
        get { return data[i, j].GetComponent<Field>(); }
    }

    private void OnValidate()
    {
        SetSimpleColors();
    }
    public void NextTurn()
    {
        WhoMove = (WhoMove == Side.White) ? Side.Black : Side.White;
    }

    public void CreateFields(int rows, int columns)
    {
        Rows = data.Rows = rows;
        Columns = data.Columns = columns;

        if (data.Fields.Count != 0)
            DestroyFields();
        
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Columns; j++)
            {
                data.Fields.Add(Instantiate(fieldPrefab, transform));

                Field field = data[i, j].GetComponent<Field>() ?? data[i, j].AddComponent<Field>();
                field.Board = this;
                field.Position = new Vector2Int(i, j);

                data[i, j].transform.localPosition = DefinePosition(i, j);
            }
        }

        SetSimpleColors();
    }

    public void DestroyFields()
    {
        foreach (GameObject field in data.Fields)
            if (field != null)
                DestroyImmediate(field?.gameObject);

        data.Fields.Clear();
    }

    public void SetSimpleColors()
    {
        if (data.Fields.Count == 0)
            return;

        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Columns; j++)
            {
                SpriteRenderer sprite = data[i, j].GetComponent<SpriteRenderer>();
                sprite.color = DefineColor(i, j);
            }
        }
    }

    public Color DefineColor(int row, int column)
    {
        if ((row * Columns + column + row) % 2 == 0)
            return firstColor;
        else
            return secondColor;
    }

    public Vector2 DefinePosition(int row, int column)
    {
        RectTransform rectTransform = data[row, column].gameObject.GetComponent<RectTransform>()
                                        ?? data[row, column].gameObject.AddComponent<RectTransform>();
        float sizeX = rectTransform.rect.width * rectTransform.localScale.x;
        float sizeY = rectTransform.rect.height * rectTransform.localScale.y;

        Vector2 position = new Vector2();
        position.x = sizeX * (row - (float)Rows / 2) + (Rows % 2 == 0 ? sizeX / 2 : 0);
        position.y = sizeY * (column - (float)Columns / 2) + (Columns % 2 == 0 ? sizeY / 2 : 0);

        return position;
    }
}