using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace RTS_Game
{
    public static class Bitmaps
    {
        public static string HomeDirectory = @"C:\Users\rado\Documents\Visual Studio 2010\Projects\RTS_Game\RTS_Game\";

        #region Tiles

        public static String[] Tiles = new String[] 
        { 
        Bitmaps.HomeDirectory + @"Tiles\TileGrass.bmp",
        Bitmaps.HomeDirectory + @"Tiles\TileFloorDirt.bmp",
        Bitmaps.HomeDirectory + @"Tiles\TileRoadPaved.bmp",
        Bitmaps.HomeDirectory + @"Tiles\TileWallStone.bmp"
        };

        #endregion

        #region Items

        public static String[] Items = new String[] 
        { 
        Bitmaps.HomeDirectory + @"Items\ItemBed.bmp",
        Bitmaps.HomeDirectory + @"Items\ItemToolTable.bmp",
        Bitmaps.HomeDirectory + @"Items\ItemTreeApple.bmp",
        Bitmaps.HomeDirectory + @"Items\ItemWell.bmp"
        };

        #endregion

        #region Creatures

        public static String[] Creatures = new String[] 
        { 
        Bitmaps.HomeDirectory + @"Creatures\Player.bmp",
        Bitmaps.HomeDirectory + @"Creatures\Gnome.bmp"
        };

        #endregion
    }
}
