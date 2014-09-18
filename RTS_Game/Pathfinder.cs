using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace RTS_Game
{
    /// <summary>
    /// Pathfinder class. Finds routes give a passability map for the grid.
    /// </summary>
    public class Pathfinder
    {
        private BitArray[] _passabilityMap;
        public BitArray[] PassabilityMap
        {
            get
            {
                return _passabilityMap;
            }
        }

        #region Pathfinder

        /// <summary>
        /// Heuristic function for the A* pathfinder
        /// </summary> 
        public delegate float HeuristicFunction(Coords here, Coords end);

        private delegate float hFunction(Coords here);

        /// <summary>
        /// Node struct for the A* pathfinder.
        /// </summary>
        private struct NodeAStar : IComparable
        {
            public Direction connection;
            public float costSoFar;
            public float estimatedTotalCost;

            public Int32 CompareTo(object obj)
            {
                if (!(obj is NodeAStar))
                {
                    throw new Exception("Bad NodeAStar comparison.");
                }

                NodeAStar compareWithMe = (NodeAStar)obj;
                Int32 returnValue = 0;
                float difference = this.estimatedTotalCost - compareWithMe.estimatedTotalCost;
                if (difference > 0)
                {
                    returnValue = 1;
                }
                else if (difference < 0)
                {
                    returnValue = -1;
                }

                return returnValue;
            }

            public NodeAStar(Direction d, float cost, float estimate)
            {
                this.connection = d;
                this.costSoFar = cost;
                this.estimatedTotalCost = estimate;
            }
        }

        private List<Direction> _PathfinderAStar(Coords start, Coords endTopLeft, Coords endBottomRight, BitArray[] _passabilityMap, hFunction h)
        {
            // NOTE: Should later implemented a collision predictor mechanic to work in tandem
            // with the path-finder to provide better agent behavior.
            // NOTE: Consider returning the number of tiles scanned in case no path is found.
            // This will alert a boxed-in creature of its predicament.
            // NOTE: Introduce a flag for a straight-line initial check(for outdoors environmens and
            // for when the goal is near).

            Int32 rangeX = _passabilityMap.Length;
            Int32 rangeY = _passabilityMap[0].Count;

            NodeAStar?[,] nodeArray = new NodeAStar?[rangeX, rangeY];

            NodeAStar startNode = new NodeAStar();
            startNode.costSoFar = 0;
            startNode.estimatedTotalCost = h(start);

            nodeArray[start.X, start.Y] = startNode;

            List<Coords> ListOpen = new List<Coords>();
            ListOpen.Add(start);
            while (ListOpen.Count > 0)
            {
                // I have to use this bool the way I've implemented the algo. Consider rewriting.
                bool resortList = false;

                Coords currentCoords = ListOpen.First();
                // Check to see if goal is reached.
                //if (currentCoords.Equals(endTopLeft))
                if (StaticMathFunctions.CoordinateIsInBox(currentCoords, endTopLeft, endBottomRight))
                {
                    break;
                }

                NodeAStar currentNode = nodeArray[currentCoords.X, currentCoords.Y].Value;
                for (byte i = 0; i <= 3; ++i)
                {
                    Direction currentDir = (Direction)(2 * i + 1);
                    Coords dirCoords = StaticMathFunctions.DirectionToCoords(currentDir);
                    Coords potential = currentCoords + dirCoords;
                    // check if move in dir is allowed
                    if (potential.X >= 0 && potential.X < rangeX && potential.Y >= 0 && potential.Y < rangeY // bounds check
                        && _passabilityMap[potential.X][potential.Y]) // passability check
                    {
                        // Using the simplest cost function possible. Can be easily updated
                        // once tile walkability coefficients are added.
                        Coords newNodePosition = new Coords(CoordsType.General, currentCoords.X + dirCoords.X, currentCoords.Y + dirCoords.Y);
                        float accruedCost = currentNode.costSoFar + Constants.MovementCost[(byte)currentDir];

                        // Straight line correction
                        if (currentDir == nodeArray[currentCoords.X, currentCoords.Y].Value.connection)
                        {
                            accruedCost -= Constants.PathfinderStraightPathCorrection;
                        }

                        // Check to see if the node under examination is in the closed list.
                        //NodeAStar? oldNode = nodeArray[newNodePosition.X, newNodePosition.Y];
                        if (nodeArray[newNodePosition.X, newNodePosition.Y] != null)
                        {
                            // If node is in closed list, see if it needs updating.
                            if (nodeArray[newNodePosition.X, newNodePosition.Y].Value.costSoFar > accruedCost)
                            {
                                float expectedAdditionalCost =
                                    nodeArray[newNodePosition.X, newNodePosition.Y].Value.estimatedTotalCost -
                                    nodeArray[newNodePosition.X, newNodePosition.Y].Value.costSoFar;
                                NodeAStar nodeToAdd =
                                    new NodeAStar(currentDir, accruedCost, accruedCost + expectedAdditionalCost);
                                nodeArray[newNodePosition.X, newNodePosition.Y] = nodeToAdd;
                                ListOpen.Add(newNodePosition);
                                resortList = true;
                            }
                        }
                        // Node is in open list. Process it.
                        else
                        {
                            float expectedAdditionalCost = h(newNodePosition);
                            NodeAStar nodeToAdd =
                                new NodeAStar(currentDir, accruedCost, accruedCost + expectedAdditionalCost);
                            nodeArray[newNodePosition.X, newNodePosition.Y] = nodeToAdd;
                            ListOpen.Add(newNodePosition);
                            resortList = true;
                        }
                    }
                }

                ListOpen.RemoveAt(0);
                if (resortList)
                {
                    ListOpen.Sort(
                        delegate(Coords c1, Coords c2)
                        {
                            float difference = nodeArray[c1.X, c1.Y].Value.estimatedTotalCost -
                                nodeArray[c2.X, c2.Y].Value.estimatedTotalCost;

                            Int32 returnValue = 0;
                            if (difference > 0)
                            {
                                returnValue = 1;
                            }
                            else if (difference < 0)
                            {
                                returnValue = -1;
                            }
                            return returnValue;
                        }
                    );
                }
            }

            List<Direction> ListRoute = new List<Direction>();

            // Return empty route if the open list is empty, i.e. there is no path to the target
            // Ideally, the game logic should be fixed so that the search isn't even attempted
            // if there is no path between the two points.
            if (ListOpen.Count == 0)
            {
                return ListRoute;
            }

            Coords trackbackCoords = endTopLeft;
            while (trackbackCoords != start)
            {
                Direction newDirection = nodeArray[trackbackCoords.X, trackbackCoords.Y].Value.connection;
                ListRoute.Add(newDirection);
                trackbackCoords = StaticMathFunctions.CoordsNeighboringInDirection(new Coords(CoordsType.Tile, trackbackCoords),
                    StaticMathFunctions.OppositeDirection(newDirection));
            }

            // Might be faster without reversing
            //ListRoute.Reverse();

            // We skip the reversal, so pick directions from the END of the list.
            return ListRoute;
        }

        /// <summary>
        /// Tile-level (coarse) A* pathfinding.
        /// </summary>
        /// <param name="start"> Start Coords </param>
        /// <param name="endTopLeft"> Goal-box TopLeft Coords </param>
        /// <param name="endBottomRight"> Goal-box BottomRight Coords </param>
        /// <param name="h"> Heuristic function </param>
        /// <returns> Route to goal, as a list of Directions </returns>
        public List<Direction> PathfinderAStarCoarse(Coords start, Coords endTopLeft, Coords endBottomRight, HeuristicFunction h)
        {
            return this._PathfinderAStar(new Coords(CoordsType.General, start), new Coords(CoordsType.General, endTopLeft), new Coords(CoordsType.General, endBottomRight), this._passabilityMap,
                delegate(Coords c) { return h(c, StaticMathFunctions.CoordsAverage(endTopLeft, endBottomRight)); });
        }

        /// <summary>
        /// Tile-level (coarse) A* pathfinding.
        /// </summary>
        /// <param name="start"> Start Coords </param>
        /// <param name="end"> Target tile Coords </param>
        /// <param name="h"> Heuristic function </param>
        /// <returns> Route to goal, as a list of Directions </returns>
        public List<Direction> PathfinderAStarCoarse(Coords start, Coords end, HeuristicFunction h)
        {
            return this.PathfinderAStarCoarse(start, end, end, h);
        }

        #endregion


        public Pathfinder(BitArray[] passMap)
        {
            _passabilityMap = passMap;
        }
    }
}
