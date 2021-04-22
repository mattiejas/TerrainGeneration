using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DefaultNamespace;
using Models;
using UnityEngine;
using UnityEngine.UI;

public class TerrainGenerator : MonoBehaviour
{
    private const int MapChunkSize = 121;

    [Range(0, 6)] public int levelOfDetail;

    public int noiseScale;
    public int heightMultiplier;
    public AnimationCurve heightCurve;

    public int seed;
    public Vector2 offset;

    public int octaves;
    [Range(0, 1)] public float persistence;
    public float lacunarity;

    public TerrainType[] terrainTypes;
    public bool hardEdges;
    public bool autoUpdate;
    
    
    private Queue<QueuedAction<MapData>> _mapDataActionQueue = new Queue<QueuedAction<MapData>>();

    // Start is called before the first frame update
    void Start()
    {
        Draw();
    }

    private void Update()
    {
        lock (_mapDataActionQueue)
        {
            while (_mapDataActionQueue.Any())
            {
                var queuedAction = _mapDataActionQueue.Dequeue();
                queuedAction.Action(queuedAction.Payload);
            }
        }
    }

    private void OnValidate()
    {
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
    }

    MapData GenerateMapData()
    {
        var noiseMap = NoiseGenerator.GenerateNoiseMap(MapChunkSize, MapChunkSize, seed, noiseScale, octaves,
            persistence, lacunarity, offset);
        var colourMap = GetColourMapFromHeightMap(noiseMap);
        return new MapData {ColourMap = colourMap, HeightMap = noiseMap};
    }

    private Color[] GetColourMapFromHeightMap(float[,] height)
    {
        Color[] colourMap = new Color[MapChunkSize * MapChunkSize];
        for (int y = 0; y < MapChunkSize; y++)
        {
            for (int x = 0; x < MapChunkSize; x++)
            {
                float currentHeight = height[x, y];
                for (int i = 0; i < terrainTypes.Length; i++)
                {
                    if (currentHeight <= terrainTypes[i].height)
                    {
                        colourMap[y * MapChunkSize + x] = terrainTypes[i].colour;
                        break;
                    }
                }
            }
        }

        return colourMap;
    }

    public void Draw()
    {
        var mapData = GenerateMapData();
        var display = GetComponent<MapDisplay>();
        var texture = TextureGenerator.TextureFromColourMap(mapData.ColourMap, MapChunkSize, MapChunkSize);

        display.DrawMinimap(texture);
        display.DrawMesh(GenerateMeshFromHeightmap(mapData.HeightMap, heightCurve, levelOfDetail), texture);
    }

    public MeshData GenerateMeshFromHeightmap(float[,] heightmap, AnimationCurve heightCurve, int levelOfDetail)
    {
        int width = heightmap.GetLength(0);
        int height = heightmap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int meshSimplificationFactor = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
        int numberOfVertices = (width - 1) / meshSimplificationFactor + 1;

        MeshData meshData = new MeshData(numberOfVertices, numberOfVertices, hardEdges);
        int vertexIndex = 0;

        for (int z = 0; z < height; z += meshSimplificationFactor)
        {
            for (int x = 0; x < width; x += meshSimplificationFactor)
            {
                var y = heightCurve.Evaluate(heightmap[x, z]) * heightMultiplier;
                meshData.Vertices[vertexIndex] = new Vector3(topLeftX + x, y, topLeftZ + z);
                meshData.UVs[vertexIndex] = new Vector2(x / (float) width, z / (float) height);
                
                // add triangles 
                if (x < width - 1 && z < height - 1)
                {
                    meshData.AddTriangle (vertexIndex + numberOfVertices, vertexIndex + numberOfVertices + 1, vertexIndex);
                    meshData.AddTriangle (vertexIndex + 1 , vertexIndex, vertexIndex + numberOfVertices + 1);
                }

                vertexIndex++;
            }
        }

        // dicretize vertices
        if (hardEdges)
        {
            Vector3[] vertices = new Vector3[meshData.Triangles.Length];
            int[] triangles = new int[meshData.Triangles.Length];
            Vector2[] uv = new Vector2[meshData.Triangles.Length];
            
            for (int i = 0; i < meshData.Triangles.Length; i++)
            {
                vertices[i] = meshData.Vertices[meshData.Triangles[i]];
                uv[i] = new Vector2(vertices[i].x / (float)width, vertices[i].z / (float)height);
                triangles[i] = i;
            }
            
            meshData.Triangles = triangles;
            meshData.Vertices = vertices;
            meshData.UVs = uv;
        }

        // calculate triangles
        // _triangles = new int[xSize * zSize * 6]; // quad has 2 triangles
        // for (int i = 0, z = 0; z < MapChunkSize; z++, i++)
        // {
        //     for (int x = 0; x < MapChunkSize; x++, i++)
        //     {
        //         meshData.Triangles.AddRange(GetTrianglesForVertex(i));
        //         // yield return new WaitForSeconds(.01f);
        //     }
        // }

        return meshData;
    }

    private IEnumerable<int> GetTrianglesForVertex(int vertex)
    {
        return new[]
        {
            vertex, vertex + MapChunkSize + 1, vertex + 1, // x + 1 goes up one row
            vertex + 1, vertex + MapChunkSize + 1, vertex + MapChunkSize + 2,
        };
    }

    // TODO
    public void RequestMapData(Action<MapData> callback)
    {
        void ThreadStart()
        {
            GenerateMapData(callback);
        }

        new Thread((ThreadStart) ThreadStart).Start();
    }

    private void GenerateMapData(Action<MapData> callback)
    {
        var data = GenerateMapData();
        lock (_mapDataActionQueue)
        {
            _mapDataActionQueue.Enqueue(new QueuedAction<MapData> {Action = callback, Payload = data});
        }
    }
}