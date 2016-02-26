using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gcl2
{
    public class Production
    {
        private readonly Symbol _producer;
        private readonly List<Symbol> _product;
        private readonly HashSet<Symbol> _productSet;

        public Symbol Producer
        {
            get { return _producer; }
        }

        public List<Symbol> Product
        {
            get { return _product; }
        }

        public Production(Symbol producer, params Symbol[] extraProductSymbols)
        {
            if (producer.Type != SymbolType.NonTerminal)
                throw new GrammaticException("Producer cannot be of type Terminal or Epsilon.", producer);
            _producer = producer;
            _product = new List<Symbol>(extraProductSymbols);
            //foreach (var extraProductSymbol in extraProductSymbols)
            //    _product.Add(extraProductSymbol);
            _productSet = new HashSet<Symbol>(_product);
        }

        public Production(Symbol producer, IEnumerable<Symbol> productionSymbols) : this(producer, productionSymbols.ToArray())
        {
            
        }

        public bool Produces(Symbol symbol)
        {
            return _productSet.Contains(symbol);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(string.Format("{0} -> ", _producer));
            foreach (var symbol in _product)
            {
                builder.Append(symbol);
            }
            return builder.ToString();
        }

        public override int GetHashCode()
        {
            var code = Producer.GetHashCode() + Product.Sum(symbol => symbol.GetHashCode());
            return code;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj is Production) == false)
                return false;
            var otherProduction = (Production)obj;
            var otherPointer = otherProduction.Product[0];
            if (otherProduction.Product.Count != Product.Count)
                return false;
            for (var i = 0; i < _product.Count; i++)
            {
                if (_product[i] != otherProduction._product[i])
                    return false;
            }

            return true;
        }

        public static bool operator ==(Production p1, Production p2)
        {
            if ((object) p1 == null || (object) p2 == null)
                return false;
            return p1.Equals(p2);
        }

        public static bool operator !=(Production p1, Production p2)
        {
            return !(p1 == p2);
        }
    }
}