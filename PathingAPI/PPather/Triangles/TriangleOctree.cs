/*
 *  Part of PPather
 *  Copyright Pontus Borg 2008
 *
 */

using PatherPath;

namespace WowTriangles
{
    public class TriangleOctree
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

            private TriangleOctree tree;

            private Node parent;
            public Node[,,] children; // [2,2,2]

            public int[] triangles;
            private Logger logger;

            public Node(TriangleOctree tree,
                        Vector min,
                        Vector max, Logger logger)
            {
                this.logger = logger;
                this.tree = tree;
                this.min = min;
                this.max = max;
                this.mid.x = (min.x + max.x) / 2;
                this.mid.y = (min.y + max.y) / 2;
                this.mid.z = (min.z + max.z) / 2;

                //triangles = new SimpleLinkedList();  // assume being a leaf node
            }

            public void Build(SimpleLinkedList triangles, int depth)
            {
                if (triangles.Count < SplitSize || depth >= 8)
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
                        //Vector size;
                        //Utils.sub(out size, max, min);
                        //logger.WriteLine("New leaf " + depth + " size: " + triangles.Count + " " + size);
                    }
                }
                else
                {
                    this.triangles = null;

                    float[] xl = new float[3] { min.x, mid.x, max.x };
                    float[] yl = new float[3] { min.y, mid.y, max.y };
                    float[] zl = new float[3] { min.z, mid.z, max.z };

                    Vector boxhalfsize = new Vector(
                           mid.x - min.x,
                            mid.y - min.y,
                            mid.z - min.z);

                    // allocate children
                    //SimpleLinkedList[, ,] childTris = new SimpleLinkedList[2, 2, 2];
                    children = new Node[2, 2, 2];

                    Vector vertex0;
                    Vector vertex1;
                    Vector vertex2;

                    //foreach (int triangle in triangles)
                    for (int x = 0; x < 2; x++)
                    {
                        for (int y = 0; y < 2; y++)
                        {
                            for (int z = 0; z < 2; z++)
                            {
                                SimpleLinkedList.Node rover = triangles.GetFirst();
                                SimpleLinkedList childTris = new SimpleLinkedList(this.logger);

                                children[x, y, z] = new Node(tree,
                                                             new Vector(xl[x], yl[y], zl[z]),
                                                             new Vector(xl[x + 1], yl[y + 1], zl[z + 1]), this.logger);
                                children[x, y, z].parent = this;
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
                                                                      children[x, y, z].mid, boxhalfsize))
                                    {
                                        childTris.Steal(rover, triangles);
                                    }
                                    rover = next;
                                }
                                if (c == 0)
                                {
                                    children[x, y, z] = null; // drop that
                                }
                                else
                                {
                                    //logger.WriteLine(depth + " of " + c + " stole " + childTris.RealCount + "(" + childTris.Count + ")" + " left is " + triangles.RealCount + "(" + triangles.Count + ")");
                                    children[x, y, z].Build(childTris, depth + 1);
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

            public void FindTrianglesInBox(Vector box_min, Vector box_max, Set<int> found)
            {
                if (triangles != null)
                {
                    found.AddRange(triangles);
                }
                else
                {
                    for (int x = 0; x < 2; x++)
                    {
                        for (int y = 0; y < 2; y++)
                        {
                            for (int z = 0; z < 2; z++)
                            {
                                Node child = children[x, y, z];
                                if (child != null)
                                {
                                    if (Utils.TestBoxBoxIntersect(box_min, box_max, child.min, child.max))
                                        child.FindTrianglesInBox(box_min, box_max, found);
                                }
                            }
                        }
                    }
                }
            }
        }

        public Set<int> FindTrianglesInBox(float min_x, float min_y, float min_z,
                                           float max_x, float max_y, float max_z)
        {
            Vector min = new Vector(min_x, min_y, min_z);
            Vector max = new Vector(max_x, max_y, max_z);
            Set<int> found = new Set<int>();
            rootNode.FindTrianglesInBox(min, max, found);
            return found;
        }

        private Logger logger;

        public TriangleOctree(TriangleCollection tc, Logger logger)
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