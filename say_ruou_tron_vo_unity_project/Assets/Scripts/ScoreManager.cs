using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Score")]
    public TMP_Text scoreText;

    public float scorePerSecond = 10f;

    private float score = 0f;
    private bool gameRunning = true;
    private float scoreMultiplier = 1f;
    private Coroutine multiplierCoroutine;

    [Header("Double Score UI")]
    public GameObject doubleScoreBar;
    public Image doubleScoreFill;

    //private float multiplierTime;
    //private float multiplierDuration;

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        ResetScore();

        // Bar beim Spielstart verstecken
        if (doubleScoreBar != null)
            doubleScoreBar.SetActive(false);
    }
    void Update()
    {
        if (!gameRunning)
            return;

        score += scorePerSecond * scoreMultiplier * Time.deltaTime;
        scoreText.text = Mathf.FloorToInt(score).ToString();
        
    }

    public int GetScore()
    {
        return Mathf.FloorToInt(score);
    }

    public void StopScore()
    {
        gameRunning = false;
    }

    public void ResetScore()
    {
        score = 0f;
        scoreMultiplier = 1f;
        gameRunning = true;
    }
    /*
    public void SetScoreMultiplier(float multiplier)
    {
        scoreMultiplier = multiplier;
    }

    public void ResetScoreMultiplier()
    {
        scoreMultiplier = 1f;
    }
    */
    public void ActivateDoubleScore(float duration)
    {
        if (multiplierCoroutine != null)
            StopCoroutine(multiplierCoroutine);

        multiplierCoroutine = StartCoroutine(DoubleScoreCoroutine(duration));
    }

    private IEnumerator DoubleScoreCoroutine(float duration)
    {
        scoreMultiplier = 2f;

        doubleScoreBar.SetActive(true);
        doubleScoreFill.fillAmount = 1f;

        float remainingTime = duration;

        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;

            doubleScoreFill.fillAmount =
                remainingTime / duration;

            yield return null;
        }

        scoreMultiplier = 1f;

        doubleScoreFill.fillAmount = 0f;
        doubleScoreBar.SetActive(false);

        multiplierCoroutine = null;
    }
}