using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

/**
 * Help the Christmas elves fetch presents in a magical labyrinth!
 **/
public enum MoveDir { Right, Left, Up, Down };

class Player
{
    #region Main

    static void Main(string[] args)
    {
        string[] inputs;

        // game loop
        while (true)
        {
            int turnType = int.Parse(Console.ReadLine());

            var board = new string[Board.Size, Board.Size];

            // Get board
            for (int i = 0; i < 7; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                for (int j = 0; j < 7; j++)
                {
                    string tile = inputs[j];

                    // Save board
                    board[j, i] = tile;
                }
            }

            // Get Players
            for (int i = 0; i < 2; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int numPlayerCards = int.Parse(inputs[0]); // the total number of quests for a player (hidden and revealed)
                int playerX = int.Parse(inputs[1]);
                int playerY = int.Parse(inputs[2]);
                string playerTile = inputs[3];

                if (i == 0)
                {
                    Info.Player.Position = new Coord(playerX, playerY);
                    Info.Player.NumOfQuests = numPlayerCards;
                    Info.Player.Tile = playerTile;
                }
                else
                {
                    Info.Enemy.Position = new Coord(playerX, playerY);
                    Info.Enemy.NumOfQuests = numPlayerCards;
                    Info.Enemy.Tile = playerTile;
                }
            }

            // Get Items
            int numItems = int.Parse(Console.ReadLine()); // the total number of items available on board and on player tiles

            // Reset stored quests positions
            Info.Player.Quests.ResetPositions();
            Info.Enemy.Quests.ResetPositions();

            // Add items
            for (int i = 0; i < numItems; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                string itemName = inputs[0];
                int itemX = int.Parse(inputs[1]);
                int itemY = int.Parse(inputs[2]);
                int itemPlayerId = int.Parse(inputs[3]);

                // Add items
                if (itemPlayerId == 0)
                {
                    Info.Player.Quests.Update(itemName, itemX, itemY);
                }
                else
                {
                    Info.Enemy.Quests.Update(itemName, itemX, itemY);
                }
            }

            // Get Quests
            int numQuests = int.Parse(Console.ReadLine()); // the total number of revealed quests for both players
            for (int i = 0; i < numQuests; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                string questItemName = inputs[0];
                int questPlayerId = int.Parse(inputs[1]);

                // Add items
                if (questPlayerId == 0)
                {
                    Info.Player.Quests.Update(questItemName, true);
                }
                else
                {
                    Info.Enemy.Quests.Update(questItemName, true);
                }

            }

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            //Console.WriteLine("PUSH 3 RIGHT"); // PUSH <id> <direction> | MOVE <direction> | PASS
            if (turnType == 0)
            {
                Push(board);
            }
            else
            {
                Move(board);
            }
        }
    }

    #endregion

    #region Push

    static void Push(string[,] boardDirections)
    {
        // Delete this
        Console.Error.WriteLine("Push Turn");

        var board = new Board(boardDirections);
        var finder = new PathFinder(board);

        // Player
        var playerPos = Info.Player.Position;
        var itemPos = Info.Player.Quests.GetActivePositions();

        // Enemy
        var enemyPos = Info.Enemy.Position;
        var enemyItemPos = Info.Enemy.Quests.GetActivePositions();

        string[] path;

        // 1. I have a path
        List<Coord> pickedItemPositions;

        if ((path = finder.FindBestPath(playerPos, itemPos, out pickedItemPositions, out Coord last)) != null
            && Rnd.Percent(Rnd.p_WouldBeNice))
        {
            Console.Error.WriteLine("I have a path...");

            // Create mask
            var mask = BoxMask.Generate(playerPos, path);

            // There are pushes that does not effect our path
            if (!mask.IsMaxSize())
            {
                Console.Error.WriteLine("Making PUSH that will no effect me...");

                // Select push that will help me (brute force, push item outside)
                // just make sure to consider BoxMask

                // Try to connect more quest items
                if (PushScenarios.BetterBoard
                    (board,
                    playerPos,
                    Coord.Except(itemPos, pickedItemPositions.ToArray()), // itemPos - pickedItemPositions
                    mask))
                {
                    Console.Error.WriteLine("Connecting more quest items...");
                    return;
                }

                // Insert item
                if (PushScenarios.ItemIsOutside(board, playerPos, mask))
                {
                    Console.Error.WriteLine("Inserting my item...");
                    return;
                }

                // Try to connect more items
                if (PushScenarios.BetterBoard(
                    board,
                    playerPos,
                    Info.Player.Quests.GetNextPossibleCoords(),
                    mask))
                {
                    Console.Error.WriteLine("Connecting more items.");
                    return;
                }

                // Try to push quest item out, but only if after we can push it in in good way (player can take it)
                if (PushScenarios.PushItemOut(Coord.Except(itemPos, pickedItemPositions.ToArray()), mask))
                {
                    Console.Error.WriteLine("Pushing my quest item out...");
                    return;
                }
                
                // Try to block enemy path
                if (PushScenarios.BlockEnemyPath(board, enemyPos, enemyItemPos, mask))
                {
                    Console.Error.WriteLine("Blocking enemy path...");
                    return;
                }

                // Try to push item out, but only if after we can push it in in good way (player can teke it)
                if (PushScenarios.PushItemOut(Info.Player.Quests.GetNextPossibleCoords(), mask))
                {
                    Console.Error.WriteLine("Pushing my item out...");
                    return;
                }

                // Try to block enemy push.
                if (PushScenarios.BlockEnemyPush(board, enemyPos, enemyItemPos, mask))
                {
                    Console.Error.WriteLine("Blocking enemy push...");
                    return;
                }

                // Random
                Console.Error.WriteLine("Random push...");
                Console.WriteLine(PushFinder.RandomPush(mask));
                return;
                
            }
            // Any push will effect our path
            else
            {
                // Try to find some push that will be good
                if (PushFinder.ValidPushBruteForce(board, playerPos, itemPos))
                {
                    Console.Error.WriteLine("Push will effect me, but I am still good...");
                    return;
                }

                // Random push
                Console.Error.WriteLine("Random push...");
                PushInfo.GetRandomPush(playerPos).ApplyPush();
                return;
            }
        }
        // 2. I dont have a path
        else
        {
            Console.Error.WriteLine("I dont have a path...");

            // Can we solve in one push (also works if item is outside)
            //if (PushScenarios.InOneMove(board, playerPos, itemPos))
            //{
            //    Console.Error.WriteLine("Solving in one push...");
            //    return;
            //}

            // Can we solve in two pushes (also works if item is outside)
            if (PushScenarios.InOneOrTwoPushes(board, playerPos, itemPos))
            {
                Console.Error.WriteLine("Solving in x pushes...");
                return;
            }

            // Push item out
            //if (PushScenarios.PushItemOutValid(board, playerPos, itemPos))
            //{
            //    Console.Error.WriteLine("Pushing valid item out...");
            //    return;
            //}

            // Does enemy have path =>
            // Try to block his path
            if (PushScenarios.BlockEnemyPath(board, enemyPos, enemyItemPos))
            {
                Console.Error.WriteLine("Blocking enemy path...");
                return;
            }

            // Move item closer to player

            // Try to block enemy push
            if (PushScenarios.BlockEnemyPush(board, enemyPos, enemyItemPos))
            {
                Console.Error.WriteLine("Blocking enemy push...");
                return;
            }

            // Random Push
            Console.Error.WriteLine("Random push...");
            PushScenarios.Random();
            return;
        }
    }

    #endregion

    #region Move

    static void Move(string[,] boardInfo)
    {
        // Delete this
        Console.Error.WriteLine("Move Turn");

        var board = new Board(boardInfo);
        var finder = new PathFinder(board);
        var itemsPos = Info.Player.Quests.GetActivePositions();
        var playerPos = Info.Player.Position;

        Coord lastPosReal; // Real last position of player
        List<Coord> visitedCoords;
        var path = finder.FindBestPath(playerPos, itemsPos, out visitedCoords, out lastPosReal);

        // We have path
        if (path != null)
        {
            Console.Error.WriteLine("Path is found!");

            // Remove completed quests
            Info.Player.Quests.RemoveQuests(visitedCoords);

            // Can we pick up some more things?
            // Pick all possible quest items
            while (path.Length < 20) // We cant extend path if it is full
            {
                // Check for active quests
                itemsPos = Info.Player.Quests.GetActivePositions();
                if (itemsPos.Length == 0) break;

                // Prepare finder
                finder.ResetNodes();

                // Prepere and search for path
                Coord lastPos;
                var secondPath = finder.FindBestPath(lastPosReal, itemsPos, out visitedCoords, out lastPos);

                // Path exist
                if (secondPath != null)
                {
                    Console.Error.WriteLine("Path is Extended!");

                    // Remove quests
                    Info.Player.Quests.RemoveQuests(visitedCoords);

                    // Set last position of player to current last position
                    lastPosReal = lastPos;

                    // change path
                    path = Path.MergePaths(path, secondPath);
                }
                else break; // Get out of loop
            }

            // Can we go to next possible quest
            if (path.Length < 20)
            {
                itemsPos = Info.Player.Quests.GetNextPossibleCoords();

                if (itemsPos.Length > 0)
                {
                    finder.ResetNodes();
                    var secondPath = finder.FindShortestPath(lastPosReal, itemsPos);

                    if (secondPath != null)
                    {
                        Console.Error.WriteLine("Moving to next possible quest!");
                        
                        // Change path
                        path = Path.MergePaths(path, secondPath);

                        // Print path
                        Path.ToOutput(path);
                        return;
                    }
                }

                // Go to closest ideal quest collect position TODO
                if (PathScenarios.GoToBestPosition(board, lastPosReal, itemsPos, path)) return;

                // Go to closes ideal position
                if (PathScenarios.GoToBestPosition(board, lastPosReal, Info.Player.Quests.GetNextPossibleCoords(), path)) return;

            }

            // Print path
            Path.ToOutput(path);
            return;
        }

        // No path was found
        // Go to closest ideal position
        if (PathScenarios.GoToBestPosition(board, playerPos, itemsPos)) return;

        // Got to position what could increase our possible path positions TODO

        Console.WriteLine("PASS");
    }

