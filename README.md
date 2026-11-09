# Lab_11

**1. How does each pathfinding algorithm calculate and prioritize paths?** <br>
<blockquote>
    All three scripts work in a similar way. Each one keeps a list of cells to check next (the frontier) and a `cameFrom` dictionary that remembers how each cell was reached. Once the goal is reached, the path is built by following `cameFrom` backward from the goal to the start. The main difference is how each algorithm decides which cell to check next. <br>

* **BFS** uses a regular queue, so it checks cells in the order it found them. It spreads out evenly from the start, one step at a time. Since every move in our grid costs the same, the first path it finds to the goal is also the shortest. BFS doesn't look at movement costs, though, so on a weighted grid it might not find the cheapest path.
* **Dijkstra** uses a priority queue and always checks the cell with the lowest total cost from the start, g(n). If it finds a cheaper way to reach a cell, it updates that cell. This guarantees the cheapest path even when cells have different costs. The downside is that it searches in every direction, even away from the goal.
* **A*** works like Dijkstra but adds a heuristic, which is an estimate of how far a cell is from the goal. Its priority is f(n) = g(n) + h(n). In our script the heuristic is Manhattan distance (|dx| + |dy|), which fits a grid where you can only move up, down, left and right. Because of the heuristic, A* checks cells closer to the goal first, so it usually explores fewer cells than Dijkstra. It still finds the shortest path, because Manhattan distance never overestimates the real distance. <br>
</blockquote>
