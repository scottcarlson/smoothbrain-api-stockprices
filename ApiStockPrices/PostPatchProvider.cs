using Intrinio.SDK.Model;
using TeaTime;

namespace ApiStockPrices
{
    public class PostPatchProvider
    {
        private TeaFile<Tick> teaFile;

        public PostPatchProvider(string filepath)
        {
            teaFile = File.Exists(filepath) ? TeaFile<Tick>.Append(filepath) : TeaFile<Tick>.Create(filepath);
        }

        public void WriteStockPrices(List<StockPriceSummary> stockPrices)
        {
            int iterationCount = stockPrices.Count() - 1;

            for (int i = iterationCount; i >= 0; i--)
            {
                StockPriceSummary stockPrice = stockPrices[i];

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

        public void Dispose()
        {
            teaFile.Dispose();
        }
    }
}

