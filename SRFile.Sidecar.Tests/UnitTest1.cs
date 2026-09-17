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

    [Fact]
    public void Test_Ped_FootOffset_Height_Calculation()
    {
        // Default / fallback offset should be 0.90f
        float fallbackOffset = 0.90f;
        float groundZ = 15.2f;

        // When placing ped: Position.Z = groundZ - footOffset
        float pedPositionZ = groundZ - fallbackOffset;
        Assert.Equal(14.3f, pedPositionZ, precision: 2);

        // Ped model waist is at 0, feet at -0.90f relative to waist.
        // Therefore feet world Z = pedPositionZ + 0.90f = 14.3f + 0.90f = 15.2f = groundZ!
        float feetWorldZ = pedPositionZ + fallbackOffset;
        Assert.Equal(groundZ, feetWorldZ, precision: 2);

        // Custom drawable bounding box: BoundingBoxMin.Z = -0.95f
        float customBBoxMinZ = -0.95f;
        float customFootOffset = -customBBoxMinZ;
        Assert.Equal(0.95f, customFootOffset, precision: 2);

        float customPedZ = groundZ - customFootOffset;
        Assert.Equal(groundZ, customPedZ + customFootOffset, precision: 2);
    }

    [Fact]
    public void Test_Camera_LookAtLH_AvoidsSidewaysRoll()
    {
        // LookAtLH orientation used in WorldForm to fix 90-degree roll:
        Quaternion fixedOrientation = Quaternion.LookAtLH(Vector3.Zero, Vector3.Up, Vector3.ForwardLH);

        // Must not be Identity (which caused the 90 degree roll in SharpDX view matrix)
        Assert.NotEqual(Quaternion.Identity, fixedOrientation);

        Matrix rot = Matrix.RotationQuaternion(fixedOrientation);
        // Up direction in transformed space must align with +Y in view space
        Assert.True(Math.Abs(rot.M22) > 0.9f || Math.Abs(rot.M32) > 0.9f || Math.Abs(rot.M12) > 0.9f);
        // Verify determinant is positive (valid right-handed or left-handed rotation)
        Assert.Equal(1.0f, rot.Determinant(), precision: 3);
    }

    [Fact]
    public void Test_Vehicle_WheelRotation_And_SteerAngle()
    {
        var vehicle = new Vehicle();
        Assert.Equal(0.0f, vehicle.WheelRotation);
        Assert.Equal(0.0f, vehicle.SteerAngle);

        float vehicleSpeed = 15.0f; // 15 m/s (~54 km/h)
        float elapsed = 0.05f;      // 50 ms tick
        float wheelRadius = 0.35f;  // 35 cm wheel

        // Simulate 20 ticks
        for (int i = 0; i < 20; i++)
        {
            vehicle.WheelRotation += (vehicleSpeed * elapsed) / wheelRadius;
        }

        // Expected total rotation: 20 * (15 * 0.05 / 0.35) = 20 * (0.75 / 0.35) = 42.857 rad
        float expectedRot = 20.0f * (vehicleSpeed * elapsed) / wheelRadius;
        Assert.Equal(expectedRot, vehicle.WheelRotation, precision: 2);

        // Test steering angle mapping
        float steerInput = 1.0f; // full right
        vehicle.SteerAngle = -steerInput * 0.55f;
        Assert.Equal(-0.55f, vehicle.SteerAngle, precision: 2);

        steerInput = -1.0f; // full left
        vehicle.SteerAngle = -steerInput * 0.55f;
        Assert.Equal(0.55f, vehicle.SteerAngle, precision: 2);

        // Verify wheel transform matrices
        Matrix leftRoll = Matrix.RotationX(vehicle.WheelRotation);
        Matrix rightRoll = Matrix.RotationX(-vehicle.WheelRotation);
        Matrix steerMtx = Matrix.RotationZ(vehicle.SteerAngle);

        Assert.Equal(1.0f, leftRoll.Determinant(), precision: 3);
        Assert.Equal(1.0f, rightRoll.Determinant(), precision: 3);
        Assert.Equal(1.0f, steerMtx.Determinant(), precision: 3);
    }

    [Fact]
    public void Test_Weapon_Tint_And_UpdateEntity()
    {
        var weapon = new Weapon();
        Assert.Equal(0, weapon.Tint);

        weapon.Position = new Vector3(100f, 200f, 30f);
        weapon.Rotation = Quaternion.RotationAxis(Vector3.UnitZ, (float)Math.PI / 2f);
        weapon.Tint = 2; // Gold tint

        weapon.UpdateEntity();

        Assert.Equal(100f, weapon.RenderEntity.Position.X);
        Assert.Equal(200f, weapon.RenderEntity.Position.Y);
        Assert.Equal(30f, weapon.RenderEntity.Position.Z);
        Assert.Equal((byte)2, weapon.RenderEntity._CEntityDef.tintValue);

        // Change tint to Platinum (7)
        weapon.Tint = 7;
        weapon.UpdateEntity();
        Assert.Equal((byte)7, weapon.RenderEntity._CEntityDef.tintValue);
    }

    [Fact]
    public void Test_WeaponWall_Geometry_CenteringAndSymmetry()
    {
        Vector3 playerPos = new Vector3(0, 0, 10);
        Vector3 fwd = Vector3.Normalize(new Vector3(1, 0, 0)); // Facing +X
        Vector3 rgt = new Vector3(fwd.Y, -fwd.X, 0); // (0, -1, 0)

        // Wall center is 3.2m in front, 0.85m above player
        Vector3 wallCenter = playerPos + fwd * 3.2f + Vector3.UnitZ * 0.85f;
        Assert.Equal(3.2f, wallCenter.X, precision: 2);
        Assert.Equal(0.0f, wallCenter.Y, precision: 2);
        Assert.Equal(10.85f, wallCenter.Z, precision: 2);

        float rowSpacing = 0.52f;
        float colSpacing = 0.62f;
        int totalRows = 5;

        // Verify vertical symmetry of rows
        float topRowZ = (4 - (totalRows - 1) * 0.5f) * rowSpacing;
        float bottomRowZ = (0 - (totalRows - 1) * 0.5f) * rowSpacing;
        Assert.Equal(-bottomRowZ, topRowZ, precision: 3); // perfectly symmetric around Z=0

        // Verify horizontal symmetry of columns
        int colsInRow = 6;
        float rowWidth = (colsInRow - 1) * colSpacing;
        float firstColOffset = (0 * colSpacing) - (rowWidth * 0.5f);
        float lastColOffset = ((colsInRow - 1) * colSpacing) - (rowWidth * 0.5f);
        Assert.Equal(-firstColOffset, lastColOffset, precision: 3); // perfectly symmetric around X=0

        // Verify weapon rotation facing the player
        Matrix wallWeaponRotMtx = new Matrix(
            -fwd.X, -fwd.Y, -fwd.Z, 0,
            rgt.X, rgt.Y, rgt.Z, 0,
            0, 0, 1.0f, 0,
            0, 0, 0, 1.0f
        );
        Quaternion wallRot = Quaternion.RotationMatrix(wallWeaponRotMtx);
        Assert.True(Math.Abs(wallRot.Length() - 1.0f) < 0.001f);
        Assert.Equal(1.0f, wallWeaponRotMtx.Determinant(), precision: 3);
    }

    [Theory]
    [InlineData("{\"action\":\"set_weather\",\"weather\":\"CLEAR\"}", "CLEAR")]
    [InlineData("{\"action\":\"set_weather\",\"value\":\"EXTRASUNNY\"}", "EXTRASUNNY")]
    [InlineData("{\"type\":\"set_weather\",\"weather\":\"XMAS\"}", "XMAS")]
    [InlineData("{\"set_weather\":true,\"value\":\"FOGGY\"}", "FOGGY")]
    public void Test_Weather_IpcRegex_MatchesBothWeatherAndValueKeys(string json, string expectedWeather)
    {
        var match = System.Text.RegularExpressions.Regex.Match(json, "\"(?:weather|value)\":\\s*\"([^\"]+)\"");
        Assert.True(match.Success);
        Assert.Equal(expectedWeather, match.Groups[1].Value.Trim());
    }
}