using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Semantic;

namespace Syntax
{
    [Serializable]
    public class GrammaticException : SintacticParserException
    {
        public Symbol Producer { get; private set; }
        public List<LinkedList<Symbol>> Productions { get; private set; }

        public GrammaticException(string message, Symbol producer)
            : this(message, producer, new LinkedList<Symbol>[] { })
        {

        }

        public GrammaticException(string message, Symbol producer, IEnumerable<LinkedList<Symbol>> productions)
            : base(message)
        {
            Producer = producer;
            Productions = new List<LinkedList<Symbol>>(productions);
        }
    }
}
