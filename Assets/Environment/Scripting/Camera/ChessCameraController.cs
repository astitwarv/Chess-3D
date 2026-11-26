using System.Collections;
using UnityEngine;

public class ChessCameraController : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public ChessBoard chessBoard;

    [Header("Transition")]
    public bool smoothTransition = true;
    public float transitionDuration = 1.2f;
    public float delayBeforeSwitch = 0.3f;

    public bool IsMoving { get; private set; }

    private Coroutine currentTransition;

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (chessBoard == null) chessBoard = GetComponent<ChessBoard>();
    }

    // Moves the camera to a view. instant = true jumps immediately.
    public void MoveTo(Transform view, bool instant)
    {
        if (targetCamera == null)
        {
            Debug.LogError("ChessCameraController: no camera found (tag your camera 'MainCamera').");
            return;
        }
        if (view == null)
        {
            Debug.LogError("ChessCameraController: camera view Transform is not assigned.");
            return;
        }

        if (currentTransition != null)
            StopCoroutine(currentTransition);

        if (instant || !smoothTransition)
        {
            Snap(view);
            IsMoving = false;
            currentTransition = null;
            return;
        }

        currentTransition = StartCoroutine(Transition(view));
    }

    void Snap(Transform view)
    {
        targetCamera.transform.SetPositionAndRotation(view.position, view.rotation);
    }

    // Circles the camera around the board centre instead of flying straight through it.
    IEnumerator Transition(Transform view)
    {
        IsMoving = true;

        if (delayBeforeSwitch > 0f)
            yield return new WaitForSeconds(delayBeforeSwitch);

        Transform cam = targetCamera.transform;
        Vector3 centre = chessBoard != null ? chessBoard.boardOrigin.position : Vector3.zero;

        Vector3 startOffset = cam.position - centre;
        Vector3 endOffset = view.position - centre;
        Quaternion startRot = cam.rotation;
        Quaternion endRot = view.rotation;

        // How far (degrees) to circle around the board
        float startAngle = Mathf.Atan2(startOffset.x, startOffset.z) * Mathf.Rad2Deg;
        float endAngle = Mathf.Atan2(endOffset.x, endOffset.z) * Mathf.Rad2Deg;
        float totalAngle = Mathf.DeltaAngle(startAngle, endAngle);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, transitionDuration);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));   // ease in and out

            // Turn the start view forward and the end view backward by the same amount,
            // then blend between them. Both stay on the circle around the board.
            Quaternion turnFromStart = Quaternion.Euler(0f, totalAngle * k, 0f);
            Quaternion turnFromEnd = Quaternion.Euler(0f, totalAngle * (k - 1f), 0f);

            cam.position = centre + Vector3.Lerp(turnFromStart * startOffset,
                                                 turnFromEnd * endOffset, k);
            cam.rotation = Quaternion.Slerp(turnFromStart * startRot,
                                            turnFromEnd * endRot, k);

            yield return null;   // wait one frame
        }

        Snap(view);
        IsMoving = false;
        currentTransition = null;
    }

    // Warns if a view is placed or pointed the wrong way.
    public void CheckViews(Transform whiteView, Transform blackView)
    {
        if (whiteView == null || blackView == null)
        {
            Debug.LogError("ChessCameraController: assign both camera positions on ChessGameManager.");
            return;
        }

        if (whiteView.position.z <= 0f || whiteView.forward.z >= 0f)
            Debug.LogWarning("White camera should be on the +Z side looking toward -Z.");

        if (blackView.position.z >= 0f || blackView.forward.z <= 0f)
            Debug.LogWarning("Black camera should be on the -Z side looking toward +Z.");
    }
}