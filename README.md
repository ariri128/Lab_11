# Lab_11

**1. How does each pathfinding algorithm calculate and prioritize paths?** <br>
<blockquote>
  All three scripts work in a similar way. Each one keeps a list of cells to check next (the frontier) and a _cameFrom_ dictionary that remembers how each cell was reached. Once the goal is reached, the path is built by following _cameFrom_ backward from the goal to the start. The main difference is how each algorithm decides which cell to check next. <br>

* **BFS** uses a regular queue, so it checks cells in the order it found them. It spreads out evenly from the start, one step at a time. Since every move in our grid costs the same, the first path it finds to the goal is also the shortest. BFS doesn't look at movement costs, though, so on a weighted grid it might not find the cheapest path.
* **Dijkstra** uses a priority queue and always checks the cell with the lowest total cost from the start, g(n). If it finds a cheaper way to reach a cell, it updates that cell. This guarantees the cheapest path even when cells have different costs. The downside is that it searches in every direction, even away from the goal.
* **A*** works like Dijkstra but adds a heuristic, which is an estimate of how far a cell is from the goal. Its priority is f(n) = g(n) + h(n). In our script the heuristic is Manhattan distance (|dx| + |dy|), which fits a grid where you can only move up, down, left and right. Because of the heuristic, A* checks cells closer to the goal first, so it usually explores fewer cells than Dijkstra. It still finds the shortest path, because Manhattan distance never overestimates the real distance. <br>
</blockquote>

**2. What challenges arise when dynamically updating obstacles in real time?** <br>
<blockquote>
* **The current path can become outdated.** If a new obstacle lands on the path, the path has to be recalculated. My _AddObstacle_ method checks whether the cell is on the current path before turning it into a wall, and only recalculates if it is. If the cell isn't on the path, the path is still valid, because adding a wall can't create a shorter route.
* **Removing obstacles is different.** Removing a wall can open up a shorter path, so the path should always be recalculated after a removal.
* **Performance.** Recalculating the whole path every time something changes is fine on a small grid. On a big map with many characters, or with obstacles changing every frame, it can slow the game down. Algorithms like D* Lite or LPA* help by updating only the part of the path that was affected.
* **Edge cases.** An obstacle could be placed on the start or goal, or it could block off the goal completely. My code doesn't allow walls on the start or goal, and when no path exists it logs a message instead of crashing.
* **Moving characters.** In an actual game, a character might already be walking along the path when it gets blocked. It would need to recalculate from where it currently is, not from the original start. <br>
</blockquote>
