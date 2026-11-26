using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct MazeWall
{
    public Vector2Int facingDirection;

    public Vector2Int? backConnectedWallCoord;
    public Vector2Int? frontConnectedWallCoord;
}

public class DungeonGenerator : NetworkBehaviour
{
    [Header("Wall Types")]
    public GameObject wall;
    public GameObject wall_corner;
    public GameObject wall_gap;
    public GameObject wall_crawlspace;

    [Header("Dungeon Generation")]
    public int seed;
    public bool randomizeSeed = true;

    private System.Random mapGen;

    [Space]
    public int minInitialStartPoints = 10;
    public int maxInitialStartPoints = 20;

    [Header("Branches")]
    public int minBranchLength = 4;
    public int maxBranchLength = 10;

    [Space]

    [Range(0, 100)]
    public int turnChance = 30;

    [Range(0f, 1f)]
    public float gapChance = 0.1f;
    public int minGapsPerBranch = 2;
    public int maxGapsPerBranch = 5;

    [Header("Grid")]
    public Transform dungeonCenterPT;
    public float tileSize = 2f;

    public int gridX = 25;
    public int gridY = 30;

    public int wallsBoundaryX = 1;
    public int wallsBoundaryY = 1;

    private int gridXWalls;
    private int gridYWalls;

    private int currentSpawnedTiles;

