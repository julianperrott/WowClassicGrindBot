/*
 *  Part of PPather
 *  Copyright Pontus Borg 2008
 *
 */

using PatherPath;

namespace WowTriangles
{
    /// <summary>
    /// Quadtree (splits on x and y)
    /// </summary>
    public class TriangleQuadtree
    {
        private const int SplitSize = 64;

        public Node rootNode;
        private TriangleCollection tc;

        private Vector min;
        private Vector max;

        public class Node
        {
            public Vector min;
            public Vector mid;
            public Vector max;

            private TriangleQuadtree tree;

            private Node parent;
            public Node[,] children; // [2,2]

            public int[] triangles;

            private Logger logger;

            public Node(TriangleQuadtree tree,
                        Vector min,
                        Vector max, Logger logger)
            {
                this.logger = logger;
                this.tree = tree;
                this.min = min;
                this.max = max;
                this.mid.x = (min.x + max.x) / 2;
                this.mid.y = (min.y + max.y) / 2;
                this.mid.z = 0;
            }

            public void Build(SimpleLinkedList triangles, int depth)
            {
                if (triangles.Count < SplitSize || depth >= 10)
                {
                    this.triangles = new int[triangles.Count];
                    SimpleLinkedList.Node rover = triangles.first;
                    int i = 0;
                    while (rover != null)
                    {
                        this.triangles[i] = rover.val;
                        rover = rover.next;
                        i++;
                    }
                    if (triangles.Count >= SplitSize)
                    {
                        Vector size;
                        Utils.sub(out size, ref max, ref min);
                        logger.WriteLine("New leaf " + depth + " size: " + triangles.Count + " " + size);
                    }
                }
                else
                {
                    this.triangles = null;

                    float[] xl = new float[3] { min.x, mid.x, max.x };
                    float[] yl = new float[3] { min.y, mid.y, max.y };

                    Vector boxhalfsize = new Vector(
                           mid.x - min.x,
                           mid.y - min.y,
                           1E10f);

                    children = new Node[2, 2];

                    Vector vertex0;
                    Vector vertex1;
                    Vector vertex2;

                    // if (depth <= 3)
                    //     logger.WriteLine(depth + " Pre tris: " + triangles.Count);

                    int ugh = 0;
                    //foreach (int triangle in triangles)
                    for (int x = 0; x < 2; x++)
                    {
                        for (int y = 0; y < 2; y++)
                        {
                            SimpleLinkedList.Node rover = triangles.GetFirst();
                            SimpleLinkedList childTris = new SimpleLinkedList(this.logger);

                            children[x, y] = new Node(tree,
                                                         new Vector(xl[x], yl[y], 0),
                                                         new Vector(xl[x + 1], yl[y + 1], 0), this.logger);
                            children[x, y].parent = this;
                            int c = 0;
                            while (rover != null)
                            {
                                c++;
                                SimpleLinkedList.Node next = rover.next;
                                int triangle = rover.val;
                                tree.tc.GetTriangleVertices(triangle,
                                        out vertex0.x, out vertex0.y, out vertex0.z,
                                        out vertex1.x, out vertex1.y, out vertex1.z,
                                        out vertex2.x, out vertex2.y, out vertex2.z);

                                if (Utils.TestTriangleBoxIntersect(vertex0, vertex1, vertex2,
                                                                  children[x, y].mid, boxhalfsize))
                                {
                                    childTris.Steal(rover, triangles);

                                    ugh++;
                                }
                                rover = next;
                            }
                            if (c == 0)
                            {
                                children[x, y] = null; // drop that
                            }
                            else
                            {
                                //logger.WriteLine(depth + " of " + c + " stole " + childTris.RealCount + "(" + childTris.Count + ")" + " left is " + triangles.RealCount + "(" + triangles.Count + ")");
                                children[x, y].Build(childTris, depth + 1);
                                triangles.StealAll(childTris);
                            }
                            /*if (depth == 0)
                            {
                                logger.WriteLine("Post tris: " + triangles.Count);
                                logger.WriteLine("count: " + c);
                            }*/
                        }
                    }
                }
            }
        }

        private Logger logger;

        public TriangleQuadtree(TriangleCollection tc, Logger logger)
        {
            this.logger = logger;
            logger.WriteLine("Build oct " + tc.GetNumberOfTriangles());
            this.tc = tc;
            tc.GetBBox(out min.x, out min.y, out min.z,
                       out max.x, out max.y, out max.z);
            rootNode = new Node(this, min, max, this.logger);

            SimpleLinkedList tlist = new SimpleLinkedList(this.logger);
            for (int i = 0; i < tc.GetNumberOfTriangles(); i++)
            {
                tlist.AddNew(i);
            }
            rootNode.Build(tlist, 0);
            logger.WriteLine("done");
        }
    }
}