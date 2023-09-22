using System.Text;

namespace ApiStockPrices
{
    public class StreamProvider
    {
        public static async Task WriteValueToStream(Stream stream, string value)
        {
            byte[] valueAsBytes = Encoding.UTF8.GetBytes(value);
            await stream.WriteAsync(valueAsBytes, 0, valueAsBytes.Length);
            await stream.FlushAsync();
        }
    }
}

