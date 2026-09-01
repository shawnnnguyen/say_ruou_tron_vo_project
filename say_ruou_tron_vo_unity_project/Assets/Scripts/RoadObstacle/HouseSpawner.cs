using System.Collections.Generic;
using UnityEngine;

public class HouseSpawner : MonoBehaviour
{
    public enum HouseSide { Left, Right }

    [System.Serializable]
    public class HouseEntry
    {
        public GameObject prefab;

        [Tooltip("Sidewalk this prefab is authored to face at identity rotation. Spawning it on the other side applies a 180-degree flip instead.")]
        public HouseSide nativeSide = HouseSide.Left;
    }

    public List<HouseEntry> houses = new List<HouseEntry>();

    [Tooltip("Extra clearance added beyond the measured road / street light reach.")]
    public float sidewalkMargin = 0f;

    [Tooltip("Minimum gap left between adjacent houses along the sidewalk.")]
    public float houseZMargin = 1.5f;

    [Range(0f, 1f)]
    public float slotFillChance = 0.6f;

    public string streetLightRootName = "GAME-STRETT-LIGHT";
    public string roadRootName = "GAME-ROAD";

    const float MinZStep = 0.01f;

    // A wrap prefab's mesh is not necessarily centered on its own origin
    // transform (some are authored several units off to one side), so the
    // X placement math needs the actual min/max extent relative to that
    // origin rather than an assumed symmetric half-width.
    struct HouseMeasurement
    {
        public float minX, maxX; // local extent along X, relative to the prefab's own origin
        public float minZ, maxZ; // local extent along Z, relative to the prefab's own origin
        public float groundOffset;
    }

    static readonly Dictionary<GameObject, HouseMeasurement> measurementCache = new Dictionary<GameObject, HouseMeasurement>();

    public void SpawnHouses(float roadLength)
    {
        if (houses.Count == 0) return;

        // The road's own paved width is the real "no gap" boundary; street
        // light reach is kept as a Mathf.Max floor purely so a house never
        // clips through one that happens to poke out past the pavement edge.
        // Trucks are allowed to overlap houses, so they aren't factored in.
        float roadHalfWidth = MeasureRoadHalfWidth();
        float lightClearanceX = MeasureStreetLightClearance();
        float baseX = Mathf.Max(roadHalfWidth, lightClearanceX) + sidewalkMargin;

        SpawnSide(baseX, roadLength, leftSide: true);
        SpawnSide(baseX, roadLength, leftSide: false);
    }

    void SpawnSide(float baseX, float roadLength, bool leftSide)
    {
        HouseSide side = leftSide ? HouseSide.Left : HouseSide.Right;
        GameObject lastPlacedPrefab = null;

        float z = houseZMargin;

        while (z < roadLength - houseZMargin)
        {
            HouseEntry entry = PickHouse(lastPlacedPrefab);
            if (entry == null || entry.prefab == null)
            {
                z += Mathf.Max(houseZMargin * 2f, MinZStep);
                continue;
            }

            // Wrap prefabs are already correctly oriented at identity rotation,
            // so unlike a fixed 90-degree twist, spawning at 0 or 180 degrees
            // never swaps which raw mesh axis is spacing (X) vs depth (Z).
            HouseMeasurement measurement = GetMeasurement(entry);
            bool flipped = entry.nativeSide != side;

            // A 180-degree flip about Y negates local X and Z alike, so the
            // mesh's world-space extent on both axes mirrors around the origin.
            float m = flipped ? -1f : 1f;

            float depthZ = measurement.maxZ - measurement.minZ;
            float slotPitch = depthZ + houseZMargin;

            if (z + depthZ > roadLength - houseZMargin) break;

            // Solve for the origin's Z so the mesh's leading edge sits exactly
            // at the cursor, using the house's real edge offset instead of
            // assuming it's centered on its own origin — the same fix as X,
            // so adjacent houses end up flush with zero gap between them.
            float zEdgeA = m * measurement.minZ;
            float zEdgeB = m * measurement.maxZ;
            float leadingZExtent = Mathf.Min(zEdgeA, zEdgeB);
            float posZ = z - leadingZExtent;

            if (Random.value <= slotFillChance)
            {
                float extentA = m * measurement.minX;
                float extentB = m * measurement.maxX;
                float innerExtent = leftSide ? Mathf.Max(extentA, extentB) : Mathf.Min(extentA, extentB);

                // Solve for the origin position that puts the mesh's inner
                // (road-facing) edge exactly baseX away from this object's
                // own origin transform, using the house's real edge offset
                // instead of assuming it's centered on its own origin.
                float x = leftSide ? -baseX - innerExtent : baseX - innerExtent;

                float groundY = measurement.groundOffset;
                Vector3 position = transform.position + new Vector3(x, groundY, posZ);
                Quaternion rotation = entry.nativeSide == side
                    ? entry.prefab.transform.rotation
                    : entry.prefab.transform.rotation * Quaternion.Euler(0f, 180f, 0f);

                GameObject obj = Instantiate(entry.prefab, position, rotation, transform);
                obj.SetActive(true);

                lastPlacedPrefab = entry.prefab;
            }

            z += Mathf.Max(slotPitch, MinZStep);
        }
    }

