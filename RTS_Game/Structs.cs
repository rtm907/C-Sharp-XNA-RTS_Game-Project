using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace RTS_Game
{
    public enum CoordsType
    {
        General = 0,
        Pixel = 1,
        Tile = 2
    }

    /// <summary>
    /// Coords struct. Similar to 'Point', but I chose a different name to 
    /// distinguish this structure from Drawing.Point
    /// </summary>
    public struct Coords : IComparable
    {
        private Int32 _X;
        public Int32 X
        {
            get
            {
                return _X;
            }
            set
            {
                _X = value;
            }
        }

        private Int32 _Y;
        public Int32 Y
        {
            get
            {
                return _Y;
            }
            set
            {
                _Y = value;
            }
        }

        private CoordsType _type;
        public CoordsType Type
        {
            get
            {
                return _type;
            }
        }

        public Coords NeighborInDirection(Direction dir)
        {
            switch (dir)
            {
                case (Direction.Northeast):
                    return new Coords(this.Type, this.X + 1, this.Y - 1);
                case (Direction.East):
                    return new Coords(this.Type, this.X + 1, this.Y);
                case (Direction.Southeast):
                    return new Coords(this.Type, this.X + 1, this.Y + 1);
                case (Direction.South):
                    return new Coords(this.Type, this.X, this.Y + 1);
                case (Direction.Southwest):
                    return new Coords(this.Type, this.X - 1, this.Y + 1);
                case (Direction.West):
                    return new Coords(this.Type, this.X - 1, this.Y);
                case (Direction.Northwest):
                    return new Coords(this.Type, this.X - 1, this.Y - 1);
                case (Direction.North):
                    return new Coords(this.Type, this.X, this.Y - 1);
            }

            // This code should be unreachable. Added because compiler wants it.
            return this;
        }

        public float DistanceTo(Coords c)
        {
            return (float)Math.Sqrt(Math.Pow((this.X - c.X), 2) + Math.Pow((this.Y - c.Y), 2));
        }

        #region Operators
        public Int32 CompareTo(object obj)
        {
            if (!(obj is Coords))
            {
                throw new Exception("Bad Coords comparison.");
            }

            Coords compared = (Coords)obj;

            Int32 score = 0;

            score = (this.X + this.Y) - (compared.X + compared.Y);

            if (score == 0)
            {
                score = this.X - compared.X;
            }

            return score;
        }

        public override bool Equals(object obj)
        {
            return (obj is Coords) && this == (Coords)obj;
        }

        public override Int32 GetHashCode()
        {
            return _X ^ _Y;
        }

        public override string ToString()
        {
            return (String)"(" + this._X + ", " + this._Y + ")";
        }

        public static bool operator ==(Coords c1, Coords c2)
        {
            return (c1.X == c2.X && c1.Y == c2.Y);
        }

        public static bool operator !=(Coords c1, Coords c2)
        {
            return (c1.X != c2.X || c1.Y != c2.Y);
        }

        public static Coords operator -(Coords c1, Coords c2)
        {
            return new Coords(c1._type, c1.X - c2.X, c1.Y - c2.Y);
        }

        public static Coords operator +(Coords c1, Coords c2)
        {
            return new Coords(c1._type, c1.X + c2.X, c1.Y + c2.Y);
        }

        #endregion

        public Coords(Int32 Xval, Int32 Yval)
            : this(CoordsType.General, Xval, Yval)
        {
        }

        public Coords(CoordsType myType, Int32 Xval, Int32 Yval)
        {
            this._type = myType;
            this._X = Xval;
            this._Y = Yval;
        }

        public Coords(CoordsType newType, Coords c)
        {
            this._type = newType;
            if (newType == CoordsType.Tile && c._type == CoordsType.Pixel)
            {
                _X = c.X / Constants.TileSize;
                _Y = c.Y / Constants.TileSize;
                return;
            }
            else if (newType == CoordsType.Pixel && c._type == CoordsType.Tile)
            {
                _X = c.X + (Int32)(0.5 * Constants.TileSize);
                _Y = c.Y + (Int32)(0.5 * Constants.TileSize);
                return;
            }

            _X = c.X;
            _Y = c.Y;
        }

        public Coords(CoordsType newType, Vector v)
        {
            this._type = newType;
            if (newType == CoordsType.Tile)
            {
                _X = (Int32)(v.X / Constants.TileSize);
                _Y = (Int32)(v.Y / Constants.TileSize);
                return;
            }

            _X = (Int32)Math.Floor(v.X);
            _Y = (Int32)Math.Floor(v.Y);
        }
    }

    public struct Vector
    {
        private double _X;
        public double X
        {
            get
            {
                return _X;
            }
            set
            {
                _X = value;
            }
        }

        private double _Y;
        public double Y
        {
            get
            {
                return _Y;
            }
            set
            {
                _Y = value;
            }
        }

        #region Operators

        public override bool Equals(object obj)
        {
            return (obj is Vector) && this == (Vector)obj;
        }

        public override Int32 GetHashCode()
        {
            return (byte)_X ^ (byte)_Y;
        }

        public override string ToString()
        {
            return (String)"(" + this._X + ", " + this._Y + ")";
        }

        public static bool operator ==(Vector c1, Vector c2)
        {
            return (c1.X == c2.X && c1.Y == c2.Y);
        }

        public static bool operator !=(Vector c1, Vector c2)
        {
            return (c1.X != c2.X || c1.Y != c2.Y);
        }

        public static Vector operator -(Vector c1, Vector c2)
        {
            return new Vector(c1.X - c2.X, c1.Y - c2.Y);
        }

        public static Vector operator +(Vector c1, Vector c2)
        {
            return new Vector(c1.X + c2.X, c1.Y + c2.Y);
        }

        #endregion

        public double Length()
        {
            return Math.Sqrt(_X * _X + _Y * _Y);
        }

        public Vector Rotate(double angle)
        {
            Double sinA = Math.Sin(angle);
            Double cosA = Math.Cos(angle);
            return new Vector(_X * cosA + _Y * sinA, -_X * sinA + _Y * cosA);
        }

        public Vector PerpendiculatLeft()
        {
            return new Vector(-this.Y, this.X);
        }

        public Vector PerpendiculatRight()
        {
            return new Vector(this.Y, -this.X);
        }

        public Vector Opposite()
        {
            return new Vector(-this.X, -this.Y);
        }

        public void ScaleToLength(double length)
        {
            double currentLength = this.Length();

            if (currentLength > 0)
            {
                double scale = length / this.Length();
                _X = _X * scale;
                _Y = _Y * scale;
            }
        }

        public double DistanceTo(Vector somewhere)
        {
            return Math.Sqrt(Math.Pow(this.X - somewhere.X, 2) + Math.Pow(this.Y - somewhere.Y, 2));
        }

        public Vector(double x, double y)
        {
            _X = x;
            _Y = y;
        }

        public Vector(Coords c)
        {
            if (c.Type == CoordsType.Tile)
            {
                c = new Coords(CoordsType.Pixel, c);
            }
            _X = c.X;
            _Y = c.Y;
        }
    }

    public struct TileGenerator
    {
        public readonly String name;
        public readonly float visibilityCoefficient;
        public readonly SpriteTile tileBitmap;
        public readonly bool passable;

        public TileGenerator(bool givenPassable, String givenName, float givenVisibilityCoefficient, SpriteTile givenBitmap)
        {
            this.passable = givenPassable;
            this.name = givenName;
            this.visibilityCoefficient = givenVisibilityCoefficient;
            this.tileBitmap = givenBitmap;
        }
    }

    public struct ItemGenerator
    {
        public readonly String name;
        public readonly SpriteItem itemBitmap;
        public readonly ItemType typeOfItem;
        public readonly Dictionary<Stimulus, float> functions;

        public ItemGenerator(String givenName, SpriteItem givenBitmap, ItemType givenType, Dictionary<Stimulus, float> givenFunctions)
        {
            this.name = givenName;
            this.typeOfItem = givenType;
            this.functions = givenFunctions;

            this.itemBitmap = givenBitmap;
            //this.itemBitmap.MakeTransparent(itemBitmap.GetPixel(0,0)); // am trying to modify static field?
        }
    }

    public struct CreatureGenerator
    {
        public readonly String name;
        public readonly SpriteBatchCreature creatureBitmaps;

        public readonly UInt16 StatHPMax;
        public readonly UInt16 StatArmor;
        public readonly UInt16 StatSight;
        public readonly UInt16 StatMoveSpeed;

        public readonly UInt16 StatDamage;
        public readonly UInt16 StatAttackTime;
        public readonly double StatAttackRange;
        //public readonly UInt16 HPMax;

        public CreatureGenerator(String givenName, SpriteBatchCreature givenCreatureBitmaps, 
            UInt16 givenStatHPMax, UInt16 givenStatDamage, UInt16 givenStatArmor, UInt16 givenStatSight, 
            UInt16 givenStatMoveSpeed, UInt16 givenStatAttackTime, double givenStatAttackRange)
        {
            name = givenName;
            creatureBitmaps = givenCreatureBitmaps;

            StatHPMax = givenStatHPMax;
            StatDamage = givenStatDamage;
            StatArmor = givenStatArmor;
            StatSight = givenStatSight;
            StatMoveSpeed = givenStatMoveSpeed;
            StatAttackTime = givenStatAttackTime;
            StatAttackRange = givenStatAttackRange;
        }
    }


    // Helper struct for the map address book. Stores the stimulus value of an item, with a reference to the item.
    public struct ItemStimulusValuePair : IComparable
    {
        private float _stimulusValue;
        public float StimulusValue
        {
            get
            {
                return this._stimulusValue;
            }
            set
            {
                this._stimulusValue = value;
            }
        }
        private Item _itemReference;
        public Item ItemReference
        {
            get
            {
                return this._itemReference;
            }
        }

        public Int32 CompareTo(object obj)
        {
            if (!(obj is ItemStimulusValuePair))
            {
                throw new Exception("Bad ItemStimulusValuePair comparison.");
            }

            ItemStimulusValuePair compared = (ItemStimulusValuePair)obj;

            return this._itemReference.CompareTo(compared._itemReference);
        }

        public ItemStimulusValuePair(float newValue, Item newItem)
        {
            this._stimulusValue = newValue;
            this._itemReference = newItem;
        }
    }


    // Stats struct for the creatures
    // LET'S USE AN ENUM INSTEAD 
    /*
    public struct Stats
    {
        // Primary stats.
        // Min 1, max 20?
        private UInt16 _strength;
        public UInt16 Strength
        {
            get
            {
                return _strength;
            }
            set
            {
                _strength = value;
            }
        }

        private UInt16 _speed;
        public UInt16 Speed
        {
            get
            {
                return _speed;
            }
            set
            {
                _speed = value;
            }
        }

        private UInt16 _eyesight;
        public UInt16 Eyesight
        {
            get
            {
                return _eyesight;
            }
            set
            {
                _eyesight = value;
            }
        }

        public Stats(UInt16 strength, UInt16 speed, UInt16 eyesight)
        {
            this._strength = strength;
            this._speed = speed;
            this._eyesight = eyesight;
        }
    }
     * */

    // Creature's body (maybe combine with stats?)
    // Min for each is 0? Limb missing? (Or mortal wound for head/ torso).
    // Instead, let's just have an UInt16 array in the creature, and use an enum
    /*
    public struct Body
    {
        private UInt16 _head;
        public UInt16 Head
        {
            get
            {
                return _head;
            }
            set
            {
                _head = value;
            }
        }

        private UInt16 _torso;
        public UInt16 Torso
        {
            get
            {
                return _torso;
            }
            set
            {
                _torso = value;
            }
        }

        private UInt16 _armLeft;
        public UInt16 ArmLeft
        {
            get
            {
                return _armLeft;
            }
            set
            {
                _armLeft = value;
            }
        }

        private UInt16 _armRight;
        public UInt16 ArmRight
        {
            get
            {
                return _armRight;
            }
            set
            {
                _armRight = value;
            }
        }

        private UInt16 _legLeft;
        public UInt16 LegLeft
        {
            get
            {
                return _legLeft;
            }
            set
            {
                _legLeft = value;
            }
        }

        private UInt16 _legRight;
        public UInt16 LegRight
        {
            get
            {
                return _legRight;
            }
            set
            {
                _legRight = value;
            }
        }

        public Body(UInt16 head, UInt16 torso, UInt16 armL, UInt16 armR, UInt16 legL, UInt16 legR)
        {
            this._head = head;
            this._torso = torso;
            this._armLeft = armL;
            this._armRight = armR;
            this._legLeft = legL;
            this._legRight = legR;
        }
    }
    */

    /*
    public enum Stats : sbyte
    {
        Strength = 0,
        Speed = 1,
        Eyesight = 2
    }

    public enum BodyParts : sbyte
    {
        Head = 0,
        Torso = 1,
        ArmLeft = 2,
        ArmRight = 3,
        LegLeft = 4,
        LegRight = 5
    }
    */

    public enum Stimulus : sbyte
    {
        Hunger = 0,
        Thirst = 1,
        Work = 2, // do types of work?
        Violence = 3,
        Friend = 4,
        Rest = 5
    }

    public enum ItemType : sbyte
    {
        StaticUsable = 0,
        StaticOrnamental = 1,
        STATICTHRESHOLD = 2,
        PortableUsable = 3,
        PortableOrnamental = 4,
        PortableConsumable = 5
    }

    /*
    [Flags]
    public enum EdgeTileFlags
    {
        None = 0x0,
        WalkThrough = 0x1,
        SeeThrough = 0x2
    }
     */

    public enum CollisionType
    {
        None = 0,
        Terrain,
        Creature
    }

    /// <summary>
    /// The compass directions, clockwise, Northeast is 1.
    /// </summary>
    public enum Direction : sbyte
    {
        Northeast = 0,
        East = 1,
        Southeast = 2,
        South = 3,
        Southwest = 4,
        West = 5,
        Northwest = 6,
        North = 7
    }

    /// <summary>
    /// Scrolling type flag.
    /// </summary>
    public enum ScrollingType : sbyte
    {
        /// <summary>
        /// Player centered scrolling (a-la Diablo).
        /// Fits RPG games.
        /// </summary>
        Player = 0,
        /// <summary>
        /// Free scrolling.
        /// Fits RTS-type games.
        /// </summary>
        Free
    }

    public enum SpriteTile : sbyte
    {
        Grass = 0,
        FloorDirt,
        RoadPaved,
        WallStone,
        COUNT
    }

    public enum SpriteBatchCreature : sbyte
    {
        Player = 0,
        Gnome,
        COUNT
    }

    public enum SpriteItem : sbyte
    {
        Bed = 0,
        ToolTable,
        TreeApple,
        Well,
        COUNT
    }
}
