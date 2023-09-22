
![SmoothBrain Stock Price Crash](https://cdnlearnblog.etmoney.com/wp-content/uploads/2022/09/7-Things-to-do-if-stock-markets-crash_1200x499.png "SmoothBrain Stock Price Crash")

# SmoothBrain - Stock Prices API

SmoothBrain stock prices API using TeaFiles on .NET 7

## Development

Open the `.sln` file with Visual Studio to start a new project solution instance.

Run the `Debug` by pressing the ▶ button to start the development debug server. You can then reach the server at `http://localhost:5200/`

If you run a `Release` server using the ▶ button instead of the debug server, then you can reach the server at `http://localhost:8080/`

### Environment Values

For setting environment variables on MacOS, you need to start the Visual Studio IDE editor from teh CLI terminal and `export` the env values with teh launch command. Example:

```
export INTRINIO_API_KEY={VALUE} && /Applications/Visual\ Studio.app/Contents/MacOS/VisualStudio
```

For setting environment variables on Windows OS follow [these instructions](https://stackoverflow.com/a/73021317/2221024).

## Endpoints

* [POST] `/stock-prices?ticker=AAPL`

> Will create a TeaFile for the stock prices if one does not exist. It will use the Intrinio API by default. See code for all available query string arguments.

* [PATCH] `/stock-prices?ticker=AAPL`

> Will update the TeaFile, if one exists, with newest stock prices since last update. It will use the Intrinio API by default. See code for all available query string arguments.

* [GET] `/stock-prices?ticker=AAPL`

> Will fetch stock prices from the corresponding TeaFile. Data is returned in DESCENDING order by date. See code for all available query string arguments. This will **not** hit any external APIs, use the `POST` or `PATCH` endpoints to get the newest stock prices before calling the `GET` endpoint.

* [GET] `/stock-prices/stream?ticker=AAPL`

> Will fetch stock prices from the corresponding TeaFile and stream the JSON response data back in chunks. This can be useful for large datasets that might begin rendering response data before the complete set. Data is returned in ASCENDING order by date. See code for all available query string arguments. This will **not** hit any external APIs, use the `POST` or `PATCH` endpoints to get the newest stock prices before calling the `GET` endpoint.


* [GET] `/stock-prices/count?ticker=AAPL`

> Will fetch the stock price count from the corresponding TeaFile. See code for all available query string arguments. This will **not** hit any external APIs, use the `POST` or `PATCH` endpoints to get the newest stock prices before calling the `GET` endpoint.

## TeaFiles

All data is stored using TeaFiles for blazing fast lookup times. For more information see the [TeaFiles site](http://discretelogics.com/teafiles/) and [API documentation](http://discretelogics.com/doc/teafiles.net/).