    [Header("Current Generation")]
    private List<Vector2Int> freeMazeCoords = new List<Vector2Int>();
    private Dictionary<Vector2Int, MazeWall> usedMazeCoords = new();



    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeSeed();
        GenerateMap();
    }

    private void InitializeSeed()
    {
        if (randomizeSeed)
            seed = Random.Range(1000000, 10000000);

        mapGen = new System.Random(seed);
    }

    private void GenerateMap()
    {
        gridXWalls = gridX / (int)tileSize;
        gridYWalls = gridY / (int)tileSize;   

        usedMazeCoords.Clear();
        currentSpawnedTiles = 0;

        InitializeFreeMazeCoords();
        GenerateWalls();
        GenerateCorners();
    }

    private void InitializeFreeMazeCoords()
    {
        for (int x = -gridXWalls + wallsBoundaryX; x < gridXWalls - wallsBoundaryX; x++)
        {
            for (int y = -gridYWalls + wallsBoundaryY; y < gridYWalls - wallsBoundaryY; y++)
            {
                freeMazeCoords.Add(new Vector2Int(x, y));
            }
        }
    }

    // =========================================================
    // GENERATE STARTING WALLS
    // =========================================================

    private void GenerateWalls()
    {
        int startingPoints = mapGen.Next(
            minInitialStartPoints,
            maxInitialStartPoints + 1
        );

        int created = 0;

        for (int i = 0; i < startingPoints && created < startingPoints; i++)
        {
            int freeSpaceIndex = mapGen.Next(0, freeMazeCoords.Count - 1);

            Vector2Int startCoord = freeMazeCoords[freeSpaceIndex];
            freeMazeCoords.RemoveAt(freeSpaceIndex);

            if (!IsValidWallSpawnPT(startCoord))
                continue;

            MazeWall startWall = new MazeWall();

            startWall.backConnectedWallCoord = null;
            startWall.frontConnectedWallCoord = null;

            usedMazeCoords.Add(startCoord, startWall);

            GenerateWallBranch(startCoord);

            created++;
        }
    }

    // =========================================================
    // GENERATE ONE CONTINUOUS BRANCH
    // =========================================================

    private void GenerateWallBranch(Vector2Int startCoord)
    {
        int branchLength = mapGen.Next(
            minBranchLength,
            maxBranchLength + 1
        );

        Vector2Int curr_coord = startCoord;
        Vector2Int curr_direction = GetRandomWallDirection();

        bool spawnedStartWall = false;

        List<GameObject> spawnedWalls = new List<GameObject>();

        for (int i = 0; i < branchLength; i++)
        {
            // Find a valid next direction.
            if (!GetNewWallDirection(
                curr_coord,
                curr_direction,
                out Vector2Int newDirection))
            {
                break;
            }



            curr_direction = newDirection;

            Vector2Int new_coord =
                curr_coord + curr_direction;


            // -----------------------------------------
            // FIRST WALL
            // -----------------------------------------

            if (!spawnedStartWall)
            {
                MazeWall startWall =
                    usedMazeCoords[startCoord];

                startWall.facingDirection =
                    curr_direction;

                usedMazeCoords[startCoord] =
                    startWall;

                SpawnWall(startCoord, curr_direction, ref spawnedWalls);

                currentSpawnedTiles++;
                spawnedStartWall = true;
            }


            // -----------------------------------------
            // CURRENT WALL -> FRONT WALL
            // -----------------------------------------

            MazeWall currentWall =
                usedMazeCoords[curr_coord];

            currentWall.frontConnectedWallCoord =
                new_coord;

            usedMazeCoords[curr_coord] =
                currentWall;


            // -----------------------------------------
            // CREATE NEW WALL
            // -----------------------------------------

            MazeWall newWall = new MazeWall();

            newWall.facingDirection =
                curr_direction;

            newWall.backConnectedWallCoord =
                curr_coord;

            newWall.frontConnectedWallCoord =
                null;

            usedMazeCoords.Add(
                new_coord,
                newWall
            );


            SpawnWall(new_coord, curr_direction, ref spawnedWalls);

            currentSpawnedTiles++;


            // Move forward.
            curr_coord = new_coord;
        }

        // If this start point could not generate anything,
        // remove it so it doesn't count as an invisible wall.
        if (!spawnedStartWall)
        {
            usedMazeCoords.Remove(startCoord);
        }

        SpawnGaps(spawnedWalls);
    }

    // =========================================================
    // DIRECTIONS
    // =========================================================

    private Vector2Int GetRandomWallDirection()
    {
        int direction = mapGen.Next(0, 4);

        if (direction == 0)
            return Vector2Int.up;

        if (direction == 1)
            return Vector2Int.right;

        if (direction == 2)
            return Vector2Int.down;

        return Vector2Int.left;
    }

    private bool GetNewWallDirection(Vector2Int wallCoord, Vector2Int wallDirection, out Vector2Int chosenDirection)
    {
        Vector2Int leftDirection =
            GetLeftDirection(wallDirection);

        Vector2Int rightDirection =
            GetRightDirection(wallDirection);


        // Randomize which side is preferred.
        if (mapGen.Next(0, 2) == 0)
        {
            Vector2Int temp = leftDirection;

            leftDirection = rightDirection;
            rightDirection = temp;
        }


        bool shouldTurn =
            mapGen.Next(0, 100) < turnChance;


        Vector2Int[] directions;

        if (shouldTurn)
        {
            directions = new Vector2Int[]
            {
                leftDirection,
                rightDirection,
                wallDirection
            };
        }
        else
        {
            directions = new Vector2Int[]
            {
                wallDirection,
                leftDirection,
                rightDirection
            };
        }


        foreach (Vector2Int direction in directions)
        {
            Vector2Int newCoord =
                wallCoord + direction;

            if (IsValidWallSpawnPT(
                newCoord,
                wallCoord))
            {
                chosenDirection = direction;
                return true;
            }
        }


        chosenDirection = Vector2Int.zero;
        return false;
    }

    private Vector2Int GetRightDirection(Vector2Int direction)
    {
        return new Vector2Int(
            direction.y,
            -direction.x
        );
    }

    private Vector2Int GetLeftDirection(Vector2Int direction)
    {
        return new Vector2Int(
            -direction.y,
            direction.x
        );
    }

    // =========================================================
    // WALL VALIDATION
    // =========================================================

    private bool IsValidWallSpawnPT(Vector2Int wallCoord, Vector2Int? previousWallCoord = null)
    {
        bool withinBounds =
            wallCoord.x <= gridXWalls - wallsBoundaryX &&
            wallCoord.x >= -gridXWalls + wallsBoundaryX &&
            wallCoord.y <= gridYWalls - wallsBoundaryY &&
            wallCoord.y >= -gridYWalls + wallsBoundaryY;


        if (!withinBounds)
            return false;


        // Cannot spawn directly on another wall.
        if (usedMazeCoords.ContainsKey(wallCoord))
            return false;


        // Check all surrounding coordinates.
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;


                Vector2Int nearbyCoord =
                    wallCoord +
                    new Vector2Int(x, y);


                if (!usedMazeCoords.ContainsKey(nearbyCoord))
                    continue;


                if (previousWallCoord.HasValue)
                {
                    // Ignore the wall we're extending from.
                    if (nearbyCoord ==
                        previousWallCoord.Value)
                    {
                        continue;
                    }


                    // Ignore the wall behind it.
                    // This allows a normal 90 degree turn.
                    MazeWall previousWall = usedMazeCoords[previousWallCoord.Value];

                    if (previousWall.backConnectedWallCoord.HasValue)
                    {
                        if (nearbyCoord ==
                            previousWall.backConnectedWallCoord.Value)
                        {
                            continue;
                        }
                    }
                }


                // Any other wall is too close.
                return false;
            }
        }


        return true;
    }

    // =========================================================
    // STEP 5: SPAWN GAPS & SPAWN WALLS
    // =========================================================

 
    private void SpawnWall(Vector2Int wallCoord, Vector2Int faceDirection, ref List<GameObject> spawnedWalls)
    {
        Vector3 wallPosition =
            dungeonCenterPT.position +
            new Vector3(
                wallCoord.x * tileSize,
                0f,
                wallCoord.y * tileSize
            );


        Quaternion wallRotation =
            Quaternion.identity;


        if (faceDirection == Vector2Int.up)
            wallRotation =
                Quaternion.Euler(0f, 90f, 0f);

        else if (faceDirection == Vector2Int.right)
            wallRotation =
                Quaternion.Euler(0f, 180f, 0f);

        else if (faceDirection == Vector2Int.down)
            wallRotation =
                Quaternion.Euler(0f, 270f, 0f);

        else if (faceDirection == Vector2Int.left)
            wallRotation =
                Quaternion.Euler(0f, 0f, 0f);



        GameObject temp_wall = Instantiate(wall, wallPosition, wallRotation);
        spawnedWalls.Add(temp_wall);
    }

    private void SpawnGaps(List<GameObject> wallBranches)
    {
        int gapsToSpawn = mapGen.Next(minGapsPerBranch, maxGapsPerBranch);
        gapsToSpawn = gapsToSpawn > wallBranches.Count ? wallBranches.Count : gapsToSpawn;

        for (int i = 0; i < gapsToSpawn; i++)
        {
            GameObject temp_wall = wallBranches[mapGen.Next(0, wallBranches.Count)];
            wallBranches.Remove(temp_wall);

            Vector3 newWallPos = temp_wall.transform.position;
            Quaternion newWallRot = temp_wall.transform.rotation;

            Destroy(temp_wall);

            Debug.Log("TRY SPAWN SERVER GAP");

            if (NetworkServer.active)
            {
                Debug.Log("SPAWN SERVER GAP");
                ServerSpawnGap(newWallPos, newWallRot);
            }
        }     
    }

    [Server]
    private void ServerSpawnGap(Vector3 newWallPos, Quaternion newWallRot)
    {
        GameObject gapToSpawn = mapGen.Next(0, 2) == 0 ? wall_gap : wall_crawlspace;

        GameObject spawnedGap = Instantiate(gapToSpawn, newWallPos, newWallRot);
        NetworkServer.Spawn(spawnedGap);
    }


    // =========================================================
    // STEP 6: GENERATE CORNERS
    // =========================================================

    private void GenerateCorners()
    {
        foreach (var wallData in usedMazeCoords)
        {
            Vector2Int currentCoord = wallData.Key;

            MazeWall currentWall = wallData.Value;


            // -----------------------------------------
            // START OF BRANCH
            // -----------------------------------------

            if (!currentWall.backConnectedWallCoord.HasValue)
            {
                Vector2Int endCoord = currentCoord - currentWall.facingDirection;
                SpawnCorner(endCoord);
                continue;
            }


            // -----------------------------------------
            // END OF BRANCH
            // -----------------------------------------

            if (!currentWall.frontConnectedWallCoord.HasValue)
            {
                
                SpawnCorner(currentCoord);

                continue;
            }


            // -----------------------------------------
            // MIDDLE OF BRANCH / TURN
            // -----------------------------------------

            Vector2Int frontCoord = currentWall.frontConnectedWallCoord.Value;

            MazeWall frontWall = usedMazeCoords[frontCoord];


            if (currentWall.facingDirection != frontWall.facingDirection)
            {
                SpawnCorner(currentCoord);
            }
        }
    }

    // =========================================================
    // STEP 7: SPAWN CORNER
    // =========================================================

    private void SpawnCorner(
        Vector2Int cornerCoord)
    {
        Vector3 cornerPosition =
            dungeonCenterPT.position +
            new Vector3(
                cornerCoord.x * tileSize,
                0f,
                cornerCoord.y * tileSize
            );


        Instantiate(
            wall_corner,
            cornerPosition,
            Quaternion.identity
        );
    }
}