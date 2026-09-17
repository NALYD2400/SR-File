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
        public Gxt2TableDto ParseGxt2(byte[] data, string fileName = "", CryptoService? cryptoService = null)
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
                        resolved = JenkIndex.TryGetString(entry.Hash);
                    }
                    if (string.IsNullOrEmpty(resolved) || resolved == entry.Hash.ToString())
                    {
                        resolved = cryptoService?.LookupHash(entry.Hash);
                    }
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

        public Gxt2TableDto ParseGxt2FromRequest(ParseGxt2Request req, CryptoService? cryptoService = null)
        {
            if (req == null) return new Gxt2TableDto("empty.gxt2", 0, new List<Gxt2EntryDto>());

            if (!string.IsNullOrWhiteSpace(req.FilePath) && File.Exists(req.FilePath))
            {
                byte[] fileBytes = File.ReadAllBytes(req.FilePath);
                string name = !string.IsNullOrWhiteSpace(req.FileName) ? req.FileName : Path.GetFileName(req.FilePath);
                return ParseGxt2(fileBytes, name, cryptoService);
            }

            if (!string.IsNullOrWhiteSpace(req.Base64Data))
            {
                string rawBase64 = req.Base64Data.Trim();
                int commaIdx = rawBase64.IndexOf(',');
                if (commaIdx >= 0 && rawBase64.Substring(0, commaIdx).Contains("base64"))
                {
                    rawBase64 = rawBase64.Substring(commaIdx + 1);
                }
                byte[] bytes = Convert.FromBase64String(rawBase64);
                string name = !string.IsNullOrWhiteSpace(req.FileName) ? req.FileName : "uploaded.gxt2";
                return ParseGxt2(bytes, name, cryptoService);
            }

            throw new ArgumentException("Aucun chemin de fichier valide ou donnée base64 fournie.");
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

        public List<Gxt2SearchResultDto> SearchGlobalStrings(string query, CryptoService? cryptoService = null, int maxResults = 100)
        {
            var results = new List<Gxt2SearchResultDto>();
            if (string.IsNullOrWhiteSpace(query)) return results;

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

            var seenHashes = new HashSet<uint>();

            static string? Clean(string? str, uint hash)
            {
                if (string.IsNullOrEmpty(str) || str == hash.ToString()) return null;
                return str;
            }

            // 1. Direct hash lookup if numeric/hex was parsed
            if (hasUintHash)
            {
                string? directGlobal = Clean(GlobalText.TryGetString(queryHash), queryHash);
                string? directJenk = Clean(JenkIndex.TryGetString(queryHash), queryHash);
                string? cryptoLookup = Clean(cryptoService?.LookupHash(queryHash), queryHash);

                if (directGlobal != null || directJenk != null || cryptoLookup != null || isHex)
                {
                    string foundText = directGlobal ?? directJenk ?? cryptoLookup ?? $"0x{queryHash:X8}";
                    string? resolvedKey = directJenk ?? cryptoLookup;

                    results.Add(new Gxt2SearchResultDto(
                        RpfPath: "global_dictionary",
                        EntryPath: "strings.txt",
                        Hash: queryHash,
                        HexHash: $"0x{queryHash:X8}",
                        Text: foundText,
                        ResolvedKey: resolvedKey
                    ));
                    seenHashes.Add(queryHash);
                }
            }

            // 1b. Direct Jenkins hash lookup for the query label itself
            uint stringJenkHash = JenkHash.GenHash(q, JenkHashInputEncoding.UTF8);
            if (!seenHashes.Contains(stringJenkHash))
            {
                string? sGlobal = Clean(GlobalText.TryGetString(stringJenkHash), stringJenkHash);
                string? sJenk = Clean(JenkIndex.TryGetString(stringJenkHash), stringJenkHash);
                string? sCrypto = Clean(cryptoService?.LookupHash(stringJenkHash), stringJenkHash);

                if (sGlobal != null || sJenk != null || sCrypto != null)
                {
                    results.Add(new Gxt2SearchResultDto(
                        RpfPath: "global_dictionary",
                        EntryPath: "JenkHash",
                        Hash: stringJenkHash,
                        HexHash: $"0x{stringJenkHash:X8}",
                        Text: sGlobal ?? sCrypto ?? sJenk ?? q,
                        ResolvedKey: sJenk ?? sCrypto ?? q
                    ));
                    seenHashes.Add(stringJenkHash);
                }
            }

            // 2. GlobalText.Index
            try
            {
                var snapshot = GlobalText.Index.ToArray();
                foreach (var kvp in snapshot)
                {
                    if (results.Count >= maxResults) break;
                    if (seenHashes.Contains(kvp.Key)) continue;

                    bool match = false;
                    if (kvp.Value != null && kvp.Value.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        match = true;
                    }
                    else if ($"0x{kvp.Key:X8}".IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        match = true;
                    }

                    if (match)
                    {
                        seenHashes.Add(kvp.Key);
                        string? resolvedKey = Clean(JenkIndex.TryGetString(kvp.Key), kvp.Key) ?? Clean(cryptoService?.LookupHash(kvp.Key), kvp.Key);
                        results.Add(new Gxt2SearchResultDto(
                            RpfPath: "global_text",
                            EntryPath: "GlobalText.Index",
                            Hash: kvp.Key,
                            HexHash: $"0x{kvp.Key:X8}",
                            Text: kvp.Value ?? string.Empty,
                            ResolvedKey: resolvedKey
                        ));
                    }
                }
            }
            catch { }

            // 3. Search CryptoService dictionary
            if (cryptoService != null && results.Count < maxResults)
            {
                var dictMatches = cryptoService.SearchDictionary(q, maxResults - results.Count);
                foreach (var item in dictMatches)
                {
                    if (results.Count >= maxResults) break;
                    if (seenHashes.Contains(item.HashUint)) continue;

                    seenHashes.Add(item.HashUint);
                    string? globalVal = Clean(GlobalText.TryGetString(item.HashUint), item.HashUint);
                    results.Add(new Gxt2SearchResultDto(
                        RpfPath: "dictionary",
                        EntryPath: "strings.txt",
                        Hash: item.HashUint,
                        HexHash: item.HashHex,
                        Text: !string.IsNullOrEmpty(globalVal) ? globalVal : item.Text,
                        ResolvedKey: item.Text
                    ));
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
                    string labelOrHex = !string.IsNullOrEmpty(entry.ResolvedKey) ? entry.ResolvedKey : entry.HexHash;
                    sb.Append(labelOrHex);
                    sb.Append(" = ");
                    sb.AppendLine(entry.Text);
                }
            }
            return sb.ToString();
        }

        public byte[] BuildGxt2(string textContent, string entryName = "")
        {
            var lines = (textContent ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var entryMap = new Dictionary<uint, Gxt2Entry>();

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
                else if (uint.TryParse(keyPart, out uint parsedUint))
                {
                    hash = parsedUint;
                }
                else
                {
                    // Hash string using JenkHash (Jenkins 32-bit one-at-a-time hash)
                    hash = JenkHash.GenHash(keyPart, JenkHashInputEncoding.UTF8);
                }

                entryMap[hash] = new Gxt2Entry
                {
                    Hash = hash,
                    Text = valPart
                };
            }

            var entries = entryMap.Values.ToList();
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
