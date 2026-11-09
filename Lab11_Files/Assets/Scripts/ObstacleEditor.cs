using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Turns grid cells into obstacles while the game is running and re-plans the BFS path only when it is actually affected
 * Controls (Play mode, with the Game view focused):
 *  - Hover a cell + press O = AddObstacle
 *  - Hover a cell + press X = RemoveObstacle
 * Inspector context menu "Add Test Obstacle", and an optional timer that drops random obstacles (dynamic obstacles)
*/

// Adds / removes obstacles on a <see cref="Pathfinding"/> grid at runtime and keeps the path up to date
[RequireComponent(typeof(Pathfinding))]
public class ObstacleEditor : MonoBehaviour
{
    // INSPECTOR FIELDS

    [Header("Keyboard Controls")]
    [Tooltip("Hover a cell and press this key to make it an obstacle.")]
    [SerializeField] private Key addObstacleKey = Key.O;

    [Tooltip("Hover a cell and press this key to clear an obstacle.")]
    [SerializeField] private Key removeObstacleKey = Key.X;

    [Header("Inspector Test")]
    [Tooltip("Cell used by the 'Add Test Obstacle' context menu item.")]
    [SerializeField] private Vector2Int testObstaclePosition = new Vector2Int(2, 2);

    [Header("Dynamic Obstacles (optional)")]
    [Tooltip("Drop an obstacle on a random walkable cell every few seconds.")]
    [SerializeField] private bool spawnRandomObstacles = false;

    [Tooltip("Seconds between random obstacles.")]
    [SerializeField, Min(0.1f)] private float spawnInterval = 2f;


    // PRIVATE STATE

    private Pathfinding pathfinder;
    private float spawnTimer;

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

    private void Update()
    {
        HandleKeyboardInput();

        if (spawnRandomObstacles)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnRandomObstacle();
            }
        }
    }


    // ENHANCEMENT 3: ADD OBSTACLE

    // Marks the cell at <paramref name="position"/> as an obstacle
    // The path is only recalculated if the new wall sits on it: adding a wall can never make another route shorter, so an untouched path is still valid
    // Returns True if the cell was changed into an obstacle
    public bool AddObstacle(Vector2Int position)
    {
        if (!Pathfinder.IsInBounds(position))
        {
            Debug.LogWarning($"AddObstacle: {position} is outside the grid.");
            return false;
        }

        if (position == Pathfinder.StartCell || position == Pathfinder.GoalCell)
        {
            Debug.LogWarning($"AddObstacle: {position} is the start or goal and can't be blocked.");
            return false;
        }

        if (Pathfinder.IsObstacle(position))
        {
            Debug.Log($"AddObstacle: {position} is already an obstacle.");
            return false;
        }

        // Check BEFORE changing the cell, while the old path is still stored
        bool wasOnPath = Pathfinder.PathContains(position);

        Pathfinder.SetCellValue(position, 1);
        Debug.Log($"AddObstacle: {position} is now an obstacle.");

        if (wasOnPath)
        {
            Debug.Log("AddObstacle: obstacle blocked the current path - recalculating.");
            Pathfinder.RecalculatePath();
        }

        return true;
    }

    // Clears the obstacle at <paramref name="position"/>
	// Always recalculates, because opening a cell CAN create a new, shorter route (or the first route)
    // Returns True if an obstacle was removed
    public bool RemoveObstacle(Vector2Int position)
    {
        if (!Pathfinder.IsInBounds(position) || !Pathfinder.IsObstacle(position))
        {
            return false;
        }

        Pathfinder.SetCellValue(position, 0);
        Debug.Log($"RemoveObstacle: {position} is walkable again.");
        Pathfinder.RecalculatePath();
        return true;
    }


    // INPUT AND HELPERS

    private void HandleKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[addObstacleKey].wasPressedThisFrame && Pathfinder.TryGetCellUnderMouse(out Vector2Int addCell))
        {
            AddObstacle(addCell);
        }
        else if (keyboard[removeObstacleKey].wasPressedThisFrame && Pathfinder.TryGetCellUnderMouse(out Vector2Int removeCell))
        {
            RemoveObstacle(removeCell);
        }
    }

    // Picks a random walkable, non-endpoint cell (a few tries) and blocks it
    private void SpawnRandomObstacle()
    {
        const int maxTries = 20;

        for (int i = 0; i < maxTries; i++)
        {
            Vector2Int cell = new Vector2Int(Random.Range(0, Pathfinder.Width), Random.Range(0, Pathfinder.Height));

            bool isEndpoint = cell == Pathfinder.StartCell || cell == Pathfinder.GoalCell;
            if (!isEndpoint && !Pathfinder.IsObstacle(cell))
            {
                AddObstacle(cell);
                return;
            }
        }
    }

    // Right-click the component header in the Inspector -> "Add Test Obstacle"
    [ContextMenu("Add Test Obstacle")]
    private void AddTestObstacle()
    {
        AddObstacle(testObstaclePosition);
    }
}