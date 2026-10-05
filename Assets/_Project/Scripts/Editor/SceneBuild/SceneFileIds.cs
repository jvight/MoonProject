using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MoonProject.Editor.SceneBuild
{
    /// <summary>
    /// Rewrites the local file IDs of a text-serialised scene so they derive from the scene's structure instead of
    /// Unity's random allocation: a rebuilt scene with identical content becomes a byte-identical file.
    /// <para>Each object gets a key from its place in the hierarchy (root order, sibling index, name, component
    /// index; prefab instances by source prefab and their stripped objects by source object), the key is hashed into
    /// a 62-bit ID, every local <c>{fileID: N}</c> reference is remapped, and documents are sorted by ID as Unity
    /// writes them, so opening and re-saving the scene in Unity leaves it unchanged. Scene settings objects and
    /// SceneRoots keep their fixed IDs. External references (with a guid) are never touched.</para>
    /// </summary>
    public static class SceneFileIds
    {
        private const int SceneRootsClass = 1660057539;
        private const long MinimumId = 1L << 20;
        private const long IdMask = (1L << 62) - 1;

        private static readonly HashSet<int> FixedClasses = new HashSet<int>
        {
            29, 104, 157, 196, SceneRootsClass,
        };

        private static readonly Regex HeaderPattern = new Regex(@"^--- !u!(\d+) &(-?\d+)( stripped)?$");
        private static readonly Regex LocalReferencePattern = new Regex(@"\{fileID: (-?\d+)\}");
        private static readonly Regex ExternalReferencePattern =
            new Regex(@"\{fileID: (-?\d+), guid: ([0-9a-f]+), type: \d+\}");

        /// <summary>Normalises the scene file at <paramref name="path"/> in place. Returns true if it changed.</summary>
        public static bool NormalizeFile(string path)
        {
            string original = File.ReadAllText(path);
            string normalized = Normalize(original);
            if (string.Equals(original, normalized, StringComparison.Ordinal))
            {
                return false;
            }

            File.WriteAllText(path, normalized, new UTF8Encoding(false));
            return true;
        }

        /// <summary>Returns <paramref name="yaml"/> (a Unity text scene) with structural, deterministic file IDs.</summary>
        public static string Normalize(string yaml)
        {
            if (yaml == null)
            {
                throw new ArgumentNullException(nameof(yaml));
            }

            string newline = yaml.Contains("\r\n") ? "\r\n" : "\n";
            string[] lines = yaml.Replace("\r\n", "\n").Split('\n');
            var preamble = new List<string>();
            List<Document> documents = Parse(lines, preamble);
            var byId = new Dictionary<long, Document>();
            foreach (Document document in documents)
            {
                if (byId.ContainsKey(document.Id))
                {
                    throw new InvalidDataException($"Duplicate fileID {document.Id} in scene.");
                }

                byId.Add(document.Id, document);
            }

            var keys = new Dictionary<long, string>();
            AssignStructuralKeys(documents, byId, keys);
            AssignRemainingKeys(documents, keys);
            Dictionary<long, long> remap = AllocateIds(documents, keys);

            foreach (Document document in documents)
            {
                document.NewId = remap.TryGetValue(document.Id, out long newId) ? newId : document.Id;
            }

            documents.Sort((a, b) => a.NewId.CompareTo(b.NewId));
            var output = new StringBuilder(yaml.Length);
            foreach (string line in preamble)
            {
                output.Append(line).Append(newline);
            }

            for (int d = 0; d < documents.Count; d++)
            {
                Document document = documents[d];
                output.Append("--- !u!").Append(document.ClassId.ToString(CultureInfo.InvariantCulture))
                    .Append(" &").Append(document.NewId.ToString(CultureInfo.InvariantCulture))
                    .Append(document.Stripped ? " stripped" : string.Empty).Append(newline);
                for (int i = 0; i < document.Body.Count; i++)
                {
                    bool last = d == documents.Count - 1 && i == document.Body.Count - 1;
                    output.Append(RemapLine(document.Body[i], remap));
                    if (!last)
                    {
                        output.Append(newline);
                    }
                }
            }

            return output.ToString();
        }

        private static List<Document> Parse(string[] lines, List<string> preamble)
        {
            var documents = new List<Document>();
            Document current = null;
            foreach (string line in lines)
            {
                Match header = HeaderPattern.Match(line);
                if (header.Success)
                {
                    current = new Document(
                        int.Parse(header.Groups[1].Value, CultureInfo.InvariantCulture),
                        long.Parse(header.Groups[2].Value, CultureInfo.InvariantCulture),
                        header.Groups[3].Success);
                    documents.Add(current);
                }
                else if (current == null)
                {
                    preamble.Add(line);
                }
                else
                {
                    current.Body.Add(line);
                }
            }

            if (documents.Count == 0)
            {
                throw new InvalidDataException("Not a Unity text scene: no '--- !u!' documents.");
            }

            return documents;
        }

        private static void AssignStructuralKeys(List<Document> documents, Dictionary<long, Document> byId,
            Dictionary<long, string> keys)
        {
            Document roots = documents.Find(d => d.ClassId == SceneRootsClass);
            if (roots == null)
            {
                return;
            }

            List<long> rootIds = roots.ReferenceList("  m_Roots:", "  - ");
            for (int i = 0; i < rootIds.Count; i++)
            {
                VisitTransform(rootIds[i], string.Empty, i, byId, keys, documents);
            }
        }

        private static void VisitTransform(long transformId, string parentPath, int siblingIndex,
            Dictionary<long, Document> byId, Dictionary<long, string> keys, List<Document> documents)
        {
            if (!byId.TryGetValue(transformId, out Document transform) || keys.ContainsKey(transformId))
            {
                return;
            }

            if (transform.Stripped)
            {
                VisitPrefabInstance(transform, parentPath, siblingIndex, byId, keys, documents);
                return;
            }

            if (!byId.TryGetValue(transform.Reference("  m_GameObject: "), out Document gameObject))
            {
                return;
            }

            string path = $"{parentPath}/{gameObject.Field("  m_Name: ")}#{siblingIndex}";
            keys[gameObject.Id] = "GO|" + path;
            List<long> components = gameObject.ReferenceList("  m_Component:", "  - component: ");
            for (int i = 0; i < components.Count; i++)
            {
                if (!keys.ContainsKey(components[i]))
                {
                    keys[components[i]] = $"C|{path}|{i}";
                }
            }

            List<long> children = transform.ReferenceList("  m_Children:", "  - ");
            for (int i = 0; i < children.Count; i++)
            {
                VisitTransform(children[i], path, i, byId, keys, documents);
            }
        }

        private static void VisitPrefabInstance(Document strippedRoot, string parentPath, int siblingIndex,
            Dictionary<long, Document> byId, Dictionary<long, string> keys, List<Document> documents)
        {
            long instanceId = strippedRoot.Reference("  m_PrefabInstance: ");
            if (!byId.TryGetValue(instanceId, out Document instance) || keys.ContainsKey(instanceId))
            {
                return;
            }

            string source = instance.ExternalGuid("  m_SourcePrefab: ");
            string path = $"{parentPath}/<{source}>#{siblingIndex}";
            keys[instanceId] = "PI|" + path;
            foreach (Document document in documents)
            {
                if (document.Stripped && document.Reference("  m_PrefabInstance: ") == instanceId)
                {
                    keys[document.Id] = $"S|{path}|{document.ClassId}|{document.ExternalKey("  m_CorrespondingSourceObject: ")}";
                }
            }

            List<long> addedObjects = instance.ReferenceList("    m_AddedGameObjects:", "      addedObject: ");
            for (int i = 0; i < addedObjects.Count; i++)
            {
                VisitTransform(addedObjects[i], path + "|added", i, byId, keys, documents);
            }

            List<long> addedComponents = instance.ReferenceList("    m_AddedComponents:", "      addedObject: ");
            for (int i = 0; i < addedComponents.Count; i++)
            {
                if (!keys.ContainsKey(addedComponents[i]))
                {
                    keys[addedComponents[i]] = $"AC|{path}|{i}";
                }
            }
        }

        private static void AssignRemainingKeys(List<Document> documents, Dictionary<long, string> keys)
        {
            var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Document document in documents)
            {
                if (FixedClasses.Contains(document.ClassId) || keys.ContainsKey(document.Id))
                {
                    continue;
                }

                var content = new StringBuilder();
                foreach (string line in document.Body)
                {
                    content.Append(LocalReferencePattern.Replace(line, "{fileID: ?}")).Append('\n');
                }

                string key = $"U|{document.ClassId}|{Hash(content.ToString())}";
                occurrences.TryGetValue(key, out int count);
                occurrences[key] = count + 1;
                keys[document.Id] = $"{key}#{count}";
            }
        }

        private static Dictionary<long, long> AllocateIds(List<Document> documents, Dictionary<long, string> keys)
        {
            var used = new HashSet<long>();
            foreach (Document document in documents)
            {
                if (!keys.ContainsKey(document.Id))
                {
                    used.Add(document.Id);
                }
            }

            var ordered = new List<KeyValuePair<long, string>>(keys);
            ordered.Sort((a, b) => string.CompareOrdinal(a.Value, b.Value));
            var remap = new Dictionary<long, long>(ordered.Count);
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<long, string> entry in ordered)
            {
                if (!seenKeys.Add(entry.Value))
                {
                    throw new InvalidDataException($"Two scene objects share the structural key '{entry.Value}'.");
                }

                long id = HashToId(entry.Value, 0);
                for (int attempt = 1; !used.Add(id); attempt++)
                {
                    id = HashToId(entry.Value, attempt);
                }

                remap.Add(entry.Key, id);
            }

            return remap;
        }

        private static string RemapLine(string line, Dictionary<long, long> remap)
        {
            if (line.IndexOf("{fileID: ", StringComparison.Ordinal) < 0)
            {
                return line;
            }

            return LocalReferencePattern.Replace(line, match =>
            {
                long id = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                return remap.TryGetValue(id, out long mapped)
                    ? "{fileID: " + mapped.ToString(CultureInfo.InvariantCulture) + "}"
                    : match.Value;
            });
        }

        private static long HashToId(string key, int attempt)
        {
            using (var sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(attempt == 0 ? key : $"{key}\u0001{attempt}"));
                long value = BitConverter.ToInt64(digest, 0) & IdMask;
                return value < MinimumId ? value + MinimumId : value;
            }
        }

        private static string Hash(string text)
        {
            using (var sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                return BitConverter.ToString(digest, 0, 12).Replace("-", string.Empty);
            }
        }

        private sealed class Document
        {
            public Document(int classId, long id, bool stripped)
            {
                ClassId = classId;
                Id = id;
                Stripped = stripped;
            }

            public int ClassId { get; }

            public long Id { get; }

            public long NewId { get; set; }

            public bool Stripped { get; }

            public List<string> Body { get; } = new List<string>();

            /// <summary>Raw text after <paramref name="prefix"/> on the first line starting with it.</summary>
            public string Field(string prefix)
            {
                foreach (string line in Body)
                {
                    if (line.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        return line.Substring(prefix.Length);
                    }
                }

                return string.Empty;
            }

            /// <summary>The local fileID on the line starting with <paramref name="prefix"/>, or 0.</summary>
            public long Reference(string prefix)
            {
                Match match = LocalReferencePattern.Match(Field(prefix));
                return match.Success ? long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
            }

            /// <summary>"fileID:guid" of the external reference on the line starting with <paramref name="prefix"/>.</summary>
            public string ExternalKey(string prefix)
            {
                Match match = ExternalReferencePattern.Match(Field(prefix));
                return match.Success ? $"{match.Groups[1].Value}:{match.Groups[2].Value}" : string.Empty;
            }

            public string ExternalGuid(string prefix)
            {
                Match match = ExternalReferencePattern.Match(Field(prefix));
                return match.Success ? match.Groups[2].Value : string.Empty;
            }

            /// <summary>
            /// Local references listed under the <paramref name="header"/> line, taken from item lines starting with
            /// <paramref name="itemPrefix"/>; the list ends at the first line that is neither an item of it nor
            /// indented deeper than the header.
            /// </summary>
            public List<long> ReferenceList(string header, string itemPrefix)
            {
                var result = new List<long>();
                int start = Body.IndexOf(header);
                if (start < 0)
                {
                    return result;
                }

                string indent = header.Substring(0, header.Length - header.TrimStart().Length);
                for (int i = start + 1; i < Body.Count; i++)
                {
                    string line = Body[i];
                    bool item = line.StartsWith(indent + "- ", StringComparison.Ordinal);
                    bool nested = line.StartsWith(indent + "  ", StringComparison.Ordinal);
                    if (!item && !nested)
                    {
                        break;
                    }

                    if (line.StartsWith(itemPrefix, StringComparison.Ordinal))
                    {
                        Match match = LocalReferencePattern.Match(line, itemPrefix.Length);
                        if (match.Success)
                        {
                            result.Add(long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
                        }
                    }
                }

                return result;
            }
        }
    }
}
