using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TASMod.Minigames
{
    public class AStar<T>
    {
        public class Location
        {
            public T state;
            public double F => G + H;
            public double G;
            public double H;
            public Location Parent;
        }

        public Func<T, IEnumerable<T>> GetNeighbors = null;
        public Func<T, T, double> DistanceStep;
        public Func<T, T, double> DistanceHeuristic;
        public Func<T, T, bool> EqualityFunction;

        public AStar(
            Func<T, IEnumerable<T>> neighbors,
            Func<T, T, double> distStep,
            Func<T, T, double> distHeuristic,
            Func<T, T, bool> equalityFunction = null
        )
        {
            GetNeighbors = neighbors;
            DistanceStep = distStep;
            DistanceHeuristic = distHeuristic;
            EqualityFunction = equalityFunction ?? ((a, b) => a.Equals(b));
        }

        public List<T> Search(T _start, T _end, out double cost, int max_evals = -1)
        {
            cost = 0;
            Location current = null;
            Location start = new Location() { state = _start };
            Location target = new Location() { state = _end };

            var priorityQueue = new PriorityQueue<Location, double>();
            var closedList = new List<Location>();

            priorityQueue.Enqueue(start, 0);

            int n_evals = 0;
            while (priorityQueue.Count > 0)
            {
                n_evals++;
                if (max_evals > 0 && n_evals > max_evals)
                    return null;

                current = priorityQueue.Dequeue();
                closedList.Add(current);

                // reached the target state
                if (EqualityFunction(current.state, target.state))
                    break;

                var neighbors = BuildNeighbors(current, target);
                foreach (var neighbor in neighbors)
                {
                    // we've already closed this node
                    if (
                        closedList.FirstOrDefault(l => EqualityFunction(l.state, neighbor.state))
                        != null
                    )
                        continue;

                    // already selected?
                    var priorNode = priorityQueue
                        .UnorderedItems.Select(l => l.Element)
                        .FirstOrDefault(l => EqualityFunction(l.state, neighbor.state));
                    if (priorNode == null)
                    {
                        neighbor.Parent = current;
                        priorityQueue.Enqueue(neighbor, neighbor.F);
                    }
                    else
                    {
                        // test if using current G scores makes the adjacent squares F score lower
                        if (neighbor.G < priorNode.G)
                        {
                            priorNode.G = neighbor.G;
                            priorNode.Parent = current;
                        }
                    }
                }
            }

            // didn't find a solution
            if (current == null)
                return null;
            if (!EqualityFunction(current.state, target.state))
                return null;

            cost = current.G;
            var solution = new List<T>();
            while (current != null)
            {
                solution.Add(current.state);
                current = current.Parent;
            }
            solution.Reverse();
            return solution;
        }

        private List<Location> BuildNeighbors(Location current, Location target)
        {
            List<Location> locs = new List<Location>();
            T state = current.state;
            foreach (T neighbor in GetNeighbors(state))
            {
                Location newNode = new Location() { state = neighbor };
                newNode.G = DistanceStep(current.state, newNode.state) + current.G;
                if (double.IsNaN(newNode.G))
                    continue;
                newNode.H = DistanceHeuristic(newNode.state, target.state);
                locs.Add(newNode);
            }
            return locs;
        }
    }
}
