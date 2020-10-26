using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WowTriangles;

namespace PathingAPI
{
    public class MeshFactory
    {
        public static Data.Vertex[] CreatePointList(List<TriangleCollection> m_TriangleCollection)
        {
            var points = new Data.Vertex[m_TriangleCollection.Sum(tc => tc.VertexCount())];

            int vertextNumber = 0;

            m_TriangleCollection.ForEach(tc =>
            {
                int[] triangleFlags = new int[tc.VertexCount()];
                for (int i = 0; i < tc.VertexCount(); i++)
                {
                    triangleFlags[i] = 4;
                }

                for (int i = 0; i < tc.TriangleCount(); i++)
                {
                    int v0, v1, v2, flags, sequence;

                    tc.GetTriangle(i, out v0, out v1, out v2, out flags, out sequence);
                    triangleFlags[v0] = flags;
                    triangleFlags[v1] = flags;
                    triangleFlags[v2] = flags;
                }

                for (int i = 0; i < tc.VertexCount(); i++)
                {
                    float x, y, z;
                    tc.GetVertex(i, out x, out y, out z);
                    points[vertextNumber] = Data.Vertex.Create(x, y, z, triangleFlags[i]);
                    vertextNumber++;
                }
            });

            return points;
        }

        public static int[] CreateTrianglesList(int modelType, List<TriangleCollection> m_TriangleCollection)
        {
            var triangleCount = m_TriangleCollection.Sum(tc => tc.TriangleCount());
            int[] triangles = new int[triangleCount * 3];

            int triangleNumber = 0;
            int vertexOffset = 0;
            foreach (TriangleCollection tc in m_TriangleCollection)
            {
                for (int i = 0; i < tc.TriangleCount(); i++)
                {
                    int v0, v1, v2, flags, sequence;

                    tc.GetTriangle(i, out v0, out v1, out v2, out flags, out sequence);
                    if (flags == modelType || modelType == -1)
                    {
                        triangles[triangleNumber * 3] = v0 + vertexOffset;
                        triangles[triangleNumber * 3 + 1] = v1 + vertexOffset;
                        triangles[triangleNumber * 3 + 2] = v2 + vertexOffset;
                    }
                    else
                    {
                        triangles[triangleNumber * 3] = -1;
                        triangles[triangleNumber * 3 + 1] = -1;
                        triangles[triangleNumber * 3 + 2] = -1;
                    }
                    triangleNumber++;
                }
                vertexOffset += tc.VertexCount();
            }
            return triangles.Where(t => t != -1).ToArray();
        }
    }
}
