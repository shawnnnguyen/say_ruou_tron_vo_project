using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Transform model;
    public float forwardSpeed = 15f;
    public float laneDistance = 8f;
    public float laneSwitchSpeed = 15f;

    public float maxForwardSpeed = 40f;
    public float speedRampDistance = 1500f;

    public float jumpHeight = 10f;
    public float jumpDuration = 5f;
    public float fastFallDuration = 0.15f;

    public float rollDuration = 1f;
    public float duckSize = 0.5f;

    private bool isRolling = false;
    private bool isJumping = false;
    private bool isChangingLane = false;
    private bool isBouncing = false;
    private bool invulnerable = false;
    private Quaternion modelStartRotation;

    private int currentLane = 1;
    private int previousLane;
    private Rigidbody rb;
    private Collider col;
    private CapsuleCollider capsule;
    private float normalHeight;
    private Vector3 normalCenter;
    private Animator anim;
    private float baseY;
    private float verticalOffset = 0f;

    private Coroutine jumpRoutine;
    private Coroutine rollRoutine;
    private Coroutine bounceRoutine;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        capsule = col as CapsuleCollider;
        if (capsule != null)
        {
            normalHeight = capsule.height;
            normalCenter = capsule.center;
        }
        anim = model.GetComponent<Animator>();
        modelStartRotation = model.localRotation;
        baseY = transform.position.y;
        previousLane = currentLane;
    }

    void Update()
    {
        // Move left
        if (!isBouncing && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)))
        {
            int newLane = Mathf.Max(0, currentLane - 1);
            if (newLane != currentLane)
            {
                if (!isChangingLane) previousLane = currentLane;
                currentLane = newLane;
                isChangingLane = true;
            }
        }

        // Move right
        if (!isBouncing && (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)))
        {
            int newLane = Mathf.Min(2, currentLane + 1);
            if (newLane != currentLane)
            {
                if (!isChangingLane) previousLane = currentLane;
                currentLane = newLane;
                isChangingLane = true;
            }
        }

        // Jump
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) && !isJumping)
        {
            if (jumpRoutine != null) StopCoroutine(jumpRoutine);
            jumpRoutine = StartCoroutine(JumpRoutine());
        }

        // Roll
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && !isRolling)
        {
            if (rollRoutine != null) StopCoroutine(rollRoutine);
            rollRoutine = StartCoroutine(RollRoutine());
        }
    }

    private const float LaneChangeEpsilon = 0.05f;

    // Exposed for obstacle-collision classification (PlayerObstacleHandler),
    // which needs the live hitbox/rigidbody to measure overlap and penetration direction.
    public Collider Col => col;
    public Rigidbody Rb => rb;
    public bool IsChangingLane => isChangingLane;
    public bool IsInvulnerable => invulnerable;

    public float maxVerticalOverlap = 2.0f;

    public bool IsVerticalClear(Collider other)
    {
        Bounds playerBounds = col.bounds;
        Bounds obstacleBounds = other.bounds;

        float overlap = Mathf.Min(playerBounds.max.y, obstacleBounds.max.y)
                       - Mathf.Max(playerBounds.min.y, obstacleBounds.min.y);

        return overlap <= maxVerticalOverlap;
    }

    public bool IsVerticalDodge(Collider other)
    {
        return (isJumping || isRolling) && IsVerticalClear(other);
    }

    public void CancelLaneChangeAndBounce(float bounceDuration, float invulnerabilityDuration)
    {
        if (bounceRoutine != null) StopCoroutine(bounceRoutine);
        bounceRoutine = StartCoroutine(BounceRoutine(bounceDuration, invulnerabilityDuration));
    }

    private IEnumerator BounceRoutine(float bounceDuration, float invulnerabilityDuration)
    {
        isBouncing = true;
        invulnerable = true;

        int laneBeforeRevert = currentLane;
        currentLane = previousLane;
        previousLane = laneBeforeRevert;
        isChangingLane = true;

        if (anim != null) anim.SetTrigger("Bounce");

        yield return new WaitForSeconds(bounceDuration);
        isBouncing = false;

        yield return new WaitForSeconds(invulnerabilityDuration);
        invulnerable = false;

        bounceRoutine = null;
    }

    private float GetLaneTargetX()
    {
        return (currentLane - 1) * laneDistance;
    }

    private float GetSpeedT()
    {
        return Mathf.Clamp01(rb.position.z / speedRampDistance);
    }

    void FixedUpdate()
    {
        float speedT = GetSpeedT();
        float currentForwardSpeed = Mathf.Lerp(forwardSpeed, maxForwardSpeed, speedT);

        Vector3 nextPosition = rb.position + Vector3.forward * currentForwardSpeed * Time.fixedDeltaTime;

        float targetX = GetLaneTargetX();
        nextPosition.x = Mathf.Lerp(rb.position.x, targetX, laneSwitchSpeed * Time.fixedDeltaTime);

        nextPosition.y = baseY + verticalOffset;

        rb.MovePosition(nextPosition);
    }

    void LateUpdate()
    {
        if (!isChangingLane) return;

        if (Mathf.Abs(rb.position.x - GetLaneTargetX()) <= LaneChangeEpsilon)
        {
            isChangingLane = false;
        }
    }

    private IEnumerator JumpRoutine()
    {
        isJumping = true;
        anim.SetBool("isJumping", true);
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);
            verticalOffset = jumpHeight * Mathf.Sin(t * Mathf.PI);
            yield return null;
        }

        verticalOffset = 0f;
        isJumping = false;
        anim.SetBool("isJumping", false);
        jumpRoutine = null;
    }

    private IEnumerator RollRoutine()
    {
        isRolling = true;
        anim.SetBool("isRolling", true);
        model.localRotation = modelStartRotation * Quaternion.Euler(90f, 0f, 0f);

        if (capsule != null)
        {
            float newHeight = Mathf.Max(2f * capsule.radius, normalHeight * duckSize);
            float bottomY = normalCenter.y - normalHeight / 2f;
            Vector3 newCenter = normalCenter;
            newCenter.y = bottomY + newHeight / 2f;

            capsule.height = newHeight;
            capsule.center = newCenter;
        }

        if (isJumping)
        {
            if (jumpRoutine != null) StopCoroutine(jumpRoutine);
            isJumping = false;
            anim.SetBool("isJumping", false);
            jumpRoutine = null;

            float startOffset = verticalOffset;
            float elapsed = 0f;
            while (elapsed < fastFallDuration)
            {
                elapsed += Time.deltaTime;
                verticalOffset = Mathf.Lerp(startOffset, 0f, elapsed / fastFallDuration);
                yield return null;
            }
            verticalOffset = 0f;
        }

        yield return new WaitForSeconds(rollDuration);

        if (capsule != null)
        {
            capsule.height = normalHeight;
            capsule.center = normalCenter;
        }

        model.localRotation = modelStartRotation;

        isRolling = false;
        anim.SetBool("isRolling", false);
        rollRoutine = null;
    }
}
