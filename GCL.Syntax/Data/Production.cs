using System.Collections.Generic;
using System.Linq;
using System.Text;
using Semantic;

namespace GCL.Syntax.Data
{
    public class Production
    {
        private readonly int hashCode;

        public Symbol Producer { get; }

        public IReadOnlyList<Symbol> Product { get; }

        public Production(Symbol producer, IEnumerable<Symbol> extraProductSymbols)
        {
            if (producer.Type != SymbolType.NonTerminal)
                throw new GrammaticException("Producer cannot be of type Terminal or Epsilon.", producer);

            Producer = producer;
            Product = extraProductSymbols.ToList();

            this.hashCode = Producer.GetHashCode() + Product.Sum(symbol => symbol.GetHashCode()); 
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append($"{Producer} -> ");
            foreach (var symbol in Product)
            {
                builder.Append(symbol);
            }
            return builder.ToString();
        }

        public override int GetHashCode()
        {
            return this.hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj is Production) == false)
                return false;
            var otherProduction = (Production)obj;
            if (otherProduction.Product.Count != Product.Count)
                return false;

            return Product.SequenceEqual(otherProduction.Product);
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