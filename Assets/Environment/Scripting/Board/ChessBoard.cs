using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    [Header("Board Configuration")]
    public Transform boardOrigin;
    public int boardSize = 8;
    public float squareSize = 1f;

    public Vector3 GetSquarePosition(int x, int z)
    {
        if (!IsInsideBoard(x, z))
        {
            Debug.LogError("Invalid board coordinates!");
            return Vector3.zero;
        }

        float worldX = boardOrigin.position.x + (x - 3.5f) * squareSize;
        float worldZ = boardOrigin.position.z + (z - 3.5f) * squareSize;
        float worldY = boardOrigin.position.y;

        return new Vector3(worldX, worldY, worldZ);
    }

    public Vector2Int GetBoardCoordinates(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - boardOrigin.position.x) / squareSize + 3.5f);
        int z = Mathf.RoundToInt((worldPosition.z - boardOrigin.position.z) / squareSize + 3.5f);

        return new Vector2Int(x, z);
    }

    public bool IsInsideBoard(int x, int z)
    {
        return x >= 0 && x < boardSize &&
               z >= 0 && z < boardSize;
    }

    // Chess notation for a square, e.g. (4, 7) -> "d1".
    // White sits at +Z, so from White's view the a-file is x = 7 and rank 1 is z = 7.
    public string SquareName(Vector2Int square)
    {
        char file = (char)('a' + (boardSize - 1 - square.x));
        int rank = boardSize - square.y;
        return file.ToString() + rank;
    }
}