using CodeWalker.GameFiles;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeWalker.World
{
    public class Heightmaps : BasePathData
    {
        public volatile bool Inited = false;
        public GameFileCache GameFileCache;

        public List<HeightmapFile> HeightmapFiles = new List<HeightmapFile>();


        public Vector4[] GetNodePositions()
        {
            return NodePositions;
        }
        public EditorVertex[] GetPathVertices()
        {
            return null;
        }
        public EditorVertex[] GetTriangleVertices()
        {
            return TriangleVerts;
        }

        public Vector4[] NodePositions;
        public EditorVertex[] TriangleVerts;

        public bool GetHeight(float x, float y, out float minZ, out float maxZ)
        {
            minZ = 0.0f;
            maxZ = 0.0f;
            if (!Inited || HeightmapFiles == null || HeightmapFiles.Count == 0) return false;

            for (int i = 0; i < HeightmapFiles.Count; i++)
            {
                var hmf = HeightmapFiles[i];
                if (hmf == null || hmf.MaxHeights == null) continue;

                var min = hmf.BBMin;
                var max = hmf.BBMax;
                if (x < min.X || x > max.X || y < min.Y || y > max.Y) continue;

                var w = hmf.Width;
                var h = hmf.Height;
                if (w < 2 || h < 2) continue;

                var siz = max - min;
                float normX = (x - min.X) / siz.X;
                float normY = (y - min.Y) / siz.Y;
                float fx = normX * (w - 1);
                float fy = normY * (h - 1);

                int x0 = (int)Math.Floor(fx);
                int y0 = (int)Math.Floor(fy);
                int x1 = Math.Min(x0 + 1, w - 1);
                int y1 = Math.Min(y0 + 1, h - 1);
                x0 = Math.Max(0, Math.Min(x0, w - 1));
                y0 = Math.Max(0, Math.Min(y0, h - 1));

                float tx = fx - x0;
                float ty = fy - y0;
                float stepZ = siz.Z / 255.0f;

                var hmax = hmf.MaxHeights;
                var hmin = hmf.MinHeights;

                float max00 = min.Z + stepZ * hmax[y0 * w + x0];
                float max10 = min.Z + stepZ * hmax[y0 * w + x1];
                float max01 = min.Z + stepZ * hmax[y1 * w + x0];
                float max11 = min.Z + stepZ * hmax[y1 * w + x1];
                float top0 = max00 * (1.0f - tx) + max10 * tx;
                float top1 = max01 * (1.0f - tx) + max11 * tx;
                maxZ = top0 * (1.0f - ty) + top1 * ty;

                if (hmin != null && hmin.Length == hmax.Length)
                {
                    float min00 = min.Z + stepZ * hmin[y0 * w + x0];
                    float min10 = min.Z + stepZ * hmin[y0 * w + x1];
                    float min01 = min.Z + stepZ * hmin[y1 * w + x0];
                    float min11 = min.Z + stepZ * hmin[y1 * w + x1];
                    float bot0 = min00 * (1.0f - tx) + min10 * tx;
                    float bot1 = min01 * (1.0f - tx) + min11 * tx;
                    minZ = bot0 * (1.0f - ty) + bot1 * ty;
                }
                else
                {
                    minZ = maxZ;
                }

                return true;
            }

            return false;
        }


        public void Init(GameFileCache gameFileCache, Action<string> updateStatus)
        {
            Inited = false;

            GameFileCache = gameFileCache;


            HeightmapFiles.Clear();


            if (gameFileCache.EnableDlc)
            {
                LoadHeightmap("update\\update.rpf\\common\\data\\levels\\gta5\\heightmap.dat");
                LoadHeightmap("update\\update.rpf\\common\\data\\levels\\gta5\\heightmapheistisland.dat");
            }
            else
            {
                LoadHeightmap("common.rpf\\data\\levels\\gta5\\heightmap.dat");
            }


            BuildVertices();

            Inited = true;
        }

        private void LoadHeightmap(string filename)
        {
            var hmf = GameFileCache.RpfMan.GetFile<HeightmapFile>(filename);
            HeightmapFiles.Add(hmf);
        }



        public void BuildVertices()
        {

            var vlist = new List<EditorVertex>();
            var nlist = new List<Vector4>();

            foreach (var hmf in HeightmapFiles)
            {
                BuildHeightmapVertices(hmf, vlist, nlist);
            }

            if (vlist.Count > 0)
            {
                TriangleVerts = vlist.ToArray();
            }
            else
            {
                TriangleVerts = null;
            }
            if (nlist.Count > 0)
            {
                NodePositions = nlist.ToArray();
            }
            else
            {
                NodePositions = null;
            }

        }
        private void BuildHeightmapVertices(HeightmapFile hmf, List<EditorVertex> vl, List<Vector4> nl)
        {
            var v1 = new EditorVertex();
            var v2 = new EditorVertex();
            var v3 = new EditorVertex();
            var v4 = new EditorVertex();

            uint cgrn = (uint)new Color(0, 128, 0, 60).ToRgba();
            uint cyel = (uint)new Color(128, 128, 0, 200).ToRgba();

            var w = hmf.Width;
            var h = hmf.Height;
            var hmin = hmf.MinHeights;
            var hmax = hmf.MaxHeights;
            var min = hmf.BBMin;
            var max = hmf.BBMax;
            var siz = max - min;
            var step = siz / new Vector3(w - 1, h - 1, 255);

            v1.Colour = v2.Colour = v3.Colour = v4.Colour = cyel;
            for (int yi = 1; yi < h; yi++)
            {
                var yo = yi - 1;
                for (int xi = 1; xi < w; xi++)
                {
                    var xo = xi - 1;
                    var o1 = yo * w + xo;
                    var o2 = yo * w + xi;
                    var o3 = yi * w + xo;
                    var o4 = yi * w + xi;
                    v1.Position = min + step * new Vector3(xo, yo, hmin[o1]);
                    v2.Position = min + step * new Vector3(xi, yo, hmin[o2]);
                    v3.Position = min + step * new Vector3(xo, yi, hmin[o3]);
                    v4.Position = min + step * new Vector3(xi, yi, hmin[o4]);
                    vl.Add(v1); vl.Add(v2); vl.Add(v3);
                    vl.Add(v3); vl.Add(v2); vl.Add(v4);
                }
            }
            v1.Colour = v2.Colour = v3.Colour = v4.Colour = cgrn;
            for (int yi = 1; yi < h; yi++)
            {
                var yo = yi - 1;
                for (int xi = 1; xi < w; xi++)
                {
                    var xo = xi - 1;
                    var o1 = yo * w + xo;
                    var o2 = yo * w + xi;
                    var o3 = yi * w + xo;
                    var o4 = yi * w + xi;
                    v1.Position = min + step * new Vector3(xo, yo, hmax[o1]);
                    v2.Position = min + step * new Vector3(xi, yo, hmax[o2]);
                    v3.Position = min + step * new Vector3(xo, yi, hmax[o3]);
                    v4.Position = min + step * new Vector3(xi, yi, hmax[o4]);
                    vl.Add(v1); vl.Add(v2); vl.Add(v3);
                    vl.Add(v3); vl.Add(v2); vl.Add(v4);
                }
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var o = y * w + x;
                    nl.Add(new Vector4(min + step * new Vector3(x, y, hmin[o]), 10));
                    nl.Add(new Vector4(min + step * new Vector3(x, y, hmax[o]), 10));
                }
            }


        }


    }
}
