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
    /// Creatures interface.
    /// Stores the creatures ID and Coords, its brain, map and tile references, its sight range,
    /// and other relevant data.
    /// </summary>
    public class Creature : IComparable
    {
        #region Properties

        private String _name;
        public String Name
        {
            get
            {
                return _name;
            }
        }

        // Creature unique ID
        private UInt32 _uniqueID;
        public UInt32 UniqueID
        {
            get
            {
                return this._uniqueID;
            }
            set
            {
                this._uniqueID = value;
            }
        }

        // Reference to the Map the creature lives in. 
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

        // Creature coarse Coords on the map - used for FOV and the LOD Pathfinder
        private Coords _positionTile;
        public Coords PositionTile
        {
            get
            {
                return this._positionTile;
            }
            set
            {
                this._positionTile = value;
                this.FieldOfViewUpdate();
            }
        }

        // Pixel-accurate position
        private Coords _positionPixel;
        public Coords PositionPixel
        {
            get
            {
                return _positionPixel;
            }
            set
            {
                // NOTE: This first check is a needless repetition. Fix it so this list is somehow passed.
                // obtain new entry tiles
                _positionPixel = value;


                Coords newCoords = new Coords(CoordsType.Tile, value);

                if (!newCoords.Equals(_positionTile))
                {
                    // visibility tracking update
                    _myVisibilityTracker.VisibilityResidentRemove(_positionTile, this);
                    _myVisibilityTracker.VisibilityResidentAdd(newCoords, this);
                    PositionTile = newCoords;
                }
            }
        }

        private Vector _positionDouble;
        public Vector PositionDouble
        {
            get
            {
                return _positionDouble;
            }
            set
            {
                _positionDouble = value;
                Coords check = new Coords(CoordsType.Pixel, value);
                if (check != this._positionPixel)
                {
                    this.PositionPixel = check;
                }
            }
        }

        // Bounding ellipse's radii.
        // MUST BE LESS THAN HALF OF ONE TILE'S PIXEL SIZE. FIX LATER!
        private UInt16 _radiusX;
        public UInt16 RadiusX
        {
            get
            {
                return _radiusX;
            }
        }
        private UInt16 _radiusY;
        public UInt16 RadiusY
        {
            get
            {
                return _radiusY;
            }
        }

        protected SpriteBatchCreature _myBitmap;
        public SpriteBatchCreature MyBitmap
        {
            get
            {
                return _myBitmap;
            }
            set
            {
                _myBitmap = value;
            }
        }

        private Team _myTeam;
        public Team Team
        {
            get
            {
                return _myTeam;
            }
        }

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

        #region stats

        private UInt16 _statHP;
        public UInt16 StatHP
        {
            get
            {
                return _statHP;
            }
            set
            {
                _statHP = value;

                if (value == 0)
                {
                    this._dead = true;
                }
            }
        }

        private UInt16 _statHPMax;
        public UInt16 StatHPMax
        {
            get
            {
                return _statHPMax;
            }
            set
            {
                _statHPMax = value;
            }
        }

        private UInt16 _statDamage;
        public UInt16 StatDamage
        {
            get
            {
                return _statDamage;
            }
            set
            {
                _statDamage = value;
            }
        }

        private UInt16 _statArmor;
        public UInt16 StatArmor
        {
            get
            {
                return _statArmor;
            }
            set
            {
                _statArmor = value;
            }
        }

        private UInt16 _statSightRange;
        public UInt16 StatSightRange
        {
            get
            {
                return this._statSightRange;
            }
            set
            {
                this._statSightRange = value;
            }
        }

        private UInt16 _statAttackTime;
        public UInt16 StatAttackTime
        {
            get
            {
                return _statAttackTime;
            }
            set
            {
                _statAttackTime = value;
            }
        }

        private double _statAttackRange;
        public double StatAttackRange
        {
            get
            {
                return _statAttackRange;
            }
            set
            {
                _statAttackRange = value;
            }
        }

        protected double _moveSpeedCurrent;
        public double MoveSpeedCurrent
        {
            get
            {
                return _moveSpeedCurrent;
            }
            set
            {
                _moveSpeedCurrent = value;
            }
        }

        protected double _moveSpeedMax;
        public double MoveSpeedMax
        {
            get
            {
                return _moveSpeedMax;
            }
        }

        protected double _moveAcceleration;
        public double MoveAcceleration
        {
            get
            {
                return _moveAcceleration;
            }
        }

        #endregion

        private bool _dead;
        public bool Dead
        {
            get
            {
                return _dead;
            }
            set
            {
                _dead = value;
            }
        }

        /*
        // Creature statistics (there should be two of these, base and current)
        private UInt16[] _statistics;
        public UInt16[] Statistics
        {
            get
            {
                return this._statistics;
            }
            set
            {
                this._statistics = value;
            }
        }

        // Creature body
        private UInt16[] _bodyParts;
        public UInt16[] MyBody
        {
            get
            {
                return this._bodyParts;
            }
            set
            {
                this._bodyParts = value;
            }
        }

        protected double _turnSpeed;
        public double TurnSpeeed
        {
            get
            {
                return _turnSpeed;
            }
        }
        */

        /*
        // Creature Field-Of-View
        // Perhaps should be in the mnemosyne class?
        private List<Coords> _FOV;
        public List<Coords> FOV
        {
            get
            {
                return this._FOV;
            }
            set
            {
                this._FOV = value;
            }
        }
        */

        private BitArray[] _fieldOfView;
        public BitArray[] FieldOfView
        {
            get
            {
                return _fieldOfView;
            }
        }

        private Brain _creatureBrain;
        public Brain CreatureBrain
        {
            get
            {
                return _creatureBrain;
            }
            set
            {
                this._creatureBrain = value;
            }
        }

        // The label below the creature (creature name?)
        private String _labelLower;
        public String LabelLower
        {
            get
            {
                return this._labelLower;
            }
            set
            {
                this._labelLower = value;
            }
        }

        // The label above the creature (for talking)
        private String _labelUpper;
        public String LabelUpper
        {
            get
            {
                return this._labelUpper;
            }
            set
            {
                this._labelUpper = value;
            }
        }

        private bool _selected;
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                _selected = value;
            }
        }

        #endregion

        #region Methods

        public Int32 CompareTo(object obj)
        {
            if (!(obj is Creature))
            {
                throw new Exception("Bad Creature comparison.");
            }

            Creature compared = (Creature)obj;

            return (Int32)(this._uniqueID - compared._uniqueID);
        }

        public override bool Equals(object obj)
        {
            return (obj is Creature) && this == (Creature)obj;
        }

        public override Int32 GetHashCode()
        {
            return (Int32)_uniqueID;
        }

        // Returns true if coords c are on the creature's FOV list.
        // There is some redundancy here, because both the tile and the creature store the
        // visibility info.
        public bool Sees(Coords c)
        {
            //return this._FOV.Contains(c);
            return this._fieldOfView[c.X][c.Y];
        }

        public void FieldOfViewUpdate()
        {
            UInt16 range = this.StatSightRange;
            //Map currentMap = this.InhabitedMap;

            if (range < 0)
            {
                return;
            }

            //REMOVE REDUNDANCY HERE
            BitArray[] update = new BitArray[_inhabitedMap.BoundX];
            for (int i = 0; i < _inhabitedMap.BoundX; ++i)
            {
                update[i] = new BitArray(_inhabitedMap.BoundY);
            }

            for (Int32 i = -range + 1; i < range; i++)
            {
                for (Int32 j = -range + 1; j < range; j++)
                {
                    Coords current = new Coords(CoordsType.Tile, _positionTile.X + i, _positionTile.Y + j);
                    if (
                        !_myCollider.CheckInBounds(current)
                        ||
                        (StaticMathFunctions.DistanceBetweenTwoCoordsEucledean(this._positionTile, current) > range)
                        )
                    {
                        continue;
                    }

                    bool val = _myVisibilityTracker.RayTracerVisibilityCheckTile(this._positionTile, current, false);

                    update[current.X][current.Y] = val;
                }
            }

            // determine values that were changed
            for (int i = 0; i < _inhabitedMap.BoundX; ++i)
            {
                update[i] = update[i].Xor(_fieldOfView[i]);
            }

            // update changes
            for (int i = 0; i < _inhabitedMap.BoundX; ++i)
            {
                for (int j = 0; j < _inhabitedMap.BoundY; ++j)
                {
                    if (update[i][j])
                    {
                        bool val = _fieldOfView[i][j];
                        _fieldOfView[i][j] = !val;
                        //_inhabitedMap.GetTile(i, j).VisibilityUpdate(this, !val);
                        _myVisibilityTracker.VisibilityUpdate(new Coords(CoordsType.Tile, i, j), this, !val);
                    }
                }
            }
        }

        /// <summary>
        /// Creature processes incurred hit.
        /// </summary>
        /// <param name="attackerID">ID of the attacker</param>
        /// <param name="struckMember">Struck body part</param>
        /// <param name="hitMagnitude">Hit magnitude</param>
        public void HitIncurred(Creature attacker, UInt16 hitMagnitude)
        {
            // elementary combat model
            this._statHP = (UInt16) Math.Max(0, _statHP - (hitMagnitude - _statArmor));

            if (_statHP == 0)
            {
                this._dead = true;
            }
        }

        //bool deregistered = false;

        /// <summary>
        /// Creature death clean-up.
        /// </summary>
        public void Death()
        {
            //deregistered = true;

            // remove self from current tile and menagerie
            _myCollider.RemoveCreature(this);
            _myVisibilityTracker.DeregisterCreature(this);
            this._inhabitedMap.MenagerieDeleteCreatureFrom(this._uniqueID);
            this._myTeam.MemberRemove(this);
            // clear brain/ memory
            this._creatureBrain = null;
        }

        #endregion

        #region Constructors

        // Creates the creature at startPos on the Map
        public Creature(Map currentMap, SpriteBatchCreature myBitmap, Coords startPos, UInt32 ID, UInt16 sightRange, Brain mind)
        {
            this._myInventory = new Inventory(this);
            this._myBitmap = myBitmap;
            this._radiusX = Constants.StandardUnitRadiusX;
            this._radiusY = Constants.StandardUnitRadiusY;

            this._statSightRange = sightRange;
            this._inhabitedMap = currentMap;
            this._myCollider = _inhabitedMap.MyCollider;
            this._myVisibilityTracker = _inhabitedMap.MyVisibilityTracker;
            this._myPathfinder = _inhabitedMap.MyPathfinder;

            this._uniqueID = ID;
            this._inhabitedMap.MenagerieAddCreatureTo(ID, this);

            this._creatureBrain = mind;
            mind.MyCreature = this;

            this._fieldOfView = new BitArray[currentMap.BoundX];
            for (int i = 0; i < currentMap.BoundX; ++i)
            {
                _fieldOfView[i] = new BitArray(currentMap.BoundY);
            }

            Tile temp = this.InhabitedMap.GetTile(startPos);
            this.PositionDouble = new Vector(temp.PixelTopLeft() + new Coords(CoordsType.Pixel, _radiusX, _radiusY));

            /*
            this.MyBody = new UInt16[] {Constants.StatsMax, Constants.StatsMax, Constants.StatsMax, Constants.StatsMax,
                Constants.StatsMax, Constants.StatsMax};
            */

            this._inhabitedMap.MyCollider.RegisterCreature(this);

            this._moveSpeedCurrent = 0;
        }

        public Creature(Map currentMap, Coords startPos, UInt16 ID, Team team, CreatureGenerator generator)
        {
            this._name = generator.name;
            this._myTeam = team;

            this._myInventory = new Inventory(this);
            this._myBitmap = generator.creatureBitmaps;
            this._radiusX = Constants.StandardUnitRadiusX;
            this._radiusY = Constants.StandardUnitRadiusY;

            this._inhabitedMap = currentMap;
            this._uniqueID = ID;
            this._inhabitedMap.MenagerieAddCreatureTo(ID, this);

            this._myCollider = _inhabitedMap.MyCollider;
            this._myVisibilityTracker = _inhabitedMap.MyVisibilityTracker;
            this._myPathfinder = _inhabitedMap.MyPathfinder;

            this._creatureBrain = new BrainRandomWalk();
            _creatureBrain.MyCreature = this;

            this._fieldOfView = new BitArray[currentMap.BoundX];
            for (int i = 0; i < currentMap.BoundX; ++i)
            {
                _fieldOfView[i] = new BitArray(currentMap.BoundY);
            }

            _statHPMax = generator.StatHPMax;
            _statHP = _statHPMax;
            _statDamage = generator.StatDamage;
            _statArmor = generator.StatArmor;
            _statSightRange = generator.StatSight;
            _statAttackTime = generator.StatAttackTime;
            _statAttackRange = generator.StatAttackRange;

            _moveSpeedMax = generator.StatMoveSpeed;
            this._moveSpeedCurrent = 0;
            _moveAcceleration = _moveSpeedMax / 10;

            Tile temp = this._inhabitedMap.GetTile(startPos);
            this.PositionDouble = new Vector(temp.PixelTopLeft() + new Coords(CoordsType.Pixel, _radiusX, _radiusY));

            this._inhabitedMap.MyCollider.RegisterCreature(this);
        }

        #endregion

    }

}
