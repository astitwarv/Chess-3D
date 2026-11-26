using UnityEngine;

public class ChessGameManager : MonoBehaviour
{
    [Header("Camera Positions")]
    public Transform blackCameraPosition;
    public Transform whiteCameraPosition;

    [Header("Chess Settings")]
    public float squareSize = 1f;   // not used; ChessBoard's Square Size is the real one
    public bool isWhiteTurn = true;

    [Header("References")]
    public ChessBoard chessBoard;
    public PieceManager pieceManager;
    public ChessCameraController cameraController;
    public PieceSelection pieceSelection;
    public ChessUI ui;

    [Header("Debug")]
    public bool printBoardAfterMove = true;

    public bool IsGameOver { get; private set; }

    private string lastMoveText = "";

    void Awake()
    {
        if (chessBoard == null) chessBoard = GetComponent<ChessBoard>();
        if (pieceManager == null) pieceManager = GetComponent<PieceManager>();
        if (cameraController == null) cameraController = GetComponent<ChessCameraController>();
        if (pieceSelection == null) pieceSelection = GetComponent<PieceSelection>();
        if (ui == null) ui = FindAnyObjectByType<ChessUI>();
    }

    void Start()
    {
        Debug.Log("Chess game has been initialized.");

        if (ui == null)
            Debug.LogWarning("ChessGameManager: no ChessUI found; messages will go to the Console.");

        if (cameraController != null)
        {
            cameraController.CheckViews(whiteCameraPosition, blackCameraPosition);
            cameraController.MoveTo(CurrentPlayerView(), true);
        }

        if (ui != null)
            ui.ShowTurn(isWhiteTurn);
    }

    public void MakeMove(ChessPiece piece, Vector2Int target)
    {
        if (IsGameOver) return;

        Vector2Int from = piece.boardPosition;
        int dx = target.x - from.x;
        int dz = target.y - from.y;

        // Recognise special moves by their shape (BEFORE the board changes)
        bool isPawn = piece.pieceType == ChessPiece.PieceType.Pawn;
        bool isCastling = piece.pieceType == ChessPiece.PieceType.King && Mathf.Abs(dx) == 2;
        bool isEnPassant = isPawn && dx != 0 && pieceManager.IsEmpty(target);

        // 1. Move the piece in the data
        ChessPiece captured = pieceManager.MovePieceData(piece, target);

        if (piece.boardPosition != target)
        {
            Debug.LogError("MakeMove failed: board data was not updated. Move cancelled.");
            return;
        }

        // 2. En passant: the captured pawn is beside the start square, not on the target
        if (isEnPassant)
            captured = pieceManager.RemovePieceData(new Vector2Int(target.x, from.y));

        piece.hasMoved = true;

        // 3. Visuals
        if (captured != null)
            captured.Capture();

        piece.PlaceOnSquare(chessBoard.GetSquarePosition(target.x, target.y));

        // 4. Castling: also move the rook
        string castleSide = "";
        if (isCastling)
            castleSide = MoveCastlingRook(from, dx);

        // 5. Remember the en passant square for the NEXT move only
        if (isPawn && Mathf.Abs(dz) == 2)
            pieceManager.enPassantSquare = new Vector2Int(from.x, from.y + dz / 2);
        else
            pieceManager.enPassantSquare = PieceManager.NoSquare;

        // 6. Message
        if (isCastling)
        {
            lastMoveText = piece.pieceColor + " castles " + castleSide;
        }
        else
        {
            lastMoveText = piece.pieceColor + " " + piece.pieceType + " " +
                           chessBoard.SquareName(from) + " → " + chessBoard.SquareName(target);
            if (captured != null)
                lastMoveText += ", takes " + captured.pieceType + (isEnPassant ? " en passant" : "");
        }

        Debug.Log(lastMoveText + "   [" + from + " -> " + target + "]");

        if (printBoardAfterMove)
            pieceManager.PrintBoard();

        EndTurn();
    }

    // Moves the rook next to the king. Returns "kingside" or "queenside".
    string MoveCastlingRook(Vector2Int kingFrom, int dx)
    {
        int dir = dx > 0 ? 1 : -1;

        // An unmoved rook is always in the corner on the king's side of travel
        int cornerX = dir > 0 ? chessBoard.boardSize - 1 : 0;
        Vector2Int rookFrom = new Vector2Int(cornerX, kingFrom.y);
        Vector2Int rookTo = kingFrom + new Vector2Int(dir, 0);

        ChessPiece rook = pieceManager.GetPieceAt(rookFrom);

        if (rook == null || rook.pieceType != ChessPiece.PieceType.Rook)
        {
            Debug.LogError("Castling: no rook found on " + rookFrom);
            return "";
        }

        pieceManager.MovePieceData(rook, rookTo);
        rook.hasMoved = true;
        rook.PlaceOnSquare(chessBoard.GetSquarePosition(rookTo.x, rookTo.y));

        // h-file is x = 0 in this project's layout
        return cornerX == 0 ? "kingside" : "queenside";
    }

    public void EndTurn()
    {
        isWhiteTurn = !isWhiteTurn;

        ChessPiece.PieceColor current = CurrentColor();
        bool inCheck = MoveRules.IsInCheck(current, pieceManager);
        bool hasMoves = MoveRules.HasAnyLegalMove(current, pieceManager);

        if (!hasMoves)
        {
            IsGameOver = true;

            string result = inCheck
                ? "Checkmate! " + (isWhiteTurn ? "Black" : "White") + " wins"
                : "Stalemate! It's a draw";

            if (ui != null) ui.ShowGameOver(result);
            ShowMessage(lastMoveText + ". Click New Game to play again.", false);
            Debug.Log(result);
            return;
        }

        if (ui != null) ui.ShowTurn(isWhiteTurn, inCheck);

        if (inCheck)
            ShowMessage(lastMoveText + " - Check!", true);
        else
            ShowMessage(lastMoveText, false);

        Debug.Log(current + " to move" + (inCheck ? " (in check)" : "") + ".");

        if (cameraController != null)
            cameraController.MoveTo(CurrentPlayerView(), false);
    }

    public void NewGame()
    {
        bool cameraAlreadyAtWhite = isWhiteTurn;

        if (pieceSelection != null)
            pieceSelection.ClearSelection();

        pieceManager.ResetBoard();   // also clears en passant; pieces reset hasMoved
        isWhiteTurn = true;
        IsGameOver = false;
        lastMoveText = "";

        if (cameraController != null)
            cameraController.MoveTo(whiteCameraPosition, cameraAlreadyAtWhite);

        if (ui != null)
            ui.ShowTurn(isWhiteTurn);

        ShowMessage("New game started", false);
        Debug.Log("New game started.");
    }

    public void ShowMessage(string text, bool isError)
    {
        if (ui != null)
            ui.ShowMessage(text, isError);
        else
            Debug.Log(text);
    }

    ChessPiece.PieceColor CurrentColor()
    {
        return isWhiteTurn ? ChessPiece.PieceColor.White : ChessPiece.PieceColor.Black;
    }

    Transform CurrentPlayerView()
    {
        return isWhiteTurn ? whiteCameraPosition : blackCameraPosition;
    }
}