using System.Collections.Generic;
using UnityEngine;

public class RoadObjectsSpawner : MonoBehaviour
{
    public GameObject bananaSkinPrefab;
    public GameObject trashBinPrefab;
    public GameObject alcoholSignPrefab;
    public GameObject truckPrefab;
    public GameObject thuocLaoPrefab;
    public GameObject wineBottlePrefab;

    public float firstRowZ = 6f;

    public float minReactionGap = 10f;

    [Range(0f, 1f)] public float jumpObstacleChance = 0.5f;
    [Range(0f, 1f)] public float duckObstacleChance = 0.5f;
    [Range(0f, 1f)] public float laneBlockObstacleChance = 0.25f;
    [Range(0f, 1f)] public float sidewaysTruckChance = 0.35f;
    [Range(0f, 1f)] public float pickupRowChance = 0.3f;
    [Range(0f, 1f)] public float forcedPickupChance = 0.15f;

    public float laneDistance = 8f;
    public float truckYOffset = 0f;

    public Vector2 truckPivotOffset = Vector2.zero;

    public float difficultyRampDistance = 1200f;
    public float minSpawnChance = 0.45f;
    public float maxSpawnChance = 1f;

    public int minRowCount = 1;
    public int maxRowCount = 3;

    public void SpawnObjects(float distance, float roadLength)
    {
        float t = Mathf.Clamp01(distance / difficultyRampDistance);
        float difficultyChance = Mathf.Lerp(minSpawnChance, maxSpawnChance, t);

        float rowSpan = minReactionGap * 4f;
        float rowContentSpan = minReactionGap * 3f;

        int maxRowsThatFit = Mathf.Max(1, 1 + Mathf.FloorToInt((roadLength - firstRowZ - rowContentSpan) / rowSpan));
        int cappedMaxRowCount = Mathf.Min(maxRowCount, maxRowsThatFit);
        int cappedMinRowCount = Mathf.Min(minRowCount, cappedMaxRowCount);

        int rowsAtDifficulty = Mathf.RoundToInt(Mathf.Lerp(cappedMinRowCount, cappedMaxRowCount, t));
        int rowCount = Random.Range(cappedMinRowCount, rowsAtDifficulty + 1);

        float[] laneX = { -laneDistance, 0f, laneDistance };

        List<GameObject> pickupPool = new List<GameObject> { bananaSkinPrefab, thuocLaoPrefab, wineBottlePrefab };
        pickupPool.RemoveAll(p => p == null);

        for (int row = 0; row < rowCount; row++)
        {
            float jumpZ = firstRowZ + row * rowSpan;
            float duckZ = jumpZ + minReactionGap;
            float laneBlockZ = duckZ + minReactionGap;
            float pickupZ = laneBlockZ + minReactionGap;

            if (Random.value < jumpObstacleChance * difficultyChance)
                Spawn(trashBinPrefab, laneX[Random.Range(0, 3)], 0f, jumpZ);

            if (Random.value < duckObstacleChance * difficultyChance)
                Spawn(alcoholSignPrefab, laneX[Random.Range(0, 3)], 0f, duckZ);

            if (Random.value < laneBlockObstacleChance * difficultyChance)
            {
                if (Random.value < sidewaysTruckChance)
                {
                    bool leftSide = Random.value < 0.5f;
                    float perpendicularX = leftSide ? -laneDistance / 2f : laneDistance / 2f;
                    float yRot = leftSide ? 0f : 180f;
                    SpawnTruck(perpendicularX, truckYOffset, laneBlockZ, yRot);
                }
                else
                {
                    float yRot = Random.value < 0.5f ? 90f : 270f;
                    SpawnTruck(laneX[Random.Range(0, 3)], truckYOffset, laneBlockZ, yRot);
                }
            }

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
        SpawnRotated(prefab, x, y, z, 0f);
    }

    void SpawnRotated(GameObject prefab, float x, float y, float z, float extraYRotation)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab is missing!");
            return;
        }

        Vector3 position = transform.position + new Vector3(x, y, z);
        Quaternion rotation = prefab.transform.rotation * Quaternion.Euler(0f, extraYRotation, 0f);

        GameObject obj = Instantiate(
            prefab,
            position,
            rotation,
            transform
        );
        obj.SetActive(true);
    }

    void SpawnTruck(float x, float y, float z, float extraYRotation)
    {
        if (truckPrefab == null)
        {
            Debug.LogError("Prefab is missing!");
            return;
        }

        Quaternion rotation = truckPrefab.transform.rotation * Quaternion.Euler(0f, extraYRotation, 0f);
        Vector3 pivotOffset = rotation * new Vector3(truckPivotOffset.x, 0f, truckPivotOffset.y);
        Vector3 position = transform.position + new Vector3(x, y, z) + pivotOffset;

        GameObject obj = Instantiate(
            truckPrefab,
            position,
            rotation,
            transform
        );
        obj.SetActive(true);
    }
}