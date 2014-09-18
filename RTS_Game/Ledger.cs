using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTS_Game
{
    /// <summary>
    /// Message structure for the ledger.
    /// </summary>
    public struct Message
    {
        private String _sender;
        public String Sender
        {
            get
            {
                return this._sender;
            }
            set
            {
                this._sender = value;
            }
        }
        private String _receiver;
        public String Receiver
        {
            get
            {
                return this._receiver;
            }
            set
            {
                this._receiver = value;
            }
        }
        private UInt64 _time;
        public UInt64 Time
        {
            get
            {
                return this._time;
            }
            set
            {
                this._time = value;
            }
        }
        private String[] _text;
        public String[] Text
        {
            get
            {
                return _text;
            }
            set
            {
                this._text = value;
            }
        }

        public Message(String sender, String receiver, String[] message)
        {
            this._time = 0;
            this._sender = sender;
            this._receiver = receiver;
            this._text = message;
        }

        public Message(String sender, String receiver, UInt64 time, String[] message)
            : this(sender, receiver, message)
        {
            this._time = time;
        }
    }

    public abstract class PlayerInput
    {
        private UInt64 _time;

        public PlayerInput(UInt64 timeStamp)
        {
            this._time = timeStamp;
        }
    }

    public class PlayerInputMoveInDir : PlayerInput
    {
        UInt32 _unitID;
        Vector _dir;

        public PlayerInputMoveInDir(UInt64 timeStamp, UInt32 unitID, Vector dir)
            : base(timeStamp)
        {
            this._unitID = unitID;
            this._dir = dir;
        }
    }

    public class PlayerInputMoveTo : PlayerInput
    {
        UInt32 _unitID;
        Coords _point;

        public PlayerInputMoveTo(UInt64 timeStamp, UInt32 unitID, Coords point)
            : base(timeStamp)
        {
            this._unitID = unitID;
            this._point = point;
        }
    }

    /// <summary>
    /// Keeps a record of the important events in the game.
    /// Class isn't finished.
    /// </summary>
    public class Ledger
    {
        // Scheduler ref so we can timestamp the message
        private Scheduler _schedulerReference;
        private List<String> _ledger;

        private LinkedList<PlayerInput> _input;

        public Ledger(Scheduler scheduler)
        {
            this._schedulerReference = scheduler;
            this._ledger = new List<string>();
            this._input = new LinkedList<PlayerInput>();
        }

        public void RecordMessage(Message message)
        {
            // timestamp
            message.Time = (message.Time == 0) ? this._schedulerReference.TimeCounter : message.Time;

            this._ledger.Add(message.Sender + "; " + message.Receiver + "; " + message.Time + "; " + message.Text);
        }

        public void RecordInput(PlayerInput command)
        {
            if (Constants.RecordInput)
            {
                this._input.AddLast(command);
            }
        }

    }
}