    #endregion
}

#region Path and Pathfinding

/// <summary>
/// Helping methods for working with path.
/// </summary>
public static class Path
{
    #region Converting

    /// <summary>
    /// Converts path to output (server).
    /// </summary>
    public static void ToOutput(string[] path)
    {
        var builder = new StringBuilder("MOVE");

        int counter = 0;
        foreach (var dir in path)
        {
            builder.Append(" ");
            builder.Append(dir);

            // Increase counter
            counter++;

            // End loop if max elements
            if (counter == 20) break;
        }

        Console.WriteLine(builder);
    }

    #endregion

    #region Generations

    /// <summary>
    /// Generate path out of nodes or empty array.
    /// </summary>
    public static string[] GeneratePath(Node current)
    {
        var list = new List<string>();

        while (current.Direction != null)
        {
            // Add direction
            list.Add(current.Direction);

            // Go to previous node
            current = current.PrevNode;
        }

        // Reverse list to get correct order of directions
        list.Reverse();

        return list.ToArray();
    }

    /// <summary>
    /// Generates path and locations that path crossed.
    /// </summary>
    /// <returns>Path or null</returns>
    public static string[] GeneratePath(Node last, ref List<Coord> locations)
    {
        // Check if we have some path
        if (last.PrevNode == null) return null;

        // For storing path
        var list = new List<string>();

        // Is current end
        if (last.IsEnd) locations.Add(new Coord(last.Pos.X, last.Pos.Y));

        // Trace to the beginning
        while (last.Direction != null)
        {
            // Add direction
            list.Add(last.Direction);

            // Go to previous node
            last = last.PrevNode;

            // Is it end
            if (last.IsEnd) locations.Add(new Coord(last.Pos.X, last.Pos.Y));
        }

        // Reverse list to get correct order of directions
        list.Reverse();

        // Remove locations from quests TODO: not really sure if this is right, what if enemy block the move
        Info.Player.Quests.RemoveQuests(locations);

        // Cut if to long
        return FixPathLength(list);
    }

    #endregion

    #region Merge

    /// <summary>
    /// Marge two paths in a way that dont exceed a limit.
    /// </summary>
    public static string[] MergePaths(string[] first, string[] second)
    {
        if (second == null || second.Length == 0)
            return first;

        if (first.Length < 20)
        {
            int newArrayLen = first.Length + second.Length;
            if (newArrayLen > 20) newArrayLen = 20;

            var newArray = new string[newArrayLen];

            // copy first
            for (int i = 0; i < first.Length; i++)
            {
                newArray[i] = first[i];
            }

            // copy second
            for (int i = first.Length; i < newArrayLen; i++)
            {
                newArray[i] = second[i - first.Length];
            }

            // return
            return newArray;
        }

        return first;
    }

    #endregion

    #region Fixing

    /// <summary>
    /// Fix path length if needed.
    /// </summary>
    public static string[] FixPathLength(List<string> path)
    {
        string[] result;
        if (path.Count >= 20) result = new string[20];
        else result = new string[path.Count];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = path[i];
        }

        return result;
    }

    #endregion

    #region Special

    /// <summary>
    /// Desired push direction. X=1 right, X=-1 left, Y=1 up, Y=-1 down.
    /// </summary>
    public static Coord GetDesiredDirection(Coord from, Coord to)
    {
        // Check if any coord is out of board
        if (from.X < 0 || from.Y < 0 || to.X < 0 || to.Y < 0)
        {
            return new Coord(0, 0);
        }
        // Same Horizontal
        if (from.Y == to.Y)
        {
            return new Coord(0, 1);
        }

        // Same Vertical
        if (from.X == to.X)
        {
            return new Coord(1, 0);
        }

        // Find direction
        int x = from.X - to.X;
        if (x > 0) x = -1; // Go left
        else if (x < 0) x = 1; // Go right
        else x = 0; // dont move

        int y = from.Y - to.Y;
        if (y > 0) y = 1;
        else if (y < 0) y = -1;
        else y = 0;

        return new Coord(x, y);
    }

    #endregion
}

/// <summary>
/// Path finding algorithms.
/// </summary>
public class PathFinder
{
    #region Init

    Board board;

    public PathFinder(Board board)
    {
        this.board = board;
    }

    #endregion

    #region Gen 1 => two points

    /// <summary>
    /// Finds shortest path in direction (UP, DOWN, RIGHT, LEFT).
    /// </summary>
    public string[] FindShortestPath(Coord start, Coord end)
    {
        return FindShortestPath(start.X, start.Y, end.X, end.Y);
    }

    /// <summary>
    /// Finds shortest path in direction (UP, DOWN, RIGHT, LEFT).
    /// </summary>
    public string[] FindShortestPath(int startX, int startY, int endX, int endY)
    {
        // Check if both positions are in board
        if (!Coord.InBoard(new Coord(startX, startY), new Coord(endX, endY)))
        {
            return null;
        }

        // Get start and end node
        var start = board[startX, startY];
        var end = board[endX, endY];

        // Create queue
        var queue = new Queue<Node>();
        start.Checked = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            // Enqueue next node
            var current = queue.Dequeue();

            // Check if end
            if (current == end)
            {
                return Path.GeneratePath(current);
            }

            // Add adjacent nodes
            if (Node.ExistAndNotChecked(current.Up))
            {
                // Mark it, set prev, save direction, enqueue
                current.Up.MarkIt(current, "UP");
                queue.Enqueue(current.Up);
            }

            if (Node.ExistAndNotChecked(current.Right))
            {
                current.Right.MarkIt(current, "RIGHT");
                queue.Enqueue(current.Right);
            }

            if (Node.ExistAndNotChecked(current.Down))
            {
                current.Down.MarkIt(current, "DOWN");
                queue.Enqueue(current.Down);
            }

            if (Node.ExistAndNotChecked(current.Left))
            {
                current.Left.MarkIt(current, "LEFT");
                queue.Enqueue(current.Left);
            }
        }

        return null;
    }

    #endregion

    #region Gen 2 => More points

    /// <summary>
    /// Best path is that go through more searched positions.
    /// </summary>
    /// <param name="visitedPos">List of positions that path goes through.</param>
    public string[] FindBestPath(Coord startPosition, Coord[] searchPositions, out List<Coord> visitedPos, out Coord LastCoord)
    {
        LastCoord = new Coord();
        visitedPos = new List<Coord>();

        // Check input
        if (searchPositions == null || searchPositions.Length == 0)
        {
            return null;
        }

        // Filter out search positions that are out of board
        var ends = FilterOutOfBoard(searchPositions);
        if (ends.Count == 0) return null;

        // Create queue for BFS
        var q = new Queue<Node>();
        var start = board[startPosition.X, startPosition.Y];
        start.Checked = true;
        start.Pos = startPosition;

        // keep track of the best
        Node best = start;

        // Set root
        q.Enqueue(start);

        // Start BFS
        while (q.Count > 0)
        {
            // Enqueue next node
            var current = q.Dequeue();

            // Check if end is found
            Node end;
            if (CheckIfEnd(ends, current, out end))
            {
                // Remove end node from searched nods
                ends.Remove(end);

                // Add weight to current
                current.Weight++;

                // Mark current as end
                current.IsEnd = true;

                // Check if current is the new best
                if (current.Weight > best.Weight) best = current;
            }

            // Check if we found all searched points
            if (ends.Count == 0)
            {
                // also fill visited positions
                break;
            }

            // Add adjacent nodes
            if (Node.ExistAndNotChecked(current.Up))
            {
                // Mark it, set prev, save direction, inherit weight, calculate pos, enqueue
                current.Up.MarkIt(current, "UP", current.Weight, new Coord(current.Pos.X, current.Pos.Y - 1));
                q.Enqueue(current.Up);
            }

            if (Node.ExistAndNotChecked(current.Right))
            {
                current.Right.MarkIt(current, "RIGHT", current.Weight, new Coord(current.Pos.X + 1, current.Pos.Y));
                q.Enqueue(current.Right);
            }

            if (Node.ExistAndNotChecked(current.Down))
            {
                current.Down.MarkIt(current, "DOWN", current.Weight, new Coord(current.Pos.X, current.Pos.Y + 1));
                q.Enqueue(current.Down);
            }

            if (Node.ExistAndNotChecked(current.Left))
            {
                current.Left.MarkIt(current, "LEFT", current.Weight, new Coord(current.Pos.X - 1, current.Pos.Y));
                q.Enqueue(current.Left);
            }
        }

        // Get path, last coord and visited positions
        LastCoord = best.Pos;
        return Path.GeneratePath(best, ref visitedPos);
    }

    /// <summary>
    /// Finds shortest path to one of items or null.
    /// </summary>
    public string[] FindShortestPath(Coord startPos, Coord[] searchPositions)
    {
        // Make sure that start position is inside board
        if (!startPos.InBoard()) return null;

        // Get start and end node
        var start = board[startPos.X, startPos.Y];

        // Filter out search positions that are out of board
        var ends = new List<Node>();
        foreach (var pos in searchPositions)
        {
            if (pos.InBoard()) ends.Add(board[pos.X, pos.Y]);
        }
        if (ends.Count == 0) return null;

        // Create queue
        var queue = new Queue<Node>();
        start.Checked = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            // Enqueue next node
            var current = queue.Dequeue();

            // Check if end
            foreach (var searchNode in ends)
            {
                if (current == searchNode)
                {
                    return Path.GeneratePath(current);
                }
            }

            // Add adjacent nodes
            if (Node.ExistAndNotChecked(current.Up))
            {
                // Mark it, set prev, save direction, enqueue
                current.Up.MarkIt(current, "UP");
                queue.Enqueue(current.Up);
            }

            if (Node.ExistAndNotChecked(current.Right))
            {
                current.Right.MarkIt(current, "RIGHT");
                queue.Enqueue(current.Right);
            }

            if (Node.ExistAndNotChecked(current.Down))
            {
                current.Down.MarkIt(current, "DOWN");
                queue.Enqueue(current.Down);
            }

            if (Node.ExistAndNotChecked(current.Left))
            {
                current.Left.MarkIt(current, "LEFT");
                queue.Enqueue(current.Left);
            }
        }

        return null;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Checks if current node belongs to end nodes.
    /// </summary>
    public bool CheckIfEnd(List<Node> endNodes, Node current, out Node end)
    {
        foreach (var node in endNodes)
        {
            if (current == node)
            {
                end = current;
                return true;
            }
        }

        end = board[0, 0]; // Just random node, we dont use it
        return false;
    }

    /// <summary>
    /// Reset nodes in board.
    /// </summary>
    public void ResetNodes()
    {
        board.ResetNodes();
    }

    /// <summary>
    /// Filters out positions that are outside board and creates a list of nodes.
    /// </summary>
    public List<Node> FilterOutOfBoard(Coord[] positions)
    {
        var ends = new List<Node>();

        foreach (var pos in positions)
        {
            if (pos.InBoard()) ends.Add(board[pos.X, pos.Y]);
        }

        return ends;
    }

    #endregion
}

