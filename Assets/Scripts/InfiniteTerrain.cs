using System.Collections.Generic;
using System.Linq;
using Models;
using UnityEngine;

namespace DefaultNamespace
{
    public class InfiniteTerrain : MonoBehaviour
    {
        public const float MaxViewingDistance = 450;
        public Transform Viewer;
        public static Vector2 POV;
        public Material MapMaterial;

        private int _chunkSize;
        private int _numberOfChunksVisible;
        private TerrainGenerator _generator;

        private Dictionary<Vector2, TerrainChunk> _chunks = new Dictionary<Vector2, TerrainChunk>();
        private List<TerrainChunk> _visible = new List<TerrainChunk>();

        void Start()
        {
            _generator = FindObjectOfType<TerrainGenerator>();
            _chunkSize = TerrainGenerator.MapChunkSize - 1;
            _numberOfChunksVisible = Mathf.RoundToInt(MaxViewingDistance / _chunkSize);
        }

        void Update()
        {
            var position = Viewer.position;
            POV = new Vector2(position.x, position.z);
            UpdateVisibleChunks();
        }

        void UpdateVisibleChunks()
        {
            _visible.ForEach(x => x.SetVisible(false));
            _visible.Clear();
            
            int x = Mathf.RoundToInt(POV.x / _chunkSize);
            int y = Mathf.RoundToInt(POV.y / _chunkSize);

            for (int yOffset = -_numberOfChunksVisible; yOffset <= _numberOfChunksVisible; yOffset++)
            {
                for (int xOffset = -_numberOfChunksVisible; xOffset <= _numberOfChunksVisible; xOffset++)
                {
                    Vector2 chunk = new Vector2(x + xOffset, y + yOffset);

                    if (_chunks.ContainsKey(chunk))
                    {
                        _chunks[chunk].Update(MaxViewingDistance, POV);
                    }
                    else
                    {
                        _chunks.Add(chunk, new TerrainChunk(_generator, chunk, _chunkSize, transform, MapMaterial));
                    }
                    
                    if (_chunks[chunk].IsVisible()) _visible.Add(_chunks[chunk]);
                }
            }
        }
    }
}