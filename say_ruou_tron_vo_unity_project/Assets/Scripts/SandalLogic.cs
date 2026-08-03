using UnityEngine;

public class SandalLogic : MonoBehaviour
{
    private Transform player;
    private Transform cameraTransform;
    private Vector3 startPosition;
    private float reactionTime;
    private float flightDuration;
    private float arcHeight;
    private float apexProgress;
    private float groundY;
    private float distanceBehindAtLaunch;
    private float overtakeDistance;
    private float elapsedTime;
    private bool isFlying;
    private bool hasLanded;

    public void Launch(
        Transform playerTransform,
        Transform flightCamera,
        float timeToPlayer,
        float totalFlightTime,
        float heightOfArc,
        float apexDistanceInFrontOfCamera,
        float distanceAheadAtEnd)
    {
        player = playerTransform;
        cameraTransform = flightCamera;
        startPosition = transform.position;
        reactionTime = Mathf.Max(0.1f, timeToPlayer);
        flightDuration = Mathf.Max(reactionTime, totalFlightTime);
        arcHeight = Mathf.Max(0.1f, heightOfArc);
        groundY = player.position.y;
        distanceBehindAtLaunch = Mathf.Max(0f, player.position.z - startPosition.z);
        overtakeDistance = Mathf.Max(0f, distanceAheadAtEnd);

        float apexZ = cameraTransform != null
            ? cameraTransform.position.z + apexDistanceInFrontOfCamera
            : Mathf.Lerp(startPosition.z, player.position.z, 0.5f);
        apexProgress = Mathf.Clamp(
            Mathf.InverseLerp(startPosition.z, player.position.z, apexZ),
            0.1f,
            0.9f);
        isFlying = true;

        CreateTriggerFromVisualBounds();

        Rigidbody body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void FixedUpdate()
    {
        if (!isFlying || hasLanded || player == null) return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            isFlying = false;
            return;
        }

        elapsedTime += Time.fixedDeltaTime;
        float approachT = Mathf.Clamp01(elapsedTime / reactionTime);

        // Move relative to the runner from behind them to well ahead of them.
        // This guarantees that the sandal is visibly faster at every player speed.
        float relativeStartZ = -distanceBehindAtLaunch;
        float relativeZ = Mathf.Lerp(relativeStartZ, overtakeDistance, approachT);
        float z = player.position.z + relativeZ;

        // Two gravity-shaped parabolas make the peak occur at the requested
        // camera-relative position instead of always halfway through the path.
        float heightFactor;
        if (approachT <= apexProgress)
        {
            float upT = approachT / apexProgress;
            heightFactor = 1f - (1f - upT) * (1f - upT);
        }
        else
        {
            float downT = (approachT - apexProgress) / (1f - apexProgress);
            heightFactor = 1f - downT * downT;
        }

        float y = Mathf.Lerp(startPosition.y, groundY, approachT) + arcHeight * heightFactor;
        transform.position = new Vector3(startPosition.x, y, z);

        if (approachT >= 1f)
        {
            hasLanded = true;
            Destroy(gameObject, Mathf.Max(0.25f, flightDuration - reactionTime));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying) return;

        PlayerMovement hitPlayer = other.GetComponentInParent<PlayerMovement>();
        if (hitPlayer == null) return;

        isFlying = false;
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver("GOTCHA B!TCH");
    }

    private void CreateTriggerFromVisualBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;

        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        trigger.center = transform.InverseTransformPoint(bounds.center);
        Vector3 scale = transform.lossyScale;
        trigger.size = new Vector3(
            bounds.size.x / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
            bounds.size.y / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
            bounds.size.z / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
    }
}
