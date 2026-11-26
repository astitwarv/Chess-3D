using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PiecePivotFixer
{
    [MenuItem("Chess/Fix Piece Pivots")]
    static void FixPivots()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Exit Play Mode before fixing pivots.");
            return;
        }

        ChessBoard board = Object.FindAnyObjectByType<ChessBoard>();
        if (board == null || board.boardOrigin == null)
        {
            Debug.LogError("ChessBoard or its Board Origin not found.");
            return;
        }

        ChessPiece[] pieces = Object.FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
        int changed = 0;

        foreach (ChessPiece piece in pieces)
        {
            Transform root = piece.transform;
            Renderer[] renderers = piece.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                Debug.LogWarning(piece.name + ": no mesh found, skipped.", piece);
                continue;
            }

            // Box around everything visible in this piece
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // Which square is the VISIBLE model standing on?
            Vector2Int square = board.GetBoardCoordinates(bounds.center);
            if (!board.IsInsideBoard(square.x, square.y))
            {
                Debug.LogError(piece.name + ": visible model is off the board at " + square, piece);
                continue;
            }

            Vector3 target = board.GetSquarePosition(square.x, square.y);
            Vector3 delta = new Vector3(target.x - root.position.x, 0f,
                                        target.z - root.position.z);

            if (delta.magnitude < 0.001f)
                continue; // pivot already correct

            if (piece.GetComponent<Renderer>() != null)
            {
                Debug.LogError(piece.name + ": mesh is on the piece object itself; " +
                               "pivot can't be moved automatically.", piece);
                continue;
            }

            Undo.RecordObject(root, "Fix Piece Pivots");
            foreach (Transform child in root)
                Undo.RecordObject(child, "Fix Piece Pivots");

            // Move the pivot onto the square, move the model back so it stays put
            root.position += delta;
            foreach (Transform child in root)
                child.position -= delta;

            Debug.Log(piece.name + ": pivot moved " + delta + " onto square " + square, piece);
            changed++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Pivot fix done: " + changed + " piece(s) changed. Save the scene (Ctrl+S).");
    }
}