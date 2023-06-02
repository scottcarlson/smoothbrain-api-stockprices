﻿using System;
using System.Collections.Generic;
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
                // Defaults to limited sandbox API Key
                Environment.GetEnvironmentVariable("INTRINIO_API_KEY") ?? "Ojc4MGZkOWQxZTY0ODdhOTUwN2FkYTFmYzI2ZGE0NzA1"
            );
            Configuration.Default.AllowRetries = true;
        }

        public ApiResponseSecurityStockPrices GetStockPrices(string identifier, string frequency)
		{
            ApiResponseSecurityStockPrices result = securityApi.GetSecurityStockPrices(identifier, null, null, frequency, pageSize);

            if (result.NextPage != null)
            {
                result.StockPrices = GetRecursiveBatchStockPrices(identifier, frequency, result.NextPage, result.StockPrices);
            }

            return result;
        }

        public static List<StockPriceSummary> GetRecursiveBatchStockPrices(
            string identifier,
            string frequency,
            string nextPage,
            List<StockPriceSummary> previousBatch
        ) {
            ApiResponseSecurityStockPrices result = securityApi.GetSecurityStockPrices(identifier, null, null, frequency, pageSize, nextPage);

            List<StockPriceSummary> stockPrices = new List<StockPriceSummary>(previousBatch.Count + result.StockPrices.Count);

            stockPrices.AddRange(previousBatch);
            stockPrices.AddRange(result.StockPrices);

            if (result.NextPage != null)
            {
                return GetRecursiveBatchStockPrices(identifier, frequency, result.NextPage, stockPrices);
            }

            return stockPrices;
        }
    }
}
