using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace UKS;

public partial class UKS
{
    /// <summary>
    /// Export a neighborhood starting from <paramref name="root"/> to the bracketed txt file format.
    /// Emits facts as [S,R,O] (or [S,R,O,N] when R is a numeric specialization like "has.4").
    /// Optionally emits simple clause pairs if Thing exposes a Clauses collection.
    /// </summary>
    public void ExportTextFile(string root, string path, int maxDepth = 12)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Start label is required.", nameof(root));
        Cogneme Root = theUKS.Labeled(root);
        if (Root is null) return;
        HashSet<string> alreadyWritten = new();
        try
        {
            using (var writer = new StreamWriter(path))
            {
                if (writer is null) throw new ArgumentNullException(nameof(writer));
                foreach (Cogneme t in Root.Descendants)
                {
                    foreach (Cogneme r in t.RecursiveRelationships)
                    {
                        string s = r.ToString() + r.Weight.ToString("0.00");
                        if (!alreadyWritten.Contains(s))
                        {
                            writer.WriteLine(s);
                            alreadyWritten.Add(s);
                        }

                        //hack to follow sequences
                        //if (r?.RelType?.Label == "is-a") continue;

                        foreach (Cogneme t1 in r.Target.SequenceNodes())
                        {
                            if (t1 is Cogneme r1 && r1.RelType is not null)
                            {
                                s = r1.SingleToString() + r1.Weight.ToString("0.00");
                                if (!alreadyWritten.Contains(s))
                                {
                                    writer.WriteLine(s);
                                    alreadyWritten.Add(s);
                                }
                            }
                            foreach (Cogneme r2 in t1.Relationships.Where(x => x.RelType.Label != "is-a"))
                            {
                                s = r2.SingleToString() + r2.Weight.ToString("0.00");
                                if (!alreadyWritten.Contains(s))
                                {
                                    writer.WriteLine(s);
                                    alreadyWritten.Add(s);
                                }
                            }
                        }
                    }
                }
                writer.Flush();
            }
        }
        catch(Exception ex)
        { }
    }


    public static Cogneme GetNonInstance(Cogneme source)
    {
        Cogneme theSource = source;
        while (theSource.HasProperty("isInstance")) theSource = theSource.Parents[0];
        return theSource;
    }

    // int or decimal, optional leading minus
    private static readonly Regex NumericRegex = new(@"^-?\d+(\.\d+)?$", RegexOptions.Compiled);

    /// <summary>
    /// Text file format format:
    /// A line represents a relationsship of the form LABEL[S->T->O]Weight,
    /// S,T,&O may themseves be bracketed relatisnips.
    /// Examples:
    ///   [Dog->has.4->leg]0.90
    ///   R23[Fido,plays,outside] IF [weather,is,sunny]1.00
    /// Comments (# or //) allowed outside quotes/brackets.
    /// </summary>
    public void ImportTextFile(string filePath)
    {
        if (filePath is null) throw new ArgumentNullException(nameof(filePath));

        int lineNo = 0;
        foreach (var raw in File.ReadLines(filePath))
        {
            lineNo++;
            string code = StripEolComment(raw);
            if (string.IsNullOrWhiteSpace(code)) continue;

            var tokens = TokenizeTopLevel(code); // bracket tokens + connector tokens
            if (tokens.Count == 0) continue;

            var stmt = ParseBracketStmt(tokens[1], lineNo);
            Cogneme r = AddRelStmt(tokens[0], stmt, tokens[2]);
        }
    }


    // Parse "[S->R->O]" or "[S,R,O,N]" (comma separated, quotes allowed around items)
    private static List<string> ParseBracketStmt(string s, int lineNo)
    {
        s = s.Substring(1, s.Length - 2); // drop initialFinal [ ]
        var result = new List<string>();
        var sb = new StringBuilder();
        int bracketDepth = 0;

        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '[') bracketDepth++;
            if (s[i] == ']') bracketDepth--;

            if (bracketDepth == 0 &&
                i + 1 < s.Length &&
                s[i] == '-' &&
                s[i + 1] == '>')
            {
                result.Add(sb.ToString());
                sb.Clear();
                i++; // skip '>'
                continue;
            }

            sb.Append(s[i]);
        }

        result.Add(sb.ToString());
        return result;
    }

    // Adds a relationship, honoring numeric sugar (N → R.N + has-value + number typing)
    private Cogneme AddRelStmt(string label, List<string> ss, string sWeight)
    {
        Cogneme r = null;
        if (ss.Count < 3) return null;
        if (r is null)
        {
            object r1 = ss[0];
            object r2 = ss[2];
            if (ss[0].Contains("->"))
            {
                var stmtContent = TokenizeTopLevel(ss[0]);
                var stmtContent1 = ParseBracketStmt(stmtContent[1], -1);
                r1 = AddRelStmt(stmtContent[0], stmtContent1, stmtContent[2]);
                r1 = ((Cogneme)r1).Label;
            }
            if (ss[2].Contains("->"))
            {
                var stmtContent = TokenizeTopLevel(ss[2]);
                var stmtContent1 = ParseBracketStmt(stmtContent[1], -1);
                r2 = AddRelStmt(stmtContent[0], stmtContent1, stmtContent[2]);
                r2 = ((Cogneme)r2).Label;
            }

            //if (r1 or r2 are set, use them instead here
            r = AddStatement((string)r1, ss[1], (string)r2);
            if (label != "")
                r.Label = label;
        }
        if (sWeight is { } n)
        {
            if (float.TryParse(n, out float weight))
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

        int leftBracketPos = code.IndexOf("[");
        int rightBrackedPos = code.LastIndexOf("]") + 1;
        if (leftBracketPos == -1 || rightBrackedPos == -1) return tokens;

        string label = code[..leftBracketPos];
        string weight = code[rightBrackedPos..];
        string body = code[leftBracketPos..rightBrackedPos];

        tokens.Add(label);
        tokens.Add(body);
        tokens.Add(weight);

        return tokens;
    }
}


