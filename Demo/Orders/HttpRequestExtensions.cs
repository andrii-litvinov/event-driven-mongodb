using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

        public static async Task Write<T>(this HttpResponse response, T value)
        {
            var serializer = new JsonSerializer {ContractResolver = new CamelCasePropertyNamesContractResolver()};

            using (var stream = new MemoryStream())
            {
                using (var writer = new JsonTextWriter(new StreamWriter(stream)) {Formatting = Formatting.Indented})
                {
                    serializer.Serialize(writer, value);
                }

                var bytes = stream.ToArray();
                await response.Body.WriteAsync(bytes, 0, bytes.Length);
            }
        }
    }
}