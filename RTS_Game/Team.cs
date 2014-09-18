using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;


namespace RTS_Game
{
    public class Team
    {
        private String _name;
        public String Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private Color _teamColor;
        public Color TeamColor
        {
            get
            {
                return _teamColor;
            }
        }

        private List<Creature> _members = new List<Creature>();

        public void MemberRegister(Creature newGuy)
        {
            _members.Add(newGuy);
        }
        public void MemberRemove(Creature oldGuy)
        {
            _members.Remove(oldGuy);
        }

        private SortedList<UInt32, Creature> _observedEnemies = new SortedList<uint,Creature>();
        public bool EnemyIsObserved(Creature enemy)
        {
            return _observedEnemies.ContainsKey(enemy.UniqueID);
        }
        public void ObservedEnemyAdd(Creature enemy)
        {
            if(!_observedEnemies.ContainsKey(enemy.UniqueID))
            {
                _observedEnemies.Add(enemy.UniqueID, enemy);
            }
        }
        public void ObservedEnemyRemove(Creature enemy)
        {
            _observedEnemies.Remove(enemy.UniqueID);
        }

        public Team(Color teamColor)
        {
            _teamColor = teamColor;
        }
    }
}
