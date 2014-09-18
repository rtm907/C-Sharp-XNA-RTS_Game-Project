using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections;

namespace RTS_Game
{
    /// <summary>
    /// Map class. The map is a double array of Tiles.
    /// Contains the Pathfinding algos, such as A* and Bresenheim lines.
    /// Contains Checking methods for tile visibility/ walkability/ in-bounds.
    /// </summary>
    public class Map
    {
        #region Properties

        // Map dimensions
        private UInt16 _xMax;
        /// <summary>
        /// The X bound of this map.
        /// </summary>
        public UInt16 BoundX
        {
            get
            {
                return _xMax;
            }
        }
        private UInt16 _yMax;
        /// <summary>
        /// The Y bound of this map.
        /// </summary>
        public UInt16 BoundY
        {
            get
            {
                return _yMax;
            }
        }

        private UInt32 _pixelMaxX;
        public UInt32 PixelBoundX
        {
            get
            {
                return _pixelMaxX;
            }
        }
        private UInt32 _pixelMaxY;
        public UInt32 PixelBoundY
        {
            get
            {
                return _pixelMaxY;
            }
        }

        private Tile[,] _tiles;
        /// <summary>
        /// Double Tile array representing the map
        /// </summary>
        public Tile[,] Tiles
        {
            get
            {
                return this._tiles;
            }
        }

        // keeps track of which tiles are passable
        // should be faster than constantly checking if a tile is impassable
        // also will make for better looking code
        private BitArray[] _passabilityMap;
        public BitArray[] PassabilityMap
        {
            get
            {
                return _passabilityMap;
            }
        }

        private float[,] _visibilityMap;
        public float[,] VisibilityMap
        {
            get
            {
                return _visibilityMap;
            }
            set
            {
                _visibilityMap = value;
            }
        }

        // Painter reference
        private Painter _myPainter;
        public Painter MyPainter
        {
            get
            {
                return _myPainter;
            }
            set
            {
                this._myPainter = value;
            }
        }

        // Counts spawned creatures. Used to issue unique IDs. Consider moving this to some kind of a 
        // 'world' or 'game' type.
        private UInt32 _creatureCount = 0;
        public UInt32 CreatureCount
        {
            get
            {
                return _creatureCount;
            }
        }

        private UInt32 _itemCount = 0;
        public UInt32 ItemCount
        {
            get
            {
                return this._itemCount;
            }
        }

        private RandomStuff _randomator;
        /// <summary>
        /// Reference to the random number generator for this map
        /// </summary>
        public RandomStuff Randomator
        {
            get
            {
                return this._randomator;
            }
            set
            {
                this._randomator = value;
            }
        }

        private Collider _myCollider;
        public Collider MyCollider
        {
            get
            {
                return _myCollider;
            }
        }

        private VisiblityTracker _myVisibilityTracker;
        public VisiblityTracker MyVisibilityTracker
        {
            get
            {
                return _myVisibilityTracker;
            }
        }

        private Pathfinder _myPathfinder;
        public Pathfinder MyPathfinder
        {
            get
            {
                return _myPathfinder;
            }
        }

        private SortedDictionary<UInt32, Creature> _menagerie = new SortedDictionary<UInt32, Creature>();
        /// <summary>
        /// Monsters belonging to the map.
        /// </summary>
        public SortedDictionary<UInt32, Creature> Menagerie
        {
            get
            {
                return this._menagerie;
            }
            set
            {
                this._menagerie = value;
            }
        }

        private SortedDictionary<UInt32, Creature> _mortuary = new SortedDictionary<UInt32, Creature>();
        public SortedDictionary<UInt32, Creature> Mortuary
        {
            get
            {
                return this._mortuary;
            }
            set
            {
                this._mortuary = value;
            }
        }

        // Item catalogue, indexed by ID
        private SortedDictionary<UInt32, Item> _catalogue = new SortedDictionary<UInt32, Item>();
        public SortedDictionary<UInt32, Item> Catalogue
        {
            get
            {
                return this._catalogue;
            }
            set
            {
                this._catalogue = value;
            }
        }

