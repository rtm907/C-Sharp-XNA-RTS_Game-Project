using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RTS_Game
{
    // Form class. Deals with the window the game plays in, takes input, etc.
    // Should move some of the code here to a 'Game' class
    public partial class MainFrame : Form
    {
        private Map _currentMap;
        //private Player _player;
        private Timer _timer;
        private Scheduler _scheduler;
        private UInt16 _invalidateCounter = 0;
        //private Graphics graphicsObj;
        private PointF _screenAnchor;
        private Painter _painter;

        private Ledger _ledger;

        private float _zoom;
        private UInt64 _tickCounter = 0;

        //protected Timer MouseHeldTimer;
        protected bool MouseRightButtonIsDown;
        protected bool MouseLeftButtonIsDown;

        private List<Keys> _pressedKeys = new List<Keys>();

        private List<Creature> _selectedCreatures = new List<Creature>();
        public List<Creature> SelectedCreatures
        {
            get
            {
                return _selectedCreatures;
            }
        }

        private Nullable<Coords> _selectionBoxAnchor;
        public Nullable<Coords> SelectionBoxAnchor
        {
            get
            {
                return _selectionBoxAnchor;
            }
        }

        private Vector SelectedCreaturesAveragePosition()
        {
            double xval = 0;
            double yval = 0;

            for (int i = 0; i < _selectedCreatures.Count; ++i)
            {
                xval += _selectedCreatures[i].PositionDouble.X;
                yval += _selectedCreatures[i].PositionDouble.Y;
            }

            return new Vector(xval / _selectedCreatures.Count, yval / _selectedCreatures.Count);
        }

        public MainFrame()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            // reduce flicker
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);

            this.MouseWheel += new MouseEventHandler(MainFrame_MouseWheel);

            this.MouseRightButtonIsDown = false;
        }

        private void MainFrame_Load(object sender, System.EventArgs e)
        {
            _timer = new Timer();
            _timer.Interval = Constants.defaultTimerPeriod;
            _timer.Tick += new System.EventHandler(MainFrame_Tick);

            _zoom = Constants.ZoomDefault;

            WorldGeneration generator = new WorldGeneration(907);

            _currentMap = generator.GenerateVillage(Constants.MapSize, Constants.MapSize);
            _painter = new Painter(this, _currentMap, _zoom);

            _currentMap.AnalyzeTileAccessibility();

            Team team1 = new Team(Color.Red);
            Team team2 = new Team(Color.Blue);

            for (int j = 0; j < 2; ++j)
            {
                for (int i = 0; i < 5; ++i)
                {
                    _currentMap.SpawnCreature(new Coords(CoordsType.Tile, i, j), team1, Constants.CreatureGeneratorGnome);
                }
            }

            for (int j = 0; j < 2; ++j)
            {
                for (int i = 0; i < 5; ++i)
                {
                    _currentMap.SpawnCreature(new Coords(CoordsType.Tile, i, _currentMap.BoundY-1-j), team2, Constants.CreatureGeneratorGnome);
                }
            }

            _currentMap.SpawnPlayer(Constants.PlayerStartPos);
            //_player = _currentMap.PlayerReference;

            //_screenAnchor = this.TransformCenterAtPlayer();

            _scheduler = new Scheduler(_currentMap);
            _ledger = new Ledger(_scheduler);
            _timer.Start();
        }

        /*
        // Returns the transform that centers the form on the player
        private PointF TransformCenterAtPlayer()
        {
            PointF topDownPoint = new PointF(_player.PositionPixel.X * this._zoom, this._player.PositionPixel.Y * this._zoom);
            return new PointF(0.5f * this.Width - topDownPoint.X, 0.5f * this.Height - topDownPoint.Y);
        }
        */

        // Returns the coords of the clicked tile assuming the center of the screen is the 
        // player's position
        private Coords TransformInverseScreenpointToCoords(Int32 x, Int32 y, CoordsType type)
        {
            // maybe should store the translation instead of recalcualting it
            float xf = (x - _screenAnchor.X) / this._zoom;
            float yf = (y - _screenAnchor.Y) / this._zoom;

            Int32 xcoord = (Int32)(Math.Floor(xf));
            Int32 ycoord = (Int32)(Math.Floor(yf));

            if (type == CoordsType.Tile)
            {
                return new Coords(type, xcoord / Constants.TileSize, ycoord / Constants.TileSize);
            }

            return new Coords(type, xcoord, ycoord);
        }

        public Coords PixelUnderMousecursor()
        {
            Point mousePosition = this.PointToClient(Cursor.Position);
            return TransformInverseScreenpointToCoords(mousePosition.X, mousePosition.Y, CoordsType.Pixel);
        }

        // The drawing routine should be like this:
        // Check which tiles are visible and then analyze them. Have the tiles return delegate drawers
        // with various levels: the tiles have the lowest level, then items, then creatures, then
        // the creature strings, etc.
        // Right now I draw stuff tile-by-tile, which will obviously cause artifacts for objects taking
        // than one tile (such as strings).
        private void MainFrame_Paint(object sender, PaintEventArgs e)
        {

            Graphics graphicsObj = e.Graphics;

            graphicsObj.TranslateTransform(_screenAnchor.X, _screenAnchor.Y);

            // Find the coords of the tile lying under the (0,0) Form pixel
            Coords topLeft = TransformInverseScreenpointToCoords(0, 0, CoordsType.Tile);
            // Bounds correction:
            topLeft = new Coords(CoordsType.Tile, Math.Max(0, topLeft.X), Math.Max(0, topLeft.Y));

            Coords bottomRight = TransformInverseScreenpointToCoords(this.Width, this.Height, CoordsType.Tile);
            // Bounds correction:
            bottomRight = new Coords(CoordsType.Tile, Math.Min(bottomRight.X, _currentMap.BoundX - 1),
                Math.Min(bottomRight.Y, _currentMap.BoundY - 1));

            this._currentMap.MyPainter.Paint(graphicsObj, topLeft, bottomRight);

            //graphicsObj.Dispose();
        }

        private void MainFrame_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                this.HandlerPauseUnpause();
                return;
            }

            if (!_pressedKeys.Contains(e.KeyCode))
            {
                _pressedKeys.Add(e.KeyCode);
            }

            e.Handled = true;
        }

        private void MainFrame_KeyUp(object sender, KeyEventArgs e)
        {
            _pressedKeys.Remove(e.KeyCode);
            e.Handled = true;
        }

        private void MainFrame_Tick(object source, EventArgs e)
        {
            if (_pressedKeys.Count > 0)
            {
                this.HandlerKeysHeld();
            }

            if (MouseRightButtonIsDown)
            {
                this.HandlerMouseRightButtonHeld();
            }

            this._scheduler.Update();

            this._invalidateCounter = (UInt16)((_invalidateCounter + Constants.defaultTimerPeriod) % Constants.redrawPeriod);
            if (_invalidateCounter <= Constants.defaultTimerPeriod)
            {
                this.Invalidate();
            }

            ++this._tickCounter;
        }

        private void MainFrame_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.MouseRightButtonIsDown = true;
            }
            else if (e.Button == MouseButtons.Left)
            {
                this.MouseLeftButtonIsDown = true;
                _selectionBoxAnchor = PixelUnderMousecursor();
            }
        }

        private void MainFrame_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.MouseRightButtonIsDown = false;

                HandlerMouseRightClick();
            }
            else if (e.Button == MouseButtons.Left)
            {
                this.MouseLeftButtonIsDown = false;

                HandlerMouseLeftClick();
            }
            // Add button-click contextual handling here.
        }

        private void MainFrame_MouseWheel(object sender, MouseEventArgs e)
        {
            this.HandlerMousewheel(e);
        }

        private void HandlerKeysHeld()
        {
            foreach (Keys key in _pressedKeys)
            {
                if (Constants.Scrolling == ScrollingType.Free)
                {
                    switch (key)
                    {
                        case Keys.Right:
                            _screenAnchor = new PointF(Math.Max(-this._currentMap.PixelBoundX * this._zoom + this.Width - Constants.TileBitmapSize, _screenAnchor.X - Constants.FreeScrollingSpeed), _screenAnchor.Y);
                            break;
                        case Keys.Left:
                            _screenAnchor = new PointF(Math.Min(0, _screenAnchor.X + Constants.FreeScrollingSpeed), _screenAnchor.Y);
                            break;
                        case Keys.Down:
                            _screenAnchor = new PointF(_screenAnchor.X, Math.Max(-this._currentMap.PixelBoundY * this._zoom + this.Height - Constants.TileBitmapSize, _screenAnchor.Y - Constants.FreeScrollingSpeed));
                            break;
                        case Keys.Up:
                            _screenAnchor = new PointF(_screenAnchor.X, Math.Min(0, _screenAnchor.Y + Constants.FreeScrollingSpeed));
                            break;
                    }
                }
            }
        }

        private void HandlerMouseRightButtonHeld()
        {
            //Point mousePosition = this.PointToClient(Cursor.Position);

        }

        private void HandlerPauseUnpause()
        {
            this._timer.Enabled = (!this._timer.Enabled);
        }

        private void HandlerMousewheel(MouseEventArgs e)
        {
            if (Constants.ZoomingAllowed)
            {
                this._zoom = Math.Min(Math.Max(Constants.ZoomMin, _zoom + e.Delta * Constants.ZoomSpeed), Constants.ZoomMax);
                this._painter.Zoom = _zoom;
            }
        }

        private void HandlerMouseRightClick()
        {
            Coords clicked = PixelUnderMousecursor();

            Vector averagePosition= new Vector();
            if (_selectedCreatures.Count > 0)
            {
                averagePosition = SelectedCreaturesAveragePosition();
            }
            Coords averagePositionCoords = new Coords(CoordsType.Pixel, averagePosition);

            foreach (Creature c in _selectedCreatures)
            {
                if (!c.Dead)
                {
                    Coords delta = c.PositionPixel - averagePositionCoords;
                    c.CreatureBrain.OrderMove(clicked + delta);
                }
            }
        }

        private void HandlerMouseLeftClick()
        {
            Coords clicked = PixelUnderMousecursor();

            Coords selectionBoxTopLeft =
                new Coords(CoordsType.Pixel, Math.Min(_selectionBoxAnchor.Value.X, clicked.X), Math.Min(_selectionBoxAnchor.Value.Y, clicked.Y));
            Coords selectionBoxBottomRight =
                new Coords(CoordsType.Pixel, Math.Max(_selectionBoxAnchor.Value.X, clicked.X), Math.Max(_selectionBoxAnchor.Value.Y, clicked.Y));

            // deselect old selection
            foreach (Creature selected in _selectedCreatures)
            {
                selected.Selected = false;
            }

            _selectedCreatures = _currentMap.MyCollider.CreaturesInSelectionBox(selectionBoxTopLeft, selectionBoxBottomRight);
            _selectionBoxAnchor = null;

            // select new selection
            foreach (Creature selected in _selectedCreatures)
            {
                selected.Selected = true;
            }
        }
    }
}
