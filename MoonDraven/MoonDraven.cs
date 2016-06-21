// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoonDraven.cs" company="ChewyMoon">
//   Copyright (C) 2015 ChewyMoon
//   
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//   
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//   
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// <summary>
//   The MoonDraven class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MoonDraven
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Events;
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy.SDK.Rendering;

    using SharpDX;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly",
        Justification = "Reviewed. Suppression is OK here.")]
    internal class MoonDraven
    {
        private static bool IsWindingUp = false;

        public Spell.Skillshot E { get; set; }

        public AIHeroClient Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        public Spell.Active Q { get; set; }

        public int QCount
        {
            get
            {
                return (this.Player.HasBuff("dravenspinning") ? 1 : 0)
                       + (this.Player.HasBuff("dravenspinningleft") ? 1 : 0) + this.QReticles.Count;
            }
        }

        public List<QRecticle> QReticles { get; set; }

        public Spell.Skillshot R { get; set; }

        public Spell.Active W { get; set; }

        private int LastAxeMoveTime { get; set; }

        public void Load()
        {
            // Create spells
            this.Q = new Spell.Active(SpellSlot.Q, (uint)Player.GetAutoAttackRange(this.Player));
            this.W = new Spell.Active(SpellSlot.W);
            this.E = new Spell.Skillshot(SpellSlot.E, 1050, SkillShotType.Linear, 250, 1400, 130);
            E.AllowedCollisionCount = 0;
            this.R = new Spell.Skillshot(SpellSlot.R, int.MaxValue, SkillShotType.Linear, 400, 2000, 160);
            R.AllowedCollisionCount = int.MaxValue;

            this.QReticles = new List<QRecticle>();

            this.CreateMenu();

            Chat.Print("<font color=\"#7CFC00\"><b>MoonDraven:</b></font> Loaded");

            //Obj_AI_Base.OnNewPath += this.Obj_AI_Base_OnNewPath;
            GameObject.OnCreate += this.GameObjectOnOnCreate;
            GameObject.OnDelete += this.GameObjectOnOnDelete;
            AntiGapcloser.OnEnemyGapcloser += this.AntiGapcloserOnOnEnemyGapcloser;
            Interrupter.OnInterruptableSpell += this.Interrupter2OnOnInterruptableTarget2;
            //Interrupter2.OnInterruptableTarget += this.Interrupter2OnOnInterruptableTarget;
            Drawing.OnDraw += this.DrawingOnOnDraw;
            Game.OnTick += this.GameOnOnUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;
        }

        private void Obj_AI_Base_OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                IsWindingUp = false;
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                IsWindingUp = true;
            };
        }

        private void AntiGapcloserOnOnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!miscMenu["UseEGapcloser"].Cast<CheckBox>().CurrentValue || !this.E.IsReady()
                || !gapcloser.Sender.IsValidTarget(this.E.Range))
            {
                return;
            }

            this.E.Cast(gapcloser.Sender);
        }

        private void CatchAxe()
        {
            var catchOption = axeMenu["AxeMode"].Cast<Slider>().CurrentValue;

            if (((catchOption == 0 && Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
                 || (catchOption == 1 && Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.None))
                || catchOption == 2)
            {
                var bestReticle =
                    this.QReticles.Where(
                        x =>
                        x.Object.Position.Distance(Game.CursorPos)
                        < axeMenu["CatchAxeRange"].Cast<Slider>().CurrentValue)
                        .OrderBy(x => x.Position.Distance(this.Player.ServerPosition))
                        .ThenBy(x => x.Position.Distance(Game.CursorPos))
                        .ThenBy(x => x.ExpireTime)
                        .FirstOrDefault();

                if (bestReticle != null && bestReticle.Object.Position.Distance(this.Player.ServerPosition) > 100)
                {
                    var eta = 1000 * (this.Player.Distance(bestReticle.Position) / this.Player.MoveSpeed);
                    var expireTime = bestReticle.ExpireTime - Environment.TickCount;

                    if (eta >= expireTime && axeMenu["UseWForQ"].Cast<CheckBox>().CurrentValue)
                    {
                        this.W.Cast();
                    }

                    if (axeMenu["DontCatchUnderTurret"].Cast<CheckBox>().CurrentValue)
                    {
                        // If we're under the turret as well as the axe, catch the axe
                        if (this.Player.IsUnderTurret() && bestReticle.Object.Position.IsUnderTurret())
                        {
                            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.None)
                            {
                                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);
                            }
                            else
                            {
                                //this.Orbwalker.SetOrbwalkingPoint(bestReticle.Position);
                                Orbwalker.OrbwalkTo(bestReticle.Position);

                            }
                        }
                        else if (!bestReticle.Position.IsUnderTurret())
                        {
                            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.None)
                            {
                                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);
                            }
                            else
                            {
                                Orbwalker.OrbwalkTo(bestReticle.Position);

                            }
                        }
                    }
                    else
                    {
                        if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.None)
                        {
                            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);
                        }
                        else
                        {
                            Orbwalker.OrbwalkTo(bestReticle.Position);
                        }
                    }
                }
                else
                {
                    //Orbwalker.OrbwalkTo(Game.CursorPos);
                }
            }
            else
            {
             //   Orbwalker.OrbwalkTo(Game.CursorPos);
            }
        }

        private void Combo()
        {
            var target = TargetSelector.GetTarget(this.E.Range, DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            var useQ = comboMenu["UseQCombo"].Cast<CheckBox>().CurrentValue;
            var useW = comboMenu["UseWCombo"].Cast<CheckBox>().CurrentValue;
            var useE = comboMenu["UseECombo"].Cast<CheckBox>().CurrentValue;
            var useR = comboMenu["UseRCombo"].Cast<CheckBox>().CurrentValue;

            if (useQ && this.QCount < axeMenu["MaxAxes"].Cast<Slider>().CurrentValue - 1 && this.Q.IsReady()
                && Player.IsInAutoAttackRange(target) && !this.Player.Spellbook.IsAutoAttacking)
            {
                this.Q.Cast();
            }

            if (useW && this.W.IsReady()
                && Player.ManaPercent > miscMenu["UseWManaPercent"].Cast<Slider>().CurrentValue)
            {
                if (miscMenu["UseWSetting"].Cast<CheckBox>().CurrentValue)
                {
                    this.W.Cast();
                }
                else
                {
                    if (!this.Player.HasBuff("dravenfurybuff"))
                    {
                        this.W.Cast();
                    }
                }
            }

            if (useE && this.E.IsReady())
            {
                this.E.Cast(target);
            }

            if (!useR || !this.R.IsReady())
            {
                return;
            }

            // Patented Advanced Algorithms D321987
            var killableTarget =
                EntityManager.Heroes.Enemies.Where
                (x => x.IsValidTarget(2000))
                    .FirstOrDefault(
                        x =>
                        this.Player.GetSpellDamage(x, SpellSlot.R) * 2 > x.Health
                        && (!Player.IsInAutoAttackRange(x) || this.Player.CountEnemiesInRange(this.E.Range) > 2));

            if (killableTarget != null)
            {
                this.R.Cast(killableTarget);
            }
        }

        public Menu Menu, comboMenu, harassMenu, laneClearMenu, axeMenu, drawMenu, miscMenu;

        private void CreateMenu()
        {
            this.Menu = MainMenu.AddMenu("MoonDraven", "cmMoonDraven");

            // Combo
            comboMenu = Menu.AddSubMenu("Combo", "combo");
            comboMenu.Add("UseQCombo", new CheckBox("Use Q"));
            comboMenu.Add("UseWCombo", new CheckBox("Use W"));
            comboMenu.Add("UseECombo", new CheckBox("Use E"));
            comboMenu.Add("UseRCombo", new CheckBox("Use R"));
            

            // Harass
            harassMenu = Menu.AddSubMenu("Harass", "harass");
            harassMenu.Add("UseEHarass", new CheckBox("Use E"));
            harassMenu.Add("UseHarassToggle", new CheckBox("Harass! (Toggle)"));
           

            // Lane Clear
            laneClearMenu = Menu.AddSubMenu("Wave Clear", "waveclear");
            laneClearMenu.Add("UseQWaveClear", new CheckBox("Use Q"));
            laneClearMenu.Add("UseWWaveClear", new CheckBox("Use W"));
            laneClearMenu.Add("UseEWaveClear", new CheckBox("Use E", false));
            laneClearMenu.Add("WaveClearManaPercent", new Slider("Mana Percent", 50));
            

            // Axe Menu
            axeMenu = Menu.AddSubMenu("Axe Settings", "axeSetting");
            /*axeMenu.AddItem(
                new MenuItem("AxeMode", "Catch Axe on Mode:").SetValue(
                    new StringList(new[] { "Combo", "Any", "Always" }, 2)));*/
            axeMenu.AddLabel("AxeMode");
            var AxeMode = axeMenu.Add("AxeMode", new Slider("Catch Axe on Mode:", 2, 0, 2));
            var modeArrayAxe = new[] { "Combo", "Any", "Always" };
            AxeMode.DisplayName = modeArrayAxe[AxeMode.CurrentValue];
            AxeMode.OnValueChange +=
            delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
            {
                sender.DisplayName = modeArrayAxe[changeArgs.NewValue];
            };
            axeMenu.Add("CatchAxeRange", new Slider("Catch Axe Range", 800, 120, 1500));
            axeMenu.Add("MaxAxes", new Slider("Maximum Axes", 2, 1, 3));
            axeMenu.Add("UseWForQ", new CheckBox("Use W if Axe too far"));
            axeMenu.Add("DontCatchUnderTurret", new CheckBox("Don't Catch Axe Under Turret"));
            

            // Drawing
            drawMenu = Menu.AddSubMenu("Drawing", "draw");
            drawMenu.Add("DrawE", new CheckBox("Draw E"));
            drawMenu.Add("DrawAxeLocation", new CheckBox("Draw Axe Location"));
            drawMenu.Add("DrawAxeRange", new CheckBox("Draw Axe Catch Range"));
            

            // Misc Menu
            miscMenu = Menu.AddSubMenu("Misc", "misc");
            miscMenu.Add("UseWSetting", new CheckBox("Use W Instantly(When Available)", false));
            miscMenu.Add("UseEGapcloser", new CheckBox("Use E on Gapcloser"));
            miscMenu.Add("UseEInterrupt", new CheckBox("Use E to Interrupt"));
            miscMenu.Add("UseWManaPercent", new Slider("Use W Mana Percent", 50));
            miscMenu.Add("UseWSlow", new CheckBox("Use W if Slowed"));

        }

        private void DrawingOnOnDraw(EventArgs args)
        {
            var drawE = drawMenu["DrawE"].Cast<CheckBox>().CurrentValue;
            var drawAxeLocation = drawMenu["DrawAxeLocation"].Cast<CheckBox>().CurrentValue;
            var drawAxeRange = drawMenu["DrawAxeRange"].Cast<CheckBox>().CurrentValue;

            if (drawE)
            {
                Circle.Draw(E.IsReady() ? Color.Aqua : Color.Red,
                    E.Range, Player.Position);
            }

            if (drawAxeLocation)
            {
                var bestAxe =
                    this.QReticles.Where(
                        x =>
                        x.Position.Distance(Game.CursorPos) < axeMenu["CatchAxeRange"].Cast<Slider>().CurrentValue)
                        .OrderBy(x => x.Position.Distance(this.Player.ServerPosition))
                        .ThenBy(x => x.Position.Distance(Game.CursorPos))
                        .FirstOrDefault();

                if (bestAxe != null)
                {
                    Circle.Draw(Color.LimeGreen, 120, bestAxe.Position);
                }

                foreach (var axe in
                    this.QReticles.Where(x => x.Object.NetworkId != (bestAxe == null ? 0 : bestAxe.Object.NetworkId)))
                {
                    Circle.Draw(Color.Yellow, 120, axe.Position);
                }
            }

            if (drawAxeRange)
            {
                Circle.Draw(Color.DodgerBlue, axeMenu["CatchAxeRange"].Cast<Slider>().CurrentValue, Game.CursorPos);
            }
        }

        private void GameObjectOnOnCreate(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                return;
            }

            this.QReticles.Add(new QRecticle(sender, Environment.TickCount + 1800));
            Core.DelayAction(() => this.QReticles.RemoveAll(x => x.Object.NetworkId == sender.NetworkId), 1800);
        }

        private void GameObjectOnOnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                return;
            }

            this.QReticles.RemoveAll(x => x.Object.NetworkId == sender.NetworkId);
        }

        private void GameOnOnUpdate(EventArgs args)
        {
            this.QReticles.RemoveAll(x => x.Object.IsDead);

            this.CatchAxe();

            if (this.W.IsReady() && miscMenu["UseWSlow"].Cast<CheckBox>().CurrentValue && this.Player.HasBuffOfType(BuffType.Slow))
            {
                this.W.Cast();
            }

            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Harass:
                    this.Harass();
                    break;
                case Orbwalker.ActiveModes.LaneClear:
                    this.LaneClear();
                    break;
                case Orbwalker.ActiveModes.Combo:
                    this.Combo();
                    break;
            }

            if (harassMenu["UseHarassToggle"].Cast<CheckBox>().CurrentValue)
            {
                this.Harass();
            }
        }

        private void Harass()
        {
            var target = TargetSelector.GetTarget(this.E.Range, DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (harassMenu["UseEHarass"].Cast<CheckBox>().CurrentValue && this.E.IsReady())
            {
                this.E.Cast(target);
            }
        }

        private void Interrupter2OnOnInterruptableTarget2(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (!miscMenu["UseEInterrupt"].Cast<CheckBox>().CurrentValue || !this.E.IsReady() || !sender.IsValidTarget(this.E.Range))
            {
                return;
            }

            if (args.DangerLevel == DangerLevel.Medium || args.DangerLevel == DangerLevel.High)
            {
                this.E.Cast(sender);
            }
        }

        private void LaneClear()
        {
            var useQ = laneClearMenu["UseQWaveClear"].Cast<CheckBox>().CurrentValue;
            var useW = laneClearMenu["UseWWaveClear"].Cast<CheckBox>().CurrentValue;
            var useE = laneClearMenu["UseEWaveClear"].Cast<CheckBox>().CurrentValue;

            if (Player.ManaPercent < laneClearMenu["WaveClearManaPercent"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            if (useQ && this.QCount < axeMenu["MaxAxes"].Cast<Slider>().CurrentValue - 1 && this.Q.IsReady()
                && Orbwalker.ForcedTarget is Obj_AI_Minion && !this.Player.Spellbook.IsAutoAttacking
                && !IsWindingUp)
            {
                this.Q.Cast();
            }

            if (useW && this.W.IsReady()
                && Player.ManaPercent > miscMenu["UseWManaPercent"].Cast<Slider>().CurrentValue)
            {
                if (miscMenu["UseWSetting"].Cast<CheckBox>().CurrentValue)
                {
                    this.W.Cast();
                }
                else
                {
                    if (!this.Player.HasBuff("dravenfurybuff"))
                    {
                        this.W.Cast();
                    }
                }
            }

            if (!useE || !this.E.IsReady())
            {
                return;
            }

            //            var bestLocation = this.E.GetLineFarmLocation(MinionManager.GetMinions(this.E.Range));
            var bestLocation = EntityManager.MinionsAndMonsters.GetLineFarmLocation
                 (EntityManager.MinionsAndMonsters.Minions, E.Width, (int)E.Range);

            if (bestLocation.HitNumber > 1)
            {
                this.E.Cast(bestLocation.CastPosition);
            }
        }

        private void Obj_AI_Base_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            this.CatchAxe();
        }

        internal class QRecticle
        {

            public QRecticle(GameObject rectice, int expireTime)
            {
                this.Object = rectice;
                this.ExpireTime = expireTime;
            }

            public int ExpireTime { get; set; }

            public GameObject Object { get; set; }

            public Vector3 Position
            {
                get
                {
                    return this.Object.Position;
                }
            }

        }

    }
}