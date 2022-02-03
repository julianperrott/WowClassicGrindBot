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

*/

using System;
using System.Collections.Generic;

namespace PatherPath.Graph
{
    public class GraphChunk
    {
        private Logger logger;

        public const int CHUNK_SIZE = 512;

        private float base_x, base_y;
        public int ix, iy;
        public bool modified;
        public long LRU;

        private Spot[,] spots;

        public GraphChunk(float base_x, float base_y, int ix, int iy, Logger logger)
        {
            this.logger = logger;
            this.base_x = base_x;
            this.base_y = base_y;
            this.ix = ix;
            this.iy = iy;
            spots = new Spot[CHUNK_SIZE, CHUNK_SIZE];
            modified = false;
        }

        public void Clear()
        {
            foreach (Spot s in spots)
                if (s != null)
                    s.traceBack = null;

            spots = null;
        }

        private void LocalCoords(float x, float y, out int ix, out int iy)
        {
            ix = (int)(x - base_x);
            iy = (int)(y - base_y);
        }

        public Spot GetSpot2D(float x, float y)
        {
            int ix, iy;
            LocalCoords(x, y, out ix, out iy);
            Spot s = spots[ix, iy];
            return s;
        }

        public Spot GetSpot(float x, float y, float z)
        {
            Spot s = GetSpot2D(x, y);

            while (s != null && !s.IsCloseZ(z))
            {
                s = s.next;
            }

            return s;
        }

        // return old spot at conflicting poision
        // or the same as passed the function if all was ok
        public Spot AddSpot(Spot s)
        {
            Spot old = GetSpot(s.X, s.Y, s.Z);
            if (old != null)
                return old;
            int x, y;

            s.chunk = this;

            LocalCoords(s.X, s.Y, out x, out y);

            s.next = spots[x, y];
            spots[x, y] = s;
            modified = true;
            return s;
        }

        public List<Spot> GetAllSpots()
        {
            List<Spot> l = new List<Spot>();
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    Spot s = spots[x, y];
                    while (s != null)
                    {
                        l.Add(s);
                        s = s.next;
                    }
                }
            }
            return l;
        }

        private string FileName()
        {
            return String.Format("c_{0,3:000}_{1,3:000}.bin", ix, iy);
        }

        private const uint FILE_MAGIC = 0x12341234;
        private const uint FILE_ENDMAGIC = 0x43214321;
        private const uint SPOT_MAGIC = 0x53504f54;

        // Per spot:
        // uint32 magic
        // uint32 reserved;
        // uint32 flags;
        // float x;
        // float y;
        // float z;
        // uint32 no_paths
        //   for each path
        //     float x;
        //     float y;
        //     float z;

        public bool Load(string baseDir)
        {
            string fileName = FileName();
            string filenamebin = baseDir + fileName;

            System.IO.Stream stream = null;
            System.IO.BinaryReader file = null;
            int n_spots = 0;
            int n_steps = 0;
            try
            {
                if (!System.IO.Directory.Exists(filenamebin) || !System.IO.File.Exists(filenamebin))
                    return false;

                stream = System.IO.File.OpenRead(filenamebin);
                if (stream != null)
                {
                    file = new System.IO.BinaryReader(stream);
                    if (file != null)
                    {
                        uint magic = file.ReadUInt32();
                        if (magic == FILE_MAGIC)
                        {
                            uint type;
                            while ((type = file.ReadUInt32()) != FILE_ENDMAGIC)
                            {
                                n_spots++;
                                uint reserved = file.ReadUInt32();
                                uint flags = file.ReadUInt32();
                                float x = file.ReadSingle();
                                float y = file.ReadSingle();
                                float z = file.ReadSingle();
                                uint n_paths = file.ReadUInt32();
                                if (x != 0 && y != 0)
                                {
                                    Spot s = new Spot(x, y, z);
                                    s.flags = flags;

                                    for (uint i = 0; i < n_paths; i++)
                                    {
                                        n_steps++;
                                        float sx = file.ReadSingle();
                                        float sy = file.ReadSingle();
                                        float sz = file.ReadSingle();
                                        s.AddPathTo(sx, sy, sz);
                                    }
                                    AddSpot(s);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.IO.FileNotFoundException e)
            {
                logger.Debug(e.Message);
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                logger.Debug(e.Message);
            }
            catch (Exception e)
            {
                logger.Debug(e.Message);
            }

            if (file != null)
            {
                file.Close();
            }
            if (stream != null)
            {
                stream.Close();
            }

            Log("Loaded " + fileName + " " + n_spots + " spots " + n_steps + " steps");

            modified = false;
            return false;
        }

        bool saveEnabled = true;

        public bool Save(string baseDir)
        {
            if (!modified)
                return true; // doh

            if (!saveEnabled)
            {
                //don't save
                return true;
            }

            string fileName = FileName();
            string filename = baseDir + fileName;

            System.IO.Stream fileout = null;
            System.IO.BinaryWriter file = null;

            //try {
            if (!System.IO.Directory.Exists(baseDir))
                System.IO.Directory.CreateDirectory(baseDir);
            //} catch { };

            int n_spots = 0;
            int n_steps = 0;
            try
            {
                fileout = System.IO.File.Create(filename + ".new");

                if (fileout != null)
                {
                    file = new System.IO.BinaryWriter(fileout);

                    if (file != null)
                    {
                        file.Write(FILE_MAGIC);

                        List<Spot> spots = GetAllSpots();
                        foreach (Spot s in spots)
                        {
                            file.Write(SPOT_MAGIC);
                            file.Write((uint)0); // reserved
                            file.Write((uint)s.flags);
                            file.Write((float)s.X);
                            file.Write((float)s.Y);
                            file.Write((float)s.Z);
                            uint n_paths = (uint)s.n_paths;
                            file.Write((uint)n_paths);
                            for (uint i = 0; i < n_paths; i++)
                            {
                                uint off = i * 3;
                                file.Write((float)s.paths[off]);
                                file.Write((float)s.paths[off + 1]);
                                file.Write((float)s.paths[off + 2]);
                                n_steps++;
                            }
                            n_spots++;
                        }
                        file.Write(FILE_ENDMAGIC);
                    }

                    if (file != null)
                    {
                        file.Close();
                        file = null;
                    }

                    if (fileout != null)
                    {
                        fileout.Close();
                        fileout = null;
                    }

                    String old = filename + ".bak";

                    if (System.IO.File.Exists(old))
                        System.IO.File.Delete(old);
                    if (System.IO.File.Exists(filename))
                        System.IO.File.Move(filename, old);
                    System.IO.File.Move(filename + ".new", filename);
                    if (System.IO.File.Exists(old))
                        System.IO.File.Delete(old);

                    modified = false;
                }
                else
                {
                    Log("Save failed");
                }
                Log("Saved " + fileName + " " + n_spots + " spots " + n_steps + " steps");
            }
            catch (Exception e)
            {
                Log("Save failed " + e);
            }

            if (file != null)
            {
                file.Close();
                file = null;
            }

            if (fileout != null)
            {
                fileout.Close();
                fileout = null;
            }

            return false;
        }

        private void Log(String s)
        {
            //logger.WriteLine(s);
            logger.Debug(s);
        }
    }
}