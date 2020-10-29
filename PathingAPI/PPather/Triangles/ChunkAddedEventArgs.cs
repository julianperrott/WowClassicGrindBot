/*
 *  Part of PPather
 *  Copyright Pontus Borg 2008
 *
 */

namespace WowTriangles
{
    public class ChunkAddedEventArgs
    {
        public ChunkAddedEventArgs(TriangleCollection triangles)
        {
            Triangles = triangles;
        }

        public TriangleCollection Triangles { get; } // readonly
    }
}