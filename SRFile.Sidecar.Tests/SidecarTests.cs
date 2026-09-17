using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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
}
