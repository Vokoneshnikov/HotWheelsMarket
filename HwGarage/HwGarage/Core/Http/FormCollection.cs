using System.Collections.Generic;

namespace HwGarage.Core.Http
{
    public class FormCollection
    {
        public Dictionary<string, string> Fields { get; } = new();
        public Dictionary<string, FormFile> Files { get; } = new();

        public string this[string key] => Fields.TryGetValue(key, out var v) ? v : "";
    }
}