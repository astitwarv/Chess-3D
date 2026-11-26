using UnityEngine;
using System.Text;

public class PieceManager : MonoBehaviour
{
    [Header("References")]
    public ChessBoard chessBoard;
    public Transform piecesRoot;

    [Header("Validation")]
    [Tooltip("Max distance (units) a piece may sit from its square centre.")]
    public float positionTolerance = 0.1f;

    [Header("Debug")]
    public bool printBoardOnStart = true;

    private ChessPiece[,] board;
    private ChessPiece[] allPieces;

    void Awake()
    {
        InitializePieces();
    }

    void Start()
    {
        if (printBoardOnStart)
            PrintBoard();
    }

    // ───────────────────────── Initialization ─────────────────────────

    void InitializePieces()
    {
        if (chessBoard == null)
            chessBoard = GetComponent<ChessBoard>();

        if (chessBoard == null || chessBoard.boardOrigin == null)
        {
            Debug.LogError("PieceManager: ChessBoard or its Board Origin is not assigned.");
            return;
        }

        if (piecesRoot == null)
        {
            Debug.LogError("PieceManager: Pieces Root is not assigned. Drag the 'Pieces' object in.");
            return;
        }

        int size = chessBoard.boardSize;
        board = new ChessPiece[size, size];
        allPieces = piecesRoot.GetComponentsInChildren<ChessPiece>();
        int problems = 0;

        if (allPieces.Length != 32)
        {
            Debug.LogWarning("PieceManager: found " + allPieces.Length + " pieces, expected 32.");
            problems++;
        }

        foreach (ChessPiece piece in allPieces)
        {
            Vector2Int coords = chessBoard.GetBoardCoordinates(piece.transform.position);
            piece.boardPosition = coords;

            if (!chessBoard.IsInsideBoard(coords.x, coords.y))
            {
                Debug.LogError(piece.name + " is outside the board at " + coords +
                               " (world " + piece.transform.position + ")");
                problems++;
                continue;
            }

            Vector3 centre = chessBoard.GetSquarePosition(coords.x, coords.y);
            Vector2 offset = new Vector2(piece.transform.position.x - centre.x,
                                         piece.transform.position.z - centre.z);
            if (offset.magnitude > positionTolerance)
            {
                Debug.LogWarning(piece.name + " is " + offset.magnitude.ToString("F3") +
                                 " units off its square centre.");
                problems++;
            }

            if (board[coords.x, coords.y] != null)
            {
                Debug.LogError(piece.name + " and " + board[coords.x, coords.y].name +
                               " both map to square " + coords);
                problems++;
                continue;
            }

            board[coords.x, coords.y] = piece;   // register piece on its square

            bool wrongSide =
                (piece.pieceColor == ChessPiece.PieceColor.White && coords.y < 6) ||
                (piece.pieceColor == ChessPiece.PieceColor.Black && coords.y > 1);
            if (wrongSide)
            {
                Debug.LogWarning(piece.name + " (" + piece.pieceColor + ") starts on rank z=" + coords.y);
                problems++;
            }
        }

        problems += ValidateStartingLayout();

        Debug.Log("PieceManager: initialized " + allPieces.Length +
                  " pieces, " + problems + " problem(s).");
    }

    // Checks the overall starting setup, not just individual pieces.
    int ValidateStartingLayout()
    {
        int problems = 0;
        int size = chessBoard.boardSize;

        // Ranks 0, 1, 6, 7 must be full; ranks 2-5 must be empty.
        for (int z = 0; z < size; z++)
        {
            int count = 0;
            for (int x = 0; x < size; x++)
                if (board[x, z] != null) count++;

            bool shouldBeFull = (z <= 1 || z >= 6);
            int expected = shouldBeFull ? size : 0;

            if (count != expected)
            {
                Debug.LogWarning("Rank z=" + z + " has " + count + " pieces, expected " + expected);
                problems++;
            }
        }

        // Kings must face each other, and queens must face each other.
        problems += CheckFacing(ChessPiece.PieceType.King);
        problems += CheckFacing(ChessPiece.PieceType.Queen);

        return problems;
    }

