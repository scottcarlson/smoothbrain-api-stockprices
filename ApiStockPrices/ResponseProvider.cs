using System;
using System.IO;
using Newtonsoft.Json;
using TeaTime;

namespace ApiStockPrices
{
    public struct IncludedResponseFields
    {
        public Boolean IncludeOpen;
        public Boolean IncludeHigh;
        public Boolean IncludeLow;
        public Boolean IncludeClose;
        public Boolean IncludeVolume;
        public Boolean IncludeAdjustedOpen;
        public Boolean IncludeAdjustedHigh;
        public Boolean IncludeAdjustedLow;
        public Boolean IncludeAdjustedClose;
        public Boolean IncludeAdjustedVolume;
        public Boolean IncludeDividend;
    }

    public class ResponseProvider
	{
        private TeaFile<Tick> teaFile;
        private long itemCount;
        public static JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        public static DateTime epochDate = new DateTime(1970, 1, 1);

        public ResponseProvider(string filename)
        {
            teaFile = TeaFile<Tick>.OpenRead(filename);
            itemCount = teaFile.Items.Count;
        }

        public string GetResponse(string? fieldsCsv, string? from = null, string? to = null)
        {
            IncludedResponseFields includedFields = GetIncludedFields(fieldsCsv);

            List<ResponseTick> stockPrices = from == null && to == null
                ? new List<ResponseTick>((int)itemCount)
                : new List<ResponseTick>();

            // This is inclusive. Return items on or after this date
            uint fromTimestamp = from == null ? 0 : GetTimestampFromDateStringJson(from);

            // This is inclusive. Return items on or before this date
            uint toTimestamp = to == null ? 0 : GetTimestampFromDateStringJson(to); ; 

            // Ticks are from newest to oldest
            for (int i = 0; i < itemCount; i++)
            {
                teaFile.SetFilePointerToItem(i);

                Tick tick = teaFile.Read();

                uint timestamp = GetTimestampFromDateStringJson(tick.Date.ToString());

                if (toTimestamp != 0 && timestamp > toTimestamp)
                {
                    continue;
                }

                if (timestamp < fromTimestamp)
                {
                    break;
                }

                stockPrices.Add(new ResponseTick
                {
                    T = timestamp,
                    O = includedFields.IncludeOpen ? tick.O : null,
                    H = includedFields.IncludeHigh ? tick.H : null,
                    L = includedFields.IncludeLow ? tick.L : null,
                    C = includedFields.IncludeClose ? tick.C : null,
                    V = includedFields.IncludeVolume ? tick.V : null,

                    AO = includedFields.IncludeAdjustedOpen ? tick.AO : null,
                    AH = includedFields.IncludeAdjustedHigh ? tick.AH : null,
                    AL = includedFields.IncludeAdjustedLow ? tick.AL : null,
                    AC = includedFields.IncludeAdjustedClose ? tick.AC : null,
                    AV = includedFields.IncludeAdjustedVolume ? tick.AV : null,

                    D = includedFields.IncludeDividend ? tick.D : null,
                });
            }

            return ToJson(stockPrices);
        }

        public async Task GetStreamedResponse(
            Stream stream,
            string? fieldsCsv = null,
            string? from = null, // Must be a parsable datetime string. This is inclusive; Returns items on or after this date 
            string? to = null    // Must be a parsable datetime string. This is inclusive; Returns items on or before this date 
        )
        {
            IncludedResponseFields includedFields = GetIncludedFields(fieldsCsv);

            // This is inclusive. Return items on or after this date
            uint fromTimestamp = from == null ? 0 : GetTimestampFromDateStringJson(from);

            // This is inclusive. Return items on or before this date
            uint toTimestamp = to == null ? 0 : GetTimestampFromDateStringJson(to); ;

            await StreamProvider.WriteValueToStream(stream, $"[{Environment.NewLine}");

            for (int i = 0; i < itemCount; i++)
            {
                teaFile.SetFilePointerToItem(i);

                Tick tick = teaFile.Read();

                uint timestamp = GetTimestampFromDateStringJson(tick.Date.ToString());

                if (toTimestamp != 0 && timestamp > toTimestamp)
                {
                    continue;
                }

                if (timestamp < fromTimestamp)
                {
                    break;
                }

                ResponseTick responseTick = new ResponseTick
                {
                    T = timestamp,
                    O = includedFields.IncludeOpen ? tick.O : null,
                    H = includedFields.IncludeHigh ? tick.H : null,
                    L = includedFields.IncludeLow ? tick.L : null,
                    C = includedFields.IncludeClose ? tick.C : null,
                    V = includedFields.IncludeVolume ? tick.V : null,

                    AO = includedFields.IncludeAdjustedOpen ? tick.AO : null,
                    AH = includedFields.IncludeAdjustedHigh ? tick.AH : null,
                    AL = includedFields.IncludeAdjustedLow ? tick.AL : null,
                    AC = includedFields.IncludeAdjustedClose ? tick.AC : null,
                    AV = includedFields.IncludeAdjustedVolume ? tick.AV : null,

                    D = includedFields.IncludeDividend ? tick.D : null,
                };

                await StreamProvider.WriteValueToStream(stream, $"{ToJson(responseTick)},{Environment.NewLine}");
            }

            await StreamProvider.WriteValueToStream(stream, $"null{Environment.NewLine}]");
        }

        public long GetCount()
        {
            return itemCount;
        }

        public static string ToJson(object value)
        {
            return JsonConvert.SerializeObject(value, (Formatting)0, jsonSerializerSettings);
        }

        public static IncludedResponseFields GetIncludedFields(string? fieldsCsv = null)
        {
            Boolean isFieldsUndefined = fieldsCsv == null;

            List<string> fields = fieldsCsv == null ? new List<string>() : fieldsCsv.Split(",").ToList();

            return new IncludedResponseFields
            {
                IncludeOpen = isFieldsUndefined || fields.Contains("O"),
                IncludeHigh = isFieldsUndefined || fields.Contains("H"),
                IncludeLow = isFieldsUndefined || fields.Contains("L"),
                IncludeClose = isFieldsUndefined || fields.Contains("C"),
                IncludeVolume = isFieldsUndefined || fields.Contains("V"),
                IncludeAdjustedOpen = isFieldsUndefined || fields.Contains("AO"),
                IncludeAdjustedHigh = isFieldsUndefined || fields.Contains("AH"),
                IncludeAdjustedLow = isFieldsUndefined || fields.Contains("AL"),
                IncludeAdjustedClose = isFieldsUndefined || fields.Contains("AC"),
                IncludeAdjustedVolume = isFieldsUndefined || fields.Contains("AV"),
                IncludeDividend = isFieldsUndefined || fields.Contains("D"),
            };
        }

        public static uint GetTimestampFromDateStringJson(string parsableDateString)
        {
            DateTime date = DateTime.Parse(parsableDateString);

            return (uint)date.Subtract(epochDate).TotalSeconds;
        }

        public void Dispose()
        {
            teaFile.Dispose();
        }
    }
}

