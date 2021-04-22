using System;
using DefaultNamespace;
using UnityEngine;

namespace Models
{
    public class TerrainChunk
    {
        private Vector2 _position;
        private GameObject _meshObject;
        private Bounds _bounds;
        private TerrainGenerator _generator;

        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        
        public TerrainChunk(TerrainGenerator generator, Vector2 coord, int size, Transform parent, Material material)
        {
            _generator = generator;
            _position = coord * size;
            _bounds = new Bounds(_position, Vector2.one * size);
            Vector3 position3d = new Vector3(_position.x, 0, _position.y);

            _meshObject = new GameObject($"Terrain Chunk ({coord.x}, {coord.y})");
            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshFilter = _meshObject.AddComponent<MeshFilter>();
            _meshRenderer.material = material;

            _meshObject.transform.position = position3d;
            _meshObject.transform.parent = parent;

            SetVisible(false);
            
            _generator.RequestMapData(OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            _generator.RequestMeshData(mapData, OnMeshDataReceived);
        }
        
        void OnMeshDataReceived(MeshData meshData)
        {
            _meshFilter.mesh = meshData.CreateMesh();
        }

        public void Update(float viewDistance, Vector3 viewerPosition)
        {
            float viewerToEdge = Mathf.Sqrt(_bounds.SqrDistance(viewerPosition));
            bool isVisible = viewerToEdge <= viewDistance;
            SetVisible(isVisible);
        }

        public void SetVisible(bool visible)
        {
            _meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return _meshObject.activeSelf;
        }
    }
}