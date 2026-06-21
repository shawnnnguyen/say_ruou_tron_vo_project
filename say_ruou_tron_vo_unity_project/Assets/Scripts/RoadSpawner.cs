using System.Collections.Generic;
using UnityEngine;

public class RoadSpawner : MonoBehaviour
{
    public GameObject roadPrefab;
    public Transform player;

    public float roadLength = 30f;
    public int roadsOnScreen = 5;

    private float nextSpawnZ = 0f;
    private Queue<GameObject> activeRoads = new Queue<GameObject>();

    void Start()
    {
        for (int i = 0; i < roadsOnScreen; i++)
        {
            SpawnRoad();
        }
    }

    void Update()
    {
        // Spawn new road when player approaches the end
        if (player.position.z + roadLength * 2 > nextSpawnZ)
        {
            SpawnRoad();

            // Remove oldest road if too many are active
            if (activeRoads.Count > roadsOnScreen)
            {
                GameObject oldRoad = activeRoads.Dequeue();
                Destroy(oldRoad);
            }
        }
    }

    void SpawnRoad()
    {
        GameObject road = Instantiate(
            roadPrefab,
            new Vector3(0f, 0f, nextSpawnZ),
            Quaternion.identity
        );

        road.SetActive(true);
        foreach (Transform child in road.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.SetActive(true);
        }

        RoadObjectsSpawner objectSpawner = road.GetComponent<RoadObjectsSpawner>();

        if (objectSpawner != null)
        {
            objectSpawner.SpawnObjects();
        }
        else
        {
            Debug.LogError("RoadObjectsSpawner missing on spawned road!");
        }

        activeRoads.Enqueue(road);
        nextSpawnZ += roadLength;
    }
}