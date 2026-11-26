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

    // ───────────────────────── Basic helpers ─────────────────────────

    // White moves toward negative Z, Black toward positive Z.
    public static int ForwardDirection(ChessPiece.PieceColor color)
    {
        return color == ChessPiece.PieceColor.White ? -1 : 1;
    }

    static int PawnStartRank(ChessPiece.PieceColor color)
    {
        return color == ChessPiece.PieceColor.White ? 6 : 1;
    }

    public static ChessPiece.PieceColor Opponent(ChessPiece.PieceColor color)
    {
        return color == ChessPiece.PieceColor.White
            ? ChessPiece.PieceColor.Black
            : ChessPiece.PieceColor.White;
    }

    // ───────────────────────── Legal moves (use these for gameplay) ─────────────────────────

    public static List<Vector2Int> GetLegalMoves(ChessPiece piece, PieceManager board)
    {
        List<Vector2Int> legal = new List<Vector2Int>();

        foreach (Vector2Int target in GetMoves(piece, board))
        {
            if (!LeavesKingInCheck(piece, target, board))
                legal.Add(target);
        }

        return legal;
    }

    public static bool LeavesKingInCheck(ChessPiece piece, Vector2Int target, PieceManager board)
    {
        PieceManager.SimulatedMove sim = board.SimulateMove(piece, target);
        bool inCheck = IsInCheck(piece.pieceColor, board);
        board.UndoSimulatedMove(sim);
        return inCheck;
    }

    public static bool IsInCheck(ChessPiece.PieceColor color, PieceManager board)
    {
        ChessPiece king = board.FindKing(color);

        if (king == null)
        {
            Debug.LogError("MoveRules: no " + color + " King on the board.");
            return false;
        }

        return IsSquareAttacked(king.boardPosition, Opponent(color), board);
    }

    // Can any piece of 'byColor' attack this square right now?
    public static bool IsSquareAttacked(Vector2Int square, ChessPiece.PieceColor byColor, PieceManager board)
    {
        foreach (ChessPiece attacker in board.GetPieces(byColor))
        {
            if (attacker.pieceType == ChessPiece.PieceType.Pawn)
            {
                int dir = ForwardDirection(attacker.pieceColor);
                Vector2Int p = attacker.boardPosition;

                if (square == p + new Vector2Int(-1, dir) || square == p + new Vector2Int(1, dir))
                    return true;
            }
            // includeSpecial = false: castling and en passant are never attacks
            else if (GetMoves(attacker, board, false).Contains(square))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasAnyLegalMove(ChessPiece.PieceColor color, PieceManager board)
    {
        foreach (ChessPiece piece in board.GetPieces(color))
        {
            if (GetLegalMoves(piece, board).Count > 0)
                return true;
        }

        return false;
    }

    // ───────────────────────── Piece patterns (ignore king safety) ─────────────────────────

    // includeSpecial = true adds castling and en passant.
    public static List<Vector2Int> GetMoves(ChessPiece piece, PieceManager board, bool includeSpecial = true)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        switch (piece.pieceType)
        {
            case ChessPiece.PieceType.Pawn:
                AddPawnMoves(piece, board, moves, includeSpecial);
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
                if (includeSpecial)
                    AddCastlingMoves(piece, board, moves);
                break;
        }

        return moves;
    }

    static void AddPawnMoves(ChessPiece piece, PieceManager board,
                             List<Vector2Int> moves, bool includeSpecial)
    {
        int dir = ForwardDirection(piece.pieceColor);
        Vector2Int pos = piece.boardPosition;

        Vector2Int oneStep = pos + new Vector2Int(0, dir);
        if (board.IsEmpty(oneStep))
        {
            moves.Add(oneStep);

            Vector2Int twoStep = pos + new Vector2Int(0, 2 * dir);
            if (pos.y == PawnStartRank(piece.pieceColor) && board.IsEmpty(twoStep))
                moves.Add(twoStep);
        }

        Vector2Int left = pos + new Vector2Int(-1, dir);
        Vector2Int right = pos + new Vector2Int(1, dir);

        if (board.IsEnemyAt(left, piece.pieceColor)) moves.Add(left);
        if (board.IsEnemyAt(right, piece.pieceColor)) moves.Add(right);

        if (includeSpecial)
        {
            if (IsEnPassantCapture(piece, left, board)) moves.Add(left);
            if (IsEnPassantCapture(piece, right, board)) moves.Add(right);
        }
    }

    // True if this pawn can capture en passant onto 'target'.
    static bool IsEnPassantCapture(ChessPiece pawn, Vector2Int target, PieceManager board)
    {
        if (target != board.enPassantSquare)
            return false;

        // The pawn being captured sits beside us, on our rank, in the target's column.
        ChessPiece victim = board.GetPieceAt(new Vector2Int(target.x, pawn.boardPosition.y));

        return victim != null
            && victim.pieceType == ChessPiece.PieceType.Pawn
            && victim.pieceColor != pawn.pieceColor;
    }

    static void AddCastlingMoves(ChessPiece king, PieceManager board, List<Vector2Int> moves)
    {
        if (king.hasMoved)
            return;

        ChessPiece.PieceColor enemy = Opponent(king.pieceColor);
        Vector2Int pos = king.boardPosition;

        if (IsSquareAttacked(pos, enemy, board))
            return;   // can't castle out of check

        // Try both directions along the back rank
        int[] directions = { -1, 1 };

        foreach (int dir in directions)
        {
            Vector2Int step = new Vector2Int(dir, 0);

            // Walk toward the corner; every square before the rook must be empty
            Vector2Int square = pos + step;
            while (board.IsInsideBoard(square) && board.IsEmpty(square))
                square += step;

            ChessPiece rook = board.GetPieceAt(square);

            bool rookOk = rook != null
                          && rook.pieceType == ChessPiece.PieceType.Rook
                          && rook.pieceColor == king.pieceColor
                          && !rook.hasMoved;
            if (!rookOk)
                continue;

            // King may not pass through or land on an attacked square
            Vector2Int passSquare = pos + step;
            Vector2Int landSquare = pos + step * 2;

            if (IsSquareAttacked(passSquare, enemy, board) || IsSquareAttacked(landSquare, enemy, board))
                continue;

            moves.Add(landSquare);
        }
    }

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
                        moves.Add(square);
                    break;
                }

                square += dir;
            }
        }
    }

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