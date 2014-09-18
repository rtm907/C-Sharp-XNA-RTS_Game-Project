using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace RTS_Game
{
    // This class contains the global constants
    public static class Constants
    {
        // VERSION: v0.002

        #region Game - related

        public static UInt16 defaultTimerPeriod = 20;

        public static UInt16 redrawPeriod = 40; // 25 frames per second

        public static Int32 defaultMapRandomatorSeed = 907;

        public static bool RecordInput = false;

        // Moving in a diagonal direction takes root(2) time (with movement left/right/top/bottom
        // taking one unit time).
        public static float diagonalCoefficient = (float)Math.Sqrt(2);

        public static float IsometricCoefficient = (float)(0.5 * Math.Sqrt(3));

        public static float IsometricTransformEllipseX = (float)(0.5 * Math.Sqrt(6));

        public static float IsometricTransformEllipseY = (float)(0.5 * Math.Sqrt(2));

        public static float[] MovementCost =
        { Constants.diagonalCoefficient, 1, 
           Constants.diagonalCoefficient, 1, 
           Constants.diagonalCoefficient, 1, 
           Constants.diagonalCoefficient, 1
        };

        // This is so I don't have to deal with expanding visibility arrays in the Tile class. FIX later.
        public static UInt32 MaximumNumberOfCreatures = 256;

        #endregion

        #region Drawing - related

        public static ScrollingType Scrolling = ScrollingType.Free;

        public static bool ShowGrid = true;
        public static bool ShowCoordinates = false;
        public static bool ShowMap = false;
        public static bool ShowBoundingCircles = true;
        public static bool ZoomingAllowed = true;

        public static float FreeScrollingSpeed = 20f;

        public static float ZoomSpeed = 0.001f; // use 2^(-10) in hexadecimal, or something
        public static float ZoomMin = 1f;
        public static float ZoomMax = 4f;

        public static Color BoundingCircleColor = Color.Red;
        public static Color SelectionBoxColor = Color.White;

        #endregion

        #region Map - related

        //the size of the smallest grid member in pixels. must be a divisor of _tileSize.
        public static UInt16 TileBitmapSize = 32;

        public static UInt16 TileSizePixels = 32;

        //Size of each tile
        private static UInt16 _tileSize = TileBitmapSize;
        public static UInt16 TileSize
        {
            get
            {
                return _tileSize;
            }
        }

        public static UInt16 StandardUnitRadiusX = (UInt16)(TileSize / 4);
        public static UInt16 StandardUnitRadiusY = (UInt16)(TileSize / 6);

        public static float ZoomDefault = ((float)TileSizePixels / (float)TileBitmapSize);

        // Default map size
        private static UInt16 _mapSize = 32;
        public static UInt16 MapSize
        {
            get
            {
                return _mapSize;
            }
        }

        //Starting position of player on the map
        private static Coords _playerStartPos = new Coords(CoordsType.Tile, 0, 5);
        public static Coords PlayerStartPos
        {
            get
            {
                return _playerStartPos;
            }
        }

        // Max distance threshold for recursive influence generation algorithm (InfluenceSourceMap).
        private static UInt32 _influenceMapMaxDistance = 20;
        public static UInt32 InfluenceMapMaxDistance
        {
            get
            {
                return _influenceMapMaxDistance;
            }
        }

        private static float _influenceMapMinThreshold = (float)Math.Pow(10, (-5));
        public static float InfluenceMapMinThreshold
        {
            get
            {
                return _influenceMapMinThreshold;
            }
        }

        private static float _visibilityTreshold = 0.1f;
        public static float VisibilityTreshold
        {
            get
            {
                return _visibilityTreshold;
            }
        }

        #endregion

        #region Tile - related

        public static TileGenerator TileGeneratorGrass = new TileGenerator(true, "Grass", 1f, SpriteTile.Grass);
        public static TileGenerator TileGeneratorWallStone = new TileGenerator(false, "Stone Wall", 0f, SpriteTile.WallStone);
        public static TileGenerator TileGeneratorFloorDirt = new TileGenerator(true, "Dirt Floor", 1f, SpriteTile.FloorDirt);
        public static TileGenerator TileGeneratorRoadPaved = new TileGenerator(true, "Paved Road", 1f, SpriteTile.RoadPaved);

        #endregion

        #region Item - related

        public static ItemGenerator ItemGeneratorBed = new ItemGenerator("Bed", SpriteItem.Bed, ItemType.StaticUsable,
            new Dictionary<Stimulus, float>() { { Stimulus.Rest, 1f } });

        public static ItemGenerator ItemGeneratorWell = new ItemGenerator("Well", SpriteItem.Well, ItemType.StaticUsable,
            new Dictionary<Stimulus, float>() { { Stimulus.Thirst, 1f } });

        public static ItemGenerator ItemGeneratorTreeApple = new ItemGenerator("Apple Tree", SpriteItem.TreeApple, ItemType.StaticUsable,
            new Dictionary<Stimulus, float>() { { Stimulus.Hunger, 1f } });

        public static ItemGenerator ItemGeneratorToolTable = new ItemGenerator("Tool Table", SpriteItem.ToolTable, ItemType.StaticUsable,
            new Dictionary<Stimulus, float>() { { Stimulus.Work, 1f } });

        #endregion

        #region Creature - related

        public static CreatureGenerator CreatureGeneratorGnome = new CreatureGenerator("Gnome", SpriteBatchCreature.Gnome, 30, 5, 0, 5, 4, 10, 30);

        #endregion

        #region AI - related

        public static double CollisionAvoidanceRotation = Math.PI / 6;

        public static float PathfinderStraightPathCorrection = 0f;

        // 1 = takes 3x3 box centered at agent's PositionTile, 3 = takes 5x5 box, etc.
        public static byte PathfinderFineBoxSize = 1;

        public static float StimulusEvaluationDistanceRedundancyCoefficient = 0.5f;
        public static float StimulusEvaluationDistanceRedundancyPower = 0.5f;

        public static float CurrentLongTermPriorityMultiplier = 1.5f;
        public static float CurrentShortTermPriortyMultiplier = 1.2f;

        public static float MnemosyneNewInformationThreshold = 1.0f;

        public static double TravelTimeOverestimationCoefficient = 2d;
        public static UInt32 FailedCollisionAvoidanceWaitTime = 50;

        public static UInt16 SideStepTimeDefault = 20;

        public static UInt16 DefaultRouteToForcedTargetRecalcTimer = 100;

        #endregion

        #region Strings

        public static Int32 FontSize = 12;

        public static String[] QuipsGreetings = 
        {"Yo, man!",
            "Hello!",
            "How's it going.",
            "Hey.",
            "*nods*"
        };

        public static String[] GnomeNamebits = 
        {"ka", "kri", "kyu", "khe", "ko",
            "sam", "sir", "suk", "sech", "soj",
            "bik", "trom", "shrok", "jem", "kop"
        };

        #endregion


        public static Int32 _navigationSuccesses = 0;
        public static Int32 _navigationFailures = 0;
    }
}
