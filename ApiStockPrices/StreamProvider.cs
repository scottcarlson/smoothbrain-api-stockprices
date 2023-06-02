using H.Necessaire.Serialization;
using ApiStockPrices;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TeaTime;
using Intrinio.SDK.Model;
using Newtonsoft.Json;

namespace ApiStockPrices
{
    public class StreamProvider
    {
        static readonly Random random = new Random();
        static readonly TimeSpan defaultDuration = TimeSpan.FromSeconds(30);
        static readonly TimeSpan maxDuration = TimeSpan.FromMinutes(5);

        readonly DataEntryProvider dataEntryProvider = new DataEntryProvider();

        // This is for testing streaming data
        public async Task StreamDataEntriesTo(Stream stream, TimeSpan? desiredDuration = null)
        {
            TimeSpan duration = GetActualDuration(desiredDuration);

            DateTime startedAt = DateTime.UtcNow;

            await WriteValueToStream(stream, $"[{Environment.NewLine}");

            foreach (DataEntry dataEntry in new EndlessEnumerable<DataEntry>(dataEntryProvider.NewRandomEntry))
            {
                if (DateTime.UtcNow > startedAt + duration)
                    break;

                await WriteValueToStream(stream, $"{dataEntry.ToJsonObject()},{Environment.NewLine}");
            }

            await WriteValueToStream(stream, $"null{Environment.NewLine}]");
        }

        public static async Task WriteValueToStream(Stream stream, string value)
        {
            byte[] valueAsBytes = Encoding.UTF8.GetBytes(value);
            await stream.WriteAsync(valueAsBytes, 0, valueAsBytes.Length);
            await stream.FlushAsync();
        }

        static TimeSpan GetActualDuration(TimeSpan? desiredDuration = null)
        {
            TimeSpan duration = desiredDuration ?? defaultDuration;
            duration = duration < TimeSpan.Zero ? -duration : duration;
            duration = duration > maxDuration ? maxDuration : duration;
            return duration;
        }
    }
}

