namespace Irelia_Reloaded
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;

    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy.SDK.Events;

    using EloBuddy.SDK.Rendering;

    internal class Program
    {
        private static int gatotsuTick;

        private static Item Botrk; 

        private static Item Cutlass;

        private static Item Omen;

        private static Spell.Targeted Q;

        private static Spell.Active W;

        private static Spell.Targeted E;
        
        private static Spell.Skillshot R;

        private static bool HasSheenBuff
            => Player.HasBuff("sheen") || Player.HasBuff("LichBane") || Player.HasBuff("ItemFrozenFist");

        public static bool HasSpell(string s)
        {
            return EloBuddy.Player.Spells.FirstOrDefault(o => o.SData.Name.Contains(s)) != null;
        }

        private static Spell.Targeted IgniteSlot;
        private static IEnumerable<Obj_AI_Minion> entities;

        private static Menu Menu, comboMenu, harassMenu, 
                          ksMenu, lastHitMenu, farmingMenu, 
                 jungleClearMenu, waveClearMenu, drawMenu,
            miscMenu;

        //private static Orbwalking.Orbwalker Orbwalker { get; set; }

        private static AIHeroClient Player => ObjectManager.Player;

        private static bool UltActivated => Player.HasBuff("IreliaTranscendentBladesSpell");

        public static void GameOnOnGameLoad(EventArgs args)
        {
            if (Player.CharData.BaseSkinName != "Irelia")
            {
                return;
            }

            // Setup Spells
            Q = new Spell.Targeted(SpellSlot.Q, 650);
            W = new Spell.Active(SpellSlot.W, ((uint)Player.GetAutoAttackRange()));
            E = new Spell.Targeted(SpellSlot.E, 425);
            R = new Spell.Skillshot(SpellSlot.R, 1000, SkillShotType.Linear, 500, 1600, 120);

            // Setup Ignite
            if (HasSpell("SummonerDot"))
            {
                IgniteSlot = new Spell.Targeted(ObjectManager.Player.GetSpellSlotFromName("SummonerDot"), 600);
            }
            // Add skillshots


            // Create Items
            Botrk = new Item(ItemId.Blade_of_the_Ruined_King);
            Cutlass = new Item(ItemId.Bilgewater_Cutlass);
            Omen = new Item(ItemId.Randuins_Omen);

            // Create Menu
            SetupMenu();

            Chat.Print("<font color=\"#7CFC00\"><b>Irelia Reloaded:</b></font> Loaded");

            // Setup Dmg Indicator
          //  FarofakidsUtility.HpBarDamageIndicator.DamageToUnit = DamageToUnit; no work
          //  FarofakidsUtility.HpBarDamageIndicator.Enabled = true;    no work
            

            // Subscribe to needed events
            // Game.OnUpdate += Game_OnGameUpdate;
            Game.OnTick += Game_OnGameUpdate;
            Drawing.OnDraw += DrawingOnOnDraw;
            FarofakidsUtility.AntiGapcloser.OnEnemyGapcloser += AntiGapcloserOnOnEnemyGapcloser;
            Interrupter.OnInterruptableSpell += InterrupterOnOnPossibleToInterrupt;

            // to get Q tickcount in least amount of lines.
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
          /*  Obj_AI_Base.OnBuffGain += (sender, EventArgs) =>
            {
                if (sender.IsMe) Console.WriteLine(EventArgs.Buff.Name);
            };*/

        }

        private static void InterrupterOnOnPossibleToInterrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            var spell = args;
            var unit = sender;

            if (spell.DangerLevel != DangerLevel.High || !unit.CanStunTarget())
            {
                return;
            }

            var interruptE = miscMenu["interruptE"].Cast<CheckBox>().CurrentValue;
            var interruptQe = miscMenu["interruptQE"].Cast<CheckBox>().CurrentValue;

            if (E.IsReady() && E.IsInRange(unit) && interruptE)
            {
                E.Cast(unit);
            }

            if (Q.IsReady() && E.IsReady() && Q.IsInRange(unit) && interruptQe)
            {
                Q.Cast(unit);

                var timeToArrive = (int)(1000 * Player.Distance(unit) / 2200f + Game.Ping);
                Core.DelayAction(() => E.Cast(unit), timeToArrive);
            }
        }

        private static void AntiGapcloserOnOnEnemyGapcloser(FarofakidsUtility.ActiveGapcloser gapcloser)
        {
            if (gapcloser.Sender.IsValidTarget() && miscMenu["gapcloserE"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                E.Cast(gapcloser.Sender);
            }
        }

        private static void Combo()
        {
            var useQ = comboMenu["useQ"].Cast<CheckBox>().CurrentValue;
            var useW = comboMenu["useW"].Cast<CheckBox>().CurrentValue;
            var useE = comboMenu["useE"].Cast<CheckBox>().CurrentValue;
            var useR = comboMenu["useR"].Cast<CheckBox>().CurrentValue;
            var minQRange = comboMenu["minQRange"].Cast<Slider>().CurrentValue;
            var useEStun = comboMenu["useEStun"].Cast<CheckBox>().CurrentValue;
            var useQGapclose = comboMenu["useQGapclose"].Cast<CheckBox>().CurrentValue;
            var useWBeforeQ = comboMenu["useWBeforeQ"].Cast<CheckBox>().CurrentValue;
            var procSheen = comboMenu["procSheen"].Cast<CheckBox>().CurrentValue;
            var useIgnite = comboMenu["useIgnite"].Cast<CheckBox>().CurrentValue;
            var useRGapclose = comboMenu["useRGapclose"].Cast<CheckBox>().CurrentValue;

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (target == null && useQGapclose)
            {
                /*var minionQ =
                    MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .FirstOrDefault(
                            x =>
                            Q.IsKillable(x) && Q.IsInRange(x)
                            && x.Distance(HeroManager.Enemies.OrderBy(y => y.Distance(Player)).FirstOrDefault())
                            < Player.Distance(HeroManager.Enemies.OrderBy(z => z.Distance(Player)).FirstOrDefault()));*/

                var minionQ = EntityManager.MinionsAndMonsters.EnemyMinions
                        .FirstOrDefault(
                            x =>
                            Player.GetSpellDamage(x, SpellSlot.Q) >= x.Health && Q.IsInRange(x)
                            && x.Distance(EntityManager.Heroes.Enemies.OrderBy(y => y.Distance(Player)).FirstOrDefault())
                            < Player.Distance(EntityManager.Heroes.Enemies.OrderBy(z => z.Distance(Player)).FirstOrDefault()));


              /*  var minionQ = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(it => 
                it.IsValidTarget(Q.Range) && it.Distance(Player) >= 
                Player.GetAutoAttackRange(it) && Player.GetSpellDamage(it, SpellSlot.Q) >= it.Health);*/

                if (minionQ != null && Player.Mana >
ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).SData.Mana * 2)
                {
                    //Q.CastOnUnit(minionQ);
                    Q.Cast(minionQ);
                    return;
                }

                if (useRGapclose)
                {
                    var minionR =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                x =>
                                x.IsValidTarget() && x.Distance(Player) < Q.Range && x.CountEnemiesInRange(Q.Range) >= 1)
                            .FirstOrDefault(
                                x =>
                                x.Health - Player.GetSpellDamage(x, SpellSlot.R) < Player.GetSpellDamage(x, SpellSlot.Q));

                    if (minionR != null)
                    {
                        R.Cast(minionR);
                    }
                }
            }

            // Get target that is in the R range
            var rTarget = TargetSelector.GetTarget(R.Range, DamageType.Physical);
            if (useR && UltActivated && rTarget.IsValidTarget())
            {
                if (procSheen)
                {
                    // Fire Ult if player is out of AA range, with Q not up or not in range
                    if (target.Distance(Player) > Player.GetAutoAttackRange(Player))
                    {
                        R.Cast(rTarget);
                    }
                    else
                    {
                        if (!HasSheenBuff)
                        {
                            R.Cast(rTarget);
                        }
                    }
                }
                else
                {
                    R.Cast(rTarget);
                }
            }

            if (!target.IsValidTarget())
            {
                return;
            }

            if (Botrk.IsReady())
            {
                Botrk.Cast(target);
            }

            if (Cutlass.IsReady())
            {
                Cutlass.Cast(target);
            }

            if (Omen.IsReady() && Omen.IsInRange(target)
                && target.Distance(Player) > Player.GetAutoAttackRange(Player))
            {
                Omen.Cast();
            }

            if (useIgnite && target != null && target.IsValidTarget(600)
                && (IgniteSlot.IsReady()
                    && Player.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health))
            {
                IgniteSlot.Cast(target);
            }

            if (useWBeforeQ)
            {
                if (useW && W.IsReady())
                {
                    W.Cast();
                }

                if (useQ && Q.IsReady() && target.Distance(Player.ServerPosition) > minQRange)
                {
                    //Q.CastOnUnit(target);
                    Q.Cast(target);
                }
            }
            else
            {
                if (useQ && Q.IsReady() && target.Distance(Player.ServerPosition) > minQRange)
                {
                    //Q.CastOnUnit(target);
                    Q.Cast(target);
                }

                if (useW && W.IsReady())
                {
                    W.Cast();
                }
            }

            if (useEStun)
            {
                if (target.CanStunTarget() && useE && E.IsReady())
                {
                    E.Cast(target);
                }
            }
            else
            {
                if (useE && E.IsReady())
                {
                    E.Cast(target);
                }
            }

            if (useR && R.IsReady() && !UltActivated)
            {
                R.Cast(target);
            }
        }

        private static float DamageToUnit(AIHeroClient hero)
        {
            float dmg = 0;

            if (Q.IsReady())
            {
                dmg += Player.GetSpellDamage(hero, SpellSlot.Q);
            }
            if (W.IsReady())
            {
                dmg += Player.GetSpellDamage(hero, SpellSlot.W);
            }
            if (E.IsReady())
            {
                dmg += Player.GetSpellDamage(hero, SpellSlot.E);
            }
            if (R.IsReady())
            {
                dmg += Player.GetSpellDamage(hero, SpellSlot.R) * 4;
            }
            if (Botrk.IsReady())
            {
                dmg += (float)Player.GetItemDamage(hero, ItemId.Blade_of_the_Ruined_King);
            }

            if (Cutlass.IsReady())
            {
                dmg += (float)Player.GetItemDamage(hero, ItemId.Bilgewater_Cutlass);
            }

            return dmg;
        }

        private static void DrawingOnOnDraw(EventArgs args)
        {
            var drawQ = drawMenu["drawQ"].Cast<CheckBox>().CurrentValue;
            var drawE = drawMenu["drawE"].Cast<CheckBox>().CurrentValue;
            var drawR = drawMenu["drawR"].Cast<CheckBox>().CurrentValue;
            var drawStunnable = drawMenu["drawStunnable"].Cast<CheckBox>().CurrentValue;
            var p = Player.Position;

            if (drawQ)
            {
                Circle.Draw(Q.IsReady() ? SharpDX.Color.Aqua : SharpDX.Color.Red, Q.Range + E.Range, p);
            }

            if (drawE)
            {
                Circle.Draw(E.IsReady() ? SharpDX.Color.Aqua : SharpDX.Color.Red, E.Range, p);
            }

            if (drawR)
            {
                Circle.Draw(R.IsReady() ? SharpDX.Color.Aqua : SharpDX.Color.Red, R.Range, p);
            }

            if (drawMenu["drawKillableQ"].Cast<CheckBox>().CurrentValue)
            foreach (var minion in
                EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => Player.GetSpellDamage(x, SpellSlot.Q) > x.Health))
            {
                Circle.Draw(SharpDX.Color.FromAbgr(124), 65, 3, minion.Position);
            }

            if (!drawStunnable)
            {
                return;
            }

            foreach (var unit in
                ObjectManager.Get<AIHeroClient>().Where(x => x.CanStunTarget() && x.IsValidTarget() && x.IsEnemy))
            {
                var drawPos = Drawing.WorldToScreen(unit.Position);
                var textSizea = Drawing.GetTextEntent("Stunnable", 22);
                Drawing.DrawText(drawPos.X - textSizea.Width / 2f, drawPos.Y, Color.Aqua, "Stunnable");
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            KillSteal();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) ||
                     Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                {
                    JungleClear();
                }
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                {
                    WaveClear();
                }
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
              //  Flee();
            }

        }

        private static void Harass()
        {
            var useQ = harassMenu["UseQHarass"].Cast<CheckBox>().CurrentValue;
            var useW = harassMenu["UseWHarass"].Cast<CheckBox>().CurrentValue;
            var useE = harassMenu["UseEHarass"].Cast<CheckBox>().CurrentValue;

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (useQ && Q.IsReady())
            {
                //Q.CastOnUnit(target);
                Q.Cast(target);
            }

            if (useW && W.IsReady() && Player.IsInAutoAttackRange(target))
            {
                W.Cast();
            }

            if (useE && E.IsReady())
            {
                //E.CastOnUnit(target);
                E.Cast(target);
            }
        }

        private static void JungleClear()
        {
            var useQ = jungleClearMenu["UseQJungleClear"].Cast<CheckBox>().CurrentValue;
            var useW = jungleClearMenu["UseWJungleClear"].Cast<CheckBox>().CurrentValue;
            var useE = jungleClearMenu["UseEJungleClear"].Cast<CheckBox>().CurrentValue;

            var orbwalkerTarget = Orbwalker.ForcedTarget;// Orbwalker.GetTarget();
            var minion = orbwalkerTarget as Obj_AI_Minion;
            
            if (minion == null || minion.Team != GameObjectTeam.Neutral)
            {
                if (minion != null || !Q.IsReady())
                {
                    return;
                }

             /*   var bestQMinion =
                    MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral)
                        .OrderByDescending(x => x.MaxHealth)
                        .FirstOrDefault();*/
                var bestQminionAAAA =
                    EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Position, Q.Range)
                    .OrderByDescending(x => x.MaxHealth)
                    .FirstOrDefault();
                 var minionAAAAA =
                     EntityManager.MinionsAndMonsters.Monsters.OrderByDescending(a => a.MaxHealth)
                         .FirstOrDefault();


                if (bestQminionAAAA != null)
                {
                    Q.Cast(bestQminionAAAA);
                }

                return;
            }

            if (useQ && Q.IsReady())
            {
                Q.Cast(minion);
            }

            if (useW && Player.Distance(minion) < Player.GetAutoAttackRange(Player))
            {
                W.Cast();
            }

            if (useE && E.IsReady())
            {
                //E.CastOnUnit(minion);
                E.Cast(minion);
            }
        }

        private static void KillSteal()
        {
            var useQ = ksMenu["useQKS"].Cast<CheckBox>().CurrentValue;
            var useR = ksMenu["useRKS"].Cast<CheckBox>().CurrentValue;
            var useIgnite = ksMenu["useIgniteKS"].Cast<CheckBox>().CurrentValue;

            if (useQ && Q.IsReady())
            {
                var bestTarget =
                    ObjectManager.Get<AIHeroClient>()
                        .Where(x => x.IsValidTarget(Q.Range) && Player.GetSpellDamage(x, SpellSlot.Q) > x.Health)
                        .OrderBy(x => x.Distance(Player))
                        .FirstOrDefault();

                if (bestTarget != null)
                {
                    Q.Cast(bestTarget);
                }
            }

            if (useR && (R.IsReady() || UltActivated))
            {
                var bestTarget =
                    ObjectManager.Get<AIHeroClient>()
                        .Where(x => x.IsValidTarget(R.Range))
                        .Where(x => Player.GetSpellDamage(x, SpellSlot.Q) > x.Health)
                        .OrderBy(x => x.Distance(Player))
                        .FirstOrDefault();

                if (bestTarget != null)
                {
                    R.Cast(bestTarget);
                }
            }

            if (useIgnite && IgniteSlot.IsReady())
            {
                var bestTarget =
                    ObjectManager.Get<AIHeroClient>()
                        .Where(x => x.IsValidTarget(600))
                        .Where(x => Player.GetSummonerSpellDamage(x, DamageLibrary.SummonerSpells.Ignite) / 5 > x.Health)
                        .OrderBy(x => x.ChampionsKilled)
                        .FirstOrDefault();

                if (bestTarget != null)
                {
                    IgniteSlot.Cast(bestTarget);
                }
            }
        }

        private static void LastHit()
        {
            var useQ = lastHitMenu["lastHitQ"].Cast<CheckBox>().CurrentValue;
            var waitTime = farmingMenu["gatotsuTime"].Cast<Slider>().CurrentValue;
            var manaNeeded = lastHitMenu["manaNeededQ"].Cast<Slider>().CurrentValue;
            var dontQUnderTower = lastHitMenu["noQMinionTower"].Cast<CheckBox>().CurrentValue;

            if (useQ && Player.Mana / Player.MaxMana * 100 > manaNeeded
                && Environment.TickCount - gatotsuTick >= waitTime * 10)
            {
                foreach (var minion in
                    EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Position, Q.Range)
                    .Where(x => Player.GetSpellDamage(x, SpellSlot.Q) > x.Health))
                    //MinionManager.GetMinions(Q.Range).Where(x => Player.GetSpellDamage(x, SpellSlot.Q) > x.Health))
                {
                    if (dontQUnderTower && !minion.IsUnderHisturret())
                    {
                        Q.Cast(minion);
                    }
                    else
                    {
                        Q.Cast(minion);
                    }
                }
            }
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += GameOnOnGameLoad;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name == "IreliaGatotsu" && sender.IsMe)
            {
                gatotsuTick = Environment.TickCount;
            }
        }

        private static void SetupMenu()
        {
            Menu = MainMenu.AddMenu("Irelia Reloaded", "cmIreliaReloaded");

            // Combo
            comboMenu = Menu.AddSubMenu("Combo Settings", "cmCombo");
            comboMenu.AddLabel(":: Q SETTINGS ::");
            comboMenu.Add("useQ", new CheckBox("Use Q"));
            comboMenu.Add("useQGapclose", new CheckBox("Gapclose with Q"));
            comboMenu.Add("minQRange", new Slider("Minimum Q Range", 250, 20, 400));

            comboMenu.AddLabel(":: W SETTINGS ::");
            comboMenu.Add("useW", new CheckBox("Use W"));
            comboMenu.Add("useWBeforeQ", new CheckBox("Use W before Q"));

            comboMenu.AddLabel(":: E SETTINGS ::");
            comboMenu.Add("useE", new CheckBox("Use E"));
            comboMenu.Add("useEStun", new CheckBox("Only Use E to Stun", false));

            comboMenu.AddLabel(":: R SETTINGS ::");
            comboMenu.Add("useR", new CheckBox("Use R"));
            comboMenu.Add("procSheen", new CheckBox("Proc Sheen Before Firing R"));
            comboMenu.Add("useRGapclose", new CheckBox("Use R to Weaken Minion to Gapclose"));

            comboMenu.AddLabel(":: OTHER SETTINGS ::");
            comboMenu.Add("useIgnite", new CheckBox("Use Ignite"));

            // Harass
            harassMenu = Menu.AddSubMenu("Harass Settings", "cmHarass");
            harassMenu.Add("UseQHarass", new CheckBox("Use Q", false));
            harassMenu.Add("UseWHarass", new CheckBox("Use W", false));
            harassMenu.Add("UseEHarass", new CheckBox("Use E", false));
            harassMenu.Add("HarassMana", new Slider("Harass Mana %", 75, 0));

            // KS
            ksMenu = Menu.AddSubMenu("KillSteal Settings", "cmKS");
            ksMenu.Add("useQKS", new CheckBox("KS With Q"));
            ksMenu.Add("useRKS", new CheckBox("KS With R", false));
            ksMenu.Add("useIgniteKS", new CheckBox("KS with Ignite"));

            // Farming
            farmingMenu = Menu.AddSubMenu("Farming Settings", "cmFarming");
            farmingMenu.Add("gatotsuTime", new Slider("Legit Q Delay (MS)", 250, 0, 1500));

            lastHitMenu = Menu.AddSubMenu("Last Hit", "cmLastHit");
            lastHitMenu.Add("lastHitQ", new CheckBox("Last Hit with Q", false));
            lastHitMenu.Add("manaNeededQ", new Slider("Last Hit Mana %", 35));
            lastHitMenu.Add("noQMinionTower", new CheckBox("Don't Q Minion Undertower"));

            // Wave Clear SubMenu
            waveClearMenu = Menu.AddSubMenu("Wave Clear", "cmWaveClear");
            waveClearMenu.Add("waveclearQ", new CheckBox("Use Q"));
            waveClearMenu.Add("waveclearQKillable", new CheckBox("Only Q Killable Minion"));
            waveClearMenu.Add("waveclearW", new CheckBox("Use W"));
            waveClearMenu.Add("waveclearR", new CheckBox("Use R", false));
            waveClearMenu.Add("waveClearMana", new Slider("Wave Clear Mana %", 20));

            jungleClearMenu = Menu.AddSubMenu("Jungle Clear", "cmJungleClear");
            jungleClearMenu.Add("UseQJungleClear", new CheckBox("Use Q"));
            jungleClearMenu.Add("UseWJungleClear", new CheckBox("Use W"));
            jungleClearMenu.Add("UseEJungleClear", new CheckBox("Use E"));

            // Drawing
            drawMenu = Menu.AddSubMenu("Drawing Settings", "cmDraw");
            drawMenu.Add("drawQ", new CheckBox("Draw Q"));
            drawMenu.Add("drawE", new CheckBox("Draw E"));
            drawMenu.Add("drawR", new CheckBox("Draw R"));
            drawMenu.Add("drawDmg", new CheckBox("Draw Combo Damage"));
            drawMenu.Add("drawStunnable", new CheckBox("Draw Stunnable"));
            drawMenu.Add("drawKillableQ", new CheckBox("Draw Minions Killable with Q", false));

            // Misc
            miscMenu =  Menu.AddSubMenu("Miscellaneous Settimgs", "cmMisc");
            miscMenu.Add("interruptE", new CheckBox("E to Interrupt"));
            miscMenu.Add("interruptQE", new CheckBox("Use Q & E to Interrupt"));
            miscMenu.Add("gapcloserE", new CheckBox("Use E on Gapcloser"));


            var color = Color.FromArgb(124, 252, 0);
            Menu.AddLabel("Irelia Reloaded " + Assembly.GetExecutingAssembly().GetName().Version);
            Menu.AddLabel("Made by ChewyMoon, PORT BY FAROFAKIDS");

        }

        private static void WaveClear()
        {
            var useQ = waveClearMenu["waveclearQ"].Cast<CheckBox>().CurrentValue;
            var useQKillable = waveClearMenu["waveclearQKillable"].Cast<CheckBox>().CurrentValue;
            var useW = waveClearMenu["waveclearW"].Cast<CheckBox>().CurrentValue;
            var useR = waveClearMenu["waveclearR"].Cast<CheckBox>().CurrentValue;
            var reqMana = waveClearMenu["waveclearMana"].Cast<Slider>().CurrentValue;
            var waitTime = farmingMenu["gatotsuTime"].Cast<Slider>().CurrentValue;
            var dontQUnderTower = lastHitMenu["noQMinionTower"].Cast<CheckBox>().CurrentValue;
            
            if (Player.ManaPercent < reqMana)
            {
                return;
            }
            
            if (useQ && Q.IsReady() && Environment.TickCount - gatotsuTick >= waitTime)
            {
                if (useQKillable)
                {
                    var minion =
                        EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Position, Q.Range)
                        .FirstOrDefault(
                            x => Player.GetSpellDamage(x, SpellSlot.Q) > x.Health && (!dontQUnderTower || !x.IsUnderTurret()));

                    if (minion != null)
                    {
                        
                        Q.Cast(minion);
                    }
                }
                else
                {
                    Q.Cast(EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Position, Q.Range).FirstOrDefault());
                }
            }

            if (useW && W.IsReady())
            {
                if (Orbwalker.ForcedTarget is Obj_AI_Minion && W.IsInRange(Orbwalker.ForcedTarget.Position))
                {
                    W.Cast();
                }
            }

            if ((!useR || !R.IsReady())
                && (!R.IsReady() || !UltActivated || Player.CountEnemiesInRange(R.Range + 100) != 0))
            {
                return;
            }

            // Get best position for ult
            //            var pos = R.GetLineFarmLocation(MinionManager.GetMinions(R.Range));
            var posAAA = EntityManager.MinionsAndMonsters.GetLineFarmLocation(entities, R.Width, (int)R.Range, Player.Position.To2D());
       
            R.Cast(posAAA.CastPosition);
        }

    }

    public static class Extension
    {

        public static bool CanStunTarget(this AttackableUnit unit)
        {
            return unit.HealthPercent > ObjectManager.Player.HealthPercent;
        }

        public static SharpDX.Color ToSharpDxColor(this Color color)
        {
            return new SharpDX.Color(color.R, color.G, color.B, color.A);
        }

    }
}