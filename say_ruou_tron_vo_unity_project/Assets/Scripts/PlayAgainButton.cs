using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PlayAgainButton : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        button.onClick.AddListener(HandleClick);
    }

    void OnDisable()
    {
        button.onClick.RemoveListener(HandleClick);
    }

    public void HandleClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance is null - no GameManager in scene?");
        }
    }
}
