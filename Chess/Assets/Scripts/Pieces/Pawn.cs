using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece
{
    protected override void ShowMoves()
    {
        Board board = field.Board;
        Vector2Int pos = field.Position;

        int direction = Side == Side.White ? 1 : -1;

        if (board[pos.x, pos.y + direction].Piece == null)
            board[pos.x, pos.y + direction].IsTarget = true;

        if (board[pos.x + 1, pos.y + direction].Piece != null)
            board[pos.x + 1, pos.y + direction].IsTarget = true;
        if (board[pos.x - 1, pos.y + direction].Piece != null)
            board[pos.x - 1, pos.y + direction].IsTarget = true;
    }
}
