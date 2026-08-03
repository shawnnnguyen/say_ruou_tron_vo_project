using System.Collections;
using UnityEngine;

public class SandalSpawner : MonoBehaviour
{
    [SerializeField] private GameObject sandalPrefab;
    [SerializeField] private float minSpawnDelay = 5f;
    [SerializeField] private float maxSpawnDelay = 10f;
    [SerializeField] private float warningDuration = 1.5f;
    [SerializeField] private float warningForwardOffset = 6f;
    [SerializeField] private float laneDistance = 8f;
    [SerializeField] private float spawnDistanceInFrontOfCamera = 3f;
    [SerializeField] private float spawnHeight = 0.25f;
    [SerializeField] private float reactionTime = 5f;
    [SerializeField] private float flightDuration = 5.25f;
    [SerializeField] private float arcHeight = 3f;
    [SerializeField] private float apexDistanceInFrontOfCamera = 1f;
    [SerializeField] private float overtakeDistance = 125f;
    [SerializeField] private Vector3 sandalScale = new Vector3(10f, 10f, 10f);
    [SerializeField] private Vector3 sandalRotation = new Vector3(0f, 0f, 90f);

    private IEnumerator Start()
    {
        while (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));

            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                yield break;

            int lane = Random.Range(-1, 2);
            GameObject warning = ShowLaneWarning(lane);

            yield return new WaitForSeconds(warningDuration);

            if (warning != null) Destroy(warning);
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                yield break;

            SpawnSandal(lane);
        }
    }

    private GameObject ShowLaneWarning(int lane)
    {
        GameObject warning = new GameObject("Sandal Warning");
        warning.AddComponent<SandalLaneWarning>().Show(
            transform,
            lane * laneDistance,
            warningForwardOffset,
            warningDuration);
        return warning;
    }

    private void SpawnSandal(int lane)
    {
        if (sandalPrefab == null)
        {
            Debug.LogError("SandalSpawner: GAME-DEP prefab is missing.", this);
            return;
        }

        Camera mainCamera = Camera.main;
        float spawnZ = mainCamera != null
            ? mainCamera.transform.position.z + spawnDistanceInFrontOfCamera
            : transform.position.z - 3f;

        Vector3 spawnPosition = new Vector3(
            lane * laneDistance,
            transform.position.y + spawnHeight,
            spawnZ);

        GameObject sandal = Instantiate(
            sandalPrefab,
            spawnPosition,
            Quaternion.Euler(sandalRotation));

        sandal.name = "Flying GAME-DEP";
        sandal.transform.localScale = sandalScale;
        sandal.SetActive(true);
        sandal.AddComponent<SandalLogic>().Launch(
            transform,
            mainCamera != null ? mainCamera.transform : null,
            reactionTime,
            flightDuration,
            arcHeight,
            apexDistanceInFrontOfCamera,
            overtakeDistance);
    }

    private void OnValidate()
    {
        minSpawnDelay = Mathf.Max(0.1f, minSpawnDelay);
        maxSpawnDelay = Mathf.Max(minSpawnDelay, maxSpawnDelay);
        warningDuration = Mathf.Max(0.1f, warningDuration);
        laneDistance = Mathf.Max(0f, laneDistance);
        spawnDistanceInFrontOfCamera = Mathf.Max(0.5f, spawnDistanceInFrontOfCamera);
        reactionTime = Mathf.Max(0.1f, reactionTime);
        flightDuration = Mathf.Max(reactionTime, flightDuration);
        arcHeight = Mathf.Max(0.1f, arcHeight);
        overtakeDistance = Mathf.Max(1f, overtakeDistance);
    }
}

public class SandalLaneWarning : MonoBehaviour
{
    private Transform player;
    private float laneX;
    private float zOffset;
    private Renderer warningRenderer;

    public void Show(Transform playerTransform, float x, float forwardOffset, float duration)
    {
        player = playerTransform;
        laneX = x;
        zOffset = forwardOffset;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Sandal Warning Marker";
        marker.transform.SetParent(transform, false);
        marker.transform.localScale = new Vector3(6f, 0.08f, 7f);

        Collider markerCollider = marker.GetComponent<Collider>();
        markerCollider.enabled = false;
        Destroy(markerCollider);

        warningRenderer = marker.GetComponent<Renderer>();
        Material warningMaterial = new Material(FindWarningShader());
        warningMaterial.color = new Color(1f, 0.05f, 0.02f, 1f);
        warningRenderer.material = warningMaterial;

        StartCoroutine(Blink(duration));
    }

    private void LateUpdate()
    {
        if (player == null) return;

        transform.position = new Vector3(
            laneX,
            player.position.y + 0.05f,
            player.position.z + zOffset);
    }

    private IEnumerator Blink(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            warningRenderer.enabled = !warningRenderer.enabled;
            yield return new WaitForSeconds(0.15f);
            elapsed += 0.15f;
        }

        warningRenderer.enabled = true;
    }

    private static Shader FindWarningShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        return shader;
    }
}
