using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTS_Game
{

    /// <summary>
    /// This class creates Action objects that the creatures can own to 'remember' and queue.
    /// These are generally things that are related to the real-time logic of the game.
    /// </summary>
    public abstract class Action
    {
        #region Properties

        // Reference to creature who owns this Action
        private Creature _actor;
        // Read-only. Changing an action's owner makes no sense. Maybe should be fully private?
        public Creature Actor
        {
            get
            {
                return _actor;
            }
        }

        // Total duration of action in ticks. For now set to some default.
        private UInt16 _actionTotalDuration;
        // maybe should be private
        public UInt16 ActionTotalDuration
        {
            get
            {
                return _actionTotalDuration;
            }
            set
            {
                this._actionTotalDuration = value;
            }
        }

        // Passive actions can be executed without regard for concurrent actions.
        // This is useful for example for 'talking'.
        // Should be set in the constructor. Default value - false.
        private bool _passive = false;
        public bool Passive
        {
            get
            {
                return this._passive;
            }
            set
            {
                this._passive = value;
            }
        }

        // whether Action is done, in which case it should be discarded.
        // Maybe should just use 'TicksRemaining'?
        protected bool _finished;
        public bool Finished
        {
            get
            {
                return _finished;
            }
        }

        // _actionTimer measures how much time the action has been active. It should stop once it
        // reaches _actionTotalDuration. Should be less than 65k frames (ushort).
        protected UInt16 _actionTimer = 0;
        #endregion

        #region Methods
        protected void TicksIncrement()
        {
            _actionTimer++;
        }
        public UInt16 TicksRemaining()
        {
            // this should always be positive to avoid lame bugs!
            Int32 returnVal = _actionTotalDuration - _actionTimer;
            return (returnVal >= 0) ? ((UInt16)(returnVal)) : (UInt16)0;
        }

        // Update method. Executed every tick while action is active.
        // Returns true if Action is complete.
        public abstract void Update();

        // Sometimes an Action is interrupted and must be interrupted cleanly.
        public abstract void InterruptionCleanUp();

        // Once an action is complete, there might be things to wrap up and update.
        // Action is respnsible for cleaning up itself!
        protected abstract void WrapUp();
        #endregion

        public Action(Creature actor)
        {
            this._actor = actor;
            _finished = false;
        }
    }

    #region Actions

    /// <summary>
    /// Just waits the given amount of ticks.
    /// </summary>
    public class ActionWait : Action
    {
        public override void Update()
        {
            if (_finished)
            {
                return;
            }

            this.TicksIncrement();
            if (this.TicksRemaining() == 0)
            {
                this.WrapUp();
            }
        }

        protected override void WrapUp()
        {
            this._finished = true;
        }

        // Nothing to do here.
        public override void InterruptionCleanUp()
        {
            this.WrapUp();
        }

        public ActionWait(Creature actor, UInt16 ticksToWait)
            : base(actor)
        {
            this.ActionTotalDuration = ticksToWait;
        }
    }

    /// <summary>
    /// Has the actor say something.
    /// </summary>
    public class ActionSaySomething : Action
    {
        private String _text;

        public override void InterruptionCleanUp()
        {
            this.WrapUp();
        }

        protected override void WrapUp()
        {
            this.Actor.LabelUpper = null;
            this._finished = true;
        }

        public override void Update()
        {
            if (_finished)
            {
                return;
            }

            if (this._actionTimer == 0)
            {
                this.Actor.LabelUpper = this._text;
            }

            this.TicksIncrement();
            if (this.TicksRemaining() <= 0)
            {
                this.WrapUp();
            }
        }

        public ActionSaySomething(Creature actor, UInt16 duration, String wordsOfWisdom)
            : base(actor)
        {
            this.ActionTotalDuration = duration;
            this._text = wordsOfWisdom;
            this.Passive = true;
        }
    }

    public class ActionAttack : Action
    {
        Creature _target;

        public override void Update()
        {
            if (_finished)
            {
                return;
            }

            this.TicksIncrement();
            if (this.TicksRemaining() == 0)
            {
                _target.HitIncurred(Actor, Actor.StatDamage);
                this.WrapUp();
            }
        }

        public override void InterruptionCleanUp()
        {
            this.WrapUp();
        }

        protected override void WrapUp()
        {
            this._finished = true;
        }

        public ActionAttack(Creature actor, Creature target, UInt16 attackTime) : base(actor)
        {
            _target = target;
            this.ActionTotalDuration = attackTime;
        }
    }

    #endregion
}