    int CheckFacing(ChessPiece.PieceType type)
    {
        int whiteX = -1, blackX = -1;

        for (int x = 0; x < chessBoard.boardSize; x++)
        {
            ChessPiece w = board[x, 7];
            ChessPiece b = board[x, 0];
            if (w != null && w.pieceType == type) whiteX = x;
            if (b != null && b.pieceType == type) blackX = x;
        }

        if (whiteX != blackX)
        {
            Debug.LogWarning("White " + type + " is on x=" + whiteX + " but Black " + type +
                             " is on x=" + blackX + ". They should be on the same column.");
            return 1;
        }
        return 0;
    }

    // ───────────────────────── Queries (used by move rules) ─────────────────────────

    public bool IsInsideBoard(Vector2Int square)
    {
        return chessBoard.IsInsideBoard(square.x, square.y);
    }

    public ChessPiece GetPieceAt(Vector2Int square)
    {
        if (!IsInsideBoard(square))
            return null;

        return board[square.x, square.y];
    }

    public bool IsEmpty(Vector2Int square)
    {
        return IsInsideBoard(square) && board[square.x, square.y] == null;
    }

    public bool IsEnemyAt(Vector2Int square, ChessPiece.PieceColor myColor)
    {
        ChessPiece other = GetPieceAt(square);
        return other != null && other.pieceColor != myColor;
    }

    public bool IsFriendlyAt(Vector2Int square, ChessPiece.PieceColor myColor)
    {
        ChessPiece other = GetPieceAt(square);
        return other != null && other.pieceColor == myColor;
    }

    // ───────────────────────── Updates (used when a move happens) ─────────────────────────

    // Updates DATA only (no visuals). Returns the captured piece, or null.
    public ChessPiece MovePieceData(ChessPiece piece, Vector2Int target)
    {
        Vector2Int from = piece.boardPosition;

        if (GetPieceAt(from) != piece)
        {
            Debug.LogError("Board data out of sync: " + piece.name + " is not on " + from);
            return null;
        }

        if (!IsInsideBoard(target))
        {
            Debug.LogError("MovePieceData: target " + target + " is outside the board.");
            return null;
        }

        ChessPiece captured = board[target.x, target.y];

        if (captured != null && captured.pieceColor == piece.pieceColor)
        {
            Debug.LogError("MovePieceData: cannot capture own piece " + captured.name);
            return null;
        }

        board[from.x, from.y] = null;
        board[target.x, target.y] = piece;
        piece.boardPosition = target;

        return captured;
    }

    // ───────────────────────── Debug ─────────────────────────

    // Uppercase = White, lowercase = Black, '.' = empty
    public void PrintBoard()
    {
        int size = chessBoard.boardSize;
        StringBuilder sb = new StringBuilder("Board (top row = z 7, left column = x 0)\n");

        for (int z = size - 1; z >= 0; z--)
        {
            sb.Append(z).Append(" | ");
            for (int x = 0; x < size; x++)
                sb.Append(GetSymbol(board[x, z])).Append(' ');
            sb.Append('\n');
        }
        sb.Append("    0 1 2 3 4 5 6 7");

        Debug.Log(sb.ToString());
    }

    char GetSymbol(ChessPiece piece)
    {
        if (piece == null) return '.';

        char c;
        switch (piece.pieceType)
        {
            case ChessPiece.PieceType.Pawn: c = 'p'; break;
            case ChessPiece.PieceType.Rook: c = 'r'; break;
            case ChessPiece.PieceType.Knight: c = 'n'; break;
            case ChessPiece.PieceType.Bishop: c = 'b'; break;
            case ChessPiece.PieceType.Queen: c = 'q'; break;
            default: c = 'k'; break;
        }

        return piece.pieceColor == ChessPiece.PieceColor.White ? char.ToUpper(c) : c;
    }
}