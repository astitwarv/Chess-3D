using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

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
            return Vector3.zero;            // returns 0, 0, 0
        }

        float worldX= boardOrigin.position.x + squareSize*(x-3.5f);
        float worldZ = boardOrigin.position.z + squareSize * (z - 3.5f);
        float worldY = boardOrigin.position.y;          // surface remains the same 

        return new Vector3(worldX, worldY, worldZ);     // return the unity chess coordinates 
    }

    // converting unity world position into board coordinates

    public Vector2Int GetBoardCoordinates(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(
            (worldPosition.x - boardOrigin.position.x) / squareSize + 3.5f);

        int z = Mathf.RoundToInt(
            (worldPosition.z - boardOrigin.position.z) / squareSize + 3.5f); 

        return new Vector2Int(x, z);
    }

    public bool IsInsideBoard(int x, int z)
    {
        return x >= 0 && x <boardSize && z >= 0 && z <boardSize;
    }
}
