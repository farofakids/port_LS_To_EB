using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;

namespace MoonDraven
{
    public static class AntiGapcloser
    {
        public static List<ActiveGapcloser> ActiveGapclosers;
        public static List<Gapcloser> Spells;

        public static event OnGapcloseH OnEnemyGapcloser;

        public static void Initialize() { }
        public static void Shutdown() { }
    }

    public delegate void OnGapcloseH(ActiveGapcloser gapcloser);

    public struct ActiveGapcloser
    {
        public global::SharpDX.Vector3 End;
        public AIHeroClient Sender;
        public GapcloserType SkillType;
        public SpellSlot Slot;
        public global::SharpDX.Vector3 Start;
        public int TickCount;
    }

    public enum GapcloserType
    {
        Skillshot = 0,
        Targeted = 1
    }

    public struct Gapcloser
    {
        public string ChampionName;
        public GapcloserType SkillType;
        public SpellSlot Slot;
        public string SpellName;
    }

}
