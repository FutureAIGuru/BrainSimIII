using System.Text;
using System.Text.RegularExpressions;

namespace UKS;

public partial class UKS
{
    /// <summary>
    /// Export a neighborhood starting from <paramref name="root"/> to the bracketed txt file format.
    /// Emits facts as [S,R,O] (or [S,R,O,N] when R is a numeric specialization like "has.4").
    /// Optionally emits simple clause pairs if Relationship exposes a Clauses collection.
    /// </summary>
    public void ExportTextFile(
        string root,
        string path,
        int maxDepth = 12)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Start label is required.", nameof(root));

        using (var writer = new StreamWriter(path))
        {
            if (writer is null) throw new ArgumentNullException(nameof(writer));
            if (maxDepth < 0) maxDepth = 0;

            var start = Labeled(root) ?? throw new InvalidOperationException($"Thing '{root}' not found.");

            var q = new Queue<(Thing t, int d)>();
            var seenThings = new HashSet<string>(StringComparer.Ordinal);

            q.Enqueue((start, 0));
            seenThings.Add(start.Label);

            while (q.Count > 0)
            {
                var (t, depth) = q.Dequeue();
                if (depth > maxDepth) continue;

                foreach (Relationship r in t.Relationships)
                {
                    //don't save extra clause baggage.
                    if (!r.isStatement && r.Clauses.Count == 0) continue;
                    if (r.reltype.Label == "hasProperty" && r.target.Label == "isInstance") continue;
                    if (r.reltype.Label == "has-child" && r.target.HasProperty("isInstance"))
                    {
                        if (seenThings.Add(r.source.Label)) q.Enqueue((r.source, depth + 1));
                        if (seenThings.Add(r.target.Label)) q.Enqueue((r.target, depth + 1));
                        continue;
                    }

                    Thing theSource = GetNonInstance(r.source);
                    var line = $"[{theSource.Label},{r.relType.Label},{r.target.Label},{r.Weight.ToString("0.00")}]";
                    writer.Write(line);

                    foreach (Clause c in r.Clauses)
                    {
                        var clause = $" {c.clauseType.Label} [{c.clause.source.Label},{c.clause.reltype.Label},{c.clause.target.Label},{r.Weight.ToString("0.00")}] ";
                        writer.Write(clause);
                    }

                    writer.WriteLine();
                    if (depth < maxDepth)
                    {
                        if (seenThings.Add(r.source.Label)) q.Enqueue((r.source, depth + 1));
                        if (seenThings.Add(r.target.Label)) q.Enqueue((r.target, depth + 1));
                    }
                }
            }
            writer.Flush();
        }
    }

    public static Thing GetNonInstance(Thing source)
    {
        Thing theSource = source;
        while (theSource.HasProperty("isInstance")) theSource = theSource.Parents[0];
        return theSource;
    }

    // int or decimal, optional leading minus
    private static readonly Regex NumericRegex = new(@"^-?\d+(\.\d+)?$", RegexOptions.Compiled);

    /// <summary>
    /// Text file format format:
    /// A line is one or more bracketed statements [S,R,O] or [S,R,O,N] (N weight),
    /// optionally chained by RELTYPE connector tokens between them.
    /// Examples:
    ///   [Dog,has.4,leg]
    ///   [Fido,plays,outside] IF [weather,is,sunny]
    ///   [A,rel,B] WITH [C,rel,D] BECAUSE [E,rel,F]
    /// Comments (# or //) allowed outside quotes/brackets.
    /// </summary>
    public void ImportTextFile(string filePath)
    {
        if (filePath == null) throw new ArgumentNullException(nameof(filePath));

        int lineNo = 0;
        foreach (var raw in File.ReadLines(filePath))
        {
            lineNo++;
            bool flowControl = ParseOneLine(lineNo, raw);
            if (!flowControl)
            {
                continue;
            }
        }
    }

    public bool ParseOneLine(int lineNo, string raw)
    {
        string code = StripEolComment(raw);
        if (string.IsNullOrWhiteSpace(code)) return false;

        var tokens = TokenizeTopLevel(code); // bracket tokens + connector tokens
        if (tokens.Count == 0) return false;

        // Validate pattern: must start with [ ... ], alternate connector, [ ... ], ...
        if (tokens.Count % 2 == 0)
            throw new FormatException($"Line {lineNo}: expected odd number of tokens ([S,R,O] (REL [S,R,O])*)");

        for (int i = 0; i < tokens.Count; i += 2)
            if (!IsBracket(tokens[i]))
                throw new FormatException($"Line {lineNo}: token {i + 1} must be a [S,R,O] statement.");

        // Build relationships for each [S,R,O(,N)?]
        var rels = new List<Relationship>();
        bool isStatement = tokens.Count < 3;
        for (int i = 0; i < tokens.Count; i += 2)
        {
            var stmt = ParseBracketStmt(tokens[i], lineNo);
            Relationship r = AddRelStmt(stmt,isStatement);
            rels.Add(r);
            r.isStatement = isStatement;
        }

        // Connect adjacent relationships with AddClause(connector)
        for (int i = 1, relIndex = 0; i < tokens.Count; i += 2, relIndex++)
        {
            string relTypeConnector = tokens[i];
            if (relTypeConnector == "AFTER")
            { }
            Thing t = Labeled(relTypeConnector);
            if (t == null)
                t = AddThing(relTypeConnector, "ClauseType");
            Relationship r =  AddClause(rels[relIndex],relTypeConnector, rels[relIndex + 1]);
            r.isStatement = isStatement;
        }

        return true;
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
    private Relationship AddRelStmt(BrStmt s,bool isSatement)
    {
        string[] source = s.S.Split(".");
        string[] relType = s.R.Split(".");
        string[] target = s.O.Split(".");

        Relationship r = AddStatement(source[0], relType[0], target[0], source[1..], relType[1..], target[1..], isSatement);
        if (s.N is { } n)
        {
            if (float.TryParse(s.N, out float weight))
                r.Weight = weight;
        }
        return r;
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


