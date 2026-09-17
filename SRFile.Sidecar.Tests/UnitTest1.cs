using CodeWalker.GameFiles;
using CodeWalker.World;
using SharpDX;
using Xunit;

namespace SRFile.Sidecar.Tests;

public class UnitTest1
{
    [Fact]
    public void Test_Heightmaps_Uninitialized_ReturnsFalse()
    {
        var hm = new Heightmaps();
        bool ok = hm.GetHeight(100f, 100f, out float minZ, out float maxZ);
        Assert.False(ok);
        Assert.Equal(0f, minZ);
        Assert.Equal(0f, maxZ);
    }

    [Fact]
    public void Test_Heightmaps_OutOfBounds_ReturnsFalse()
    {
        var hm = new Heightmaps { Inited = true };
        var hmf = new HeightmapFile
        {
            BBMin = new Vector3(-100, -100, 0),
            BBMax = new Vector3(100, 100, 100),
            Width = 2,
            Height = 2,
            MaxHeights = new byte[] { 0, 0, 0, 0 },
            MinHeights = new byte[] { 0, 0, 0, 0 }
        };
        hm.HeightmapFiles.Add(hmf);

        bool ok = hm.GetHeight(200f, 200f, out float minZ, out float maxZ);
        Assert.False(ok);
    }

    [Fact]
    public void Test_Heightmaps_BilinearInterpolation_CalculatesCorrectElevation()
    {
        var hm = new Heightmaps { Inited = true };
        var hmf = new HeightmapFile
        {
            BBMin = new Vector3(0, 0, 0),
            BBMax = new Vector3(100, 100, 255),
            Width = 2,
            Height = 2,
            // 4 corners: (0,0)=0, (1,0)=100, (0,1)=100, (1,1)=200
            MaxHeights = new byte[] { 0, 100, 100, 200 },
            MinHeights = new byte[] { 0, 50, 50, 100 }
        };
        hm.HeightmapFiles.Add(hmf);

        // Center of cell (50, 50): should be average of all 4 corners = (0 + 100 + 100 + 200) / 4 = 100
        bool ok = hm.GetHeight(50f, 50f, out float minZ, out float maxZ);
        Assert.True(ok);
        Assert.Equal(100f, maxZ, precision: 1);
        Assert.Equal(50f, minZ, precision: 1);

        // Corner (0, 0): should be exactly 0
        bool okCorner00 = hm.GetHeight(0f, 0f, out float minZ00, out float maxZ00);
        Assert.True(okCorner00);
        Assert.Equal(0f, maxZ00, precision: 2);

        // Corner (100, 100): should be exactly 200
        bool okCorner11 = hm.GetHeight(100f, 100f, out float minZ11, out float maxZ11);
        Assert.True(okCorner11);
        Assert.Equal(200f, maxZ11, precision: 2);
    }
}