using System;
using H.Necessaire;

namespace ApiStockPrices
{
    public class DataEntry : IGuidIdentity
    {
        public Guid ID { get; set; } = Guid.NewGuid();

        public DateTime AsOf { get; set; } = DateTime.UtcNow;
        public string Name { get; set; }
        public string Description { get; set; }
        public Note[] Attributes { get; set; }

        public override string ToString()
        {
            return $"{ID} - {Name} w/ {Attributes?.Length ?? 0} attrs (as of {AsOf.PrintTimeStamp()})";
        }
    }
}

