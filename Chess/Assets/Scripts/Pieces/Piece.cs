using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Field), typeof(Image))]
public abstract class Piece : MonoBehaviour
{
    [SerializeField] protected Sprite icon;
    [SerializeField] protected Field  field;
    [SerializeField] protected Side   side;

    public Sprite Icon
    {
        get { return icon; }
    }

    public Field Field
    {
        get { return field; }
        set
        {
            if (field == value)
                return;

            if (field != null)
            {
                field.onTouch -= OnTouch;
                field.Piece = null;
            }

            field = value;

            if (field != null)
            {
                field.onTouch += OnTouch;
                field.Piece = this;
            }
        }
    }

    public Side Side 
    { 
        get { return side; } 
    }

    protected static Piece s_choosePiece;

    public static Piece ChoosePiece
    {
        get { return s_choosePiece; }
        private set
        {
            if (s_choosePiece != null)
                s_choosePiece?.Field?.ResetTargets();

            s_choosePiece = value;

            if (s_choosePiece != null)
                s_choosePiece?.Field?.SetChooseColor();
        }
    }

    protected void OnValidate()
    {
        Field field = GetComponent<Field>();
        field.Piece = this;
        GetComponent<Image>().sprite = icon;
    }

    protected void OnEnable()
    {
        field.onTouch += OnTouch;
    }

    private void OnDisable()
    {
        field.onTouch -= OnTouch;
    }

    protected void OnTouch()
    {
        if (Field.Board.WhoMove == Side)
        {
            if (ChoosePiece != this)
                ChoosePiece = this;
            else
                ChoosePiece = null;

            ChoosePiece?.ShowMoves();
        }
        else
        {
            if (Field.IsTarget)
            {
                Field.Piece = ChoosePiece;
                Destroy(this);
            }
        }
    }

    protected abstract void ShowMoves();
}
