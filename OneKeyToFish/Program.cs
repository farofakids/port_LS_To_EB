using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
//using Color = System.Drawing.Color;
using EloBuddy.SDK.Rendering;

namespace OneKeyToFish
{
    internal class Program
    {
        private static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static Menu Menu, comboMenu, harassMenu, miscMenu, drawMenu;
        private static Vector3? LastHarassPos { get; set; }
        private static AIHeroClient DrawTarget { get; set; }
        private static Geometry.Polygon.Rectangle RRectangle { get; set; }
        private static readonly HpBarIndicator Indicator = new HpBarIndicator();

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += GameOnOnGameLoad;
        }

        //#region OneKeyToFish :: Menu

        private static void CreateMenu()
        {
            Menu = MainMenu.AddMenu("OneKeyToFish", "cmFizzKAPPA - PORT BY FAROFAKIDS");
            
            // Combo
            comboMenu = Menu.AddSubMenu("Combo", "combo");
            comboMenu.Add("UseQCombo", new CheckBox("Use Q"));
            comboMenu.Add("UseWCombo", new CheckBox("Use W"));
            comboMenu.Add("UseECombo", new CheckBox("Use E"));
            comboMenu.Add("UseRCombo", new CheckBox("Use R"));
            comboMenu.Add("UseREGapclose", new CheckBox("Use R, then E for gapclose if killable"));
            

            // Harass
            harassMenu = Menu.AddSubMenu("Harass", "harass");
            harassMenu.Add("UseQMixed", new CheckBox("Use Q"));
            harassMenu.Add("UseWMixed", new CheckBox("Use W"));
            harassMenu.Add("UseEMixed", new CheckBox("Use E"));
            harassMenu.AddLabel("E Mode:");
            var EMode = harassMenu.Add("UseEHarassMode", new Slider("E Mode:", 1, 0, 1));
            var modeArrayE = new[] { "Back to Position", "On Enemy" };
            EMode.DisplayName = modeArrayE[EMode.CurrentValue];
            EMode.OnValueChange +=
            delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
            {
                sender.DisplayName = modeArrayE[changeArgs.NewValue];
            };

            // Misc
            miscMenu = Menu.AddSubMenu("Misc", "miscerino");
            miscMenu.Add("UseETower", new CheckBox("Dodge tower shots with E"));
            miscMenu.AddLabel("Use W:");
            var UseWWhen = miscMenu.Add("UseWWhen", new Slider("Use W:", 0, 0, 1));
            var modearrayW = new[] { "Before Q", "After Q" };
            UseWWhen.DisplayName = modearrayW[UseWWhen.CurrentValue];
            UseWWhen.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = modearrayW[changeArgs.NewValue];
                };

