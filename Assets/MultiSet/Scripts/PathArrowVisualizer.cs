using UnityEngine;
using System.Collections.Generic;

public class PathArrowVisualizer : MonoBehaviour
{
    public GameObject arrowPrefab;
    public float arrowSpacing = 1.5f;
    public float arrowHeight = 0.5f;

    private List<GameObject> activeArrows = new List<GameObject>();
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        // Clear existing arrows
        foreach (var arrow in activeArrows) Destroy(arrow);
        activeArrows.Clear();

        // Get path points from LineRenderer
        Vector3[] pathCorners = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(pathCorners);

        // Place arrows along the path
        for (int i = 0; i < pathCorners.Length - 1; i++)
        {
            Vector3 start = pathCorners[i];
            Vector3 end = pathCorners[i + 1];
            Vector3 direction = (end - start).normalized;

            float distance = Vector3.Distance(start, end);
            int arrowsToPlace = Mathf.FloorToInt(distance / arrowSpacing);

            for (int j = 0; j < arrowsToPlace; j++)
            {
                Vector3 position = start + direction * (j * arrowSpacing);
                position.y += arrowHeight;
                Quaternion rotation = Quaternion.LookRotation(direction);

                GameObject arrow = Instantiate(arrowPrefab, position, rotation);
                activeArrows.Add(arrow);
            }
        }
    }
}