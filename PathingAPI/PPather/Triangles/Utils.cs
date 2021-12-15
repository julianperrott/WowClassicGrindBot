/*
  This file is part of ppather.

    PPather is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    PPather is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with ppather.  If not, see <http://www.gnu.org/licenses/>.

    Copyright Pontus Borg 2008

 */

using System;

//using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WowTriangles
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Vector
    {
        [FieldOffset(0)]
        public float x;

        [FieldOffset(4)]
        public float y;

        [FieldOffset(8)]
        public float z;

        public Vector(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector(Vector v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        public override string ToString()
        {
            return String.Format("({0:.00} {1:.00} {2:.00})", x, y, z);
        }
    }

    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public override string ToString()
        {
            return String.Format("({0:.00} {1:.00} {2:.00} {3:.00})", x, y, z, w);
        }
    }

    public class Matrix4
    {
        private float[,] m = new float[4, 4];

        public void makeQuaternionRotate(Quaternion q)
        {
            m[0, 0] = 1.0f - 2.0f * q.y * q.y - 2.0f * q.z * q.z;
            m[0, 1] = 2.0f * q.x * q.y + 2.0f * q.w * q.z;
            m[0, 2] = 2.0f * q.x * q.z - 2.0f * q.w * q.y;
            m[1, 0] = 2.0f * q.x * q.y - 2.0f * q.w * q.z;
            m[1, 1] = 1.0f - 2.0f * q.x * q.x - 2.0f * q.z * q.z;
            m[1, 2] = 2.0f * q.y * q.z + 2.0f * q.w * q.x;
            m[2, 0] = 2.0f * q.x * q.z + 2.0f * q.w * q.y;
            m[2, 1] = 2.0f * q.y * q.z - 2.0f * q.w * q.x;
            m[2, 2] = 1.0f - 2.0f * q.x * q.x - 2.0f * q.y * q.y;
            m[0, 3] = m[1, 3] = m[2, 3] = m[3, 0] = m[3, 1] = m[3, 2] = 0;
            m[3, 3] = 1.0f;
        }

        public Vector mutiply(Vector v)
        {
            Vector o;
            o.x = m[0, 0] * v.x + m[0, 1] * v.y + m[0, 2] * v.z + m[0, 3];
            o.y = m[1, 0] * v.x + m[1, 1] * v.y + m[1, 2] * v.z + m[1, 3];
            o.z = m[2, 0] * v.x + m[2, 1] * v.y + m[2, 2] * v.z + m[2, 3];
            return o;
        }
    }

    internal unsafe class ccode
    {
        // int triBoxOverlap(float boxcenter[3],float boxhalfsize[3],float triverts[3][3]);
        [DllImport("MPQ\\ccode.dll")]
        public static extern int triBoxOverlap(
            float* boxcenter,
            float* boxhalfsize,
            float* trivert0,
            float* trivert1,
            float* trivert2
            );
    }

    public unsafe class Utils
    {
        public static float abs(float a)
        {
            if (a < 0.0f) return -a;
            return a;
        }

        public static float min(float a, float b)
        {
            if (a < b) return a;
            return b;
        }

        public static float min(float a, float b, float c)
        {
            if (a < b && a < c) return a;
            if (b < c) return b;
            return c;
        }

        public static float max(float a, float b)
        {
            if (a > b) return a;
            return b;
        }

        public static float max(float a, float b, float c)
        {
            if (a > b && a > c) return a;
            if (b > c) return b;
            return c;
        }

        public static float VecLength(ref Vector d)
        {
            return (float)Math.Sqrt(d.x * d.x + d.y * d.y + d.z * d.z);
        }

        public static void findMinMax(float a, float b, float c, out float min, out float max)
        {
            min = Utils.min(a, b, c);
            max = Utils.max(a, b, c);
        }

        public static void sub(out Vector C, ref Vector A, ref Vector B)
        {
            C.x = A.x - B.x;
            C.y = A.y - B.y;
            C.z = A.z - B.z;
        }

        public static void add(out Vector C, ref Vector A, ref Vector B)
        {
            C.x = A.x + B.x;
            C.y = A.y + B.y;
            C.z = A.z + B.z;
        }

        public static void mul(out Vector C, ref Vector A, float b)
        {
            C.x = A.x * b;
            C.y = A.y * b;
            C.z = A.z * b;
        }

        public static void div(out Vector C, ref Vector A, float b)
        {
            C.x = A.x / b;
            C.y = A.y / b;
            C.z = A.z / b;
        }

        public static void cross(out Vector dest, ref Vector v1, ref Vector v2)
        {
            dest.x = v1.y * v2.z - v1.z * v2.y;
            dest.y = v1.z * v2.x - v1.x * v2.z;
            dest.z = v1.x * v2.y - v1.y * v2.x;
        }

        public static float dot(ref Vector v0, ref Vector v1)
        {
            return v0.x * v1.x + v0.y * v1.y + v0.z * v1.z;
        }

        public static bool SegmentTriangleIntersect(Vector p0, Vector p1,
                                                    Vector t0, Vector t1, Vector t2,
                                                    out Vector I)
        {
            I.x = I.y = I.z = 0;

            Vector u; sub(out u, ref t1, ref t0); // triangle vector 1
            Vector v; sub(out v, ref t2, ref t0); // triangle vector 2
            Vector n; cross(out n, ref u, ref v); // triangle normal

            Vector dir; sub(out dir, ref p1, ref p0); // ray direction vector
            Vector w0; sub(out w0, ref p0, ref t0);
            float a = -dot(ref n, ref w0);
            float b = dot(ref n, ref dir);
            if (abs(b) < float.Epsilon) return false; // parallel

            // get intersect point of ray with triangle plane
            float r = a / b;
            if (r < 0.0) return false; // "before" p0
            if (r > 1.0) return false; // "after" p1

            Vector M; mul(out M, ref dir, r);
            add(out I, ref p0, ref M);// intersect point of line and plane

            // is I inside T?
            float uu = dot(ref u, ref u);
            float uv = dot(ref u, ref v);
            float vv = dot(ref v, ref v);
            Vector w; sub(out w, ref I, ref t0);
            float wu = dot(ref w, ref u);
            float wv = dot(ref w, ref v);
            float D = uv * uv - uu * vv;

            // get and test parametric coords
            float s = (uv * wv - vv * wu) / D;
            if (s < 0.0 || s > 1.0)        // I is outside T
                return false;

            float t = (uv * wu - uu * wv) / D;
            if (t < 0.0 || (s + t) > 1.0)  // I is outside T
                return false;

            return true;
        }

        public static float PointDistanceToSegment(Vector p0,
                                           Vector x1, Vector x2)
        {
            Vector L; sub(out L, ref x2, ref x1); // the segment vector
            float l2 = dot(ref L, ref L);   // square length of the segment

            Vector D; sub(out D, ref p0, ref x1);   // vector from point to segment start
            float d = dot(ref D, ref L);     // projection factor [x2-x1].[p0-x1]

            if (d < 0.0f) // closest to x1
                return VecLength(ref D);

            Vector E; mul(out E, ref L, d / l2); // intersect

            if (dot(ref E, ref L) > l2) // closest to x2
            {
                Vector L2; sub(out L2, ref D, ref L);
                return VecLength(ref L2);
            }

            Vector L3; sub(out L3, ref D, ref E);
            return VecLength(ref L3);
        }

        public static void GetTriangleNormal(Vector t0, Vector t1, Vector t2, out Vector normal)
        {
            Vector u; sub(out u, ref t1, ref t0); // triangle vector 1
            Vector v; sub(out v, ref t2, ref t0); // triangle vector 2
            cross(out normal, ref u, ref v); // triangle normal
            float l = VecLength(ref normal);
            div(out normal, ref normal, l);
        }

        public static float PointDistanceToTriangle(Vector p0,
                                                    Vector t0, Vector t1, Vector t2)
        {
            Vector u; sub(out u, ref t1, ref t0); // triangle vector 1
            Vector v; sub(out v, ref t2, ref t0); // triangle vector 2
            Vector n; cross(out n, ref u, ref v); // triangle normal
            n.x *= -1E6f;
            n.y *= -1E6f;
            n.z *= -1E6f;

            Vector intersect;
            bool hit = SegmentTriangleIntersect(p0, n, t0, t1, t2, out intersect);
            if (hit)
            {
                Vector L; sub(out L, ref intersect, ref p0);
                return VecLength(ref L);
            }

            float d0 = PointDistanceToSegment(p0, t0, t1);
            float d1 = PointDistanceToSegment(p0, t0, t1);
            float d2 = PointDistanceToSegment(p0, t0, t1);

            return min(d0, d1, d2);
        }

        private static void VecMin(Vector v0, Vector v1, Vector v2, out Vector min)
        {
            min.x = Utils.min(v0.x, v1.x, v2.x);
            min.y = Utils.min(v0.y, v1.y, v2.y);
            min.z = Utils.min(v0.z, v1.z, v2.z);
        }

        private static void VecMax(Vector v0, Vector v1, Vector v2, out Vector max)
        {
            max.x = Utils.max(v0.x, v1.x, v2.x);
            max.y = Utils.max(v0.y, v1.y, v2.y);
            max.z = Utils.max(v0.z, v1.z, v2.z);
        }

        public static bool TestBoxBoxIntersect(Vector box0_min, Vector box0_max,
                                               Vector box1_min, Vector box1_max)
        {
            if (box0_min.x > box1_max.x) return false;
            if (box0_min.y > box1_max.y) return false;
            if (box0_min.z > box1_max.z) return false;

            if (box1_min.x > box0_max.x) return false;
            if (box1_min.y > box0_max.y) return false;
            if (box1_min.z > box0_max.z) return false;

            return true;
        }

        public static bool TestTriangleBoxIntersect(Vector vertex0, Vector vertex1, Vector vertex2,
                                                    Vector boxcenter, Vector boxhalfsize)
        {
            int i = 0;
            float* pcenter = (float*)&boxcenter;
            float* phalf = (float*)&boxhalfsize;
            float* ptriangle0 = (float*)&vertex0;
            float* ptriangle1 = (float*)&vertex1;
            float* ptriangle2 = (float*)&vertex2;

            //int triBoxOverlap(float boxcenter[3],float boxhalfsize[3],float triverts[3][3]);
            try
            {
                i = ccode.triBoxOverlap(pcenter, phalf, ptriangle0, ptriangle1, ptriangle2);
            }
            catch (Exception e)
            {
                Console.WriteLine("WTF " + e);
            }
            if (i == 1) return true;
            return false;
            /*
            Vector min, max;
            min.x = ((vertex0.x < vertex1.x && vertex0.x < vertex2.x) ? vertex0.x : ((vertex1.x < vertex2.x) ? vertex1.x : vertex2.x));
            min.y = ((vertex0.y < vertex1.y && vertex0.y < vertex2.y) ? vertex0.y : ((vertex1.y < vertex2.y) ? vertex1.y : vertex2.y));
            min.z = ((vertex0.z < vertex1.z && vertex0.z < vertex2.z) ? vertex0.z : ((vertex1.z < vertex2.z) ? vertex1.z : vertex2.z));

            max.x = ((vertex0.x > vertex1.x && vertex0.x > vertex2.x) ? vertex0.x : ((vertex1.x > vertex2.x) ? vertex1.x : vertex2.x));
            max.y = ((vertex0.y > vertex1.y && vertex0.y > vertex2.y) ? vertex0.y : ((vertex1.y > vertex2.y) ? vertex1.y : vertex2.y));
            max.z = ((vertex0.z > vertex1.z && vertex0.z > vertex2.z) ? vertex0.z : ((vertex1.z > vertex2.z) ? vertex1.z : vertex2.z));

            bool outside = false;
            if (min.x > boxcenter.x + boxhalfsize.x) outside = true;
            if (max.x < boxcenter.x - boxhalfsize.x) outside = true;

            if (min.y > boxcenter.y + boxhalfsize.y) outside = true;
            if (max.y < boxcenter.y - boxhalfsize.y) outside = true;

            if (min.z > boxcenter.z + boxhalfsize.z) outside = true;
            if (max.z < boxcenter.z - boxhalfsize.z) outside = true;

            return !outside;*/
        }
    }

    public class SparseFloatMatrix3D<T> : SparseMatrix3D<T>
    {
        private float gridSize;

        public SparseFloatMatrix3D(float gridSize)
        {
            this.gridSize = gridSize;
        }

        private const float offset = 100000f;

        private int LocalToGrid(float f)
        {
            return (int)((f + offset) / gridSize);
        }

        public List<T> GetAllInCube(float min_x, float min_y, float min_z,
                                    float max_x, float max_y, float max_z)
        {
            int startx = LocalToGrid(min_x);
            int starty = LocalToGrid(min_y);
            int startz = LocalToGrid(min_z);

            int stopx = LocalToGrid(max_x);
            int stopy = LocalToGrid(max_y);
            int stopz = LocalToGrid(max_z);
            List<T> l = new List<T>();

            for (; startx <= stopx; startx++)
            {
                for (; starty <= stopy; starty++)
                {
                    for (; startz <= stopz; startz++)
                    {
                        if (base.IsSet(startx, starty, startz))
                            l.Add(base.Get(startx, starty, startz));
                    }
                }
            }
            return l;
        }

        public T Get(float x, float y, float z)
        {
            int ix = LocalToGrid(x);
            int iy = LocalToGrid(y);
            int iz = LocalToGrid(z);
            return base.Get((int)ix, (int)iy, (int)iz);
        }

        public bool IsSet(float x, float y, float z)
        {
            int ix = LocalToGrid(x);
            int iy = LocalToGrid(y);
            int iz = LocalToGrid(z);
            return base.IsSet((int)ix, (int)iy, (int)iz);
        }

        public void Set(float x, float y, float z, T val)
        {
            int ix = LocalToGrid(x);
            int iy = LocalToGrid(y);
            int iz = LocalToGrid(z);
            base.Set((int)ix, (int)iy, (int)iz, val);
        }
    }

    public class SparseFloatMatrix2D<T> : SparseMatrix2D<T>
    {
        private float gridSize;

        public SparseFloatMatrix2D(float gridSize)
            : base(0)
        {
            this.gridSize = gridSize;
        }

        public SparseFloatMatrix2D(float gridSize, int initialCapazity)
            : base(initialCapazity)
        {
            this.gridSize = gridSize;
        }

        public void GetGridStartAt(float x, float y, out float grid_x, out float grid_y)
        {
            grid_x = GridToLocal(LocalToGrid(x));
            grid_y = GridToLocal(LocalToGrid(y));
        }

        private const float offset = 100000f;

        public float GridToLocal(int grid)
        {
            return (float)grid * gridSize - offset;
        }

        public int LocalToGrid(float f)
        {
            return (int)((f + offset) / gridSize);
        }

        public List<T> GetAllInSquare(float min_x, float min_y,
                                      float max_x, float max_y)
        {
            int startx = LocalToGrid(min_x);
            int stopx = LocalToGrid(max_x);

            int starty = LocalToGrid(min_y);
            int stopy = LocalToGrid(max_y);

            List<T> l = new List<T>();

            for (int x = startx; x <= stopx; x++)
            {
                for (int y = starty; y <= stopy; y++)
                {
                    if (base.IsSet(x, y))
                        l.Add(base.Get(x, y));
                }
            }
            return l;
        }

        public T Get(float x, float y)
        {
            int ix = LocalToGrid(x);
            int iy = LocalToGrid(y);

            return base.Get((int)ix, (int)iy);
        }

        public bool IsSet(float x, float y)
        {
            int ix = LocalToGrid(x);
            int iy = LocalToGrid(y);

            return base.IsSet((int)ix, (int)iy);
        }

        public void Set(float x, float y, T val)
        {
            int ix = LocalToGrid(x);
            int iy = LocalToGrid(y);

            base.Set((int)ix, (int)iy, val);
        }

        public void Clear(float x, float y)
        {
            int ix = LocalToGrid(x);
            int iy = LocalToGrid(y);

            base.Clear((int)ix, (int)iy);
        }
    }

    public class SparseMatrix2D<T>
    {
        public SuperMap<XY, T> dic = new SuperMap<XY, T>();

        private bool last = false;
        private bool last_HasValue;
        private int last_x, last_y;
        private T last_value = default(T);

        public class XY
        {
            public int x, y;

            public XY(int x, int y)
            {
                this.x = x; this.y = y;
            }

            public override bool Equals(object obj)
            {
                XY other = (XY)obj;
                if (other == this) return true;
                return other.x == x && other.y == y;
            }

            public override int GetHashCode()
            {
                return x + y * 100000;
            }
        }

        public SparseMatrix2D(int initialCapacity)
        {
            dic = new SuperMap<XY, T>(initialCapacity);
        }

        public bool HasValue(int x, int y)
        {
            if (last && x == last_x && y == last_y) return last_HasValue;
            XY c = new XY(x, y);
            T r = default(T);
            bool b = dic.TryGetValue(c, out r);
            last = true;
            last_x = x; last_y = y; last_HasValue = b; last_value = r;
            return b;
        }

        public T Get(int x, int y)
        {
            if (last && x == last_x && y == last_y && last_HasValue) return last_value;
            XY c = new XY(x, y);
            T r = default(T);
            bool b = dic.TryGetValue(c, out r);
            last = true;
            last_x = x; last_y = y; last_HasValue = b; last_value = r;
            return r;
        }

        public void Set(int x, int y, T val)
        {
            XY c = new XY(x, y);
            if (dic.ContainsKey(c))
                dic.Remove(c);
            dic.Add(c, val);
            last = true;
            last_x = x; last_y = y; last_HasValue = true; last_value = val;
        }

        public bool IsSet(int x, int y)
        {
            return HasValue(x, y);
        }

        public void Clear(int x, int y)
        {
            XY c = new XY(x, y);
            if (dic.ContainsKey(c))
                dic.Remove(c);
            if (last_x == x && last_y == y) last = false;
        }

        public ICollection<T> GetAllElements()
        {
            return dic.GetAllValues();
        }
    }

    public class SparseMatrix3D<T>
    {
        private SuperMap<XYZ, T> dic = new SuperMap<XYZ, T>();

        private class XYZ
        {
            private int x, y, z;

            public XYZ(int x, int y, int z)
            {
                this.x = x; this.y = y; this.z = z;
            }

            public override bool Equals(object obj)
            {
                XYZ other = (XYZ)obj;
                if (other == this) return true;
                return other.x == x && other.y == y && other.z == z;
            }

            public override int GetHashCode()
            {
                return x + y * 1000 + z * 1000000;
            }
        }

        public T Get(int x, int y, int z)
        {
            XYZ c = new XYZ(x, y, z);
            T r = default(T);
            dic.TryGetValue(c, out r);
            return r;
        }

        public bool IsSet(int x, int y, int z)
        {
            XYZ c = new XYZ(x, y, z);
            T r = default(T);
            return dic.TryGetValue(c, out r);
        }

        public void Set(int x, int y, int z, T val)
        {
            XYZ c = new XYZ(x, y, z);
            if (dic.ContainsKey(c))
                dic.Remove(c);
            dic.Add(c, val);
        }

        public void Clear(int x, int y, int z)
        {
            XYZ c = new XYZ(x, y, z);
            if (dic.ContainsKey(c))
                dic.Remove(c);
        }

        public ICollection<T> GetAllElements()
        {
            return dic.GetAllValues();
        }
    }

    public class TrioArray<T>
    {
        private const int SIZE = 4096; // Max size if SIZE*SIZE = 16M

        // Jagged array
        // pointer chasing FTL

        // SIZE*(SIZE*3)
        private T[][] arrays = null;

        private static void getIndices(int index, out int i0, out int i1)
        {
            i1 = index % SIZE; index /= SIZE;
            i0 = index % SIZE;
        }

        private void allocateAt(int i0, int i1)
        {
            if (arrays == null) arrays = new T[SIZE][];

            T[] a1 = arrays[i0];
            if (a1 == null) { a1 = new T[SIZE * 3]; arrays[i0] = a1; }
        }

        public void SetSize(int new_size)
        {
            if (arrays == null) return;
            int i0, i1;
            getIndices(new_size, out i0, out i1);
            for (int i = i0 + 1; i < SIZE; i++)
                arrays[i] = null;
        }

        public void Set(int index, T x, T y, T z)
        {
            int i0, i1;
            getIndices(index, out i0, out i1);
            allocateAt(i0, i1);
            T[] innermost = arrays[i0];
            i1 *= 3;
            innermost[i1 + 0] = x;
            innermost[i1 + 1] = y;
            innermost[i1 + 2] = z;
        }

        public void Get(int index, out T x, out T y, out T z)
        {
            int i0, i1;
            getIndices(index, out i0, out i1);

            x = default(T);
            y = default(T);
            z = default(T);

            T[] a1 = arrays[i0];
            if (a1 == null) return;

            T[] innermost = arrays[i0];
            i1 *= 3;
            x = innermost[i1 + 0];
            y = innermost[i1 + 1];
            z = innermost[i1 + 2];
        }
    }

    public class QuadArray<T>
    {
        private const int SIZE = 1024 * 5; // Max size if SIZE*SIZE = 16M

        // Jagged array
        // pointer chasing FTL

        // SIZE*(SIZE*4)
        private T[][] arrays = null;

        private static void getIndices(int index, out int i0, out int i1)
        {
            i1 = index % SIZE; index /= SIZE;
            i0 = index % SIZE;
        }

        private void allocateAt(int i0, int i1)
        {
            if (arrays == null) arrays = new T[SIZE][];

            T[] a1 = arrays[i0];
            if (a1 == null) { a1 = new T[SIZE * 5]; arrays[i0] = a1; }
        }

        public void SetSize(int new_size)
        {
            if (arrays == null) return;
            int i0, i1;
            getIndices(new_size, out i0, out i1);
            for (int i = i0 + 1; i < SIZE; i++)
                arrays[i] = null;
        }

        public void Set(int index, T x, T y, T z, T w, T sequence)
        {
            int i0, i1;
            getIndices(index, out i0, out i1);
            allocateAt(i0, i1);
            T[] innermost = arrays[i0];
            i1 *= 5;
            innermost[i1 + 0] = x;
            innermost[i1 + 1] = y;
            innermost[i1 + 2] = z;
            innermost[i1 + 3] = w;
            innermost[i1 + 4] = sequence;
        }

        public void Get(int index, out T x, out T y, out T z, out T w, out T sequence)
        {
            int i0, i1;
            getIndices(index, out i0, out i1);

            x = default(T);
            y = default(T);
            z = default(T);
            w = default(T);
            sequence = default(T);

            T[] a1 = arrays[i0];
            if (a1 == null) return;

            T[] innermost = arrays[i0];
            i1 *= 5;
            x = innermost[i1 + 0];
            y = innermost[i1 + 1];
            z = innermost[i1 + 2];
            w = innermost[i1 + 3];
            sequence = innermost[i1 + 4];
        }
    }

    /// <summary>
    /// Default implementation of ISet.
    /// </summary>
    public class Set<T> : ICollection<T>
    {
        // Use an ISuperMap to implement.

        private SuperHash<T> dictionary = new SuperHash<T>(); // bool?!?!?

        public Set()
        {
        }

        public bool IsReadOnly { get { return false; } }

        public void CopyTo(T[] a, int off)
        {
            foreach (T e in this)
            {
                a[off++] = e;
            }
        }

        public Set(ICollection<T> objects)
        {
            AddRange(objects);
        }

        #region ISet Members

        public void Add(T anObject)
        {
            dictionary.Add(anObject);
        }

        public void AddRange(ICollection<T> objects)
        {
            foreach (T obj in objects) Add(obj);
        }

        public void Clear()
        {
            this.dictionary.Clear(0);
        }

        public bool Contains(T anObject)
        {
            return this.dictionary.Contains(anObject);
        }

        public bool Remove(T anObject)
        {
            return this.dictionary.Remove(anObject);
        }

        #endregion ISet Members

        #region ICollection Members

        public int Count
        {
            get
            {
                return this.dictionary.Count;
            }
        }

        #endregion ICollection Members

        #region IEnumerable Members

        public IEnumerator<T> GetEnumerator()
        {
            return dictionary.GetAll().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IEnumerable Members
    }

    /// <summary>
    /// Represents an item stored in a priority queue.
    /// </summary>
    /// <typeparam name="TValue">The type of object in the queue.</typeparam>
    /// <typeparam name="TPriority">The type of the priority field.</typeparam>

    internal struct PriorityQueueItem<TValue, TPriority>
    {
        private TValue value;
        private TPriority priority;

        public PriorityQueueItem(TValue val, TPriority pri)
        {
            this.value = val;
            this.priority = pri;
        }

        /// <summary>
        /// Gets the value of this PriorityQueueItem.
        /// </summary>
        public TValue Value
        {
            get { return value; }
        }

        /// <summary>
        /// Gets the priority associated with this PriorityQueueItem.
        /// </summary>
        public TPriority Priority
        {
            get { return priority; }
        }
    }

    /// <summary>
    /// Represents a binary heap priority queue.
    /// </summary>
    /// <typeparam name="TValue">The type of object in the queue.</typeparam>
    /// <typeparam name="TPriority">The type of the priority field.</typeparam>

    public class PriorityQueue<TValue, TPriority>
    {
        private PriorityQueueItem<TValue, TPriority>[] items;

        private const Int32 DefaultCapacity = 16;
        private Int32 capacity;
        private Int32 numItems;

        private Comparison<TPriority> compareFunc;

        public PriorityQueue()
            : this(DefaultCapacity, Comparer<TPriority>.Default)
        {
        }

        public PriorityQueue(Int32 initialCapacity)
            : this(initialCapacity, Comparer<TPriority>.Default)
        {
        }

        public PriorityQueue(IComparer<TPriority> comparer)
            : this(DefaultCapacity, comparer)
        {
        }

        public PriorityQueue(int initialCapacity, IComparer<TPriority> comparer)
        {
            Init(initialCapacity, new Comparison<TPriority>(comparer.Compare));
        }

        public PriorityQueue(Comparison<TPriority> comparison)
            : this(DefaultCapacity, comparison)
        {
        }

        public PriorityQueue(int initialCapacity, Comparison<TPriority> comparison)
        {
            Init(initialCapacity, comparison);
        }

        // Initializes the queue
        private void Init(int initialCapacity, Comparison<TPriority> comparison)
        {
            numItems = 0;
            compareFunc = comparison;
            SetCapacity(initialCapacity);
        }

        public int Count
        {
            get { return numItems; }
        }

        public int Capacity
        {
            get { return items.Length; }
            set { SetCapacity(value); }
        }

        // Set the queue's capacity.
        private void SetCapacity(int newCapacity)
        {
            int newCap = newCapacity;
            if (newCap < DefaultCapacity)
                newCap = DefaultCapacity;

            // throw exception if newCapacity < numItems
            if (newCap < numItems)
                throw new ArgumentOutOfRangeException("newCapacity", "New capacity is less than Count");

            this.capacity = newCap;
            if (items == null)
            {
                // Initial allocation.
                items = new PriorityQueueItem<TValue, TPriority>[newCap];
                return;
            }

            // Resize the array.
            Array.Resize(ref items, newCap);
        }

        public void Enqueue(TValue value, TPriority priority)
        {
            if (numItems == capacity)
            {
                // need to increase capacity
                // grow by 50 percent
                SetCapacity((3 * Capacity) / 2);
            }

            // Create the new item
            PriorityQueueItem<TValue, TPriority> newItem =
                new PriorityQueueItem<TValue, TPriority>(value, priority);
            int i = numItems;
            ++numItems;

            // and insert it into the heap.
            while ((i > 0) && (compareFunc(items[i / 2].Priority, newItem.Priority) < 0))
            {
                items[i] = items[i / 2];
                i /= 2;
            }
            items[i] = newItem;
        }

        // Remove a node at a particular position in the queue.
        private PriorityQueueItem<TValue, TPriority> RemoveAt(Int32 index)
        {
            // remove an item from the heap
            PriorityQueueItem<TValue, TPriority> o = items[index];
            PriorityQueueItem<TValue, TPriority> tmp = items[numItems - 1];
            items[--numItems] = default(PriorityQueueItem<TValue, TPriority>);
            if (numItems > 0)
            {
                int i = index;
                int j = i + 1;
                while (i < Count / 2)
                {
                    if ((j < Count - 1) && (compareFunc(items[j].Priority, items[j + 1].Priority) < 0))
                    {
                        j++;
                    }
                    if (compareFunc(items[j].Priority, tmp.Priority) <= 0)
                    {
                        break;
                    }
                    items[i] = items[j];
                    i = j;
                    j *= 2;
                }
                items[i] = tmp;
            }
            return o;
        }

        public TValue Dequeue(out TPriority prio)
        {
            if (Count == 0)
                throw new InvalidOperationException("The queue is empty");
            PriorityQueueItem<TValue, TPriority> item = RemoveAt(0);
            prio = item.Priority;
            return item.Value;
        }

        public void Remove(TValue item, IEqualityComparer<TValue> comparer)
        {
            // need to find the PriorityQueueItem that has the Data value of o
            for (int index = 0; index < numItems; ++index)
            {
                if (comparer.Equals(item, items[index].Value))
                {
                    RemoveAt(index);
                    return;
                }
            }
            //throw new ApplicationException("The specified itemm is not in the queue.");
        }

        public void Remove(TValue item)
        {
            Remove(item, EqualityComparer<TValue>.Default);
        }

        /// <returns>The object at the beginning of the queue.</returns>
        private PriorityQueueItem<TValue, TPriority> Peek()
        {
            if (Count == 0)
                throw new InvalidOperationException("The queue is empty");
            return items[0];
        }

        /// <summary>
        /// Removes all objects from the queue.
        /// </summary>
        public void Clear()
        {
            numItems = 0;
            TrimExcess();
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the Queue,
        /// if that number is less than 90 percent of current capacity.
        /// </summary>
        public void TrimExcess()
        {
            if (numItems < (0.9 * capacity))
                SetCapacity(numItems);
        }

        /// <summary>
        /// Determines whether an element is in the queue.
        /// </summary>
        /// <param name="item">The object to locate in the queue.</param>
        /// <returns>True if item found in the queue.  False otherwise.</returns>
        public bool Contains(TValue item)
        {
            EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;
            // need to find the PriorityQueueItem that has the Data value of o
            for (int index = 0; index < numItems; ++index)
            {
                if (comparer.Equals(item, items[index].Value))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class SuperMap<TKey, TValue>
    {
        public class Entry
        {
            public TKey key;
            public TValue value;
            public Entry next;

            public Entry(TKey k, TValue v)
            {
                key = k;
                value = v;
            }
        }

        // table of good has size tables
        private static uint[] sizes = {
           89,
           179,
           359,
           719,
           1439,
           2879,
           5759,
           11519,
           23039,
           46079,
           92159,
           184319,
           368639,
           737279,
           1474559,
           2949119,
           5898239,
           11796479,
           23592959,
           47185919,
           94371839,
           188743679,
           377487359,
           754974719,
           1509949439
         };

        private int elements = 0; // elements in hash
        private int size_table_entry = 0;
        private Entry[] array;

        public SuperMap()
        {
            Clear(16);
        }

        public SuperMap(int initialCapacity)
        {
            Clear(initialCapacity);
        }

        private static uint GetEntryIn(Entry[] array, TKey key)
        {
            return (uint)key.GetHashCode() % (uint)array.Length;
        }

        private static void AddToArrayNoCheck(Entry[] array, Entry e)
        {
            uint key = GetEntryIn(array, e.key);
            e.next = array[key];
            array[key] = e;
        }

        private static bool HasValue(Entry[] array, TKey k, TValue val)
        {
            uint key = GetEntryIn(array, k);
            Entry rover = array[key];
            while (rover != null)
            {
                if (rover.value.Equals(val))
                    return true;
                rover = rover.next;
            }
            return false;
        }

        private static void AddToArray(Entry[] array, Entry e)
        {
            uint key = GetEntryIn(array, e.key);
            TValue val = e.value;
            // check for existance
            Entry rover = array[key];
            while (rover != null)
            {
                if (rover.key.Equals(e.key))
                    return;

                rover = rover.next;
            }
            e.next = array[key];
            array[key] = e;
        }

        private void MakeLarger()
        {
            size_table_entry++;
            uint new_size = sizes[size_table_entry];
            Entry[] new_array = new Entry[sizes[size_table_entry]];

            // add all old stuff to the new one

            for (int i = 0; i < array.Length; i++)
            {
                Entry rover = array[i];
                while (rover != null)
                {
                    Entry next = rover.next;
                    AddToArrayNoCheck(new_array, rover);
                    rover = next;
                }
            }
            array = new_array;
        }

        private void MakeSmaller()
        {
            if (size_table_entry == 0) return;
            size_table_entry--;
            uint new_size = sizes[size_table_entry];
            Entry[] new_array = new Entry[sizes[size_table_entry]];

            // add all old stuff to the new one

            for (int i = 0; i < array.Length; i++)
            {
                Entry rover = array[i];
                while (rover != null)
                {
                    Entry next = rover.next;
                    AddToArrayNoCheck(new_array, rover);
                    rover = next;
                }
            }
            array = new_array;
        }

        public void Add(TKey k, TValue v)
        {
            AddToArray(array, new Entry(k, v));
            elements++;
            if (elements > array.Length * 2)
            {
                MakeLarger();
            }
        }

        public void Clear(int initialCapacity)
        {
            elements = 0;
            size_table_entry = 0;
            for (size_table_entry = 0; sizes[size_table_entry] < initialCapacity; size_table_entry++) ;
            array = new Entry[sizes[size_table_entry]];
        }

        public bool ContainsValue(TValue val)
        {
            throw new Exception("ContainsValue not implemented");
        }

        public bool ContainsKey(TKey k)
        {
            uint key = GetEntryIn(array, k);
            Entry rover = array[key];
            while (rover != null)
            {
                if (rover.next == rover)
                {
                    Console.WriteLine("lsdfjlskfjkl>");
                }
                if (rover.key.Equals(k))
                    return true;
                rover = rover.next;
            }
            return false;
        }

        public bool TryGetValue(TKey k, out TValue v)
        {
            uint key = GetEntryIn(array, k);
            Entry rover = array[key];
            while (rover != null)
            {
                if (rover.next == rover)
                {
                    Console.WriteLine("ksjdflksdjf");
                }
                if (rover.key.Equals(k))
                {
                    v = rover.value;
                    return true;
                }
                rover = rover.next;
            }
            v = default(TValue);
            return false;
        }

        public bool Remove(TKey k)
        {
            uint key = GetEntryIn(array, k);
            Entry rover = array[key];
            Entry prev = null;
            while (rover != null)
            {
                if (rover.key.Equals(k))
                {
                    if (prev == null)
                        array[key] = rover.next;
                    else
                        prev.next = rover.next;
                    elements--;
                    return true;
                }
                rover = rover.next;
            }
            return false;
        }

        public ICollection<TValue> GetAllValues()
        {
            List<TValue> list = new List<TValue>();
            for (int i = 0; i < array.Length; i++)
            {
                Entry rover = array[i];
                while (rover != null)
                {
                    list.Add(rover.value);
                    rover = rover.next;
                }
            }
            return list;
        }

        public ICollection<TKey> GetAllKeys()
        {
            List<TKey> list = new List<TKey>();
            for (int i = 0; i < array.Length; i++)
            {
                Entry rover = array[i];
                while (rover != null)
                {
                    list.Add(rover.key);
                    rover = rover.next;
                }
            }
            return list;
        }

        public int Count
        {
            get
            {
                return elements;
            }
        }
    }

    public class SuperHash<T>
    {
        public class Entry
        {
            public T value;
            public Entry next;

            public Entry(T v)
            {
                value = v;
            }
        }

        // table of good has size tables
        private static uint[] sizes = {
           89,
           179,
           359,
           719,
           1439,
           2879,
           5759,
           11519,
           23039,
           46079,
           92159,
           184319,
           368639,
           737279,
           1474559,
           2949119,
           5898239,
           11796479,
           23592959,
           47185919,
           94371839,
           188743679,
           377487359,
           754974719,
           1509949439
         };

        private int elements = 0; // elements in hash
        private int size_table_entry = 0;
        private Entry[] array;

        public SuperHash()
        {
            Clear(16);
        }

        public SuperHash(int initialCapacity)
        {
            Clear(initialCapacity);
        }

        private static uint GetEntryIn(Entry[] array, T key)
        {
            return (uint)key.GetHashCode() % (uint)array.Length;
        }

        private static void AddToArrayNoCheck(Entry[] array, Entry e)
        {
            uint key = GetEntryIn(array, e.value);
            e.next = array[key];
            array[key] = e;
        }

        private static bool HasValue(Entry[] array, T k)
        {
            uint key = GetEntryIn(array, k);
            Entry rover = array[key];
            while (rover != null)
            {
                if (rover.value.Equals(k))
                    return true;
                rover = rover.next;
            }
            return false;
        }

        private static void AddToArray(Entry[] array, Entry e)
        {
            uint key = GetEntryIn(array, e.value);
            T val = e.value;
            // check for existance
            Entry rover = array[key];
            while (rover != null)
            {
                if (rover.value.Equals(e.value))
                    return;

                rover = rover.next;
            }
            e.next = array[key];
            array[key] = e;
        }

        private void MakeLarger()
        {
            size_table_entry++;
            uint new_size = sizes[size_table_entry];
            Entry[] new_array = new Entry[sizes[size_table_entry]];

            // add all old stuff to the new one

            for (int i = 0; i < array.Length; i++)
            {
                Entry rover = array[i];
                while (rover != null)
                {
                    Entry next = rover.next;
                    AddToArrayNoCheck(new_array, rover);
                    rover = next;
                }
            }
            array = new_array;
        }

        private void MakeSmaller()
        {
            if (size_table_entry == 0) return;
            size_table_entry--;
            uint new_size = sizes[size_table_entry];
            Entry[] new_array = new Entry[sizes[size_table_entry]];

            // add all old stuff to the new one

            for (int i = 0; i < array.Length; i++)
            {
                Entry rover = array[i];
                while (rover != null)
                {
                    Entry next = rover.next;
                    AddToArrayNoCheck(new_array, rover);
                    rover = next;
                }
            }
            array = new_array;
        }

        public void Add(T k)
        {
            AddToArray(array, new Entry(k));
            elements++;
            if (elements > array.Length * 2)
            {
                MakeLarger();
            }
        }

        public void Clear(int initialCapacity)
        {
            elements = 0;
            size_table_entry = 0;
            for (size_table_entry = 0; sizes[size_table_entry] < initialCapacity; size_table_entry++) ;
            array = new Entry[sizes[size_table_entry]];
        }

        public bool Contains(T k)
        {
            uint key = GetEntryIn(array, k);
            Entry rover = array[key];
            while (rover != null)
            {
                if (rover.next == rover)
                {
                    Console.WriteLine("lsdfjlskfjkl>");
                }
                if (rover.value.Equals(k))
                    return true;
                rover = rover.next;
            }
            return false;
        }

        public bool Remove(T k)
        {
            uint key = GetEntryIn(array, k);
            Entry rover = array[key];
            Entry prev = null;
            while (rover != null)
            {
                if (rover.value.Equals(k))
                {
                    if (prev == null)
                        array[key] = rover.next;
                    else
                        prev.next = rover.next;
                    elements--;
                    return true;
                }
                rover = rover.next;
            }
            return false;
        }

        public ICollection<T> GetAll()
        {
            List<T> list = new List<T>();
            for (int i = 0; i < array.Length; i++)
            {
                Entry rover = array[i];
                while (rover != null)
                {
                    list.Add(rover.value);
                    rover = rover.next;
                }
            }
            return list;
        }

        public int Count
        {
            get
            {
                return elements;
            }
        }
    }
}