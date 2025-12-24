using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace HwGarage.Core.Http
{
    public class HttpContext
    {
        public HttpListenerContext RawContext { get; }
        public HttpListenerRequest Request => RawContext.Request;
        public HttpListenerResponse Response => RawContext.Response;

        public object? User { get; set; }
        public Dictionary<string, object> Items { get; } = new();

        public HttpContext(HttpListenerContext context)
        {
            RawContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void WriteResponse(string content, string contentType = "text/html", int statusCode = 200)
        {
            var buffer = Encoding.UTF8.GetBytes(content);
            Response.StatusCode = statusCode;
            Response.ContentType = contentType;
            Response.ContentLength64 = buffer.Length;
            using var output = Response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
        }

        public async Task WriteAsync(string content, string contentType = "text/html", int statusCode = 200)
        {
            var buffer = Encoding.UTF8.GetBytes(content);
            Response.StatusCode = statusCode;
            Response.ContentType = contentType;
            Response.ContentLength64 = buffer.Length;
            await Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            Response.OutputStream.Close();
        }

        public async Task SendFileAsync(string filePath, string contentType = "application/octet-stream")
        {
            byte[] buffer = await File.ReadAllBytesAsync(filePath);
            Response.StatusCode = 200;
            Response.ContentType = contentType;
            Response.ContentLength64 = buffer.Length;
            await Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            Response.OutputStream.Close();
        }

        public async Task<FormCollection> ReadFormAsync()
        {
            var result = new FormCollection();
            var contentType = Request.ContentType ?? string.Empty;

            using var ms = new MemoryStream();
            await Request.InputStream.CopyToAsync(ms);
            var data = ms.ToArray();

            if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var text = Encoding.Latin1.GetString(data);

                var boundaryMatch = Regex.Match(contentType, "boundary=(.+)");
                var boundary = boundaryMatch.Success
                    ? boundaryMatch.Groups[1].Value.Trim('"')
                    : string.Empty;

                if (string.IsNullOrEmpty(boundary))
                    return result;

                var splitBoundary = "--" + boundary;
                var parts = text.Split(splitBoundary, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    if (part.Equals("--\r\n") || part.Equals("--\n"))
                        continue;

                    if (!part.Contains("Content-Disposition", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var headerEnd = part.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    if (headerEnd < 0)
                        continue;

                    var headers = part[..headerEnd];
                    var bodyText = part[(headerEnd + 4)..];
                    
                    bodyText = bodyText.TrimEnd('\r', '\n');

                    var nameMatch = Regex.Match(headers, @"name=""([^""]+)""");
                    var fileNameMatch = Regex.Match(headers, @"filename=""([^""]+)""");
                    var contentTypeMatch = Regex.Match(headers, @"Content-Type:\s*(.+)");

                    var name = nameMatch.Success ? nameMatch.Groups[1].Value : string.Empty;
                    var filename = fileNameMatch.Success ? fileNameMatch.Groups[1].Value : string.Empty;
                    var contentTypeHeader = contentTypeMatch.Success
                        ? contentTypeMatch.Groups[1].Value.Trim()
                        : string.Empty;

                    if (string.IsNullOrEmpty(name))
                        continue;

                    if (!string.IsNullOrEmpty(filename))
                    {
                        var fileBytes = Encoding.Latin1.GetBytes(bodyText);

                        result.Files[name] = new FormFile
                        {
                            FieldName = name,
                            FileName = filename,
                            ContentType = string.IsNullOrEmpty(contentTypeHeader)
                                ? "application/octet-stream"
                                : contentTypeHeader,
                            Content = fileBytes
                        };
                    }
                    else
                    {
                        var value = bodyText.TrimEnd('\r', '\n');
                        result.Fields[name] = value;
                    }
                }
            }
            else
            {
                var body = Encoding.UTF8.GetString(data);
                var parsed = HttpUtility.ParseQueryString(body);
                foreach (string? key in parsed.AllKeys)
                {
                    if (key != null)
                    {
                        result.Fields[key] = parsed[key] ?? string.Empty;
                    }
                }
            }

            return result;
        }

        public void SetCookie(string name, string value, bool httpOnly = false)
        {
            var cookie = new Cookie(name, value)
            {
                HttpOnly = httpOnly,
                Path = "/"
            };
            Response.AppendCookie(cookie);
        }

        public void DeleteCookie(string name)
        {
            var cookie = new Cookie(name, "")
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                Path = "/"
            };
            Response.AppendCookie(cookie);
        }

        public void Redirect(string url) => Response.Redirect(url);
    }
}
