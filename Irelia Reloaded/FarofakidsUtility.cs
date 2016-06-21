using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using EloBuddy;
using EloBuddy.SDK.Events;

namespace Irelia_Reloaded
{
    public class FarofakidsUtility
    {
        public static class HpBarDamageIndicator
        {
            public static Color Color;
            public static bool Enabled;

            public static DamageToUnitDelegate DamageToUnit { get; set; }

            public delegate float DamageToUnitDelegate(AIHeroClient hero);
        }

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

        public delegate void OnGapcloseH(ActiveGapcloser gapcloser);

        public static class AntiGapcloser
        {
            public static List<ActiveGapcloser> ActiveGapclosers;
            public static List<Gapcloser> Spells;

            public static event OnGapcloseH OnEnemyGapcloser;

            public static void Initialize() { }
            public static void Shutdown() { }
        }

    }
}
