/*
 *  Part of PPather
 *  Copyright Pontus Borg 2008
 *
 */

using PatherPath;

namespace WowTriangles
{
    /// <summary>
    ///
    /// </summary>
    public class TriangleCollection
    {
        private Logger logger;

        public TriangleCollection(Logger logger)
        {
            this.logger = logger;
        }

        public bool changed = true;
        public int LRU;
        public float base_x, base_y;
        public int grid_x, grid_y;

        private class VertexArray : TrioArray<float>
        {
        }

        private class IndexArray : QuadArray<int>
        {
        }

        public int TriangleCount()
        {
            return no_triangles;
        }

        public int VertexCount()
        {
            return no_vertices;
        }

        private VertexArray vertices = new VertexArray();
        private int no_vertices;

        private IndexArray triangles = new IndexArray();
        private int no_triangles;

        private SparseFloatMatrix3D<int> vertexMatrix = new SparseFloatMatrix3D<int>(0.1f);

        private TriangleMatrix collisionMatrix;

        public float max_x = -1E30f;
        public float max_y = -1E30f;
        public float max_z = -1E30f;

        public float min_x = 1E30f;
        public float min_y = 1E30f;
        public float min_z = 1E30f;

        private float limit_max_x = 1E30f;
        private float limit_max_y = 1E30f;
        private float limit_max_z = 1E30f;

        private float limit_min_x = -1E30f;
        private float limit_min_y = -1E30f;
        private float limit_min_z = -1E30f;

        public float[] color;
        public bool fill;

        public void Clear()
        {
            no_triangles = 0;
            no_vertices = 0;
            vertexMatrix = new SparseFloatMatrix3D<int>(0.1f);
            changed = true;
        }

        private TriangleOctree oct;

        public TriangleOctree GetOctree()
        {
            if (oct == null)
                oct = new TriangleOctree(this, this.logger);
            return oct;
        }

        // remove unused vertices
        public void CompactVertices()
        {
            bool[] used_indices = new bool[GetNumberOfVertices()];
            int[] old_to_new = new int[GetNumberOfVertices()];

            // check what vertives are used
            for (int i = 0; i < GetNumberOfTriangles(); i++)
            {
                int v0, v1, v2;
                GetTriangle(i, out v0, out v1, out v2);
                used_indices[v0] = true;
                used_indices[v1] = true;
                used_indices[v2] = true;
            }

            // figure out new indices and move
            int sum = 0;
            for (int i = 0; i < used_indices.Length; i++)
            {
                if (used_indices[i])
                {
                    old_to_new[i] = sum;
                    float x, y, z;
                    vertices.Get(i, out x, out y, out z);
                    vertices.Set(sum, x, y, z);
                    sum++;
                }
                else
                    old_to_new[i] = -1;
            }

            vertices.SetSize(sum);

            // Change all triangles
            for (int i = 0; i < GetNumberOfTriangles(); i++)
            {
                int v0, v1, v2, flags, sequence;
                GetTriangle(i, out v0, out v1, out v2, out flags, out sequence);
                triangles.Set(i, old_to_new[v0], old_to_new[v1], old_to_new[v2], flags, sequence);
            }
            no_vertices = sum;
        }

        private TriangleQuadtree quad;

        public TriangleQuadtree GetQuadtree()
        {
            if (quad == null)
                quad = new TriangleQuadtree(this, this.logger);
            return quad;
        }

        public TriangleMatrix GetTriangleMatrix()
        {
            if (collisionMatrix == null)
                collisionMatrix = new TriangleMatrix(this, this.logger);
            return collisionMatrix;
        }

        public void SetLimits(float min_x, float min_y, float min_z,
                              float max_x, float max_y, float max_z)
        {
            limit_max_x = max_x;
            limit_max_y = max_y;
            limit_max_z = max_z;

            limit_min_x = min_x;
            limit_min_y = min_y;
            limit_min_z = min_z;
        }

        public void GetLimits(out float min_x, out float min_y, out float min_z,
                              out float max_x, out float max_y, out float max_z)
        {
            max_x = limit_max_x;
            max_y = limit_max_y;
            max_z = limit_max_z;

            min_x = limit_min_x;
            min_y = limit_min_y;
            min_z = limit_min_z;
        }

        public void PaintPath(float x, float y, float z, float x2, float y2, float z2)
        {
            int v0 = AddVertex(x, y, z + 0.1f);
            int v1 = AddVertex(x, y, z + 0.5f);
            int v2 = AddVertex(x2, y2, z2 + 0.1f);

            //int v0 = AddVertex(x, y, z + 2.0f - 0.5f);
            //int v1 = AddVertex(x, y, z + 2.0f);
            //int v2 = AddVertex(x2, y2, z2+2.0f-0.5f);

            AddTriangle(v0, v1, v2);
            AddTriangle(v2, v1, v0);
        }

        public void AddMarker(float x, float y, float z)
        {
            int v0 = AddVertex(x, y, z);
            int v1 = AddVertex(x + 0.3f, y, z + 1.0f);
            int v2 = AddVertex(x - 0.3f, y, z + 1.0f);
            int v3 = AddVertex(x, y + 0.3f, z + 1.0f);
            int v4 = AddVertex(x, y - 0.3f, z + 1.0f);
            AddTriangle(v0, v1, v2);
            AddTriangle(v2, v1, v0);
            AddTriangle(v0, v3, v4);
            AddTriangle(v4, v3, v0);
        }

