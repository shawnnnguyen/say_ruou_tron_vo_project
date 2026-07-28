using UnityEngine;

public class ItemFloatSpin : MonoBehaviour
{
    public float floatHeight = 0.3f;
    public float floatSpeed = 2f;
    public float spinSpeed = 90f;
    public float heightOffset = 1f;

    private Vector3 startLocalPos;

    void Start()
    {
        startLocalPos = transform.localPosition + new Vector3(0f, heightOffset, 0f);
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.localPosition = startLocalPos + new Vector3(0f, yOffset, 0f);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }
}
