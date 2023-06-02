using System;
using TeaTime;

namespace ApiStockPrices
{
    struct Tick
    {
        public Time Date;

        public decimal O;  // Open
        public decimal H;  // High
        public decimal L;  // Low
        public decimal C;  // Close
        public ulong V;    // Volume
        public decimal AO; // Adjusted Open
        public decimal AH; // Adjusted High
        public decimal AL; // Adjusted Low
        public decimal AC; // Adjusted Close
        public ulong AV;   // Adjusted Volume

        // Split/Dividend Data
        public decimal D;  // Dividend: The dividend amount, if a dividend was paid.
        public decimal SR; // SplitRatio: The ratio of the stock split, if a stock split occurred.
        public decimal F;  // Factor: The factor by which to multiply stock prices before this date, in order to calculate historically-adjusted stock prices.
    }
}

