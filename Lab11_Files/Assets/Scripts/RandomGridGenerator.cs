using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Fills the Pathfinding grid with random obstacles
 * Each cell independently becomes a wall with probability 'obstacleProbability' - optionally re-rolls until the start can reach the goal
 * Kept in its own script so the BFS script stays focused on pathfinding
 * Controls: press R in Play mode, or right-click the component header in the Inspector -> "Generate Random Grid"
*/

// Generates random obstacle grids for a <see cref="Pathfinding"/> component on the same GameObject
[RequireComponent(typeof(Pathfinding))]
public class RandomGridGenerator : MonoBehaviour
{
    // INSPECTOR FIELDS

    [Header("Grid Size")]
    [Tooltip("Number of columns.")]
    [SerializeField, Min(2)] private int width = 10;

    [Tooltip("Number of rows.")]
    [SerializeField, Min(2)] private int height = 10;

    [Header("Obstacles")]
    [Tooltip("Chance (0-1) that any single cell becomes a wall.")]
    [SerializeField, Range(0f, 1f)] private float obstacleProbability = 0.25f;

    [Header("Options")]
    [Tooltip("Generate a random grid automatically when Play starts.")]
    [SerializeField] private bool generateOnStart = true;

    [Tooltip("Keep re-rolling until a path from start to goal exists.")]
    [SerializeField] private bool ensurePathExists = true;

    [Tooltip("Maximum re-rolls when 'Ensure Path Exists' is on.")]
    [SerializeField, Min(1)] private int maxAttempts = 50;

    [Tooltip("Use a fixed seed so the same grid can be reproduced.")]
    [SerializeField] private bool useSeed = false;

    [Tooltip("Seed used when 'Use Seed' is on.")]
    [SerializeField] private int seed = 12345;


    // PRIVATE STATE

    private Pathfinding pathfinder;

    // Lazy lookup so the ContextMenu also works in edit mode (Awake hasn't run)
    private Pathfinding Pathfinder
    {
        get
        {
            if (pathfinder == null)
            {
                pathfinder = GetComponent<Pathfinding>();
            }
            return pathfinder;
        }
    }


    // UNITY CALLBACKS

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateRandomGrid(width, height, obstacleProbability);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            GenerateRandomGrid(width, height, obstacleProbability);
        }
    }


    // ENHANCEMENT 2: GENERATE RANDOM GRID

    // Builds a new <paramref name="width"/> x <paramref name="height"/> grid where each cell is an obstacle with probability <paramref name="obstacleProbability"/>, then hands it to the pathfinder
    public void GenerateRandomGrid(int width, int height, float obstacleProbability)
    {
        // Validate inputs so bad values from other scripts can't break the grid.
        width = Mathf.Max(2, width);
        height = Mathf.Max(2, height);
        obstacleProbability = Mathf.Clamp01(obstacleProbability);

        if (useSeed)
        {
            // Seed once, before the attempt loop, so re-rolls still differ
            Random.InitState(seed);
        }

        int attempts = ensurePathExists ? maxAttempts : 1;

        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            int[,] newGrid = new int[height, width]; // [row, column]

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Random.value is in [0, 1], so this is true ~obstacleProbability of the time
                    newGrid[y, x] = Random.value < obstacleProbability ? 1 : 0;
                }
            }

            // Only log the final result, not every failed attempt
            bool pathFound = Pathfinder.SetGrid(newGrid, false);

            if (pathFound || !ensurePathExists)
            {
                Debug.Log($"Random grid {width}x{height} (p = {obstacleProbability:0.00}) generated " +
                          $"in {attempt} attempt(s). Path found: {pathFound}.");
                Pathfinder.RecalculatePath();
                return;
            }
        }

        Debug.LogWarning($"No solvable grid found in {maxAttempts} attempts at p = {obstacleProbability:0.00}. " +
                         "Showing the last grid. Try a lower obstacle probability.");
    }

    // Right-click the component header in the Inspector -> "Generate Random Grid"
    [ContextMenu("Generate Random Grid")]
    private void GenerateFromInspector()
    {
        GenerateRandomGrid(width, height, obstacleProbability);
    }
}