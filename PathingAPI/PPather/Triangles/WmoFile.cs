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

using PatherPath;
using System;
using System.Collections.Generic;
using System.IO;

namespace Wmo
{
    internal class Dbg
    {
        public static void Log(string s)
        {
            // System.Diagnostics.Debug.Write(s);
        }

        public static void LogLine(string s)
        {
            // logger.WriteLine(s);
        }
    }

    internal unsafe class ChunkReader
    {
        public static float TILESIZE = 533.33333f;
        public static float ZEROPOINT = (32.0f * (TILESIZE));
        public static float CHUNKSIZE = ((TILESIZE) / 16.0f);
        public static float UNITSIZE = (CHUNKSIZE / 8.0f);

        public static uint ToBin(String s)
        {
            char[] ca = s.ToCharArray();
            uint b0 = (uint)ca[0];
            uint b1 = (uint)ca[1];
            uint b2 = (uint)ca[2];
            uint b3 = (uint)ca[3];
            uint r = b3 | (b2 << 8) | (b1 << 16) | (b0 << 24);
            return r;
        }

        public static string ReadString(System.IO.BinaryReader file)
        {
            char[] bytes = new char[1024];
            int len = 0;
            sbyte b = 0;
            do
            {
                b = file.ReadSByte();
                bytes[len] = (char)b;
                len++;
            } while (b != 0);

            string s = new string(bytes, 0, len - 1);
            return s;
        }

        public static string ExtractString(byte[] b, int off)
        {
            string s;
            fixed (byte* bp = b)
            {
                sbyte* sp = (sbyte*)bp;
                sp += off;
                s = new string(sp);
            }

            return s;
        }

        public static uint MWMO = ToBin("MWMO");
        public static uint MODF = ToBin("MODF");
        public static uint MAIN = ToBin("MAIN");
        public static uint MPHD = ToBin("MPHD");

        public static uint CBDW = ToBin("CBDW");

        public static uint MVER = ToBin("MVER");
        public static uint MOGI = ToBin("MOGI");
        public static uint MOHD = ToBin("MOHD");
        public static uint MOTX = ToBin("MOTX");
        public static uint MOMT = ToBin("MOMT");
        public static uint MOGN = ToBin("MOGN");
        public static uint MOLT = ToBin("MOLT");
        public static uint MODN = ToBin("MODN");
        public static uint MODS = ToBin("MODS");
        public static uint MODD = ToBin("MODD");
        public static uint MOSB = ToBin("MOSB");
        public static uint MOPV = ToBin("MOPV");
        public static uint MOPR = ToBin("MOPR");
        public static uint MFOG = ToBin("MFOG");

        public static uint MOGP = ToBin("MOGP");
        public static uint MOPY = ToBin("MOPY");
        public static uint MOVI = ToBin("MOVI");

        public static uint MOVT = ToBin("MOVT");
        public static uint MONR = ToBin("MONR");
        public static uint MOLR = ToBin("MOLR");
        public static uint MODR = ToBin("MODR");
        public static uint MOBA = ToBin("MOBA");
        public static uint MOCV = ToBin("MOCV");
        public static uint MLIQ = ToBin("MLIQ");
        public static uint MOBN = ToBin("MOBN");
        public static uint MOBR = ToBin("MOBR");

        public static uint MCIN = ToBin("MCIN");
        public static uint MTEX = ToBin("MTEX");
        public static uint MMDX = ToBin("MMDX");

        public static uint MDDF = ToBin("MDDF");
        public static uint MCNK = ToBin("MCNK");

        public static uint MCNR = ToBin("MCNR");
        public static uint MCRF = ToBin("MCRF");
        public static uint MCVT = ToBin("MCVT");
        public static uint MCLY = ToBin("MCLY");
        public static uint MCSH = ToBin("MCSH");
        public static uint MCAL = ToBin("MCAL");
        public static uint MCLQ = ToBin("MCLQ");
        public static uint MH2O = ToBin("MH2O");
        public static uint MCSE = ToBin("MCSE");
    }

    public class DBC
    {
        public uint recordCount;
        public uint fieldCount;
        public uint recordSize;
        public uint stringSize;

        public uint[] rawRecords;
        public byte[] strings;

        public uint GetUint(int record, int id)
        {
            int recoff = (int)(record * fieldCount + id);
            return rawRecords[recoff];
        }

        public int GetInt(int record, int id)
        {
            int recoff = (int)(record * fieldCount + id);
            return (int)rawRecords[recoff];
        }

        public string GetString(int record, int id)
        {
            int recoff = (int)(record * fieldCount + id);
            return ChunkReader.ExtractString(strings, (int)rawRecords[recoff]);
        }
    }

    public class Vec3D
    {
        public float x, y, z;

        public Vec3D(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            return x + " " + y + " " + z;
        }
    }

    public class WMOManager : Manager<WMO>
    {
        private StormDll.ArchiveSet set;
        private ModelManager modelmanager;
        private DataConfig dataConfig;

        public WMOManager(StormDll.ArchiveSet set, ModelManager modelmanager, int maxItems, DataConfig dataConfig)
            : base(maxItems)
        {
            this.set = set;
            this.modelmanager = modelmanager;
            this.dataConfig = dataConfig;
        }

        public override WMO Load(String path)
        {
            string localPath = Path.Join(dataConfig.PPather, "wmo.tmp");
            Dbg.Log(" wmo");
            set.ExtractFile(path, localPath);
            WMO w = new WMO();
            w.fileName = path;

            WmoRootFile wrf = new WmoRootFile(localPath, w, modelmanager);

            for (int i = 0; i < w.groups.Length; i++)
            {
                string part = path.Substring(0, path.Length - 4);
                string gf = String.Format("{0}_{1,3:000}.wmo", part, i);
                Dbg.Log(" wmog");
                set.ExtractFile(gf, localPath);
                new WmoGroupFile(w.groups[i], localPath);
            }
            return w;
        }
    }

    public class WMOInstance
    {
        public WMO wmo;
        public int id;
        public Vec3D pos, pos2, pos3;
        public Vec3D dir;
        public int d2, d3;
        public int doodadset;

        public WMOInstance(WMO wmo, System.IO.BinaryReader file)
        {
            // read X bytes from file
            this.wmo = wmo;

            id = file.ReadInt32();
            {
                float f0 = file.ReadSingle();
                float f1 = file.ReadSingle();
                float f2 = file.ReadSingle();
                pos = new Vec3D(f0, f1, f2);
            }
            {
                float f0 = file.ReadSingle();
                float f1 = file.ReadSingle();
                float f2 = file.ReadSingle();
                dir = new Vec3D(f0, f1, f2);
            }

            {
                float f0 = file.ReadSingle();
                float f1 = file.ReadSingle();
                float f2 = file.ReadSingle();
                pos2 = new Vec3D(f0, f1, f2);
            }
            {
                float f0 = file.ReadSingle();
                float f1 = file.ReadSingle();
                float f2 = file.ReadSingle();
                pos3 = new Vec3D(f0, f1, f2);
            }

            d2 = file.ReadInt32();
            doodadset = file.ReadInt16();
            short crap = file.ReadInt16();
        }
    }

    public struct DoodadSet
    {
        public uint firstInstance;
        public uint nInstances;
    }

    public class WMO
    {
        public string fileName = "";
        public WMOGroup[] groups;

        //int nTextures, nGroups, nP, nLight nX;
        public Vec3D v1, v2; // bounding box

        public byte[] MODNraw;
        public uint nModels;
        public uint nDoodads;
        public uint nDoodadSets;

        public DoodadSet[] doodads;
        public ModelInstance[] doodadInstances;

        //List<string> textures;
        //List<string> models;
        //Vector<ModelInstance> modelis;

        //Vector<WMOLight> lights;
        //List<WMOPV> pvs;
        //List<WMOPR> prs;

        //Vector<WMOFog> fogs;

        //Vector<WMODoodadSet> doodadsets;
    }

    public abstract class Manager<T>
    {
        private Dictionary<string, T> items = new Dictionary<string, T>();
        private Dictionary<string, int> items_LRU = new Dictionary<string, int>();

