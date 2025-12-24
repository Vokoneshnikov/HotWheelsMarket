using System;

namespace HwGarage.Core.Http
{
    public class HttpNotFoundException : Exception
    {
        public HttpNotFoundException(string message) : base(message) { }
    }
}