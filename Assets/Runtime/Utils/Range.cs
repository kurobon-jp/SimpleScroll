namespace SimpleScroll
{
    public struct Range
    {
        public readonly int Start;
        public readonly int End;

        public Range(int start = 0, int end = 0)
        {
            Start = start;
            End = end;
        }

        public override string ToString()
        {
            return $"Range start:{Start} end:{End}";
        }
    }
}