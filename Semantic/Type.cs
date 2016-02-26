namespace Semantic
{
    public class Type
    {

        public int ByteSize { get; set; }
        public TypeParameters Parameters { get; set; }
        public int HierarchyPosition { get; set; }

        public Type()
        {
            
        }

        public Type(int byteSize, int hierarchyPosition, TypeParameters parameters)
        {
            ByteSize = byteSize;
            HierarchyPosition = hierarchyPosition;
            Parameters = parameters;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is Type == false)
                return false;
            var o = (Type) obj;
            if (ByteSize == o.ByteSize && Parameters == o.Parameters && HierarchyPosition == o.HierarchyPosition)
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return ByteSize ^ (int) Parameters;
        }
    }
}
