using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Transform model;
    public float forwardSpeed = 8f;
    public float laneDistance = 8f;
    public float laneSwitchSpeed = 10f;
    public float jumpForce = 7f;
    private bool isRolling = false;
    private Quaternion modelStartRotation;

    private int currentLane = 1; // 0 = left, 1 = center, 2 = right
    private Rigidbody rb;
    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        modelStartRotation = model.localRotation;
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
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        // Roll / Duck
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && !isRolling)
        {
            StartCoroutine(Roll());
        }

        // Constant forward movement
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime, Space.World);

        // Calculate target lane position
        Vector3 targetPosition = transform.position;
        targetPosition.x = (currentLane - 1) * laneDistance;

        // Smooth lane switching
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            laneSwitchSpeed * Time.deltaTime
        );
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private IEnumerator Roll()
    {
        isRolling = true;

        model.localRotation = modelStartRotation * Quaternion.Euler(90f, 0f, 0f);

        yield return new WaitForSeconds(1f);

        model.localRotation = modelStartRotation;

        isRolling = false;
    }
}