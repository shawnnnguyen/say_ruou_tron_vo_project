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
    private Rigidbody body;
    private BoxCollider sandalCollider;
    private Vector3 previousPosition;
    private Vector3 currentFlightVelocity;

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
        previousPosition = startPosition;

        float apexZ = cameraTransform != null
            ? cameraTransform.position.z + apexDistanceInFrontOfCamera
            : Mathf.Lerp(startPosition.z, player.position.z, 0.5f);
        apexProgress = Mathf.Clamp(
            Mathf.InverseLerp(startPosition.z, player.position.z, apexZ),
            0.1f,
            0.9f);
        isFlying = true;

        CreateTriggerFromVisualBounds();

        body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
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
        Vector3 nextPosition = new Vector3(startPosition.x, y, z);
        currentFlightVelocity = (nextPosition - previousPosition) / Time.fixedDeltaTime;
        transform.position = nextPosition;
        previousPosition = nextPosition;

        if (approachT >= 1f)
        {
            DropWithPhysics();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFlying) return;
        if (other.transform.IsChildOf(transform)) return;
        if (other.CompareTag("Ground") || other.transform.root.CompareTag("Ground")) return;

        PlayerMovement hitPlayer = other.GetComponentInParent<PlayerMovement>();
        if (hitPlayer != null)
        {
            isFlying = false;
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver("GOTCHA B!TCH");
            return;
        }

        DropWithPhysics();
    }

    private void DropWithPhysics()
    {
        if (!isFlying) return;

        isFlying = false;
        hasLanded = true;

        sandalCollider.isTrigger = false;
        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = currentFlightVelocity;
        body.angularVelocity = new Vector3(5f, 3f, 8f);

        Destroy(gameObject, 5f);
    }

    private void CreateTriggerFromVisualBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        sandalCollider = gameObject.AddComponent<BoxCollider>();
        sandalCollider.isTrigger = true;

        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        sandalCollider.center = transform.InverseTransformPoint(bounds.center);
        Vector3 scale = transform.lossyScale;
        sandalCollider.size = new Vector3(
            bounds.size.x / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
            bounds.size.y / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
            bounds.size.z / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
    }
}
