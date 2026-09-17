using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CodeWalker.GameFiles;
using SRFile.Sidecar.Models;

namespace SRFile.Sidecar.Services
{
    public class CryptoService
    {
        private readonly ConcurrentDictionary<uint, string> _dictionary = new();
        private readonly ConcurrentDictionary<string, uint> _reverseLookup = new(StringComparer.OrdinalIgnoreCase);

        public CryptoService()
        {
            InitializeDictionary();
        }

        private void InitializeDictionary()
        {
            // Seed with common GTA V modding strings
            var defaultStrings = new[]
            {
                "prop_weed_01", "prop_weed_02", "prop_barrier_work05", "vw_prop_vw_casino_door",
                "hei_prop_heist_weed_block", "prop_container_05a", "prop_dumpster_01a", "prop_gate_airport_01",
                "prop_phonebox_04", "prop_traffic_01a", "prop_streetlight_01", "v_ilev_cd_door",
                "dt1_05_ground_01", "dt1_05_build1", "cs1_10_sea_rocks_01", "hei_ch3_03_tunnel",
                "custom_car", "dlc_addon_vehicle", "mod_engine_block", "veh_wheel_sport_01",
                "weapon_pistol", "weapon_combatpistol", "weapon_appistol", "weapon_pistol50",
                "weapon_smg", "weapon_microsmg", "weapon_assaultrifle", "weapon_carbinerifle",
                "a_m_m_skater_01", "a_m_y_beach_01", "player_zero", "player_one", "player_two"
            };

            foreach (var s in defaultStrings)
            {
                AddDictionaryEntry(s);
            }

            // Attempt to load strings.txt if found
            string[] searchPaths = {
                "strings.txt",
                Path.Combine(AppContext.BaseDirectory, "strings.txt"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "strings.txt"),
                Path.Combine(Environment.CurrentDirectory, "strings.txt")
            };

            foreach (var path in searchPaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        foreach (var line in File.ReadLines(path))
                        {
                            var trimmed = line.Trim();
                            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("//") && !trimmed.StartsWith("#"))
                            {
                                AddDictionaryEntry(trimmed);
                            }
                        }
                        break;
                    }
                }
                catch { }
            }
        }

        public void AddDictionaryEntry(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            uint hash = JenkHash.GenHash(text);
            _dictionary[hash] = text;
            _reverseLookup[text] = hash;
            JenkIndex.Ensure(text);
        }

        public JoaatResponse ComputeJoaat(string text, string? encoding = "utf8")
        {
            var enc = string.Equals(encoding, "ascii", StringComparison.OrdinalIgnoreCase)
                ? JenkHashInputEncoding.ASCII
                : JenkHashInputEncoding.UTF8;

            uint hashUint = JenkHash.GenHash(text, enc);
            int hashInt = (int)hashUint;
            string hex = "0x" + hashUint.ToString("X8");

            // Cache entered text in dictionary
            AddDictionaryEntry(text);

            return new JoaatResponse(
                Text: text,
                HashUint: hashUint,
                HashInt: hashInt,
                HashHex: hex
            );
        }

        public List<JoaatResponse> BatchCompute(List<string> texts, string? encoding = "utf8")
        {
            return texts.Select(t => ComputeJoaat(t, encoding)).ToList();
        }

        public string? LookupHash(uint hash)
        {
            if (_dictionary.TryGetValue(hash, out var text))
                return text;

            var jenkText = JenkIndex.TryGetString(hash);
            return !string.IsNullOrEmpty(jenkText) ? jenkText : null;
        }

        public List<DictionarySearchResponse> SearchDictionary(string query, int limit = 50)
        {
            var results = new List<DictionarySearchResponse>();
            if (string.IsNullOrWhiteSpace(query))
            {
                return _dictionary.Take(limit).Select(kv => new DictionarySearchResponse(
                    HashUint: kv.Key,
                    HashHex: "0x" + kv.Key.ToString("X8"),
                    Text: kv.Value,
                    Score: 1.0
                )).ToList();
            }

            string qLower = query.Trim().ToLowerInvariant();

            // Check if query is hex (0x1234 or 1234ABCD)
            uint parsedHex = 0;
            bool isHex = false;
            if (qLower.StartsWith("0x") && uint.TryParse(qLower.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out parsedHex))
            {
                isHex = true;
            }
            else if (uint.TryParse(qLower, System.Globalization.NumberStyles.HexNumber, null, out parsedHex))
            {
                isHex = true;
            }

            if (isHex)
            {
                var match = LookupHash(parsedHex);
                if (match != null)
                {
                    results.Add(new DictionarySearchResponse(
                        HashUint: parsedHex,
                        HashHex: "0x" + parsedHex.ToString("X8"),
                        Text: match,
                        Score: 100.0
                    ));
                }
            }

            foreach (var kv in _dictionary)
            {
                if (results.Count >= limit) break;
                if (kv.Value.IndexOf(qLower, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    double score = (double)qLower.Length / kv.Value.Length;
                    results.Add(new DictionarySearchResponse(
                        HashUint: kv.Key,
                        HashHex: "0x" + kv.Key.ToString("X8"),
                        Text: kv.Value,
                        Score: score
                    ));
                }
            }

            return results.OrderByDescending(r => r.Score).Take(limit).ToList();
        }

        public CryptoKeysStatus GetKeysStatus()
        {
            bool hasAes = GTA5Keys.PC_AES_KEY != null && GTA5Keys.PC_AES_KEY.Length == 32;
            string? hexKey = hasAes ? BitConverter.ToString(GTA5Keys.PC_AES_KEY!).Replace("-", "").ToUpperInvariant() : null;
            string? b64Key = hasAes ? Convert.ToBase64String(GTA5Keys.PC_AES_KEY!) : null;

            int ngCount = GTA5Keys.PC_NG_KEYS?.Length ?? 0;
            int decTablesCount = GTA5Keys.PC_NG_DECRYPT_TABLES != null ? 17 * 16 : 0;
            bool awcLoaded = GTA5Keys.PC_AWC_KEY != null && GTA5Keys.PC_AWC_KEY.Length > 0;

            return new CryptoKeysStatus(
                AesKeyLoaded: hasAes,
                AesKeyHex: hexKey,
                AesKeyBase64: b64Key,
                NgKeysCount: ngCount,
                DecryptTablesCount: decTablesCount,
                AwcKeyLoaded: awcLoaded
            );
        }

        public bool SetAesKey(string rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey)) return false;
            rawKey = rawKey.Trim();

            byte[]? keyBytes = null;
            if (rawKey.Length == 64)
            {
                // Hex
                keyBytes = new byte[32];
                for (int i = 0; i < 32; i++)
                {
                    keyBytes[i] = Convert.ToByte(rawKey.Substring(i * 2, 2), 16);
                }
            }
            else
            {
                try
                {
                    var bytes = Convert.FromBase64String(rawKey);
                    if (bytes.Length == 32)
                    {
                        keyBytes = bytes;
                    }
                }
                catch { }
            }

            if (keyBytes == null || keyBytes.Length != 32)
                throw new ArgumentException("La clé AES doit comporter 32 octets (64 caractères hexadécimaux ou 44 caractères base64).");

            GTA5Keys.PC_AES_KEY = keyBytes;
            return true;
        }

        public RpfEncryptionInfo InspectRpfEncryption(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Fichier introuvable: {filePath}");

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var br = new BinaryReader(fs);

            if (fs.Length < 16)
            {
                return new RpfEncryptionInfo(
                    FilePath: filePath,
                    Encryption: "INVALID",
                    IsValidHeader: false,
                    Version: 0
                );
            }

            uint magic = br.ReadUInt32();
            uint entryCount = br.ReadUInt32();
            uint namesLength = br.ReadUInt32();
            uint encMagic = br.ReadUInt32();

            bool validMagic = (magic == 0x52504637 || magic == 0x52504638 || magic == 0x37465052 || magic == 0x38465052);
            uint version = (magic == 0x52504638 || magic == 0x38465052) ? 8u : 7u;

            string encName = "UNKNOWN";
            switch (encMagic)
            {
                case 0x4E45504F: // "OPEN"
                    encName = "OPEN (Non chiffré)";
                    break;
                case 0x0FFFFFF9:
                    encName = "AES 256-bit";
                    break;
                case 0x0FFFFFFD:
                    encName = "NG (Next-Gen Table Crypto)";
                    break;
                case 0:
                    encName = "NONE";
                    break;
                default:
                    encName = $"CUSTOM (0x{encMagic:X8})";
                    break;
            }

            return new RpfEncryptionInfo(
                FilePath: filePath,
                Encryption: encName,
                IsValidHeader: validMagic,
                Version: version
            );
        }
    }
}
