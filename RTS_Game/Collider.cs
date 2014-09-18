using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace RTS_Game
{
    /// <summary>
    /// Tracks collisions between agents and terrain on a map.
    /// </summary>
    public class Collider
    {
        private UInt16 _sizeX;
        public UInt16 SizeX
        {
            get
            {
                return _sizeX;
            }
        }

        private UInt16 _sizeY;
        public UInt16 SizeY
        {
            get
            {
                return _sizeY;
            }
        }

        private UInt16 _pixelsPerBoxX;
        private UInt16 _pixelsPerBoxY;

        private BitArray[] _passabilityMap;
        public BitArray[] PassabilityMap
        {
            get
            {
                return _passabilityMap;
            }
            set
            {
                _passabilityMap = value;
            }
        }

        #region collision / bounds / etc -checkers

        private bool CoordsPassable(Coords c)
        {
            return _passabilityMap[c.X][c.Y];
        }

        private Coords VertexTopLeft(Coords box)
        {
            return new Coords(_pixelsPerBoxX * box.X, _pixelsPerBoxY * box.Y);
        }
        private Coords VertexTopRight(Coords box)
        {
            return new Coords(_pixelsPerBoxX * (box.X + 1) - 1, _pixelsPerBoxY * box.Y);
        }
        private Coords VertexBottomLeft(Coords box)
        {
            return new Coords(_pixelsPerBoxX * box.X, _pixelsPerBoxY * (box.Y + 1) - 1);
        }
        private Coords VertexBottonRight(Coords box)
        {
            return new Coords(_pixelsPerBoxX * (box.X + 1) - 1, _pixelsPerBoxY * (box.Y + 1) - 1);
        }

        private List<Creature>[,] _clippingBoxes;
        private SortedList<Creature, LinkedList<Coords>> _occupiedBoxes = new SortedList<Creature, LinkedList<Coords>>();

        /// <summary>
        /// Register creature in the Collider.
        /// </summary>
        public void RegisterCreature(Creature someGuy)
        {
            LinkedList<Coords> steppedOn = TilesCoveredByEllipse(new Coords(CoordsType.Pixel, someGuy.PositionDouble), someGuy.RadiusX, someGuy.RadiusY);

            foreach (Coords c in steppedOn)
            {
                this.ClippingBoxesAddTo(c, someGuy);
            }

            _occupiedBoxes.Add(someGuy, steppedOn);
        }

        /// <summary>
        /// Remove a creature from the Collider database.
        /// </summary>
        public void RemoveCreature(Creature someGuy)
        {
            //LinkedList<Coords> steppedOn = TilesCoveredByEllipse(new Coords(CoordsType.Pixel, someGuy.PositionDouble), someGuy.RadiusX, someGuy.RadiusY);

            foreach (Coords c in _occupiedBoxes[someGuy])
            {
                this.ClippingBoxesRemoveFrom(c, someGuy);
            }

            _occupiedBoxes.Remove(someGuy);
        }

        /// <summary>
        /// Updates the tiles of the grid which an agent covers.
        /// </summary>
        public void UpdateCreatureBoxes(Creature someGuy, Vector oldPosition)
        {
            Coords oldCoords = new Coords((Int32)(oldPosition.X / _pixelsPerBoxX), (Int32)(oldPosition.Y / _pixelsPerBoxY));
            Coords newCoords = new Coords((Int32)(someGuy.PositionDouble.X / _pixelsPerBoxX), (Int32)(someGuy.PositionDouble.Y / _pixelsPerBoxY));

            if (oldCoords == newCoords)
            {
                // nothing to do
                return;
            }

            LinkedList<Coords> steppedOn = TilesCoveredByEllipse(new Coords(CoordsType.Pixel, someGuy.PositionDouble), someGuy.RadiusX, someGuy.RadiusY);
            foreach (Coords c in _occupiedBoxes[someGuy])
            {
                this.ClippingBoxesRemoveFrom(c, someGuy);
            }
            foreach (Coords c in steppedOn)
            {
                this.ClippingBoxesAddTo(c, someGuy);
            }

            _occupiedBoxes[someGuy] = steppedOn;
        }

        /// <summary>
        /// Handles collisions for an agent.
        /// </summary>
        public void HandleCollisions(Creature someGuy, Vector oldPosition)
        {
            this.UpdateCreatureBoxes(someGuy, oldPosition);

            List<Creature> collisions = this.PotentialCreatureToCreatureCollision(someGuy, new Coords(CoordsType.Pixel, someGuy.PositionDouble));

            for (int i = 0; i < collisions.Count; ++i)
            {
                this.HandleCollisionBetweenTwoPlayers(someGuy, collisions[i]);
            }
        }

        /// <summary>
        /// Handles a collision between two agents.
        /// </summary>
        private void HandleCollisionBetweenTwoPlayers(Creature first, Creature second)
        {
            double distance = first.PositionDouble.DistanceTo(second.PositionDouble);
            double preferredDistance = Math.Sqrt(Math.Pow(first.RadiusX + second.RadiusX, 2) + Math.Pow(first.RadiusY + second.RadiusY, 2));

            double deltaOver2 = 0.5 * (preferredDistance - distance);

            if (deltaOver2 < 0)
            {
                return;
            }

            // push both actors in opposite direction, by a vector of length delta/2.
            Vector mover = first.PositionDouble - second.PositionDouble;
            mover.ScaleToLength(deltaOver2);
            first.PositionDouble += mover;
            second.PositionDouble -= mover;

            // this might have cause other collisions. the algo needs fixing.
        }

        /// <summary>
        /// Does a clip check on an agent. Returns the type of collision.
        /// 'null' - terrain collision. 'empty list' - no collision.
        /// </summary>
        public List<Creature> CreatureClippingCheck(Creature critter, Coords potentialPosition, bool creaturesClipCheck)
        {
            // obtain new entry tiles
            LinkedList<Coords> newEntries = this.TilesCoveredByEllipse(potentialPosition, critter.RadiusX, critter.RadiusY);

            // we've hit a tileImpassable or out of bounds
            if (newEntries == null)
            {
                return null;
            }

            List<Creature> returnVal = new List<Creature>();
            // if the flags demands it, check if there is a creature in the way
            if (creaturesClipCheck)
            {
                returnVal = this.PotentialCreatureToCreatureCollision(critter, potentialPosition);
            }

            return returnVal;
        }

        /// <summary>
        /// Adds a creature to the list of tenant creatures of a tile of the grid.
        /// </summary>
        public void ClippingBoxesAddTo(Coords box, Creature newGuy)
        {
            _clippingBoxes[box.X, box.Y].Add(newGuy);
        }

        /// <summary>
        /// Removes a creature from the list of tenants of a tile of the grid.
        /// </summary>
        public void ClippingBoxesRemoveFrom(Coords box, Creature removeMe)
        {
            _clippingBoxes[box.X, box.Y].Remove(removeMe);
        }

        /// <summary>
        ///  Returns Creatures overlapped by agent.
        /// </summary>
        public List<Creature> PotentialCreatureToCreatureCollision(Creature critter, Coords potentialPosition)
        {
            List<Creature> returnVal = new List<Creature>();

            UInt16 critterRadiusX = critter.RadiusX;
            UInt16 critterRadiusY = critter.RadiusY;

            LinkedList<Coords> checkList = this.TilesCoveredByEllipse(potentialPosition, critterRadiusX, critterRadiusY);

            foreach (Coords checkme in checkList)
            {
                foreach (Creature obstacle in _clippingBoxes[checkme.X, checkme.Y])
                {
                    // ignore self
                    if (obstacle == critter)
                    {
                        continue;
                    }

                    if (CollisionCheckEllipses(potentialPosition, critterRadiusX, critterRadiusY,
                        new Coords(CoordsType.Pixel, obstacle.PositionDouble), obstacle.RadiusX, obstacle.RadiusY))
                    {
                        returnVal.Add(obstacle);
                    }
                }
            }

            return returnVal;
        }

        /*
        /// <summary>
        /// Returns all Creatures clipped by the agent; empty if none.
        /// </summary>
        public List<Creature> CreatureClippingCheck(Creature critter, Coords potentialPosition, bool creaturesClipCheck)
        {
            // obtain new entry tiles
            LinkedList<Coords> newEntries = this.TilesCoveredByEllipse(potentialPosition, critter.RadiusX, critter.RadiusY);

            // if the flags demands it, check if there is a creature in the way
            if (creaturesClipCheck)
            {
                if (this.PotentialCreatureToCreatureCollision(critter, potentialPosition))
                {
                    return CollisionType.Creature;
                }
            }

            return CollisionType.None;
        }
        */

        /// <summary>
        /// Returns the coordinates of the tiles covered by the agent.
        /// Null if any of those tiles are out of bounds or not passable.
        /// </summary>
        public LinkedList<Coords> TilesCoveredByEllipse(Coords center, UInt16 radiusX, UInt16 radiusY)
        {
            if (PotentialOutOfBoundsEllipse(center, radiusX, radiusY))
            {
                return null;
            }

            LinkedList<Coords> returnValue = new LinkedList<Coords>();

            Coords current = new Coords((Int32)(center.X / _pixelsPerBoxX), (Int32)(center.Y / _pixelsPerBoxY));
            // add tile on which center-pixel lies (if it's passable)
            if (!CoordsPassable(current))
            {
                return null;
            }
            returnValue.AddLast(current);

            #region overlap to the right check
            if ((center.X + radiusX) / _pixelsPerBoxX > current.X) // overlap to the right
            {
                Coords tileRight = new Coords(current.X + 1, current.Y);
                if (!CoordsPassable(tileRight))
                {
                    return null;
                }
                returnValue.AddLast(tileRight);

                if ((center.Y + radiusY) / _pixelsPerBoxY > current.Y) // overlap also to the bottom
                {
                    Coords tileBottom = new Coords(current.X, current.Y + 1);
                    if (!CoordsPassable(tileBottom))
                    {
                        return null;
                    }
                    returnValue.AddLast(tileBottom);

                    Coords tileBottomRight = new Coords(current.X + 1, current.Y + 1); // bottom-right inspection
                    if (CollisionCheckPixelInEllipse(VertexTopLeft(tileBottomRight), center, radiusX, radiusY)) // we're inside!
                    {
                        if (!CoordsPassable(tileBottomRight))
                        {
                            return null;
                        }
                        returnValue.AddLast(tileBottomRight);
                    }
                }
                else if ((center.Y - radiusY) / _pixelsPerBoxY < current.Y) // overlap also to the top
                {
                    Coords tileTop = new Coords(current.X, current.Y - 1);
                    if (!CoordsPassable(tileTop))
                    {
                        return null;
                    }
                    returnValue.AddLast(tileTop);

                    Coords tileTopRight = new Coords(current.X + 1, current.Y - 1); // top-right inspection
                    if (CollisionCheckPixelInEllipse(VertexBottomLeft(tileTopRight), center, radiusX, radiusY)) // we're inside!
                    {
                        if (!CoordsPassable(tileTopRight))
                        {
                            return null;
                        }
                        returnValue.AddLast(tileTopRight);
                    }
                }
            }
            #endregion
            #region overlap to the left check
            else if ((center.X - radiusX) / _pixelsPerBoxX < current.X) // overlap to the left
            {
                Coords tileLeft = new Coords(current.X - 1, current.Y);
                if (!CoordsPassable(tileLeft))
                {
                    return null;
                }
                returnValue.AddLast(tileLeft);

                if ((center.Y + radiusY) / _pixelsPerBoxY > current.Y) // overlap also to the bottom
                {
                    Coords tileBottom = new Coords(current.X, current.Y + 1);
                    if (!CoordsPassable(tileBottom))
                    {
                        return null;
                    }
                    returnValue.AddLast(tileBottom);

                    Coords tileBottomLeft = new Coords(current.X - 1, current.Y + 1); // bottom-left inspection
                    if (CollisionCheckPixelInEllipse(VertexTopRight(tileBottomLeft), center, radiusX, radiusY)) // we're inside!
                    {
                        if (!CoordsPassable(tileBottomLeft))
                        {
                            return null;
                        }
                        returnValue.AddLast(tileBottomLeft);
                    }
                }
                else if ((center.Y - radiusY) / _pixelsPerBoxY < current.Y) // overlap also to the top
                {
                    Coords tileTop = new Coords(current.X, current.Y - 1);
                    if (!CoordsPassable(tileTop))
                    {
                        return null;
                    }
                    returnValue.AddLast(tileTop);

                    Coords tileTopLeft = new Coords(current.X - 1, current.Y - 1); // top-left inspection
                    if (CollisionCheckPixelInEllipse(VertexBottonRight(tileTopLeft), center, radiusX, radiusY)) // we're inside!
                    {
                        if (!CoordsPassable(tileTopLeft))
                        {
                            return null;
                        }
                        returnValue.AddLast(tileTopLeft);
                    }
                }
            }
            #endregion
            #region in between
            else // still have to check Y
            {
                if ((center.Y + radiusY) / _pixelsPerBoxY > current.Y) // overlap also to the bottom
                {
                    Coords tileBottom = new Coords(current.X, current.Y + 1);
                    if (!CoordsPassable(tileBottom))
                    {
                        return null;
                    }
                    returnValue.AddLast(tileBottom);
                }
                else if ((center.Y - radiusY) / _pixelsPerBoxY < current.Y) // overlap also to the top
                {
                    Coords tileTop = new Coords(current.X, current.Y - 1);
                    if (!CoordsPassable(tileTop))
                    {
                        return null;
                    }
                    returnValue.AddLast(tileTop);
                }
            }
            #endregion

            // clean-up

            foreach (Coords c in returnValue)
            {
                if (!CheckInBounds(c))
                {
                    returnValue.Remove(c);
                }
            }


            return returnValue;
        }

        /// <summary>
        /// checks whether Coords are at all on the map
        /// </summary> 
        public bool CheckInBounds(Coords point)
        {
            if ((point.X < 0) || (point.X >= this._sizeX))
                return false;
            if ((point.Y < 0) || (point.Y >= this._sizeY))
                return false;

            return true;
        }

        /// <summary>
        /// Checks if the point at 'pixel' is inside the given ellipse.
        /// </summary>
        public bool CollisionCheckPixelInEllipse(Coords pixel, Coords center, UInt16 radiusX, UInt16 radiusY)
        {
            Int32 asquare = radiusX * radiusX;
            Int32 bsquare = radiusY * radiusY;
            return ((pixel.X - center.X) * (pixel.X - center.X) * bsquare + (pixel.Y - center.Y) * (pixel.Y - center.Y) * asquare) < (asquare * bsquare);
        }

        /// <summary>
        /// Checks if two ellipses intersect.
        /// </summary>
        private bool CollisionCheckEllipses(Coords center1, UInt16 radius1X, UInt16 radius1Y, Coords center2, UInt16 radius2X, UInt16 radius2Y)
        {
            UInt16 radiusSumX = (UInt16)(radius1X + radius2X);
            UInt16 radiusSumY = (UInt16)(radius1Y + radius2Y);
            // IS this even correct? It is for circles. Should be for ellipses.
            return CollisionCheckPixelInEllipse(center1, center2, radiusSumX, radiusSumY);
        }

        private bool CollisionCheckPointInRectangle(Coords point, Coords topLeft, Coords bottomRight)
        {
            return (point.X >= topLeft.X && point.X <= bottomRight.X && point.Y >= topLeft.Y && point.Y <= bottomRight.Y);
        }

        /// <summary>
        /// Checks if a pixel move in the given direction can send the Creature out of bounds.
        /// </summary>
        public bool PotentialOutOfBoundsEllipse(Coords potentialPosition, UInt16 radiusX, UInt16 radiusY)
        {
            if ((potentialPosition.X - radiusX < 0) || (potentialPosition.Y - radiusY < 0) ||
                 (potentialPosition.X + radiusX >= _sizeX * _pixelsPerBoxX) || (potentialPosition.Y + radiusY >= _sizeY * _pixelsPerBoxY))
            {
                return true;
            }

            return false;
        }

        private List<Creature> RayTracerPassabilityCheckPrecise(Creature client, Coords c1, Coords c2)
        {
            // this is only for pixels
            if (c1.Type != CoordsType.Pixel || c2.Type != CoordsType.Pixel)
            {
                throw new Exception("Non-pixel coordinate passed for pixel-level collision check");
            }

            // check last point first.
            List<Creature> testLast = this.CreatureClippingCheck(client, c2, true);
            if (testLast == null || testLast.Count > 0)
            {
                return testLast;
            }

            Int32 x0 = c1.X;
            Int32 y0 = c1.Y;
            Int32 x1 = c2.X;
            Int32 y1 = c2.Y;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int x = x0;
            int y = y0;
            int n = dx + dy;
            int x_inc = (x1 > x0) ? 1 : -1;
            int y_inc = (y1 > y0) ? 1 : -1;
            int error = dx - dy;
            dx *= 2;
            dy *= 2;

            for (; n > 0; --n)
            {
                Coords potentialPosition = new Coords(CoordsType.Pixel, x, y);

                // validity check
                List<Creature> collision = this.CreatureClippingCheck(client, potentialPosition, true);
                if (collision == null || collision.Count > 0)
                {
                    return collision;
                }

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

            return new List<Creature>();

        }

        /// <summary>
        /// Returns an exact pixel-by-pixel collision check.
        /// 'null' - terrain collision.
        /// empty - no collision.
        /// non-empty - creature collision.
        /// </summary>
        public List<Creature> RayTracerPassabilityCheckPrecise(Creature client, Vector v1, Vector v2)
        {
            return RayTracerPassabilityCheckPrecise(client, new Coords(CoordsType.Pixel, v1), new Coords(CoordsType.Pixel, v2));
        }

        /// <summary>
        /// Returns the creatures in the selection box defined by the two parameters.
        /// </summary>
        public List<Creature> CreaturesInSelectionBox(Coords topLeft, Coords bottomRight)
        {
            //establish boxes to look into

            Int32 gridXMin = Math.Max(0, topLeft.X / _pixelsPerBoxX);
            Int32 gridXMax = Math.Min(_sizeX - 1, bottomRight.X / _pixelsPerBoxX);
            Int32 gridYMin = Math.Max(0, topLeft.Y / _pixelsPerBoxY);
            Int32 gridYMax = Math.Min(_sizeY - 1, bottomRight.Y / _pixelsPerBoxY);

            // init return val
            List<Creature> returnVal = new List<Creature>();

            // loop over relevant gridtiles
            for (int i = gridXMin; i <= gridXMax; ++i)
            {
                for (int j = gridYMin; j <= gridYMax; ++j)
                {
                    foreach (Creature guy in _clippingBoxes[i, j])
                    {
                        if (!returnVal.Contains(guy) && CollisionCheckPointInRectangle(guy.PositionPixel,
                            new Coords(topLeft.X - guy.RadiusX, topLeft.Y - guy.RadiusY), 
                            new Coords(bottomRight.X + guy.RadiusX, bottomRight.Y + guy.RadiusY)))
                        {
                            returnVal.Add(guy);
                        }
                    }
                }
            }

            return returnVal;
        }


        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sizeX">Grid width.</param>
        /// <param name="sizeY">Grid height.</param>
        /// <param name="pixelsPerBoxX">Width of a gridtile.</param>
        /// <param name="pixelsPerBoxY">Height of a gridtile.</param>
        /// <param name="passMap">Terrain passability map.</param>
        public Collider(UInt16 sizeX, UInt16 sizeY, UInt16 pixelsPerBoxX, UInt16 pixelsPerBoxY, BitArray[] passMap)
        {
            _passabilityMap = passMap;

            _sizeX = sizeX;
            _sizeY = sizeY;
            _pixelsPerBoxX = pixelsPerBoxX;
            _pixelsPerBoxY = pixelsPerBoxY;

            _clippingBoxes = new List<Creature>[_sizeX, _sizeY];
            for (int i = 0; i < _sizeX; ++i)
            {
                for (int j = 0; j < _sizeY; ++j)
                {
                    _clippingBoxes[i, j] = new List<Creature>();
                }
            }

        }
    }
}
