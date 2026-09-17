using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using SRFile.Sidecar.Models;

namespace SRFile.Sidecar.Services
{
    public class ProjectEditorService
    {
        private ProjectSummaryDto? _currentProject;
        private readonly List<YmapEntityDto> _loadedEntities = new();
        private readonly List<ArchetypeDto> _loadedArchetypes = new();

        public ProjectEditorService()
        {
            // Initialize with default template project
            _currentProject = new ProjectSummaryDto(
                Name: "Nouveau Projet SR File",
                Version: 1,
                Filepath: null,
                YmapFiles: new List<string> { "custom_map.ymap" },
                YtypFiles: new List<string> { "custom_types.ytyp" },
                YbnFiles: new List<string>()
            );

            // Populate some sample entities for instant editing
            _loadedEntities.Add(new YmapEntityDto(
                Name: "prop_barrier_work05_01",
                ArchetypeName: "prop_barrier_work05",
                Position: new Vector3Dto(-1034.5f, -2732.1f, 13.8f),
                Rotation: new Vector4Dto(0f, 0f, 0.7071f, 0.7071f),
                EulerRotation: new Vector3Dto(0f, 0f, 90f),
                LodDist: 150f,
                ChildLodDist: 0f,
                Flags: 32,
                Guid: 1001
            ));

            _loadedEntities.Add(new YmapEntityDto(
                Name: "vw_prop_casino_door_01",
                ArchetypeName: "vw_prop_vw_casino_door",
                Position: new Vector3Dto(925.3f, 47.1f, 81.2f),
                Rotation: new Vector4Dto(0f, 0f, 0f, 1f),
                EulerRotation: new Vector3Dto(0f, 0f, 0f),
                LodDist: 250f,
                ChildLodDist: 50f,
                Flags: 1572864,
                Guid: 1002
            ));

            _loadedArchetypes.Add(new ArchetypeDto(
                Name: "prop_barrier_work05",
                TextureDictionary: "props_barriers",
                PhysicsDictionary: "props_barriers",
                LodDist: 150f,
                HdTextureDist: 50f,
                BbMin: new Vector3Dto(-1.2f, -0.2f, -0.5f),
                BbMax: new Vector3Dto(1.2f, 0.2f, 0.5f),
                BsCentre: new Vector3Dto(0f, 0f, 0f),
                BsRadius: 1.5f
            ));
        }

        public ProjectSummaryDto GetCurrentProject()
        {
            return _currentProject ?? new ProjectSummaryDto(
                Name: "Projet Vide",
                Version: 1,
                Filepath: null,
                YmapFiles: new List<string>(),
                YtypFiles: new List<string>(),
                YbnFiles: new List<string>()
            );
        }

        public ProjectSummaryDto CreateProject(string name)
        {
            _currentProject = new ProjectSummaryDto(
                Name: name,
                Version: 1,
                Filepath: null,
                YmapFiles: new List<string>(),
                YtypFiles: new List<string>(),
                YbnFiles: new List<string>()
            );
            _loadedEntities.Clear();
            _loadedArchetypes.Clear();
            return _currentProject;
        }

