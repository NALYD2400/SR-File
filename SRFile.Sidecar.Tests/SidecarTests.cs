using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using SRFile.Sidecar.Models;
using SRFile.Sidecar.Services;
using SRFile.Sidecar.Utils;
using Xunit;

namespace SRFile.Sidecar.Tests
{
    public class PngEncoderTests
    {
        [Fact]
        public void EncodeRgba_NullOrInsufficientBuffer_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => PngEncoder.EncodeRgba(null!, 10, 10));
            Assert.Throws<ArgumentException>(() => PngEncoder.EncodeRgba(new byte[10], 10, 10));
        }

        [Fact]
        public void EncodeRgba_ValidBuffer_ProducesValidPngStream()
        {
            int width = 8;
            int height = 8;
            byte[] pixels = new byte[width * height * 4];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;     // R
                pixels[i + 1] = 122; // G (SR Orange: #FF7A29)
                pixels[i + 2] = 41;  // B
                pixels[i + 3] = 255; // A
            }

            byte[] png = PngEncoder.EncodeRgba(pixels, width, height);

            Assert.NotNull(png);
            Assert.True(png.Length > 8);

            // 1. Check 8-byte PNG signature
            byte[] expectedSig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            for (int i = 0; i < expectedSig.Length; i++)
            {
                Assert.Equal(expectedSig[i], png[i]);
            }

            // 2. Check IHDR chunk
            string ihdrType = Encoding.ASCII.GetString(png, 12, 4);
            Assert.Equal("IHDR", ihdrType);

            // Check dimensions in IHDR
            int encodedW = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            int encodedH = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
            Assert.Equal(width, encodedW);
            Assert.Equal(height, encodedH);

            byte bitDepth = png[24];
            byte colorType = png[25];
            Assert.Equal(8, bitDepth);
            Assert.Equal(6, colorType); // 6 = RGBA

            // 3. Check IEND chunk at the end
            string iendType = Encoding.ASCII.GetString(png, png.Length - 8, 4);
            Assert.Equal("IEND", iendType);
        }

        [Theory]
        [InlineData(16, 16, 0)]
        [InlineData(16, 16, 1)]
        [InlineData(16, 16, 2)]
        [InlineData(16, 16, 3)]
        public void EncodeRgba_MipLevels_EncodesProperDimensions(int baseW, int baseH, int mip)
        {
            int mipW = Math.Max(1, baseW >> mip);
            int mipH = Math.Max(1, baseH >> mip);
            byte[] pixels = new byte[mipW * mipH * 4];

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;
                pixels[i + 1] = 122;
                pixels[i + 2] = 41;
                pixels[i + 3] = 255;
            }

            byte[] png = PngEncoder.EncodeRgba(pixels, mipW, mipH);
            Assert.NotNull(png);

            int encodedW = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            int encodedH = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
            Assert.Equal(mipW, encodedW);
            Assert.Equal(mipH, encodedH);
        }
    }

    public class RpfServiceTests
    {
        [Fact]
        public void GetStatus_ReturnsRunningStatusWithDefaults()
        {
            var service = new RpfService();
            var status = service.GetStatus();

            Assert.NotNull(status);
            Assert.True(status.Running);
            Assert.Equal("1.0.0", status.Version);
            Assert.Equal(0, status.LoadedRpfsCount);
        }

        [Fact]
        public void ConfigureGtaFolder_EmptyPath_ReturnsFalse()
        {
            var service = new RpfService();
            bool result = service.ConfigureGtaFolder("", false, null);
            Assert.False(result);
        }

        [Fact]
        public void OpenRpf_NonExistentFile_ThrowsFileNotFoundException()
        {
            var service = new RpfService();
            Assert.Throws<FileNotFoundException>(() => service.OpenRpf("C:\\non_existent_archive_12345.rpf"));
        }

        [Fact]
        public void GetEntries_UnopenedRpf_ThrowsFileNotFoundException()
        {
            var service = new RpfService();
            Assert.Throws<FileNotFoundException>(() => service.GetEntries("C:\\not_loaded.rpf", ""));
        }

        [Fact]
        public void ExtractFile_UnopenedRpf_ThrowsFileNotFoundException()
        {
            var service = new RpfService();
            Assert.Throws<FileNotFoundException>(() => service.ExtractFile("C:\\not_loaded.rpf", "file.xml"));
        }

        [Fact]
        public void RpfService_RealArchive_Creation_Opening_Traversal_And_Extraction()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string rpfFilePath = Path.Combine(tempDir, "test.rpf");

            try
            {
                // 1. Create a genuine unencrypted OpenIV RPF archive using CodeWalker.Core
                var rpf = CodeWalker.GameFiles.RpfFile.CreateNew(tempDir, "test.rpf", CodeWalker.GameFiles.RpfEncryption.OPEN);
                Assert.NotNull(rpf);
                Assert.True(File.Exists(rpfFilePath));

                // 2. Add subdirectories
                var dataDir = CodeWalker.GameFiles.RpfFile.CreateDirectory(rpf.Root, "data");
                Assert.NotNull(dataDir);

                var commonDir = CodeWalker.GameFiles.RpfFile.CreateDirectory(dataDir, "common");
                Assert.NotNull(commonDir);

                // 3. Add text/xml files into subdirectories
                string xmlSettingsContent = "<settings><volume>100</volume><resolution>1080p</resolution></settings>";
                byte[] settingsBytes = Encoding.UTF8.GetBytes(xmlSettingsContent);
                CodeWalker.GameFiles.RpfFile.CreateFile(dataDir, "settings.xml", settingsBytes, true);

                string xmlFrontendContent = "<ui><theme>dark</theme><accent>orange</accent></ui>";
                byte[] frontendBytes = Encoding.UTF8.GetBytes(xmlFrontendContent);
                CodeWalker.GameFiles.RpfFile.CreateFile(commonDir, "frontend.xml", frontendBytes, true);

                // 4. Test RpfService opening the archive
                var service = new RpfService();
                var info = service.OpenRpf(rpfFilePath);

                Assert.NotNull(info);
                Assert.Equal("test.rpf", info.Name);
                Assert.True(info.TotalFolderCount >= 2);
                Assert.True(info.TotalFileCount >= 2);

                // 5. Test root entry listing
                var rootEntries = service.GetEntries(rpfFilePath, "");
                Assert.NotEmpty(rootEntries);
                var rootDataDir = rootEntries.FirstOrDefault(e => e.Name.Equals("data", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(rootDataDir);
                Assert.True(rootDataDir.IsDirectory);

                // 6. Test subfolder listing using path with archive prefix (as returned by DTO)
                var dataEntriesWithPrefix = service.GetEntries(rpfFilePath, rootDataDir.Path);
                Assert.NotEmpty(dataEntriesWithPrefix);
                Assert.Contains(dataEntriesWithPrefix, e => e.Name.Equals("common", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(dataEntriesWithPrefix, e => e.Name.Equals("settings.xml", StringComparison.OrdinalIgnoreCase));

                // 7. Test subfolder listing using relative path
                var dataEntriesRelative = service.GetEntries(rpfFilePath, "data");
                Assert.NotEmpty(dataEntriesRelative);
                Assert.Contains(dataEntriesRelative, e => e.Name.Equals("common", StringComparison.OrdinalIgnoreCase));

                // 8. Test deeper nested directory listing
                var commonEntries = service.GetEntries(rpfFilePath, "test.rpf\\data\\common");
                Assert.NotEmpty(commonEntries);
                Assert.Contains(commonEntries, e => e.Name.Equals("frontend.xml", StringComparison.OrdinalIgnoreCase));

                // 9. Test file text extraction with full archive path
                string extractedSettings1 = service.ExtractFileText(rpfFilePath, "test.rpf\\data\\settings.xml");
                Assert.Equal(xmlSettingsContent, extractedSettings1);

                // 10. Test file text extraction with relative path
                string extractedSettings2 = service.ExtractFileText(rpfFilePath, "data\\settings.xml");
                Assert.Equal(xmlSettingsContent, extractedSettings2);

                // 11. Test file text extraction with filename fallback
                string extractedFrontend = service.ExtractFileText(rpfFilePath, "frontend.xml");
                Assert.Equal(xmlFrontendContent, extractedFrontend);

                // 12. Test search
                var searchFrontend = service.SearchEntries(rpfFilePath, "frontend");
                Assert.NotEmpty(searchFrontend);
                Assert.Equal("frontend.xml", searchFrontend[0].Name);

                var searchSettings = service.SearchEntries(rpfFilePath, "settings");
                Assert.NotEmpty(searchSettings);
                Assert.Equal("settings.xml", searchSettings[0].Name);
            }
            finally
            {
                // Cleanup temp folder
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch { }
            }
        }

        [Fact]
        public void RpfService_NestedChildRpf_Creation_And_Traversal()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_ChildRpf_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string rootRpfPath = Path.Combine(tempDir, "root.rpf");

            try
            {
                // 1. Create root RPF
                var rootRpf = CodeWalker.GameFiles.RpfFile.CreateNew(tempDir, "root.rpf", CodeWalker.GameFiles.RpfEncryption.OPEN);
                var packDir = CodeWalker.GameFiles.RpfFile.CreateDirectory(rootRpf.Root, "dlcpack");

                // 2. Create child RPF inside packDir
                var childRpf = CodeWalker.GameFiles.RpfFile.CreateNew(packDir, "content.rpf", CodeWalker.GameFiles.RpfEncryption.OPEN);
                Assert.NotNull(childRpf);

                // 3. Add directory and file inside child RPF
                var audioDir = CodeWalker.GameFiles.RpfFile.CreateDirectory(childRpf.Root, "audio");
                string trackText = "STREAM_AUDIO_DATA_TEST";
                CodeWalker.GameFiles.RpfFile.CreateFile(audioDir, "track.dat", Encoding.UTF8.GetBytes(trackText), true);

                // 4. Open with RpfService
                var service = new RpfService();
                var info = service.OpenRpf(rootRpfPath);
                Assert.NotNull(info);

                // 5. List entries in dlcpack
                var dlcEntries = service.GetEntries(rootRpfPath, "root.rpf\\dlcpack");
                Assert.NotEmpty(dlcEntries);
                var childEntry = dlcEntries.FirstOrDefault(e => e.Name.Equals("content.rpf", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(childEntry);
                Assert.Equal("rpf", childEntry.ResourceType);

                // 6. Navigate into child RPF using its path
                var childRootEntries = service.GetEntries(rootRpfPath, childEntry.Path);
                Assert.NotEmpty(childRootEntries);
                Assert.Contains(childRootEntries, e => e.Name.Equals("audio", StringComparison.OrdinalIgnoreCase));

                // 7. Extract file from inside child RPF
                string extracted = service.ExtractFileText(rootRpfPath, "track.dat");
                Assert.Equal(trackText, extracted);

                // 8. Search across nested hierarchy
                var search = service.SearchEntries(rootRpfPath, "track");
                Assert.NotEmpty(search);
                Assert.Equal("track.dat", search[0].Name);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch { }
            }
        }
    }

    public class ModManagerServiceTests
    {
        [Fact]
        public void ModManager_Scanning_ConflictDetection_And_Toggle()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_ModTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string modsDir = Path.Combine(tempDir, "mods");
            Directory.CreateDirectory(modsDir);

            try
            {
                // Mod 1
                string mod1Dir = Path.Combine(modsDir, "SuperCar");
                Directory.CreateDirectory(Path.Combine(mod1Dir, "update", "x64", "dlcpacks"));
                File.WriteAllBytes(Path.Combine(mod1Dir, "update", "x64", "dlcpacks", "dlc.rpf"), new byte[] { 1, 2, 3 });
                File.WriteAllText(Path.Combine(mod1Dir, "common.meta"), "<meta>test</meta>");

                // Mod 2 (Conflicts on common.meta)
                string mod2Dir = Path.Combine(modsDir, "WeatherMod");
                Directory.CreateDirectory(mod2Dir);
                File.WriteAllText(Path.Combine(mod2Dir, "common.meta"), "<meta>weather</meta>");

                var rpfService = new RpfService();
                var modManager = new ModManagerService(rpfService);

                var mods = modManager.GetMods(tempDir);
                Assert.Equal(2, mods.Count);

                var mod1 = mods.FirstOrDefault(m => m.Name == "SuperCar");
                var mod2 = mods.FirstOrDefault(m => m.Name == "WeatherMod");
                Assert.NotNull(mod1);
                Assert.NotNull(mod2);
                Assert.Equal("DLC", mod1.Type);
                Assert.True(mod1.IsEnabled);

                // Conflict detected on common.meta
                Assert.NotEmpty(mod1.Conflicts);
                Assert.NotEmpty(mod2.Conflicts);

                // Toggle disable mod2
                bool toggleRes = modManager.ToggleMod("weathermod", false, tempDir);
                Assert.True(toggleRes);

                // After disable, conflict should be resolved
                var modsAfter = modManager.GetMods(tempDir);
                var mod1After = modsAfter.FirstOrDefault(m => m.Name == "SuperCar");
                var mod2After = modsAfter.FirstOrDefault(m => m.Name == "WeatherMod");
                Assert.NotNull(mod2After);
                Assert.False(mod2After.IsEnabled);
                Assert.Empty(mod1After!.Conflicts);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void ModManager_InspectOiv_ParsesMetadata()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_OivTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string oivPath = Path.Combine(tempDir, "test_mod.oiv");

            try
            {
                using (var zip = System.IO.Compression.ZipFile.Open(oivPath, System.IO.Compression.ZipArchiveMode.Create))
                {
                    var entry = zip.CreateEntry("assembly.xml");
                    using var writer = new StreamWriter(entry.Open());
                    writer.Write("<package><name>HD Roads 4K</name><author>ModderPro</author><version>2.5</version><description>High resolution asphalt</description><content><archive path=\"update/update.rpf\"/></content></package>");
                }

                var rpfService = new RpfService();
                var modManager = new ModManagerService(rpfService);

                var manifest = modManager.InspectOiv(oivPath);
                Assert.NotNull(manifest);
                Assert.Equal("HD Roads 4K", manifest.Name);
                Assert.Equal("ModderPro", manifest.Author);
                Assert.Equal("2.5", manifest.Version);
                Assert.Single(manifest.TargetComponents);
                Assert.Equal("update/update.rpf", manifest.TargetComponents[0]);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
    }

    public class Gen9ConverterServiceTests
    {
        [Fact]
        public void Gen9Converter_ServiceWorkflow_And_Logging()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_Gen9Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string inputDir = Path.Combine(tempDir, "input");
            string outputDir = Path.Combine(tempDir, "output");
            Directory.CreateDirectory(inputDir);

            try
            {
                // Create dummy loose files
                File.WriteAllText(Path.Combine(inputDir, "test.txt"), "SRFile converter test");

                var service = new Gen9ConverterService();
                var initialStatus = service.GetStatus();
                Assert.False(initialStatus.IsConverting);

                var req = new Models.Gen9ConversionRequest(
                    InputPath: inputDir,
                    OutputPath: outputDir,
                    Preset: "gen9_to_pc",
                    ProcessSubfolders: true,
                    OverwriteExisting: true,
                    CopyUnconverted: true
                );

                var started = service.StartConversion(req);
                Assert.NotNull(started);

                // Wait for task completion
                System.Threading.Thread.Sleep(500);

                var finalStatus = service.GetStatus();
                Assert.NotEmpty(finalStatus.Logs);
                Assert.True(File.Exists(Path.Combine(outputDir, "test.txt")));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
    }

    public class CryptoServiceTests
    {
        [Fact]
        public void JoaatHash_Calculation_And_DictionaryLookup()
        {
            var service = new CryptoService();
            string testString = "prop_barrier_work05";

            var res = service.ComputeJoaat(testString, "utf8");
            Assert.NotNull(res);
            Assert.NotEqual(0u, res.HashUint);
            Assert.StartsWith("0x", res.HashHex);

            // Lookup reverse
            string? found = service.LookupHash(res.HashUint);
            Assert.Equal(testString, found);

            // Batch hash
            var batch = service.BatchCompute(new List<string> { "door", "wheel", "light" });
            Assert.Equal(3, batch.Count);

            // Dictionary search
            var search = service.SearchDictionary("barrier", 10);
            Assert.NotEmpty(search);
            Assert.Contains(search, s => s.Text.Contains("barrier"));
        }

        [Fact]
        public void CryptoKeys_Status_And_SetAesKey()
        {
            var service = new CryptoService();
            var status = service.GetKeysStatus();
            Assert.NotNull(status);

            // 64-char hex key for 32 bytes (AES-256)
            string sampleHex = "00112233445566778899AABBCCDDEEFF00112233445566778899AABBCCDDEEFF";
            bool ok = service.SetAesKey(sampleHex);
            Assert.True(ok);

            var updatedStatus = service.GetKeysStatus();
            Assert.True(updatedStatus.AesKeyLoaded);
            Assert.Equal(sampleHex, updatedStatus.AesKeyHex);
        }

        [Fact]
        public void InspectRpfEncryption_RealArchive_ReturnsOpen()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_EncTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string rpfPath = Path.Combine(tempDir, "test_enc.rpf");

            try
            {
                var rpf = CodeWalker.GameFiles.RpfFile.CreateNew(tempDir, "test_enc.rpf", CodeWalker.GameFiles.RpfEncryption.OPEN);
                Assert.NotNull(rpf);

                var service = new CryptoService();
                var encInfo = service.InspectRpfEncryption(rpfPath);
                Assert.True(encInfo.IsValidHeader);
                Assert.Contains("OPEN", encInfo.Encryption);
                Assert.Equal(7u, encInfo.Version);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
    }

    public class ProjectEditorServiceTests
    {
        [Fact]
        public void ProjectEditor_Create_Save_Open_Workflow()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_ProjTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string projPath = Path.Combine(tempDir, "test.cwproj");

            try
            {
                var service = new ProjectEditorService();
                var created = service.CreateProject("Mon Projet GTA V");
                Assert.Equal("Mon Projet GTA V", created.Name);

                var saveReq = new Models.SaveProjectRequest(
                    FilePath: projPath,
                    Name: "Mon Projet GTA V",
                    Version: 1,
                    YmapFiles: new List<string> { "map1.ymap", "map2.ymap" },
                    YtypFiles: new List<string> { "types1.ytyp" },
                    YbnFiles: new List<string> { "collision.ybn" }
                );

                bool saved = service.SaveProject(saveReq);
                Assert.True(saved);
                Assert.True(File.Exists(projPath));

                // Open back
                var opened = service.OpenProject(projPath);
                Assert.Equal("Mon Projet GTA V", opened.Name);
                Assert.Equal(2, opened.YmapFiles.Count);
                Assert.Single(opened.YtypFiles);
                Assert.Single(opened.YbnFiles);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void ProjectEditor_ExportAndImportYmapXml_RoundTrip()
        {
            var service = new ProjectEditorService();
            var entities = new List<Models.YmapEntityDto>
            {
                new Models.YmapEntityDto(
                    Name: "prop_barrier_1",
                    ArchetypeName: "prop_barrier_work05",
                    Position: new Models.Vector3Dto(100.5f, 200.25f, 50.125f),
                    Rotation: new Models.Vector4Dto(0f, 0f, 0f, 1f),
                    EulerRotation: new Models.Vector3Dto(0f, 0f, 0f),
                    LodDist: 200f,
                    ChildLodDist: 0f,
                    Flags: 32,
                    Guid: 42
                )
            };

            string xml = service.ExportYmapXml("custom_zone", entities);
            Assert.Contains("<CMapData>", xml);
            Assert.Contains("prop_barrier_work05", xml);
            Assert.Contains("100.500000", xml);

            var imported = service.ImportYmapXml(xml);
            Assert.Single(imported);
            Assert.Equal("prop_barrier_work05", imported[0].ArchetypeName);
            Assert.Equal(100.5f, imported[0].Position.X);
            Assert.Equal(200.25f, imported[0].Position.Y);
            Assert.Equal(50.125f, imported[0].Position.Z);
            Assert.Equal(32u, imported[0].Flags);
        }

        [Fact]
        public void ProjectEditor_EulerAndQuaternion_ConversionConsistency()
        {
            // Test 90 degree yaw
            var quat = ProjectEditorService.EulerToQuaternion(0f, 0f, 90f);
            var euler = ProjectEditorService.QuaternionToEuler(quat.X, quat.Y, quat.Z, quat.W);

            Assert.InRange(euler.X, -0.01f, 0.01f);
            Assert.InRange(euler.Y, -0.01f, 0.01f);
            Assert.InRange(euler.Z, 89.9f, 90.1f);
        }

        [Fact]
        public void ProjectEditor_ImportYmapXml_AttributeAndElementVectorSyntax()
        {
            var service = new ProjectEditorService();

            // XML with attribute syntax (standard CodeWalker YMAP)
            string attributeXml = @"<CMapData>
  <entities>
    <Item>
      <archetypeName>prop_lamp_01</archetypeName>
      <position x=""123.500000"" y=""456.750000"" z=""78.250000"" />
      <rotation x=""0.000000"" y=""0.000000"" z=""0.707106"" w=""0.707106"" />
      <lodDist value=""150.000000"" />
      <flags value=""32"" />
      <guid value=""1001"" />
    </Item>
  </entities>
</CMapData>";

            var importedAttr = service.ImportYmapXml(attributeXml);
            Assert.Single(importedAttr);
            Assert.Equal("prop_lamp_01", importedAttr[0].ArchetypeName);
            Assert.Equal(123.5f, importedAttr[0].Position.X);
            Assert.Equal(456.75f, importedAttr[0].Position.Y);
            Assert.Equal(78.25f, importedAttr[0].Position.Z);
            Assert.Equal(150f, importedAttr[0].LodDist);
            Assert.Equal(32u, importedAttr[0].Flags);
            Assert.Equal(1001u, importedAttr[0].Guid);

            // XML with element syntax
            string elementXml = @"<CMapData>
  <entities>
    <Item>
      <archetypeName>prop_tree_02</archetypeName>
      <position>
        <x>321.25</x>
        <y>654.5</y>
        <z>12.0</z>
      </position>
      <rotation>
        <x>0.0</x>
        <y>0.0</y>
        <z>0.0</z>
        <w>1.0</w>
      </rotation>
      <lodDist>200.0</lodDist>
      <flags>64</flags>
      <guid>2002</guid>
    </Item>
  </entities>
</CMapData>";

            var importedElem = service.ImportYmapXml(elementXml);
            Assert.Single(importedElem);
            Assert.Equal("prop_tree_02", importedElem[0].ArchetypeName);
            Assert.Equal(321.25f, importedElem[0].Position.X);
            Assert.Equal(654.5f, importedElem[0].Position.Y);
            Assert.Equal(12.0f, importedElem[0].Position.Z);
            Assert.Equal(200f, importedElem[0].LodDist);
            Assert.Equal(64u, importedElem[0].Flags);
            Assert.Equal(2002u, importedElem[0].Guid);
        }

        [Fact]
        public void ProjectEditor_ImportYmapXml_EmptyOrMalformed_ThrowsExceptions()
        {
            var service = new ProjectEditorService();
            Assert.Throws<ArgumentException>(() => service.ImportYmapXml(""));
            Assert.Throws<ArgumentException>(() => service.ImportYmapXml("   \n\t   "));
            Assert.Throws<System.Xml.XmlException>(() => service.ImportYmapXml("<not_closed_tag>something"));
        }
    }

    public class ModManagerAdvancedServiceTests
    {
        [Fact]
        public void GetMods_ScansFolder_DetectsEnabledAndDisabledMods()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_ModTests_" + Guid.NewGuid().ToString("N"));
            string modsDir = Path.Combine(tempDir, "mods");
            Directory.CreateDirectory(modsDir);

            string enabledModDir = Path.Combine(modsDir, "pack_cars");
            Directory.CreateDirectory(enabledModDir);
            File.WriteAllText(Path.Combine(enabledModDir, "car.yft"), "dummy car content");

            string disabledModDir = Path.Combine(modsDir, "pack_weapons.disabled");
            Directory.CreateDirectory(disabledModDir);
            File.WriteAllText(Path.Combine(disabledModDir, "weapon.ydr"), "dummy weapon content");

            try
            {
                var rpfService = new RpfService();
                var modManager = new ModManagerService(rpfService);

                var mods = modManager.GetMods(tempDir);
                Assert.Equal(2, mods.Count);

                var carMod = mods.FirstOrDefault(m => m.Name.Equals("pack_cars", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(carMod);
                Assert.True(carMod.IsEnabled);
                Assert.Single(carMod.Files);

                var weaponMod = mods.FirstOrDefault(m => m.Name.Equals("pack_weapons", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(weaponMod);
                Assert.False(weaponMod.IsEnabled);
                Assert.Single(weaponMod.Files);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void ToggleMod_RenamesFoldersProperly()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_ModToggleTests_" + Guid.NewGuid().ToString("N"));
            string modsDir = Path.Combine(tempDir, "mods");
            Directory.CreateDirectory(modsDir);

            string enabledModDir = Path.Combine(modsDir, "my_mod");
            Directory.CreateDirectory(enabledModDir);
            File.WriteAllText(Path.Combine(enabledModDir, "file.txt"), "mod file");

            try
            {
                var rpfService = new RpfService();
                var modManager = new ModManagerService(rpfService);

                // Disable it
                bool disabledOk = modManager.ToggleMod("my_mod", false, tempDir);
                Assert.True(disabledOk);
                Assert.False(Directory.Exists(Path.Combine(modsDir, "my_mod")));
                Assert.True(Directory.Exists(Path.Combine(modsDir, "my_mod.disabled")));

                // Re-enable it
                bool enabledOk = modManager.ToggleMod("my_mod", true, tempDir);
                Assert.True(enabledOk);
                Assert.True(Directory.Exists(Path.Combine(modsDir, "my_mod")));
                Assert.False(Directory.Exists(Path.Combine(modsDir, "my_mod.disabled")));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void GetConflicts_DetectsOverlappingFiles()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_ConflictTests_" + Guid.NewGuid().ToString("N"));
            string modsDir = Path.Combine(tempDir, "mods");
            Directory.CreateDirectory(modsDir);

            string modADir = Path.Combine(modsDir, "modA");
            Directory.CreateDirectory(Path.Combine(modADir, "dlc"));
            File.WriteAllText(Path.Combine(modADir, "dlc", "shared.rpf"), "A content");

            string modBDir = Path.Combine(modsDir, "modB");
            Directory.CreateDirectory(Path.Combine(modBDir, "dlc"));
            File.WriteAllText(Path.Combine(modBDir, "dlc", "shared.rpf"), "B content");

            try
            {
                var rpfService = new RpfService();
                var modManager = new ModManagerService(rpfService);

                var mods = modManager.GetMods(tempDir);
                Assert.Equal(2, mods.Count);
                var modA = mods.FirstOrDefault(m => m.Name == "modA");
                var modB = mods.FirstOrDefault(m => m.Name == "modB");
                Assert.NotNull(modA);
                Assert.NotNull(modB);
                Assert.NotEmpty(modA.Conflicts);
                Assert.NotEmpty(modB.Conflicts);
                Assert.Contains(modA.Conflicts, c => c.Contains("shared.rpf") && c.Contains("modB"));
                Assert.Contains(modB.Conflicts, c => c.Contains("shared.rpf") && c.Contains("modA"));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void InspectOiv_ParsesValidAssemblyXml()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_OivTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string oivPath = Path.Combine(tempDir, "sample.oiv");

            try
            {
                string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<package version=\"2.0\" id=\"test-pack\">\n  <metadata>\n    <name>Super Realistic Visuals</name>\n    <version>2.5.0</version>\n    <author>SR Team</author>\n    <description>Enhances lighting and textures</description>\n  </metadata>\n</package>";
                
                using (var zip = System.IO.Compression.ZipFile.Open(oivPath, System.IO.Compression.ZipArchiveMode.Create))
                {
                    var entry = zip.CreateEntry("assembly.xml");
                    using var writer = new StreamWriter(entry.Open());
                    writer.Write(xml);
                }

                var rpfService = new RpfService();
                var modManager = new ModManagerService(rpfService);

                var manifest = modManager.InspectOiv(oivPath);
                Assert.NotNull(manifest);
                Assert.Equal("Super Realistic Visuals", manifest.Name);
                Assert.Equal("2.5.0", manifest.Version);
                Assert.Equal("SR Team", manifest.Author);
                Assert.Equal("Enhances lighting and textures", manifest.Description);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
    }

    public class FileStreamHelperTests
    {
        [Fact]
        public void ReadFileBytes_ChunkedStreaming_ReturnsExactSlices()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "SRFile_ChunkTest_" + Guid.NewGuid().ToString("N") + ".bin");
            try
            {
                byte[] sourceBytes = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    sourceBytes[i] = (byte)i;
                }
                File.WriteAllBytes(tempFile, sourceBytes);

                long startOffset = 50;
                int readLen = 20;

                byte[] buffer = new byte[readLen];
                using (var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Seek(startOffset, SeekOrigin.Begin);
                    int bytesRead = fs.Read(buffer, 0, readLen);
                    Assert.Equal(readLen, bytesRead);
                }

                for (int i = 0; i < readLen; i++)
                {
                    Assert.Equal((byte)(50 + i), buffer[i]);
                }

                string base64 = Convert.ToBase64String(buffer);
                byte[] decoded = Convert.FromBase64String(base64);
                Assert.Equal(buffer, decoded);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }
    }

    public class RpfCacheAndBatchExtractionTests
    {
        [Fact]
        public void CacheStats_TracksHitsMissesAndClearsProperly()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_CacheTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string rpfPath = Path.Combine(tempDir, "cache_test.rpf");

            try
            {
                var rpf = CodeWalker.GameFiles.RpfFile.CreateNew(tempDir, "cache_test.rpf", CodeWalker.GameFiles.RpfEncryption.OPEN);
                var dir = CodeWalker.GameFiles.RpfFile.CreateDirectory(rpf.Root, "cfg");
                byte[] content = Encoding.UTF8.GetBytes("speed=250");
                CodeWalker.GameFiles.RpfFile.CreateFile(dir, "tuning.meta", content, true);

                var service = new RpfService();
                var info = service.OpenRpf(rpfPath);
                Assert.NotNull(info);

                var initialStats = service.GetCacheStats();
                Assert.Equal(1, initialStats.LoadedRpfsCount);
                Assert.NotEmpty(initialStats.OpenArchives);

                // First access - can be miss or index hit
                byte[] data1 = service.ExtractFile(rpfPath, "cfg\\tuning.meta");
                Assert.Equal(content, data1);

                // Second access - should hit the indexed cache
                byte[] data2 = service.ExtractFile(rpfPath, "cfg\\tuning.meta");
                Assert.Equal(content, data2);

                var updatedStats = service.GetCacheStats();
                Assert.True(updatedStats.CacheHits > 0);

                // Close RPF
                bool closed = service.CloseRpf(rpfPath);
                Assert.True(closed);
                var closedStats = service.GetCacheStats();
                Assert.Equal(0, closedStats.LoadedRpfsCount);

                // Clear Cache
                service.OpenRpf(rpfPath);
                Assert.Equal(1, service.GetCacheStats().LoadedRpfsCount);
                int cleared = service.ClearCache();
                Assert.Equal(1, cleared);
                Assert.Equal(0, service.GetCacheStats().LoadedRpfsCount);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void ExtractFolderToDisk_And_ExtractFolderToZip_WorkCorrectly()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_BatchTest_" + Guid.NewGuid().ToString("N"));
            string exportDir = Path.Combine(tempDir, "exported");
            Directory.CreateDirectory(tempDir);
            string rpfPath = Path.Combine(tempDir, "batch.rpf");

            try
            {
                var rpf = CodeWalker.GameFiles.RpfFile.CreateNew(tempDir, "batch.rpf", CodeWalker.GameFiles.RpfEncryption.OPEN);
                var audioDir = CodeWalker.GameFiles.RpfFile.CreateDirectory(rpf.Root, "audio");
                var sfxDir = CodeWalker.GameFiles.RpfFile.CreateDirectory(audioDir, "sfx");

                byte[] fileA = Encoding.UTF8.GetBytes("AudioTrackA");
                byte[] fileB = Encoding.UTF8.GetBytes("AudioTrackB");
                CodeWalker.GameFiles.RpfFile.CreateFile(audioDir, "meta.dat", fileA, true);
                CodeWalker.GameFiles.RpfFile.CreateFile(sfxDir, "horn.wav", fileB, true);

                var service = new RpfService();
                service.OpenRpf(rpfPath);

                // 1. Extract Folder to Disk
                var diskResult = service.ExtractFolderToDisk(rpfPath, "audio", exportDir, recursive: true);
                Assert.True(diskResult.Success);
                Assert.Equal(2, diskResult.ExtractedCount);
                Assert.Equal(0, diskResult.ErrorCount);
                Assert.True(File.Exists(Path.Combine(exportDir, "meta.dat")));
                Assert.True(File.Exists(Path.Combine(exportDir, "sfx", "horn.wav")));

                // 2. Extract Folder to Zip
                byte[] zipBytes = service.ExtractFolderToZip(rpfPath, "audio", recursive: true);
                Assert.NotNull(zipBytes);
                Assert.True(zipBytes.Length > 0);

                using (var zipStream = new MemoryStream(zipBytes))
                using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read))
                {
                    Assert.Equal(2, archive.Entries.Count);
                    Assert.Contains(archive.Entries, e => e.FullName.Replace('\\', '/') == "meta.dat");
                    Assert.Contains(archive.Entries, e => e.FullName.Replace('\\', '/') == "sfx/horn.wav");
                }

                // 3. Extract Batch to Disk
                string batchOut = Path.Combine(tempDir, "batch_out");
                var batchResult = service.ExtractBatchToDisk(rpfPath, new List<string> { "audio\\meta.dat", "non_existent.xml" }, batchOut);
                Assert.False(batchResult.Success);
                Assert.Equal(1, batchResult.ExtractedCount);
                Assert.Equal(1, batchResult.ErrorCount);
                Assert.True(File.Exists(Path.Combine(batchOut, "meta.dat")));

                // 4. Extract Batch to Zip
                byte[] batchZip = service.ExtractBatchToZip(rpfPath, new List<string> { "audio\\meta.dat" });
                using (var bzStream = new MemoryStream(batchZip))
                using (var bzArchive = new System.IO.Compression.ZipArchive(bzStream, System.IO.Compression.ZipArchiveMode.Read))
                {
                    Assert.Single(bzArchive.Entries);
                    Assert.Equal("meta.dat", bzArchive.Entries[0].Name);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
    }

    public class SystemServiceTests
    {
        [Fact]
        public void GetMetrics_ReturnsCompleteDiagnostics()
        {
            var rpfService = new RpfService();
            var systemService = new SystemService(rpfService);

            var metrics = systemService.GetMetrics();

            Assert.NotNull(metrics);
            Assert.True(metrics.ProcessWorkingSetBytes > 0);
            Assert.True(metrics.ProcessWorkingSetMB > 0);
            Assert.True(metrics.ProcessPrivateMemoryBytes > 0);
            Assert.True(metrics.ProcessId > 0);
            Assert.True(metrics.ThreadCount > 0);
            Assert.True(metrics.ProcessorCount > 0);
            Assert.NotEmpty(metrics.OsPlatform);
            Assert.NotEmpty(metrics.FrameworkDescription);
            Assert.Contains(".NET", metrics.FrameworkDescription);
            Assert.True(metrics.UptimeSeconds >= 0);
            Assert.NotNull(metrics.OpenArchives);
            Assert.Equal(0, metrics.OpenArchivesCount);
        }
    }

    public class TextServiceTests
    {
        [Fact]
        public void BuildGxt2_And_ParseGxt2_RoundtripSucceeds()
        {
            var service = new TextService();
            string sourceText = "0x12345678 = Test Subtitle Line\nVEH_TURISMO = Grotti Turismo R\n0xAABBCCDD: Another Weapon";

            byte[] gxtBytes = service.BuildGxt2(sourceText, "american.gxt2");
            Assert.NotNull(gxtBytes);
            Assert.True(gxtBytes.Length > 16);

            // Check GXT2 magic signature (1196971058 or "GXT2")
            uint magic = BitConverter.ToUInt32(gxtBytes, 0);
            Assert.Equal(1196971058u, magic);

            var table = service.ParseGxt2(gxtBytes, "american.gxt2");
            Assert.NotNull(table);
            Assert.Equal(3u, table.EntryCount);
            Assert.Contains(table.Entries, e => e.HexHash == "0x12345678" && e.Text == "Test Subtitle Line");
            Assert.Contains(table.Entries, e => e.Text == "Grotti Turismo R");
            Assert.Contains(table.Entries, e => e.HexHash == "0xAABBCCDD" && e.Text == "Another Weapon");
        }

        [Fact]
        public void SearchGxt2_FindsMatchesBySubstringAndHash()
        {
            var service = new TextService();
            string sourceText = "0x11111111 = Mission Passed\n0x22222222 = Mission Failed\n0x33333333 = Wasted";
            byte[] bytes = service.BuildGxt2(sourceText);
            var table = service.ParseGxt2(bytes);

            // Substring search
            var passedMatches = service.SearchGxt2(table, "Passed");
            Assert.Single(passedMatches);
            Assert.Equal("Mission Passed", passedMatches[0].Text);

            // Hex hash search
            var hexMatches = service.SearchGxt2(table, "0x33333333");
            Assert.Single(hexMatches);
            Assert.Equal("Wasted", hexMatches[0].Text);

            // Empty / no match
            var noMatches = service.SearchGxt2(table, "NonExistentWord");
            Assert.Empty(noMatches);
        }

        [Fact]
        public void ExportToText_FormatsCorrectly()
        {
            var service = new TextService();
            var entries = new List<Models.Gxt2EntryDto>
            {
                new Models.Gxt2EntryDto(0x1234ABCD, "0x1234ABCD", "Hello Los Santos", null),
                new Models.Gxt2EntryDto(0xCAFEBABE, "0xCAFEBABE", "Welcome to Blaine County", null)
            };

            string text = service.ExportToText(entries);
            Assert.Contains("0x1234ABCD = Hello Los Santos", text);
            Assert.Contains("0xCAFEBABE = Welcome to Blaine County", text);
        }

        [Fact]
        public void SearchRpfGxt2_FindsEntriesInRpfArchive()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_GxtRpfTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string rpfPath = Path.Combine(tempDir, "text_archive.rpf");

            try
            {
                var textService = new TextService();
                byte[] gxtData = textService.BuildGxt2("0x99999999 = Secret Agent Car\n0x88888888 = Weapon Silencer");

                var rpf = CodeWalker.GameFiles.RpfFile.CreateNew(tempDir, "text_archive.rpf", CodeWalker.GameFiles.RpfEncryption.OPEN);
                var textDir = CodeWalker.GameFiles.RpfFile.CreateDirectory(rpf.Root, "text");
                CodeWalker.GameFiles.RpfFile.CreateFile(textDir, "global.gxt2", gxtData, true);

                var rpfService = new RpfService();
                rpfService.OpenRpf(rpfPath);

                var results = textService.SearchRpfGxt2(rpfService, rpfPath, "Secret");
                Assert.Single(results);
                Assert.Equal("Secret Agent Car", results[0].Text);
                Assert.Equal("0x99999999", results[0].HexHash);
                Assert.Contains("global.gxt2", results[0].EntryPath);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void Gxt2_UnicodeAccents_ColorTokens_And_Deduplication()
        {
            var service = new TextService();
            string source = @"
                0x11223344 = ~r~Alerte rouge : ~g~Véhicule blindé à 100€ — Événement débloqué !
                0x11223344 = ~y~Mise à jour : Prix réduit à 50€ ~w~avec succès !
                0xAABB0011 = ~HUD_COLOUR_RED~MISSION ACTIVE : Neutraliser la cible
            ";

            byte[] bytes = service.BuildGxt2(source, "french.gxt2");
            Assert.NotNull(bytes);

            var table = service.ParseGxt2(bytes, "french.gxt2");
            Assert.NotNull(table);
            // Deduplication must reduce 3 raw lines (with 1 duplicate) to 2 unique entries
            Assert.Equal(2u, table.EntryCount);

            // Verify the duplicate was overwritten by the second value
            var entry1 = table.Entries.FirstOrDefault(e => e.HexHash == "0x11223344");
            Assert.NotNull(entry1);
            Assert.Contains("Mise à jour", entry1.Text);
            Assert.Contains("50€", entry1.Text);
            Assert.Contains("succès", entry1.Text);

            var entry2 = table.Entries.FirstOrDefault(e => e.HexHash == "0xAABB0011");
            Assert.NotNull(entry2);
            Assert.Contains("~HUD_COLOUR_RED~", entry2.Text);
            Assert.Contains("Neutraliser", entry2.Text);
        }

        [Fact]
        public void RpfCache_ImmediateRelativeHit_And_NoFalseMatches()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_CacheAccuracy_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string rpfPath = Path.Combine(tempDir, "accuracy.rpf");

            try
            {
                var rpf = CodeWalker.GameFiles.RpfFile.CreateNew(tempDir, "accuracy.rpf", CodeWalker.GameFiles.RpfEncryption.OPEN);
                var dirA = CodeWalker.GameFiles.RpfFile.CreateDirectory(rpf.Root, "dirA");
                var dirB = CodeWalker.GameFiles.RpfFile.CreateDirectory(rpf.Root, "dirB");

                CodeWalker.GameFiles.RpfFile.CreateFile(dirA, "data.xml", Encoding.UTF8.GetBytes("<data>A</data>"), true);
                CodeWalker.GameFiles.RpfFile.CreateFile(dirB, "data.xml", Encoding.UTF8.GetBytes("<data>B</data>"), true);

                var service = new RpfService();
                service.OpenRpf(rpfPath);

                // 1. Initial access using relative path MUST be a cache hit directly from index
                byte[] dataA = service.ExtractFile(rpfPath, "dirA\\data.xml");
                Assert.Equal("<data>A</data>", Encoding.UTF8.GetString(dataA));

                var stats = service.GetCacheStats();
                Assert.Equal(1, stats.CacheHits);
                Assert.Equal(0, stats.CacheMisses);

                // 2. Relative path for dirB
                byte[] dataB = service.ExtractFile(rpfPath, "dirB\\data.xml");
                Assert.Equal("<data>B</data>", Encoding.UTF8.GetString(dataB));
                Assert.Equal(2, service.GetCacheStats().CacheHits);

                // 3. Requesting non-existent folder with existing filename MUST NOT return a false match
                Assert.Throws<FileNotFoundException>(() => service.ExtractFile(rpfPath, "missingDir\\data.xml"));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void ExtractZipStream_And_BatchDisambiguation_WorkCleanly()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_StreamBatch_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string rpfPath = Path.Combine(tempDir, "stream_test.rpf");

            try
            {
                var rpf = CodeWalker.GameFiles.RpfFile.CreateNew(tempDir, "stream_test.rpf", CodeWalker.GameFiles.RpfEncryption.OPEN);
                var dir1 = CodeWalker.GameFiles.RpfFile.CreateDirectory(rpf.Root, "folder1");
                var dir2 = CodeWalker.GameFiles.RpfFile.CreateDirectory(rpf.Root, "folder2");

                CodeWalker.GameFiles.RpfFile.CreateFile(dir1, "config.meta", Encoding.UTF8.GetBytes("config1"), true);
                CodeWalker.GameFiles.RpfFile.CreateFile(dir2, "config.meta", Encoding.UTF8.GetBytes("config2"), true);

                var service = new RpfService();
                service.OpenRpf(rpfPath);

                // 1. ExtractFolderToZipStream returns a readable zip stream
                using (var zipStream = service.ExtractFolderToZipStream(rpfPath, "folder1"))
                {
                    Assert.NotNull(zipStream);
                    Assert.True(zipStream.CanRead);
                    Assert.True(zipStream.Length > 0);

                    using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
                    Assert.Single(archive.Entries);
                    Assert.Equal("config.meta", archive.Entries[0].Name);
                }

                // 2. ExtractBatchToDisk with duplicate filenames in different folders writes both without overwriting
                string batchOut = Path.Combine(tempDir, "batch_out");
                var result = service.ExtractBatchToDisk(rpfPath, new List<string> { "folder1\\config.meta", "folder2\\config.meta" }, batchOut);
                Assert.True(result.Success);
                Assert.Equal(2, result.ExtractedCount);

                var files = Directory.GetFiles(batchOut);
                Assert.Equal(2, files.Length);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void ParseGxt2FromRequest_WithBase64AndFilePath_ParsesAccurately()
        {
            var service = new TextService();
            string source = "0x12345678 = Base64 GXT2 Entry\n0x87654321 = Second Entry";
            byte[] binary = service.BuildGxt2(source, "unit_test.gxt2");
            string base64 = Convert.ToBase64String(binary);

            // 1. Test Base64 parsing (with data URI prefix handling)
            var reqBase64 = new Models.ParseGxt2Request(null, $"data:application/octet-stream;base64,{base64}", "custom.gxt2");
            var tableFromB64 = service.ParseGxt2FromRequest(reqBase64);
            Assert.NotNull(tableFromB64);
            Assert.Equal("custom.gxt2", tableFromB64.FileName);
            Assert.Equal(2u, tableFromB64.EntryCount);
            Assert.Contains(tableFromB64.Entries, e => e.HexHash == "0x12345678" && e.Text == "Base64 GXT2 Entry");

            // 2. Test FilePath parsing
            string tempFile = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid().ToString("N") + ".gxt2");
            try
            {
                File.WriteAllBytes(tempFile, binary);
                var reqFile = new Models.ParseGxt2Request(tempFile, null, null);
                var tableFromFile = service.ParseGxt2FromRequest(reqFile);
                Assert.NotNull(tableFromFile);
                Assert.Equal(Path.GetFileName(tempFile), tableFromFile.FileName);
                Assert.Equal(2u, tableFromFile.EntryCount);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }

            // 3. Test Invalid input
            Assert.Throws<ArgumentException>(() => service.ParseGxt2FromRequest(new Models.ParseGxt2Request(null, null, null)));
        }

        [Fact]
        public void SearchGlobalStrings_WithCryptoDictionary_ReturnsMatchesAndResolvesKeys()
        {
            var textService = new TextService();
            var cryptoService = new CryptoService();
            cryptoService.AddDictionaryEntry("special_carbine_mk2");

            uint carbineHash = CodeWalker.GameFiles.JenkHash.GenHash("special_carbine_mk2");

            // Direct hex search: verify text and resolvedKey are not empty
            var hexResults = textService.SearchGlobalStrings($"0x{carbineHash:X8}", cryptoService, 10);
            Assert.NotEmpty(hexResults);
            var directMatch = hexResults.First(r => r.Hash == carbineHash);
            Assert.Equal("special_carbine_mk2", directMatch.Text);
            Assert.Equal("special_carbine_mk2", directMatch.ResolvedKey);

            // Search by exact label name
            var labelResults = textService.SearchGlobalStrings("special_carbine_mk2", cryptoService, 10);
            Assert.NotEmpty(labelResults);
            Assert.Contains(labelResults, r => r.Hash == carbineHash && r.ResolvedKey == "special_carbine_mk2");

            // Substring search
            var subResults = textService.SearchGlobalStrings("special_carbine", cryptoService, 10);
            Assert.NotEmpty(subResults);
            Assert.Contains(subResults, r => r.Text.Contains("special_carbine_mk2", StringComparison.OrdinalIgnoreCase) ||
                                             (r.ResolvedKey != null && r.ResolvedKey.Contains("special_carbine_mk2", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void BuildGxt2_WithLabelNamesAndHexHashes_ComputesJenkinsHashAccurately()
        {
            var textService = new TextService();
            string input = "FEEDBACK = Feedback text\nTURISMO_R = Grotti Turismo\n0x867B4512 = Raw Hex Hash Entry";
            byte[] binary = textService.BuildGxt2(input, "labels.gxt2");

            Assert.NotNull(binary);
            Assert.True(binary.Length > 16);

            var parsed = textService.ParseGxt2(binary, "labels.gxt2");
            Assert.Equal(3u, parsed.EntryCount);

            uint expectedFeedbackHash = CodeWalker.GameFiles.JenkHash.GenHash("FEEDBACK");
            uint expectedTurismoHash = CodeWalker.GameFiles.JenkHash.GenHash("TURISMO_R");

            Assert.Contains(parsed.Entries, e => e.Hash == expectedFeedbackHash && e.Text == "Feedback text");
            Assert.Contains(parsed.Entries, e => e.Hash == expectedTurismoHash && e.Text == "Grotti Turismo");
            Assert.Contains(parsed.Entries, e => e.Hash == 0x867B4512 && e.Text == "Raw Hex Hash Entry");

            // Entries must be sorted in ascending order of hash
            for (int i = 1; i < parsed.Entries.Count; i++)
            {
                Assert.True(parsed.Entries[i].Hash >= parsed.Entries[i - 1].Hash);
            }
        }

        [Fact]
        public void ExportToText_WithResolvedKeys_IncludesLabelNamesInOutput()
        {
            var textService = new TextService();
            var entries = new List<Gxt2EntryDto>
            {
                new Gxt2EntryDto(0x1234, "0x00001234", "Value with label", "VEH_LABEL"),
                new Gxt2EntryDto(0x5678, "0x00005678", "Value without label", null)
            };

            string exported = textService.ExportToText(entries);
            Assert.Contains("VEH_LABEL = Value with label", exported);
            Assert.Contains("0x00005678 = Value without label", exported);
        }

        [Fact]
        public void ExtractFolderToZipStream_WithEmptyOrNullPath_ExtractsRootArchiveAccurately()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_ZipExtract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string rpfPath = Path.Combine(tempDir, "root_test.rpf");

            try
            {
                var rpf = CodeWalker.GameFiles.RpfFile.CreateNew(tempDir, "root_test.rpf", CodeWalker.GameFiles.RpfEncryption.OPEN);
                CodeWalker.GameFiles.RpfFile.CreateFile(rpf.Root, "sample1.txt", Encoding.UTF8.GetBytes("sample 1 content"), true);
                CodeWalker.GameFiles.RpfFile.CreateFile(rpf.Root, "sample2.txt", Encoding.UTF8.GetBytes("sample 2 content"), true);

                var rpfService = new RpfService();
                rpfService.OpenRpf(rpfPath);

                // Null or empty folderPath should extract the root directory without error
                using var zipStream = rpfService.ExtractFolderToZipStream(rpfPath, "");
                Assert.NotNull(zipStream);
                Assert.True(zipStream.Length > 0);

                using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);
                Assert.Equal(2, archive.Entries.Count);
                Assert.Contains(archive.Entries, e => e.Name == "sample1.txt");
                Assert.Contains(archive.Entries, e => e.Name == "sample2.txt");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void SystemService_CacheClear_EvictsLoadedArchives()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SRFile_SysClear_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string rpfPath = Path.Combine(tempDir, "sys_clear.rpf");

            try
            {
                var rpf = CodeWalker.GameFiles.RpfFile.CreateNew(tempDir, "sys_clear.rpf", CodeWalker.GameFiles.RpfEncryption.OPEN);
                CodeWalker.GameFiles.RpfFile.CreateFile(rpf.Root, "test.txt", Encoding.UTF8.GetBytes("clear me"), true);

                var rpfService = new RpfService();
                rpfService.OpenRpf(rpfPath);
                Assert.Equal(1, rpfService.GetCacheStats().LoadedRpfsCount);

                int cleared = rpfService.ClearCache();
                Assert.Equal(1, cleared);
                Assert.Equal(0, rpfService.GetCacheStats().LoadedRpfsCount);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
    }
}

