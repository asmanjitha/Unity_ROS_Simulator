using UnityEngine;
using System.Collections.Generic;

public class MazeGenerator : MonoBehaviour
{
    public Vector3 cubeSize = new Vector3(2f, 0.1f, 2f); // Cube size (width, height, depth)
    public int mazeWidth = 11;  // Maze max width (Must be an ODD number)
    public int mazeHeight = 11; // Maze max height (Must be an ODD number)
    public GameObject wallPrefab; // Assign a cube prefab in Unity

    private int[,] maze; // Maze grid: 0 = path, 1 = wall
    private Vector2Int entrance;
    private Vector2Int exit;

    private static readonly Vector2Int[] directions = 
    {
        new Vector2Int(0, 2), new Vector2Int(2, 0), new Vector2Int(0, -2), new Vector2Int(-2, 0)
    };

    void Start()
    {
        GenerateMaze();
    }

    public void GenerateMaze()
    {
        if (mazeWidth % 2 == 0) mazeWidth -= 1;  // Ensure odd size
        if (mazeHeight % 2 == 0) mazeHeight -= 1;

        maze = new int[mazeWidth, mazeHeight];
        
        // Initialize maze with walls
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                maze[x, y] = 1; // Set all to walls
            }
        }

        // Select a random starting point (must be odd indices)
        int startX = Random.Range(1, mazeWidth - 1);
        int startY = Random.Range(1, mazeHeight - 1);
        startX = (startX % 2 == 0) ? startX + 1 : startX;
        startY = (startY % 2 == 0) ? startY + 1 : startY;

        // Depth-First Search Maze Generation
        CarveMaze(startX, startY);

        // Set Entrance and Exit
        SetEntranceAndExit();

        // Instantiate Maze
        BuildMaze();
    }

    private void CarveMaze(int x, int y)
    {
        maze[x, y] = 0; // Mark as a path

        List<Vector2Int> shuffledDirections = new List<Vector2Int>(directions);
        Shuffle(shuffledDirections);

        foreach (Vector2Int dir in shuffledDirections)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            if (IsInBounds(nx, ny) && maze[nx, ny] == 1)
            {
                int wallX = x + dir.x / 2;
                int wallY = y + dir.y / 2;

                maze[wallX, wallY] = 0; // Remove wall
                CarveMaze(nx, ny);
            }
        }
    }

    private void SetEntranceAndExit()
    {
        entrance = new Vector2Int(0, 1); // Always at (0, 1)
        exit = new Vector2Int(mazeWidth - 1, mazeHeight - 2); // Exit at (maxWidth-1, maxHeight-2)

        maze[entrance.x, entrance.y] = 0;
        maze[exit.x, exit.y] = 0;
    }

    private void BuildMaze()
    {
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                if (maze[x, y] == 1)
                {
                    Vector3 position = new Vector3(x * cubeSize.x, cubeSize.y / 2, y * cubeSize.z);
                    Instantiate(wallPrefab, position, Quaternion.identity, gameObject.transform);
                }
            }
        }
    }

    private bool IsInBounds(int x, int y)
    {
        return x > 0 && y > 0 && x < mazeWidth - 1 && y < mazeHeight - 1;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
