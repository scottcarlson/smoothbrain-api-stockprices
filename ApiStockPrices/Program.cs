using Newtonsoft.Json;
using ApiStockPrices;
using Intrinio.SDK.Model;
using TeaTime;
using System.IO;
using System.Formats.Tar;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.
// app.UseHttpsRedirection(); // This may be required for actual server builds, but not for Docker localhost
app.UseResponseCompression();

// Endpoints
app.MapPost("/stock-prices", (
    HttpContext context,
    string ticker, // Casing is sensitive. Must be uppercase if using a ticker as identfier
    string? provider,
    string? frequency
) =>
{
    string _frequency = frequency ?? "daily";

    string filepath = ResponseProvider.GetFilePath(provider ?? "intrinio", ticker, _frequency);

    if (File.Exists(filepath))
    {
        context.Response.StatusCode = 400;

        return "File already exists. Use PATCH request to update.";
    }

    var fetch = new IntrinioFetchService();

    ApiResponseSecurityStockPrices result = fetch.GetStockPrices(ticker, _frequency);

    List<StockPriceSummary> stockPrices = result.StockPrices;

    int iterationCount = stockPrices.Count() - 1;

    if (iterationCount < 0)
    {
        return "";
    }

    var postPatchProvider = new PostPatchProvider(filepath);

    try
    {
        postPatchProvider.WriteStockPrices(stockPrices);
    }
    finally
    {
        // @see https://stackoverflow.com/a/1864586
        postPatchProvider.Dispose();
    }

    context.Response.StatusCode = 201;

    return "";
});

app.MapPatch("/stock-prices", (
    HttpContext context,
    string ticker, // Casing is sensitive. Must be uppercase if using a ticker as identfier
    string? provider,
    string? frequency
) =>
{
    string _frequency = frequency ?? "daily";

    string filepath = ResponseProvider.GetFilePath(provider ?? "intrinio", ticker, _frequency);

    if (!File.Exists(filepath))
    {
        context.Response.StatusCode = 404;

        return "File does not exists. Use POST request to create initial stock price file.";
    }

    var responseProvider = new ResponseProvider(filepath);
    string responseJson;

    try
    {
        responseJson = responseProvider.GetResponse(null, null, null, 1);
    }
    finally
    {
        // @see https://stackoverflow.com/a/1864586
        responseProvider.Dispose();
    }

    List<TickResponse>? response = JsonConvert.DeserializeObject<List<TickResponse>>(responseJson);

    uint latestTimestamp = response.First().T;

    uint oneDayInSeconds = 60 * 60 * 24;
    DateTime startDate = DateTimeOffset.FromUnixTimeSeconds(latestTimestamp + oneDayInSeconds).UtcDateTime;

    var fetch = new IntrinioFetchService();

    ApiResponseSecurityStockPrices result = fetch.GetStockPrices(ticker, _frequency, startDate);

    List<StockPriceSummary> stockPrices = result.StockPrices;

    int iterationCount = stockPrices.Count() - 1;

    if (iterationCount < 0)
    {
        context.Response.StatusCode = 200;

        return "";
    }

    var postPatchProvider = new PostPatchProvider(filepath);

    try
    {
        postPatchProvider.WriteStockPrices(stockPrices);
    }
    finally
    {
        // @see https://stackoverflow.com/a/1864586
        postPatchProvider.Dispose();
    }

    context.Response.StatusCode = 201;

    return "";
});

// Returns stock prices in DESCENDING order based on timestamp
app.MapGet("/stock-prices", (
    HttpContext context,
    string ticker,
    string? provider,
    string? frequency,
    string? from, // Must be a parsable datetime string. This is inclusive; Returns items on or after this date
    string? to, // Must be a parsable datetime string. This is inclusive; Returns items on or before this date
    uint? limit,
    string? fields
) =>
{
    context.Response.ContentType = "application/json";

    string filepath = ResponseProvider.GetFilePath(provider ?? "intrinio", ticker, frequency);

    if (!File.Exists(filepath))
    {
        context.Response.StatusCode = 404;

        return JsonConvert.SerializeObject(new List<TickResponse>());
    }

    var responseProvider = new ResponseProvider(filepath);

    string response;

    try
    {
        response = responseProvider.GetResponse(fields, from, to, limit);
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
//
// IMPORTANT: Do not provide `AcceptEncoding` header when making streaming request as
// it will greatly increase response times due to each streamed chunk getting encoded.
//
// IMPORTANT: Returns stock prices in ASCENDING order based on timestamp
app.MapGet("/stock-prices/stream", async (
    HttpContext context,
    string ticker,
    string? provider,
    string? frequency,
    string? from, // Must be a parsable datetime string. This is inclusive; Returns items on or after this date
    string? to, // Must be a parsable datetime string. This is inclusive; Returns items on or before this date
    string? fields,
    uint? limit,
    uint? streamAtItemCountGt
) =>
{
    string filepath = ResponseProvider.GetFilePath(provider ?? "intrinio", ticker, frequency);

    if (!File.Exists(filepath))
    {
        context.Response.StatusCode = 404;

        await StreamProvider.WriteValueToStream(context.Response.Body, JsonConvert.SerializeObject(new List<TickResponse>()));
    }
    else
    {
        uint _streamAtItemCountGt = streamAtItemCountGt ?? 0;

        var responseProvider = new ResponseProvider(filepath);

        if (_streamAtItemCountGt < 1 || responseProvider.GetCount() > streamAtItemCountGt)
        {
            context.Response.ContentType = "text/plain; charset=utf-8; x-subtype=json";

            await responseProvider.GetStreamedResponse(context.Response.Body, fields, from, to, limit);
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

app.MapGet("/stock-prices/count", (
    HttpContext context,
    string ticker,
    string? provider,
    string? frequency
) =>
{
    context.Response.ContentType = "application/json";

    string filepath = ResponseProvider.GetFilePath(provider ?? "intrinio", ticker, frequency);

    if (!File.Exists(filepath))
    {
        context.Response.StatusCode = 404;

        return JsonConvert.SerializeObject(new CountResponse {});
    }

    var responseProvider = new ResponseProvider(filepath);

    string response;

    try
    {
        response = ResponseProvider.ToJson(new CountResponse {
            Count = responseProvider.GetCount(),
        });
    }
    finally
    {
        // @see https://stackoverflow.com/a/1864586
        responseProvider.Dispose();
    }

    return response;
});

app.Run();
