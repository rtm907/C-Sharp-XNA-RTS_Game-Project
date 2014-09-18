using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace RTS_Game
{
    static class StaticMathFunctions
    {
        public static Direction OppositeDirection(Direction d)
        {
            return (Direction)(((byte)d + 4) % 8);
        }

        public static Direction DirectionToTheRight(Direction d)
        {
            return (Direction)(((byte)d + 1) % 8);
        }

        public static Direction DirectionToTheLeft(Direction d)
        {
            return (Direction)(((byte)d + 7) % 8);
        }

        public static Coords CoordsAverage(Coords c1, Coords c2)
        {
            return new Coords(c1.Type, (Int32)0.5 * (c1.X + c2.X), (Int32)0.5 * (c1.Y + c2.Y));
        }

        public static bool CoordinateIsInBox(Coords c, Coords boxTopLeft, Coords boxBottomRight)
        {
            return (((c.X >= boxTopLeft.X) && (c.X <= boxBottomRight.X)) && ((c.Y >= boxTopLeft.Y) && (c.Y <= boxBottomRight.Y)));
        }

        /// <summary>
        /// Returns the eucledean distance between two Coords
        /// </summary>
        public static float DistanceBetweenTwoCoordsEucledean(Coords c1, Coords c2)
        {
            return (float)Math.Sqrt(Math.Pow((c1.X - c2.X), 2) + Math.Pow((c1.Y - c2.Y), 2));
        }

        /// <summary>
        /// returns the distance between two Coords
        /// </summary>
        public static float DistanceBetweenTwoCoordss(Coords c1, Coords c2)
        {
            return Math.Max(Math.Abs(c1.X - c2.X), Math.Abs(c1.Y - c2.Y));
        }

        public static Int32 DistanceBetweenTwoCoordsEucledeanSquared(Coords c1, Coords c2)
        {
            Int32 dx = c1.X - c2.X;
            Int32 dy = c1.Y - c2.Y;
            return (dx * dx + dy * dy);
        }

        /// <summary>
        /// Returns the Direction in which a vector is pointing.
        /// </summary>
        public static Nullable<Direction> DirectionVectorToDirection(Coords dirvector)
        {
            if (dirvector.X == 0 & dirvector.Y == 0)
            {
                return null;
            }

            // The angle is clockwise from the negative X, Y=0 axis. Note the positive Y-axis points down.
            double angle;
            angle = Math.Atan2(dirvector.Y, dirvector.X) + Math.PI;

            Direction moveDir = (Direction)
               (byte)((((angle + 0.125 * Math.PI) / (0.25 * Math.PI)) + 5) % 8);

            return moveDir;
        }

        public static float InfluenceDecayFunction1(UInt32 a)
        {
            return (float)1 / (a + 1);
        }

        /// <summary>
        /// Returns the Coords that neighbour 'here' in 'direction'.
        /// Note C# forms coordinate system has origin at the top-left
        /// </summary>
        public static Coords CoordsNeighboringInDirection(Coords here, Direction direction)
        {
            switch (direction)
            {
                case (Direction.Northeast):
                    return new Coords(here.Type, here.X + 1, here.Y - 1);
                case (Direction.East):
                    return new Coords(here.Type, here.X + 1, here.Y);
                case (Direction.Southeast):
                    return new Coords(here.Type, here.X + 1, here.Y + 1);
                case (Direction.South):
                    return new Coords(here.Type, here.X, here.Y + 1);
                case (Direction.Southwest):
                    return new Coords(here.Type, here.X - 1, here.Y + 1);
                case (Direction.West):
                    return new Coords(here.Type, here.X - 1, here.Y);
                case (Direction.Northwest):
                    return new Coords(here.Type, here.X - 1, here.Y - 1);
                case (Direction.North):
                    return new Coords(here.Type, here.X, here.Y - 1);
            }

            // This code should be unreachable. Added because compiler wants it.
            return here;
        }

        // Returns the coordinate-wise representation of a Direction
        public static Coords DirectionToCoords(Direction dir)
        {
            switch (dir)
            {
                case (Direction.Northeast):
                    return new Coords(1, -1);
                case (Direction.East):
                    return new Coords(1, 0);
                case (Direction.Southeast):
                    return new Coords(1, 1);
                case (Direction.South):
                    return new Coords(0, 1);
                case (Direction.Southwest):
                    return new Coords(-1, 1);
                case (Direction.West):
                    return new Coords(-1, 0);
                case (Direction.Northwest):
                    return new Coords(-1, -1);
                case (Direction.North):
                    return new Coords(0, -1);
            }

            return new Coords(CoordsType.Pixel, 0, 0);
        }

        /// <summary>
        /// Evaluation function for the AI decision making algorithm.
        /// </summary>
        public static float StimulusEvaluator(float strength, float distance)
        {
            // expensive function; consider simplyfying
            return strength / (1 + Constants.StimulusEvaluationDistanceRedundancyCoefficient *
                (float)Math.Pow(distance, Constants.StimulusEvaluationDistanceRedundancyPower));
        }

    }
}
