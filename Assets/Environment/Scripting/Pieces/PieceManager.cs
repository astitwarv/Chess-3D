using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
    // "No square" marker, and the square a pawn can be captured on en passant this turn.
    public static readonly Vector2Int NoSquare = new Vector2Int(-1, -1);
    public Vector2Int enPassantSquare = NoSquare;
    private List<ChessPiece> promotedPieces =new List<ChessPiece>();

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
            piece.RecordStartState();             // remember for New Game

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

    int ValidateStartingLayout()
    {
        int problems = 0;
        int size = chessBoard.boardSize;

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

    // ───────────────────────── Queries ─────────────────────────

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

    // ───────────────────────── Real moves ─────────────────────────

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

    // Removes whatever piece is on a square (DATA only). Returns it, or null.
    public ChessPiece RemovePieceData(Vector2Int square)
    {
        ChessPiece piece = GetPieceAt(square);

        if (piece != null)
            board[square.x, square.y] = null;

        return piece;
    }

    // Replaces a pawn with a new piece of the chosen type. Returns the new piece.
    public ChessPiece PromotePawn(ChessPiece pawn, ChessPiece.PieceType newType)
    {
        ChessPiece template = FindTemplate(newType, pawn.pieceColor);

        if (template == null)
        {
            Debug.LogError("PromotePawn: no " + pawn.pieceColor + " " + newType + " model to copy.");
            return null;
        }

        Vector2Int square = pawn.boardPosition;

        // Remove the pawn (hidden, not destroyed, so New Game can restore it)
        board[square.x, square.y] = null;
        pawn.Capture();

        // Copy the template's 3D object into the same parent group
        GameObject copy = Instantiate(template.gameObject, pawn.transform.parent);
        copy.name = pawn.pieceColor + " " + newType + " (promoted)";
        copy.SetActive(true);   // template may be a captured (hidden) piece

        // Place it on the square, keeping the template's own height and facing
        Vector3 centre = chessBoard.GetSquarePosition(square.x, square.y);
        copy.transform.SetPositionAndRotation(
            new Vector3(centre.x, template.transform.position.y, centre.z),
            template.transform.rotation);

        ChessPiece promoted = copy.GetComponent<ChessPiece>();
        promoted.isSelected = false;
        promoted.hasMoved = true;
        promoted.boardPosition = square;

        board[square.x, square.y] = promoted;
        promotedPieces.Add(promoted);

        return promoted;
    }

    // Finds an original piece of this type and colour to copy (captured ones count too).
    ChessPiece FindTemplate(ChessPiece.PieceType type, ChessPiece.PieceColor color)
    {
        foreach (ChessPiece p in allPieces)
            if (p.pieceType == type && p.pieceColor == color)
                return p;

        return null;
    }

    // ───────────────────────── New Game ─────────────────────────

    // Puts every piece back to its starting square and rebuilds the board record.
    public void ResetBoard()
    {
        // Delete promoted copies (their original pawns are restored below)
        foreach (ChessPiece p in promotedPieces)
            if (p != null)
                Destroy(p.gameObject);
        promotedPieces.Clear();
        int size = chessBoard.boardSize;
        board = new ChessPiece[size, size];

        foreach (ChessPiece piece in allPieces)
        {
            if (!piece.HasStartState())
                continue;

            piece.ResetToStart();
            board[piece.boardPosition.x, piece.boardPosition.y] = piece;
        }
        enPassantSquare = NoSquare;
        Debug.Log("PieceManager: board reset.");
        PrintBoard();
    }

    // ───────────────────────── Simulation (king-safety checks) ─────────────────────────

    public struct SimulatedMove
    {
        public ChessPiece piece;
        public Vector2Int from;
        public Vector2Int to;
        public ChessPiece captured;
        public Vector2Int capturedSquare;   // same as 'to', except for en passant
    }

    // Applies a move to the DATA only, with no checks and no visuals. Always undo it afterwards.
    public SimulatedMove SimulateMove(ChessPiece piece, Vector2Int to)
    {
        SimulatedMove sim;
        sim.piece = piece;
        sim.from = piece.boardPosition;
        sim.to = to;
        sim.capturedSquare = to;

        // En passant: pawn moves diagonally onto an EMPTY square -> the captured pawn is beside it
        bool isEnPassant = piece.pieceType == ChessPiece.PieceType.Pawn
                           && to.x != sim.from.x
                           && board[to.x, to.y] == null;

        if (isEnPassant)
            sim.capturedSquare = new Vector2Int(to.x, sim.from.y);

        sim.captured = board[sim.capturedSquare.x, sim.capturedSquare.y];

        board[sim.capturedSquare.x, sim.capturedSquare.y] = null;
        board[sim.from.x, sim.from.y] = null;
        board[to.x, to.y] = piece;
        piece.boardPosition = to;

        return sim;
    }

    public void UndoSimulatedMove(SimulatedMove sim)
    {
        board[sim.to.x, sim.to.y] = null;
        board[sim.capturedSquare.x, sim.capturedSquare.y] = sim.captured;
        board[sim.from.x, sim.from.y] = sim.piece;
        sim.piece.boardPosition = sim.from;
    }

    // All pieces of one colour currently on the board (captured pieces excluded).
    public List<ChessPiece> GetPieces(ChessPiece.PieceColor color)
    {
        List<ChessPiece> result = new List<ChessPiece>();
        int size = chessBoard.boardSize;

        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
            {
                ChessPiece p = board[x, z];
                if (p != null && p.pieceColor == color)
                    result.Add(p);
            }

        return result;
    }

    public ChessPiece FindKing(ChessPiece.PieceColor color)
    {
        foreach (ChessPiece p in GetPieces(color))
            if (p.pieceType == ChessPiece.PieceType.King)
                return p;

        return null;
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