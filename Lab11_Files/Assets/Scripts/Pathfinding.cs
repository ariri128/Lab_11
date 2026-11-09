using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * The provided BFS script, modified for Enhancement 1
 * Changes from the original:
 *  - Start and goal are [SerializeField]s, editable in the Inspector (edit mode AND play mode) and by mouse at runtime:
 *      Left click  = set START cell
 *      Right click = set GOAL cell
 *  - The path is cleared before every search, so the path can be recalculated any number of times (the original appended to the old path)
 *  - current / next are now local variables instead of fields
 *  - Exposes a small public grid API so RandomGridGenerator.cs and ObstacleEditor.cs can change the grid without touching BFS
 *  - Added a cells-explored counter for comparing algorithms
 *  - The grid is drawn relative to this GameObject's position
*/

// Breadth-First Search pathfinding on a 2D int grid (0 = walkable, 1 = wall), with start and goal cells that can be changed at runtime
public class Pathfinding : MonoBehaviour
{
    // INSPECTOR FIELDS

    [Header("Start / Goal")]
    [Tooltip("Start cell (x = column, y = row). Shown as GREEN.")]
    [SerializeField] private Vector2Int start = new Vector2Int(0, 1);

    [Tooltip("Goal cell (x = column, y = row). Shown as RED.")]
    [SerializeField] private Vector2Int goal = new Vector2Int(4, 4);

    [Header("Display")]
    [Tooltip("World-space size of one grid cell.")]
    [SerializeField, Min(0.1f)] private float cellSize = 1f;

    [Header("Runtime Mouse Selection")]
    [Tooltip("Left click sets the start, right click sets the goal.")]
    [SerializeField] private bool enableMouseSelection = true;

    [Tooltip("Camera used to turn the mouse position into a grid cell. Empty = Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Header("Debug")]
    [Tooltip("Print path length and explored-cell count to the Console after each search.")]
    [SerializeField] private bool logSearchResults = true;


    // PRIVATE STATE

    // 0 = walkable, 1 = wall. Indexed as grid[row (y), column (x)]
    private int[,] grid = new int[,]
    {
        { 0, 1, 0, 0, 0 },
        { 0, 1, 0, 1, 0 },
        { 0, 0, 0, 1, 0 },
        { 0, 1, 1, 1, 0 },
        { 0, 0, 0, 0, 0 }
    };

    private readonly List<Vector2Int> path = new List<Vector2Int>(); // Final path from start to goal (inclusive) - empty when no path exists

    // 4-way movement: right, left, up, down
    private readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private int lastExploredCount; // How many cells the last search dequeued (useful for comparing algorithms)


    // PUBLIC READ-ONLY PROPERTIES

    public int Width => grid.GetLength(1); // Number of columns in the grid
    public int Height => grid.GetLength(0); // Number of rows in the grid
    public Vector2Int StartCell => start; // Current start cell 
    public Vector2Int GoalCell => goal; // Current goal cell
    public IReadOnlyList<Vector2Int> Path => path; // The most recently calculated path (empty if none)


    // UNITY CALLBACKS

    private void Start()
    {
        RecalculatePath();
    }

    private void Update()
    {
        if (enableMouseSelection)
        {
            HandleMouseSelection();
        }
    }

    // Called by the Editor whenever a value changes in the Inspector (in edit mode and in play mode)
	// Keeps start/goal inside the grid and refreshes the path so the Scene view updates immediately
    private void OnValidate()
    {
        start = ClampToGrid(start);
        goal = ClampToGrid(goal);
        RecalculatePath(false);
    }

    private void OnDrawGizmos()
    {
        Vector3 cellScale = new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f);

        // Draw grid cells: black = wall, white = walkable
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Gizmos.color = grid[y, x] == 1 ? Color.black : Color.white;
                Gizmos.DrawCube(CellToWorld(new Vector2Int(x, y)), cellScale);
            }
        }

        // Draw path slightly above the grid so it doesn't z-fight with cells
        Gizmos.color = Color.blue;
        foreach (Vector2Int step in path)
        {
            Gizmos.DrawCube(CellToWorld(step) + Vector3.up * 0.05f, cellScale);
        }

        // Draw start and goal on top of everything
        Gizmos.color = Color.green;
        Gizmos.DrawCube(CellToWorld(start) + Vector3.up * 0.1f, cellScale);

