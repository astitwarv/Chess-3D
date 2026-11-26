using System.Collections.Generic;
using UnityEngine;

public static class MoveRules
{
    static readonly Vector2Int[] StraightDirections =
    {
        new Vector2Int( 1, 0), new Vector2Int(-1, 0),
        new Vector2Int( 0, 1), new Vector2Int( 0,-1)
    };

    static readonly Vector2Int[] DiagonalDirections =
    {
        new Vector2Int( 1, 1), new Vector2Int( 1,-1),
        new Vector2Int(-1, 1), new Vector2Int(-1,-1)
    };

    static readonly Vector2Int[] KnightJumps =
    {
        new Vector2Int( 1, 2), new Vector2Int( 2, 1),
        new Vector2Int( 2,-1), new Vector2Int( 1,-2),
        new Vector2Int(-1,-2), new Vector2Int(-2,-1),
        new Vector2Int(-2, 1), new Vector2Int(-1, 2)
    };

    static readonly Vector2Int[] KingSteps =
    {
        new Vector2Int( 1, 0), new Vector2Int(-1, 0),
        new Vector2Int( 0, 1), new Vector2Int( 0,-1),
        new Vector2Int( 1, 1), new Vector2Int( 1,-1),
        new Vector2Int(-1, 1), new Vector2Int(-1,-1)
    };

    // White moves toward negative Z, Black toward positive Z.
    public static int ForwardDirection(ChessPiece.PieceColor color)
    {
        return color == ChessPiece.PieceColor.White ? -1 : 1;
    }

    static int PawnStartRank(ChessPiece.PieceColor color)
    {
        return color == ChessPiece.PieceColor.White ? 6 : 1;
    }

    // All squares this piece can move to, ignoring king safety for now.
    public static List<Vector2Int> GetMoves(ChessPiece piece, PieceManager board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        switch (piece.pieceType)
        {
            case ChessPiece.PieceType.Pawn:
                AddPawnMoves(piece, board, moves);
                break;
            case ChessPiece.PieceType.Rook:
                AddSlidingMoves(piece, board, StraightDirections, moves);
                break;
            case ChessPiece.PieceType.Bishop:
                AddSlidingMoves(piece, board, DiagonalDirections, moves);
                break;
            case ChessPiece.PieceType.Queen:
                AddSlidingMoves(piece, board, StraightDirections, moves);
                AddSlidingMoves(piece, board, DiagonalDirections, moves);
                break;
            case ChessPiece.PieceType.Knight:
                AddStepMoves(piece, board, KnightJumps, moves);
                break;
            case ChessPiece.PieceType.King:
                AddStepMoves(piece, board, KingSteps, moves);
                break;
        }

        return moves;
    }

    static void AddPawnMoves(ChessPiece piece, PieceManager board, List<Vector2Int> moves)
    {
        int dir = ForwardDirection(piece.pieceColor);
        Vector2Int pos = piece.boardPosition;

        // One square forward (must be empty)
        Vector2Int oneStep = pos + new Vector2Int(0, dir);
        if (board.IsEmpty(oneStep))
        {
            moves.Add(oneStep);

            // Two squares forward from the starting rank (both squares must be empty)
            Vector2Int twoStep = pos + new Vector2Int(0, 2 * dir);
            if (pos.y == PawnStartRank(piece.pieceColor) && board.IsEmpty(twoStep))
                moves.Add(twoStep);
        }

        // Diagonal captures (only if an enemy is there)
        Vector2Int left = pos + new Vector2Int(-1, dir);
        Vector2Int right = pos + new Vector2Int(1, dir);

        if (board.IsEnemyAt(left, piece.pieceColor)) moves.Add(left);
        if (board.IsEnemyAt(right, piece.pieceColor)) moves.Add(right);
    }

    // Rook, Bishop, Queen: keep going in a direction until blocked.
    static void AddSlidingMoves(ChessPiece piece, PieceManager board,
                                Vector2Int[] directions, List<Vector2Int> moves)
    {
        foreach (Vector2Int dir in directions)
        {
            Vector2Int square = piece.boardPosition + dir;

            while (board.IsInsideBoard(square))
            {
                if (board.IsEmpty(square))
                {
                    moves.Add(square);
                }
                else
                {
                    if (board.IsEnemyAt(square, piece.pieceColor))
                        moves.Add(square);   // can capture, but not go further

                    break;                   // blocked either way
                }

                square += dir;
            }
        }
    }

    // Knight, King: fixed jumps, no path checking.
    static void AddStepMoves(ChessPiece piece, PieceManager board,
                             Vector2Int[] offsets, List<Vector2Int> moves)
    {
        foreach (Vector2Int offset in offsets)
        {
            Vector2Int square = piece.boardPosition + offset;

            if (board.IsInsideBoard(square) && !board.IsFriendlyAt(square, piece.pieceColor))
                moves.Add(square);
        }
    }
}