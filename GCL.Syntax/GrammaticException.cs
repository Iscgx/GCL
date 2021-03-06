﻿using System;
using System.Collections.Generic;
using Semantic;

namespace GCL.Syntax
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
