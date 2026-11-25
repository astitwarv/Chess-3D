using UnityEngine;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    [Header("Camera Positions")]

    public Transform blackCameraPosition;
    public Transform whiteCameraPosition;

    [Header ("Chess Settings")]

    public float squareSize = 1.0f;
    public bool isWhiteTurn = true;

    void Start()
    {
        Debug.Log("Chess game has been initialized."); 
    }

}