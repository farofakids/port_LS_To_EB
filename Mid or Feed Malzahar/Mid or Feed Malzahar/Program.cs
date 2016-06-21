using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;



namespace Mid_or_Feed_Malzahar
{
    class Program
    {
        private static Spell.Skillshot Q, W;
        private static Spell.Targeted E, R;

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 900, SkillShotType.Linear, 500, int.MaxValue, 100);
            Q.AllowedCollisionCount = 0;
            W = new Spell.Skillshot(SpellSlot.W, 800, SkillShotType.Circular, 500, 20, 240);
            W.AllowedCollisionCount = 0;
            E = new Spell.Targeted(SpellSlot.E, 650);
            R = new Spell.Targeted(SpellSlot.R, 700);

            Chat.Print("Malzahar loaded.");

            Game.OnTick += Game_OnUpdate;

        }

        private static void Game_OnUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Harass:
                    DoHarass();
                    break;

                case Orbwalker.ActiveModes.Combo:
                    DoCombo();
                    break;
            }
        }

        private static void DoHarass()
        {
            var target = TargetSelector.GetTarget(900, DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }

            foreach (var spell in
                this.SpellList.Where(x => x.IsReady())
                    .Where(x => x.Slot != SpellSlot.R)
                    .Where(x => this.GetBool("Use" + x.Slot.ToString() + "Harass")))
            {
                spell.Cast(target, this.Packets);
            }
        }
    }
}
