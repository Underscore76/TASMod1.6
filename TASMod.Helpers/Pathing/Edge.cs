namespace TASMod.Helpers.Pathing
{
    public class Edge
    {
        public int X;
        public int Y;
        public int Length;
        public bool Horizontal;

        public override bool Equals(object obj)
        {
            if (obj is Edge other)
            {
                return X == other.X && Y == other.Y && Length == other.Length && Horizontal == other.Horizontal;
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return X * 1024 * 1024 + Y * 1024 + Length * 10 + (Horizontal ? 1 : 0);
        }
        public override string ToString()
        {
            return $"Edge ({X}, {Y}) Length: {Length} Horizontal: {Horizontal}";
        }

        public bool Overlaps(Edge other)
        {
            if (Horizontal != other.Horizontal)
                return false;
            if (Horizontal)
            {
                if (Y != other.Y)
                    return false;
                return X < other.X + other.Length && X + Length > other.X;
            }
            else
            {
                if (X != other.X)
                    return false;
                return Y < other.Y + other.Length && Y + Length > other.Y;
            }
        }
    }

}