        private int NOW;
        private int maxItems;

        public Manager(int maxItems)
        {
            this.maxItems = maxItems;
        }

        public abstract T Load(String path);

        private void EvictIfNeeded()
        {
            string toEvict = null;
            int toEvictLRU = Int32.MaxValue;
            if (items.Count > maxItems)
            {
                foreach (string path in items_LRU.Keys)
                {
                    int LRU = items_LRU[path];
                    if (LRU < toEvictLRU)
                    {
                        toEvictLRU = LRU;
                        toEvict = path;
                    }
                }
            }
            if (toEvict != null)
            {
                //                logger.WriteLine("Drop item : " + toEvict);
                items.Remove(toEvict);
                items_LRU.Remove(toEvict);
            }
        }

        public T AddAndLoadIfNeeded(string path)
        {
            path = path.ToLower();
            T w = Get(path);
            if (w == null)
            {
                EvictIfNeeded();
                w = Load(path);
                //Dbg.LogLine("need " + path);
                if (w != null)
                    Add(path, w);
            }

            items_LRU.Remove(path);
            items_LRU.Add(path, NOW++);
            return w;
        }

        public void Add(string path, T wmo)
        {
            items.Add(path, wmo);
        }

        public T Get(string path)
        {
            T r;
            if (items.TryGetValue(path, out r))
                return r;
            return default(T);
        }
    }

    public class ModelManager : Manager<Model>
    {
        private StormDll.ArchiveSet set;
        private DataConfig dataConfig;

        public ModelManager(StormDll.ArchiveSet set, int maxModels, DataConfig dataConfig)
            : base(maxModels)
        {
            this.set = set;
            this.dataConfig = dataConfig;
        }

        public override Model Load(String path)
        {
            // change .mdx to .m2
            //string file=path.Substring(0, path.Length-4)+".m2";

            string file = path;
            if (Path.GetExtension(path).Equals(".mdx"))
            {
                file = Path.ChangeExtension(path, ".m2");
            }
            else if (Path.GetExtension(path).Equals(".mdl"))
            {
                file = Path.ChangeExtension(path, ".m2");
            }

            //logger.WriteLine("Load model " + path);
            string localPath = Path.Join(dataConfig.PPather, "model.tmp");
            Dbg.Log(" m");
            if (set.ExtractFile(file, localPath))
            {
                Model w = new Model();
                w.fileName = file;
                ModelFile wrf = new ModelFile(localPath, w);
                return w;
            }
            return null;
        }
    }

    public class ModelInstance
    {
        public Model model;
        public Vec3D pos;
        public Vec3D dir;
        public float w;

        //bool w_is_set = false;
        public float sc;

        public ModelInstance(Model m, System.IO.BinaryReader file)
        {
            model = m;
            uint d1 = file.ReadUInt32();
            {
                float f0 = file.ReadSingle();
                float f1 = file.ReadSingle();
                float f2 = file.ReadSingle();
                pos = new Vec3D(f0, f1, f2);
            }
            {
                float f0 = file.ReadSingle();
                float f1 = file.ReadSingle();
                float f2 = file.ReadSingle();
                dir = new Vec3D(f0, f1, f2);
            }
            uint scale = file.ReadUInt32();
            sc = (float)scale / 1024.0f;
        }

        public ModelInstance(Model m, Vec3D pos, Vec3D dir, float sc, float w)
        {
            this.model = m;
            this.pos = pos;
            this.dir = dir;
            this.sc = sc;
            this.w = w;
            //w_is_set = true;
        }
    }

    public class ModelView
    {
        public UInt16[] indexList;
        public uint offIndex;

        public UInt16[] triangleList;
        public uint offTriangle;
    }

    public class Model
    {
        public string fileName = "";
        // 4 bytes Magic
        // 4 bytes version
        //uint model_name_length; // (including \0);
        //uint model_name_offset;

        public float[] vertices; // 3 per vertex

        public ModelView[] view;

        public float[] boundingVertices; // 3 per vertex
        public UInt16[] boundingTriangles;
    }

    public class ModelFile
    {
        private System.IO.Stream model_stream;
        private System.IO.BinaryReader file;
        private Model model;

        public ModelFile(string path, Model m)
        {
            this.model = m;
            model_stream = System.IO.File.OpenRead(path);
            file = new System.IO.BinaryReader(model_stream);
            try
            {
                ReadHeader();
            }
            catch (System.IO.EndOfStreamException)
            {
            }
            file.Close();
            model_stream.Close();
        }

        private void ReadHeader()
        {
            // UPDATED FOR WOTLK 17.10.2008 by toblakai
            // SOURCE: http://www.madx.dk/wowdev/wiki/index.php?title=M2/WotLK

            char[] Magic = file.ReadChars(4);
            //PPather.Debug("M2 MAGIC: {0}",new string(Magic));
            uint version = file.ReadUInt32(); // (including \0);
                                              // check that we have the new known WOTLK Magic 0x80100000
                                              //PPather.Debug("M2 HEADER VERSION: 0x{0:x8}",
                                              //    (uint) (version >> 24) | ((version << 8) & 0x00FF0000) | ((version >> 8) & 0x0000FF00) | (version << 24));
            uint model_name_length = file.ReadUInt32(); // (including \0);
            uint model_name_offset = file.ReadUInt32();
            uint GlobalModelFlags = file.ReadUInt32(); // ? always 0, 1 or 3 (mostly 0);
            uint nGlobalSequences = file.ReadUInt32(); //  - number of global sequences;
            uint ofsGlobalSequences = file.ReadUInt32(); //  - offset to global sequences;
            uint nAnimations = file.ReadUInt32(); //  - number of animation sequences;
            uint ofsAnimations = file.ReadUInt32(); //  - offset to animation sequences;
            uint nAnimationLookup = file.ReadUInt32();
            uint ofsAnimationLookup = file.ReadUInt32(); // Mapping of global IDs to the entries in the Animation sequences block.
                                                         // NOT IN WOTLK uint nD=file.ReadUInt32(); //  - always 201 or 203 depending on WoW client version;
                                                         // NOT IN WOTLK uint ofsD=file.ReadUInt32();
            uint nBones = file.ReadUInt32(); //  - number of bones;
            uint ofsBones = file.ReadUInt32(); //  - offset to bones;
            uint nKeyBoneLookup = file.ReadUInt32(); //  - bone lookup table;
            uint ofsKeyBoneLookup = file.ReadUInt32();
            uint nVertices = file.ReadUInt32(); //  - number of vertices;
            uint ofsVertices = file.ReadUInt32(); //  - offset to vertices;
            uint nViews = file.ReadUInt32(); //  - number of views (LOD versions?) 4 for every model;
                                             // NOT IN WOTLK (now in .skins) uint ofsViews=file.ReadUInt32(); //  - offset to views;
            uint nColors = file.ReadUInt32(); //  - number of color definitions;
            uint ofsColors = file.ReadUInt32(); //  - offset to color definitions;
            uint nTextures = file.ReadUInt32(); //  - number of textures;
            uint ofsTextures = file.ReadUInt32(); //  - offset to texture definitions;
            uint nTransparency = file.ReadUInt32(); //  - number of transparency definitions;
            uint ofsTransparency = file.ReadUInt32(); //  - offset to transparency definitions;
            // NOT IN WOTLK uint nTexAnims = file.ReadUInt32(); //  - number of texture animations;
            // NOT IN WOTLK uint ofsTexAnims = file.ReadUInt32(); //  - offset to texture animations;
            uint nUnknown = file.ReadUInt32(); //  - always 0;
            uint ofsUnknown = file.ReadUInt32();
            uint nTexReplace = file.ReadUInt32();
            uint ofsTexReplace = file.ReadUInt32();
            uint nRenderFlags = file.ReadUInt32(); //  - number of blending mode definitions;
            uint ofsRenderFlags = file.ReadUInt32(); //  - offset to blending mode definitions;
            uint nBoneLookupTable = file.ReadUInt32(); //  - bone lookup table;
            uint ofsBoneLookupTable = file.ReadUInt32();
            uint nTexLookup = file.ReadUInt32(); //  - number of texture lookup table entries;
            uint ofsTexLookup = file.ReadUInt32(); //  - offset to texture lookup table;
            uint nTexUnits = file.ReadUInt32(); //  - texture unit definitions?;
            uint ofsTexUnits = file.ReadUInt32();
            uint nTransLookup = file.ReadUInt32(); //  - number of transparency lookup table entries;
            uint ofsTransLookup = file.ReadUInt32(); //  - offset to transparency lookup table;
            uint nTexAnimLookup = file.ReadUInt32(); //  - number of texture animation lookup table entries;
            uint ofsTexAnimLookup = file.ReadUInt32(); //  - offset to texture animation lookup table;
            float[] theFloats = new float[14]; // Noone knows. Meeh, they are here.
            for (int i = 0; i < 14; i++)
                theFloats[i] = file.ReadSingle();

            uint nBoundingTriangles = file.ReadUInt32();
            uint ofsBoundingTriangles = file.ReadUInt32();
            uint nBoundingVertices = file.ReadUInt32();
            uint ofsBoundingVertices = file.ReadUInt32();
            uint nBoundingNormals = file.ReadUInt32();
            uint ofsBoundingNormals = file.ReadUInt32();
            uint nAttachments = file.ReadUInt32();
            uint ofsAttachments = file.ReadUInt32();
            uint nAttachLookup = file.ReadUInt32();
            uint ofsAttachLookup = file.ReadUInt32();
            uint nAttachments_2 = file.ReadUInt32();
            uint ofsAttachments_2 = file.ReadUInt32();
            uint nLights = file.ReadUInt32(); //  - number of lights;
            uint ofsLights = file.ReadUInt32(); //  - offset to lights;
            uint nCameras = file.ReadUInt32(); //  - number of cameras;
            uint ofsCameras = file.ReadUInt32(); //  - offset to cameras;
            uint nCameraLookup = file.ReadUInt32();
            uint ofsCameraLookup = file.ReadUInt32();
            uint nRibbonEmitters = file.ReadUInt32(); //  - number of ribbon emitters;
            uint ofsRibbonEmitters = file.ReadUInt32(); //  - offset to ribbon emitters;
            uint nParticleEmitters = file.ReadUInt32(); //  - number of particle emitters;
            uint ofsParticleEmitters = file.ReadUInt32(); //  - offset to particle emitters;

            //model.views = new ModelView[nViews];
            model.view = null;//ReadViews(nViews, ofsViews);
                              //model.nVertices = nVertices;
            model.vertices = ReadVertices(nVertices, ofsVertices);

            //model.nBoundingTriangles = nBoundingTriangles;
            model.boundingTriangles = ReadBoundingTriangles(nBoundingTriangles, ofsBoundingTriangles);
            //model.nBoundingVertices = nBoundingVertices;
            model.boundingVertices = ReadBoundingVertices(nBoundingVertices, ofsBoundingVertices);
        }

