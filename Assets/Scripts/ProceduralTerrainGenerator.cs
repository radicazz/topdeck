using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralTerrainGenerator : MonoBehaviour
{
    [Header("Grid")]
    [Min(5)] public int width = 25;
    [Min(5)] public int height = 25;
    [Min(0.5f)] public float cellSize = 1f;

    [Header("Random")]
    public bool randomizeSeed = true;
    public int seed = 0;
    [Min(1)] public int maxWfcRetries = 8;

    [Header("Paths")]
    [Min(3)] public int pathCount = 3;
    public float pathOverlayHeight = 0.02f;

    [Header("Heights")]
    public float grassHeight = 0f;
    public float dirtHeight = 0.03f;
    public float rockHeight = 0.2f;

    [Header("Materials")]
    public Material groundMaterial;
    public Material pathMaterial;

    public IReadOnlyList<List<Vector3>> PathsWorld => pathsWorld;
    public Vector3 CenterWorld => transform.position;
    public int LastSeedUsed => lastSeedUsed;

    private readonly List<List<Vector3>> pathsWorld = new List<List<Vector3>>();
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private GameObject pathOverlay;
    private int lastSeedUsed;

    private enum TileType
    {
        Grass,
        Dirt,
        Rock,
        Path
    }

    private struct TileDef
    {
        public float height;
        public int allowedNeighbors;
    }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        Generate();
    }

    private void OnValidate()
    {
        if (width % 2 == 0) width += 1;
        if (height % 2 == 0) height += 1;
        if (pathCount < 3) pathCount = 3;
    }

    public void Generate()
    {
        EnsureMaterials();
        ClampDimensions();

        int seedToUse = randomizeSeed ? (int)(DateTime.UtcNow.Ticks & 0x7fffffff) : seed;
        lastSeedUsed = seedToUse;

        TileType[,] tiles = null;
        List<List<Vector2Int>> pathCells = null;

        for (int attempt = 0; attempt < maxWfcRetries; attempt++)
        {
            var random = new System.Random(seedToUse + attempt * 97);
            pathCells = BuildPaths(random);
            if (TryGenerateTiles(random, pathCells, out tiles))
            {
                break;
            }
        }

        if (tiles == null)
        {
            tiles = CreateFallbackTiles(out pathCells);
        }

        float[,] cellHeights = BuildCellHeights(tiles);
        BuildGroundMesh(cellHeights);
        BuildPathOverlay(pathCells, cellHeights);
        CachePathsWorld(pathCells, cellHeights);
    }

    private void EnsureMaterials()
    {
        Shader shader = GetDefaultShader();
        if (shader == null)
        {
            Debug.LogWarning("ProceduralTerrainGenerator: No compatible shader found for terrain materials.");
            return;
        }

        if (groundMaterial == null)
        {
            groundMaterial = new Material(shader) { color = new Color(0.25f, 0.5f, 0.25f) };
        }

        if (pathMaterial == null)
        {
            pathMaterial = new Material(shader) { color = new Color(0.55f, 0.4f, 0.25f) };
        }

        meshRenderer.sharedMaterial = groundMaterial;
    }

    private void ClampDimensions()
    {
        if (width < 5) width = 5;
        if (height < 5) height = 5;
        if (width % 2 == 0) width += 1;
        if (height % 2 == 0) height += 1;
        if (pathCount < 3) pathCount = 3;
    }

    private Shader GetDefaultShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        return shader;
    }

    private List<List<Vector2Int>> BuildPaths(System.Random random)
    {
        int centerX = width / 2;
        int centerY = height / 2;
        var center = new Vector2Int(centerX, centerY);

        var paths = new List<List<Vector2Int>>();
        var usedStarts = new HashSet<Vector2Int>();
        int attempts = 0;

        while (paths.Count < pathCount && attempts < pathCount * 12)
        {
            attempts++;
            Vector2Int start = GetRandomEdgeCell(random);
            if (!usedStarts.Add(start))
            {
                continue;
            }

            var path = BuildPathFrom(start, center, random);
            paths.Add(path);
        }

        return paths;
    }

    private Vector2Int GetRandomEdgeCell(System.Random random)
    {
        int side = random.Next(4);
        if (side == 0) return new Vector2Int(0, random.Next(height));
        if (side == 1) return new Vector2Int(width - 1, random.Next(height));
        if (side == 2) return new Vector2Int(random.Next(width), 0);
        return new Vector2Int(random.Next(width), height - 1);
    }

    private List<Vector2Int> BuildPathFrom(Vector2Int start, Vector2Int center, System.Random random)
    {
        var path = new List<Vector2Int>();
        var current = start;
        path.Add(current);

        while (current != center)
        {
            int dx = Math.Sign(center.x - current.x);
            int dy = Math.Sign(center.y - current.y);
            var options = new List<Vector2Int>();
            if (dx != 0) options.Add(new Vector2Int(dx, 0));
            if (dy != 0) options.Add(new Vector2Int(0, dy));

            Vector2Int step = options[random.Next(options.Count)];
            current += step;
            path.Add(current);
        }

        return path;
    }

    private bool TryGenerateTiles(System.Random random, List<List<Vector2Int>> pathCells, out TileType[,] tiles)
    {
        tiles = null;

        TileDef[] defs = BuildTileDefinitions();
        int tileCount = defs.Length;
        int[] compatibleMaskByTile = BuildCompatibilityMasks(defs);

        int allGroundMask = (1 << (int)TileType.Grass) | (1 << (int)TileType.Dirt) | (1 << (int)TileType.Rock);
        int pathMask = 1 << (int)TileType.Path;

        int[,] masks = new int[width, height];
        bool[,] isPath = new bool[width, height];

        foreach (var path in pathCells)
        {
            foreach (var cell in path)
            {
                if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height) continue;
                isPath[cell.x, cell.y] = true;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                masks[x, y] = isPath[x, y] ? pathMask : allGroundMask;
            }
        }

        if (!PropagateAllConstraints(masks, compatibleMaskByTile))
        {
            return false;
        }

        int remaining = width * height;
        while (remaining > 0)
        {
            int bestX = -1;
            int bestY = -1;
            int bestEntropy = int.MaxValue;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int count = CountBits(masks[x, y]);
                    if (count == 0) return false;
                    if (count == 1) continue;
                    if (count < bestEntropy)
                    {
                        bestEntropy = count;
                        bestX = x;
                        bestY = y;
                    }
                }
            }

            if (bestX == -1)
            {
                break;
            }

            int chosenTile = PickRandomTile(masks[bestX, bestY], random, tileCount);
            masks[bestX, bestY] = 1 << chosenTile;

            if (!PropagateConstraints(masks, bestX, bestY, compatibleMaskByTile))
            {
                return false;
            }

            remaining--;
        }

        tiles = new TileType[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int tileIndex = MaskToIndex(masks[x, y], tileCount);
                tiles[x, y] = (TileType)tileIndex;
            }
        }

        return true;
    }

    private TileDef[] BuildTileDefinitions()
    {
        var defs = new TileDef[4];
        defs[(int)TileType.Grass] = new TileDef
        {
            height = grassHeight,
            allowedNeighbors = Mask(TileType.Grass, TileType.Dirt, TileType.Rock, TileType.Path)
        };
        defs[(int)TileType.Dirt] = new TileDef
        {
            height = dirtHeight,
            allowedNeighbors = Mask(TileType.Grass, TileType.Dirt, TileType.Rock, TileType.Path)
        };
        defs[(int)TileType.Rock] = new TileDef
        {
            height = rockHeight,
            allowedNeighbors = Mask(TileType.Grass, TileType.Dirt, TileType.Rock)
        };
        defs[(int)TileType.Path] = new TileDef
        {
            height = grassHeight,
            allowedNeighbors = Mask(TileType.Grass, TileType.Dirt, TileType.Path)
        };
        return defs;
    }

    private int[] BuildCompatibilityMasks(TileDef[] defs)
    {
        int tileCount = defs.Length;
        int[] compat = new int[tileCount];

        for (int t = 0; t < tileCount; t++)
        {
            int mask = 0;
            for (int n = 0; n < tileCount; n++)
            {
                if ((defs[n].allowedNeighbors & (1 << t)) != 0)
                {
                    mask |= 1 << n;
                }
            }
            compat[t] = mask;
        }

        return compat;
    }

    private bool PropagateAllConstraints(int[,] masks, int[] compatibleMaskByTile)
    {
        var queue = new Queue<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (CountBits(masks[x, y]) == 1)
                {
                    queue.Enqueue(new Vector2Int(x, y));
                }
            }
        }

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            if (!PropagateFromCell(masks, cell.x, cell.y, compatibleMaskByTile, queue))
            {
                return false;
            }
        }

        return true;
    }

    private bool PropagateConstraints(int[,] masks, int startX, int startY, int[] compatibleMaskByTile)
    {
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            if (!PropagateFromCell(masks, cell.x, cell.y, compatibleMaskByTile, queue))
            {
                return false;
            }
        }

        return true;
    }

    private bool PropagateFromCell(int[,] masks, int x, int y, int[] compatibleMaskByTile, Queue<Vector2Int> queue)
    {
        int allowedMask = AllowedNeighborMask(masks[x, y], compatibleMaskByTile);

        for (int dir = 0; dir < 4; dir++)
        {
            int nx = x;
            int ny = y;
            if (dir == 0) nx -= 1;
            if (dir == 1) nx += 1;
            if (dir == 2) ny -= 1;
            if (dir == 3) ny += 1;

            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
            {
                continue;
            }

            int oldMask = masks[nx, ny];
            int newMask = oldMask & allowedMask;
            if (newMask == oldMask)
            {
                continue;
            }

            if (newMask == 0)
            {
                return false;
            }

            masks[nx, ny] = newMask;
            queue.Enqueue(new Vector2Int(nx, ny));
        }

        return true;
    }

    private int AllowedNeighborMask(int cellMask, int[] compatibleMaskByTile)
    {
        int allowed = 0;
        for (int t = 0; t < compatibleMaskByTile.Length; t++)
        {
            if ((cellMask & (1 << t)) != 0)
            {
                allowed |= compatibleMaskByTile[t];
            }
        }
        return allowed;
    }

    private TileType[,] CreateFallbackTiles(out List<List<Vector2Int>> pathCells)
    {
        pathCells = BuildPaths(new System.Random(lastSeedUsed));
        var tiles = new TileType[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = TileType.Grass;
            }
        }

        foreach (var path in pathCells)
        {
            foreach (var cell in path)
            {
                tiles[cell.x, cell.y] = TileType.Path;
            }
        }

        return tiles;
    }

    private float[,] BuildCellHeights(TileType[,] tiles)
    {
        float[,] heights = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = GetTileHeight(tiles[x, y]);
            }
        }
        return heights;
    }

    private void BuildGroundMesh(float[,] cellHeights)
    {
        int vertWidth = width + 1;
        int vertHeight = height + 1;
        int vertexCount = vertWidth * vertHeight;
        var vertices = new Vector3[vertexCount];
        var uvs = new Vector2[vertexCount];
        var triangles = new int[width * height * 6];

        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;

        for (int y = 0; y < vertHeight; y++)
        {
            for (int x = 0; x < vertWidth; x++)
            {
                float heightValue = GetVertexHeight(x, y, cellHeights);
                int index = y * vertWidth + x;
                vertices[index] = new Vector3((x - halfWidth) * cellSize, heightValue, (y - halfHeight) * cellSize);
                uvs[index] = new Vector2((float)x / width, (float)y / height);
            }
        }

        int t = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * vertWidth + x;
                triangles[t++] = i;
                triangles[t++] = i + vertWidth + 1;
                triangles[t++] = i + vertWidth;
                triangles[t++] = i;
                triangles[t++] = i + 1;
                triangles[t++] = i + vertWidth + 1;
            }
        }

        var mesh = new Mesh
        {
            name = "ProceduralTerrain"
        };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private void BuildPathOverlay(List<List<Vector2Int>> pathCells, float[,] cellHeights)
    {
        if (pathOverlay != null)
        {
            Destroy(pathOverlay);
        }

        int totalCells = 0;
        foreach (var path in pathCells)
        {
            totalCells += path.Count;
        }

        if (totalCells == 0)
        {
            return;
        }

        var overlay = new GameObject("PathOverlay");
        overlay.transform.SetParent(transform, false);
        var filter = overlay.AddComponent<MeshFilter>();
        var renderer = overlay.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = pathMaterial;
        pathOverlay = overlay;

        var vertices = new Vector3[totalCells * 4];
        var uvs = new Vector2[totalCells * 4];
        var triangles = new int[totalCells * 6];

        float halfSize = cellSize * 0.5f;
        int v = 0;
        int t = 0;
        int centerX = width / 2;
        int centerY = height / 2;

        foreach (var path in pathCells)
        {
            foreach (var cell in path)
            {
                float heightValue = cellHeights[cell.x, cell.y] + pathOverlayHeight;
                Vector3 center = new Vector3((cell.x - centerX) * cellSize, heightValue, (cell.y - centerY) * cellSize);

                vertices[v] = center + new Vector3(-halfSize, 0f, -halfSize);
                vertices[v + 1] = center + new Vector3(-halfSize, 0f, halfSize);
                vertices[v + 2] = center + new Vector3(halfSize, 0f, halfSize);
                vertices[v + 3] = center + new Vector3(halfSize, 0f, -halfSize);

                uvs[v] = new Vector2(0f, 0f);
                uvs[v + 1] = new Vector2(0f, 1f);
                uvs[v + 2] = new Vector2(1f, 1f);
                uvs[v + 3] = new Vector2(1f, 0f);

                triangles[t++] = v;
                triangles[t++] = v + 1;
                triangles[t++] = v + 2;
                triangles[t++] = v;
                triangles[t++] = v + 2;
                triangles[t++] = v + 3;

                v += 4;
            }
        }

        var mesh = new Mesh
        {
            name = "PathOverlay"
        };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        filter.sharedMesh = mesh;
    }

    private void CachePathsWorld(List<List<Vector2Int>> pathCells, float[,] cellHeights)
    {
        pathsWorld.Clear();
        int centerX = width / 2;
        int centerY = height / 2;

        foreach (var path in pathCells)
        {
            if (path.Count == 0) continue;
            var worldPath = new List<Vector3>(path.Count);
            foreach (var cell in path)
            {
                float heightValue = cellHeights[cell.x, cell.y];
                Vector3 local = new Vector3((cell.x - centerX) * cellSize, heightValue, (cell.y - centerY) * cellSize);
                worldPath.Add(transform.TransformPoint(local));
            }
            pathsWorld.Add(worldPath);
        }
    }

    private float GetTileHeight(TileType type)
    {
        if (type == TileType.Dirt) return dirtHeight;
        if (type == TileType.Rock) return rockHeight;
        return grassHeight;
    }

    private float GetVertexHeight(int vx, int vy, float[,] cellHeights)
    {
        float sum = 0f;
        int count = 0;
        for (int dx = -1; dx <= 0; dx++)
        {
            for (int dy = -1; dy <= 0; dy++)
            {
                int cx = vx + dx;
                int cy = vy + dy;
                if (cx < 0 || cx >= width || cy < 0 || cy >= height)
                {
                    continue;
                }
                sum += cellHeights[cx, cy];
                count++;
            }
        }

        if (count == 0) return 0f;
        return sum / count;
    }

    private int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            mask &= mask - 1;
            count++;
        }
        return count;
    }

    private int PickRandomTile(int mask, System.Random random, int tileCount)
    {
        int options = CountBits(mask);
        int pick = random.Next(options);
        for (int t = 0; t < tileCount; t++)
        {
            if ((mask & (1 << t)) == 0) continue;
            if (pick == 0) return t;
            pick--;
        }
        return 0;
    }

    private int MaskToIndex(int mask, int tileCount)
    {
        for (int t = 0; t < tileCount; t++)
        {
            if ((mask & (1 << t)) != 0)
            {
                return t;
            }
        }
        return 0;
    }

    private int Mask(params TileType[] tiles)
    {
        int mask = 0;
        for (int i = 0; i < tiles.Length; i++)
        {
            mask |= 1 << (int)tiles[i];
        }
        return mask;
    }
}
