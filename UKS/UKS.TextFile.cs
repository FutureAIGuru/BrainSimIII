/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Thought and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
using System.Text;
using System.Text.RegularExpressions;

namespace UKS;

public partial class UKS
{
    /// <summary>
    /// Export a neighborhood starting from <paramref name="root"/> to the bracketed txt file format.
    /// Emits facts as [S,R,O] (or [S,R,O,N] when R is a numeric specialization like "has.4").
    /// Optionally emits simple clause pairs if Thought exposes a Clauses collection.
    /// </summary>
    public void ExportTextFile(string root, string path, int maxDepth = 12)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Start label is required.", nameof(root));
        Thought Root = theUKS.Labeled(root);
        if (Root is null) return;


        HashSet<string> alreadyWritten = new();
        try
        {
            using (var writer = new StreamWriter(path))
            {
                if (writer is null) throw new ArgumentNullException(nameof(writer));
                foreach (var t in Root.EnumerateSubThoughts())
                {
                    string s = FormatThought(t) + " " + t.Weight.ToString("F2");
                    if (!alreadyWritten.Contains(s))
                    {
                        writer.WriteLine(s);
                        alreadyWritten.Add(s);
                    }
                }
                writer.Flush();
            }
        }
        catch (Exception ex)
        { }
        RemoveTempLabels(Root);
    }

    private void RemoveTempLabels(Thought Root)
    {
        if (Root is null) return;
        //remove unnecessary "unl_..."  labels
        foreach (var t in Root.EnumerateSubThoughts())
        {
            if (t.Label.ToLower() == "fido")
            { }
            if (t.Label.StartsWith("unl_"))
                t.Label = "";
        }
    }

    void EnsureLabel(Thought t)
    {
        if (string.IsNullOrWhiteSpace(t.Label))
            // Put the GUID into the label only when it's unlabeled
            t.Label = $"unl_{Guid.NewGuid().ToString("N")[..8]}";
    }

    string FormatThought(Thought t)
    {
        EnsureLabel(t);
        string retVal = t.Label.PadRight(15);
        if (t.V is not null)
            retVal += " V: " + t.V.ToString();

        if (t.From is null || t.LinkType is null)
            return retVal;

        retVal += "[";
        if (t.From is not null)
            retVal += t.From?.Label;
        if (t.LinkType is not null)
            retVal += ((retVal == "") ? "" : "->") + t.LinkType?.Label;
        if (t.To is not null)
            retVal += ((retVal == "") ? "" : "->") + ((t.To.Label == "") ? t.To?.Label : t.To.Label);
        retVal += "]";
        return retVal;
    }




    // int or decimal, optional leading minus
    private static readonly Regex NumericRegex = new(@"^-?\d+(\.\d+)?$", RegexOptions.Compiled);

    /// <summary>
    /// Text file format format:
    /// A line represents a relationsship of the form LABEL[S->T->O]Weight,
    /// S,T,&O may themseves be bracketed relatisnips.
    /// Examples:
    ///   [Dog->has.4->leg]0.90
    ///   R23[Fido->plays->outside] IF [weather,is,sunny]1.00
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
            Thought r = AddLinkStmt(tokens[0], stmt, tokens[2]);
        }

        //remove unnecessary "unl_..."  labels
        foreach (var t in ((Thought)"Thought").EnumerateSubThoughts())
        {
            if (t.Label.StartsWith("unl_"))
            {
                t.Label = "";
            }
        }
        //This is a bit of a hack because the default AddStatement adds sequence elements to Unknown unnecessarily
        for (int i = 0; i < theUKS.AllThoughts.Count; i++)
        {
            Thought t = AllThoughts[i];
            if (IsSequenceElement(t))
                t.RemoveLink("Unknown", "is-a");
        }
    }


    // Adds a link, 
    private Thought AddLinkStmt(string label, List<string> linkParts, string sWeight)
    {
        if (linkParts[0].Contains("seq2"))
        { }
        Thought r = null;
        if (linkParts.Count < 2) return null;
        if (r is null)
        {
            //get value strings (used in config
            string value = "";
            if (linkParts[0].Contains("_V:"))
            {
                int index = linkParts[0].IndexOf("_V:");
                value = linkParts[0][(index + 3)..];
                linkParts[0] = linkParts[0][..index];
                DeleteThought(linkParts[0]);
            }
            //if (r1 or r2 are set, use them instead here
            Thought from = Labeled(linkParts[0]);
            if (from is null) from = AddThought(linkParts[0], null);
            Thought linkType = Labeled(linkParts[1]);
            if (linkType is null) linkType = AddThought(linkParts[1], null);
            Thought to = null;
            if (linkParts.Count > 2)
            {
                to = Labeled(linkParts[2]);
                if (to is null) to = AddThought(linkParts[2], null);
            }

            r = AddStatement(from, linkType, to, label);

            if (value != "")
                r.From.V = value;
            if (label != "" && !label.StartsWith("unl_"))
            {
                r.Label = label.Trim();
                if (!AllThoughts.Contains(r))
                    AllThoughts.Add(r);
            }
        }
        if (sWeight is { } n)
        {
            if (float.TryParse(n, out float weight))
                r.Weight = weight;
        }
        return r;
    }
    // Parse "[F->L->T]" or "[S,R,O,N]" (comma separated, quotes allowed around items)
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

        result.Add(sb.ToString().Trim());
        return result;
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


