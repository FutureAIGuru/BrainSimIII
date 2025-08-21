//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using UKS;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleLoadTxtFile : ModuleBase
    {
        // Any public variable you create here will automatically be saved and restored  
        // with the network unless you precede it with the [XmlIgnore] directive
        // [XmlIgnore] 
        // public theStatus = 1;


        // Fill this method in with code which will execute
        // once for each cycle of the engine
        public override void Fire()
        {
            Init();

            UpdateDialog();
        }

        // Fill this method in with code which will execute once
        // when the module is added, when "initialize" is selected from the context menu,
        // or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        // The following can be used to massage public data to be different in the xml file
        // delete if not needed
        public override void SetUpBeforeSave()
        {
        }
        public override void SetUpAfterLoad()
        {
        }

        // called whenever the UKS performs an Initialize()
        public override void UKSInitializedNotification()
        {

        }

        // int or decimal, optional leading minus
        private static readonly Regex NumericRegex = new(@"^-?\d+(\.\d+)?$", RegexOptions.Compiled);

        /// <summary>
        /// <summary>
        /// v3 format:
        /// A line is one or more bracketed statements [S,R,O] or [S,R,O,N] (N numeric),
        /// optionally chained by RELTYPE connector tokens between them.
        /// Examples:
        ///   [Dog,has,leg,4]
        ///   [Fido,plays,outside] IF [weather,is,sunny]
        ///   [A,rel,B] WITH [C,rel,D] BECAUSE [E,rel,F]
        /// Comments (# or //) allowed outside quotes/brackets.
        /// </summary>
        public void IngestTxt(UKS.UKS uks, string filePath)
        {
            if (uks == null) throw new ArgumentNullException(nameof(uks));
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            int lineNo = 0;
            foreach (var raw in File.ReadLines(filePath))
            {
                lineNo++;

                string code = StripEolComment(raw);
                if (string.IsNullOrWhiteSpace(code)) continue;

                var tokens = TokenizeTopLevel(code); // bracket tokens + connector tokens
                if (tokens.Count == 0) continue;

                // Validate pattern: must start with [ ... ], alternate connector, [ ... ], ...
                if (tokens.Count % 2 == 0)
                    throw new FormatException($"Line {lineNo}: expected odd number of tokens ([S,R,O] (REL [S,R,O])*)");

                for (int i = 0; i < tokens.Count; i += 2)
                    if (!IsBracket(tokens[i]))
                        throw new FormatException($"Line {lineNo}: token {i + 1} must be a [S,R,O] statement.");

                // Build relationships for each [S,R,O(,N)?]
                var rels = new List<Relationship>();
                for (int i = 0; i < tokens.Count; i += 2)
                {
                    var stmt = ParseBracketStmt(tokens[i], lineNo);
                    rels.Add(AddRelStmt(uks, stmt));
                }

                // Connect adjacent relationships with AddClause(connector)
                for (int i = 1, relIndex = 0; i < tokens.Count; i += 2, relIndex++)
                {
                    string relTypeConnector = tokens[i];
                    Thing t = uks.Labeled(relTypeConnector);
                    if (t == null)
                        t = uks.AddThing(relTypeConnector, "RelationshipType");
                    rels[relIndex].AddClause(relTypeConnector, rels[relIndex + 1]);
                }
            }
        }

        private static bool IsNumeric(string s) => NumericRegex.IsMatch(s);
        private static bool IsBracket(string s) => s.Length >= 2 && s[0] == '[' && s[^1] == ']';

        private sealed record BrStmt(string S, string R, string O, string? N);

        // Parse "[S,R,O]" or "[S,R,O,N]" (comma separated, quotes allowed around items)
        private static BrStmt ParseBracketStmt(string bracketToken, int lineNo)
        {
            var inner = bracketToken.Substring(1, bracketToken.Length - 2); // drop [ ]
            var parts = SplitCsvLike(inner); // returns unquoted, trimmed items

            if (parts.Count == 3)
                return new BrStmt(parts[0], parts[1], parts[2], null);

            if (parts.Count == 4 && IsNumeric(parts[3]))
                return new BrStmt(parts[0], parts[1], parts[2], parts[3]);

            throw new FormatException(
                $"Line {lineNo}: bracket must be [S,R,O] or [S,R,O,N] with numeric N. Got: [{inner}]");
        }

        // Adds a relationship, honoring numeric sugar (N → R.N + has-value + number typing)
        private static Relationship AddRelStmt(UKS.UKS uks, BrStmt s)
        {
            if (s.N is { } n)
            {
                var rN = $"{s.R}.{n}";
                uks.AddStatement(rN, "is-a", s.R);
                uks.AddStatement(rN, "has-value", n);
                uks.AddStatement(n, "is-a", "number");
                return uks.AddStatement(s.S, rN, s.O);
            }
            return uks.AddStatement(s.S, s.R, s.O);
        }

        // Strip EOL comments outside of quotes and brackets
        private static string StripEolComment(string line)
        {
            if (line is null) return string.Empty;

            var sb = new StringBuilder(line.Length);
            bool inQuotes = false;
            int bracketDepth = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (!inQuotes)
                {
                    if (c == '[') { bracketDepth++; sb.Append(c); continue; }
                    if (c == ']' && bracketDepth > 0) { bracketDepth--; sb.Append(c); continue; }
                }

                if (c == '"' && bracketDepth == 0)
                {
                    inQuotes = !inQuotes;
                    sb.Append(c);
                    continue;
                }

                if (!inQuotes && bracketDepth == 0)
                {
                    if (c == '#') break;
                    if (c == '/' && i + 1 < line.Length && line[i + 1] == '/') break;
                }

                sb.Append(c);
            }

            return sb.ToString().Trim();
        }

        // Tokenize top-level into: [ ... ]  or  connector tokens (whitespace separated)
        private static List<string> TokenizeTopLevel(string code)
        {
            var tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(code)) return tokens;

            int i = 0, n = code.Length;
            while (i < n)
            {
                // skip whitespace
                while (i < n && char.IsWhiteSpace(code[i])) i++;
                if (i >= n) break;

                if (code[i] == '[')
                {
                    int start = i++;
                    int depth = 1;
                    bool inQuotes = false;
                    bool esc = false;

                    for (; i < n; i++)
                    {
                        char c = code[i];
                        if (inQuotes)
                        {
                            if (esc) { esc = false; continue; }
                            if (c == '\\') { esc = true; continue; }
                            if (c == '"') { inQuotes = false; continue; }
                            continue;
                        }
                        else
                        {
                            if (c == '"') { inQuotes = true; continue; }
                            if (c == '[') { depth++; continue; }
                            if (c == ']')
                            {
                                depth--;
                                if (depth == 0) { i++; break; }
                                continue;
                            }
                        }
                    }

                    if (depth != 0)
                        throw new FormatException("Unclosed bracketed statement.");

                    tokens.Add(code.Substring(start, i - start)); // inclusive [ ... ]
                    continue;
                }

                // connector token until next whitespace
                int j = i;
                while (j < n && !char.IsWhiteSpace(code[j])) j++;
                tokens.Add(code.Substring(i, j - i));
                i = j;
            }

            return tokens;
        }

        // Split comma-separated content (not top-level), supporting quotes and escapes, then trim and unquote items
        private static List<string> SplitCsvLike(string s)
        {
            var items = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            bool esc = false;

            void Flush()
            {
                var raw = sb.ToString().Trim();
                items.Add(Unquote(raw));
                sb.Clear();
            }

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (inQuotes)
                {
                    if (esc) { sb.Append(c); esc = false; continue; }
                    if (c == '\\') { esc = true; continue; }
                    if (c == '"') { inQuotes = false; continue; }
                    sb.Append(c);
                    continue;
                }

                if (c == '"') { inQuotes = true; continue; }
                if (c == ',') { Flush(); continue; }

                sb.Append(c);
            }

            Flush();
            return items;
        }

        private static string Unquote(string s)
        {
            if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
            {
                var inner = s.Substring(1, s.Length - 2);
                return inner.Replace("\\\"", "\"").Replace("\\\\", "\\");
            }
            return s;
        }
    }
}
