using System;
using System.Text;

namespace gcl2
{

    /// <summary>
    /// Represents both a production and a current read index.
    /// </summary>
    public class Element
    {
        public Production Production { get; private set; }
        public int ReadIndex { get; private set; }

        /// <summary>
        /// Returns the symbol being read by the read index.
        /// </summary>
        public Symbol ReadSymbol
        {
            get { return Production.Product[ReadIndex]; }
        }

        /// <summary>
        /// Returns true if the product reading is completed.
        /// </summary>
        public bool ReadCompleted
        {
            get { return ReadIndex == Production.Product.Count; }
        }

        /// <summary>
        /// Creates an element with the default read index of 0.
        /// </summary>
        /// <param name="production">Production to base this element on.</param>
        public Element(Production production) : this(production, 0)
        {
            
        }

        /// <summary>
        /// Returns true if the product reading is completed.
        /// </summary>
        /// <param name="production">Production to base this element on.</param>
        /// <param name="readIndex">Specific reading index.</param>
        public Element(Production production, int readIndex)
        {
            if (production == null)
                throw new ArgumentNullException();
            Production = production;
            ReadIndex = readIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Element Read()
        {
            if (ReadCompleted == false)
                return new Element(Production, ReadIndex + 1);
            return new Element(Production, 0);
        }

        public override int GetHashCode()
        {
            var code = ReadIndex;
            if (Production != null)
                code += Production.GetHashCode();
            return code;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || (obj is Element) == false)
                return false;
            var otherElement = (obj as Element);
            if (Production == null || otherElement.Production == null)
                return false;
            return ReadIndex == otherElement.ReadIndex && Production == otherElement.Production;
        }

        public override string ToString()
        {
            var prod = Production.ToString();
            var left = new StringBuilder();
            var right = new StringBuilder();
            for (var i = 0; i < Production.Product.Count; i++)
            {
                if (i < ReadIndex)
                    left.Append(Production.Product[i]);
                else
                    right.Append(Production.Product[i]);
            }


            return string.Format("{0} -> {1}●{2}", Production.Producer, left, right); ;
        }

        public static bool operator ==(Element e1, Element e2)
        {
            if ((object)e1 == null || (object)e2 == null)
                return false;
            return e1.Equals(e2);
        }

        public static bool operator !=(Element e1, Element e2)
        {
            return !(e1 == e2);
        }
    }
}
