using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerObstacleHandler : MonoBehaviour
{
    private enum HitOutcome
    {
        Clean,
        Graze,
        SideCollision,
        Direct
    }

    [SerializeField] private float grazeOverlapThreshold = 0.25f;
    [SerializeField] private float lateralDominanceBias = 1.2f;

    [SerializeField] private float bounceBackDuration = 0.25f;
    [SerializeField] private float postBounceInvulnerability = 0.5f;

    [SerializeField] private int maxStrikes = 2;

    public float collidedDuration = 4f;

    private PlayerMovement playerMovement;
    private readonly HashSet<int> resolvedObstacles = new HashSet<int>();

    private int strikeCount = 0;
    private Coroutine strikeResetRoutine;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void OnTriggerEnter(Collider other) => HandlePotentialHit(other);
    void OnTriggerStay(Collider other) => HandlePotentialHit(other);

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Obstacle")) return;
        resolvedObstacles.Remove(other.GetInstanceID());
    }

    private void HandlePotentialHit(Collider other)
    {
        if (!other.CompareTag("Obstacle")) return;
        if (playerMovement.IsInvulnerable) return;

        int id = other.GetInstanceID();
        if (resolvedObstacles.Contains(id)) return;

        if (playerMovement.IsVerticalDodge(other)) return;

        HitOutcome outcome = Classify(other, out float overlapFraction, out _);
        if (outcome == HitOutcome.Clean) return;

        resolvedObstacles.Add(id);

        switch (outcome)
        {
            case HitOutcome.Graze:
                Debug.Log($"Graze - near-miss strike ({overlapFraction:P0} overlap)");
                RegisterStrike();
                break;

            case HitOutcome.SideCollision:
                Debug.Log("Side collision mid lane-change - near-miss strike, bouncing back");
                playerMovement.CancelLaneChangeAndBounce(bounceBackDuration, postBounceInvulnerability);
                RegisterStrike();
                break;

            case HitOutcome.Direct:
                NotifyCaught();
                break;
        }
    }

    private HitOutcome Classify(Collider other, out float overlapFraction, out Vector3 direction)
    {
        direction = Vector3.zero;
        overlapFraction = 0f;

        Bounds playerBounds = playerMovement.Col.bounds;
        Bounds obstacleBounds = other.bounds;

        float overlapX = Mathf.Min(playerBounds.max.x, obstacleBounds.max.x) - Mathf.Max(playerBounds.min.x, obstacleBounds.min.x);
        float overlapY = Mathf.Min(playerBounds.max.y, obstacleBounds.max.y) - Mathf.Max(playerBounds.min.y, obstacleBounds.min.y);
        float overlapZ = Mathf.Min(playerBounds.max.z, obstacleBounds.max.z) - Mathf.Max(playerBounds.min.z, obstacleBounds.min.z);

        if (overlapX <= 0f || overlapY <= 0f || overlapZ <= 0f)
            return HitOutcome.Clean;

        float playerVolume = playerBounds.size.x * playerBounds.size.y * playerBounds.size.z;
        float obstacleVolume = obstacleBounds.size.x * obstacleBounds.size.y * obstacleBounds.size.z;
        float referenceVolume = Mathf.Min(playerVolume, obstacleVolume);
        float overlapVolume = overlapX * overlapY * overlapZ;
        overlapFraction = referenceVolume > 0f ? overlapVolume / referenceVolume : 1f;

        bool hasPenetrationInfo = Physics.ComputePenetration(
            playerMovement.Col, playerMovement.Rb.position, playerMovement.Rb.rotation,
            other, other.transform.position, other.transform.rotation,
            out direction, out _);

        bool lateral = hasPenetrationInfo && Mathf.Abs(direction.x) * lateralDominanceBias > Mathf.Abs(direction.z);

        if (playerMovement.IsChangingLane && lateral)
            return HitOutcome.SideCollision;

        return overlapFraction < grazeOverlapThreshold ? HitOutcome.Graze : HitOutcome.Direct;
    }

    private void RegisterStrike()
    {
        strikeCount++;
        Debug.Log($"Strike {strikeCount}/{maxStrikes}");

        if (strikeResetRoutine != null) StopCoroutine(strikeResetRoutine);
        strikeResetRoutine = StartCoroutine(ResetStrikesAfterDelay());

        if (strikeCount >= maxStrikes)
        {
            // TODO: real game-over (max strikes reached). For now, just log like a caught hit.
            NotifyCaught();
        }
    }

    private IEnumerator ResetStrikesAfterDelay()
    {
        yield return new WaitForSeconds(collidedDuration);
        strikeCount = 0;
        strikeResetRoutine = null;
        Debug.Log("Strike count reset - no near misses for a while");
    }

    private void NotifyCaught()
    {
        // TODO: real game-over logic (stop run, show reason, restart). For now just log.
        Debug.Log("player caught");
    }
}
