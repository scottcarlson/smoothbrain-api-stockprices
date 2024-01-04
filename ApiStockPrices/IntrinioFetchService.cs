using Intrinio.SDK.Api;
using Intrinio.SDK.Client;
using Intrinio.SDK.Model;

namespace ApiStockPrices
{
	public class IntrinioFetchService
	{
        private static SecurityApi securityApi = new SecurityApi();
        private static int pageSize = 10000; // Max page size

        public IntrinioFetchService()
        {
            Configuration.Default.AddApiKey(
                "api_key",
                // To set the env value in development on MacOS, start VS at the CLI terminal with:
                // `export INTRINIO_API_KEY={VALUE} && /Applications/Visual\ Studio.app/Contents/MacOS/VisualStudio`
                Environment.GetEnvironmentVariable("INTRINIO_API_KEY")
            );
            Configuration.Default.AllowRetries = true;
        }

        public ApiResponseSecurityStockPrices GetStockPrices(string identifier, string frequency, DateTime? startDate = null)
		{
            ApiResponseSecurityStockPrices result = securityApi.GetSecurityStockPrices(identifier, startDate ?? null, null, frequency, pageSize);

            if (result.NextPage != null)
            {
                result.StockPrices = GetRecursiveBatchStockPrices(identifier, frequency, startDate, result.NextPage, result.StockPrices);
            }

            return result;
        }

        public static List<StockPriceSummary> GetRecursiveBatchStockPrices(
            string identifier,
            string frequency,
            DateTime? startDate,
            string nextPage,
            List<StockPriceSummary> previousBatch
        ) {
            ApiResponseSecurityStockPrices result = securityApi.GetSecurityStockPrices(identifier, startDate ?? null, null, frequency, pageSize, nextPage);

            List<StockPriceSummary> stockPrices = new List<StockPriceSummary>(previousBatch.Count + result.StockPrices.Count);

            stockPrices.AddRange(previousBatch);
            stockPrices.AddRange(result.StockPrices);

            if (result.NextPage != null)
            {
                return GetRecursiveBatchStockPrices(identifier, frequency, startDate, result.NextPage, stockPrices);
            }

            return stockPrices;
        }
    }
}
