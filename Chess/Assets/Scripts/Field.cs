using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class Field : MonoBehaviour
{
    public Board Board;
    [SerializeField] private Piece      piece;
    [SerializeField] private Vector2Int position;

    private bool isTarget = false;

    public Piece Piece
    {
        get { return piece; }
        set
        {
            if (piece == value)
                return;

            if (piece != null)
                piece.Field = null;

            piece = value;

            if (piece != null)
                piece.Field = this;
        }
    }

    public Vector2Int Position
    {
        get { return position; }
        set
        {
            if ((value.y >= 0 && value.y < Board?.Rows) && (value.x >= 0 && value.x < Board?.Columns))
                position = value;
        }
    }

    public bool IsTarget
    {
        get { return isTarget; }
        set
        {
            isTarget = value;

            if (isTarget)
            {
                if (Piece == null)
                    SetTargetColor();
                else
                    SetTargetEnemyColor();
            }
            else
            {
                SetNormalColor();
            }
        }
    }

    public void ResetTargets()
    {
        for (int i = 0; i < Board.Rows; i++)
            for (int j = 0; j < Board.Columns; j++)
                Board[i, j].IsTarget = false;
    }

    public void SetNormalColor()
    {
        GetComponent<SpriteRenderer>().color = Board.DefineColor(Position.x, Position.y);
    }

    public void SetChooseColor()
    {
        GetComponent<SpriteRenderer>().color = Board.ChooseColor;
    }

    public void SetTargetColor()
    {
        GetComponent<SpriteRenderer>().color = Board.TargetColor;
    }

    public void SetTargetEnemyColor()
    {
        GetComponent<SpriteRenderer>().color = Board.TargetEnemyColor;
    }

    public void ResetColors()
    {
        Board.SetSimpleColors();
    }

    public delegate void OnTouch();
    public event OnTouch onTouch;

    private void OnMouseUpAsButton()
    {
        onTouch?.Invoke();

        if ((IsTarget) && (Piece == null))
            Piece = Piece.ChoosePiece;
    }
}