    // Uniform pick across every entry with a prefab assigned, excluding the
    // last placed prefab (if any other choice exists) so the same house type
    // never lands in two consecutive slots along one sidewalk. Weighting
    // between house types isn't exposed for per-entry tuning right now —
    // if a specific house should appear rarer (e.g. caotang as a landmark),
    // that's handled by listing it fewer times / adjusting this method
    // directly rather than via an Inspector field.
    HouseEntry PickHouse(GameObject excludePrefab)
    {
        List<HouseEntry> valid = new List<HouseEntry>();
        foreach (HouseEntry entry in houses)
            if (entry.prefab != null) valid.Add(entry);

        if (valid.Count == 0) return null;

        List<HouseEntry> withoutRepeat = valid.FindAll(e => e.prefab != excludePrefab);
        List<HouseEntry> pool = withoutRepeat.Count > 0 ? withoutRepeat : valid;

        return pool[Random.Range(0, pool.Count)];
    }

    HouseMeasurement GetMeasurement(HouseEntry entry)
    {
        EnsureMeasured(entry);
        return measurementCache[entry.prefab];
    }

    void EnsureMeasured(HouseEntry entry)
    {
        if (measurementCache.ContainsKey(entry.prefab)) return;

        GameObject temp = Instantiate(entry.prefab, Vector3.zero, entry.prefab.transform.rotation);
        temp.SetActive(true);
        Bounds bounds = CombineRendererBounds(temp);
        Destroy(temp);

        measurementCache[entry.prefab] = new HouseMeasurement
        {
            minX = bounds.min.x,
            maxX = bounds.max.x,
            minZ = bounds.min.z,
            maxZ = bounds.max.z,
            groundOffset = -bounds.min.y,
        };
    }

    float MeasureRoadHalfWidth()
    {
        return MeasureChildClearanceX(roadRootName);
    }

    float MeasureStreetLightClearance()
    {
        return MeasureChildClearanceX(streetLightRootName);
    }

    float MeasureChildClearanceX(string namePrefix)
    {
        float maxAbsX = 0f;

        foreach (Transform child in transform)
        {
            if (!child.name.StartsWith(namePrefix)) continue;

            foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>())
            {
                float reach = Mathf.Max(
                    Mathf.Abs(renderer.bounds.min.x - transform.position.x),
                    Mathf.Abs(renderer.bounds.max.x - transform.position.x)
                );
                maxAbsX = Mathf.Max(maxAbsX, reach);
            }
        }

        return maxAbsX;
    }

    static Bounds CombineRendererBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }
}
