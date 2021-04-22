using UnityEngine;

namespace DefaultNamespace
{
    public class InfiniteTerrain
    {
        public const float MaxViewingDistance = 300;
        public Transform Viewer;
        public static Vector2 POV;

        private int chunkSize; 
    }
}