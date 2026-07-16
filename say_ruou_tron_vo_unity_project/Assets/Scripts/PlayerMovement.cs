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

    private bool isRolling = false;
    private bool isJumping = false;
    private Quaternion modelStartRotation;

    private int currentLane = 1;
    private Rigidbody rb;
    private Animator anim;
    private float baseY;
    private float verticalOffset = 0f;

    private Coroutine jumpRoutine;
    private Coroutine rollRoutine;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = model.GetComponent<Animator>();
        modelStartRotation = model.localRotation;
        baseY = transform.position.y;
    }

    void Update()
    {
        // Move left
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentLane = Mathf.Max(0, currentLane - 1);
        }

        // Move right
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentLane = Mathf.Min(2, currentLane + 1);
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

    private float GetSpeedT()
    {
        return Mathf.Clamp01(rb.position.z / speedRampDistance);
    }

    void FixedUpdate()
    {
        float speedT = GetSpeedT();
        float currentForwardSpeed = Mathf.Lerp(forwardSpeed, maxForwardSpeed, speedT);

        Vector3 nextPosition = rb.position + Vector3.forward * currentForwardSpeed * Time.fixedDeltaTime;

        float targetX = (currentLane - 1) * laneDistance;
        nextPosition.x = Mathf.Lerp(rb.position.x, targetX, laneSwitchSpeed * Time.fixedDeltaTime);

        nextPosition.y = baseY + verticalOffset;

        rb.MovePosition(nextPosition);
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

        model.localRotation = modelStartRotation * Quaternion.Euler(90f, 0f, 0f);

        yield return new WaitForSeconds(rollDuration);

        model.localRotation = modelStartRotation;

        isRolling = false;
        anim.SetBool("isRolling", false);
        rollRoutine = null;
    }
}
