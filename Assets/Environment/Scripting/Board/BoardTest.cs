using UnityEngine;

public class BoardTest : MonoBehaviour
{
    public ChessBoard chessBoard;

    void Start()
    {
        Vector3 position1 =
            chessBoard.GetSquarePosition(0, 0);

        Debug.Log("Square (0,0): " + position1);


        Vector3 position2 =
            chessBoard.GetSquarePosition(7, 7);

        Debug.Log("Square (7,7): " + position2);


        Vector2Int coordinates =
            chessBoard.GetBoardCoordinates(position1);

        Debug.Log("Converted Coordinates: " + coordinates);
    }
}