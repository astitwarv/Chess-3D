using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChessUI : MonoBehaviour
{
    [Header("References")]
    public ChessGameManager gameManager;
    public TMP_Text turnText;
    public TMP_Text messageText;
    public Button newGameButton;

    [Header("Promotion Panel")]
    public GameObject promotionPanel;
    public TMP_Text promotionTitle;
    public Button queenButton;
    public Button rookButton;
    public Button bishopButton;
    public Button knightButton;

    [Header("Messages")]
    public float messageDuration = 2.5f;
    public Color infoColor = Color.white;
    public Color errorColor = new Color(1f, 0.45f, 0.45f);

    [Header("Turn Text Colours")]
    public Color turnColor = Color.white;
    public Color checkColor = new Color(1f, 0.45f, 0.45f);
    public Color gameOverColor = new Color(1f, 0.85f, 0.3f);

    private float hideMessageAt = -1f;

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<ChessGameManager>();
    }

    void Start()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("ChessUI: Game Manager not found.");
            return;
        }

        if (newGameButton != null)
            newGameButton.onClick.AddListener(gameManager.NewGame);
        else
            Debug.LogWarning("ChessUI: New Game Button not assigned.");

        // Each button calls CompletePromotion with its own piece type
        ConnectPromotionButton(queenButton, ChessPiece.PieceType.Queen);
        ConnectPromotionButton(rookButton, ChessPiece.PieceType.Rook);
        ConnectPromotionButton(bishopButton, ChessPiece.PieceType.Bishop);
        ConnectPromotionButton(knightButton, ChessPiece.PieceType.Knight);

        HidePromotion();
        ClearMessage();
        ShowTurn(gameManager.isWhiteTurn);
    }

    void ConnectPromotionButton(Button button, ChessPiece.PieceType type)
    {
        if (button == null)
        {
            Debug.LogWarning("ChessUI: " + type + " promotion button not assigned.");
            return;
        }

        button.onClick.AddListener(() => gameManager.CompletePromotion(type));
    }

    void Update()
    {
        if (hideMessageAt > 0f && Time.time >= hideMessageAt)
            ClearMessage();
    }

    // ───────────────────────── Turn / game state ─────────────────────────

    public void ShowTurn(bool isWhiteTurn, bool inCheck = false)
    {
        if (turnText == null) return;

        turnText.text = (isWhiteTurn ? "White" : "Black") + " to move" + (inCheck ? " - CHECK!" : "");
        turnText.color = inCheck ? checkColor : turnColor;
    }

    public void ShowGameOver(string result)
    {
        if (turnText == null) return;

        turnText.text = result;
        turnText.color = gameOverColor;
    }

    // ───────────────────────── Promotion ─────────────────────────

    public void ShowPromotion(ChessPiece.PieceColor color)
    {
        if (promotionTitle != null)
            promotionTitle.text = color + " pawn promotes to:";

        if (promotionPanel != null)
            promotionPanel.SetActive(true);
    }

    public void HidePromotion()
    {
        if (promotionPanel != null)
            promotionPanel.SetActive(false);
    }

    // ───────────────────────── Messages ─────────────────────────

    public void ShowMessage(string text, bool isError = false)
    {
        if (messageText == null)
        {
            Debug.Log(text);
            return;
        }

        messageText.text = text;
        messageText.color = isError ? errorColor : infoColor;
        hideMessageAt = Time.time + messageDuration;
    }

    public void ClearMessage()
    {
        if (messageText != null)
            messageText.text = "";

        hideMessageAt = -1f;
    }
}