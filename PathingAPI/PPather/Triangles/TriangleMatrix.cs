/*
 *  Part of PPather
 *  Copyright Pontus Borg 2008
 *
 */

using PatherPath;
using System.Collections.Generic;

namespace WowTriangles
{
    public class TriangleMatrix
    {
        private float resolution = 2.0f;
        private SparseFloatMatrix2D<List<int>> matrix;
        private int maxAtOne;

        private void AddTriangleAt(float x, float y, int triangle)
        {
            List<int> l = matrix.Get(x, y);
            if (l == null)
            {
                l = new List<int>(8); // hmm
                l.Add(triangle);

                matrix.Set(x, y, l);
            }
            else
            {
                l.Add(triangle);
            }

            if (l.Count > maxAtOne)
                maxAtOne = l.Count;
        }

        private Logger logger;

        public TriangleMatrix(TriangleCollection tc, Logger logger)
        {
            this.logger = logger;

            System.DateTime pre = System.DateTime.Now;
            logger.WriteLine("Build hash  " + tc.GetNumberOfTriangles());
            matrix = new SparseFloatMatrix2D<List<int>>(resolution, tc.GetNumberOfTriangles());

            Vector vertex0;
            Vector vertex1;
            Vector vertex2;

            for (int i = 0; i < tc.GetNumberOfTriangles(); i++)
            {
                tc.GetTriangleVertices(i,
                        out vertex0.x, out vertex0.y, out vertex0.z,
                        out vertex1.x, out vertex1.y, out vertex1.z,
                        out vertex2.x, out vertex2.y, out vertex2.z);

                float minx = Utils.min(vertex0.x, vertex1.x, vertex2.x);
                float maxx = Utils.max(vertex0.x, vertex1.x, vertex2.x);
                float miny = Utils.min(vertex0.y, vertex1.y, vertex2.y);
                float maxy = Utils.max(vertex0.y, vertex1.y, vertex2.y);

                Vector box_center;
                Vector box_halfsize;
                box_halfsize.x = resolution / 2;
                box_halfsize.y = resolution / 2;
                box_halfsize.z = 1E6f;

                int startx = matrix.LocalToGrid(minx);
                int endx = matrix.LocalToGrid(maxx);
                int starty = matrix.LocalToGrid(miny);
                int endy = matrix.LocalToGrid(maxy);

                for (int x = startx; x <= endx; x++)
                    for (int y = starty; y <= endy; y++)
                    {
                        float grid_x = matrix.GridToLocal(x);
                        float grid_y = matrix.GridToLocal(y);
                        box_center.x = grid_x + resolution / 2;
                        box_center.y = grid_y + resolution / 2;
                        box_center.z = 0;
                        if (Utils.TestTriangleBoxIntersect(vertex0, vertex1, vertex2, box_center, box_halfsize))
                            AddTriangleAt(grid_x, grid_y, i);
                    }
            }
            System.DateTime post = System.DateTime.Now;
            System.TimeSpan ts = post.Subtract(pre);
            logger.WriteLine("done " + maxAtOne + " time " + ts);
        }

        public Set<int> GetAllCloseTo(float x, float y, float distance)
        {
            List<List<int>> close = matrix.GetAllInSquare(x - distance, y - distance, x + distance, y + distance);
            Set<int> all = new Set<int>();

            foreach (List<int> l in close)
            {
                all.AddRange(l);
            }
            return all;
        }

        public ICollection<int> GetAllInSquare(float x0, float y0, float x1, float y1)
        {
            Set<int> all = new Set<int>();
            List<List<int>> close = matrix.GetAllInSquare(x0, y0, x1, y1);

            foreach (List<int> l in close)
            {
                all.AddRange(l);
            }
            return all;
        }
    }
}