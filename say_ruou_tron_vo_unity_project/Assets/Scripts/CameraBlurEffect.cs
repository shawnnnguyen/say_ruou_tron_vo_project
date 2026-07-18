using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class CameraBlurEffect : MonoBehaviour
{
    [SerializeField] private PostProcessVolume postProcessVolume;

    private DepthOfField depthOfField;
    private Coroutine blurRoutine;

    private void Awake()
    {
        if (postProcessVolume == null)
        {
            Debug.LogError("Post Process Volume wurde nicht zugewiesen.");
            return;
        }

        if (postProcessVolume.profile == null)
        {
            Debug.LogError("Das Post Process Volume besitzt kein Profile.");
            return;
        }

        if (!postProcessVolume.profile.TryGetSettings(out depthOfField))
        {
            Debug.LogError(
                "Im Post Process Profile fehlt der Depth-of-Field-Effekt."
            );
            return;
        }

        depthOfField.enabled.Override(false);
    }

    public void PlayBlur(float duration)
    {
        if (depthOfField == null)
            return;

        if (blurRoutine != null)
            StopCoroutine(blurRoutine);

        blurRoutine = StartCoroutine(BlurRoutine(duration));
    }

    private IEnumerator BlurRoutine(float duration)
    {
        depthOfField.enabled.Override(true);

        yield return new WaitForSeconds(duration);

        depthOfField.enabled.Override(false);
        blurRoutine = null;
    }
}