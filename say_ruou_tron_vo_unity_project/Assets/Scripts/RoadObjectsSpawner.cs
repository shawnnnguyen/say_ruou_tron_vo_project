using System.Collections.Generic;
using UnityEngine;

public class RoadObjectsSpawner : MonoBehaviour
{
    public GameObject bananaSkinPrefab;
    public GameObject trashBinPrefab;
    public GameObject alcoholSignPrefab;

    [Header("Pickups")]
    public GameObject[] additionalPickupPrefabs;

    [Header("Rows")]
    public int rowCount = 2;
    public float firstRowZ = 6f;

    [Tooltip("Minimum z distance between consecutive obstacle/pickup slots, so the player has time to see, react to, and jump/roll past one before the next arrives.")]
    public float minReactionGap = 10f;

    [Header("Spawn chances (per lane, per row)")]
    [Range(0f, 1f)] public float jumpObstacleChance = 0.5f;
    [Range(0f, 1f)] public float duckObstacleChance = 0.5f;
    [Range(0f, 1f)] public float pickupRowChance = 0.3f;
    [Range(0f, 1f)] public float forcedPickupChance = 0.15f;

    public float laneDistance = 8f;
    public float roadLength = 30f;

    void Start()
    {
        Debug.Log("RoadObjectsSpawner started on: " + gameObject.name);
    }

    public void SpawnObjects()
    {
        float[] laneX = { -laneDistance, 0f, laneDistance };

        List<GameObject> pickupPool = new List<GameObject> { bananaSkinPrefab };
        pickupPool.AddRange(additionalPickupPrefabs);
        pickupPool.RemoveAll(p => p == null);

        float rowSpan = minReactionGap * 3f; // jump -> duck -> pickup -> next row's jump, each minReactionGap apart

        for (int row = 0; row < rowCount; row++)
        {
            float jumpZ = firstRowZ + row * rowSpan;
            float duckZ = jumpZ + minReactionGap;
            float pickupZ = duckZ + minReactionGap;

            if (Random.value < jumpObstacleChance)
                Spawn(trashBinPrefab, laneX[Random.Range(0, 3)], 0f, jumpZ);

            if (Random.value < duckObstacleChance)
                Spawn(alcoholSignPrefab, laneX[Random.Range(0, 3)], 0f, duckZ);

            if (pickupPool.Count > 0 && Random.value < pickupRowChance)
            {
                GameObject pickupPrefab = pickupPool[Random.Range(0, pickupPool.Count)];
                if (Random.value < forcedPickupChance)
                {
                    for (int lane = 0; lane < 3; lane++)
                        Spawn(pickupPrefab, laneX[lane], 0f, pickupZ);
                }
                else
                {
                    int lane = Random.Range(0, 3);
                    Spawn(pickupPrefab, laneX[lane], 0f, pickupZ);
                }
            }
        }
    }

    void Spawn(GameObject prefab, float x, float y, float z)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab is missing!");
            return;
        }

        Vector3 position = transform.position + new Vector3(x, y, z);

        GameObject obj = Instantiate(
            prefab,
            position,
            prefab.transform.rotation,
            transform
        );

        Debug.Log("Spawned: " + obj.name);
        obj.SetActive(true);
    }
}