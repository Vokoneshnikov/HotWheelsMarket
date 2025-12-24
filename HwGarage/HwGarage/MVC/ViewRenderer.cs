using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HwGarage.MVC
{
    public class ViewRenderer
    {
        private readonly string _viewsRoot;

        public ViewRenderer(string viewsRoot)
        {
            _viewsRoot = viewsRoot;
        }
        
        public async Task<string> RenderAsync(string viewPath, Dictionary<string, object>? data = null)
        {
            string fullPath = Path.Combine(
                _viewsRoot,
                viewPath.Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"View not found: {fullPath}");

            string template = await File.ReadAllTextAsync(fullPath);
            return RenderTemplate(template, data ?? new Dictionary<string, object>());
        }
        
        private string RenderTemplate(string template, Dictionary<string, object> data)
        {
            if (data == null || data.Count == 0)
                return template;

            template = RenderPlaceholders(
                RenderEachBlocks(
                    RenderIfBlocks(
                        template,
                        data),
                    data),
                data);
            return template;
        }

        
        private string RenderIfBlocks(string template, Dictionary<string, object> data)
        {
            var ifRegex = new Regex(
                @"\{\{\s*#if\s+(.+?)\s*\}\}(.*?)\{\{\s*/if\s*\}\}",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );

            string Evaluator(Match match)
            {
                var conditionRaw = match.Groups[1].Value;  // то, что между #if и }}
                var inner = match.Groups[2].Value;         // содержимое между {{#if}} и {{/if}}

                // Разделяем на if-часть и else-часть (если есть)
                var parts = Regex.Split(
                    inner,
                    @"\{\{\s*else\s*\}\}",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase
                );

                string ifBlock = parts.Length > 0 ? parts[0] : string.Empty;
                string elseBlock = parts.Length > 1 ? parts[1] : string.Empty;

                bool condition = EvaluateCondition(conditionRaw, data);

                var chosen = condition ? ifBlock : elseBlock;

                return RenderTemplate(chosen, data);
            }

            return ifRegex.Replace(template, m => Evaluator(m));
        }
        
        private bool EvaluateCondition(string conditionRaw, Dictionary<string, object> data)
        {
            if (string.IsNullOrWhiteSpace(conditionRaw))
            {
                return false;
            }

            var expr = conditionRaw.Trim();

            bool negate = false;
            if (expr.StartsWith("!"))
            {
                negate = true;
                expr = expr.Substring(1).Trim();
            }

            // сейчас поддерживаем только простые имена без точек
            var key = expr;

            data.TryGetValue(key, out var value);

            bool result = IsTruthy(value);
            return negate ? !result : result;
        }
        
        private bool IsTruthy(object? value)
        {
            if (value == null)
                return false;

            switch (value)
            {
                case bool b:
                    return b;

                case string s:
                    return !string.IsNullOrWhiteSpace(s);

                case sbyte sb:
                    return sb != 0;
                case byte by:
                    return by != 0;
                case short sh:
                    return sh != 0;
                case ushort ush:
                    return ush != 0;
                case int i:
                    return i != 0;
                case uint ui:
                    return ui != 0;
                case long l:
                    return l != 0;
                case ulong ul:
                    return ul != 0;
                case float f:
                    return Math.Abs(f) > float.Epsilon;
                case double d:
                    return Math.Abs(d) > double.Epsilon;
                case decimal dec:
                    return dec != 0m;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                var enumerator = enumerable.GetEnumerator();
                try
                {
                    return enumerator.MoveNext();
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            }

            return true;
        }
        
        private string RenderEachBlocks(string template, Dictionary<string, object> data)
        {
            var eachRegex = new Regex(
                @"\{\{\s*#each\s+([a-zA-Z0-9_]+)\s*\}\}(.*?)\{\{\s*/each\s*\}\}",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );

            string Evaluator(Match match)
            {
                var collectionName = match.Groups[1].Value;
                var blockContent = match.Groups[2].Value;

                if (!data.TryGetValue(collectionName, out var collectionObj) ||
                    collectionObj is not IEnumerable enumerable)
                {
                    return string.Empty;
                }

                var sb = new StringBuilder();

                foreach (var item in enumerable)
                {
                    var itemData = ItemToDictionary(item);

                    var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                    // родительские данные
                    foreach (var kv in data)
                        merged[kv.Key] = kv.Value;

                    // данные элемента
                    foreach (var kv in itemData)
                        merged[kv.Key] = kv.Value;

                    var renderedBlock = RenderTemplate(blockContent, merged);
                    sb.Append(renderedBlock);
                }

                return sb.ToString();
            }

            return eachRegex.Replace(template, m => Evaluator(m));
        }
        
        private string RenderPlaceholders(string template, Dictionary<string, object> data)
        {
            foreach (var kv in data)
            {
                var pattern = @"\{\{\s*" + Regex.Escape(kv.Key) + @"\s*\}\}";
                template = Regex.Replace(
                    template,
                    pattern,
                    kv.Value?.ToString() ?? string.Empty,
                    RegexOptions.IgnoreCase
                );
            }

            return template;
        }
        
        private static Dictionary<string, object> ItemToDictionary(object item)
        {
            if (item is Dictionary<string, object> dict)
                return new Dictionary<string, object>(dict, StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var type = item.GetType();

            foreach (var prop in type.GetProperties())
            {
                var name = prop.Name;
                var value = prop.GetValue(item);
                result[name] = value ?? string.Empty;
            }

            return result;
        }
    }
}
