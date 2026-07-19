using TMPro;
using UnityEngine;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public TMP_Text scoreText;

    public float scorePerSecond = 10f;

    private float score = 0f;
    private bool gameRunning = true;
    private float scoreMultiplier = 1f;
    private Coroutine multiplierCoroutine;

    void Awake()
    {
        Instance = this;
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
        score = 0;
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

        yield return new WaitForSeconds(duration);

        scoreMultiplier = 1f;
        multiplierCoroutine = null;
    }
}