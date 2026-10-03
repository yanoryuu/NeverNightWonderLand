#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// テストプレイボット用の最小 JSON (外部パッケージに依存しないため自前)。
/// 書き出し: Dictionary&lt;string, object&gt; / IList / string / bool / 数値 / null を再帰的に文字列化。
/// 読み込み: object (Dictionary&lt;string, object&gt; / List&lt;object&gt; / string / double / bool / null) に戻す。
/// </summary>
public static class BotJson
{
    // ------------------------------------------------------------------ 書き出し

    public static string Serialize(object value)
    {
        var sb = new StringBuilder(1024);
        Write(sb, value);
        return sb.ToString();
    }

    private static void Write(StringBuilder sb, object value)
    {
        switch (value)
        {
            case null:
                sb.Append("null");
                break;
            case string s:
                WriteString(sb, s);
                break;
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            case float f:
                WriteNumber(sb, f);
                break;
            case double d:
                WriteNumber(sb, d);
                break;
            case int or long or short or byte or uint or ulong:
                sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
            case IDictionary dict:
            {
                sb.Append('{');
                var first = true;
                foreach (DictionaryEntry e in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, Convert.ToString(e.Key, CultureInfo.InvariantCulture));
                    sb.Append(':');
                    Write(sb, e.Value);
                }

                sb.Append('}');
                break;
            }
            case IEnumerable list:
            {
                sb.Append('[');
                var first = true;
                foreach (var item in list)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    Write(sb, item);
                }

                sb.Append(']');
                break;
            }
            default:
                WriteString(sb, value.ToString());
                break;
        }
    }

    private static void WriteNumber(StringBuilder sb, double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d))
        {
            sb.Append("null");
            return;
        }

        // 2 桁に丸める (状態を小さく保つ)
        sb.Append(Math.Round(d, 2).ToString("0.##", CultureInfo.InvariantCulture));
    }

    private static void WriteString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }

        sb.Append('"');
    }

    // ------------------------------------------------------------------ 読み込み

    public static object Parse(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        var index = 0;
        var value = ParseValue(json, ref index);
        return value;
    }

    private static void SkipWs(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }

    private static object ParseValue(string s, ref int i)
    {
        SkipWs(s, ref i);
        if (i >= s.Length) return null;
        var c = s[i];
        if (c == '{') return ParseObject(s, ref i);
        if (c == '[') return ParseArray(s, ref i);
        if (c == '"') return ParseString(s, ref i);
        if (c == 't' && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
        if (c == 'f' && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
        if (c == 'n' && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
        return ParseNumber(s, ref i);
    }

    private static Dictionary<string, object> ParseObject(string s, ref int i)
    {
        var result = new Dictionary<string, object>();
        i++; // {
        while (true)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) break;
            if (s[i] == '}') { i++; break; }
            if (s[i] == ',') { i++; continue; }
            var key = ParseString(s, ref i);
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ':') i++;
            result[key] = ParseValue(s, ref i);
        }

        return result;
    }

    private static List<object> ParseArray(string s, ref int i)
    {
        var result = new List<object>();
        i++; // [
        while (true)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) break;
            if (s[i] == ']') { i++; break; }
            if (s[i] == ',') { i++; continue; }
            result.Add(ParseValue(s, ref i));
        }

        return result;
    }

    private static string ParseString(string s, ref int i)
    {
        var sb = new StringBuilder();
        i++; // "
        while (i < s.Length)
        {
            var c = s[i++];
            if (c == '"') break;
            if (c != '\\' || i >= s.Length)
            {
                sb.Append(c);
                continue;
            }

            var e = s[i++];
            switch (e)
            {
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'u':
                    if (i + 4 <= s.Length)
                    {
                        sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                    }

                    break;
                default: sb.Append(e); break;
            }
        }

        return sb.ToString();
    }

    private static object ParseNumber(string s, ref int i)
    {
        var start = i;
        while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
        if (i == start)
        {
            i++; // 不正な文字は飛ばす
            return null;
        }

        double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var d);
        return d;
    }

    // ------------------------------------------------------------------ 読み出しの補助

    public static object Get(object node, string key) =>
        node is Dictionary<string, object> d && d.TryGetValue(key, out var v) ? v : null;

    public static string GetString(object node, string key) => Get(node, key) as string;

    public static double GetNumber(object node, string key, double fallback = 0) =>
        Get(node, key) is double d ? d : fallback;
}
#endif