        private float[] ReadBoundingVertices(uint nVertices, uint ofsVertices)
        {
            if (nVertices == 0)
                return null;
            file.BaseStream.Seek(ofsVertices, System.IO.SeekOrigin.Begin);
            float[] vertices = new float[nVertices * 3];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = file.ReadSingle();
            }
            return vertices;
        }

        private UInt16[] ReadBoundingTriangles(uint nTriangles, uint ofsTriangles)
        {
            if (nTriangles == 0)
                return null;
            file.BaseStream.Seek(ofsTriangles, System.IO.SeekOrigin.Begin);
            UInt16[] triangles = new UInt16[nTriangles];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = file.ReadUInt16();
            }
            return triangles;
        }

        private float[] ReadVertices(uint nVertices, uint ofcVertices)
        {
            float[] vertices = new float[nVertices * 3];
            file.BaseStream.Seek(ofcVertices, System.IO.SeekOrigin.Begin);
            for (int i = 0; i < nVertices; i++)
            {
                vertices[i * 3 + 0] = file.ReadSingle();
                vertices[i * 3 + 1] = file.ReadSingle();
                vertices[i * 3 + 2] = file.ReadSingle();
                file.ReadUInt32();  // bone weights
                file.ReadUInt32();  // bone indices

                file.ReadSingle(); // normal *3
                file.ReadSingle();
                file.ReadSingle();

                file.ReadSingle(); // texture coordinates
                file.ReadSingle();

                file.ReadSingle(); // some crap
                file.ReadSingle();
            }
            return vertices;
        }

        private ModelView[] ReadViews(uint nViews, uint offset)
        {
            file.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin);
            ModelView[] views = new ModelView[nViews];
            for (uint i = 0; i < nViews; i++)
            {
                views[i] = new ModelView();
                uint nIndex = file.ReadUInt32();
                uint offIndex = file.ReadUInt32();
                views[i].offIndex = offIndex;
                views[i].indexList = new UInt16[nIndex];

                uint nTriangle = file.ReadUInt32();
                uint offTriangle = file.ReadUInt32();
                views[i].offTriangle = offTriangle;
                views[i].triangleList = new UInt16[nTriangle];

                uint nVertexProp = file.ReadUInt32();
                uint offVertexProp = file.ReadUInt32();

                uint nSubMesh = file.ReadUInt32();
                uint offSubMesh = file.ReadUInt32();

                uint nTexture = file.ReadUInt32();
                uint offTexture = file.ReadUInt32();

                file.ReadUInt32(); // some crap
            }
            for (uint i = 0; i < nViews; i++)
            {
                ReadView(views[i]);
            }
            return views;
        }

        private void ReadView(ModelView view)
        {
            file.BaseStream.Seek(view.offIndex, System.IO.SeekOrigin.Begin);
            for (int i = 0; i < view.indexList.Length; i++)
            {
                view.indexList[i] = file.ReadUInt16();
            }
            file.BaseStream.Seek(view.offTriangle, System.IO.SeekOrigin.Begin);
            for (int i = 0; i < view.triangleList.Length; i++)
            {
                view.triangleList[i] = file.ReadUInt16();
            }
        }
    }

    public class WMOGroup
    {
        public uint nameStart, nameStart2;
        public uint flags;
        public Vec3D v1;
        public Vec3D v2;
        public UInt16 batchesA;
        public UInt16 batchesB;
        public UInt16 batchesC;
        public UInt16 batchesD;
        public UInt16 portalStart;
        public UInt16 portalCount;
        public uint id;

        public uint nVertices;
        public float[] vertices; // 3 per vertex

        public uint nTriangles;
        public UInt16[] triangles; // 3 per triangle
        public UInt16[] materials;  // 1 per triangle

        public const UInt16 MAT_FLAG_NOCAMCOLLIDE = 0x001;
        public const UInt16 MAT_FLAG_DETAIL = 0x002;
        public const UInt16 MAT_FLAG_COLLISION = 0x004;
        public const UInt16 MAT_FLAG_HINT = 0x008;
        public const UInt16 MAT_FLAG_RENDER = 0x010;
        public const UInt16 MAT_FLAG_COLLIDE_HIT = 0x020;
    }

    internal class WDT
    {
        public bool[,] maps = new bool[64, 64];
        public int gnWMO;
        public int nMaps;
        public List<string> gwmos = new List<string>();
        public List<WMOInstance> gwmois = new List<WMOInstance>();

        public MapTile[,] maptiles = new MapTile[64, 64];
    }

    internal class WDTFile
    {
        public bool loaded;
        private System.IO.Stream stream;
        private System.IO.BinaryReader file;
        private WMOManager wmomanager;
        private ModelManager modelmanager;
        private WDT wdt;
        private string name;
        private StormDll.ArchiveSet archive;

        private Logger logger;
        private DataConfig dataConfig;

        //Alterac Valley> ZonePath :” world\\maps\\PVPZone01\\PVPZone01”

        // 238.
        //  Warsong Gulch> ZonePath : world\\maps\\PVPZone03\\PVPZone03”

        // 239.
        //  Arathi Basin> ZonePath : “world\\mapsPVPZone04\\PVPZone04”

        // 240.
        //  Eye of the Storm> ZonePath : “world\\maps\\NetherstormBG\\NetherstormBG”

        // 241.
        //  Strand of the Ancients> ZonePath : "world\\maps\\NorthrendBG\\NorthrendBG”

        // 242.
        //  Isle of Conquest> ZonePath : “world\\maps\\IsleofConquest\\IsleofConquest”

        // 243.
        //  Twin Peaks> ZonePath : “world\\maps\\CataclysmCTF\\CataclysmCTF”

        // 244.
        //  Tol Barad> ZonePath : “world\\maps\\TolBarad\\TolBarad”

        // 245.
        //  The Battle for Gilneas > ZonePath : “world\\maps\\Gilneas_BG_2\\Gilneas_BG_2”*/

        //        1.
        //Azeroth

        // 2.
        //Expansion01

        // 3.
        //Kalimdor

        // 4.
        //Mauradon

        // 5.
        //NetherstormBG

        // 6.
        //Northrend

        // 7.
        //NorthrendBG

        // 8.
        //PVPZone01

        // 9.
        //PVPZone02

        // 10.
        //PVPZone03

        // 11.
        //PVPZone04

        // 12.
        //PVPZone05

        // 13.
        //TanarisInstance

        // 14.

        // 15.

        // 16.
        //----

        // 17.
        //Now there are the instance mesh for instancebuffy . Some Are raid instances not dungeons

        // 18.
        //and aren't meshed probably, so we just need the dungeon ones.

        // 19.

        // 20.
        //----

        // 21.

        // 22.
        //From Expansion01 MPQ probably:

        // 23.
        //CavernsOfTime

        // 24.
        //HellfireRampart

        // 25.

        // 26.
        //Expansion02 MPQ :

        // 27.
        //Azjol_Lowercity

        // 28.
        //Azjol_Uppercity

        // 29.
        //DeathKnightStart

        // 30.
        //DrakeTheronKeep

        // 31.
        //GunDrak

        // 32.
        //IsleofConquest

        // 33.
        //Nexus70

        // 34.
        //Nexus80

        // 35.
        //StratholmeCOT

        // 36.
        //UtgardePinnacle

        // 37.

        // 38.

        // 39.
        //World.MPQ

        // 40.
        //BlackrockDepths

        // 41.
        //BlackRockSpire

        // 42.
        //BlackwingLair

        // 43.
        //DeadminesInstance

        // 44.
        //DeeprunTram

        // 45.
        //DesolaceBomb

        // 46.
        //Diremaul

        // 47.
        //GnomeragonInstance

        // 48.
        //MonestaryInstances

        // 49.
        //OrgrimmarInstance

        // 50.
        //RazorfenDowns

        // 51.
        //RazorfenKraulInstance

        // 52.
        //Shadowfang

        // 53.
        //Stormwind

        // 54.
        //StormwindJail

        // 55.
        //Stratholme

        // 56.
        //SunkenTemple

        // 57.
        //Uldaman

        // 58.
        //WailingCarverns

        public WDTFile(StormDll.ArchiveSet archive, string name, WDT wdt, WMOManager wmomanager, ModelManager modelmanager, Logger logger, DataConfig dataConfig)
        {
            this.logger = logger;
            this.dataConfig = dataConfig;
            string wdtfile = "World\\Maps\\" + name + "\\" + name + ".wdt";
            Dbg.Log(" wdt");
            var path = Path.Join(dataConfig.PPather, "wdt.tmp");
            if (!archive.ExtractFile(wdtfile, path))
                return;

            loaded = true;
            this.name = name;
            this.wdt = wdt;
            this.wmomanager = wmomanager;
            this.modelmanager = modelmanager;
            this.archive = archive;

            stream = System.IO.File.OpenRead(path);
            file = new System.IO.BinaryReader(stream);

            bool done = false;
            do
            {
                try
                {
                    uint type = file.ReadUInt32();
                    uint size = file.ReadUInt32();
                    long curpos = file.BaseStream.Position;

                    if (type == ChunkReader.MVER)
                    {
                    }
                    else if (type == ChunkReader.MPHD)
                    {
                    }
                    else if (type == ChunkReader.MODF)
                    {
                        HandleMODF(size);
                    }
                    else if (type == ChunkReader.MWMO)
                    {
                        HandleMWMO(size);
                    }
                    else if (type == ChunkReader.MAIN)
                    {
                        int cnt = HandleMAIN(size);
                        logger.WriteLine("Map Tiles available in " + wdtfile + " = " + cnt);
                    }
                    else
                    {
                        logger.WriteLine("WDT Unknown " + type);
                        //done = true;
                    }
                    file.BaseStream.Seek(curpos + size, System.IO.SeekOrigin.Begin);
                }
                catch (System.IO.EndOfStreamException)
                {
                    done = true;
                }
            } while (!done);

            file.Close();
            stream.Close();

            // load map tiles
        }

        public void LoadMapTile(int x, int y)
        {
            if (wdt.maps[x, y])
            {
                MapTile t = new MapTile();

                string filename = "World\\Maps\\" + name + "\\" + name + "_" + x + "_" + y + ".adt";
                Dbg.Log(" adt");
                var path = Path.Join(dataConfig.PPather, "adt.tmp");
                if (archive.ExtractFile(filename, path))
                {
                    logger.Debug("Reading adt: " + filename);
                    //PPather.mover.Stop();
                    MapTileFile f = new MapTileFile(path, t, wmomanager, modelmanager);
                    if (t.models.Count != 0 || t.wmos.Count != 0)
                    {
                        //logger.WriteLine(name + " " + x + " " + z + " models: " + t.models.Count + " wmos: " + t.wmos.Count);
                        // Weee
                    }
                    wdt.maptiles[x, y] = t;
                }
            }
        }

        private void HandleMWMO(uint size)
        {
            if (size != 0)
            {
                int l = 0;
                byte[] raw = file.ReadBytes((int)size);
                while (l < size)
                {
                    string s = ChunkReader.ExtractString(raw, l);
                    l += s.Length + 1;
                    wdt.gwmos.Add(s);
                }
            }
        }

        private void HandleMODF(uint size)
        {
            // global wmo instance data
            wdt.gnWMO = (int)size / 64;
            for (uint i = 0; i < wdt.gnWMO; i++)
            {
                int id = file.ReadInt32();
                string path = wdt.gwmos[id];

                WMO wmo = wmomanager.AddAndLoadIfNeeded(path);

                WMOInstance wmoi = new WMOInstance(wmo, file);
                wdt.gwmois.Add(wmoi);
            }
        }

        private int HandleMAIN(uint size)
        {
            // global map objects
            int cnt = 0;
            for (int j = 0; j < 64; j++)
            {
                for (int i = 0; i < 64; i++)
                {
                    int d = file.ReadInt32();
                    if (d != 0)
                    {
                        wdt.maps[i, j] = true;
                        wdt.nMaps++;
                        cnt++;
                    }
                    else
                        wdt.maps[i, j] = false;
                    file.ReadInt32(); // kasta
                }
            }
            return cnt;
        }
    }

    internal class DBCFile
    {
        private System.IO.Stream stream;
        private System.IO.BinaryReader file;
        private DBC dbc;

        private Logger logger;

        public DBCFile(string name, DBC dbc, Logger logger)
        {
            this.logger = logger;
            this.dbc = dbc;
            stream = System.IO.File.OpenRead(name);
            file = new System.IO.BinaryReader(stream);

            bool done = false;
            do
            {
                try
                {
                    uint type = file.ReadUInt32();
                    //uint size = file.ReadUInt32();
                    //long curpos = file.BaseStream.Position;

                    if (type == ChunkReader.CBDW)
                    {
                        HandleWDBC();
                    }
                    else
                    {
                        logger.WriteLine("DBC Unknown " + type);
                        //done = true;
                    }
                    //file.BaseStream.Seek(curpos + size, System.IO.SeekOrigin.Begin);
                }
                catch (System.IO.EndOfStreamException)
                {
                    done = true;
                }
            } while (!done);

            file.Close();
            stream.Close();
        }

        private void HandleWDBC()
        {
            dbc.recordCount = file.ReadUInt32();

            dbc.fieldCount = file.ReadUInt32(); // words per record
            dbc.recordSize = file.ReadUInt32();
            dbc.stringSize = file.ReadUInt32();

            if (dbc.fieldCount * 4 != dbc.recordSize)
            {
                // !!!
                logger.WriteLine("WOOT");
            }
            int off = 0;
            uint[] raw = new uint[dbc.fieldCount * dbc.recordCount];
            for (uint i = 0; i < dbc.recordCount; i++)
            {
                for (int j = 0; j < dbc.fieldCount; j++)
                {
                    raw[off++] = file.ReadUInt32();
                }
            }
            dbc.rawRecords = raw;

            byte[] b = file.ReadBytes((int)dbc.stringSize);
            dbc.strings = b;
        }
    }

    internal class MapChunk
    {
        // public int nTextures;

        public float xbase, ybase, zbase;
        //public float r;

        public uint areaID;

        public bool haswater;

        // public bool visible;
        public bool hasholes;

        public uint holes;

        //public float waterlevel;

        //  0   1   2   3   4   5   6   7   8
        //    9  10  11  12  13  14  15  16
        // 17  18  19  20  21  22  23  24  25
        // ...
        public float[] vertices = new float[3 * ((9 * 9) + (8 * 8))];

        public float water_height1;
        public float water_height2;
        public float[,] water_height;
        public byte[,] water_flags;

        private static readonly int[] holetab_h = new int[] { 0x1111, 0x2222, 0x4444, 0x8888 };
        private static readonly int[] holetab_v = new int[] { 0x000F, 0x00F0, 0x0F00, 0xF000 };

        // 0 ..3, 0 ..3
        public bool isHole(int i, int j)
        {
            if (!hasholes)
                return false;
            i /= 2;
            j /= 2;
            if (i > 3 || j > 3)
                return false;
            //if(holes != 0)
            //    System.Diagnostics.Debug.Write("Someone checking for holes " + i + " " + j);
            bool r = (holes & holetab_h[i] & holetab_v[j]) != 0;

            return r;
        }

        //TextureID textures[4];
        //TextureID alphamaps[3];
        //TextureID shadow, blend;
        //int animated[4];

        //short *strip;
        //int striplen;
        //Liquid *lq;
    }

    internal class MapTile
    {
        // public int x, z; // matches maps in WDT

        public List<ModelInstance> modelis = new List<ModelInstance>();
        public List<WMOInstance> wmois = new List<WMOInstance>();

        public List<string> wmos = new List<string>();
        public List<string> models = new List<string>();

        public MapChunk[,] chunks = new MapChunk[16, 16];
    }

    internal class MapTileFile // adt file
    {
        private Logger logger;

        public MapTileFile(Logger logger)
        {
            this.logger = logger;
        }

        private MapTile tile;
        private LiquidData[] LiquidDataChunk; //256 elements
        private System.IO.Stream stream;
        private System.IO.BinaryReader file;
        private int[] mcnk_offsets = new int[256];
        private int[] mcnk_sizes = new int[256];
        private WMOManager wmomanager;
        private ModelManager modelmanager;

        public MapTileFile(string name, MapTile tile, WMOManager wmomanager, ModelManager modelmanager)
        {
            this.tile = tile;
            this.wmomanager = wmomanager;
            this.modelmanager = modelmanager;
            stream = System.IO.File.OpenRead(name);
            file = new System.IO.BinaryReader(stream);
            bool done = false;
            do
            {
                try
                {
                    uint type = file.ReadUInt32();
                    uint size = file.ReadUInt32();
                    long curpos = file.BaseStream.Position;

                    if (type == ChunkReader.MVER)
                        HandleMVER(size);
                    if (type == ChunkReader.MCIN)
                        HandleMCIN(size);
                    else if (type == ChunkReader.MTEX)
                        HandleMTEX(size);
                    else if (type == ChunkReader.MMDX)
                        HandleMMDX(size);
                    else if (type == ChunkReader.MWMO)
                        HandleMWMO(size);
                    else if (type == ChunkReader.MDDF)
                        HandleMDDF(size);
                    else if (type == ChunkReader.MODF)
                        HandleMODF(size);
                    else if (type == ChunkReader.MH2O)
                        HandleMH2O(size);
                    //else if(type==ChunkReader.MCNK)
                    //HandleMCNK(size);
                    else
                    {
                        //logger.WriteLine("MapTile Unknown " + type);
                        //done = true;
                    }
                    file.BaseStream.Seek(curpos + size, System.IO.SeekOrigin.Begin);
                }
                catch (System.IO.EndOfStreamException)
                {
                    done = true;
                }
            } while (!done);

            for (int j = 0; j < 16; j++)
            {
                for (int i = 0; i < 16; i++)
                {
                    int off = mcnk_offsets[j * 16 + i];
                    file.BaseStream.Seek(off, System.IO.SeekOrigin.Begin);
                    //logger.WriteLine("Chunk " + i + " " + j + " at off " + off);
                    MapChunk chunk = new MapChunk();
                    ReadMapChunk(chunk);

                    if (LiquidDataChunk != null) //not null means an MH2O chunk was found
                    {
                        //set liquid info from the MH2O chunk since the old MCLQ is no more
                        chunk.haswater = (LiquidDataChunk[j * 16 + i].used & 1) == 1;
                        if (LiquidDataChunk[j * 16 + i].data1 != null)
                        {
                            chunk.water_height1 = LiquidDataChunk[j * 16 + i].data1.heightLevel1;
                            chunk.water_height2 = LiquidDataChunk[j * 16 + i].data1.heightLevel2;
                        }

                        //TODO: set height map and flags, very important
                        chunk.water_height = LiquidDataChunk[j * 16 + i].water_height;
                        chunk.water_flags = LiquidDataChunk[j * 16 + i].water_flags;
                    }
                    tile.chunks[j, i] = chunk;
                }
            }

            //if (ChunkLiquidData != null)
            //logger.Debug("ADT HAS MH2O");
            //else
            //logger.Debug("ADT HAS MCLQ");

            file.Close();
            stream.Close();
        }

        public class LiquidData
        {
            public uint offsetData1;
            public int used;
            public uint offsetData2;

            public MH2OData1 data1;

            public float[,] water_height = new float[9, 9];
            public byte[,] water_flags = new byte[8, 8];
        }

        public class MH2OData1
        {
            public UInt16 flags; //0x1 might mean there is a height map @ data2b ??
            public UInt16 type; //0 = normal/lake, 1 = lava, 2 = ocean
            public float heightLevel1;
            public float heightLevel2;
            public byte xOffset;
            public byte yOffset;
            public byte Width;
            public byte Height;
            public uint offsetData2a;
            public uint offsetData2b;
            //public uint Data2bLength = 0;
        }

        private void HandleMH2O(uint size)
        {
            long chunkStart = file.BaseStream.Position;
            LiquidDataChunk = new LiquidData[256];

            for (int i = 0; i < 256; i++)
            {
                LiquidDataChunk[i] = new LiquidData();
                LiquidDataChunk[i].offsetData1 = file.ReadUInt32();
                LiquidDataChunk[i].used = file.ReadInt32();
                LiquidDataChunk[i].offsetData2 = file.ReadUInt32();
            }

            for (int i = 0; i < 256; i++)
            {
                if (LiquidDataChunk[i].offsetData1 != 0)
                {
                    file.BaseStream.Seek(chunkStart + LiquidDataChunk[i].offsetData1, System.IO.SeekOrigin.Begin);
                    LiquidDataChunk[i].data1 = new MH2OData1();
                    LiquidDataChunk[i].data1.flags = file.ReadUInt16();
                    LiquidDataChunk[i].data1.type = file.ReadUInt16();
                    LiquidDataChunk[i].data1.heightLevel1 = file.ReadSingle();
                    LiquidDataChunk[i].data1.heightLevel2 = file.ReadSingle();
                    LiquidDataChunk[i].data1.xOffset = file.ReadByte();
                    LiquidDataChunk[i].data1.yOffset = file.ReadByte();
                    LiquidDataChunk[i].data1.Width = file.ReadByte();
                    LiquidDataChunk[i].data1.Height = file.ReadByte();
                    LiquidDataChunk[i].data1.offsetData2a = file.ReadUInt32();
                    LiquidDataChunk[i].data1.offsetData2b = file.ReadUInt32();
                }
            }

            for (int k = 0; k < 256; k++)
            {
                if ((LiquidDataChunk[k].used & 1) == 1 &&
                    LiquidDataChunk[k].data1 != null && LiquidDataChunk[k].data1.offsetData2b != 0
                    && (LiquidDataChunk[k].data1.flags & 1) == 1)
                {
                    file.BaseStream.Seek(chunkStart + LiquidDataChunk[k].data1.offsetData2b, System.IO.SeekOrigin.Begin);

                    for (int x = LiquidDataChunk[k].data1.xOffset; x <= LiquidDataChunk[k].data1.xOffset + LiquidDataChunk[k].data1.Width; x++)
                    {
                        for (int y = LiquidDataChunk[k].data1.yOffset; y <= LiquidDataChunk[k].data1.yOffset + LiquidDataChunk[k].data1.Height; y++)
                        {
                            LiquidDataChunk[k].water_height[x, y] = file.ReadSingle();
                            if (float.IsNaN(LiquidDataChunk[k].water_height[x, y]))
                            {
                                throw new Exception("Major inconsistency in MH2O-handler.");
                            }
                        }
                    }

                    for (int x = LiquidDataChunk[k].data1.xOffset; x < LiquidDataChunk[k].data1.xOffset + LiquidDataChunk[k].data1.Width; x++)
                    {
                        for (int y = LiquidDataChunk[k].data1.yOffset; y < LiquidDataChunk[k].data1.yOffset + LiquidDataChunk[k].data1.Height; y++)
                            LiquidDataChunk[k].water_flags[x, y] = file.ReadByte();
                    }
                }
            }
        }

        private static void HandleMVER(uint size)
        {
        }

        private void HandleMCIN(uint size)
        {
            for (int i = 0; i < 256; i++)
            {
                mcnk_offsets[i] = file.ReadInt32();
                mcnk_sizes[i] = file.ReadInt32();
                file.ReadInt32(); // crap
                file.ReadInt32();// crap
            }
        }

        private static void HandleMTEX(uint size)
        {
        }

        private void HandleMMDX(uint size)
        {
            if (size != 0)
            {
                int l = 0;
                byte[] raw = file.ReadBytes((int)size);
                while (l < size)
                {
                    string s = ChunkReader.ExtractString(raw, l);
                    l += s.Length + 1;

                    tile.models.Add(s);
                }
            }
        }

        private void HandleMWMO(uint size)
        {
            if (size != 0)
            {
                int l = 0;
                byte[] raw = file.ReadBytes((int)size);
                while (l < size)
                {
                    string s = ChunkReader.ExtractString(raw, l);
                    l += s.Length + 1;

                    tile.wmos.Add(s);
                }
            }
        }

        private void HandleMDDF(uint size)
        {
            int nMDX = (int)size / 36;

            for (int i = 0; i < nMDX; i++)
            {
                int id = file.ReadInt32();
                Model model = modelmanager.AddAndLoadIfNeeded(tile.models[id]);

                ModelInstance mi = new ModelInstance(model, file);
                tile.modelis.Add(mi);
            }
        }

        private void HandleMODF(uint size)
        {
            int nWMO = (int)size / 64;
            for (int i = 0; i < nWMO; i++)
            {
                int id = file.ReadInt32();
                WMO wmo = wmomanager.AddAndLoadIfNeeded(tile.wmos[id]);

                WMOInstance wi = new WMOInstance(wmo, file);
                tile.wmois.Add(wi);
            }
        }

        private void ReadMapChunk(MapChunk chunk)
        {
            // Read away Magic and size
            uint crap_head = file.ReadUInt32();
            uint crap_size = file.ReadUInt32();

            // Each map chunk has 9x9 vertices,
            // and in between them 8x8 additional vertices, several texture layers, normal vectors, a shadow map, etc.

            uint flags = file.ReadUInt32();
            uint ix = file.ReadUInt32();
            uint iy = file.ReadUInt32();
            uint nLayers = file.ReadUInt32();
            uint nDoodadRefs = file.ReadUInt32();
            uint ofsHeight = file.ReadUInt32();
            uint ofsNormal = file.ReadUInt32();
            uint ofsLayer = file.ReadUInt32();
            uint ofsRefs = file.ReadUInt32();
            uint ofsAlpha = file.ReadUInt32();
            uint sizeAlpha = file.ReadUInt32();
            uint ofsShadow = file.ReadUInt32();
            uint sizeShadow = file.ReadUInt32();
            uint areaid = file.ReadUInt32();
            uint nMapObjRefs = file.ReadUInt32();
            uint holes = file.ReadUInt32();
            ushort s1 = file.ReadUInt16();
            ushort s2 = file.ReadUInt16();
            uint d1 = file.ReadUInt32();
            uint d2 = file.ReadUInt32();
            uint d3 = file.ReadUInt32();
            uint predTex = file.ReadUInt32();
            uint nEffectDoodad = file.ReadUInt32();
            uint ofsSndEmitters = file.ReadUInt32();
            uint nSndEmitters = file.ReadUInt32();
            uint ofsLiquid = file.ReadUInt32();
            uint sizeLiquid = file.ReadUInt32();
            float zpos = file.ReadSingle();
            float xpos = file.ReadSingle();
            float ypos = file.ReadSingle();
            uint textureId = file.ReadUInt32();
            uint props = file.ReadUInt32();
            uint effectId = file.ReadUInt32();

            chunk.areaID = areaid;

            chunk.zbase = zpos;
            chunk.xbase = xpos;
            chunk.ybase = ypos;

            // correct the x and z values
            chunk.zbase = -chunk.zbase + ChunkReader.ZEROPOINT;
            chunk.xbase = -chunk.xbase + ChunkReader.ZEROPOINT;

            chunk.hasholes = (holes != 0);
            chunk.holes = holes;

            bool debug = false;
            //logger.WriteLine("  " + zpos + " " + xpos + " " + ypos);
            bool done = false;
            do
            {
                try
                {
                    uint type = file.ReadUInt32();
                    uint size = file.ReadUInt32();
                    long curpos = file.BaseStream.Position;

                    if (type == ChunkReader.MCNR)
                    {
                        size = 0x1C0; // WTF
                        if (debug)
                            logger.WriteLine("MCNR " + size);
                        HandleChunkMCNR(chunk, size);
                    }
                    else if (type == ChunkReader.MCVT)
                    {
                        if (debug)
                            logger.WriteLine("MCVT " + size);
                        HandleChunkMCVT(chunk, size);
                    }
                    else if (type == ChunkReader.MCRF)
                    {
                        if (debug)
                            logger.WriteLine("MCRF " + size);
                        HandleChunkMCRF(chunk, size);
                    }
                    else if (type == ChunkReader.MCLY)
                    {
                        if (debug)
                            logger.WriteLine("MCLY " + size);
                        HandleChunkMCLY(chunk, size);
                    }
                    else if (type == ChunkReader.MCSH)
                    {
                        if (debug)
                            logger.WriteLine("MCSH " + size);
                        HandleChunkMCSH(chunk, size);
                    }
                    else if (type == ChunkReader.MCAL)
                    {
                        if (debug)
                            logger.WriteLine("MCAL " + size);
                        HandleChunkMCAL(chunk, size);
                        // TODO rumors are that the size of this chunk is wrong sometimes
                    }
                    else if (type == ChunkReader.MCLQ)
                    {
                        /* Some .adt-files are still using the old MCLQ chunks. Far from all though.
                         * And those which use the MH2O chunk does not use these MCLQ chunks */
                        size = sizeLiquid;
                        if (debug)
                        {
                            logger.Debug(string.Format("MCLQ {0}", size));
                        }
                        if (sizeLiquid != 8)
                        {
                            chunk.haswater = true;
                            HandleChunkMCLQ(chunk, size);
                            //done = true; // size if fucked up, give up
                        }
                    }
                    else if (type == ChunkReader.MCSE)
                    {
                        if (debug)
                            logger.WriteLine("MCSE " + size);
                        HandleChunkMCSE(chunk, size);
                    }
                    else if (type == ChunkReader.MCNK)
                    {
                        //logger.WriteLine("MCNK " + size);
                        done = true; // found next
                                     //HandleChunkMCSE(chunk, size);
                    }
                    else
                    {
                        //logger.WriteLine("MapChunk Unknown " + type);
                        //done = true;
                    }
                    file.BaseStream.Seek(curpos + size, System.IO.SeekOrigin.Begin);
                }
                catch (System.IO.EndOfStreamException)
                {
                    done = true;
                }
            } while (!done);
        }

        private static void HandleChunkMCNR(MapChunk chunk, uint size)
        {
            // Normals
        }

        private void HandleChunkMCVT(MapChunk chunk, uint size)
        {
            // vertices
            int off = 0;
            for (int j = 0; j < 17; j++)
            {
                for (int i = 0; i < ((j % 2 != 0) ? 8 : 9); i++)
                {
                    float h, xpos, zpos;
                    h = file.ReadSingle();
                    xpos = i * ChunkReader.UNITSIZE;
                    zpos = j * 0.5f * ChunkReader.UNITSIZE;
                    if (j % 2 != 0)
                    {
                        xpos += ChunkReader.UNITSIZE * 0.5f;
                    }
                    float x = chunk.xbase + xpos;
                    float y = chunk.ybase + h;
                    float z = chunk.zbase + zpos;

                    chunk.vertices[off++] = x;
                    chunk.vertices[off++] = y;
                    chunk.vertices[off++] = z;
                }
            }
        }

        private static void HandleChunkMCRF(MapChunk chunk, uint size)
        {
        }

        private static void HandleChunkMCLY(MapChunk chunk, uint size)
        {
            // texture info
        }

        private static void HandleChunkMCSH(MapChunk chunk, uint size)
        {
            // shadow map 64 x 64
        }

        private static void HandleChunkMCAL(MapChunk chunk, uint size)
        {
            // alpha maps  64 x 64
        }

        private void HandleChunkMCLQ(MapChunk chunk, uint size)
        {
            chunk.water_height1 = file.ReadSingle();
            chunk.water_height2 = file.ReadSingle();

            chunk.water_height = new float[9, 9];
            chunk.water_flags = new byte[8, 8];
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    UInt32 word1 = file.ReadUInt32();
                    // UInt32 word2 = file.ReadUInt32();
                    //Int16 unk1 = file.ReadInt16(); // ??
                    //Int16 unk2 = file.ReadInt16(); // ??
                    chunk.water_height[i, j] = file.ReadSingle(); //  word1 + word2; //  file.ReadSingle();
                                                                  //PPather.Debug("HandleChunkMCLQ: CHUNK.WATIER_HEIGHT[{0}, {1}] = {2}", i, j, chunk.water_height[i, j]);
                }
            }

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    chunk.water_flags[i, j] = file.ReadByte();
                    //PPather.Debug("HandleChunkMCLQ: CHUNK.WATIER_FLAGS[{0}, {1}] = {2}", i, j, chunk.water_flags[i, j]);
                }
            }
        }

        private static void HandleChunkMCSE(MapChunk chunk, uint size)
        {
            // Sound emitters
        }
    }

    internal class WmoRootFile
    {
        private System.IO.Stream stream;
        private System.IO.BinaryReader file;
        private ModelManager modelmanager;

        public WMO wmo;
        //   string groupnames;

        public WmoRootFile(string name, WMO wmo, ModelManager modelmanager)
        {
            this.wmo = wmo;
            this.modelmanager = modelmanager;
            stream = System.IO.File.OpenRead(name);
            file = new System.IO.BinaryReader(stream);
            bool done = false;
            do
            {
                try
                {
                    uint type = file.ReadUInt32();
                    uint size = file.ReadUInt32();
                    long curpos = file.BaseStream.Position;

                    if (type == ChunkReader.MVER)
                    {
                        HandleMVER(size);
                    }
                    if (type == ChunkReader.MOHD)
                    {
                        HandleMOHD(size);
                    }
                    else if (type == ChunkReader.MOGP)
                    {
                        HandleMOGP(size);
                    }
                    else if (type == ChunkReader.MOGI)
                    {
                        HandleMOGI(size);
                    }
                    else if (type == ChunkReader.MODS)
                    {
                        HandleMODS(size);
                    }
                    else if (type == ChunkReader.MODD)
                    {
                        HandleMODD(size);
                    }
                    else if (type == ChunkReader.MODN)
                    {
                        HandleMODN(size);
                    }
                    else
                    {
                        //logger.WriteLine("Root Unknown " + type);
                        //done = true;
                    }
                    file.BaseStream.Seek(curpos + size, System.IO.SeekOrigin.Begin);
                }
                catch (System.IO.EndOfStreamException)
                {
                    done = true;
                }
            } while (!done);
            file.Close();
            stream.Close();
        }

        private static void HandleMVER(uint size)
        {
        }

        private void HandleMOHD(uint size)
        {
            uint nTextures = file.ReadUInt32();
            uint nGroups = file.ReadUInt32();
            uint nP = file.ReadUInt32();
            uint nLights = file.ReadUInt32();
            wmo.nModels = file.ReadUInt32();
            wmo.nDoodads = file.ReadUInt32();
            wmo.nDoodadSets = file.ReadUInt32();

            uint col = file.ReadUInt32();
            uint nX = file.ReadUInt32();

            float f0 = file.ReadSingle();
            float f1 = file.ReadSingle();
            float f2 = file.ReadSingle();
            wmo.v1 = new Vec3D(f0, f1, f2);

            float f3 = file.ReadSingle();
            float f4 = file.ReadSingle();
            float f5 = file.ReadSingle();
            wmo.v2 = new Vec3D(f3, f4, f5);

            wmo.groups = new WMOGroup[nGroups];
        }

        private static void HandleMOGN(uint size)
        {
            // group name
            // groupnames = ChunkReader.ReadString(file);
        }

        private void HandleMODS(uint size)
        {
            wmo.doodads = new DoodadSet[wmo.nDoodadSets];
            for (int i = 0; i < wmo.nDoodadSets; i++)
            {
                byte[] name = file.ReadBytes(20); // set name;
                wmo.doodads[i].firstInstance = file.ReadUInt32();
                wmo.doodads[i].nInstances = file.ReadUInt32();
                file.ReadUInt32();
            }
        }

        private void HandleMODD(uint size)
        {
            // 40 bytes per doodad instance, nDoodads entries.
            // While WMOs and models (M2s) in a map tile are rotated along the axes,
            //  doodads within a WMO are oriented using quaternions! Hooray for consistency!
            /*
0x00 	uint32 		Offset to the start of the model's filename in the MODN chunk.
0x04 	3 * float 	Position (X,Z,-Y)
0x10 	float 		W component of the orientation quaternion
0x14 	3 * float 	X, Y, Z components of the orientaton quaternion
0x20 	float 		Scale factor
0x24 	4 * uint8 	(B,G,R,A) color. Unknown. It is often (0,0,0,255). (something to do with lighting maybe?)
			 */

            uint sets = size / 0x28;
            wmo.doodadInstances = new ModelInstance[wmo.nDoodads];
            for (int i = 0; i < sets /*wmo.nDoodads*/; i++)
            {
                byte[] s = file.ReadBytes(4);
                s[3] = 0;
                uint nameOffsetInMODN = BitConverter.ToUInt32(s, 0); // 0x00
                float posx = file.ReadSingle(); // 0x04
                float posz = file.ReadSingle(); // 0x08
                float posy = -file.ReadSingle(); // 0x0c

                float quatw = file.ReadSingle(); // 0x10

                float quatx = file.ReadSingle(); // 0x14
                float quaty = file.ReadSingle(); // 0x18
                float quatz = file.ReadSingle();// 0x1c

                float scale = file.ReadSingle(); // 0x20

                file.ReadUInt32(); // lighning crap 0x24
                                   // float last = file.ReadSingle(); // 0x28

                String name = ChunkReader.ExtractString(wmo.MODNraw, (int)nameOffsetInMODN);
                Model m = modelmanager.AddAndLoadIfNeeded(name);

                Vec3D pos = new Vec3D(posx, posy, posz);
                Vec3D dir = new Vec3D(quatz, quaty, quatz);

                ModelInstance mi = new ModelInstance(m, pos, dir, scale, quatw);
                wmo.doodadInstances[i] = mi;
            }
        }

        private void HandleMODN(uint size)
        {
            wmo.MODNraw = file.ReadBytes((int)size);
            // List of filenames for M2 (mdx) models that appear in this WMO.
        }

        private static void HandleMOGP(uint size)
        {
        }

        private void HandleMOGI(uint size)
        {
            for (int i = 0; i < wmo.groups.Length; i++)
            {
                WMOGroup g = new WMOGroup();
                wmo.groups[i] = g;

                g.flags = file.ReadUInt32();
                float f0 = file.ReadSingle();
                float f1 = file.ReadSingle();
                float f2 = file.ReadSingle();
                g.v1 = new Vec3D(f0, f1, f2);

                float f3 = file.ReadSingle();
                float f4 = file.ReadSingle();
                float f5 = file.ReadSingle();
                g.v2 = new Vec3D(f3, f4, f5);

                uint nameOfs = file.ReadUInt32();
            }
        }
    }

    internal class WmoGroupFile
    {
        private System.IO.Stream stream;
        private System.IO.BinaryReader file;
        private WMOGroup g;

        //long indicesFileMarker;
        public WmoGroupFile(WMOGroup group, string name)
        {
            g = group;
            stream = System.IO.File.OpenRead(name);
            file = new System.IO.BinaryReader(stream);

            file.BaseStream.Seek(0x14, System.IO.SeekOrigin.Begin);
            HandleMOGP(11);

            file.BaseStream.Seek(0x58, System.IO.SeekOrigin.Begin);// first chunk

            bool done = false;
            do
            {
                try
                {
                    uint type = file.ReadUInt32();
                    uint size = file.ReadUInt32();
                    long curpos = file.BaseStream.Position;
                    uint MVER = ChunkReader.ToBin("MVER");
                    if (type == ChunkReader.MVER)
                    {
                        HandleMVER(size);
                    }
                    if (type == ChunkReader.MOPY)
                    {
                        HandleMOPY(size);
                    }
                    else if (type == ChunkReader.MOVI)
                    {
                        HandleMOVI(size);
                    }
                    else if (type == ChunkReader.MOVT)
                    {
                        HandleMOVT(size);
                    }
                    else if (type == ChunkReader.MONR)
                    {
                        HandleMONR(size);
                    }
                    else if (type == ChunkReader.MOLR)
                    {
                        HandleMOLR(size);
                    }
                    else if (type == ChunkReader.MODR)
                    {
                        HandleMODR(size);
                    }
                    else if (type == ChunkReader.MOBA)
                    {
                        HandleMOBA(size);
                    }
                    else if (type == ChunkReader.MOCV)
                    {
                        HandleMOCV(size);
                    }
                    else if (type == ChunkReader.MLIQ)
                    {
                        HandleMLIQ(size);
                    }
                    else if (type == ChunkReader.MOBN)
                    {
                        HandleMOBN(size);
                    }
                    else if (type == ChunkReader.MOBR)
                    {
                        HandleMOBR(size);
                    }
                    else
                    {
                        //logger.WriteLine("Group Unknown " + type);
                        //done = true;
                    }
                    file.BaseStream.Seek(curpos + size, System.IO.SeekOrigin.Begin);
                }
                catch (System.IO.EndOfStreamException)
                {
                    done = true;
                }
            } while (!done);

            file.Close();
            stream.Close();
        }

        private static void HandleMVER(uint size)
        {
        }

        private void HandleMOPY(uint size)
        {
            g.nTriangles = size / 2;
            // materials
            /*  0x01 - inside small houses and paths leading indoors
			 *  0x02 - ???
			 *  0x04 - set on indoor things and ruins
			 *  0x08 - ???
			 *  0x10 - ???
			 *  0x20 - Always set?
			 *  0x40 - sometimes set-
			 *  0x80 - ??? never set
			 *
			 */

            g.materials = new ushort[g.nTriangles];

            for (int i = 0; i < g.nTriangles; i++)
            {
                g.materials[i] = file.ReadUInt16();
            }
        }

        private void HandleMOVI(uint size)
        {
            //indicesFileMarker = file.BaseStream.Position;
            g.triangles = new UInt16[g.nTriangles * 3];
            for (uint i = 0; i < g.nTriangles; i++)
            {
                uint off = i * 3;
                g.triangles[off + 0] = file.ReadUInt16();
                g.triangles[off + 1] = file.ReadUInt16();
                g.triangles[off + 2] = file.ReadUInt16();
            }
        }

        private void HandleMOVT(uint size)
        {
            g.nVertices = size / 12;
            // let's hope it's padded to 12 bytes, not 16...
            g.vertices = new float[g.nVertices * 3];
            for (uint i = 0; i < g.nVertices; i++)
            {
                float f0 = file.ReadSingle();
                float f1 = file.ReadSingle();
                float f2 = file.ReadSingle();
                uint off = i * 3;
                g.vertices[off + 0] = f0;
                g.vertices[off + 1] = f1;
                g.vertices[off + 2] = f2;
            }
        }

        private static void HandleMONR(uint size)
        {
        }

        private static void HandleMOLR(uint size)
        {
        }

        private static void HandleMODR(uint size)
        {
        }

        private static void HandleMOBA(uint size)
        {
        }

        private static void HandleMOCV(uint size)
        {
        }

        private static void HandleMLIQ(uint size)
        {
        }

        /*
		struct t_BSP_NODE
		{
			public UInt16 planetype;          // unsure
			public Int16 child0;        // index of bsp child node(right in this array)
			public Int16 child1;
			public UInt16 numfaces;  // num of triangle faces
			public UInt16 firstface; // index of the first triangle index(in  MOBR)
			public UInt16 nUnk;	          // 0
			public float fDist;
		};*/

        private static void HandleMOBN(uint size)
        {
            /*
			t_BSP_NODE bsp;
			uint items = size / 16;
			for (int i = 0; i < items; i++)
			{
				bsp.planetype = file.ReadUInt16();
				bsp.child0 = file.ReadInt16();
				bsp.child1 = file.ReadInt16();
				bsp.numfaces = file.ReadUInt16();
				bsp.firstface = file.ReadUInt16();
				bsp.nUnk = file.ReadUInt16();
				bsp.fDist = file.ReadSingle();

				logger.WriteLine("BSP node type: " + bsp.planetype);
				if (bsp.child0 == -1)
				{
					logger.WriteLine("  faces: " + bsp.firstface + " " + bsp.numfaces);
				}
				else
				{
					logger.WriteLine("  children: " + bsp.child0 + " " + bsp.child1 + " dist "+ bsp.fDist);
				}
			}*/
        }

        private static void HandleMOBR(uint size)
        {
        }

        private void HandleMOGP(uint size)
        {
            g.nameStart = file.ReadUInt32();
            g.nameStart2 = file.ReadUInt32();
            g.flags = file.ReadUInt32();

            float bound1X = file.ReadSingle();
            float bound1Y = file.ReadSingle();
            float bound1Z = file.ReadSingle();
            g.v1 = new Vec3D(bound1X, bound1Y, bound1Z);

            float bound2X = file.ReadSingle();
            float bound2Y = file.ReadSingle();
            float bound2Z = file.ReadSingle();
            g.v2 = new Vec3D(bound1X, bound1Y, bound1Z);

            g.portalStart = file.ReadUInt16();
            g.portalCount = file.ReadUInt16();
            g.batchesA = file.ReadUInt16();
            g.batchesB = file.ReadUInt16();
            g.batchesC = file.ReadUInt16();
            g.batchesD = file.ReadUInt16();

            uint fogCrap = file.ReadUInt32();

            uint unknown1 = file.ReadUInt32();
            g.id = file.ReadUInt32();
            uint unknown2 = file.ReadUInt32();
            uint unknown3 = file.ReadUInt32();
        }
    }
}