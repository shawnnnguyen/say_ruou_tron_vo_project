using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");
    private static readonly int JumpStateHash = Animator.StringToHash("Base Layer.jump");
    private static readonly int RunStateHash = Animator.StringToHash("Base Layer.runescape2");

    public static PlayerMovement Instance;

    public Transform model;
    public float forwardSpeed = 15f;
    public float laneDistance = 8f;
    public float laneSwitchSpeed = 15f;

    public float maxForwardSpeed = 40f;
    public float speedRampDistance = 1500f;

    public float jumpHeight = 5f;
    public float jumpDuration = 5f;
    public float fastFallDuration = 0.15f;

    public float rollDuration = 1f;
    public float duckSize = 0.5f;

    private bool isRolling = false;
    private bool isJumping = false;
    private bool controlsReversed = false;
    private bool isChangingLane = false;
    private bool isBouncing = false;
    private bool invulnerable = false;
    private bool isFrozen = false;
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
    private Coroutine wineRoutine;
    private Coroutine bounceRoutine;

    public CameraBlurEffect cameraBlurEffect;
    private bool isStunned = false;

    [Header("Banana Stun")]
    public float bananaStunDuration = 2f;
    public float stunnedSpeedMultiplier = 2f;

    [Header("Thuoc Lao x2 Score")]
    public float doubleScoreDuration = 5f;

    [Header("Wine Reverse Duration")]
    public float wineDuration = 5f;

    void Awake()
    {
        Instance = this;
    }

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
        if (isFrozen) return;

        // Move left
        if (!isBouncing && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)))
        {
            int newLane = controlsReversed
                ? Mathf.Min(2, currentLane + 1) // move right
                : Mathf.Max(0, currentLane - 1); // move left
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
            int newLane = controlsReversed
                ? Mathf.Max(0, currentLane - 1) // move left
                : Mathf.Min(2, currentLane + 1); // move right
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
            if (controlsReversed)
            {
                if (rollRoutine != null) StopCoroutine(rollRoutine);
                rollRoutine = StartCoroutine(RollRoutine());
            }
            else
            {
                if (jumpRoutine != null) StopCoroutine(jumpRoutine);
                jumpRoutine = StartCoroutine(JumpRoutine());    
            }

        }

        // Roll
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && !isRolling)
        {
            if (controlsReversed)
            {
                if (jumpRoutine != null) StopCoroutine(jumpRoutine);
                jumpRoutine = StartCoroutine(JumpRoutine());  
            }
            else
            {
                if (rollRoutine != null) StopCoroutine(rollRoutine);
                rollRoutine = StartCoroutine(RollRoutine());                
            }

        }
    }

    private const float LaneChangeEpsilon = 0.05f;

    public Collider Col => col;
    public Rigidbody Rb => rb;
    public bool IsChangingLane => isChangingLane;
    public bool IsInvulnerable => invulnerable;

    public float maxVerticalOverlap = 2.0f;

    private float GetVerticalOverlap(Collider other)
    {
        Bounds playerBounds = col.bounds;
        Bounds obstacleBounds = other.bounds;

        return Mathf.Min(playerBounds.max.y, obstacleBounds.max.y)
             - Mathf.Max(playerBounds.min.y, obstacleBounds.min.y);
    }

    public bool IsVerticalClear(Collider other)
    {
        return GetVerticalOverlap(other) <= maxVerticalOverlap;
    }

    // A jump/roll that clears the obstacle but still has some vertical bounds
    // overlap (a close call) rather than fully separating from it.
    public bool IsVerticalGraze(Collider other)
    {
        float overlap = GetVerticalOverlap(other);
        return overlap > 0f && overlap <= maxVerticalOverlap;
    }

    public bool IsVerticalDodge(Collider other)
    {
        return (isJumping || isRolling) && IsVerticalClear(other);
    }

    public void Freeze()
    {
        isFrozen = true;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
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

        if (isChangingLane)
        {
            int laneBeforeRevert = currentLane;
            currentLane = previousLane;
            previousLane = laneBeforeRevert;
        }
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
        if (isFrozen) return;

        float speedT = GetSpeedT();
        //float currentForwardSpeed = Mathf.Lerp(forwardSpeed, maxForwardSpeed, speedT);
        float currentForwardSpeed = Mathf.Lerp(
            forwardSpeed,
            maxForwardSpeed,
            speedT
        );

        if (isStunned)
        {
            currentForwardSpeed *= stunnedSpeedMultiplier;
        }
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
        if (anim != null)
        {
            anim.SetBool(IsJumpingHash, true);
            // Start the actual jump clip immediately at its first frame. A
            // transition blend here can otherwise look like a frozen run pose.
            anim.Play(JumpStateHash, 0, 0f);
            anim.Update(0f);

            AnimatorClipInfo[] jumpClips = anim.GetCurrentAnimatorClipInfo(0);
            if (jumpClips.Length > 0 && jumpDuration > 0f)
                anim.speed = jumpClips[0].clip.length / jumpDuration;
        }
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
        PlayRunAnimationAfterJump();
        jumpRoutine = null;
    }

    private void PlayRunAnimationAfterJump()
    {
        if (anim == null) return;

        anim.SetBool(IsJumpingHash, false);
        anim.speed = 1f;
        anim.CrossFadeInFixedTime(RunStateHash, 0.1f, 0, 0f);
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
            PlayRunAnimationAfterJump();
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

    private IEnumerator BananaStun()
    {
        if (isStunned)
            yield break;

        isStunned = true;

        if (cameraBlurEffect != null)
        {
            cameraBlurEffect.PlayBlur(
                bananaStunDuration
            );
        }

        yield return new WaitForSeconds(
            bananaStunDuration
        );

        isStunned = false;
    }

    private IEnumerator WineDuration()
    {
        controlsReversed = true;

        cameraBlurEffect.PlayBlur(wineDuration);

        WineEffectUI.Instance.ShowBar(wineDuration);

        yield return new WaitForSeconds(wineDuration);

        controlsReversed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Banana"))
        {
            StartCoroutine(BananaStun());
            Destroy(other.gameObject);
        }

        if (other.CompareTag("ThuocLao"))
        {
            ScoreManager.Instance.ActivateDoubleScore(doubleScoreDuration);
            Destroy(other.gameObject);
        }      

        if (other.CompareTag("WineBottle"))
        {
            if (wineRoutine != null)
            {
                StopCoroutine(wineRoutine);
            }

            wineRoutine = StartCoroutine(WineDuration());
            Destroy(other.gameObject);
        }
    }
}
