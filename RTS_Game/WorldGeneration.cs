using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Drawing;

namespace RTS_Game
{
    public class WorldGeneration
    {
        private RandomStuff _randomator;

        public Map GenerateVillage(UInt16 width, UInt16 height)
        {
            // We need a reasonable amount of space to construct the map
            if ((width < 20) || (height < 20))
            {
                return null;
            }

            // Initializes map (currently to an all-grass space);
            Map canvas = new Map(width, height, (Int32)_randomator.NSidedDice(1000, 1));
            // Initializes a double array where we keep track of which areas of the map are complete
            //BitArray[,] painted = new BitArray[width, height];
            BitArray[] painted = new BitArray[width];
            for (int i = 0; i < width; ++i)
            {
                painted[i] = new BitArray(height);
            }

            // Put roads through the middle of the map; later we'll improve the algo
            UInt16 halfwidth = (UInt16)(width * 0.5);
            FillRectangleWithTiles(canvas, new Coords(CoordsType.Tile, halfwidth, 0), new Coords(CoordsType.Tile, halfwidth, height - 1), Constants.TileGeneratorRoadPaved);
            for (int i = 0; i < height; ++i)
            {
                painted[halfwidth][i] = true;
            }

            UInt16 halfheight = (UInt16)(height * 0.5);
            FillRectangleWithTiles(canvas, new Coords(CoordsType.Tile, 0, halfheight), new Coords(CoordsType.Tile, width - 1, halfheight), Constants.TileGeneratorRoadPaved);
            for (int i = 0; i < width; ++i)
            {
                painted[i][halfheight] = true;
            }

            // Put a house or two
            GenerateRectangularRoom(canvas, new Coords(CoordsType.Tile, halfwidth + 1, 10), new Coords(CoordsType.Tile, halfwidth + 5, 15));
            FurnishRectangularLivingRoom(canvas, new Coords(CoordsType.Tile, halfwidth + 1, 10), new Coords(CoordsType.Tile, halfwidth + 5, 15));
            GenerateRectangularRoom(canvas, new Coords(CoordsType.Tile, 6, halfheight - 5), new Coords(CoordsType.Tile, 12, halfheight - 1));
            FurnishRectangularWorkshop(canvas, new Coords(CoordsType.Tile, 6, halfheight - 5), new Coords(CoordsType.Tile, 12, halfheight - 1));

            // put a well somewhere
            canvas.CreateItem(new Coords(CoordsType.Tile, 20, 20), Constants.ItemGeneratorWell);

            // Apple tree!
            canvas.CreateItem(new Coords(CoordsType.Tile, 30, 5), Constants.ItemGeneratorTreeApple);

            canvas.AnalyzeTileAccessibility();

            return canvas;
        }

        // Generates a rectangular room
        private void GenerateRectangularRoom(Map homeMap, Coords topLeft, Coords bottomRight)
        {
            Coords difference = bottomRight - topLeft;
            if (!(difference.X > 1 && difference.Y > 1))
            {
                return;
            }

            // Walls
            FillRectangleWithTiles(homeMap, topLeft, new Coords(CoordsType.Tile, topLeft.X, bottomRight.Y), Constants.TileGeneratorWallStone);
            FillRectangleWithTiles(homeMap, topLeft, new Coords(CoordsType.Tile, bottomRight.X, topLeft.Y), Constants.TileGeneratorWallStone);
            FillRectangleWithTiles(homeMap, new Coords(CoordsType.Tile, topLeft.X, bottomRight.Y), bottomRight, Constants.TileGeneratorWallStone);
            FillRectangleWithTiles(homeMap, new Coords(CoordsType.Tile, bottomRight.X, topLeft.Y), bottomRight, Constants.TileGeneratorWallStone);

            // Floor
            this.FillRectangleWithTiles(homeMap, new Coords(CoordsType.Tile, topLeft.X + 1, topLeft.Y + 1),
                new Coords(CoordsType.Tile, bottomRight.X - 1, bottomRight.Y - 1), Constants.TileGeneratorFloorDirt);

            // Open door. For now by default door is in the top-left corner.
            Coords doorSpot = new Coords(CoordsType.Tile, topLeft.X + 1, topLeft.Y);
            homeMap.SetTile(doorSpot, new TilePassable(homeMap, doorSpot, Constants.TileGeneratorFloorDirt));
        }

        // Fills a space with tiles of "tileType"
        private void FillRectangleWithTiles(Map homeMap, Coords topLeft, Coords bottomRight, TileGenerator tileType)
        {
            Coords difference = bottomRight - topLeft;
            if (!(difference.X > -1 && difference.Y > -1))
            {
                return;
            }

            // There should be a more elegant way of dealing with the "Is it passable or impassable?" problem.
            if (tileType.passable)
            {
                for (int i = 0; i < difference.X + 1; ++i)
                {
                    for (int j = 0; j < difference.Y + 1; ++j)
                    {
                        Coords currentCoords = new Coords(CoordsType.Tile, topLeft.X + i, topLeft.Y + j);
                        homeMap.SetTile(currentCoords, new TilePassable(homeMap, currentCoords, tileType));
                    }
                }
            }
            else
            {
                for (int i = 0; i < difference.X + 1; ++i)
                {
                    for (int j = 0; j < difference.Y + 1; ++j)
                    {
                        Coords currentCoords = new Coords(CoordsType.Tile, topLeft.X + i, topLeft.Y + j);
                        homeMap.SetTile(currentCoords, new TileImpassable(homeMap, currentCoords, tileType));
                    }
                }
            }
        }

        // Furnishes a rectangular room with appropriate furniture
        private void FurnishRectangularLivingRoom(Map homeMap, Coords topLeft, Coords bottomRight)
        {
            // put a bed in the middle of it
            Coords bedLocation = new Coords(CoordsType.Tile, (Int32)((bottomRight.X + topLeft.X) * 0.5), (Int32)((bottomRight.Y + topLeft.Y) * 0.5));
            homeMap.CreateItem(bedLocation, Constants.ItemGeneratorBed);
        }

        // Furnishes a rectangular room with appropriate furniture
        private void FurnishRectangularWorkshop(Map homeMap, Coords topLeft, Coords bottomRight)
        {
            // put a bed in the middle of it
            Coords toolTableLocation = new Coords(CoordsType.Tile, (Int32)((bottomRight.X + topLeft.X) * 0.5), (Int32)((bottomRight.Y + topLeft.Y) * 0.5));
            homeMap.CreateItem(toolTableLocation, Constants.ItemGeneratorToolTable);
        }

        public WorldGeneration(Int32 seed)
        {
            this._randomator = new RandomStuff(seed);
        }
    }
}
