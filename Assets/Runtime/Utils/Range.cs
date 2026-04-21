using System;

namespace SimpleScroll
{
    public readonly struct Range : IEquatable<Range>
    {
        public readonly int Start;
        public readonly int End;
        public static readonly Range Empty = new(-1);

        public int Length { get; }

        public Range(int start = 0, int end = 0)
        {
            if (start > end)
            {
                Start = -1;
                End = 0;
                Length = 0;
            }
            else
            {
                Start = start;
                End = end;
                Length = end - start + 1;
            }
        }

        public override string ToString()
        {
            return $"Range start:{Start} end:{End}";
        }

        public bool Equals(Range other)
        {
            return Start == other.Start && End == other.End;
        }

        public override bool Equals(object obj)
        {
            return obj is Range other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End);
        }
    }
}