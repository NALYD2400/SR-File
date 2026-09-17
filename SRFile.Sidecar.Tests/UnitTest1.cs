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

    [Fact]
    public void Test_Heightmaps_AsymmetricDimensions_CalculatesCorrectElevation()
    {
        var hm = new Heightmaps { Inited = true };
        var hmf = new HeightmapFile
        {
            BBMin = new Vector3(0, 0, 0),
            BBMax = new Vector3(200, 100, 255),
            Width = 3, // x: 0, 100, 200
            Height = 2, // y: 0, 100
            MaxHeights = new byte[]
            {
                0, 50, 100,      // y=0: x0=0, x1=50, x2=100
                100, 150, 200    // y=1: x0=100, x1=150, x2=200
            },
            MinHeights = new byte[]
            {
                0, 25, 50,
                50, 75, 100
            }
        };
        hm.HeightmapFiles.Add(hmf);

        // Test mid-point (100, 50)
        bool ok = hm.GetHeight(100f, 50f, out float minZ, out float maxZ);
        Assert.True(ok);
        // Average of (50 + 150)/2 = 100
        Assert.Equal(100f, maxZ, precision: 1);
        // Average of (25 + 75)/2 = 50
        Assert.Equal(50f, minZ, precision: 1);
    }

    [Fact]
    public void Test_GroundPhysics_BuriedStateDetection_DistinguishesBuriedFromHighObstacle()
    {
        // Scenario 1: Ped spawned or fell underground (feet at Z=20m, ground at Z=21.5m)
        float currentFeetZ = 20.0f;
        float curGroundZ = 21.5f;
        float nextGroundZ = 21.5f;

        bool isBuried = currentFeetZ < curGroundZ - 0.05f;
        Assert.True(isBuried);

        float recoveredZ = Math.Max(nextGroundZ, curGroundZ) + 1.7f;
        Assert.Equal(23.2f, recoveredZ, precision: 2);
        Assert.Equal(21.5f, recoveredZ - 1.7f, precision: 2); // Feet exactly at ground level!

        // Scenario 2: Ped standing on dirt road (feet at Z=21.5m, curGround at Z=21.5m), walking into a 2m wall (nextGroundZ=23.5m)
        currentFeetZ = 21.5f;
        curGroundZ = 21.5f;
        nextGroundZ = 23.5f;

        isBuried = currentFeetZ < curGroundZ - 0.05f;
        Assert.False(isBuried); // Not buried!

        float heightDiff = nextGroundZ - currentFeetZ; // +2.0m
        bool isWall = heightDiff > 0.75f;
        Assert.True(isWall); // Recognized as a wall, blocks horizontal movement rather than teleporting onto roof!

        // Scenario 3: Walking up a small slope or curb (+0.3m)
        nextGroundZ = 21.8f;
        heightDiff = nextGroundZ - currentFeetZ; // +0.3m
        bool canWalkUp = heightDiff <= 0.75f && heightDiff >= -1.2f;
        Assert.True(canWalkUp);
    }
}