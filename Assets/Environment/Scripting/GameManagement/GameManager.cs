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

    [Header("Debug")]
    public bool printBoardAfterMove = true;

    void Awake()
    {
        if (chessBoard == null) chessBoard = GetComponent<ChessBoard>();
        if (pieceManager == null) pieceManager = GetComponent<PieceManager>();
        if (cameraController == null) cameraController = GetComponent<ChessCameraController>();
    }

    void Start()
    {
        Debug.Log("Chess game has been initialized.");

        if (cameraController != null)
        {
            cameraController.CheckViews(whiteCameraPosition, blackCameraPosition);
            cameraController.MoveTo(CurrentPlayerView(), true);   // start in place, no animation
        }
        else
        {
            Debug.LogWarning("ChessGameManager: no ChessCameraController found; camera will not switch.");
        }
    }

    public void MakeMove(ChessPiece piece, Vector2Int target)
    {
        Vector2Int from = piece.boardPosition;

        ChessPiece captured = pieceManager.MovePieceData(piece, target);

        if (piece.boardPosition != target)
        {
            Debug.LogError("MakeMove failed: board data was not updated. Move cancelled.");
            return;
        }

        if (captured != null)
            captured.Capture();

        piece.PlaceOnSquare(chessBoard.GetSquarePosition(target.x, target.y));

        string message = piece.pieceColor + " " + piece.pieceType + " " + from + " -> " + target;
        if (captured != null)
            message += ", captured " + captured.pieceColor + " " + captured.pieceType;
        Debug.Log(message);

        if (printBoardAfterMove)
            pieceManager.PrintBoard();

        EndTurn();
    }

    public void EndTurn()
    {
        isWhiteTurn = !isWhiteTurn;
        Debug.Log((isWhiteTurn ? "White" : "Black") + " to move.");

        if (cameraController != null)
            cameraController.MoveTo(CurrentPlayerView(), false);
    }

    Transform CurrentPlayerView()
    {
        return isWhiteTurn ? whiteCameraPosition : blackCameraPosition;
    }
}