        public void AddBigMarker(float x, float y, float z)
        {
            int v0 = AddVertex(x, y, z);
            int v1 = AddVertex(x + 1.3f, y, z + 4);
            int v2 = AddVertex(x - 1.3f, y, z + 4);
            int v3 = AddVertex(x, y + 1.3f, z + 4);
            int v4 = AddVertex(x, y - 1.3f, z + 4);
            AddTriangle(v0, v1, v2);
            AddTriangle(v2, v1, v0);
            AddTriangle(v0, v3, v4);
            AddTriangle(v4, v3, v0);
        }

        public void GetBBox(out float min_x, out float min_y, out float min_z,
                              out float max_x, out float max_y, out float max_z)
        {
            max_x = this.max_x;
            max_y = this.max_y;
            max_z = this.max_z;

            min_x = this.min_x;
            min_y = this.min_y;
            min_z = this.min_z;
        }

        public int AddVertex(float x, float y, float z)
        {
            // Create new if needed or return old one

            if (vertexMatrix.IsSet(x, y, z))
                return vertexMatrix.Get(x, y, z);

            vertices.Set(no_vertices, x, y, z);
            vertexMatrix.Set(x, y, z, no_vertices);
            return no_vertices++;
        }

        // big list if triangles (3 vertice IDs per triangle)
        public int AddTriangle(int v0, int v1, int v2, int flags, int sequence)
        {
            // check limits
            if (!CheckVertexLimits(v0) &&
                !CheckVertexLimits(v1) &&
                !CheckVertexLimits(v2))
                return -1;
            // Create new
            SetMinMax(v0);
            SetMinMax(v1);
            SetMinMax(v2);

            triangles.Set(no_triangles, v0, v1, v2, flags, sequence);
            changed = true;
            return no_triangles++;
        }

        // big list if triangles (3 vertice IDs per triangle)
        public int AddTriangle(int v0, int v1, int v2)
        {
            return AddTriangle(v0, v1, v2, 0, 0);
        }

        private void SetMinMax(int v)
        {
            float x, y, z;
            GetVertex(v, out x, out y, out z);
            if (x < min_x)
                min_x = x;
            if (y < min_y)
                min_y = y;
            if (z < min_z)
                min_z = z;

            if (x > max_x)
                max_x = x;
            if (y > max_y)
                max_y = y;
            if (z > max_z)
                max_z = z;
        }

        private bool CheckVertexLimits(int v)
        {
            float x, y, z;
            GetVertex(v, out x, out y, out z);
            if (x < limit_min_x || x > limit_max_x)
                return false;
            if (y < limit_min_y || y > limit_max_y)
                return false;
            if (z < limit_min_z || z > limit_max_z)
                return false;

            return true;
        }

        public void GetBoundMax(out float x, out float y, out float z)
        {
            x = max_x;
            y = max_y;
            z = max_z;
        }

        public void GetBoundMin(out float x, out float y, out float z)
        {
            x = min_x;
            y = min_y;
            z = min_z;
        }

        public int GetNumberOfTriangles()
        {
            return no_triangles;
        }

        public int GetNumberOfVertices()
        {
            return no_vertices;
        }

        public void GetVertex(int i, out float x, out float y, out float z)
        {
            vertices.Get(i, out x, out y, out z);
        }

        public void GetTriangle(int i, out int v0, out int v1, out int v2)
        {
            int w;
            triangles.Get(i, out v0, out v1, out v2, out w, out int sequence);
        }

        public void GetTriangle(int i, out int v0, out int v1, out int v2, out int flags, out int sequence)
        {
            triangles.Get(i, out v0, out v1, out v2, out flags, out sequence);
        }

        public void GetTriangleVertices(int i,
                                        out float x0, out float y0, out float z0,
                                        out float x1, out float y1, out float z1,
                                        out float x2, out float y2, out float z2, out int flags, out int sequence)
        {
            int v0, v1, v2;

            triangles.Get(i, out v0, out v1, out v2, out flags, out sequence);
            vertices.Get(v0, out x0, out y0, out z0);
            vertices.Get(v1, out x1, out y1, out z1);
            vertices.Get(v2, out x2, out y2, out z2);
        }

        public void GetTriangleVertices(int i,
                                        out float x0, out float y0, out float z0,
                                        out float x1, out float y1, out float z1,
                                        out float x2, out float y2, out float z2)
        {
            int v0, v1, v2, flags, sequence;

            triangles.Get(i, out v0, out v1, out v2, out flags, out sequence);
            vertices.Get(v0, out x0, out y0, out z0);
            vertices.Get(v1, out x1, out y1, out z1);
            vertices.Get(v2, out x2, out y2, out z2);
        }

        public float[] GetFlatVertices()
        {
            float[] flat = new float[no_vertices * 3];
            for (int i = 0; i < no_vertices; i++)
            {
                int off = i * 3;
                vertices.Get(i, out flat[off], out flat[off + 1], out flat[off + 2]);
            }
            return flat;
        }

        public void ClearVertexMatrix()
        {
            vertexMatrix = new SparseFloatMatrix3D<int>(0.1f);
        }

        public void ReportSize(string pre)
        {
            logger.WriteLine(pre + "no_vertices: " + no_vertices);
            logger.WriteLine(pre + "no_triangles: " + no_triangles);
        }

        public void AddAllTrianglesFrom(TriangleCollection set)
        {
            for (int i = 0; i < set.GetNumberOfTriangles(); i++)
            {
                float v0x, v0y, v0z;
                float v1x, v1y, v1z;
                float v2x, v2y, v2z;
                set.GetTriangleVertices(i,
                    out v0x, out v0y, out v0z,
                    out v1x, out v1y, out v1z,
                    out v2x, out v2y, out v2z);
                int v0 = AddVertex(v0x, v0y, v0z);
                int v1 = AddVertex(v1x, v1y, v1z);
                int v2 = AddVertex(v2x, v2y, v2z);
                AddTriangle(v0, v1, v2);
            }
        }
    }
}