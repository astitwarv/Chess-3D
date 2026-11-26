using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    public enum PieceType { Pawn, Rook, Knight, Bishop, Queen, King }
    public enum PieceColor { White, Black }

    [Header("Piece Information")]
    public PieceType pieceType;
    public PieceColor pieceColor;

    [Header("Board Position")]
    public Vector2Int boardPosition;

    [Header("Selection")]
    public float selectedLiftHeight = 0.3f;
    public bool isSelected = false;

    private Vector3 restPosition;   // where the piece sits when not selected

    void Awake()
    {
        FitClickCollider();
    }

    void Start()
    {
        Debug.Log(pieceColor + " " + pieceType + " initialized at " + boardPosition);
    }

    public void Select()
    {
        if (isSelected) return;

        isSelected = true;
        restPosition = transform.position;
        transform.position = restPosition + Vector3.up * selectedLiftHeight;
    }

    public void Deselect()
    {
        if (!isSelected) return;

        isSelected = false;
        transform.position = restPosition;
    }

    // Moves the 3D object onto a square, keeping this piece's own height.
    public void PlaceOnSquare(Vector3 squareCentre)
    {
        Deselect();   // drop back to rest height first

        Vector3 p = transform.position;
        transform.position = new Vector3(squareCentre.x, p.y, squareCentre.z);
    }

    // Removes the piece from play (hidden, not destroyed, so New Game can restore it later).
    public void Capture()
    {
        Deselect();
        gameObject.SetActive(false);
    }

    // Builds one BoxCollider on this object that wraps every visible mesh under it.
    void FitClickCollider()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogError(name + ": no mesh found under this object. " +
                           "ChessPiece is probably on the wrong GameObject.");
            return;
        }

        Bounds localBounds = new Bounds();
        bool first = true;

        foreach (Renderer r in renderers)
        {
            Vector3 min = r.bounds.min;
            Vector3 max = r.bounds.max;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);

                Vector3 local = transform.InverseTransformPoint(corner);

                if (first)
                {
                    localBounds = new Bounds(local, Vector3.zero);
                    first = false;
                }
                else
                {
                    localBounds.Encapsulate(local);
                }
            }
        }

        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();

        box.center = localBounds.center;
        box.size = localBounds.size;
    }
}