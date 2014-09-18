using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTS_Game
{


    public class Brain
    {
        protected Creature _owner;
        public Creature MyCreature
        {
            get
            {
                return _owner;
            }
            set
            {
                _owner = value;
            }
        }

        #region Movement

        private Navigator _myNavigator;
        public Navigator MyNavigator
        {
            get
            {
                return _myNavigator;
            }
            set
            {
                _myNavigator = value;
            }
        }

        private Nullable<Vector> _moveDir;
        /// <summary>
        /// Vector direction of creature's movement.
        /// Null if creature isn't moving.
        /// </summary>
        public Nullable<Vector> MoveDir
        {
            get
            {
                return _moveDir;
            }
            set
            {
                _moveDir = value;
            }
        }

        private Vector _faceDir;
        /// <summary>
        /// Direction the agent is facing. 
        /// </summary>
        public Vector FaceDir
        {
            get
            {
                return _faceDir;
            }
            set
            {
                _faceDir = value;
            }
        }

        private bool _moveClipChecked;

        private bool _moveRequested;

        /*
        private UInt16 _sideStepTimer = 0;
        private Vector _sideStepDir;
        */

        /// <summary>
        /// Requests a move from the agent.
        /// </summary>
        /// <param name="direction">Move direction.</param>
        /// <param name="clipChecked">Whether the move has been clip-checked.</param>
        /// <param name="normalizedToSpeed">Whether the vector is normalized.</param>
        public void MoveRequest(Vector direction, bool clipChecked, bool normalizedToSpeed)
        {
            if (!normalizedToSpeed)
            {
                // Accelerate and normalize the move.
                this.Accelerate();
                direction.ScaleToLength(_owner.MoveSpeedCurrent);
            }

            _moveDir = direction;
            _moveClipChecked = clipChecked;
            _moveRequested = true;
        }

        /// <summary>
        /// Ask the agent to move aside if he can.
        /// </summary>
        public void StepAsideRequest(Vector direction)
        {
            if (_owner.MoveSpeedCurrent > 0)
            {
                //the agent is already in motion. ignore request.
                return;
            }

            // include other conditions for ignoring the request here.

            // try to step aside
            MoveRequest(direction, false, false);
        }

        private void Accelerate()
        {
            if (_owner.MoveSpeedCurrent != _owner.MoveSpeedMax)
            {
                _owner.MoveSpeedCurrent = Math.Min(_owner.MoveSpeedCurrent + _owner.MoveAcceleration, _owner.MoveSpeedMax);
            }
        }

        private void Decelerate()
        {
            if (_owner.MoveSpeedCurrent != 0)
            {
                _owner.MoveSpeedCurrent = Math.Max(_owner.MoveSpeedCurrent - _owner.MoveAcceleration, 0);
            }
        }

        private void Move()
        {
            // if move isn't clip-checked, we have to clip check it now.
            if (!_moveClipChecked)
            {
                List<Creature> checkForCollisions = _owner.MyCollider.
                    RayTracerPassabilityCheckPrecise(_owner, _owner.PositionDouble, _owner.PositionDouble + _moveDir.Value);

                if (checkForCollisions == null || checkForCollisions.Count > 0)
                {
                    // Collision detected. Move is impossible.
                    // (Perhaps add small offset checks for sliding?
                    _owner.MoveSpeedCurrent = 0;
                    return;
                }
            }

            // do move:
            Vector oldPosition = _owner.PositionDouble;
            _owner.PositionDouble += _moveDir.Value;
            _owner.InhabitedMap.MyCollider.UpdateCreatureBoxes(_owner, oldPosition);

        }

        protected void UpdateMovement()
        {
            // check if there is no explicit move request
            if (!_moveRequested)
            {
                // obtain a move from active Navigators:
                if (_myNavigator != null)
                {
                    // Navigator found. Accelerate.
                    this.Accelerate();

                    Nullable<Vector> moveDir = _myNavigator.Navigate();
                    if (moveDir == null)
                    {
                        // we've hit an impediment. kill the navigator and stop the ship.
                        _myNavigator = null;
                        _moveDir = null;
                        _owner.MoveSpeedCurrent = 0;
                    }
                    else if (moveDir.Value.X == Double.PositiveInfinity)
                    {
                        // goal attained. stop the navigator.
                        _myNavigator = null;
                    }
                    else
                    {
                        // request move from self
                        this.MoveRequest(moveDir.Value, true, true);
                    }
                }
            }

            // Time to do the move. Check if there is an active vector:
            if (_moveDir != null)
            {
                // do move
                this.Move();

                // if there is no move request, we need to decelerate and normalize the dir-vector.
                if (!_moveRequested && _myNavigator == null)
                {
                    this.Decelerate();
                    // careful with the boxing
                    if (_owner.MoveSpeedCurrent > 0)
                    {
                        Vector normalized = _moveDir.Value;
                        normalized.ScaleToLength(_owner.MoveSpeedCurrent);
                        this._moveDir = normalized;
                    }
                    else
                    {
                        this._moveDir = null;
                    }
                }
            }

            // Flush flags.
            _moveRequested = false;
            _moveClipChecked = false;
        }

        #endregion

        protected List<Action> _actionsList = new List<Action>();

        // Current words the creature is saying.
        protected ActionSaySomething _opinion;

        public void ForceAddAction(Action add)
        {
            _actionsList.Add(add);
        }
        private void AddAction(Action add)
        {
            _actionsList.Add(add);
        }
        public void ClearCurrentActions()
        {
            // Is this check even necessary?
            if (_actionsList == null)
                return;

            foreach (Action a in _actionsList)
            {
                if (a != null)
                {
                    a.InterruptionCleanUp();
                }
            }
            _actionsList.Clear();
        }

        public void AddActionSomethingToSay(ActionSaySomething opinion)
        {
            // Add priorities to words so that
            // 1) if the creature recieves overriding words it only overrides according to priority
            // 2) a volubility option can be implemented

            if (this._opinion != null)
            {
                this._opinion.InterruptionCleanUp();
            }
            this._opinion = opinion;
        }



        /*
        private void ShortTermGoalSet(Item newItem, Stimulus? stimulusType, float priority)
        {
            this._navigatorShortTerm = new Navigator(this._owner, newItem.Position().Value, stimulusType,
                Constants.CurrentShortTermPriortyMultiplier * priority);
        }
        */

        /*
        // Finds a long term goal for the agent if he lacks one.
        private Navigator ObtainLongTermGoal()
        {
            /// NOTE: I see two ways of doing this:
            /// 1) Take the current highest-priority stimulus and look for a target.
            /// 2) Examine ALL possible targets, ranked by some function of distance-to, source-strength, and creature-priority,
            /// and pick one of the top ones (say, take the top 5 and choose one of them randomly, using the calculated weights).
            /// Clearly the first one is faster but the second one is stronger.
            /// For now I'll adopt the first approach. Ideally both should be tested and perhaps used contextually.

            // Pick most relevant stimulus:
            StimulusFloatPair[] pickStimulus = new StimulusFloatPair[(Enum.GetValues(typeof(Stimulus))).Length];
            for (byte i = 0; i < pickStimulus.Length; ++i)
            {
                // considerng implementing a float delegate(float, float) for the eval. function
                pickStimulus[i]._value = StimuliPriority[i] * StimuliUrgency[i];
            }
            Array.Sort(pickStimulus);
            Stimulus targetType = pickStimulus[pickStimulus.Length - 1]._stimulus;

            // Get address book from the map:
            // WARNING: this will throw exception if addresBook is empty.
            List<ItemStimulusValuePair> addressBook = this._owner.InhabitedMap.AddressBook(targetType);

            // Problems: the evaluation should a function of distance to target and target strength. 
            // Ideally we should examine the strongest goals and the nearest ones, and pick from them.
            // For now just examine everything (MUST change this later; on a complex map it would be a killer).
            ItemStimulusValuePair[] evaluationArray = new ItemStimulusValuePair[addressBook.Count];
            addressBook.CopyTo(evaluationArray);
            for (UInt32 i = 0; i < pickStimulus.Length; ++i)
            {
                // Use delegates or a static function call here!
                float strength = evaluationArray[i].ItemReference.ItemFunctions[targetType];
                Coords targetPosition = evaluationArray[i].ItemReference.Position().Value;
                float distance =
                    (targetPosition == null) ? 0 : (StaticMathFunctions.DistanceBetweenTwoCoordsEucledean(new Coords(CoordsType.Tile, this._owner.PositionPixel), targetPosition));
                evaluationArray[i].StimulusValue = StaticMathFunctions.StimulusEvaluator(strength, distance);
            }
            Array.Sort(evaluationArray,
                delegate(ItemStimulusValuePair pair1, ItemStimulusValuePair pair2)
                {
                    return pair1.StimulusValue.CompareTo(pair2.StimulusValue);
                }
                );
            Item targetItem = evaluationArray[evaluationArray.Length - 1].ItemReference;


            // return navigator
            return new Navigator(this._owner, targetItem.Position().Value, targetType,
                Constants.CurrentLongTermPriorityMultiplier * evaluationArray[evaluationArray.Length - 1].StimulusValue);
        }
        */

        private Creature _target;
        private Creature _targetForced;

        private UInt16 _routeToForcedTargetRecalcTimer = 0;

        // Checks if there are valid contextual actions to be taken.
        private void ObtainAction()
        {
            // take care of forced target, if any
            if (_targetForced != null)
            {
                if (TargetIsInRange(_targetForced))
                {
                    AddAction(new ActionAttack(_owner, _targetForced, _owner.StatAttackTime));
                    return;
                }
                else if (_owner.Team.EnemyIsObserved(_targetForced))
                {
                    ++_routeToForcedTargetRecalcTimer;
                    if (_routeToForcedTargetRecalcTimer % Constants.DefaultRouteToForcedTargetRecalcTimer == 1)
                    {
                        _myNavigator = new Navigator(_owner, _targetForced.PositionPixel, false);
                        return;
                    }
                }
                else 
                {
                    if (!_targetForced.Dead)
                    {
                        _myNavigator = new Navigator(_owner, _targetForced.PositionPixel, false);
                    }
                    _targetForced = null;
                    _routeToForcedTargetRecalcTimer = 0;
                }
            }


            // check for enemies
            if (_observedEnemies.Count > 0)
            {
                if (_target != null)
                {
                    if (TargetIsInRange(_target))
                    {
                        AddAction(new ActionAttack(_owner, _target, _owner.StatAttackTime));
                        return;
                    }
                }

                // pick target
                _target = NearestEnemy();
                if (TargetIsInRange(_target))
                {
                    AddAction(new ActionAttack(_owner, _target, _owner.StatAttackTime));
                    return;
                }
                else
                {
                    _myNavigator = new Navigator(_owner, _target.PositionPixel, false);
                    return;
                }
            }
            else
            {
                if (_target != null)
                {
                    if (!_target.Dead)
                    {
                        _myNavigator = new Navigator(_owner, _target.PositionPixel, false);
                    }
                    _target = null;
                    return;
                }
            }
        }

        // only called if _opinion is non-zero
        private void UpdateOpinion()
        {
            this._opinion.Update();
            if (this._opinion.Finished)
            {
                this._opinion = null;
            }
        }

        // only called
        private void UpdateActions()
        {
            // First we look through the current list of actions. If there is a 
            // non-passive action to do, with set the bool below to true and do not
            // update the state AI.

            for (Int32 i = 0; i < _actionsList.Count(); i++)
            {
                Action currentAction = _actionsList[i];
                currentAction.Update();
                // Careful when updating an array/list during a loop
                if (currentAction.Finished)
                {
                    _actionsList.Remove(currentAction);
                    i--;
                }
            }
        }

        public virtual void Update()
        {
            // handle current ActionSomethingToSay.
            if (this._opinion != null)
            {
                this.UpdateOpinion();
            }

            if (_myNavigator != null && _myNavigator.ForceMove)
            {
                this.UpdateMovement();
                return;
            }

            if (this._actionsList.Count == 0)
            {
                // in case there is no pending action, try to obtain one
                this.ObtainAction();
            }

            // if current actions exists, proceed with it
            if (this._actionsList.Count > 0)
            {
                this.UpdateActions();
            }
            else
            {
                // otherwise, proceed with movement
                this.UpdateMovement();
            }
        }

        #region Orders

        public void OrderMove(Coords targetPixel)
        {
            this._myNavigator = new Navigator(this.MyCreature, targetPixel, true);
        }

        #endregion

        #region Memory


        /*
        // priority coefficients for the various stimuli. For example any physical danger stimulus overrides
        // the 'hunger' and 'thirst' stimuli (inless the latter are extreme, I suppose).
        // should take values >= 1.
        private float[] StimuliPriority = new float[(Enum.GetValues(typeof(Stimulus))).Length];

        // Stores the urgency coefficient of each stimulus type.
        // Values between 0 and 1?
        private float[] StimuliUrgency = new float[(Enum.GetValues(typeof(Stimulus))).Length];
        */


        // stores the IDs creatures the agents currently sees
        //private List<Creature> _observedCreatures = new List<Creature>();
        //private BitArray _observedCreatures = new BitArray((Int32)Constants.MaximumNumberOfCreatures);

        private List<Creature> _observedFriends = new List<Creature>();
        private List<Creature> _observedEnemies = new List<Creature>();

        //private Int32 _observedCreaturesCount = 0;

        /* public List<UInt32> ObservedCreatures
         {
             get
             {
                 return this._observedCreatures;
             }
         }*/

        // PROBLEM: make it so the update DOESN'T occur if a remove is followed by an add in the same tick.
        public void ObservedCreaturesAdd(Creature critter)
        {
            // don't add self to list.
            if (!critter.Equals(this._owner))
            {
                if (critter.Team == _owner.Team)
                {
                    _observedFriends.Add(critter);
                }
                else
                {
                    _observedEnemies.Add(critter);
                    _owner.Team.ObservedEnemyAdd(critter);
                }
            }
        }
        public void ObservedCreaturesRemove(Creature critter)
        {
            if (critter.Team == _owner.Team)
            {
                _observedFriends.Remove(critter);
            }
            else
            {
                _observedEnemies.Remove(critter);
                _owner.Team.ObservedEnemyRemove(critter);
            }
        }

        private bool TargetIsInRange(Creature target)
        {
            // NOTE: This should be adjusted according to bboxes!

            return (_owner.PositionDouble.DistanceTo(target.PositionDouble) < _owner.StatAttackRange);
        }

        private Creature NearestEnemy()
        {
            Creature nearestEnemy = null;
            double smallestDistance = (_owner.StatSightRange+1)*Constants.diagonalCoefficient*Constants.TileSize;

            foreach (Creature enemy in _observedEnemies)
            {
                double distanceTo = _owner.PositionDouble.DistanceTo(enemy.PositionDouble);
                if (distanceTo < smallestDistance)
                {
                    smallestDistance = distanceTo;
                    nearestEnemy = enemy;
                }
            }

            return nearestEnemy;
        }

        private List<Item> _observedItems = new List<Item>();
        public void ObservedItemsAdd(Item newItem)
        {
            this._observedItems.Add(newItem);
        }
        public void ObservedItemsRemove(Item oldItem)
        {
            this._observedItems.Remove(oldItem);
        }

        #endregion

        /*
        public Brain(Creature owner)
        {
            this._owner = owner;
        }*/

        public Brain()
        {

        }

        public Brain(Creature owner)
        {
            _owner = owner;
        }
    }

    public class BrainPlayer : Brain
    {
        public override void Update()
        {
            UpdateMovement();
        }

        public void MoveOrder(Coords targetPixel)
        {
            MyNavigator = new Navigator(this.MyCreature, targetPixel, true);
        }

        public BrainPlayer()
            : base()
        {
        }
    }

    public class BrainDead : Brain
    {
        public override void Update()
        {
        }

        public BrainDead()
            : base()
        {
        }
    }

    public class BrainRandomWalk : Brain
    {
        public override void Update()
        {
            base.Update();
            if (--_thinkingCounter <= 0)
            {
                GoSomewhere();
            }
        }

        private int _thinkingCounter;

        // Generates random pixel to go to.
        private void GoSomewhere()
        {
            Map currentMap = this.MyCreature.InhabitedMap;
            RandomStuff randomator = currentMap.Randomator;
            Coords targetPixel = new Coords(CoordsType.Pixel, (Int32)randomator.NSidedDice((UInt16)currentMap.PixelBoundX, 1) - 1,
                (Int32)randomator.NSidedDice((UInt16)currentMap.PixelBoundY, 1) - 1);

            //Coords targetTile = new Coords(CoordsType.Tile, targetPixel);
            List<Creature> clipCheck = _owner.MyCollider.CreatureClippingCheck(this.MyCreature, targetPixel, false);
            if (clipCheck != null && clipCheck.Count == 0)
            {
                MyNavigator = new Navigator(this.MyCreature, targetPixel, false);
                _thinkingCounter = (int)randomator.NSidedDice(1000, 1);
            }
        }

        public BrainRandomWalk()
            : base()
        {
        }
    }

    /// <summary>
    /// Guides creature from current position to its goal.
    /// </summary>
    public class Navigator
    {
        private bool _forceMove;
        public bool ForceMove
        {
            get
            {
                return _forceMove;
            }
        }

        /// <summary>
        /// Avoidance direction enum.
        /// </summary>
        private enum AvoidanceDir : sbyte
        {
            Left = 0,
            Right
        }
        private AvoidanceDir _avoidanceDirection = AvoidanceDir.Left;

        private Coords _goalPixel;
        private Coords _goalTile;
        private List<Direction> _routeCoarse; // tile-accurate
        private Creature _traveler;

        // This is where the navigator left off. Used for invalidation checks.
        private Coords _putativeCurrentPosition;

        // The current pixel-goal of the navigator, with pixel accuracy.
        private Nullable<Coords> _visiblePixelGoal;

        private UInt32 _timeTraveled;
        private UInt32 _timeTraveledUpperBound;

        private UInt32 _timeWaited = 0;

        /// <summary>
        /// Estimates travel time to next visible target. The bound is conservative, to allow for 
        /// time wasted on collision avoidance.
        /// </summary>
        private void EstimateTimeOfTravel()
        {
            if (_visiblePixelGoal == null)
            {
                throw new Exception("Time travel estimation requested when no target is present.");
            }

            double distanceToTarget = _traveler.PositionDouble.DistanceTo(new Vector(_visiblePixelGoal.Value));
            distanceToTarget = Math.Max(2 * Constants.TileSize, distanceToTarget); // over-estimate for short distances


            _timeTraveledUpperBound = (UInt32)(Constants.TravelTimeOverestimationCoefficient * distanceToTarget / _traveler.MoveSpeedMax);
            _timeTraveled = 0;
        }

        /// <summary>
        /// Finds a route to the target.
        /// </summary>
        private void AcquireRoute()
        {
            // Line of sight check to avoid needless A* calls
            //Vector here = new Vector(_traveler.PositionPixel.X , _traveler.PositionPixel.Y);
            Vector there = new Vector(_goalPixel.X, _goalPixel.Y);

            List<Creature> clipCheck =
                _traveler.InhabitedMap.RayTracerPassabilityCheckRough(_traveler, _traveler.PositionDouble, there, _traveler.RadiusX);
            if (clipCheck != null && clipCheck.Count == 0)
            {
                _visiblePixelGoal = _goalPixel;
                _putativeCurrentPosition = _goalTile;
                EstimateTimeOfTravel();

                return;
            }

            //Coords hereCoords = _traveler.Position;
            this._routeCoarse = _traveler.MyPathfinder.PathfinderAStarCoarse(_traveler.PositionTile, _goalTile, StaticMathFunctions.DistanceBetweenTwoCoordsEucledean);

            this._putativeCurrentPosition = _traveler.PositionTile;
            AcquireVisiblePixelGoal();
        }

        // Acquires new visible pixel goal. Deletes the route entries up to that point.
        private void AcquireVisiblePixelGoal()
        {
            UInt16 i = 0;
            //Coords returnVal;
            Coords currentTile = _traveler.PositionTile;

            for (; i < _routeCoarse.Count; ++i)
            {
                Direction currentDir = _routeCoarse[_routeCoarse.Count - i - 1];
                Coords newTile = currentTile.NeighborInDirection(currentDir);

                //Vector here = new Vector(_traveler.PositionPixel.X , _traveler.PositionPixel.Y);

                Coords newTileMiddle = _traveler.InhabitedMap.GetTile(newTile).PixelMiddle();
                Vector there = new Vector(newTileMiddle.X, newTileMiddle.Y);

                List<Creature> clipCheck =
                    _traveler.InhabitedMap.RayTracerPassabilityCheckRough(_traveler, _traveler.PositionDouble, there, _traveler.RadiusX);
                if (clipCheck == null || clipCheck.Count > 0)
                {
                    // We've reached an tile, to which a direct line can't go. Set the target to the midpoint of previous tile.
                    _visiblePixelGoal = _traveler.InhabitedMap.GetTile(currentTile).PixelMiddle();
                    EstimateTimeOfTravel();

                    // remove directions from route
                    _routeCoarse.RemoveRange(_routeCoarse.Count - i, i);
                    _putativeCurrentPosition = currentTile;

                    return;
                }

                currentTile = newTile;
            }

            // we went through the loop without returning. that means the end-pixel is visible.
            _visiblePixelGoal = _goalPixel;
            EstimateTimeOfTravel();
            _putativeCurrentPosition = _goalTile;
            _routeCoarse = null;
        }

        /// <summary>
        /// Obtains step towards goal. Returns null if path is blocked.
        /// Returns (infinity, infinity) if goal is reached.
        /// </summary>
        /// <returns></returns>
        public Nullable<Vector> Navigate()
        {
            ++_timeTraveled;

            // Check to see if we've arrived. In that case the navigator has finished its job.
            if (_traveler.MyCollider.CollisionCheckPixelInEllipse(_goalPixel, _traveler.PositionPixel, _traveler.RadiusX, _traveler.RadiusY))
            {
                // goal attained.
                ++Constants._navigationSuccesses;
                return new Vector(Double.PositiveInfinity, Double.PositiveInfinity);
            }

            // Check to see if we have a visible goal or if we're taking too long to get to it
            if (_visiblePixelGoal == null || _timeTraveled >= _timeTraveledUpperBound)
            {
                // no goal set. check to see if a exists and whether we're on it:
                if (_routeCoarse == null || !this._putativeCurrentPosition.Equals(_traveler.PositionTile))
                {
                    // no route or we need a recalc
                    AcquireRoute();
                }
            }

            // Check to see if we've arrived to the current visible goal. If so, get new one.
            if (_traveler.MyCollider.CollisionCheckPixelInEllipse(_visiblePixelGoal.Value, _traveler.PositionPixel, _traveler.RadiusX, _traveler.RadiusY))
            {
                AcquireVisiblePixelGoal();
            }

            // We move towards current visible pixel goal.

            // Declare movement vector
            Vector principleMoveDir = new Vector(_visiblePixelGoal.Value) - _traveler.PositionDouble;
            principleMoveDir.ScaleToLength(_traveler.MoveSpeedCurrent);
            Vector startPoint = _traveler.PositionDouble;

            // Try to move in given direction. If the move fails, offset the direction angle and try again.
            // The offsets are under a full PI in either direction (so the agent doesn't get stuck in a loop).
            // Avoidance is first attempted in one direction. If it fails, the agent tries to other direction.
            // If both directions fail, the agent gives up.
            Int32 numberOfOffsetAttempts = (Int32)(Math.PI / Constants.CollisionAvoidanceRotation);

            // flags whether avoidance direction was flipped.
            bool directionFlipped = false;

            // list of creatures the agent has asked to step aside.
            List<Creature> askedToStepAside = new List<Creature>();

            while (true)
            {
                for (sbyte i = 0; i < numberOfOffsetAttempts; ++i)
                {
                    double offsetAngle = Math.Pow(-1, (sbyte)_avoidanceDirection) * i * Constants.CollisionAvoidanceRotation;
                    Vector moveDir = principleMoveDir.Rotate(offsetAngle);

                    List<Creature> checkForCollisions = _traveler.MyCollider.RayTracerPassabilityCheckPrecise(_traveler, startPoint, startPoint + moveDir);

                    if (checkForCollisions != null)
                    {
                        if (checkForCollisions.Count > 0)
                        {
                            // ask agents in the way to move
                            foreach (Creature impediment in checkForCollisions)
                            {
                                if (!askedToStepAside.Contains(impediment))
                                {
                                    Vector vectorToImpediment = impediment.PositionDouble - _traveler.PositionDouble;
                                    vectorToImpediment.ScaleToLength(1);

                                    Vector moveDirPerpendicular = directionFlipped ? moveDir.PerpendiculatRight() : moveDir.PerpendiculatLeft();
                                    moveDirPerpendicular.ScaleToLength(1);

                                    impediment.CreatureBrain.StepAsideRequest(vectorToImpediment + moveDirPerpendicular);
                                    askedToStepAside.Add(impediment);
                                }

                            }
                        }
                        else
                        {
                            // success. update creature position.
                            //_traveler.PositionDouble = startPoint + moveDir;
                            _timeWaited = 0;
                            return moveDir;
                        }
                    }
                }

                // we failed to move anywhere. the agent tries avoidance in the other direction.
                if (!directionFlipped)
                {
                    _avoidanceDirection = (AvoidanceDir)(((sbyte)_avoidanceDirection + 1) % 2);
                    directionFlipped = true;
                }
                else
                {
                    // avoidance in either direction failed. the agent gives up.
                    // NOTE: perhaps add a brief (1 second?) waiting time for the agent before giving up.
                    ++_timeWaited;

                    if (_timeWaited >= Constants.FailedCollisionAvoidanceWaitTime)
                    {
                        // give up
                        _timeWaited = 0;
                        break;
                    }
                    else
                    {
                        // pass back static moveVector
                        return new Vector(0, 0);
                    }
                }
            }

            ++Constants._navigationFailures;
            return null;

        }

        public Navigator(Creature traveler, Coords goalPixel, bool forceMove)
        {
            this._traveler = traveler;
            this._goalPixel = goalPixel;
            this._goalTile = new Coords(CoordsType.Tile, goalPixel);
            this._forceMove = forceMove;

            this.AcquireRoute();
        }
    }

}