        private List<ItemStimulusValuePair>[] _addressBook = new List<ItemStimulusValuePair>[(Enum.GetValues(typeof(Stimulus))).Length];
        private void AddressBookAdd(Item addMe)
        {
            // only add static items
            if (addMe.MyType >= ItemType.STATICTHRESHOLD)
            {
                return;
            }

            foreach (KeyValuePair<Stimulus, float> kvp in addMe.ItemFunctions)
            {
                this._addressBook[(byte)kvp.Key].Add(new ItemStimulusValuePair(kvp.Value, addMe));
            }
        }
        private void AddressBookRemove(Item removeMe)
        {
            // only add static items
            if (removeMe.MyType >= ItemType.STATICTHRESHOLD)
            {
                return;
            }

            // POOR IMPLEMENTATION, FIX.
            foreach (KeyValuePair<Stimulus, float> kvp in removeMe.ItemFunctions)
            {
                this._addressBook[(byte)kvp.Key].Remove(new ItemStimulusValuePair(kvp.Value, removeMe));
            }
        }

        // Returns the map address book for the respective stimulus type 
        public List<ItemStimulusValuePair> AddressBook(Stimulus bookType)
        {
            return _addressBook[(byte)bookType];
        }

        #endregion

        #region Methods
        public Tile GetTile(Coords coords)
        {
            if (coords.Type == CoordsType.Pixel)
            {
                coords = new Coords(CoordsType.Tile, coords);
            }

            return _tiles[coords.X, coords.Y];
        }
        public Tile GetTile(Int32 X, Int32 Y)
        {
            return _tiles[X, Y];
        }
        public void SetTile(Coords coords, Tile newValue)
        {
            _tiles[coords.X, coords.Y] = newValue;
        }

        #region Creature / Item registers

        /// <summary>
        /// Returns the creature with ID 'key'
        /// </summary>
        public Creature MenagerieGetCreatureFrom(UInt32 key)
        {
            return this._menagerie[key];
        }
        /// <summary>
        /// Add creature to the menagerie. They 'key' is the creature ID.
        /// </summary>
        public void MenagerieAddCreatureTo(UInt32 key, Creature newGuy)
        {
            this._menagerie[key] = newGuy;
        }
        /// <summary>
        /// Deletes the creature with 'ID' key from the dictionary.
        /// </summary>
        public void MenagerieDeleteCreatureFrom(UInt32 key)
        {
            this._menagerie.Remove(key);
        }
        public void MortuaryAddCreatureTo(UInt32 key, Creature newGuy)
        {
            this._mortuary[key] = newGuy;
        }

        /// <summary>
        /// Issues ID to a creature.
        /// </summary>
        public UInt32 IssueCreatureID()
        {
            if (_creatureCount == Constants.MaximumNumberOfCreatures)
            {
                throw new Exception("Maximum number of creatures reached.");
            }
            return this._creatureCount++;
        }

        public void CatalogueAddItemTo(UInt32 key, Item newItem)
        {
            this._catalogue.Add(key, newItem);
        }
        public void CatalogueDeleteItemFrom(UInt32 key)
        {
            this._catalogue.Remove(key);
        }

        /// <summary>
        /// Issues a new Item ID and increments item count.
        /// </summary>
        /// <returns></returns>
        public UInt32 IssueItemID()
        {
            return this._itemCount++;
        }

        #endregion

        /// <summary>
        ///  Analyzes and remembers tile accessibility. Starts at northwest corner and goes through the array,
        ///  checking east / southeast / south / southwest on the current tile and in case of accessibility
        ///  recording the result in both directions.
        /// </summary>
        public void AnalyzeTileAccessibility()
        {
            Tile currentTile;

            for (UInt16 i = 0; i < this._xMax; i++)
            {
                for (UInt16 j = 0; j < this._yMax; j++)
                {
                    Tile east, southEast, south, southWest;
                    currentTile = this._tiles[i, j];
                    if (currentTile is TileImpassable)
                    {
                        continue;
                    }

                    _passabilityMap[i][j] = true;

                    _visibilityMap[i, j] = (currentTile is TilePassable) ? 1 : 0;

                    // Sort of wasteful, hopefully compiler does this smartly
                    if (i < _xMax - 1)
                    {
                        east = this.GetTile(StaticMathFunctions.CoordsNeighboringInDirection(new Coords(CoordsType.Tile, i, j), Direction.East));
                        if (east is TilePassable)
                        {
                            (currentTile as TilePassable).AllowedMovesSet(Direction.East, true);
                            (east as TilePassable).AllowedMovesSet(Direction.West, true);
                        }
                    }

                    if ((i < _xMax - 1) & (j < _yMax - 1))
                    {
                        southEast = this.GetTile(StaticMathFunctions.CoordsNeighboringInDirection(new Coords(CoordsType.Tile, i, j), Direction.Southeast));
                        if (southEast is TilePassable)
                        {
                            (currentTile as TilePassable).AllowedMovesSet(Direction.Southeast, true);
                            (southEast as TilePassable).AllowedMovesSet(Direction.Northwest, true);
                        }
                    }

                    if (j < _yMax - 1)
                    {
                        south = this.GetTile(StaticMathFunctions.CoordsNeighboringInDirection(new Coords(CoordsType.Tile, i, j), Direction.South));
                        if (south is TilePassable)
                        {
                            (currentTile as TilePassable).AllowedMovesSet(Direction.South, true);
                            (south as TilePassable).AllowedMovesSet(Direction.North, true);
                        }
                    }

                    if ((i > 0) & (j < _yMax - 1))
                    {
                        southWest = this.GetTile(StaticMathFunctions.CoordsNeighboringInDirection(new Coords(CoordsType.Tile, i, j), Direction.Southwest));
                        if (southWest is TilePassable)
                        {
                            (currentTile as TilePassable).AllowedMovesSet(Direction.Southwest, true);
                            (southWest as TilePassable).AllowedMovesSet(Direction.Northeast, true);
                        }
                    }
                }
            }
        }

