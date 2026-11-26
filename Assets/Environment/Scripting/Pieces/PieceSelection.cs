using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PieceSelection : MonoBehaviour
{
    public ChessGameManager gameManager;
    public ChessBoard chessBoard;
    public PieceManager pieceManager;
    public MoveHighlighter moveHighlighter;
    public ChessCameraController cameraController;

    private ChessPiece selectedPiece;
    private List<Vector2Int> currentMoves = new List<Vector2Int>();
    private Camera mainCamera;

    void Awake()
    {
        if (gameManager == null) gameManager = GetComponent<ChessGameManager>();
        if (chessBoard == null) chessBoard = GetComponent<ChessBoard>();
        if (pieceManager == null) pieceManager = GetComponent<PieceManager>();
        if (moveHighlighter == null) moveHighlighter = GetComponent<MoveHighlighter>();
        if (cameraController == null) cameraController = GetComponent<ChessCameraController>();

        mainCamera = Camera.main;
    }

    void Update()
    {
        if (cameraController != null && cameraController.IsMoving)
            return;   // ignore clicks while the camera is turning

        if (gameManager != null && gameManager.IsGameOver)
            return;   // no moves after checkmate/stalemate (New Game button still works)

        if (!Input.GetMouseButtonDown(0))
            return;

        // Ignore clicks on UI (e.g. the New Game button)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        HandleClick();
    }

    void HandleClick()
    {
        if (gameManager == null) { Debug.LogError("PieceSelection: ChessGameManager component missing."); return; }
        if (chessBoard == null) { Debug.LogError("PieceSelection: ChessBoard component missing."); return; }
        if (pieceManager == null) { Debug.LogError("PieceSelection: PieceManager component missing."); return; }
        if (moveHighlighter == null) { Debug.LogError("PieceSelection: MoveHighlighter component missing."); return; }
        if (mainCamera == null) { Debug.LogError("PieceSelection: No camera tagged 'MainCamera'."); return; }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        ChessPiece clickedPiece = null;
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
            clickedPiece = hit.collider.GetComponentInParent<ChessPiece>();

        // CASE 1: Own piece -> select / switch / toggle off
        if (clickedPiece != null && IsCurrentPlayersPiece(clickedPiece))
        {
            HandleOwnPieceClick(clickedPiece);
            return;
        }

        // CASE 2: Nothing selected
        if (selectedPiece == null)
        {
            if (clickedPiece != null)
                gameManager.ShowMessage("It's " + CurrentPlayerName() + "'s turn.", true);
            return;
        }

        // CASE 3: Work out the target square
        Vector2Int target;

        if (clickedPiece != null)
        {
            target = clickedPiece.boardPosition;           // clicked an enemy piece
        }
        else if (!TryGetClickedSquare(ray, out target))
        {
            ClearSelection();                              // clicked outside the board
            return;
        }

        // CASE 4: Valid or invalid move
        if (currentMoves.Contains(target))
        {
            ChessPiece pieceToMove = selectedPiece;
            ClearSelection();
            gameManager.MakeMove(pieceToMove, target);
        }
        else
        {
            // Explain WHY: wrong pattern, or it would expose the king?
            bool fitsPattern = MoveRules.GetMoves(selectedPiece, pieceManager).Contains(target);

            if (fitsPattern)
                gameManager.ShowMessage("That move would leave your King in check.", true);
            else
                gameManager.ShowMessage(selectedPiece.pieceType + " can't move to " +
                                        chessBoard.SquareName(target), true);
        }
    }

    void HandleOwnPieceClick(ChessPiece piece)
    {
        if (piece == selectedPiece)
        {
            ClearSelection();
            return;
        }

        ClearSelection();
        selectedPiece = piece;
        selectedPiece.Select();

        currentMoves = MoveRules.GetLegalMoves(selectedPiece, pieceManager);
        moveHighlighter.ShowMoves(selectedPiece, currentMoves);

        if (currentMoves.Count == 0)
            gameManager.ShowMessage(piece.pieceType + " on " +
                                    chessBoard.SquareName(piece.boardPosition) + " has no moves.", true);

        Debug.Log("Selected: " + piece.pieceColor + " " + piece.pieceType +
                  " at " + piece.boardPosition + " | moves: " + MovesToString(currentMoves));
    }

    // Finds which square the mouse ray hits on the board's surface plane.
    bool TryGetClickedSquare(Ray ray, out Vector2Int square)
    {
        square = new Vector2Int(-1, -1);

        Plane boardPlane = new Plane(Vector3.up, chessBoard.boardOrigin.position);
        float distance;

        if (!boardPlane.Raycast(ray, out distance))
            return false;

        Vector3 point = ray.GetPoint(distance);
        square = chessBoard.GetBoardCoordinates(point);

        return chessBoard.IsInsideBoard(square.x, square.y);
    }

    bool IsCurrentPlayersPiece(ChessPiece piece)
    {
        if (gameManager.isWhiteTurn)
            return piece.pieceColor == ChessPiece.PieceColor.White;
        else
            return piece.pieceColor == ChessPiece.PieceColor.Black;
    }

    string CurrentPlayerName()
    {
        return gameManager.isWhiteTurn ? "White" : "Black";
    }

    public void ClearSelection()
    {
        if (selectedPiece != null)
        {
            selectedPiece.Deselect();
            selectedPiece = null;
        }

        currentMoves.Clear();

        if (moveHighlighter != null)
            moveHighlighter.ClearMarkers();
    }

    public ChessPiece GetSelectedPiece()
    {
        return selectedPiece;
    }

    string MovesToString(List<Vector2Int> moves)
    {
        if (moves.Count == 0) return "none";
        return string.Join(", ", moves);
    }
}