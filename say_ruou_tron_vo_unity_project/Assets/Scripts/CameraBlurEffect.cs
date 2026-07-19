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
    private Coroutine effectRoutine;
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

    public void PlayBlur(float duration)
    {
        if (depthOfField == null)
            return;

        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
        }

        effectRoutine = StartCoroutine(
            BlurAndDizzyRoutine(duration)
        );
    }

    private IEnumerator BlurAndDizzyRoutine(float duration)
    {
        depthOfField.enabled.Override(true);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float zRotation =
                Mathf.Sin(elapsed * dizzySpeed) *
                dizzyAngle;

            cameraTransform.localRotation =
                originalRotation *
                Quaternion.Euler(0f, 0f, zRotation);

            yield return null;
        }

        cameraTransform.localRotation = originalRotation;
        depthOfField.enabled.Override(false);

        effectRoutine = null;
    }

    private void OnDisable()
    {
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = originalRotation;
        }

        if (depthOfField != null)
        {
            depthOfField.enabled.Override(false);
        }
    }
}