            // Drawing
            drawMenu = Menu.AddSubMenu("Drawing", "draw");
            drawMenu.Add("DrawQ", new CheckBox("Draw Q"));
            drawMenu.Add("DrawE", new CheckBox("Draw E"));
            drawMenu.Add("DrawR", new CheckBox("Draw R"));
            drawMenu.Add("DrawRPred", new CheckBox("Draw R Prediction"));
            drawMenu.Add("Dind", new CheckBox("Draw Damage Indicator"));

        }

        //#endregion OneKeyToFish :: Menu

        //#region Spells

        private static Spell.Targeted Q { get; set; }
        private static Spell.Active W { get; set; }
        private static Spell.Skillshot E { get; set; }
        private static Spell.Skillshot R { get; set; }

        //#endregion Spells

        //#region GameLoad

        private static void GameOnOnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != "Fizz")
            {
                return;
            }

            Q = new Spell.Targeted(SpellSlot.Q, 550);
            W = new Spell.Active(SpellSlot.W, (uint)Player.GetAutoAttackRange(Player));
            E = new Spell.Skillshot(SpellSlot.E, 400, SkillShotType.Circular, 250, int.MaxValue, 330);
            E.AllowedCollisionCount = 0;
            R = new Spell.Skillshot(SpellSlot.R, 1300, SkillShotType.Linear, 250, 1200, 80);
            R.AllowedCollisionCount = 0;
            R.MinimumHitChance = HitChance.High;

            CreateMenu();

      //      Utility.HpBarDamageIndicator.DamageToUnit = DamageToUnit;
        //    Utility.HpBarDamageIndicator.Enabled = true;

            RRectangle = new Geometry.Polygon.Rectangle(Player.Position, Player.Position, R.Width);

            Game.OnTick += GameOnOnUpdate;
            Obj_AI_Base.OnProcessSpellCast += ObjAiBaseOnOnProcessSpellCast;
            Drawing.OnDraw += DrawingOnOnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;

            Chat.Print("<font color=\"#7CFC00\"><b>OneKeyToFish:</b></font> Loaded");
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            foreach (
                     var enemy in
                        ObjectManager.Get<AIHeroClient>()
                        .Where(ene => ene.IsValidTarget() && ene.IsEnemy && !ene.IsZombie))
            {
                if (drawMenu["Dind"].Cast<CheckBox>().CurrentValue)
                {
                    Indicator.unit = enemy;
                    Indicator.drawDmg(DamageToUnit(enemy), new ColorBGRA(255, 204, 0, 170));
                    
                }

            }
        }

        private static void DrawingOnOnDraw(EventArgs args)
        {
            var drawQ = drawMenu["DrawQ"].Cast<CheckBox>().CurrentValue;
            var drawE = drawMenu["DrawE"].Cast<CheckBox>().CurrentValue;
            var drawR = drawMenu["DrawR"].Cast<CheckBox>().CurrentValue;
            var drawRPred = drawMenu["DrawRPred"].Cast<CheckBox>().CurrentValue;
            var p = Player.Position;

            if (drawQ)
            {
                Circle.Draw(Q.IsReady() ? Color.Aqua : Color.Red, Q.Range, p);
            }

            if (drawE)
            {
                Circle.Draw(E.IsReady() ? Color.Aqua : Color.Red, E.Range, p);
            }

            if (drawR)
            {
                Circle.Draw(R.IsReady() ? Color.Aqua : Color.Red, R.Range, p);
            }

            if (drawRPred && R.IsReady() && DrawTarget.IsValidTarget())
            {
                RRectangle.Draw(System.Drawing.Color.CornflowerBlue, 3);
            }
        }

        private static float DamageToUnit(AIHeroClient target)
        {
            var damage = 0d;

            damage += Player.GetAutoAttackDamage(target);

            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.Q);
            }

            if (W.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.W);
            }

            if (E.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.E);
            }

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.R);
            }

            return (float) damage;
        }

        private static void ObjAiBaseOnOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender is Obj_AI_Turret && args.Target.IsMe && E.IsReady() && miscMenu["UseETower"].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(Game.CursorPos);
            }

            if (!sender.IsMe)
            {
                return;
            }

            if (args.SData.Name == "FizzPiercingStrike")
            {
                if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
                {
                    Core.DelayAction(() => W.Cast(), (int)(sender.Spellbook.CastEndTime - Game.Time) + Game.Ping / 2 + 250);
                }
                else if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass &&
                         harassMenu["UseEHarassMode"].Cast<Slider>().CurrentValue == 0)
                {
                    Core.DelayAction(() => { JumpBack = true; }, (int)(sender.Spellbook.CastEndTime - Game.Time) + Game.Ping / 2 + 250);
                }
            }

            if (args.SData.Name == "fizzjumptwo" || args.SData.Name == "fizzjumpbuffer")
            {
                LastHarassPos = null;
                JumpBack = false;
            }
        }

        public static bool JumpBack { get; set; }

        //#endregion GameLoad

        //#region Update

        private static void GameOnOnUpdate(EventArgs args)
        {
            DrawTarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);

            if (DrawTarget.IsValidTarget())
            {
                RRectangle.Start = Player.Position.To2D();
                RRectangle.End = R.GetPrediction(DrawTarget).CastPosition.To2D();
                RRectangle.UpdatePolygon();
            }

            if (!Player.CanCast)
            {
                return;
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

        public static void CastRSmart(AIHeroClient target)
        {
            var castPosition = R.GetPrediction(target).CastPosition;
            castPosition = Player.ServerPosition.Extend(castPosition, R.Range).To3D();
            R.Cast(castPosition);
        }

        private static void DoCombo()
        {
            var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (comboMenu["UseREGapclose"].Cast<CheckBox>().CurrentValue && CanKillWithUltCombo(target) && Q.IsReady() && W.IsReady() &&
                E.IsReady() && R.IsReady() && (Player.Distance(target) < Q.Range + E.Range * 2))
            {
                CastRSmart(target);

                E.Cast(Player.ServerPosition.Extend(target.ServerPosition, E.Range - 1).To3D());
                E.Cast(Player.ServerPosition.Extend(target.ServerPosition, E.Range - 1).To3D());

                W.Cast();
                Q.Cast(target);
            }
            else
            {
                if (R.IsReady() && UseRCombo)
                {
                    if (Player.GetSpellDamage(target, SpellSlot.R) > target.Health)
                    {
                        CastRSmart(target);
                    }

                    if (DamageToUnit(target) > target.Health)
                    {
                        CastRSmart(target);
                    }

                    if ((Q.IsReady() || E.IsReady()))
                    {
                        CastRSmart(target);
                    }

                    if (Player.IsInAutoAttackRange(target))
                    {
                        CastRSmart(target);
                    }
                }

                // Use W Before Q
                if (W.IsReady() && UseWCombo && miscMenu["UseWWhen"].Cast<Slider>().CurrentValue == 0 &&
                    (Q.IsReady() || Player.IsInAutoAttackRange(target)))
                {
                    W.Cast();
                }

                if (Q.IsReady() && UseQCombo)
                {
                    Q.Cast(target);
                }

                if (E.IsReady() && UseECombo)
                {
                    E.Cast(target);
                }
            }
        }

        public static bool CanKillWithUltCombo(AIHeroClient target)
        {
            return Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.W) + Player.GetSpellDamage(target, SpellSlot.R) >
                   target.Health;
        }

        private static void DoHarass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (LastHarassPos == null)
            {
                LastHarassPos = ObjectManager.Player.ServerPosition;
            }

            if (JumpBack)
            {
                E.Cast((Vector3) LastHarassPos);
            }

            // Use W Before Q
            if (W.IsReady() && UseWMixed && miscMenu["UseWWhen"].Cast<Slider>().CurrentValue == 0 &&
                (Q.IsReady() || Player.IsInAutoAttackRange(target)))
            {
                W.Cast();
            }

            if (Q.IsReady() && UseQMixed)
            {
                Q.Cast(target);
            }

            if (E.IsReady() && UseEMixed && harassMenu["UseEHarassMode"].Cast<Slider>().CurrentValue == 1)
            {
                E.Cast(target);
            }
        }

        //#endregion Update
       
        public static bool UseQCombo { get { return comboMenu["UseQCombo"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseWCombo { get { return comboMenu["UseWCombo"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseECombo { get { return comboMenu["UseECombo"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseRCombo { get { return comboMenu["UseRCombo"].Cast<CheckBox>().CurrentValue; } }

        public static bool UseQMixed { get { return harassMenu["UseQMixed"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseWMixed { get { return harassMenu["UseWMixed"].Cast<CheckBox>().CurrentValue; } }
        public static bool UseEMixed { get { return harassMenu["UseEMixed"].Cast<CheckBox>().CurrentValue; } }

    }

}