    using System.IO;
using System.Threading.Tasks;

namespace HwGarage.Core.Http
{
    public class FormFile
    {
        public string FieldName { get; init; } = "";
        public string FileName { get; init; } = "";
        public string ContentType { get; init; } = "application/octet-stream";
        public byte[] Content { get; init; } = System.Array.Empty<byte>();
        public long Length => Content.Length;

        public async Task CopyToAsync(Stream target)
        {
            await target.WriteAsync(Content, 0, Content.Length);
        }
    }
}