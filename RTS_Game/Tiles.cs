using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace RTS_Game
{
    /// <summary>
    /// Tile class. Represents one square of the map.
    /// </summary>
    public abstract class Tile : IComparable
    {
        #region Properties
        // Coords of the Tile
        private Coords _position;
        public Coords Position
        {
            get
            {
                return this._position;
            }
            set
            {
                this._position = value;
            }
        }

        public Coords PixelTopLeft()
        {
            return new Coords(CoordsType.Pixel, _position.X * Constants.TileSize, _position.Y * Constants.TileSize);
        }
        public Coords PixelTopRight()
        {
            return new Coords(CoordsType.Pixel, (_position.X + 1) * Constants.TileSize - 1, _position.Y * Constants.TileSize);
        }
        public Coords PixelBottomLeft()
        {
            return new Coords(CoordsType.Pixel, _position.X * Constants.TileSize, (_position.Y + 1) * Constants.TileSize - 1);
        }
        public Coords PixelBottomRight()
        {
            return new Coords(CoordsType.Pixel, (_position.X + 1) * Constants.TileSize - 1, (_position.Y + 1) * Constants.TileSize - 1);
        }
        public Coords PixelMiddle()
        {
            return new Coords(CoordsType.Pixel, (Int32)((_position.X + 0.5) * Constants.TileSize), (Int32)((_position.Y + 0.5) * Constants.TileSize));
        }

        // Reference to the Map. 
        private Map _inhabitedMap;
        public Map InhabitedMap
        {
            get
            {
                return this._inhabitedMap;
            }
            set
            {
                this._inhabitedMap = value;
            }
        }

        private String _myName;
        public String MyName
        {
            get
            {
                return _myName;
            }
            set
            {
                _myName = value;
            }
        }

        protected SpriteTile _myBitmap;
        public SpriteTile MyBitmap
        {
            get
            {
                return _myBitmap;
            }
        }

        #endregion

        #region Methods

        public Int32 CompareTo(object obj)
        {
            if (!(obj is Tile))
            {
                throw new Exception("Bad Tile comparison.");
            }

            Tile compared = (Tile)obj;

            return this._position.CompareTo(compared.Position);
        }

        #endregion

        #region Constructors

        private Tile(Map home, Coords position)
        {
            this.InhabitedMap = home;
            this.Position = position;
        }

        protected Tile(Map home, Coords position, TileGenerator generator)
            : this(home, position)
        {
            this._myName = generator.name;
            //this._visibilityCoefficient = generator.visibilityCoefficient;
            this._myBitmap = generator.tileBitmap;
        }

        protected Tile(Map home, Coords position, String name, float visibilityCoefficient, SpriteTile myBitmap)
            : this(home, position)
        {
            this._myName = name;
            //this._visibilityCoefficient = visibilityCoefficient;
            this._myBitmap = myBitmap;
        }

        #endregion
    }

    #region TileImpassable

    /// <summary>
    ///  Impassable Tiles interface
    /// </summary>
    public class TileImpassable : Tile
    {
        public TileImpassable(Map home, Coords position, TileGenerator generator)
            : base(home, position, generator)
        {
        }

        public TileImpassable(Map home, Coords position, String name, float visibilityCoefficient, SpriteTile myBitmap)
            : base(home, position, name, visibilityCoefficient, myBitmap)
        {
        }
    }

    #endregion

    #region TilePassable

    /// <summary>
    /// Passable Tiles interface
    /// </summary>
    public class TilePassable : Tile
    {
        private Inventory _myInventory;
        public Inventory MyInventory
        {
            get
            {
                return _myInventory;
            }
            set
            {
                this._myInventory = value;
            }
        }

        public void InventoryAddItem(Item toAdd)
        {
            _myInventory.ItemAddToList(toAdd);
        }

        public void InventoryRemoveItem(Item toRemove)
        {
            _myInventory.ItemRemoveFromList(toRemove);
        }

        // Remembers which neighboring tiles allow movement, to avoid extra checks
        // The function that analyzes the map accessibilities is in the Map class
        private BitArray _allowedMoves = new BitArray(8);
        public bool AllowedMovesCheckInDirection(Direction direction)
        {
            return this._allowedMoves[(byte)direction];
        }
        public void AllowedMovesSet(Direction direction, bool newValue)
        {
            this._allowedMoves[(byte)direction] = newValue;
        }

        public TilePassable(Map home, Coords position, TileGenerator generator)
            : base(home, position, generator)
        {
            this._myInventory = new Inventory(this);
        }

        public TilePassable(Map home, Coords position, String name, float visibilityCoefficient, SpriteTile myBitmap)
            : base(home, position, name, visibilityCoefficient, myBitmap)
        {
            this._myInventory = new Inventory(this);
        }
    }

    #endregion

}