        public ProjectSummaryDto OpenProject(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Projet introuvable: {filePath}");

            var doc = new XmlDocument();
            doc.Load(filePath);

            var root = doc.DocumentElement;
            if (root == null || !string.Equals(root.Name, "CodeWalkerProject", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Le fichier sélectionné n'est pas un projet CodeWalker/SRFile (.cwproj) valide.");

            string name = root.SelectSingleNode("Name")?.InnerText ?? Path.GetFileNameWithoutExtension(filePath);
            int version = 1;
            if (int.TryParse(root.SelectSingleNode("Version")?.Attributes?["value"]?.Value, out int v))
                version = v;

            var ymaps = new List<string>();
            var ymapNodes = root.SelectNodes("YmapFilenames/Item");
            if (ymapNodes != null)
            {
                foreach (XmlNode item in ymapNodes)
                {
                    if (!string.IsNullOrWhiteSpace(item.InnerText))
                        ymaps.Add(item.InnerText.Trim());
                }
            }

            var ytyps = new List<string>();
            var ytypNodes = root.SelectNodes("YtypFilenames/Item");
            if (ytypNodes != null)
            {
                foreach (XmlNode item in ytypNodes)
                {
                    if (!string.IsNullOrWhiteSpace(item.InnerText))
                        ytyps.Add(item.InnerText.Trim());
                }
            }

            var ybns = new List<string>();
            var ybnNodes = root.SelectNodes("YbnFilenames/Item");
            if (ybnNodes != null)
            {
                foreach (XmlNode item in ybnNodes)
                {
                    if (!string.IsNullOrWhiteSpace(item.InnerText))
                        ybns.Add(item.InnerText.Trim());
                }
            }

            _currentProject = new ProjectSummaryDto(
                Name: name,
                Version: version,
                Filepath: filePath,
                YmapFiles: ymaps,
                YtypFiles: ytyps,
                YbnFiles: ybns
            );

            return _currentProject;
        }

        public bool SaveProject(SaveProjectRequest req)
        {
            var doc = new XmlDocument();
            var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(decl);

            var root = doc.CreateElement("CodeWalkerProject");
            doc.AppendChild(root);

            var nameElem = doc.CreateElement("Name");
            nameElem.InnerText = req.Name;
            root.AppendChild(nameElem);

            var verElem = doc.CreateElement("Version");
            verElem.SetAttribute("value", req.Version.ToString());
            root.AppendChild(verElem);

            var ymapsElem = doc.CreateElement("YmapFilenames");
            root.AppendChild(ymapsElem);
            foreach (var ymap in req.YmapFiles)
            {
                var item = doc.CreateElement("Item");
                item.InnerText = ymap;
                ymapsElem.AppendChild(item);
            }

            var ytypsElem = doc.CreateElement("YtypFilenames");
            root.AppendChild(ytypsElem);
            foreach (var ytyp in req.YtypFiles)
            {
                var item = doc.CreateElement("Item");
                item.InnerText = ytyp;
                ytypsElem.AppendChild(item);
            }

            var ybnsElem = doc.CreateElement("YbnFilenames");
            root.AppendChild(ybnsElem);
            foreach (var ybn in req.YbnFiles)
            {
                var item = doc.CreateElement("Item");
                item.InnerText = ybn;
                ybnsElem.AppendChild(item);
            }

            string? dir = Path.GetDirectoryName(req.FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            doc.Save(req.FilePath);

            _currentProject = new ProjectSummaryDto(
                Name: req.Name,
                Version: req.Version,
                Filepath: req.FilePath,
                YmapFiles: req.YmapFiles,
                YtypFiles: req.YtypFiles,
                YbnFiles: req.YbnFiles
            );

            return true;
        }

        public List<YmapEntityDto> GetEntities()
        {
            return _loadedEntities;
        }

        public List<ArchetypeDto> GetArchetypes()
        {
            return _loadedArchetypes;
        }

        public void UpdateEntity(int index, YmapEntityDto entity)
        {
            if (index >= 0 && index < _loadedEntities.Count)
            {
                _loadedEntities[index] = entity;
            }
            else
            {
                _loadedEntities.Add(entity);
            }
        }

        public void DeleteEntity(int index)
        {
            if (index >= 0 && index < _loadedEntities.Count)
            {
                _loadedEntities.RemoveAt(index);
            }
        }

        public string ExportYmapXml(string ymapName, List<YmapEntityDto> entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<CMapData>");
            sb.AppendLine($"  <name>{ymapName}</name>");
            sb.AppendLine("  <flags value=\"0\" />");
            sb.AppendLine("  <contentFlags value=\"1\" />");
            sb.AppendLine("  <streamingExtentsMin x=\"-4000.0\" y=\"-4000.0\" z=\"-1000.0\" />");
            sb.AppendLine("  <streamingExtentsMax x=\"4000.0\" y=\"4000.0\" z=\"1000.0\" />");
            sb.AppendLine("  <entitiesExtentsMin x=\"-4000.0\" y=\"-4000.0\" z=\"-1000.0\" />");
            sb.AppendLine("  <entitiesExtentsMax x=\"4000.0\" y=\"4000.0\" z=\"1000.0\" />");
            sb.AppendLine("  <entities>");

            foreach (var e in entities)
            {
                // Ensure quaternion matches euler angles if available
                var quat = EulerToQuaternion(e.EulerRotation.X, e.EulerRotation.Y, e.EulerRotation.Z);
                sb.AppendLine("    <Item type=\"CEntityDef\">");
                sb.AppendLine($"      <archetypeName>{e.ArchetypeName}</archetypeName>");
                sb.AppendLine($"      <flags value=\"{e.Flags}\" />");
                sb.AppendLine($"      <guid value=\"{e.Guid}\" />");
                sb.AppendLine($"      <position x=\"{e.Position.X.ToString("F6", CultureInfo.InvariantCulture)}\" y=\"{e.Position.Y.ToString("F6", CultureInfo.InvariantCulture)}\" z=\"{e.Position.Z.ToString("F6", CultureInfo.InvariantCulture)}\" />");
                sb.AppendLine($"      <rotation x=\"{quat.X.ToString("F6", CultureInfo.InvariantCulture)}\" y=\"{quat.Y.ToString("F6", CultureInfo.InvariantCulture)}\" z=\"{quat.Z.ToString("F6", CultureInfo.InvariantCulture)}\" w=\"{quat.W.ToString("F6", CultureInfo.InvariantCulture)}\" />");
                sb.AppendLine("      <scaleXY value=\"1.000000\" />");
                sb.AppendLine("      <scaleZ value=\"1.000000\" />");
                sb.AppendLine("      <parentIndex value=\"-1\" />");
                sb.AppendLine($"      <lodDist value=\"{e.LodDist.ToString("F6", CultureInfo.InvariantCulture)}\" />");
                sb.AppendLine($"      <childLodDist value=\"{e.ChildLodDist.ToString("F6", CultureInfo.InvariantCulture)}\" />");
                sb.AppendLine("      <lodLevel>LODTYPES_DEPTH_HD</lodLevel>");
                sb.AppendLine("      <numChildren value=\"0\" />");
                sb.AppendLine("      <priorityLevel>PRI_DEFAULT</priorityLevel>");
                sb.AppendLine("    </Item>");
            }

            sb.AppendLine("  </entities>");
            sb.AppendLine("</CMapData>");

            return sb.ToString();
        }

        public List<YmapEntityDto> ImportYmapXml(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                throw new ArgumentException("Le contenu XML ne peut pas être vide.", nameof(xmlContent));

            var results = new List<YmapEntityDto>();
            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            var entityNodes = doc.SelectNodes("//entities/Item") ?? doc.SelectNodes("//Item[@type='CEntityDef']");
            if (entityNodes != null)
            {
                int index = 1;
                foreach (XmlNode node in entityNodes)
                {
                    string archName = node.SelectSingleNode("archetypeName")?.InnerText?.Trim() ?? "unknown_archetype";
                    uint flags = ReadUInt(node, "flags", 0);
                    uint guid = ReadUInt(node, "guid", (uint)index);

                    var posNode = node.SelectSingleNode("position");
                    float posX = ReadFloat(posNode, "x", 0f);
                    float posY = ReadFloat(posNode, "y", 0f);
                    float posZ = ReadFloat(posNode, "z", 0f);

                    var rotNode = node.SelectSingleNode("rotation");
                    float rotX = ReadFloat(rotNode, "x", 0f);
                    float rotY = ReadFloat(rotNode, "y", 0f);
                    float rotZ = ReadFloat(rotNode, "z", 0f);
                    float rotW = ReadFloat(rotNode, "w", 1f);

                    float lodDist = ReadFloat(node, "lodDist", 100f);
                    float childLodDist = ReadFloat(node, "childLodDist", 0f);

                    var euler = QuaternionToEuler(rotX, rotY, rotZ, rotW);

                    results.Add(new YmapEntityDto(
                        Name: $"{archName}_{index++}",
                        ArchetypeName: archName,
                        Position: new Vector3Dto(posX, posY, posZ),
                        Rotation: new Vector4Dto(rotX, rotY, rotZ, rotW),
                        EulerRotation: euler,
                        LodDist: lodDist,
                        ChildLodDist: childLodDist,
                        Flags: flags,
                        Guid: guid
                    ));
                }
            }

            _loadedEntities.Clear();
            _loadedEntities.AddRange(results);

            return results;
        }

        private static float ReadFloat(XmlNode? parent, string childOrAttrName, float defaultVal = 0f)
        {
            if (parent == null) return defaultVal;
            var attr = parent.Attributes?[childOrAttrName]?.Value;
            if (!string.IsNullOrEmpty(attr) && float.TryParse(attr, NumberStyles.Float, CultureInfo.InvariantCulture, out float vAttr))
                return vAttr;

            var child = parent.SelectSingleNode(childOrAttrName);
            if (child != null)
            {
                var childAttr = child.Attributes?["value"]?.Value;
                if (!string.IsNullOrEmpty(childAttr) && float.TryParse(childAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out float vChildAttr))
                    return vChildAttr;
                if (float.TryParse(child.InnerText?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float vInnerText))
                    return vInnerText;
            }

            return defaultVal;
        }

        private static uint ReadUInt(XmlNode? parent, string childOrAttrName, uint defaultVal = 0)
        {
            if (parent == null) return defaultVal;
            var attr = parent.Attributes?[childOrAttrName]?.Value;
            if (!string.IsNullOrEmpty(attr) && uint.TryParse(attr, out uint vAttr))
                return vAttr;

            var child = parent.SelectSingleNode(childOrAttrName);
            if (child != null)
            {
                var childAttr = child.Attributes?["value"]?.Value;
                if (!string.IsNullOrEmpty(childAttr) && uint.TryParse(childAttr, out uint vChildAttr))
                    return vChildAttr;
                if (uint.TryParse(child.InnerText?.Trim(), out uint vInnerText))
                    return vInnerText;
            }

            return defaultVal;
        }

        public static Vector3Dto QuaternionToEuler(float x, float y, float z, float w)
        {
            // Pitch (X-axis rotation)
            double sinr_cosp = 2 * (w * x + y * z);
            double cosr_cosp = 1 - 2 * (x * x + y * y);
            double pitch = Math.Atan2(sinr_cosp, cosr_cosp) * (180.0 / Math.PI);

            // Roll (Y-axis rotation)
            double sinp = 2 * (w * y - z * x);
            double roll;
            if (Math.Abs(sinp) >= 1)
                roll = Math.CopySign(90.0, sinp);
            else
                roll = Math.Asin(sinp) * (180.0 / Math.PI);

            // Yaw (Z-axis rotation)
            double siny_cosp = 2 * (w * z + x * y);
            double cosy_cosp = 1 - 2 * (y * y + z * z);
            double yaw = Math.Atan2(siny_cosp, cosy_cosp) * (180.0 / Math.PI);

            return new Vector3Dto((float)pitch, (float)roll, (float)yaw);
        }

        public static Vector4Dto EulerToQuaternion(float pitchDeg, float rollDeg, float yawDeg)
        {
            double p = pitchDeg * (Math.PI / 180.0) * 0.5;
            double r = rollDeg * (Math.PI / 180.0) * 0.5;
            double y = yawDeg * (Math.PI / 180.0) * 0.5;

            double cp = Math.Cos(p);
            double sp = Math.Sin(p);
            double cr = Math.Cos(r);
            double sr = Math.Sin(r);
            double cy = Math.Cos(y);
            double sy = Math.Sin(y);

            double w = cp * cr * cy + sp * sr * sy;
            double x = sp * cr * cy - cp * sr * sy;
            double yVal = cp * sr * cy + sp * cr * sy;
            double z = cp * cr * sy - sp * sr * cy;

            return new Vector4Dto((float)x, (float)yVal, (float)z, (float)w);
        }
    }
}
