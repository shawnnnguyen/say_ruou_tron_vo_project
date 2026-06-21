using UnityEngine;

public class RoadObjectsSpawner : MonoBehaviour
{
    public GameObject bananaSkinPrefab;
    public GameObject trashBinPrefab;
    public GameObject alcoholSignPrefab;
    public GameObject streetLightPrefab;

    public float laneDistance = 8f;
    public float roadLength = 30f;

    void Start()
    {
        Debug.Log("RoadObjectsSpawner started on: " + gameObject.name);
    }

    public void SpawnObjects()
    {
        Spawn(bananaSkinPrefab, -laneDistance, 0f, 10f);
        Spawn(trashBinPrefab, 0f, 0f, 18f);
        Spawn(alcoholSignPrefab, 8f, 0f, 15f);
        Spawn(streetLightPrefab, -13f, 0f, 15f);
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