        Gizmos.color = Color.red;
        Gizmos.DrawCube(CellToWorld(goal) + Vector3.up * 0.1f, cellScale);
    }


    // ENHANCEMENT 1: DYNAMIC START / GOAL

    // Moves the start to <paramref name="cell"/> and recalculates the path
    // Returns True if the start was changed
    public bool SetStart(Vector2Int cell)
    {
        if (!CanBeEndpoint(cell, "start"))
        {
            return false;
        }

        start = cell;
        RecalculatePath();
        return true;
    }

    // Moves the goal to <paramref name="cell"/> and recalculates the path
    // Returns True if the goal was changed
    public bool SetGoal(Vector2Int cell)
    {
        if (!CanBeEndpoint(cell, "goal"))
        {
            return false;
        }

        goal = cell;
        RecalculatePath();
        return true;
    }

    // Left click = start, right click = goal (two-finger click on a Mac trackpad)
    private void HandleMouseSelection()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame && TryGetCellUnderMouse(out Vector2Int startCell))
        {
            SetStart(startCell);
        }
        else if (mouse.rightButton.wasPressedThisFrame && TryGetCellUnderMouse(out Vector2Int goalCell))
        {
            SetGoal(goalCell);
        }
    }

    // A start/goal must be inside the grid and not on a wall
    private bool CanBeEndpoint(Vector2Int cell, string label)
    {
        if (!IsInBounds(cell))
        {
            Debug.LogWarning($"Cannot set {label} to {cell}: outside the grid.");
            return false;
        }

        if (IsObstacle(cell))
        {
            Debug.LogWarning($"Cannot set {label} to {cell}: that cell is an obstacle.");
            return false;
        }

        return true;
    }


    // PUBLIC GRID API

    // Returns True if <paramref name="point"/> is inside the grid
    public bool IsInBounds(Vector2Int point)
    {
        return point.x >= 0 && point.x < Width && point.y >= 0 && point.y < Height;
    }

    // Returns True if the cell is a wall (value 1)
    public bool IsObstacle(Vector2Int point)
    {
        return grid[point.y, point.x] == 1;
    }

    // Returns True if the current path goes through <paramref name="point"/>
    public bool PathContains(Vector2Int point)
    {
        return path.Contains(point);
    }

    // Writes a raw value (0 = walkable, 1 = wall) into one cell
    // Does NOT recalculate the path - the caller decides when that's needed
    public void SetCellValue(Vector2Int point, int value)
    {
        if (IsInBounds(point))
        {
            grid[point.y, point.x] = value;
        }
    }

    // Replaces the whole grid (e.g., with a randomly generated one)
    // Start and goal are clamped into the new bounds and forced walkable
    // Returns True if a path exists on the new grid
    public bool SetGrid(int[,] newGrid, bool logResult = true)
    {
        grid = newGrid;

        start = ClampToGrid(start);
        goal = ClampToGrid(goal);

        // The endpoints must never be walls, or no path could ever be found
        grid[start.y, start.x] = 0;
        grid[goal.y, goal.x] = 0;

        return RecalculatePath(logResult);
    }

    // Converts the mouse position into a grid cell by raycasting onto the horizontal plane the grid is drawn on
    // Returns True if the mouse is over a cell inside the grid
    public bool TryGetCellUnderMouse(out Vector2Int cell)
    {
        cell = default;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null || Mouse.current == null)
        {
            return false;
        }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane gridPlane = new Plane(Vector3.up, transform.position);

        if (!gridPlane.Raycast(ray, out float distance))
        {
            return false;
        }

        // Cubes are centered on (x * cellSize, z * cellSize), so round to nearest
        Vector3 local = ray.GetPoint(distance) - transform.position;
        cell = new Vector2Int(Mathf.RoundToInt(local.x / cellSize), Mathf.RoundToInt(local.z / cellSize));

        return IsInBounds(cell);
    }

    // World-space center of a grid cell
    public Vector3 CellToWorld(Vector2Int cell)
    {
        return transform.position + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
    }


    // BFS PATHFINDING

    // Runs BFS from the current start to the current goal and stores the result in <see cref="Path"/>
    // Returns True if a path was found
    public bool RecalculatePath()
    {
        return RecalculatePath(logSearchResults);
    }

    // Same as <see cref="RecalculatePath()"/>, with explicit logging control
    // Returns True if a path was found
    public bool RecalculatePath(bool logResult)
    {
        bool found = FindPath(start, goal);

        if (logResult)
        {
            if (found)
            {
                Debug.Log($"BFS: path {start} -> {goal} found. Steps: {path.Count - 1}, cells explored: {lastExploredCount}.");
            }
            else
            {
                Debug.Log($"BFS: path {start} -> {goal} not found. Cells explored: {lastExploredCount}.");
            }
        }

        return found;
    }

    // Right-click the component header in the Inspector -> "Recalculate Path"
    [ContextMenu("Recalculate Path")]
    private void RecalculatePathFromMenu()
    {
        RecalculatePath(true);
    }

    private bool FindPath(Vector2Int startCell, Vector2Int goalCell)
    {
        // Always start from a clean path (fixes accumulation in the original)
        path.Clear();
        lastExploredCount = 0;

        if (!IsInBounds(startCell) || !IsInBounds(goalCell) || IsObstacle(startCell) || IsObstacle(goalCell))
        {
            return false;
        }

        // FIFO queue: cells are explored in order of distance (in steps) from start
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        frontier.Enqueue(startCell);

        // cameFrom doubles as the "visited" set and the breadcrumb trail
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        cameFrom[startCell] = startCell;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            lastExploredCount++;

            // Early exit: the first time BFS reaches the goal is via a fewest-steps route
            if (current == goalCell)
            {
                break;
            }

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                if (IsInBounds(next) && !IsObstacle(next) && !cameFrom.ContainsKey(next))
                {
                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }

        if (!cameFrom.ContainsKey(goalCell))
        {
            return false;
        }

        // Walk the breadcrumbs back from goal to start, then reverse
        Vector2Int step = goalCell;
        while (step != startCell)
        {
            path.Add(step);
            step = cameFrom[step];
        }
        path.Add(startCell);
        path.Reverse();

        return true;
    }


    // HELPERS

    private Vector2Int ClampToGrid(Vector2Int cell)
    {
        return new Vector2Int(Mathf.Clamp(cell.x, 0, Width - 1), Mathf.Clamp(cell.y, 0, Height - 1));
    }
}