        #region Raytracers

        /// <summary>
        /// Returns the tiles under the given line.
        /// Borrowed from: http://playtechs.blogspot.com/2007/03/raytracing-on-grid.html (James McNeill)
        /// </summary>
        public List<Coords> RayTracer(Coords c1, Coords c2)
        {
            List<Coords> returnVal = new List<Coords>();

            Int32 x0 = c1.X;
            Int32 y0 = c1.Y;
            Int32 x1 = c2.X;
            Int32 y1 = c2.Y;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int x = x0;
            int y = y0;
            int n = 1 + dx + dy;
            int x_inc = (x1 > x0) ? 1 : -1;
            int y_inc = (y1 > y0) ? 1 : -1;
            int error = dx - dy;
            dx *= 2;
            dy *= 2;

            for (; n > 0; --n)
            {
                //visit(x, y);
                returnVal.Add(new Coords(c1.Type, x, y));

                if (error > 0)
                {
                    x += x_inc;
                    error -= dy;
                }
                else
                {
                    y += y_inc;
                    error += dx;
                }
            }

            return returnVal;
        }

        public bool RayTracerVisibilityCheckPixel(Vector c1, Vector c2)
        {
            double x0 = c1.X;
            double y0 = c1.Y;
            double x1 = c2.X;
            double y1 = c2.Y;

            double dx = Math.Abs(x1 - x0);
            double dy = Math.Abs(y1 - y0);

            int x = (int)(Math.Floor(x0));
            int y = (int)(Math.Floor(y0));

            int n = 1;
            int x_inc, y_inc;
            double error;

            if (dx == 0)
            {
                x_inc = 0;
                error = Double.PositiveInfinity;
            }
            else if (x1 > x0)
            {
                x_inc = 1;
                n += (int)(Math.Floor(x1)) - x;
                error = (Math.Floor(x0) + 1 - x0) * dy;
            }
            else
            {
                x_inc = -1;
                n += x - (int)(Math.Floor(x1));
                error = (x0 - Math.Floor(x0)) * dy;
            }

            if (dy == 0)
            {
                y_inc = 0;
                error -= Double.PositiveInfinity;
            }
            else if (y1 > y0)
            {
                y_inc = 1;
                n += (int)(Math.Floor(y1)) - y;
                error -= (Math.Floor(y0) + 1 - y0) * dx;
            }
            else
            {
                y_inc = -1;
                n += y - (int)(Math.Floor(y1));
                error -= (y0 - Math.Floor(y0)) * dx;
            }


            Coords c2Tile = new Coords(CoordsType.Tile, c2);

            for (; n > 0; --n)
            {
                Coords currentCoords = new Coords(CoordsType.Tile, x, y);

                // We ignore accrued visibility for now. Can add it later.
                if ((this._visibilityMap[currentCoords.X, currentCoords.Y] == 0) && (currentCoords != c2Tile))
                {
                    return false;
                }

                if (error > 0)
                {
                    y += y_inc;
                    error -= dx;
                }
                else
                {
                    x += x_inc;
                    error += dy;
                }
            }

            return true;
        }