public static class PathScenarios
{
    /// <summary>
    /// Goes to the position where with one push could collect one or more items.
    /// </summary>
    public static bool GoToBestPosition(Board board, Coord playerPos, Coord[] itemsPos)
    {
        Console.Error.WriteLine("Trying to go to the best position");

        Coord[] connectedPositions = board.GetConnectedPositionsClosest(playerPos);
        if (connectedPositions.Length > 1)
        {
            // Try to find solution on each position
            foreach (var pos in connectedPositions)
            {
                // Mask for generating pushes that will effect position
                BoxMask mask = BoxMask.GenerateWithBorder(pos);
                // Pushes based on mask
                PushInfo[] pushes = PushInfo.GetPushesWithinBoxMask(mask);

                // Test pushes
                foreach (var push in pushes)
                {
                    if (PushFinder.TestPush(push, board, pos, itemsPos))
                    {
                        Console.Error.WriteLine("Moving to best position...");

                        // We found good push
                        // Go to push effected position
                        var finder = new PathFinder(push.ToBoard(board));
                        var path = finder.FindShortestPath(playerPos, pos);
                        if (path != null && path.Length > 0)
                        {
                            path = Path.FixPathLength(path.ToList());
                            Path.ToOutput(path);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Goes to the position where with one push could collect one or more items, but consider previous path.
    /// </summary>
    public static bool GoToBestPosition(Board board, Coord playerPos, Coord[] itemsPos, string[] prevPath)
    {
        Console.Error.WriteLine("Trying to go to the best position");

        Coord[] connectedPositions = board.GetConnectedPositionsClosest(playerPos);
        if (connectedPositions.Length > 1)
        {
            // Try to find solution on each position
            foreach (var pos in connectedPositions)
            {
                // Mask for generating pushes that will effect position
                BoxMask mask = BoxMask.GenerateWithBorder(pos);
                // Pushes based on mask
                PushInfo[] pushes = PushInfo.GetPushesWithinBoxMask(mask);

                // Test pushes
                foreach (var push in pushes)
                {
                    if (PushFinder.TestPush(push, board, pos, itemsPos))
                    {
                        Console.Error.WriteLine("Moving to best position...");

                        // We found good push
                        // Go to push effected position
                        var finder = new PathFinder(push.ToBoard(board));
                        var newPath = finder.FindShortestPath(playerPos, pos);
                        if (newPath != null && newPath.Length > 0)
                        {
                            Path.ToOutput(Path.MergePaths(prevPath, prevPath));
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}

#endregion

#region Push Finder and Scenarios

/// <summary>
/// Push scenarios. Provides abstraction for PushFinder.
/// </summary>
public static class PushScenarios
{
    #region My item is outside

    /// <summary>
    /// Item is outside, try to insert it.
    /// </summary>
    public static bool ItemIsOutside(Board board, Coord playerPos)
    {
        // Item is outside (-1,-1)
        if (Info.Player.Quests.IsQuestItemOutside() && Rnd.Percent(Rnd.p_DoIt))
        {
            if (PushFinder.ValidInsertBruteForce(board, playerPos)) return true;
        }

        return false;
    }

    /// <summary>
    /// Item is outside, try to insert it.
    /// </summary>
    public static bool ItemIsOutside(Board board, Coord playerPos, BoxMask mask)
    {
        // Item is outside (-1,-1)
        if (Info.Player.Quests.IsQuestItemOutside() && Rnd.Percent(Rnd.p_DoIt))
        {
            if (PushFinder.ValidInsertBruteForce(board, playerPos, mask)) return true;
        }

        return false;
    }

    #endregion

    #region Can solve in one move

    /// <summary>
    /// Is there one push that can help us.
    /// </summary>
    public static bool InOneMove(Board board, Coord playerPos, Coord[] itemPos)
    {
        return PushFinder.ValidPushBruteForce(board, playerPos, itemPos);
    }

    #endregion

    #region Block enemy path

    /// <summary>
    /// Tries to block enemy's path.
    /// </summary>
    public static bool BlockEnemyPath(Board board, Coord enemyPos, Coord[] enemyItemPos)
    {
        if (PushFinder.TestSolution(board, enemyPos, enemyItemPos))
        return PushFinder.InvalidPushBruteForce(board, enemyPos, enemyItemPos);

        return false;
    }

    /// <summary>
    /// Tries to block enemy's path.
    /// </summary>
    public static bool BlockEnemyPath(Board board, Coord enemyPos, Coord[] enemyItemPos, BoxMask mask)
    {
        return PushFinder.InvalidPushBruteForce(board, enemyPos, enemyItemPos, mask);
    }

    #endregion

    #region Block enemy push

    /// <summary>
    /// Tries to block enemy push.
    /// </summary>
    public static bool BlockEnemyPush(Board board, Coord enemyPos, Coord[] enemyItemPos)
    {
        return false;
        return PushFinder.ValidReversedPushBruteForce(board, enemyPos, enemyItemPos);
    }

    /// <summary>
    /// Tries to block enemy push.
    /// </summary>
    public static bool BlockEnemyPush(Board board, Coord enemyPos, Coord[] enemyItemPos, BoxMask mask)
    {
        return PushFinder.ValidReversedPushBruteForce(board, enemyPos, enemyItemPos, mask);
    }

    #endregion

    #region Push item out

    /// <summary>
    /// Tries to push one of items out.
    /// </summary>
    public static bool PushItemOut(Coord[] itemPos)
    {
        if (PushFinder.PushItemOut(itemPos)) return true;
        return false;
    }

    /// <summary>
    /// Tries to push one of items out.
    /// </summary>
    public static bool PushItemOut(Coord[] itemPos, BoxMask mask)
    {
        if (PushFinder.PushItemOut(itemPos, mask)) return true;
        return false;
    }

    /// <summary>
    /// Tries to push one of items out, but only if it can be inserted so that player can collect it.
    /// </summary>
    public static bool PushItemOutValid(Board board, Coord playerPos, Coord[] itemPos)
    {
        return PushFinder.PushItemOutDept_1(board, playerPos, itemPos);
    }
    
    #endregion

    #region Create a better board

    /// <summary>
    /// Push that will create better board.
    /// </summary>
    public static bool BetterBoard(Board board, Coord playerPos, Coord[] endsPos, BoxMask mask)
    {
        if (PushFinder.ValidPushBruteForce(board, playerPos, endsPos, mask)) return true;

        return false;
    }

    #endregion

    #region Random

    /// <summary>
    /// Random push on position.
    /// </summary>
    public static void Random(Coord pos)
    {
        PushFinder.RandomPush(pos);
    }

    /// <summary>
    /// Completely random push.
    /// </summary>
    public static void Random()
    {
        var push = PushInfo.GetRandomPush();
        push.ApplyPush();
    }

    /// <summary>
    /// Try to solve in one or two pushes.
    /// </summary>
    public static bool InOneOrTwoPushes(Board board, Coord playerPos, Coord[] itemPos)
    {
        return PushFinder.ValidPushBruteForceDept_1(board, playerPos, itemPos);
    }

    #endregion
}

/// <summary>
/// Inner push finder. Use PushScenarios instead.
/// </summary>
public static class PushFinder
{
    #region Gen 1 => Random, One Point Path, One Point Insert

    /// <summary>
    /// Gen 1. Test if solution is valid.
    /// </summary>
    public static bool TestSolution(Board board, Coord start, Coord end)
    {
        var finder = new PathFinder(board);
        var path = finder.FindShortestPath(start, end);
        if (path != null)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Valid insert or false.
    /// </summary>
    public static bool ValidInsertBruteForce(Board board, Coord player)
    {
        foreach (var push in PushInfo.GetAllPossiblePushes())
        {
            var insertedPos = push.GetInsertedTilePosition();
            var newBoard = push.ToBoard(board);
            var newPlayerPos = Coord.Fix(player, push.Direction, push.Index, true);
            if (TestSolution(newBoard, newPlayerPos, insertedPos))
            {
                Console.WriteLine(push);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Random push.
    /// </summary>
    public static void RandomPush()
    {
        PushInfo.GetRandomPush().ApplyPush();
    }

    /// <summary>
    /// Random push at position.
    /// </summary>
    public static void RandomPush(Coord pos)
    {
        PushInfo.GetRandomPush(pos).ApplyPush();
    }

    /// <summary>
    /// Finds valid push based on BoxMask.
    /// </summary>
    public static bool RandomPush(BoxMask mask)
    {
        var pushes = PushInfo.GetPushes(mask);
        return Rnd.RandomPush(pushes);
    }

    #endregion

    #region Gen 2 => Multiple End Points, Sorting Good Pushes

    /// <summary>
    /// Gen 2. Test if solution is valid.
    /// </summary>
    public static bool TestSolution(Board board, Coord start, Coord[] ends)
    {
        var finder = new PathFinder(board);
        var path = finder.FindShortestPath(start, ends); // this will find path to only one element

        if (path != null)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets pushes that can get to one or more ends. Count if you dont want all.
    /// </summary>
    public static PushInfo[] GetGoodPushes(Board board, Coord start, Coord[] ends, PushInfo[] pushes, int count = int.MaxValue)
    {
        var good = new List<PushInfo>();

        foreach (var push in pushes)
        {
            var newBoard = push.ToBoard(board);
            var newStart = push.CoordAfter(start, true);
            var newEnds = push.CoordsAfter(ends);
            int counter;

            if (TestSolution(newBoard, newStart, newEnds, out counter))
            {
                push.ItemsCount = counter;
                good.Add(push);
            }
        }

        return good.OrderByDescending(c => c.ItemsCount).Take(count).ToArray(); // order by best
    }
    
    /// <summary>
    /// Tries to find valid push by brute force.
    /// </summary>
    public static bool ValidPushBruteForce(Board board, Coord player, Coord[] ends)
    {
        var allPossible = PushInfo.GetAllPossiblePushes();
        var goodPushes = GetGoodPushes(board, player, ends, allPossible, 5);

        return Rnd.RandomPush(goodPushes.ToArray());
    }

    /// <summary>
    /// Push one position out of board.
    /// </summary>
    public static bool PushItemOut(Coord[] positions)
    {
        var onBorder = Coord.GetOnBorderCoords(positions);
        if (onBorder != null)
        {
            // one item
            if (onBorder.Count == 1)
            {
                if (Rnd.Percent(Rnd.p_WouldBeNice))
                {
                    PushInfo.GetThrowOutsidePush(onBorder[0]).ApplyPush();
                    return true;
                }
                return false;
            }
            
            // more
            int rndIndex = Rnd.Next(onBorder.Count);
            PushInfo.GetThrowOutsidePush(onBorder[rndIndex]).ApplyPush();
            return true;
        }

        return false;
    }

    #endregion

    #region Gen 2.5 => BoxMask, Good Inserts and PushOuts

    /// <summary>
    /// Gets pushes that can get to one or more ends. Count if you dont want all.
    /// </summary>
    public static PushInfo[] GetGoodInserts(Board board, Coord start, PushInfo[] pushes, int count = int.MaxValue)
    {
        var good = new List<PushInfo>();
        int currentCount = 0;

        foreach (var push in pushes)
        {
            var newBoard = push.ToBoard(board);
            var newStart = push.CoordAfter(start, true);
            var end = push.GetInsertedTilePosition();

            if (TestSolution(newBoard, newStart, end))
            {
                currentCount++;
                good.Add(push);
                if (currentCount >= count)
                {
                    break;
                }
            }
        }

        return good.ToArray();
    }

    /// <summary>
    /// Gets pushes that will push at least one position.
    /// </summary>
    public static PushInfo[] GetGoodPushOuts(Coord[] endsPos, PushInfo[] pushes)
    {
        var valid = new List<PushInfo>();

        foreach (var push in pushes)
        {
            if (push.WillItPushOutAny(endsPos))
                valid.Add(push);
        }

        return valid.ToArray();
    }

    /// <summary>
    /// Tries to find valid insert with box mask.
    /// </summary>
    public static bool ValidInsertBruteForce(Board board, Coord start, BoxMask mask)
    {
        var possiblePushes = PushInfo.GetPushes(mask);
        var validPushes = GetGoodInserts(board, start, possiblePushes, 4);

        return Rnd.RandomPush(validPushes);
    }

    /// <summary>
    /// Tries to find valid push based on box mask by brute force.
    /// </summary>
    public static bool ValidPushBruteForce(Board mainBoard, Coord start, Coord[] ends, BoxMask mask)
    {
        // Get pushes
        var possiblePushes = PushInfo.GetPushes(mask);
        var validPushes = GetGoodPushes(mainBoard, start, ends, possiblePushes, 5);

        // Randomize
        return Rnd.RandomPush(validPushes);
    }

    /// <summary>
    /// Push one position out of board based on box mask.
    /// </summary>
    public static bool PushItemOut(Coord[] positions, BoxMask mask)
    {
        // Item must be on border
        var onBorder = Coord.GetOnBorderCoords(positions);
        if (onBorder != null)
        {
            // Get Pushes
            var possiblePushes = PushInfo.GetPushes(mask);
            var validPushes = GetGoodPushOuts(onBorder.ToArray(), possiblePushes);

            // Randomize
            return Rnd.RandomPush(validPushes);
        }

        return false;
    }

    #endregion

    #region Gen 3 => Blocking Pushes

    /// <summary>
    /// Gets pushes that can get to one or more ends. Count if you dont want all.
    /// </summary>
    public static PushInfo[] GetBadPushes(Board board, Coord start, Coord[] ends, PushInfo[] pushes, int count = int.MaxValue)
    {
        var bad = new List<PushInfo>();
        int currentCount = 0;

        foreach (var push in pushes)
        {
            var newBoard = push.ToBoard(board);
            var newStart = push.CoordAfter(start, true);
            var newEnds = push.CoordsAfter(ends);

            if (!TestSolution(newBoard, newStart, newEnds))
            {
                currentCount++;
                bad.Add(push);
                if (currentCount >= count)
                {
                    break;
                }
            }
        }

        return bad.ToArray();
    }

    /// <summary>
    /// Tries to find valid push by brute force.
    /// </summary>
    public static bool InvalidPushBruteForce(Board board, Coord start, Coord[] ends)
    {
        var allPossible = PushInfo.GetAllPossiblePushes();
        var badPushes = GetBadPushes(board, start, ends, allPossible, 5);
        return Rnd.RandomPush(badPushes.ToArray());
    }

    /// <summary>
    /// Random valid but reversed push.
    /// </summary>
    public static bool ValidReversedPushBruteForce(Board board, Coord start, Coord[] ends)
    {
        var allPossible = PushInfo.GetAllPossiblePushes();
        var goodPushes = GetBadPushes(board, start, ends, allPossible, 5);
        foreach (var push in goodPushes)
        {
            push.ReverseDirection();
        }

        return Rnd.RandomPush(goodPushes.ToArray());
    }

    #endregion

    #region Gen 3.5 => Blocking Pushes with BoxMask

    /// <summary>
    /// Tries to find valid push by brute force.
    /// </summary>
    public static bool InvalidPushBruteForce(Board board, Coord start, Coord[] ends, BoxMask mask)
    {
        var allPossible = PushInfo.GetPushes(mask);
        var badPushes = GetBadPushes(board, start, ends, allPossible, 5);

        return Rnd.RandomPush(badPushes.ToArray());
    }

    /// <summary>
    /// Random valid but reversed push.
    /// </summary>
    public static bool ValidReversedPushBruteForce(Board board, Coord start, Coord[] ends, BoxMask mask)
    {
        var allPossible = PushInfo.GetPushes(mask);
        var goodPushes = GetBadPushes(board, start, ends, allPossible, 5);
        foreach (var push in goodPushes)
        {
            push.ReverseDirection();
        }

        return Rnd.RandomPush(goodPushes.ToArray());
    }

    #endregion

    #region Gen 4 => Thinking One Step Ahead

    /// <summary>
    /// Test if solution is valid. Count how many items it collect.
    /// </summary>
    public static bool TestSolution(Board board, Coord start, Coord[] ends, out int count)
    {
        var finder = new PathFinder(board);
        var path = finder.FindBestPath(start, ends, out List<Coord> vp, out Coord cc);

        count = 1;

        if (path != null)
        {
            count = path.Length;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Test if push leads to sulution.
    /// </summary>
    public static bool TestPush(PushInfo push, Board board, Coord pos, Coord[] ends)
    {
        var newBoard = push.ToBoard(board);
        var newStart = push.CoordAfter(pos, true);
        var newEnds = push.CoordsAfter(ends);

        return TestSolution(newBoard, newStart, newEnds);
    }

    /// <summary>
    /// Gets pushes that can get to one or more ends. Count if you dont want all.
    /// </summary>
    public static PushInfo[] GetGoodPushes(Board board, Coord start, Coord[] ends, PushInfo[] pushes, int count, string tile)
    {
        var good = new List<PushInfo>();

        foreach (var push in pushes)
        {
            var newBoard = push.ToBoard(board, tile);
            var newStart = push.CoordAfter(start, true);
            var newEnds = push.CoordsAfter(ends);

            if (TestSolution(newBoard, newStart, newEnds))
            {
                good.Add(push);
            }
        }

        return good.Take(count).ToArray();
    }

    /// <summary>
    /// Pushes item out, but only if item can be inserted so that start position can collect it.
    /// </summary>
    public static bool PushItemOutDept_1(Board board, Coord start, Coord[] ends)
    {
        // Get items on border
        var itemsThatCanBePushedOut = Coord.GetOnBorderCoords(ends);
        if (itemsThatCanBePushedOut == null) return false;

        // Storage valid/good pushes
        var goodPushes = new List<PushInfo>();

        // For each item on border
        foreach (var pos in itemsThatCanBePushedOut)
        {
            // If we pushed item out can we insert it so that player can collect it

            // Get all availble pushes that push item out
            var throwOutsidePushes = PushInfo.GetAllThrowOutsidePushes(pos);

            // Test for each push
            foreach (var push in throwOutsidePushes)
            {
                // how will board look after we push item out
                var tileToInsert = push.GetThrownOutTile(board);
                var boardAfterThrowOut = push.ToBoard(board);
                var playerPosAfterThrowOut = push.CoordAfter(start, true);

                // Get pussible inserts
                var inserts = PushInfo.GetAllPossiblePushes();
                foreach (var insert in inserts)
                {
                    var boardAfterInsert = insert.ToBoard(boardAfterThrowOut, tileToInsert);
                    var playerPosAfterInsert = insert.CoordAfter(playerPosAfterThrowOut, true);
                    var insertedItemPos = insert.GetInsertedTilePosition();

                    // Test solution
                    if (TestSolution(boardAfterInsert, playerPosAfterInsert, insertedItemPos))
                    {
                        // Add as possible solution
                        goodPushes.Add(push);
                        break;
                    }
                }
            }
        }

        // Random push
        return Rnd.RandomPush(goodPushes.ToArray());
    }

    /// <summary>
    /// Find good push in one or two pushes.
    /// </summary>
    public static bool ValidPushBruteForceDept_1(Board board, Coord start, Coord[] ends)
    {
        // Can we solve it with one push
        var allPossible = PushInfo.GetAllPossiblePushes();
        var goodPushes = GetGoodPushes(board, start, ends, allPossible, 3);
        if (goodPushes.Length > 0) return Rnd.RandomPush(goodPushes);


        // For push storage
        var list = new List<PushInfo>();

        // Can we solve it in two pushes
        foreach (var push in allPossible)
        {
            // New board and positions
            var newBoard = push.ToBoard(board);
            var newStart = push.CoordAfter(start);
            var newEnds = push.CoordsAfter(ends);
            var tile = push.GetThrownOutTile(board);

            var goodPushesDept_1 = GetGoodPushes(newBoard, newStart, newEnds, allPossible, 1, tile);
            if (goodPushesDept_1.Length > 0) list.Add(push);

            if (list.Count > 3) break;
        }

        return Rnd.RandomPush(list.ToArray());
    }
    
    #endregion
}

#endregion

#region Dirty Models

/// <summary>
/// Represent collection of quest and info about them.
/// </summary>
public class Quests
{
    #region Init

    Dictionary<string, Quest> allQuests;

    public Quests()
    {
        allQuests = new Dictionary<string, Quest>();
    }

    #endregion

    #region Getters

    /// <summary>
    /// Gets array of active quests.
    /// </summary>
    public Quest[] GetActive()
    {
        return allQuests.Values.Where(q => q.IsActive).ToArray();
    }

    /// <summary>
    /// Gets positions of active quests.
    /// </summary>
    public Coord[] GetActivePositions()
    {
        var result = new List<Coord>();

        foreach (var key in allQuests.Keys)
        {
            // Quest must be active and not on reseted position (-5,-5)
            if (allQuests[key].IsActive && allQuests[key].Pos.X != -5)
                result.Add(allQuests[key].Pos);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Get positions of next possible quests.
    /// </summary>
    public Coord[] GetNextPossibleCoords()
    {
        var list = new List<Coord>();

        foreach (var quest in allQuests.Values)
        {
            // Not active and inside board
            if (!quest.IsActive && !(quest.Pos.X < 0))
            {
                list.Add(quest.Pos);
            }
        }

        return list.ToArray();
    }

    #endregion

    #region Testers

    /// <summary>
    /// Check if we are holding quest item outside of board.
    /// </summary>
    public bool IsQuestItemOutside()
    {
        foreach (var quest in allQuests.Values)
        {
            if (quest.IsActive)
            {
                if (quest.Pos.X == -1 && quest.Pos.Y == -1)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    #endregion

    #region Reset

    /// <summary>
    /// Sets quest item positions to -5,-5.
    /// </summary>
    public void ResetPositions()
    {
        foreach (var quest in allQuests.Values)
        {
            quest.Pos = new Coord(-5, -5);
        }
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates the position.
    /// </summary>
    public void Update(string name, int x, int y)
    {
        allQuests[name] = new Quest(name, x, y);
    }

    /// <summary>
    /// Updates the activity.
    /// </summary>
    public void Update(string name, bool isActive)
    {
        if (allQuests.ContainsKey(name))
        {
            allQuests[name].IsActive = isActive;
        }
    }

    #endregion

    #region Removing

    /// <summary>
    /// Removes the quest.
    /// </summary>
    public void RemoveQuest(string name)
    {
        if (allQuests.ContainsKey(name))
        {
            allQuests.Remove(name);
        }
    }

    /// <summary>
    /// Removes quests based on positions.
    /// </summary>
    public void RemoveQuests(List<Coord> positions)
    {
        foreach (var pos in positions)
        {
            foreach (var quest in allQuests.Values)
            {
                if (quest.Pos.X == pos.X && quest.Pos.Y == pos.Y)
                {
                    allQuests.Remove(quest.Name);
                    break;
                }
            }
        }
    }

    #endregion


}

/// <summary>
///  Represents mask used for filtering pushes.
/// </summary>
public struct BoxMask
{
    #region Init

    public BoxMask(int x, int y)
    {
        HorizontalUpper = y;
        HorizontalLower = y;
        VerticalRight = x;
        VerticalLeft = x;
    }

    public int HorizontalUpper { get; set; }
    public int HorizontalLower { get; set; }
    public int VerticalRight { get; set; }
    public int VerticalLeft { get; set; }

    #endregion

    #region Add Methods

    /// <summary>
    /// Add vertical line (in x).
    /// </summary>
    public void AddVertical(int x)
    {
        if (x > VerticalRight)
        {
            VerticalRight = x;
        }
        else if (x < VerticalLeft)
        {
            VerticalLeft = x;
        }
    }

    /// <summary>
    /// Add horizontal line (in y).
    /// </summary>
    public void AddHorizontal(int y)
    {
        if (y > HorizontalLower)
        {
            HorizontalLower = y;
        }
        else if (y < HorizontalUpper)
        {
            HorizontalUpper = y;
        }
    }

    #endregion

    #region Testing if inside box mask

    /// <summary>
    /// Tests if x position (vertical) is in box mask.
    /// </summary>
    public bool IsInsideVertical(int x)
    {
        if (x >= VerticalLeft && x <= VerticalRight)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Tests if y position (horizontal) is in box mask.
    /// </summary>
    public bool IsInsideHorizontal(int y)
    {
        if (y >= HorizontalUpper && y <= HorizontalLower)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Tests if coord is inside the box mask.
    /// </summary>
    public bool IsInside(Coord position)
    {
        if (IsInsideVertical(position.X) && IsInsideHorizontal(position.Y))
        {
            return true;
        }
        return false;
    }

    #endregion

    #region Size Methods

    /// <summary>
    /// Determines if box mask is at max possible size (Fills board).
    /// </summary>
    public bool IsMaxSize()
    {
        if (HorizontalUpper == 0 && HorizontalLower == (Board.Border)
            && VerticalLeft == 0 && VerticalRight == (Board.Border))
        {
            return true;
        }
        return false;
    }

    #endregion

    #region Generating
    
    /// <summary>
    /// Generate box mask based on staring position and path.
    /// </summary>
    public static BoxMask Generate(Coord start, string[] path)
    {
        var mask = new BoxMask(start.X, start.Y);
        foreach (var direction in path)
        {
            if (direction == "UP")
            {
                mask.AddHorizontal(start.Y--);
            }
            else if (direction == "RIGHT")
            {
                mask.AddVertical(start.X++);
            }
            else if (direction == "DOWN")
            {
                mask.AddHorizontal(start.Y++);
            }
            else // LEFT
            {
                mask.AddVertical(start.X--);
            }
        }

        return mask;
    }

    /// <summary>
    /// Generate box mask based on position with extended border.
    /// </summary>
    public static BoxMask GenerateWithBorder(Coord pos)
    {
        // Initial
        var mask = new BoxMask(pos.X, pos.Y);

        // Horizontal
        if (pos.Y - 1 >= 0) mask.AddHorizontal(pos.Y - 1);
        if (pos.Y + 1 < Board.Size) mask.AddHorizontal(pos.Y + 1);

        // Vertical
        if (pos.X - 1 >= 0) mask.AddVertical(pos.X - 1);
        if (pos.X + 1 < Board.Size) mask.AddVertical(pos.X + 1);

        return mask;
    }

    #endregion
}

/// <summary>
/// Represents all information about push.
/// </summary>
public class PushInfo
{
    #region Init

    public int Index { get; set; }
    public MoveDir Direction { get; set; }

    /// <summary>
    /// How many items could we get after this push.
    /// </summary>
    public int ItemsCount;

    public PushInfo(MoveDir direction, int index)
    {
        Direction = direction;
        Index = index;
    }

    #endregion

    #region Main Methods

    /// <summary>
    /// Applies push on specific position.
    /// </summary>
    public void ApplyPush()
    {
        if (Direction == MoveDir.Right) // y
        {
            Console.WriteLine($"PUSH {Index} RIGHT");
        }
        else if (Direction == MoveDir.Left)
        {
            Console.WriteLine($"PUSH {Index} LEFT");
        }
        else if (Direction == MoveDir.Up)
        {
            Console.WriteLine($"PUSH {Index} UP");
        }
        else // Down
        {
            Console.WriteLine($"PUSH {Index} DOWN");
        }
    }

    /// <summary>
    /// Reverses the direction of push.
    /// </summary>
    public void ReverseDirection()
    {
        Direction = ReversedDirection();
    }

    /// <summary>
    /// Get reversed direction of this push.
    /// </summary>
    public MoveDir ReversedDirection()
    {
        if(Direction == MoveDir.Right) return MoveDir.Left;
        else if (Direction == MoveDir.Left) return MoveDir.Right;
        else if (Direction == MoveDir.Up) return MoveDir.Down;
        else return MoveDir.Up;
    }

    #endregion

    #region Positions

    /// <summary>
    /// Gets coord where tile will be inserted.
    /// </summary>
    public Coord GetInsertedTilePosition()
    {
        if (Direction == MoveDir.Right) return new Coord(0, Index);
        else if (Direction == MoveDir.Left) return new Coord(Board.Border, Index);
        else if (Direction == MoveDir.Up) return new Coord(Index, Board.Border);
        else return new Coord(Index, 0);
    }

    /// <summary>
    /// Gets position of tile that will be thrown out.
    /// </summary>
    public Coord GetThrowOutPosition()
    {
        if (Direction == MoveDir.Right) return new Coord(Board.Border, Index);
        else if (Direction == MoveDir.Left) return new Coord(0, Index);
        else if (Direction == MoveDir.Up) return new Coord(Index, 0);
        else return new Coord(Index, Board.Border);
    }

    /// <summary>
    /// Where will be coord after this move.
    /// </summary>
    public Coord CoordAfter(Coord coord, bool isPlayer = false)
    {
        return Coord.Fix(coord, Direction, Index, isPlayer);
    }

    /// <summary>
    /// Where will be coords after this move.
    /// </summary>
    public Coord[] CoordsAfter(Coord[] coords)
    {
        return Coord.Fix(coords, Direction, Index);
    }

    #endregion

    #region Get Push

    /// <summary>
    /// Get push that will throw pos out of board.
    /// </summary>param>
    /// <returns></returns>
    public static PushInfo GetThrowOutsidePush(Coord pos)
    {
        if (pos.X == 0) return new PushInfo(MoveDir.Left, pos.Y);
        else if (pos.X == Board.Border) return new PushInfo(MoveDir.Right, pos.Y);
        else if (pos.Y == 0) return new PushInfo(MoveDir.Up, pos.X);
        else return new PushInfo(MoveDir.Down, pos.X);
    }

    /// <summary>
    /// Just some random push.
    /// </summary>
    public static PushInfo GetRandomPush()
    {
        var dir = Rnd.GetRandomDirection();
        int index = Rnd.GetRandomIndex();

        return new PushInfo(dir, index);
    }

    /// <summary>
    /// Random push based on position.
    /// </summary>
    public static PushInfo GetRandomPush(Coord pos)
    {
        var dir = Rnd.GetRandomDirection();
        int index;
        if (dir == MoveDir.Right || dir == MoveDir.Left) index = pos.Y;
        else index = pos.X;

        return new PushInfo(dir, index);
    }

    #endregion

    #region Get Collection of Pushes

    /// <summary>
    /// Get all possible insert pushes.
    /// </summary>
    public static PushInfo[] GetAllPossiblePushes()
    {
        var result = new List<PushInfo>();

        for (int index = 0; index < Board.Size; index++)
        {
            result.Add(new PushInfo(MoveDir.Right, index));
            result.Add(new PushInfo(MoveDir.Left, index));
            result.Add(new PushInfo(MoveDir.Up, index));
            result.Add(new PushInfo(MoveDir.Down, index));
        }

        return result.ToArray();
    }

    /// <summary>
    /// Get push that will throw pos out of board.
    /// </summary>param>
    /// <returns></returns>
    public static PushInfo[] GetAllThrowOutsidePushes(Coord pos)
    {
        var list = new List<PushInfo>();

        if (pos.X == 0) list.Add(new PushInfo(MoveDir.Left, pos.Y));
        if (pos.X == Board.Border) list.Add(new PushInfo(MoveDir.Right, pos.Y));
        if (pos.Y == 0) list.Add(new PushInfo(MoveDir.Up, pos.X));
        if (pos.Y == Board.Border) list.Add(new PushInfo(MoveDir.Down, pos.X));

        return list.ToArray();
    }

    /// <summary>
    /// Get all possible pushes that dont interfere with box mask.
    /// </summary>
    public static PushInfo[] GetPushes(BoxMask mask)
    {
        var list = new List<PushInfo>();

        // Right of mask
        if (mask.VerticalRight < Board.Border)
        {
            for (int i = mask.VerticalRight + 1; i < Board.Size; i++)
            {
                list.Add(new PushInfo(MoveDir.Up, i));
                list.Add(new PushInfo(MoveDir.Down, i));
            }
        }

        // Left of mask
        if (mask.VerticalLeft > 0)
        {
            for (int i = 0; i < mask.VerticalLeft; i++)
            {
                list.Add(new PushInfo(MoveDir.Up, i));
                list.Add(new PushInfo(MoveDir.Down, i));
            }
        }

        // Up of mask
        if (mask.HorizontalUpper > 0)
        {
            for (int i = 0; i < mask.HorizontalUpper; i++)
            {
                list.Add(new PushInfo(MoveDir.Right, i));
                list.Add(new PushInfo(MoveDir.Left, i));
            }
        }

        // Down of mask
        if (mask.HorizontalLower < Board.Border)
        {
            for (int i = mask.HorizontalLower + 1; i < Board.Size; i++)
            {
                list.Add(new PushInfo(MoveDir.Right, i));
                list.Add(new PushInfo(MoveDir.Left, i));
            }
        }


        return list.ToArray();
    }

    /// <summary>
    /// Generates pushes that will effect the box mask.
    /// </summary>
    public static PushInfo[] GetPushesWithinBoxMask(BoxMask mask)
    {
        var list = new List<PushInfo>();

        // Up and Down (between vertical lines)
        for (int i = mask.VerticalLeft; i <= mask.VerticalRight; i++)
        {
            list.Add(new PushInfo(MoveDir.Up, i));
            list.Add(new PushInfo(MoveDir.Down, i));
        }

        // Right and Left (between horizontal lines)
        for (int i = mask.HorizontalUpper; i <= mask.HorizontalLower; i++)
        {
            list.Add(new PushInfo(MoveDir.Right, i));
            list.Add(new PushInfo(MoveDir.Left, i));
        }

        return list.ToArray();
    }

    #endregion

    #region Test Push

    /// <summary>
    /// Checks if this push will push out position.
    /// </summary>
    public bool WillItPushOut(Coord coord)
    {
        // Right
        if (Direction == MoveDir.Right)
        {
            if (coord.X == Board.Border) return true;
        }

        // Left
        if (Direction == MoveDir.Left)
        {
            if (coord.X == 0) return true;
        }

        // Up
        if (Direction == MoveDir.Up)
        {
            if (coord.Y == 0) return true;
        }

        // Down
        if (Direction == MoveDir.Down)
        {
            if (coord.Y == Board.Border) return true;
        }

        return false;
    }

    /// <summary>
    /// Check if this push will push out any of positions.
    /// </summary>
    public bool WillItPushOutAny(Coord[] coords)
    {
        foreach (var coord in coords)
        {
            if (WillItPushOut(coord)) return true;
        }

        return false;
    }

    #endregion

    #region Tile

    /// <summary>
    /// Get tile that will be thrown out.
    /// </summary>
    public string GetThrownOutTile(Board board)
    {
        var pos = GetThrowOutPosition();
        return board[pos.X, pos.Y].NodeToTile();
    }

    #endregion

    #region Converters

    /// <summary>
    /// How will board look like after this push.
    /// </summary>
    public Board ToBoard(Board source, string insertTile = null)
    {
        if (Direction == MoveDir.Right) return source.MoveHorizontalRight(Index, insertTile);
        else if (Direction == MoveDir.Left) return source.MoveHorizontalLeft(Index, insertTile);
        else if (Direction == MoveDir.Up) return source.MoveVerticalUp(Index, insertTile);
        else return source.MoveVerticalDown(Index, insertTile);
    }

    /// <summary>
    /// String command for this push.
    /// </summary>
    public override string ToString()
    {
        var builder = new StringBuilder($"PUSH {Index} ");

        if (Direction == MoveDir.Right) builder.Append("RIGHT");
        else if (Direction == MoveDir.Left) builder.Append("LEFT");
        else if (Direction == MoveDir.Up) builder.Append("UP");
        else builder.Append("DOWN");

        return builder.ToString();
    }

    #endregion
}

/// <summary>
/// Info about position (X and Y).
/// </summary>
public struct Coord : IEquatable<Coord>
{
    #region Init

    public Coord(int x, int y) : this()
    {
        X = x;
        Y = y;
    }

    public int X { get; set; }
    public int Y { get; set; }

    #endregion

    #region Is inside board

    /// <summary>
    /// Is position inside board.
    /// </summary>
    public bool InBoard()
    {
        if (X < 0 || X >= Board.Size || Y < 0 || Y >= Board.Size)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Are both position inside board.
    /// </summary>
    public static bool InBoard(Coord pos_1, Coord pos_2)
    {
        return pos_1.InBoard() && pos_2.InBoard();
    }

    /// <summary>
    /// Is position inside board.
    /// </summary>
    public static bool InBoard(int x, int y)
    {
        return new Coord(x, y).InBoard();
    }

    #endregion

    #region Border

    /// <summary>
    /// Check if position is on border
    /// </summary>
    public bool IsOnBorder()
    {
        if (X == 0 || X == Board.Border || Y == 0 || Y == Board.Border)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gives coords on border or null.
    /// </summary>
    public static List<Coord> GetOnBorderCoords(IEnumerable<Coord> collection)
    {
        var list = new List<Coord>();

        foreach (var coord in collection)
        {
            if (coord.IsOnBorder()) list.Add(coord);
        }

        if (list.Count > 0) return list;
        return null;
    }

    #endregion

    #region Math

    /// <summary>
    /// Is coord in collection.
    /// </summary>
    public bool IsIn(Coord[] collection)
    {
        foreach (var collItem in collection)
        {
            if (X == collItem.X && Y == collItem.Y) return true;
        }

        return false;
    }

    /// <summary>
    /// Get coord that only exist in first collections but not in second one.
    /// </summary>
    public static Coord[] Except(Coord[] main, Coord[] except)
    {
        var list = new List<Coord>();

        foreach (var coord in main)
        {
            if (!coord.IsIn(except))
            {
                list.Add(coord);
            }
        }

        return list.ToArray();
    }

    #endregion

    #region Fixing

    /// <summary>
    /// Move it to other side if it is outside.
    /// </summary>
    public static Coord Fix(Coord coord)
    {
        if (coord.X == -1) coord.X = Board.Border;
        if (coord.Y == -1) coord.Y = Board.Border;
        if (coord.X == Board.Size) coord.X = 0;
        if (coord.Y == Board.Size) coord.Y = 0;

        return coord;
    }

    /// <summary>
    /// What will be coord position after move.
    /// </summary>
    public static Coord Fix(Coord coord, MoveDir move, int moveIndex, bool isPlayer = false)
    {
        // Check if coord is outside (-1, -1)
        // This can only happen to item (player is always inside board)
        if (coord.X == -1 && coord.Y == -1)
        {
            if (move == MoveDir.Right)
            {
                // y = move index
                return new Coord(0, moveIndex);
            }
            if (move == MoveDir.Left)
            {
                // y = move index
                return new Coord(Board.Border, moveIndex);
            }
            if (move == MoveDir.Up)
            {
                // x = move index
                return new Coord(moveIndex, Board.Border);
            }
            if (move == MoveDir.Down)
            {
                // x = move index
                return new Coord(moveIndex, 0);
            }
        }

        // Check if move will NOT effect position
        if ((move == MoveDir.Right || move == MoveDir.Left) && coord.Y != moveIndex)
        {
            return coord;
        }
        if ((move == MoveDir.Up || move == MoveDir.Down) && coord.X != moveIndex)
        {
            return coord;
        }

        // Fix position
        Coord result;

        if (move == MoveDir.Right)
        {
            result = new Coord(coord.X + 1, coord.Y);

            // Going out on right
            if (result.X == Board.Size)
            {
                // Player
                if (isPlayer)
                {
                    result.X = 0;
                }
                else // Item
                {
                    result.X = -1;
                    result.Y = -1;
                }
            }
        }
        else if (move == MoveDir.Left)
        {
            result = new Coord(coord.X - 1, coord.Y);

            // Going out on left
            if (result.X == -1)
            {
                // Player
                if (isPlayer)
                {
                    result.X = Board.Border;
                }
                else // Item
                {
                    result.X = -1;
                    result.Y = -1;
                }
            }
        }
        else if (move == MoveDir.Up)
        {
            result = new Coord(coord.X, coord.Y - 1);

            // Going out on top
            if (result.Y == -1)
            {
                // Player
                if (isPlayer)
                {
                    result.Y = Board.Border;
                }
                else // Item
                {
                    result.X = -1;
                    result.Y = -1;
                }
            }
        }
        else // Down
        {
            result = new Coord(coord.X, coord.Y + 1);

            // Going out on top
            if (result.Y == Board.Size)
            {
                // Player
                if (isPlayer)
                {
                    result.Y = 0;
                }
                else // Item
                {
                    result.X = -1;
                    result.Y = -1;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Get positions after move based on move and if it is player or item.
    /// </summary>
    public static Coord[] Fix(Coord[] coords, MoveDir move, int moveIndex, bool isPlayer = false)
    {
        var result = new Coord[coords.Length];

        for (int i = 0; i < coords.Length; i++)
        {
            result[i] = Fix(coords[i], move, moveIndex, isPlayer);
        }

        return result;
    }

    #endregion

    #region Nearby

    /// <summary>
    /// Nearby coords algorithm. NOT GOOD.
    /// </summary>
    public Coord[] GetNearbyCoords()
    {
        // Check if coord is outside of board
        if (!InBoard())
        {
            return new Coord[0];
        }

        var list = new List<Coord>();
        var visited = new bool[Board.Size, Board.Size];
        var queue = new Queue<Coord>();

        queue.Enqueue(this);
        visited[X, Y] = true;

        while (queue.Count > 0)
        {
            // Add coord to list in perfect order :-P
            var current = queue.Dequeue();
            list.Add(current);

            // Right Up (diagonal)
            var adjCoord = Coord.Fix(new Coord(current.X + 1, current.Y - 1));
            if (!visited[adjCoord.X, adjCoord.Y])
            {
                // Add to queue and mark as visited
                queue.Enqueue(adjCoord);
                visited[adjCoord.X, adjCoord.Y] = true;
            }
            // Left Up (diagonal)
            adjCoord = Coord.Fix(new Coord(current.X - 1, current.Y - 1));
            if (!visited[adjCoord.X, adjCoord.Y])
            {
                // Add to queue and mark as visited
                queue.Enqueue(adjCoord);
                visited[adjCoord.X, adjCoord.Y] = true;
            }
            // Right Down (diagonal)
            adjCoord = Coord.Fix(new Coord(current.X + 1, current.Y + 1));
            if (!visited[adjCoord.X, adjCoord.Y])
            {
                // Add to queue and mark as visited
                queue.Enqueue(adjCoord);
                visited[adjCoord.X, adjCoord.Y] = true;
            }
            // Left Down (diagonal)
            adjCoord = Coord.Fix(new Coord(current.X - 1, current.Y + 1));
            if (!visited[adjCoord.X, adjCoord.Y])
            {
                // Add to queue and mark as visited
                queue.Enqueue(adjCoord);
                visited[adjCoord.X, adjCoord.Y] = true;
            }
            // Right
            adjCoord = Coord.Fix(new Coord(current.X + 1, current.Y));
            if (!visited[adjCoord.X, adjCoord.Y])
            {
                // Add to queue and mark as visited
                queue.Enqueue(adjCoord);
                visited[adjCoord.X, adjCoord.Y] = true;
            }
            // Down
            adjCoord = Coord.Fix(new Coord(current.X, current.Y + 1));
            if (!visited[adjCoord.X, adjCoord.Y])
            {
                // Add to queue and mark as visited
                queue.Enqueue(adjCoord);
                visited[adjCoord.X, adjCoord.Y] = true;
            }
            // Left
            adjCoord = Coord.Fix(new Coord(current.X - 1, current.Y));
            if (!visited[adjCoord.X, adjCoord.Y])
            {
                // Add to queue and mark as visited
                queue.Enqueue(adjCoord);
                visited[adjCoord.X, adjCoord.Y] = true;
            }
            // Up
            adjCoord = Coord.Fix(new Coord(current.X, current.Y - 1));
            if (!visited[adjCoord.X, adjCoord.Y])
            {
                // Add to queue and mark as visited
                queue.Enqueue(adjCoord);
                visited[adjCoord.X, adjCoord.Y] = true;
            }
        }

        return list.ToArray();
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
        return $"X = {X}, Y = {Y}";
    }

    public bool Equals(Coord other)
    {
        if (X == other.X && Y == other.Y) return true;
        return false;
    }

    #endregion
}

/// <summary>
/// Represents info about board and its nodes.
/// </summary>
public class Board
{
    #region Init

    public const int Size = 7;

    public const int Border = 6;

    public Board(string[,] directions)
    {
        // Save directions=
        this.directions = directions;

        // Set up nodes
        SetUpNodes();

        // Set nodes neighbors
        SetNodesAdjacents();
    }

    public Node[,] Nodes { get; private set; }

    string[,] directions;

    public Node this[int x, int y]
    {
        get { return Nodes[x, y]; }
    }

    #endregion

    #region Set-Up Methods

    /// <summary>
    /// Creates matrix of nodes.
    /// </summary>
    void SetUpNodes()
    {
        // Create placeholder and save directions
        Nodes = new Node[7, 7];

        // Create all nodes
        for (int i = 0; i < 7; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                Nodes[i, j] = new Node();
            }
        }
    }

    /// <summary>
    /// Sets up ajacents nodes for every node.
    /// </summary>
    void SetNodesAdjacents()
    {
        // for each node
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                // get path directions
                var direction = directions[x, y];

                // Up
                if (direction[0] == '1' && Validate(y - 1))
                {
                    // Does upper node allows moving down
                    if (directions[x, y - 1][2] == '1')
                    {
                        Nodes[x, y].Up = Nodes[x, y - 1];
                    }
                }
                // Right
                if (direction[1] == '1' && Validate(x + 1))
                {
                    // Does right node allows moving left
                    if (directions[x + 1, y][3] == '1')
                    {
                        Nodes[x, y].Right = Nodes[x + 1, y];
                    }
                }
                // Down
                if (direction[2] == '1' && Validate(y + 1))
                {
                    // Does lower node allows moving up
                    if (directions[x, y + 1][0] == '1')
                        Nodes[x, y].Down = Nodes[x, y + 1];
                }
                // Left
                if (direction[3] == '1' && Validate(x - 1))
                {
                    // Does left node allows moving right
                    if (directions[x - 1, y][1] == '1')
                        Nodes[x, y].Left = Nodes[x - 1, y];
                }
            }
        }
    }

    /// <summary>
    /// Checks if value is in bondaries.
    /// </summary>
    bool Validate(int value)
    {
        if (value < 0 || value >= Size)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Reset nodes data for new path search.
    /// </summary>
    public void ResetNodes()
    {
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                Nodes[i, j].Checked = false;
                Nodes[i, j].Direction = null;
                Nodes[i, j].PrevNode = null;
                Nodes[i, j].Weight = 0;
                Nodes[i, j].IsEnd = false;
            }
        }
    }

    #endregion

    #region Transformations

    /// <summary>
    /// Apply horizontal move to directions.
    /// </summary>
    public static string[,] MoveHorizontal(string[,] originalDirections, int index, bool right, string newTile)
    {
        var result = new string[Size, Size];

        // Copy all values
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                result[x, y] = originalDirections[x, y];
            }
        }

        // Set new node and copy rest of row
        if (right)
        {
            result[0, index] = newTile;
            for (int i = 1; i < Size; i++)
            {
                result[i, index] = originalDirections[i - 1, index];
            }
        }
        else // to the left
        {
            result[Size - 1, index] = newTile;
            for (int i = 0; i < Size - 1; i++)
            {
                result[i, index] = originalDirections[i + 1, index];
            }
        }

        return result;
    }

    /// <summary>
    /// Apply vertical move to directions.
    /// </summary>
    public static string[,] MoveVertical(string[,] originalDirections, int index, bool down, string newTile)
    {
        var result = new string[Size, Size];

        // Copy all values
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                result[x, y] = originalDirections[x, y];
            }
        }

        // Set new node and copy rest
        if (down)
        {
            result[index, 0] = newTile;
            for (int i = 1; i < Size - 1; i++)
            {
                result[index, i] = originalDirections[index, i - 1];
            }
        }
        else // Up
        {
            result[index, Size - 1] = newTile;
            for (int i = 0; i < Size - 1; i++)
            {
                result[index, i] = originalDirections[index, i + 1];
            }
        }

        return result;
    }
    
    #endregion

    #region Board Generators

    public Board MoveHorizontalRight(int index, string insertTile = null)
    {
        if (insertTile == null)
            insertTile = Info.Player.Tile;

        var dirs = MoveHorizontal(directions, index, true, insertTile);
        return new Board(dirs);
    }

    public Board MoveHorizontalLeft(int index, string insertTile = null)
    {
        if (insertTile == null)
            insertTile = Info.Player.Tile;

        var dirs = MoveHorizontal(directions, index, false, insertTile);
        return new Board(dirs);
    }

    public Board MoveVerticalDown(int index, string insertTile = null)
    {
        if (insertTile == null)
            insertTile = Info.Player.Tile;

        var dirs = MoveVertical(directions, index, true, insertTile);
        return new Board(dirs);
    }

    public Board MoveVerticalUp(int index, string insertTile = null)
    {
        if (insertTile == null)
            insertTile = Info.Player.Tile;

        var dirs = MoveVertical(directions, index, false, insertTile);
        return new Board(dirs);
    }

    #endregion

    #region Positions

    /// <summary>
    /// Get all connected positions to start pos. Closest one will be first.
    /// </summary>
    public Coord[] GetConnectedPositionsClosest(Coord startPos)
    {
        ResetNodes();

        var positions = new List<Coord>();
        var start = Nodes[startPos.X, startPos.Y];
        start.Pos = startPos;

        // Create queue
        var queue = new Queue<Node>();
        start.Checked = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            // Enqueue next node
            var current = queue.Dequeue();
            positions.Add(current.Pos);

            // Add adjacent nodes
            if (Node.ExistAndNotChecked(current.Up))
            {
                // Mark it with position
                current.Up.MarkIt(new Coord(current.Pos.X, current.Pos.Y - 1));
                queue.Enqueue(current.Up);
            }

            if (Node.ExistAndNotChecked(current.Right))
            {
                current.Right.MarkIt(new Coord(current.Pos.X + 1, current.Pos.Y));
                queue.Enqueue(current.Right);
            }

            if (Node.ExistAndNotChecked(current.Down))
            {
                current.Down.MarkIt(new Coord(current.Pos.X, current.Pos.Y + 1));
                queue.Enqueue(current.Down);
            }

            if (Node.ExistAndNotChecked(current.Left))
            {
                current.Left.MarkIt(new Coord(current.Pos.X - 1, current.Pos.Y));
                queue.Enqueue(current.Left);
            }
        }

        return positions.ToArray();
    }

    #endregion
}

#endregion

#region Math and Randomness

/// <summary>
/// Randomness generator.
/// </summary>
public static class Rnd
{
    #region Percent Constants

    /// <summary>
    /// 90 percent.
    /// </summary>
    public const int p_DoIt = 90;
    /// <summary>
    /// 80 percent.
    /// </summary>
    public const int p_WouldBeNice = 80;
    /// <summary>
    /// 70 percent.
    /// </summary>
    public const int p_YeahSure = 70;
    /// <summary>
    /// 60 percent.
    /// </summary>
    public const int p_GoFor60 = 60;
    /// <summary>
    /// 30 percent.
    /// </summary>
    public const int p_NotReally = 30;
    /// <summary>
    /// 20 percent.
    /// </summary>
    public const int p_Dont = 20;

    #endregion

    #region Init
    
    static Random rnd = new Random();

    #endregion

    #region Public Methods

    /// <summary>
    /// Test odds with percent.
    /// </summary>
    public static bool Percent(int percent)
    {
        int value = rnd.Next(101);
        if (value <= percent) return true;
        return false;
    }

    /// <summary>
    /// Gets random direction.
    /// </summary>
    public static MoveDir GetRandomDirection()
    {
        int dirValue = rnd.Next(0, 4); // 4 because there are 4 possible directions
        return (MoveDir)dirValue;
    }

    /// <summary>
    /// Random push or none. If none then returns false.
    /// </summary>
    public static bool RandomPush(PushInfo[] validPushes)
    {
        // Two opossite pushes
        if (validPushes.Length == 2)
        {
            if (validPushes[0].Index == validPushes[1].Index
                && validPushes[0].Direction == validPushes[1].ReversedDirection())
            {
                if (Percent(p_NotReally)) return false;
            }
        }

        if (validPushes.Length >= 2) // at least 2
        {
            int rndIndex = Next(0, validPushes.Length - 1);
            validPushes[rndIndex].ApplyPush();
            return true;
        }
        if (validPushes.Length == 1 && Percent(p_WouldBeNice)) // only one
        {
            validPushes[0].ApplyPush();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets random index from 0 to Board.Border.
    /// </summary>
    public static int GetRandomIndex()
    {
        return rnd.Next(Board.Size);
    }

    /// <summary>
    /// Gets random value between min and max including.
    /// </summary>
    public static int Next(int min, int max)
    {
        return rnd.Next(min, max + 1);
    }

    /// <summary>
    /// Random from 0 to max excluding.
    /// </summary>
    public static int Next(int max)
    {
        return rnd.Next(max);
    }

    #endregion
}

#endregion

#region Clean Models

/// <summary>
/// Infomations about node. Used in pathfinding.
/// </summary>
public class Node
{
    #region Init

    public Node Up { get; set; }
    public Node Down { get; set; }
    public Node Right { get; set; }
    public Node Left { get; set; }

    public Coord Pos { get; set; }

    public Node PrevNode { get; set; }
    public string Direction { get; set; }

    public bool Checked { get; set; }
    public bool IsEnd { get; set; }
    public int Weight { get; set; }

    #endregion

    #region Validation

    /// <summary>
    /// Does node exist and is is not checked?
    /// </summary>
    /// <returns></returns>
    public static bool ExistAndNotChecked(Node node)
    {
        if (node != null && !node.Checked) return true;
        return false;
    }

    #endregion

    #region Marking

    /// <summary>
    /// Marks node and sets prev node and direction.
    /// </summary>
    public void MarkIt(Node prevNode, string direction)
    {
        Checked = true;
        PrevNode = prevNode;
        Direction = direction;
    }

    /// <summary>
    /// Marks node with all possible info.
    /// </summary>
    public void MarkIt(Node prevNode, string direction, int weight, Coord pos)
    {
        MarkIt(prevNode, direction);
        Weight = weight;
        Pos = pos;
    }

    /// <summary>
    /// Marks node with position.
    /// </summary>
    public void MarkIt(Coord pos)
    {
        Checked = true;
        Pos = pos;
    }

    #endregion

    #region Convert

    /// <summary>
    /// Convert node to string directions (eg. 1111).
    /// </summary>
    public string NodeToTile()
    {
        var builder = new StringBuilder(4);

        if (Up != null) builder.Append('1');
        else builder.Append('0');

        if (Right != null) builder.Append('1');
        else builder.Append('0');

        if (Down != null) builder.Append('1');
        else builder.Append('0');

        if (Left != null) builder.Append('1');
        else builder.Append('0');

        return builder.ToString();
    }

    #endregion
}

/// <summary>
/// Holds infomations about player.
/// </summary>
public class PlayerInfo
{
    public Coord Position { get; set; }
    public Quests Quests { get; set; }
    public int NumOfQuests { get; set; }
    public string Tile { get; set; }

    public PlayerInfo()
    {
        Quests = new Quests();
        Tile = "0000";
    }
}

/// <summary>
/// Holds informations about quest.
/// </summary>
public class Quest
{
    public Quest(string name, int x, int y)
    {
        Name = name;
        Pos = new Coord(x, y);
    }

    public string Name { get; private set; }
    public Coord Pos { get; set; }
    public bool IsActive { get; set; }
}

#endregion

#region Game Info

/// <summary>
/// Holds infomations about players.
/// </summary>
public static class Info
{
    public static PlayerInfo Player = new PlayerInfo();
    public static PlayerInfo Enemy = new PlayerInfo();
}

#endregion