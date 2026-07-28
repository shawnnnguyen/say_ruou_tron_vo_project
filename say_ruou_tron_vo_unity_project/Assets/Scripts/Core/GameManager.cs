using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private PlayerMovement player;
    [SerializeField] private CameraBlurEffect cameraBlurEffect;

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private TMP_Text finalScoreText;

    public bool IsGameOver { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void TriggerGameOver(string reason)
    {
        if (IsGameOver) return;
        IsGameOver = true;

        ScoreManager.Instance.StopScore();
        player.Freeze();
        if (cameraBlurEffect != null) cameraBlurEffect.PlayBlurHeld();

        if (reasonText != null) reasonText.text = reason;
        if (finalScoreText != null) finalScoreText.text = "Score: " + ScoreManager.Instance.GetScore();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