        /// <summary>
        /// Performs a terrain passability check betwee two points by doing pixel validity checks at interval delta.
        /// </summary>
        public List<Creature> RayTracerPassabilityCheckRough(Creature client, Vector v1, Vector v2, double delta)
        {
            Vector difference = v2 - v1;
            Vector deltaV = difference;
            deltaV.ScaleToLength(delta);

            Vector currentPosition = v1;

            for (int i = 0; i < difference.Length() / deltaV.Length(); ++i)
            {
                Coords pixel = new Coords(CoordsType.Pixel, currentPosition);
                List<Creature> collision = _myCollider.CreatureClippingCheck(client, pixel, false);
                if (collision == null || collision.Count > 0)
                {
                    return collision;
                }
                currentPosition += deltaV;
            }

            return new List<Creature>();
        }

        /// <summary>
        /// Returns the Bresenham line between p0 and p1; Borrowed the code
        /// from some dude whose name I don't have, who in turn borrowed from Wikipedia.
        /// </summary>
        private List<Coords> BresenhamLine(Coords p0, Coords p1)
        {
            List<Coords> returnList = new List<Coords>();

            Boolean steep = Math.Abs(p1.Y - p0.Y) > Math.Abs(p1.X - p0.X);

            if (steep == true)
            {
                Coords tmpPoint = new Coords(CoordsType.Tile, p0.X, p0.Y);
                p0 = new Coords(CoordsType.Tile, tmpPoint.Y, tmpPoint.X);

                tmpPoint = p1;
                p1 = new Coords(CoordsType.Tile, tmpPoint.Y, tmpPoint.X);
            }

            Int32 deltaX = Math.Abs(p1.X - p0.X);
            Int32 deltaY = Math.Abs(p1.Y - p0.Y);
            Int32 error = 0;
            Int32 deltaError = deltaY;
            Int32 yStep = 0;
            Int32 xStep = 0;
            Int32 y = p0.Y;
            Int32 x = p0.X;

            if (p0.Y < p1.Y)
            {
                yStep = 1;
            }
            else
            {
                yStep = -1;
            }

            if (p0.X < p1.X)
            {
                xStep = 1;
            }
            else
            {
                xStep = -1;
            }

            Int32 tmpX = 0;
            Int32 tmpY = 0;

            while (x != p1.X)
            {

                x += xStep;
                error += deltaError;

                //if the error exceeds the X delta then
                //move one along on the Y axis
                if ((2 * error) > deltaX)
                {
                    y += yStep;
                    error -= deltaX;
                }

                //flip the coords if they're steep
                if (steep)
                {
                    tmpX = y;
                    tmpY = x;
                }
                else
                {
                    tmpX = x;
                    tmpY = y;
                }

                //check the point generated is legal
                //and if it is add it to the list
                if (_myCollider.CheckInBounds(new Coords(CoordsType.Tile, tmpX, tmpY)) == true)
                {
                    returnList.Add(new Coords(CoordsType.Tile, tmpX, tmpY));
                }
                else
                {   //a bad point has been found, so return the list thus far
                    return returnList;
                }

            }

            return returnList;
        }

