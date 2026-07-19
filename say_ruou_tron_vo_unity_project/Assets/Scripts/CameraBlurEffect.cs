using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class CameraBlurEffect : MonoBehaviour
{
    [SerializeField] private PostProcessVolume postProcessVolume;

    [Header("Dizzy Effect")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float dizzyAngle = 6f;
    [SerializeField] private float dizzySpeed = 8f;

    private DepthOfField depthOfField;

    private Coroutine blurRoutine;
    private Coroutine dizzyRoutine;

    private Quaternion originalRotation;

    private void Awake()
    {
        if (cameraTransform == null)
        {
            cameraTransform = transform;
        }

        originalRotation = cameraTransform.localRotation;

        if (postProcessVolume == null)
        {
            Debug.LogError("Post Process Volume has not been assigned.");
            return;
        }

        if (postProcessVolume.profile == null)
        {
            Debug.LogError(
                "The Post Process Volume does not have a profile assigned."
            );
            return;
        }

        if (!postProcessVolume.profile.TryGetSettings(out depthOfField))
        {
            Debug.LogError(
                "The Post Process Profile does not contain a Depth of Field effect."
            );
            return;
        }

        depthOfField.enabled.Override(false);
    }

    // Banana: blur and dizzy
    public void PlayBlur(float duration)
    {
        if (blurRoutine != null)
        {
            StopCoroutine(blurRoutine);
        }

        blurRoutine = StartCoroutine(
            BlurRoutine(duration)
        );

        PlayDizzy(duration);
    }

    // WineBottle: dizzy only
    public void PlayDizzy(float duration)
    {
        if (cameraTransform == null)
            return;

        if (dizzyRoutine != null)
        {
            StopCoroutine(dizzyRoutine);
        }

        dizzyRoutine = StartCoroutine(
            DizzyRoutine(duration)
        );
    }

    private IEnumerator BlurRoutine(float duration)
    {
        if (depthOfField == null)
            yield break;

        depthOfField.enabled.Override(true);

        yield return new WaitForSeconds(duration);

        depthOfField.enabled.Override(false);
        blurRoutine = null;
    }

    private IEnumerator DizzyRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float effectStrength =
                Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);

            float zRotation =
                Mathf.Sin(elapsed * dizzySpeed) *
                dizzyAngle *
                effectStrength;

            cameraTransform.localRotation =
                originalRotation *
                Quaternion.Euler(0f, 0f, zRotation);

            yield return null;
        }

        cameraTransform.localRotation = originalRotation;
        dizzyRoutine = null;
    }

    public void StopEffects()
    {
        if (blurRoutine != null)
        {
            StopCoroutine(blurRoutine);
            blurRoutine = null;
        }

        if (dizzyRoutine != null)
        {
            StopCoroutine(dizzyRoutine);
            dizzyRoutine = null;
        }

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = originalRotation;
        }

        if (depthOfField != null)
        {
            depthOfField.enabled.Override(false);
        }
    }

    private void OnDisable()
    {
        StopEffects();
    }
}