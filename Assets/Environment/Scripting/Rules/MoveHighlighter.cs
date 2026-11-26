using System.Collections.Generic;
using UnityEngine;

public class MoveHighlighter : MonoBehaviour
{
    public ChessBoard chessBoard;
    public PieceManager pieceManager;

    [Header("Marker Appearance")]
    public Color moveColor = new Color(0.2f, 0.9f, 0.3f);
    public Color captureColor = new Color(0.9f, 0.2f, 0.2f);
    public float markerSize = 0.4f;
    [Tooltip("Height above Board Origin. Raise if markers are hidden inside the board.")]
    public float markerHeight = 0.02f;

    private List<GameObject> activeMarkers = new List<GameObject>();

    void Awake()
    {
        if (chessBoard == null) chessBoard = GetComponent<ChessBoard>();
        if (pieceManager == null) pieceManager = GetComponent<PieceManager>();
    }

    public void ShowMoves(ChessPiece piece, List<Vector2Int> moves)
    {
        ClearMarkers();

        foreach (Vector2Int square in moves)
        {
            bool isEnPassant = piece.pieceType == ChessPiece.PieceType.Pawn
                               && square.x != piece.boardPosition.x
                               && pieceManager.IsEmpty(square);

            bool isCapture = pieceManager.IsEnemyAt(square, piece.pieceColor) || isEnPassant; CreateMarker(square, isCapture ? captureColor : moveColor);
        }
    }

    public void ClearMarkers()
    {
        foreach (GameObject marker in activeMarkers)
            Destroy(marker);

        activeMarkers.Clear();
    }

    void CreateMarker(Vector2Int square, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "MoveMarker " + square;

        // Remove collider so markers never block piece clicks
        Destroy(marker.GetComponent<Collider>());

        Vector3 position = chessBoard.GetSquarePosition(square.x, square.y);
        position.y += markerHeight;

        marker.transform.position = position;
        marker.transform.localScale = new Vector3(markerSize, 0.005f, markerSize);
        marker.transform.SetParent(transform, true);

        marker.GetComponent<Renderer>().material.color = color;

        activeMarkers.Add(marker);
    }
}