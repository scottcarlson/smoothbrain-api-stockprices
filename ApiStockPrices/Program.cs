using Newtonsoft.Json;
using ApiStockPrices;
using H.Necessaire;
using Intrinio.SDK.Model;
using Polly;
using TeaTime;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseResponseCompression();

app.MapPost("/stock-prices", (
    HttpContext context,
    string identifier,
    string? frequency
) =>
{
    string _frequency = frequency ?? "daily";

    var fetch = new IntrinioFetchService();

    ApiResponseSecurityStockPrices result = fetch.GetStockPrices(identifier, _frequency);

    List<StockPriceSummary> stockPrices = result.StockPrices;
    SecuritySummary security = result.Security;

    string ticker = security.Ticker;

    string filename = $"{ticker.ToLower()}.{_frequency}.tea";

    using (var teaFile = File.Exists(filename) ? TeaFile<Tick>.Append(filename) : TeaFile<Tick>.Create(filename))
    {
        foreach (StockPriceSummary stockPrice in stockPrices)
        {
            if (stockPrice.Date != null)
            {
                teaFile.Write(new Tick
                {
                    Date = (Time)(stockPrice.Date),
                    O = stockPrice.Open ?? 0,
                    H = stockPrice.High ?? 0,
                    L = stockPrice.Low ?? 0,
                    C = stockPrice.Close ?? 0,
                    V = (ulong)(stockPrice.Volume ?? 0),

                    AO = stockPrice.AdjOpen ?? 0,
                    AH = stockPrice.AdjHigh ?? 0,
                    AL = stockPrice.AdjLow ?? 0,
                    AC = stockPrice.AdjClose ?? 0,
                    AV = (ulong)(stockPrice.AdjVolume ?? 0),

                    D = stockPrice.Dividend ?? 0,
                    SR = stockPrice.SplitRatio ?? 0,
                    F = stockPrice.Factor ?? 0,
                });
            }
        }
    }

    return "";
});

app.MapGet("/stock-prices", (
    HttpContext context,
    string ticker,
    string? frequency,
    string? from, // Must be a parsable datetime string. This is inclusive; Returns items on or after this date 
    string? to, // Must be a parsable datetime string. This is inclusive; Returns items on or before this date 
    string? fields
) =>
{
    context.Response.ContentType = "application/json";

    string filename = $"{ticker.ToLower()}.{frequency ?? "daily"}.tea";

    if (!File.Exists(filename))
    {
        context.Response.StatusCode = 404;

        return JsonConvert.SerializeObject(new List<ResponseTick>());
    }

    var responseProvider = new ResponseProvider(filename);

    string response;

    try
    {
        response = responseProvider.GetResponse(fields, from, to);
    }
    finally
    {
        // @see https://stackoverflow.com/a/1864586
        responseProvider.Dispose();
    }

    return response;
});

// This endpoint was inspired by the following blog articles:
// @see https://www.loginradius.com/blog/engineering/guest-post/http-streaming-with-nodejs-and-fetch-api/
// @see https://hintea.com/stream-http-response-content-in-asp-net-core-web-api-part-2-infinite-data-stream/
// @see https://hintea.com/stream-http-response-content-in-asp-net-core-webapi/
// @see https://github.com/hinteadan/net-http-stream-playground
app.MapGet("/stock-prices/stream", async (
    HttpContext context,
    string ticker,
    string? frequency,
    string? from, // Must be a parsable datetime string. This is inclusive; Returns items on or after this date 
    string? to, // Must be a parsable datetime string. This is inclusive; Returns items on or before this date 
    string? fields,
    uint? streamAtItemCountGt
) =>
{
    string filename = $"{ticker.ToLower()}.{frequency ?? "daily"}.tea";

    if (!File.Exists(filename))
    {
        context.Response.StatusCode = 404;

        await StreamProvider.WriteValueToStream(context.Response.Body, JsonConvert.SerializeObject(new List<ResponseTick>()));
    }
    else
    {
        uint _streamAtItemCountGt = streamAtItemCountGt ?? 0;

        var responseProvider = new ResponseProvider(filename);

        if (_streamAtItemCountGt < 1 || responseProvider.GetCount() > streamAtItemCountGt)
        {
            context.Response.ContentType = "text/plain; charset=utf-8; x-subtype=json";

            await responseProvider.GetStreamedResponse(context.Response.Body, fields, from, to);
        }
        else
        {
            context.Response.ContentType = "application/json";

            string response = responseProvider.GetResponse(fields, from, to);

            await StreamProvider.WriteValueToStream(context.Response.Body, response);
        }

        // @see https://stackoverflow.com/a/1864586
        responseProvider.Dispose();
    }
});

// This is for testing streaming data
app.MapGet("/data-entries-example", async (HttpContext context, string? t) =>
{
    var streamProvider = new StreamProvider();

    context.Response.ContentType = "text/plain; charset=utf-8; x-subtype=json";
    double? desiredDurationInSeconds = t?.ParseToDoubleOrFallbackTo(null);
    await streamProvider.StreamDataEntriesTo(context.Response.Body, desiredDuration: desiredDurationInSeconds == null ? null : TimeSpan.FromSeconds(desiredDurationInSeconds.Value));
});

app.Run();
