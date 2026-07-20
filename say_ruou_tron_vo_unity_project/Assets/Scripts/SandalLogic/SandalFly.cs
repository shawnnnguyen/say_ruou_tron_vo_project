using UnityEngine;

public class SandalFly : MonoBehaviour
{
    private Vector3 target;
    private float speed;

    public void Init(Vector3 targetPos, float flySpeed)
    {
        target = targetPos;
        speed = flySpeed;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        transform.LookAt(target);

        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            Destroy(gameObject);
        }
    }
}