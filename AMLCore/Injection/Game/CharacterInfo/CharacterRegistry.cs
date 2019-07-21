using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.CharacterInfo
{
    public class CharacterConfigInfo
    {
        public readonly CharacterInfo Character;
        public readonly int Type;

        public CharacterConfigInfo(CharacterInfo ch, int type)
        {
            Character = ch;
            Type = type;
        }
    }

    public struct CharacterData
    {
        public int Life;
        public int LifeUp;
        public int Soul;
        public int SoulUp;
        public int Attack;
        public int AttackUp;

        //values in this group are not used
        public int RegainCycle;
        public int RegainRate;
        public float AttackOffset;

        public bool UseSpecialAlgorithm;
    }

    public class CharacterInfo
    {
        public readonly string PlayerDataName;
        public readonly int DisplayOrder;
        public readonly CharacterData CharacterData;

        public CharacterInfo(string name, int displayOrder, CharacterData data)
        {
            PlayerDataName = name;
            DisplayOrder = displayOrder;
            CharacterData = data;
        }
    }

    public interface ICharacterDataAlgorithm
    {
        //void LevelUp(int oldLevel, ref CharacterData data); //the callee should have access to SQ actor
        //TODO damage, regain and die?
    }

    public static class CharacterRegistry
    {
        private static int _nextType = 10; //skip 789 that might be used by old mods
        private static readonly Dictionary<string, CharacterInfo> _characters = new Dictionary<string, CharacterInfo>();
        private static readonly Dictionary<int, CharacterConfigInfo> _configs = new Dictionary<int, CharacterConfigInfo>();

        static CharacterRegistry()
        {
            RegisterCharacter("homura", 0, new CharacterData
            {
                Life = 88,
                LifeUp = 5,
                Soul = 5820 * 3,
                SoulUp = 102,
                Attack = 10,
                AttackUp = 1,
                RegainCycle = 5,
                RegainRate = 2,
                AttackOffset = 0.5f,
            });
            RegisterCharacterConfig("homura", 0);
            RegisterCharacterConfig("homura", 5);
            RegisterCharacter("kyouko", 1, new CharacterData
            {
                Life = 95,
                LifeUp = 6,
                Soul = 7914 * 3,
                SoulUp = 87,
                Attack = 10,
                AttackUp = 1,
                RegainCycle = 5,
                RegainRate = 2,
                AttackOffset = 0.1f,
            });
            RegisterCharacterConfig("kyouko", 1);
            RegisterCharacter("madoka", 2, new CharacterData
            {
                Life = 92,
                LifeUp = 5,
                Soul = 9625 * 3,
                SoulUp = 128,
                Attack = 10,
                AttackUp = 1,
                RegainCycle = 5,
                RegainRate = 2,
                AttackOffset = 0.1f,
            });
            RegisterCharacterConfig("madoka", 2);
            RegisterCharacter("mami", 3, new CharacterData
            {
                Life = 110,
                LifeUp = 6,
                Soul = 7426 * 3,
                SoulUp = 84,
                Attack = 10,
                AttackUp = 1,
                RegainCycle = 5,
                RegainRate = 2,
                AttackOffset = 0.1f,
            });
            RegisterCharacterConfig("mami", 3);
            RegisterCharacter("sayaka", 4, new CharacterData
            {
                Life = 138,
                LifeUp = 7,
                Soul = 6645 * 3,
                SoulUp = 81,
                Attack = 10,
                AttackUp = 1,
                RegainCycle = 3,
                RegainRate = 1,
                AttackOffset = 0.1f,
            });
            RegisterCharacterConfig("sayaka", 4);
            RegisterCharacter("qb", 6, new CharacterData
            {
                Life = 1,
                LifeUp = 0,
                Soul = 1,
                SoulUp = 0,
                Attack = 10,
                AttackUp = 1,
                RegainCycle = 5,
                RegainRate = 2,
                AttackOffset = 0.1f,
            });
            RegisterCharacterConfig("qb", 6);
        }

        public static int GetNextFreeType()
        {
            return _nextType++;
        }

        public static void RegisterCharacter(string name, int displayOrder, CharacterData data, ICharacterDataAlgorithm algorithm = null)
        {
            //internally assign a actor.type
            if (algorithm != null)
            {
                throw new NotImplementedException();
            }
            _characters.Add(name, new CharacterInfo(name, displayOrder, data));
        }

        public static void RegisterCharacterConfig(string charName, int type)
        {
            _configs.Add(type, new CharacterConfigInfo(_characters[charName], type));
        }

        public static CharacterConfigInfo GetCharacterConfigInfo(int type)
        {
            if (_configs.TryGetValue(type, out var cc))
            {
                return cc;
            }
            return null;
        }
    }
}
