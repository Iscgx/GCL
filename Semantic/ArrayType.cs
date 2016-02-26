using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    public class ArrayType : Type
    {
        public Type BaseType { get; set; }
        public int Dimensions { get; set; }
        public int[] DimensionsWidth { get; set; }

        public ArrayType(Type baseType, int dimensions, int firstDimensionWidth, params int[] extraDimensionsWidth)
        {
            BaseType = baseType;
            Dimensions = dimensions;
            HierarchyPosition = 0;
            ByteSize = extraDimensionsWidth.Aggregate(firstDimensionWidth, (current, i) => current*i) * baseType.ByteSize;
            var dim = new List<int> {firstDimensionWidth};
            if (extraDimensionsWidth != null && extraDimensionsWidth.Length > 0)
                dim.AddRange(extraDimensionsWidth);
            DimensionsWidth = dim.ToArray();
            Parameters = TypeParameters.IsArray;
            
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is ArrayType == false)
                return false;
            return (obj as ArrayType).BaseType.Equals(BaseType);
        }

        public Tuple<int, int> GetCudaValues()
        {
            int blocks = 0, threads = 0;

            const int maxThreads = 512;

            if(Dimensions == 1)
            {
                if (DimensionsWidth[0] <= maxThreads)
                {
                    blocks = 1;
                    threads = DimensionsWidth[0];
                }
                else
                {
                    blocks = DimensionsWidth[0] / maxThreads;
                    threads = maxThreads;

                    if (DimensionsWidth[0] % maxThreads != 0)
                        blocks += 1;
                }
            }
            else if(Dimensions == 2)
            {
                 
            }
            else if(Dimensions == 3)
            {
                
            }

            return new Tuple<int, int>(blocks, threads);
        }

        public override int GetHashCode()
        {
            return BaseType.GetHashCode();
        }
    }
}
