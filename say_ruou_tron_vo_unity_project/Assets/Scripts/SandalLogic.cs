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

        if (HandleSweptCollision(previousPosition, nextPosition))
            return;

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

    private bool HandleSweptCollision(Vector3 from, Vector3 to)
    {
        Vector3 movement = to - from;
        float distance = movement.magnitude;
        if (distance <= 0.0001f) return false;

        Vector3 direction = movement / distance;
        Bounds bounds = sandalCollider.bounds;
        RaycastHit[] hits = Physics.BoxCastAll(
            bounds.center,
            bounds.extents * 0.9f,
            direction,
            transform.rotation,
            distance,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Collider hitCollider = hit.collider;
            if (hitCollider == null || hitCollider == sandalCollider) continue;
            if (hitCollider.transform.IsChildOf(transform)) continue;
            if (hitCollider.CompareTag("Ground") || hitCollider.transform.root.CompareTag("Ground")) continue;

            PlayerMovement hitPlayer = hitCollider.GetComponentInParent<PlayerMovement>();
            if (hitPlayer != null)
            {
                isFlying = false;
                if (GameManager.Instance != null)
                    GameManager.Instance.TriggerGameOver("GOTCHA B!TCH");
                return true;
            }

            float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
            transform.position = from + direction * safeDistance;

            Vector3 collisionNormal = hit.normal.sqrMagnitude > 0.01f
                ? hit.normal.normalized
                : -direction;
            Vector3 bounceVelocity = Vector3.Reflect(currentFlightVelocity, collisionNormal) * 0.65f;
            bounceVelocity += Vector3.up * 2f;
            DropWithPhysics(bounceVelocity);
            return true;
        }

        return false;
    }

    private void DropWithPhysics(Vector3? initialVelocity = null)
    {
        if (!isFlying) return;

        isFlying = false;
        hasLanded = true;

        sandalCollider.isTrigger = false;
        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = initialVelocity ?? currentFlightVelocity;
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
