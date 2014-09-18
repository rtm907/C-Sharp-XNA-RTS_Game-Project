using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

// ITEMS AND INVENTORY

namespace RTS_Game
{
    public class Inventory
    {
        // Shouldn't this be a list of the item IDs? This way I do fewer look-ups.
        private List<Item> _itemList;
        public List<Item> ItemList
        {
            get
            {
                return this._itemList;
            }
            set
            {
                this._itemList = value;
            }
        }
        public void ItemAddToList(Item newone)
        {
            this._itemList.Add(newone);
            newone.ParentInventory = this;
        }
        public void ItemRemoveFromList(Item toRemove)
        {
            this._itemList.Remove(toRemove);
            toRemove.ParentInventory = null;
        }

        // One of the following two should always be null. An inventory belongs to a tile OR
        // a creature. (later maybe also to a container?)
        private TilePassable _ownerTile;
        public TilePassable OwnerTile
        {
            get
            {
                return this._ownerTile;
            }
            set
            {
                this._ownerTile = value;
                this._ownerCreature = null;
            }
        }
        private Creature _ownerCreature;
        public Creature OwnerCreature
        {
            get
            {
                return this._ownerCreature;
            }
            set
            {
                this._ownerCreature = value;
                this._ownerTile = null;
            }
        }

        // returns the item's position (in Coords)
        public Nullable<Coords> Position()
        {
            Nullable<Coords> returnValue = null;
            if (this._ownerTile != null)
            {
                returnValue = _ownerTile.Position;
            }
            else if (this._ownerCreature != null)
            {
                returnValue = new Coords(CoordsType.Tile, _ownerCreature.PositionPixel);
            }

            return returnValue;
        }

        private Inventory()
        {
            this._itemList = new List<Item>();
        }

        public Inventory(Creature owner)
            : this()
        {
            this._ownerTile = null;
            this._ownerCreature = owner;
        }

        public Inventory(TilePassable ownerTile)
            : this()
        {
            this._ownerTile = ownerTile;
            this._ownerCreature = null;
        }
    }

    public class Item : IComparable
    {
        private UInt32 _ID;
        public UInt32 ID
        {
            get
            {
                return this._ID;
            }
            set
            {
                this._ID = value;
            }
        }

        private String _itemName;
        public String Name
        {
            get
            {
                return _itemName;
            }
        }

        private SpriteItem _itemBitmap;
        public SpriteItem ItemBitmap
        {
            get
            {
                return this._itemBitmap;
            }
        }

        /*
        private UInt32 _weight;
        public UInt32 Weight
        {
            get
            {
                return _weight;
            }
            set
            {
                this._weight = value;
            }
        }

        private UInt32 _volume;
        public UInt32 Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                this._volume = value;
            }
        }
        */

        private Inventory _parent;
        public Inventory ParentInventory
        {
            get
            {
                return this._parent;
            }
            set
            {
                this._parent = value;
            }
        }

        public Nullable<Coords> Position()
        {
            return (this._parent == null) ? null : this._parent.Position();
        }

        private ItemType _itemType;
        public ItemType MyType
        {
            get
            {
                return _itemType;
            }
        }

        public Int32 CompareTo(object obj)
        {
            if (!(obj is Item))
            {
                throw new Exception("Bad Items comparison.");
            }

            Item compared = (Item)obj;

            return (Int32)(this._ID - compared.ID);
        }

        private Dictionary<Stimulus, float> _itemFunctions = new Dictionary<Stimulus, float>();
        public Dictionary<Stimulus, float> ItemFunctions
        {
            get
            {
                return _itemFunctions;
            }
        }

        #region constructors

        private Item(UInt32 itemID)
        {
            this._ID = itemID;
        }

        public Item(UInt32 itemID, ItemGenerator generator)
            : this(itemID)
        {
            this._itemName = generator.name;
            this._itemBitmap = generator.itemBitmap;
            this._itemFunctions = generator.functions;
            this._itemType = generator.typeOfItem;
        }

        public Item(UInt32 itemID, String name, SpriteItem myBitmap, Dictionary<Stimulus, float> functions, ItemType itemType)
            : this(itemID)
        {
            this._itemName = name;
            this._itemBitmap = myBitmap;
            this._itemType = itemType;
            this._itemFunctions = functions;
        }

        public Item(UInt32 itemID, String name, SpriteItem myBitmap, Dictionary<Stimulus, float> functions, ItemType itemType, Inventory parent)
            : this(itemID, name, myBitmap, functions, itemType)
        {
            this._parent = parent;
        }

        #endregion
        /*
        public Item(UInt32 itemID, String name, Dictionary<Stimulus, float> functions, ItemType itemType, Inventory parent, UInt32 itemWeight, UInt32 itemVolume)
            : this(itemID, name, functions, itemType, parent)
        {
            this._weight = itemWeight;
            this._volume = itemVolume;
        }*/
    }
}
