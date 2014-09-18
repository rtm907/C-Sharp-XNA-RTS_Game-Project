using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace RTS_Game
{
    /// <summary>
    /// Deals with the screen display algo.
    /// </summary>
    public class Painter
    {
        // What the painter draws is a portion of some map.
        private Map _currentMap;
        private MainFrame _mainFrame;

        private float _zoom;
        public float Zoom
        {
            get
            {
                return _zoom;
            }
            set
            {
                _zoom = value;
                RescaleSprites();
            }
        }

        private Bitmap[] DefaultTiles = new Bitmap[(Int32)SpriteTile.COUNT];
        private Bitmap[] DefaultCreatures = new Bitmap[(Int32)SpriteBatchCreature.COUNT];
        private Bitmap[] DefaultItems = new Bitmap[(Int32)SpriteItem.COUNT];

        private Bitmap[] Tiles = new Bitmap[(Int32)SpriteTile.COUNT];
        private Bitmap[] Creatures = new Bitmap[(Int32)SpriteBatchCreature.COUNT];
        private Bitmap[] Items = new Bitmap[(Int32)SpriteItem.COUNT];

        private Bitmap Fog;

        private void FixFogBitmap()
        {
            Bitmap temp = new Bitmap((Int32)(Constants.TileSizePixels * _zoom),
                (Int32)(Constants.TileSizePixels * _zoom));

            Color color = Color.Gray;
            Color TransparentColor = Color.FromArgb(150, color.R, color.G, color.B);
            Graphics g = Graphics.FromImage(temp);

            Rectangle box = new Rectangle(0, 0, temp.Width, temp.Height);
            g.FillRectangle(new SolidBrush(TransparentColor), box);
            g.Dispose();

            this.Fog = temp;
        }

        private void ImportSprites()
        {
            for (sbyte i = 0; i < this.DefaultTiles.Length; ++i)
            {
                Bitmap current = new Bitmap(Bitmaps.Tiles[i]);
                DefaultTiles[i] = current;
            }

            for (sbyte i = 0; i < this.DefaultCreatures.Length; ++i)
            {
                Bitmap current = new Bitmap(Bitmaps.Creatures[i]);
                current.MakeTransparent(current.GetPixel(0, 0));
                DefaultCreatures[i] = current;
            }

            for (sbyte i = 0; i < this.DefaultItems.Length; ++i)
            {
                Bitmap current = new Bitmap(Bitmaps.Items[i]);
                current.MakeTransparent(current.GetPixel(0, 0));

                DefaultItems[i] = current;
            }
        }

        private void RescaleSprites()
        {
            for (sbyte i = 0; i < this.DefaultTiles.Length; ++i)
            {
                Bitmap current = new Bitmap(DefaultTiles[i], (Int32)(Constants.TileSizePixels * _zoom), (Int32)(Constants.TileSizePixels * _zoom));
                Tiles[i] = current;
            }

            for (sbyte i = 0; i < this.DefaultCreatures.Length; ++i)
            {
                Creatures[i] = new Bitmap(DefaultCreatures[i], (Int32)(DefaultCreatures[i].Width * _zoom), (Int32)(DefaultCreatures[i].Height * _zoom));
            }

            for (sbyte i = 0; i < this.DefaultItems.Length; ++i)
            {
                Items[i] = new Bitmap(DefaultItems[i], (Int32)(DefaultItems[i].Width * _zoom), (Int32)(DefaultItems[i].Height * _zoom));
            }

            FixFogBitmap();
        }

        // List of creatures to be painted on a particular tick.
        private List<Creature> _creaturesToDraw;
        public void AddForPaintingCreature(Creature critter)
        {
            this._creaturesToDraw.Add(critter);
        }

        private List<Item> _itemsToDraw;
        public void AddForPaintingItem(Item item)
        {
            this._itemsToDraw.Add(item);
        }

        private void IsometricTransform(Graphics g)
        {
            g.RotateTransform(45f);
            g.ScaleTransform((float)Math.Sqrt(1.5), (float)Math.Sqrt(0.5));
        }

        /// <summary>
        /// Coords the painting of the portion of the map between the two Coords parameters.
        /// </summary>
        public void Paint(Graphics g, Coords topLeft, Coords bottomRight)
        {
            // The algorithm is as follows:
            // The Form determines what portion of the map should be painted and calls Painter.
            // Painter goes through the Tiles and lets them paint themselves. The fogged and 
            // unexplored Tiles are easy. The visible Tiles are responsible for communicating
            // their contents to the Painter. It paints their contents from back to front 
            // (according to the Painter's Algorithm) - first the tile background, then objects
            // such as items or creatures, and last - effects like projectiles and strings.

            // NOTE: Perhaps the algo should be improved so that only tiles that need redrawing
            // get redrawn.

            // NOTE: For smooth-scrolling, consider forcing invalidate whenever the screen moves by a pixel.

            #region Tiles

            // We assume the validity check for the two coords has been done in the caller class.
            for (Int32 i = topLeft.X; i <= bottomRight.X; ++i)
            {
                for (Int32 j = topLeft.Y; j <= bottomRight.Y; ++j)
                {
                    Coords currentCoords = new Coords(CoordsType.Tile, i, j);
                    Tile currentTile = this._currentMap.GetTile(i, j);

                    //bool visibility = _currentMap.MyVisibilityTracker.VisibilityCheck(currentCoords, this._currentMap.PlayerReference);

                    /* Unexplored feature removed; can be added easily.
                    // case Tile is unexplored
                    if (visibility < 0)
                    {
                        this.TileFillRectangle(g, currentCoords, Color.Black, Color.Black);
                    }
                     * */

                    this.TileDrawBitmap(g, currentCoords, this.Tiles[(sbyte)currentTile.MyBitmap]);

                    // fogged
                    if (!Constants.ShowMap && false)//!visibility)
                    {
                        //this.TileDrawFog(g, currentCoords);
                        this.TileDrawBitmap(g, currentCoords, this.Fog);
                    }

                    // visible
                    else //if (visibility > 0)
                    {
                        if (currentTile is TilePassable)
                        {
                            LinkedList<Creature> residents = _currentMap.MyVisibilityTracker.VisibilityResidents(currentCoords);

                            if (residents != null)
                            {
                                foreach (Creature critter in residents)
                                {
                                    this.AddForPaintingCreature(critter);
                                }
                            }
                            foreach (Item someItem in (currentTile as TilePassable).MyInventory.ItemList)
                            {
                                this.AddForPaintingItem(someItem);
                            }
                        }
                    }


                    if (Constants.ShowGrid)
                    {
                        this.TileDrawRectangle(g, currentCoords);
                    }
                    if (Constants.ShowCoordinates)
                    {
                        this.TileDrawCoordinates(g, currentCoords);
                    }
                }
            }

            #endregion

            #region Items and Creatures
            // The tiles have informed the painter about the creatures he's supposed to draw.
            foreach (Item item in this._itemsToDraw)
            {
                this.TileDrawBitmap(g, item.Position().Value, this.Items[(sbyte)item.ItemBitmap]);
            }

            // The tiles have informed the painter about the creatures he's supposed to draw.
            foreach (Creature critter in this._creaturesToDraw)
            {
                // draw bounding circles for the agents
                if (Constants.ShowBoundingCircles)
                {
                    this.DrawEllipseAtPixel(g, critter.PositionPixel, critter.RadiusX, critter.RadiusY, critter.Team.TeamColor);
                }
                if (critter.Selected)
                {
                    this.DrawEllipseAtPixel(g, critter.PositionPixel, critter.RadiusX, critter.RadiusY, Constants.SelectionBoxColor);
                }

                this.DrawBitmapAtPixel(g, new Coords(CoordsType.Pixel, critter.PositionPixel.X, critter.PositionPixel.Y),
                    this.Creatures[(sbyte)critter.MyBitmap]);

                /*
                // draw waypoints for the agents
                if (critter.CreatureBrain.MyNavigator != null)
                {
                    if (critter.CreatureBrain.MyNavigator._visiblePixelGoal != null)
                    {
                        this.DrawEllipseAtPixel(g, critter.CreatureBrain.MyNavigator._visiblePixelGoal.Value, 4, 4);
                    }
                }
                */
            }

            // Draw the labels
            foreach (Creature critter in this._creaturesToDraw)
            {
                //FIX
                // draw labels
                if ((critter.LabelUpper != null) && (critter.LabelUpper.Length > 0))
                {
                    this.DrawLabel(g, new Coords(CoordsType.Pixel, critter.PositionPixel.X, critter.PositionPixel.Y - critter.RadiusY), critter.LabelUpper);
                }

                if ((critter.LabelLower != null) && (critter.LabelLower.Length > 0))
                {
                    this.DrawLabel(g, new Coords(CoordsType.Pixel, critter.PositionPixel.X, critter.PositionPixel.Y + critter.RadiusY), critter.LabelLower);
                }
            }

            #endregion

            DrawSelectionBox(g);

            // Clean up the ID list.
            this._creaturesToDraw.Clear();
            this._itemsToDraw.Clear();
        }

        private RectangleF GetRectangle(Coords positionTile)
        {
            return new RectangleF(positionTile.X * Constants.TileBitmapSize * this._zoom, positionTile.Y * Constants.TileBitmapSize * this._zoom,
                (Constants.TileBitmapSize) * this._zoom, (Constants.TileBitmapSize) * this._zoom);
        }

        private void DrawSelectionBox(Graphics g)
        {
            Nullable<Coords> selectionBoxAnchor = _mainFrame.SelectionBoxAnchor;
            if (selectionBoxAnchor == null)
            {
                // no selection box to draw
                return;
            }

            Coords mousePointer = _mainFrame.PixelUnderMousecursor();

            Int32 gridXMin = Math.Min(selectionBoxAnchor.Value.X, mousePointer.X);
            Int32 gridXMax = Math.Max(selectionBoxAnchor.Value.X, mousePointer.X);
            Int32 gridYMin = Math.Min(selectionBoxAnchor.Value.Y, mousePointer.Y);
            Int32 gridYMax = Math.Max(selectionBoxAnchor.Value.Y, mousePointer.Y);

            gridXMin = Math.Max(0, gridXMin);
            gridXMax = Math.Min((Int32)_currentMap.PixelBoundX, gridXMax);
            gridYMin = Math.Max(0, gridYMin);
            gridYMax = Math.Min((Int32)_currentMap.PixelBoundY, gridYMax);

            Pen drawPen = new Pen(Constants.SelectionBoxColor);
            g.DrawRectangle(drawPen, gridXMin * _zoom, gridYMin * _zoom, (gridXMax - gridXMin) * _zoom, (gridYMax - gridYMin) * _zoom);
            drawPen.Dispose();
        }

        #region Tile drawers

        // Draw tile bitmap
        private void TileDrawBitmap(Graphics g, Coords position, Bitmap image)
        {
            float multiplier = Constants.TileSizePixels * _zoom;

            Point anchor = new Point((Int32)(position.X * multiplier), (Int32)(position.Y * multiplier));
            g.DrawImageUnscaled(image, anchor);
        }

        // Used for displaying the grid.
        private void TileDrawRectangle(Graphics graphicsObj, Coords position)
        {
            RectangleF box = GetRectangle(position);
            graphicsObj.DrawRectangle(new Pen(Color.Black), box.X, box.Y, box.Width, box.Height);
        }

        private void TileDrawCoordinates(Graphics g, Coords position)
        {
            StringFormat strFormat = new StringFormat();
            strFormat.Alignment = StringAlignment.Center;


            g.DrawString(position.ToString(), new Font("Tahoma", Constants.FontSize / 2), Brushes.Black,
                new PointF((position.X + 0.5f) * Constants.TileBitmapSize * this._zoom, (position.Y + 0.5f) *
                    Constants.TileBitmapSize * this._zoom), strFormat);
        }

        #endregion

        #region Pixel drawers

        private void DrawBitmapAtPixel(Graphics g, Coords pixel, Bitmap image)
        {
            // This should be at the middle of the bottom of the image.
            Point anchor = new Point((Int32)(pixel.X * this._zoom - 0.5f * image.Width), (Int32)(pixel.Y * this._zoom - image.Height));
            g.DrawImageUnscaled(image, anchor);
        }

        private void DrawEllipseAtPixel(Graphics g, Coords pixel, UInt16 radiusX, UInt16 radiusY, Color givenColor)
        {
            g.DrawEllipse(new Pen(givenColor), (pixel.X - radiusX) * this._zoom, (pixel.Y - radiusY) * this._zoom,
                radiusX * 2 * this._zoom, radiusY * 2 * this._zoom);
        }

        #endregion

        #region LabelDrawers

        // Draws an upper label on a tile (or rather creature). Should make it 
        // nicer later with color choice, etc.
        private void DrawLabel(Graphics graphicsObj, Coords position, String label)
        {
            StringFormat strFormat = new StringFormat();
            strFormat.Alignment = StringAlignment.Center;

            graphicsObj.DrawString(label, new Font("Tahoma", Constants.FontSize), Brushes.Black,
                new PointF(position.X * this._zoom, position.Y * this._zoom), strFormat);
        }

        #endregion

        public Painter(MainFrame frame, Map assignedMap, float zoom)
        {
            this._zoom = zoom;
            this._currentMap = assignedMap;
            this._mainFrame = frame;
            assignedMap.MyPainter = this;
            this._creaturesToDraw = new List<Creature>();
            this._itemsToDraw = new List<Item>();

            this.ImportSprites();
            this.RescaleSprites();
        }
    }

}
