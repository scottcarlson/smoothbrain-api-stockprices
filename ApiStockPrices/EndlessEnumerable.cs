using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ApiStockPrices
{
    public class EndlessEnumerable<TElement> : IEnumerable<TElement>, IEnumerator<TElement>, IDisposable
    {
        static readonly TimeSpan refreshRate = TimeSpan.FromSeconds(.25);

        readonly Func<TElement> elementFactory;
        public EndlessEnumerable(Func<TElement> elementFactory)
        {
            this.elementFactory = elementFactory ?? throw new ArgumentNullException(nameof(elementFactory));
        }

        public TElement Current => elementFactory();

        public bool MoveNext()
        {
            Thread.Sleep(refreshRate);
            return true;
        }

        public void Reset() { }

        public void Dispose() { }

        object IEnumerator.Current => Current;

        public IEnumerator<TElement> GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => this;
    }
}

