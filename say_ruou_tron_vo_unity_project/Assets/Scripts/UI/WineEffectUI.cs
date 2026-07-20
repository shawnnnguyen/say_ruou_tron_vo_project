using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WineEffectUI : MonoBehaviour
{
    public static WineEffectUI Instance;

    public GameObject wineBar;
    public Image wineFill;

    private Coroutine effectRoutine;

    private void Awake()
    {
        Instance = this;

        wineBar.SetActive(false);
    }

    public void ShowBar(float duration)
    {
        if (effectRoutine != null)
            StopCoroutine(effectRoutine);

        effectRoutine = StartCoroutine(BarRoutine(duration));
    }

    private IEnumerator BarRoutine(float duration)
    {
        wineBar.SetActive(true);
        wineFill.fillAmount = 1f;

        float timer = duration;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            wineFill.fillAmount = timer / duration;

            yield return null;
        }

        wineBar.SetActive(false);
        effectRoutine = null;
    }
}