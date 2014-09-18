using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTS_Game
{
    /// <summary>
    /// Generates an influence map from a given source, using a given spread function
    /// There is no 'strength' parameter - let the objects associated with the influence map
    /// have their own parameter for this. Influence is 1 at the source and decreasing as one moves away.
    /// </summary>
    public class InfluenceSourceMap
    {
        #region Properties
        // Map reference. Perhaps should be 'get' only?
        private Map _currentMap;
        public Map CurrentMap
        {
            get
            {
                return this._currentMap;
            }
            set
            {
                this._currentMap = value;
            }
        }

        // Coords of source
        private Coords _source;
        public Coords Source
        {
            get
            {
                return this._source;
            }
            set
            {
                this._source = value;
            }
        }

        // The map itself. 2D array of floats, corresponding to the main TileMap
        private float[,] _influenceMap;
        public float[,] InfluenceMap
        {
            get
            {
                return this._influenceMap;
            }

            // who gets responsibility for copying? Perhaps 'set' should be banned.
            set
            {
                this._influenceMap = value;
            }
        }

        #endregion

        #region Methods and class logic
        // Self-explanatory
        public float GetMapValue(Coords number)
        {
            // Perhaps should throw exception
            if (!(_currentMap.MyCollider.CheckInBounds(number)))
                return 0;

            return this._influenceMap[number.X, number.Y];
        }

        // Self-explanatory
        public void SetMapValue(Coords number, float newValue)
        {
            // Perhaps should throw exception
            if (!(_currentMap.MyCollider.CheckInBounds(number)))
                return;

            this._influenceMap[number.X, number.Y] = newValue;
        }

        /// <summary>
        /// Calculates influence 'distance' units from the source. Should be a [0,1] 
        /// monotonic decreasing function. Perhaps should provide some default, in
        /// case the constructor fails to initialize one.
        /// </summary>
        public delegate float InfluenceSpreadFunction(UInt32 distance);
        private InfluenceSpreadFunction _f;

        /// Generates the influence map.
        /// Uses a silly recursive algorithm.
        /// Stopping conditions: Let's use two, to avoid stupid infinite loops.
        /// One is a distance threshold check.
        // Second is a min influence threshold check.

        /// <summary>
        /// Generates the influence map.
        /// Uses a silly recursive algorithm.
        /// Stopping conditions: Let's use two, to avoid stupid infinite loops.
        /// One is a distance threshold check.
        /// Second is a min influence threshold check.
        /// </summary>
        public void GenerateInfluenceMap()
        {
            // boolean array to keep note of which tiles have been processed
            //BitArray[,] takenCareOf = new BitArray[_currentMap.BoundX, _currentMap.BoundY];
            BitArray[] takenCareOf = new BitArray[_currentMap.BoundX];
            for (int i = 0; i < _currentMap.BoundX; ++i)
            {
                takenCareOf[i] = new BitArray(_currentMap.BoundY);
            }
            takenCareOf[Source.X][Source.Y] = true;

            // sets up two queues - one for the current pass, one for the next one
            // distance increments by one at each pass
            // if too slow, the process should be broken up so it does a number of passes each tick
            Queue<Coords> currentQueue = new Queue<Coords>();
            Queue<Coords> nextQueue = new Queue<Coords>();

            currentQueue.Enqueue(_source);

            UInt32 currentDistance = 0;

            // main loop
            // Stopping conditions: the two queues are exhausted, OR InfluenceMapMaxDistance is reached
            while
                (
                ((currentQueue.Count > 0) & (nextQueue.Count > 0))
                |
                (currentDistance < Constants.InfluenceMapMaxDistance)
                )
            {
                // Checks if it's time to start the next pass
                if (currentQueue.Count == 0)
                {
                    currentQueue = nextQueue;
                    nextQueue = new Queue<Coords>();
                    currentDistance++;
                    continue;
                }

                Coords currentCoords = currentQueue.Peek();
                TilePassable currentTile = (TilePassable)CurrentMap.GetTile(currentCoords);

                // Analyzes the neighbors of the current Tile for possible additions to nextQueue
                for (byte i = 1; i <= 8; i++)
                {
                    Direction currentDir = (Direction)i;
                    if (currentTile.AllowedMovesCheckInDirection(currentDir))
                    {
                        Coords toCheck = StaticMathFunctions.CoordsNeighboringInDirection(currentCoords, currentDir);
                        if (!takenCareOf[toCheck.X][toCheck.Y])
                        {
                            nextQueue.Enqueue(toCheck);
                            takenCareOf[toCheck.X][toCheck.Y] = true;
                        }
                    }
                }

                float newVal = _f(currentDistance);

                // Check to avert infnite / excessively deep loop
                if (newVal > Constants.InfluenceMapMinThreshold)
                {
                    this.SetMapValue(currentCoords, newVal);
                }

                currentQueue.Dequeue();
            }
        }


        /// <summary>
        /// Returns list of possible moves, sorted by
        /// 1) amount of increase, 2) distance to influence map source
        /// THIS METHOD SHOULD BE IMPROVED
        /// </summary>
        public List<Direction> PossibleMoves(Coords currentPosition)
        {
            List<Direction> dirList = new List<Direction>();

            // Dangerous cast?
            TilePassable currentTile = (TilePassable)this._currentMap.GetTile(currentPosition);
            for (byte i = 1; i <= 8; i++)
            {
                Direction currentDir = (Direction)i;
                if (currentTile.AllowedMovesCheckInDirection(currentDir))
                {
                    dirList.Add(currentDir);
                }
            }

            dirList.Sort(
                delegate(Direction d1, Direction d2)
                {
                    Coords c1 = StaticMathFunctions.CoordsNeighboringInDirection(currentPosition, d1);
                    Coords c2 = StaticMathFunctions.CoordsNeighboringInDirection(currentPosition, d2);

                    Int32 returnVal = (this._influenceMap[c1.X, c1.Y]).CompareTo(this._influenceMap[c2.X, c2.Y]);

                    if (returnVal == 0)
                    {
                        returnVal = (StaticMathFunctions.DistanceBetweenTwoCoordsEucledean(c1, currentPosition)).CompareTo
                            (StaticMathFunctions.DistanceBetweenTwoCoordsEucledean(c2, currentPosition));
                    }

                    return returnVal;
                }
            );

            return dirList;
        }

        #endregion

        #region Constructors
        // Constructor. Map and source are necessarily passed. The influence function can be made to  
        // have a default value.
        public InfluenceSourceMap(Map currentMap, Coords source, InfluenceSpreadFunction f)
        {
            this._currentMap = currentMap;
            this._source = source;
            this._f = f;

            // zero our the floats
            this._influenceMap = new float[_currentMap.BoundX, _currentMap.BoundY];
        }

        #endregion
    }
}
