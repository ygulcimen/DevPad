using System.Collections.Generic;
using DevPad.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevPad.Services;

public class JsonFormatterService
{
    /// <summary>
    /// Maximum number of tree nodes rendered in the UI.
    /// Prevents the TreeView from hanging on multi-megabyte payloads.
    /// </summary>
    public const int MaxTreeNodes = 5_000;

    /// <summary>Pretty-prints JSON with 2-space indentation. Returns null on parse error.</summary>
    public string? Format(string json, out string? error)
    {
        try
        {
            error = null;
            return JToken.Parse(json).ToString(Formatting.Indented);
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return null;
        }
    }

    /// <summary>Minifies JSON. Returns null on parse error.</summary>
    public string? Minify(string json, out string? error)
    {
        try
        {
            error = null;
            return JToken.Parse(json).ToString(Formatting.None);
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Parses JSON into a tree of <see cref="JsonTreeNode"/> capped at <see cref="MaxTreeNodes"/> nodes.
    /// Returns the (possibly truncated) tree, the number of nodes built, whether the tree was truncated,
    /// and any parse error. A non-null Error means the input is invalid JSON.
    /// </summary>
    public (List<JsonTreeNode> Tree, int NodeCount, bool Truncated, string? Error) BuildTree(string json)
    {
        try
        {
            var token   = JToken.Parse(json);
            int remaining = MaxTreeNodes;
            var root    = BuildNode(null, token, ref remaining);
            int built   = MaxTreeNodes - remaining;
            bool truncated = remaining <= 0;
            return (new List<JsonTreeNode> { root }, built, truncated, null);
        }
        catch (JsonException ex)
        {
            return (new List<JsonTreeNode>(), 0, false, ex.Message);
        }
        catch
        {
            return (new List<JsonTreeNode>(), 0, false, "Parse error");
        }
    }

    /// <summary>
    /// Single-parse version: formats the JSON and builds the tree in one go.
    /// Avoids the double-parse that would result from calling Format then BuildTree separately.
    /// </summary>
    public (string Formatted, List<JsonTreeNode> Tree, int NodeCount, bool Truncated, string? Error)
        BuildTreeAndFormat(string json)
    {
        try
        {
            var token      = JToken.Parse(json);
            string fmt     = token.ToString(Formatting.Indented);
            int remaining  = MaxTreeNodes;
            var root       = BuildNode(null, token, ref remaining);
            int built      = MaxTreeNodes - remaining;
            bool truncated = remaining <= 0;
            return (fmt, new List<JsonTreeNode> { root }, built, truncated, null);
        }
        catch (JsonException ex)
        {
            return (string.Empty, new List<JsonTreeNode>(), 0, false, ex.Message);
        }
        catch
        {
            return (string.Empty, new List<JsonTreeNode>(), 0, false, "Parse error");
        }
    }

    // ── Recursive tree builders ──────────────────────────────────────────

    private static JsonTreeNode BuildNode(string? key, JToken token, ref int remaining)
    {
        if (remaining <= 0)
            // Budget exhausted — show a placeholder leaf instead of recursing
            return new JsonTreeNode { Key = key, Value = "…", TokenType = JsonNodeType.Null };

        remaining--;

        if (token.Type == JTokenType.Object)  return BuildObjectNode(key, (JObject)token, ref remaining);
        if (token.Type == JTokenType.Array)   return BuildArrayNode(key,  (JArray)token,  ref remaining);
        return BuildValueNode(key, token);
    }

    private static JsonTreeNode BuildObjectNode(string? key, JObject obj, ref int remaining)
    {
        var node = new JsonTreeNode { Key = key, TokenType = JsonNodeType.Object };
        foreach (var prop in obj.Properties())
        {
            if (remaining <= 0) break;
            node.Children.Add(BuildNode(prop.Name, prop.Value, ref remaining));
        }
        return node;
    }

    private static JsonTreeNode BuildArrayNode(string? key, JArray arr, ref int remaining)
    {
        var node = new JsonTreeNode { Key = key, TokenType = JsonNodeType.Array };
        for (int i = 0; i < arr.Count; i++)
        {
            if (remaining <= 0) break;
            node.Children.Add(BuildNode($"[{i}]", arr[i], ref remaining));
        }
        return node;
    }

    private static JsonTreeNode BuildValueNode(string? key, JToken token)
    {
        var type = token.Type switch
        {
            JTokenType.String              => JsonNodeType.String,
            JTokenType.Integer
            or JTokenType.Float            => JsonNodeType.Number,
            JTokenType.Boolean             => JsonNodeType.Boolean,
            _                              => JsonNodeType.Null
        };
        return new JsonTreeNode { Key = key, Value = token.ToString(), TokenType = type };
    }
}
