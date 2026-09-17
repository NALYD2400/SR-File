using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CodeWalker.GameFiles;
using SRFile.Sidecar.Models;

namespace SRFile.Sidecar.Services
{
    public class TextService
    {
        public Gxt2TableDto ParseGxt2(byte[] data, string fileName = "")
        {
            if (data == null || data.Length == 0)
            {
                return new Gxt2TableDto(fileName, 0, new List<Gxt2EntryDto>());
            }

            var gxt2 = new Gxt2File();
            gxt2.Load(data, null!);

            var list = new List<Gxt2EntryDto>();
            if (gxt2.TextEntries != null)
            {
                foreach (var entry in gxt2.TextEntries)
                {
                    if (entry == null) continue;

                    string hex = $"0x{entry.Hash:X8}";
                    string? resolved = GlobalText.TryGetString(entry.Hash);
                    if (string.IsNullOrEmpty(resolved) || resolved == entry.Hash.ToString())
                    {
                        resolved = null;
                    }

                    list.Add(new Gxt2EntryDto(
                        Hash: entry.Hash,
                        HexHash: hex,
                        Text: entry.Text ?? string.Empty,
                        ResolvedKey: resolved
                    ));
                }
            }

            return new Gxt2TableDto(
                FileName: fileName,
                EntryCount: (uint)list.Count,
                Entries: list
            );
        }

        public Gxt2TableDto GetGxt2FromRpf(RpfService rpfService, string rpfPath, string entryPath)
        {
            byte[] data = rpfService.ExtractFile(rpfPath, entryPath);
            return ParseGxt2(data, Path.GetFileName(entryPath));
        }

        public List<Gxt2EntryDto> SearchGxt2(Gxt2TableDto table, string query, int maxResults = 100)
        {
            if (table == null || table.Entries == null || string.IsNullOrWhiteSpace(query))
            {
                return new List<Gxt2EntryDto>();
            }

            string q = query.Trim();
            bool isHex = q.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            uint queryHash = 0;
            bool hasUintHash = false;

            if (isHex && uint.TryParse(q.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint parsedHex))
            {
                queryHash = parsedHex;
                hasUintHash = true;
            }
            else if (uint.TryParse(q, out uint parsedUint))
            {
                queryHash = parsedUint;
                hasUintHash = true;
            }

            var results = new List<Gxt2EntryDto>();
            foreach (var item in table.Entries)
            {
                bool match = false;
                if (hasUintHash && item.Hash == queryHash)
                {
                    match = true;
                }
                else if (item.Text != null && item.Text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    match = true;
                }
                else if (item.HexHash != null && item.HexHash.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    match = true;
                }
                else if (item.ResolvedKey != null && item.ResolvedKey.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    match = true;
                }

                if (match)
                {
                    results.Add(item);
                    if (results.Count >= maxResults) break;
                }
            }

            return results;
        }

        public List<Gxt2SearchResultDto> SearchRpfGxt2(RpfService rpfService, string rpfPath, string query, int maxResults = 250)
        {
            var results = new List<Gxt2SearchResultDto>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            var gxtFiles = rpfService.FindGxt2Files(rpfPath);
            foreach (var gxtPath in gxtFiles)
            {
                try
                {
                    var table = GetGxt2FromRpf(rpfService, rpfPath, gxtPath);
                    var matches = SearchGxt2(table, query, maxResults - results.Count);
                    foreach (var m in matches)
                    {
                        results.Add(new Gxt2SearchResultDto(
                            RpfPath: rpfPath,
                            EntryPath: gxtPath,
                            Hash: m.Hash,
                            HexHash: m.HexHash,
                            Text: m.Text,
                            ResolvedKey: m.ResolvedKey
                        ));

                        if (results.Count >= maxResults) return results;
                    }
                }
                catch
                {
                    // Continue scanning other GXT2 files
                }
            }

            return results;
        }

        public string ExportToText(List<Gxt2EntryDto> entries)
        {
            var sb = new StringBuilder();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    sb.Append(entry.HexHash);
                    sb.Append(" = ");
                    sb.AppendLine(entry.Text);
                }
            }
            return sb.ToString();
        }

        public byte[] BuildGxt2(string textContent, string entryName = "")
        {
            var lines = (textContent ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var entries = new List<Gxt2Entry>();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                    continue;

                int eqIdx = line.IndexOf('=');
                if (eqIdx <= 0)
                {
                    eqIdx = line.IndexOf(':');
                }

                if (eqIdx <= 0) continue;

                string keyPart = line.Substring(0, eqIdx).Trim();
                string valPart = line.Substring(eqIdx + 1).Trim();

                uint hash = 0;
                if (keyPart.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                    uint.TryParse(keyPart.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint parsedHex))
                {
                    hash = parsedHex;
                }
                else if (keyPart.Length == 8 && uint.TryParse(keyPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint parsedHex8))
                {
                    hash = parsedHex8;
                }
                else if (uint.TryParse(keyPart, out uint parsedUint))
                {
                    hash = parsedUint;
                }
                else
                {
                    // Hash string using JenkHash (Jenkins 32-bit one-at-a-time hash)
                    hash = JenkHash.GenHash(keyPart, JenkHashInputEncoding.UTF8);
                }

                entries.Add(new Gxt2Entry
                {
                    Hash = hash,
                    Text = valPart
                });
            }

            entries.Sort((a, b) => a.Hash.CompareTo(b.Hash));

            var gxt = new Gxt2File
            {
                Name = entryName,
                TextEntries = entries.ToArray(),
                EntryCount = (uint)entries.Count
            };

            return gxt.Save();
        }
    }
}
