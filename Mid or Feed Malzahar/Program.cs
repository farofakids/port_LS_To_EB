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
using EloBuddy.SDK.Rendering;
using SharpDX;



namespace Mid_or_Feed_Malzahar
{
    class Program
    {
        private static Spell.Skillshot Q, W;
        private static Spell.Targeted E, R;
        private static Menu Menu, comboMenu, harassMenu, miscMenu, drawingMenu;

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 900, SkillShotType.Linear, 500, int.MaxValue, 100);
            Q.AllowedCollisionCount = 0;
            W = new Spell.Skillshot(SpellSlot.W, 650, SkillShotType.Circular, 500, 20, 240);
            W.AllowedCollisionCount = 0;
            E = new Spell.Targeted(SpellSlot.E, 650);
            R = new Spell.Targeted(SpellSlot.R, 700);

            CreateMenu();
            Chat.Print("Malzahar loaded.");

            Game.OnTick += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;

        }

        private static void CreateMenu()
        {
            Menu = MainMenu.AddMenu("Mid or Feed", "mof" + Player.Instance.ChampionName);

            // Target Selector
            // Combo
            comboMenu = Menu.AddSubMenu("Combo", "mofCombo");
            comboMenu.Add("useQ", new CheckBox("Use Q Combo"));
            comboMenu.Add("useW", new CheckBox("Use W Combo"));
            comboMenu.Add("useE", new CheckBox("Use E Combo"));
            comboMenu.Add("useR", new CheckBox("Use R Combo"));

            // Harass
            harassMenu = Menu.AddSubMenu("Harass", "mofHarass");
            harassMenu.Add("useQ", new CheckBox("Use Q Harass"));
            harassMenu.Add("useW", new CheckBox("Use W Harass"));
            harassMenu.Add("useE", new CheckBox("Use E Harass"));

            // Misc
            miscMenu = Menu.AddSubMenu("Misc", "mofMisc");
            miscMenu.Add("InterruptQ", new CheckBox("Use InterruptQ"));
            miscMenu.Add("GapcloserQ", new CheckBox("Use anti GapcloserQ"));

            // Drawing
            drawingMenu = Menu.AddSubMenu("Drawings", "mofDrawing");
            drawingMenu.Add("DrawQ", new CheckBox("Use Q Draw"));
            drawingMenu.Add("DrawW", new CheckBox("Use W Draw"));
            drawingMenu.Add("DrawE", new CheckBox("Use E Draw"));
            drawingMenu.Add("DrawR", new CheckBox("Use R Draw"));
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            var InterruptQ = miscMenu["InterruptQ"].Cast<CheckBox>().CurrentValue;

            if (HasRBuff()) return;

            if (!sender.IsValidTarget() && !sender.IsEnemy)
            {
                return;
            }

            if (e.DangerLevel != DangerLevel.High)
            {
                return;
            }

            if (!InterruptQ)
            {
                return;
            }

            Q.Cast(sender);
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            var GapcloserQ = miscMenu["GapcloserQ"].Cast<CheckBox>().CurrentValue;

            if (HasRBuff()) return;

            if (!e.Sender.IsValidTarget())
            {
                return;
            }

            if (!GapcloserQ)
            {
                return;
            }

            Q.Cast(e.Sender);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var DrawQ = drawingMenu["DrawQ"].Cast<CheckBox>().CurrentValue;
            var DrawW = drawingMenu["DrawW"].Cast<CheckBox>().CurrentValue;
            var DrawE = drawingMenu["DrawE"].Cast<CheckBox>().CurrentValue;
            var DrawR = drawingMenu["DrawR"].Cast<CheckBox>().CurrentValue;

        if (DrawQ) Circle.Draw(Q.IsReady() ? Color.Aqua : Color.Red, Q.Range, Player.Instance.Position);
        if (DrawW) Circle.Draw(W.IsReady() ? Color.Aqua : Color.Red, W.Range, Player.Instance.Position);
        if (DrawE) Circle.Draw(E.IsReady() ? Color.Aqua : Color.Red, E.Range, Player.Instance.Position);
        if (DrawR) Circle.Draw(R.IsReady() ? Color.Aqua : Color.Red, R.Range, Player.Instance.Position);

        }

        private static bool HasRBuff()
        {
            return Player.Instance.Spellbook.IsChanneling || Player.Instance.HasBuff("MalzaharR") || Player.Instance.HasBuff("malzaharrsound");
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (HasRBuff())
            {
                Orbwalker.DisableMovement = true;
                Orbwalker.DisableAttacking = true;
            }
            else
            {
                Orbwalker.DisableMovement = false;
                Orbwalker.DisableAttacking = false;
            }
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
            var useQ = harassMenu["useQ"].Cast<CheckBox>().CurrentValue;
            var useW = harassMenu["useW"].Cast<CheckBox>().CurrentValue;
            var useE = harassMenu["useE"].Cast<CheckBox>().CurrentValue;

            var target = TargetSelector.GetTarget(900, DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (useQ && Q.IsReady()) Q.Cast(target);
            if (useW && W.IsReady()) W.Cast(target);
            if (useE && E.IsReady()) E.Cast(target);

        }

        private static void DoCombo()
        {
            var useQ = comboMenu["useQ"].Cast<CheckBox>().CurrentValue;
            var useW = comboMenu["useW"].Cast<CheckBox>().CurrentValue;
            var useE = comboMenu["useE"].Cast<CheckBox>().CurrentValue;
            var useR = comboMenu["useR"].Cast<CheckBox>().CurrentValue;
            var target = TargetSelector.GetTarget(900, DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }
            if (useQ && Q.IsReady()) Q.Cast(target);
            if (useW && W.IsReady()) W.Cast(target);
            if (useE && E.IsReady()) E.Cast(target);
            else if (useR && ShouldUseR(target))
                {
                    R.Cast(target);
                }

        }

        private static bool ShouldUseR(Obj_AI_Base target)
        {
            return Player.Instance.GetSpellDamage(target, SpellSlot.R) > target.Health;
        }
    }
}
