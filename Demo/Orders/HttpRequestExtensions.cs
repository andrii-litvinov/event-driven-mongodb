using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Orders
{
    public static class HttpRequestExtensions
    {
        public static async Task<T> ReadAs<T>(this HttpRequest request)
        {
            using (var stream = new MemoryStream())
            {
                await request.Body.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                
                var serializer = new JsonSerializer();
                using (var reader = new StreamReader(stream))
                    return (T) serializer.Deserialize(reader, typeof(T));
            }
        }
    }
}