using UnityEngine;

public class RoadObjectsSpawner : MonoBehaviour
{
    public GameObject bananaSkinPrefab;
    public GameObject trashBinPrefab;
    public GameObject alcoholSignPrefab;
    public GameObject streetLightPrefab;

    public float laneDistance = 8f;
    public float roadLength = 30f;

    public float difficultyRampDistance = 1200f;
    public float minSpawnChance = 0.45f;
    public float maxSpawnChance = 1f;

    public float minObjectZ = 5f;
    public float maxObjectZ = 15f;

    public int minRowCount = 1;
    public int maxRowCount = 3;
    public float rowSpacing = 20f;

    public void SpawnObjects(float distance)
    {
        float t = Mathf.Clamp01(distance / difficultyRampDistance);
        float spawnChance = Mathf.Lerp(minSpawnChance, maxSpawnChance, t);

        int rowsAtDifficulty = Mathf.RoundToInt(Mathf.Lerp(minRowCount, maxRowCount, t));
        int rowCount = Random.Range(minRowCount, rowsAtDifficulty + 1);

        float[] laneX = { -laneDistance, 0f, laneDistance };
        for (int row = 0; row < rowCount; row++)
        {
            float rowBaseZ = row * rowSpacing;
            foreach (float x in laneX)
            {
                if (Random.value < spawnChance) SpawnRandomObject(x, rowBaseZ);
            }
        }

        Spawn(streetLightPrefab, -13f, 0f, 15f);
    }

    void SpawnRandomObject(float laneX, float rowBaseZ)
    {
        float z = rowBaseZ + Random.Range(minObjectZ, maxObjectZ);

        switch (Random.Range(0, 3))
        {
            case 0: Spawn(bananaSkinPrefab, laneX, 0f, z); break;
            case 1: Spawn(trashBinPrefab, laneX, 0f, z); break;
            case 2: Spawn(alcoholSignPrefab, laneX, 0f, z); break;
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
        obj.SetActive(true);
    }
}