using UnityEngine;

public class TruckSidewaysRunner : MonoBehaviour
{
    private Vector3 targetLocalPosition;
    private Vector3 hiddenLocalPosition;
    private float triggerDistance;
    private float runInDuration;
    private bool triggered;
    private float animTimer;

    public void Init(float sideOffset, float triggerDistance, float runInDuration)
    {
        targetLocalPosition = transform.localPosition;
        hiddenLocalPosition = targetLocalPosition + new Vector3(sideOffset, 0f, 0f);
        transform.localPosition = hiddenLocalPosition;

        this.triggerDistance = triggerDistance;
        this.runInDuration = runInDuration;
    }

    void Update()
    {
        if (triggered)
        {
            animTimer += Time.deltaTime;
            float t = Mathf.Clamp01(animTimer / runInDuration);
            t = 1f - (1f - t) * (1f - t); // ease-out

            transform.localPosition = Vector3.Lerp(hiddenLocalPosition, targetLocalPosition, t);

            if (t >= 1f) enabled = false;
            return;
        }

        if (PlayerMovement.Instance == null) return;

        if (transform.position.z - PlayerMovement.Instance.transform.position.z <= triggerDistance)
        {
            triggered = true;
            animTimer = 0f;
        }
    }
}
