using System.Collections;
using UnityEngine;

public class SandalSpawner : MonoBehaviour
{
    [Header("Spawnpunkte (hinten)")]
    public Transform[] lanes; // 3 Spawnpunkte

    [Header("Zielpunkt (vorne)")]
    public Transform targetPoint;

    public GameObject sandalPrefab;

    public float minSpawnTime = 5f;
    public float maxSpawnTime = 10f;

    public float flySpeed = 8f;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

            int lane = Random.Range(0, lanes.Length);

            GameObject sandal = Instantiate(
                sandalPrefab,
                lanes[lane].position,
                lanes[lane].rotation
            );

            sandal.AddComponent<SandalFly>().Init(targetPoint.position, flySpeed);
        }
    }
}