        /// <summary>
        /// Checks if the Bresenham line between p0 and p1 goes only through visible tiles
        /// !!! Code repetition, should redo.
        /// </summary>
        public bool BresenhamLineCheckVisible(Coords p0, Coords p1)
        {
            if (p0.Equals(p1))
            {
                return true;
            }

            Boolean steep = Math.Abs(p1.Y - p0.Y) > Math.Abs(p1.X - p0.X);

            // fix this stupidity
            Coords p0original = new Coords(CoordsType.Tile, p0.X, p0.Y);
            Coords p1original = new Coords(CoordsType.Tile, p1.X, p1.Y);

            if (steep == true)
            {
                Coords tmpPoint = new Coords(CoordsType.Tile, p0.X, p0.Y);
                p0 = new Coords(CoordsType.Tile, tmpPoint.Y, tmpPoint.X);

                tmpPoint = p1;
                p1 = new Coords(CoordsType.Tile, tmpPoint.Y, tmpPoint.X);
            }

            Int32 deltaX = Math.Abs(p1.X - p0.X);
            Int32 deltaY = Math.Abs(p1.Y - p0.Y);
            Int32 error = 0;
            Int32 deltaError = deltaY;
            Int32 yStep = 0;
            Int32 xStep = 0;
            Int32 y = p0.Y;
            Int32 x = p0.X;

            if (p0.Y < p1.Y)
            {
                yStep = 1;
            }
            else
            {
                yStep = -1;
            }

            if (p0.X < p1.X)
            {
                xStep = 1;
            }
            else
            {
                xStep = -1;
            }

            Int32 tmpX = 0;
            Int32 tmpY = 0;


            float visibilityTotal = 1f;

            while (x != p1.X)
            {

                x += xStep;
                error += deltaError;

                //if the error exceeds the X delta then
                //move one along on the Y axis
                if ((2 * error) > deltaX)
                {
                    y += yStep;
                    error -= deltaX;
                }

                //flip the coords if they're steep
                if (steep)
                {
                    tmpX = y;
                    tmpY = x;
                }
                else
                {
                    tmpX = x;
                    tmpY = y;
                }

                // check the point generated is legal
                // using passability check. creatures will leave shadows. should write a visibility
                // check later
                Coords currentCoords = new Coords(CoordsType.Tile, tmpX, tmpY);
                // for this to look good you must make sure it takes account of the eucledean distances over which the coeffcients hold
                // otherwise you get square FOVs.
                visibilityTotal *= this._visibilityMap[currentCoords.X, currentCoords.Y];

                if (
                    (visibilityTotal < Constants.VisibilityTreshold)
                    &
                    (!(currentCoords.Equals(p0original) | currentCoords.Equals(p1original)))
                    )
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region collision / bounds / etc -checkers

        /// <summary>
        /// Checks if tile is see-through or not.
        /// </summary>
        public bool CheckSightValidity(Coords point)
        {
            if (!(_myCollider.CheckInBounds(point)))
                return false;

            return _visibilityMap[point.X, point.Y] > 0;
        }

        /// <summary>
        /// Checks if tile allows passage
        /// Some of this is redundant now that I have Tile.SeeAllowedMove. Should rethink.
        /// </summary>
        public bool CheckTilePassageValidity(Coords point)
        {
            if (!this.CheckSightValidity(point))
            {
                return false;
            }

            return _passabilityMap[point.X][point.Y];
        }

        #endregion

        #region Item / Creature spawners

        /// <summary>
        /// Spawns the player on the 'ground' at 'startPoint'
        /// returns a reference to the Player so one can more easily take care of references.
        /// </summary>
        public Creature SpawnPlayer(Coords startPoint)
        {
            //Player player = new Player(this, startPoint, this.IssueCreatureID());
            //this.PlayerReference = player;
            return null;
        }

        public void SpawnCreature(Coords startPoint, Team team, CreatureGenerator generator)
        {
            Creature newguy = new Creature(this, startPoint, (UInt16)this.IssueCreatureID(), team, generator);
            team.MemberRegister(newguy);
        }

        public void CreateItem(Coords startPoint, ItemGenerator item)
        {
            Item newItem = new Item(this.IssueItemID(), item);
            this.CatalogueAddItemTo(newItem.ID, newItem);
            //Coords bedLocation = new Coords((Int32)((bottomRight.X + topLeft.X) * 0.5), (Int32)((bottomRight.Y + topLeft.Y) * 0.5));
            TilePassable itemTile = this.GetTile(startPoint) as TilePassable;
            itemTile.InventoryAddItem(newItem);
        }

        #endregion

        #endregion

        #region Constructors

        // Constructs an xSize by ySize map. Default Tile set to TileBasicFloor.
        public Map(UInt16 xSize, UInt16 ySize, Int32 seed)
        {
            // creates and fills the tile array
            this._xMax = xSize;
            this._yMax = ySize;

            this._pixelMaxX = (UInt32)(xSize * Constants.TileSize);
            this._pixelMaxY = (UInt32)(ySize * Constants.TileSize);

            _tiles = new Tile[xSize, ySize];
            _visibilityMap = new float[xSize, ySize];
            // tiles = new TileBasicFloor[xSize, ySize];
            for (Int32 i = 0; i < xSize; i++)
            {
                for (Int32 j = 0; j < ySize; j++)
                {
                    _tiles[i, j] = new TilePassable(this, new Coords(CoordsType.Tile, i, j), Constants.TileGeneratorGrass);
                }
            }

            this._passabilityMap = new BitArray[xSize];
            for (int i = 0; i < xSize; ++i)
            {
                _passabilityMap[i] = new BitArray(ySize);
            }

            this._myCollider = new Collider(xSize, ySize, Constants.TileSize, Constants.TileSize, _passabilityMap);
            this._myVisibilityTracker = new VisiblityTracker(xSize, ySize, Constants.TileSize, Constants.TileSize, _passabilityMap, _visibilityMap);
            this._myPathfinder = new Pathfinder(_passabilityMap);

            // initializes the random number generator associated with this map
            this._randomator = new RandomStuff(seed);
        }

        // Constructs a square map. Default Tile set to TileBasicFloor.
        public Map(UInt16 dimension, Int32 seed)
            : this(dimension, dimension, seed)
        {
        }
        #endregion
    }
}
