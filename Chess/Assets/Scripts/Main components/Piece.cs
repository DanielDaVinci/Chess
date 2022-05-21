using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Field), typeof(Image))]
public abstract class Piece : MonoBehaviour
{
    [SerializeField] protected Sprite whiteIcon;
    [SerializeField] protected Sprite blackIcon;
    [SerializeField] protected Field  field;
    [SerializeField] protected Side   side;

    public Sprite WhiteIcon
    {
        get { return whiteIcon; }
    }

    public Sprite BlackIcon
    {
        get { return blackIcon; }
    }

    public Side Side
    {
        get { return side; }
    }

    public Field Field
    {
        get { return field; }
        set
        {
            if (field == value)
                return;

            if ((field != null))
            {
                OnDisable();
                field.Piece = null;
            }

            field = value;

            if (field != null)
            {
                OnEnable();
                field.Piece = this;
                SetPieceProperties();
            }
        }
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

    protected void OnValidate() => SetPieceProperties();

    public void SetPieceProperties()
    {
        Field field = GetComponent<Field>();
        field.Piece = this;

        if (side == Side.White)
            GetComponent<Image>().sprite = WhiteIcon;
        else
            GetComponent<Image>().sprite = BlackIcon;
    }

    public void Copy(Piece other)
    {
        whiteIcon = other.whiteIcon;
        blackIcon = other.blackIcon;
        side = other.side;
    }

    // Touch event

    protected void OnEnable()
    {
        if (field != null)
            field.onTouch += OnTouch;
    }

    private void OnDisable()
    {
        if (field != null)
            field.onTouch -= OnTouch;
    }

    private void OnDestroy()
    {
        if (field != null)
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
                DestroyImmediate(this);
            }
        }
    }

    // End

    protected abstract void ShowMoves();
}