﻿using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using TisButAScratch.Framework;
using static TisButAScratch.Framework.GlobalVars;
using BattleTech.Save;
using BattleTech.Save.Test;
using BattleTech.UI;
using HBS.Collections;
using Localize;

namespace TisButAScratch.Patches
{
    public class SaveAndStatePatches
    {
        [HarmonyPatch(typeof(SGCharacterCreationCareerBackgroundSelectionPanel), "Done")]
        public static class SGCharacterCreationCareerBackgroundSelectionPanel_Done_Patch
        {
            public static void Postfix(SGCharacterCreationCareerBackgroundSelectionPanel __instance)
            {
                PilotInjuryManager.PreloadIcons();
                sim = UnityGameInstance.BattleTechGame.Simulation;
//                var curPilots = new List<string>();

                if (sim == null) return;
                if (!sim.CompanyStats.ContainsStatistic("RosterCapacityRemaining"))
                {
                    sim.CompanyStats.AddStatistic("RosterCapacityRemaining", sim.GetMaxMechWarriors() - sim.PilotRoster.Count);
                }
                else
                {
                    sim.CompanyStats.ModifyStat("SetRosterCapacityRemaining", 0, "RosterCapacityRemaining", StatCollection.StatOperation.Set,
                        sim.GetMaxMechWarriors() - sim.PilotRoster.Count, -1, true);
                }
                    
                if (!sim.Commander.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    //sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{sim.GenerateSimGameUID()}");
                    sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{Guid.NewGuid()}");
                }

                var pKey = sim.Commander.FetchGUID();
//                curPilots.Add(pKey);
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog?.Info?.Write($"Added Commander to pilotInjuriesMap with iGUID {pKey}");
                }


                foreach (Pilot p in sim.PilotRoster)
                {

                    if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                    {
                        // p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{sim.GenerateSimGameUID()}");
                        p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{Guid.NewGuid()}");
                    }

                    pKey = p.FetchGUID();
//                    curPilots.Add(pKey);
                    if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                        ModInit.modLog?.Info?.Write($"{p.Callsign} missing, added to pilotInjuriesMap with iGUID {pKey}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "Dehydrate",
            new Type[] {typeof(SimGameSave), typeof(SerializableReferenceContainer)})]
        public static class SGS_Dehydrate_Patch
        {
            public static void Prefix(SimGameState __instance)
            {
                PilotInjuryManager.PreloadIcons();
                sim = __instance;

                if (!sim.CompanyStats.ContainsStatistic("RosterCapacityRemaining"))
                {
                    sim.CompanyStats.AddStatistic("RosterCapacityRemaining", sim.GetMaxMechWarriors() - sim.PilotRoster.Count);
                }
                else
                {
                    sim.CompanyStats.ModifyStat("SetRosterCapacityRemaining", 0, "RosterCapacityRemaining", StatCollection.StatOperation.Set,
                        sim.GetMaxMechWarriors() - sim.PilotRoster.Count, -1, true);
                }

                var curPilots = new List<string>();

                if (!sim.Commander.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    //sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{sim.GenerateSimGameUID()}");
                    sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{Guid.NewGuid()}");
                }

                var pKey = sim.Commander.FetchGUID();
                curPilots.Add(pKey);
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog?.Info?.Write($"Added Commander to pilotInjuriesMap with iGUID {pKey}");
                }


                foreach (Pilot p in sim.PilotRoster)
                {

                    if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                    {
                        // p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{sim.GenerateSimGameUID()}");
                        p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{Guid.NewGuid()}");
                    }

                    pKey = p.FetchGUID();
                    curPilots.Add(pKey);
                    if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                        ModInit.modLog?.Info?.Write($"{p.Callsign} missing, added to pilotInjuriesMap with iGUID {pKey}");
                    }
                }

                if (false)
                {
                    var rm = PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Keys.Where(x => !curPilots.Contains(x));
                    foreach (var key in new List<string>(rm))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Remove(key);
                        ModInit.modLog?.Info?.Write(
                            $"Pilot with pilotID {key} not in roster, removing from pilotInjuriesMap");
                    }
                }

                var rm2 = new List<string>(
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Keys.Where(x => x.EndsWith(aiPilotFlag)));
                foreach (var key in rm2)
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Remove(key);
                    ModInit.modLog?.Info?.Write($"Pilot with pilotID {key} was AI Pilot, removing from pilotInjuriesMap");
                }

                PilotInjuryHolder.HolderInstance.SerializeInjuryState();
                PilotInjuryHolder.HolderInstance.combatInjuriesMap = new Dictionary<string, List<string>>();
            }
        }

        [HarmonyPatch(typeof(SimGameState), "Rehydrate", new Type[] {typeof(GameInstanceSave)})]
        public static class SGS_Rehydrate_Patch
        {
            public static void Prefix(SimGameState __instance)
            {
                sim?.CompanyTags.Clear(); // this will either help or hurt.  or it wont do a damn thing.
            }

            public static void Postfix(SimGameState __instance)
            {
                sim = __instance;
                PilotInjuryManager.PreloadIcons();

                if (!sim.CompanyStats.ContainsStatistic("RosterCapacityRemaining"))
                {
                    sim.CompanyStats.AddStatistic("RosterCapacityRemaining", sim.GetMaxMechWarriors() - sim.PilotRoster.Count);
                }
                else
                {
                    sim.CompanyStats.ModifyStat("SetRosterCapacityRemaining", 0, "RosterCapacityRemaining", StatCollection.StatOperation.Set,
                        sim.GetMaxMechWarriors() - sim.PilotRoster.Count, -1, true);
                }

                var curPilots = new List<string>();
                PilotInjuryHolder.HolderInstance.DeserializeInjuryState();
                PilotInjuryHolder.HolderInstance.combatInjuriesMap = new Dictionary<string, List<string>>();
                ModInit.modLog?.Info?.Write($"Successfully deserialized or determined deserializing unnecessary.");

                if (!sim.Commander.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    //sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{sim.GenerateSimGameUID()}");
                    sim.Commander.pilotDef.PilotTags.Add($"{iGUID}{sim.Commander.Description.Id}{Guid.NewGuid()}");
                }
                sim.Commander.pilotDef.PilotTags.Remove(GlobalVars.DebilitatedPrefix); // nuke old DEBILITATED only tag
                var pKey = sim.Commander.FetchGUID();

                ModInit.modLog?.Info?.Write($"Fetched Commander iGUID {pKey}");
                curPilots.Add(pKey);
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog?.Info?.Write($"Added Commander to pilotInjuriesMap with iGUID {pKey}");
                }

                foreach (var id in new List<string>(PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]))
                {
                    if (PilotInjuryManager.ManagerInstance.InjuryEffectsList.All(x => x.injuryID != id) &&
                        PilotInjuryManager.ManagerInstance.InternalDmgInjuries.All(x => x.injuryID != id))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Remove(id);
                        ModInit.modLog?.Info?.Write($"Removed deprecated injury from Commander with id {id}");
                    }
                }

                if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count >
                    sim.Commander.StatCollection.GetValue<int>("Injuries"))
                {
                    if (sim.Commander.StatCollection.GetValue<int>("Injuries") < 1)
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey] = new List<string>();
                        ModInit.modLog?.Info?.Write($"Commander had no vanilla injuries, clearing TBAS injuries.");
                    }
                    else
                    {
                        var dmg = PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count -
                                  sim.Commander.StatCollection.GetValue<int>("Injuries");
                        for (int i = dmg - 1; i >= 0; i--)
                        {
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].RemoveAt(i);
                            ModInit.modLog?.Info?.Write(
                                $"Commander has {dmg} more TBAS injuries than vanilla injuries, removing TBAS injury at {i}.");
                        }
                    }

                }

                foreach (Pilot p in sim.PilotRoster)
                {
                    p.pilotDef.PilotTags.Remove(GlobalVars.DebilitatedPrefix); // nuke old DEBILITATED only tag
                    if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                    {
                        //p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{sim.GenerateSimGameUID()}");
                        p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{Guid.NewGuid()}");
                        ModInit.modLog?.Info?.Write($"Added {p.Callsign} iGUID tag");
                    }

                    pKey = p.FetchGUID();
                    ModInit.modLog?.Info?.Write($"Fetched {p.Callsign} iGUID {pKey}");
                    curPilots.Add(pKey);
                    if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                        ModInit.modLog?.Info?.Write(
                            $"{p.Callsign} missing, added to pilotInjuriesMap with iGUID {pKey}");
                    }

                    foreach (var id in new List<string>(PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]))
                    {
                        if (PilotInjuryManager.ManagerInstance.InjuryEffectsList.All(x => x.injuryID != id) &&
                            PilotInjuryManager.ManagerInstance.InternalDmgInjuries.All(x => x.injuryID != id))
                        {
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Remove(id);
                            ModInit.modLog?.Info?.Write($"Removed deprecated injury from {p.Callsign} with id {id}");
                        }
                    }
                    
                    if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count >
                        p.StatCollection.GetValue<int>("Injuries"))
                    {
                        if (p.StatCollection.GetValue<int>("Injuries") < 1)
                        {
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey] = new List<string>();
                            ModInit.modLog?.Info?.Write(
                                $"Pilot {p.Callsign} had no vanilla injuries, clearing TBAS injuries.");
                        }
                        else
                        {
                            var dmg = PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count -
                                      p.StatCollection.GetValue<int>("Injuries");
                            for (int i = dmg - 1; i >= 0; i--)
                            {
                                PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].RemoveAt(i);
                                ModInit.modLog?.Info?.Write(
                                    $"Pilot {p.Callsign} has {dmg} more TBAS injuries than vanilla injuries, removing TBAS injury at {i}.");
                            }
                        }
                    }
                }

                var rm2 = new List<string>(
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Keys.Where(x => x.EndsWith(aiPilotFlag)));
                foreach (var key in rm2)
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Remove(key);
                    ModInit.modLog?.Info?.Write(
                        $"Pilot with pilotID {key} was AI Pilot, removing from pilotInjuriesMap");
                }

                if (false)
                {
                    var rm = PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Keys.Where(x => !curPilots.Contains(x));
                    foreach (var key in new List<string>(rm))
                    {
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Remove(key);
                        ModInit.modLog?.Info?.Write(
                            $"Pilot with pilotID {key} not in roster, removing from pilotInjuriesMap");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "ApplySimGameEventResult",
            new Type[] {typeof(SimGameEventResult), typeof(List<object>), typeof(SimGameEventTracker)})]
        public static class SGS_ApplySimGameEventResult
        {
            public static void Postfix(SimGameState __instance, SimGameEventResult result, List<object> objects,
                SimGameEventTracker tracker)
            {
                for (var i = 0; i < objects.Count; i++)
                {
                    var GainedInjury = result.HasActionType(SimGameResultAction.ActionType.MechWarrior_AddInjury) || result.HasActionType(SimGameResultAction.ActionType.MechWarrior_AddInjuries);
                    var LostInjury = result.HasActionType(SimGameResultAction.ActionType.MechWarrior_SubtractInjury) || result.HasActionType(SimGameResultAction.ActionType.MechWarrior_SubtractInjuries);
                    if (!GainedInjury && !LostInjury && result.Stats == null) continue;
                    if (result.Stats != null)
                    {
                        foreach (var stat in result.Stats)
                        {
                            if (stat.typeString != "System.Int32") continue;
                            switch (stat.name)
                            {
                                case "Injuries" when stat.ToInt() > 0:
                                    GainedInjury = true;
                                    break;
                                case "Injuries" when stat.ToInt() < 0:
                                    LostInjury = true;
                                    break;
                            }
                        }
                    }

                    var obj = objects[i];
                    if (result.Scope == EventScope.MechWarrior || result.Scope == EventScope.SecondaryMechWarrior ||
                        result.Scope == EventScope.TertiaryMechWarrior)
                    {
                        var p = (Pilot) obj;
                        var pKey = p.FetchGUID();

                        if (LostInjury)
                        {
                            if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count >
                                p.StatCollection.GetValue<int>("Injuries"))
                            {
                                var dmg = PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count - p.StatCollection.GetValue<int>("Injuries");
                                ModInit.modLog?.Info?.Write($"{p.Callsign} has {dmg} extra injuries. Removing.");
                                for (int j = dmg - 1; j >= 0; j--)
                                {
                                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].RemoveAt(j);
                                }
                            }
                            continue;
                        }

                        if (GainedInjury)
                        {
                            if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count <
                                p.StatCollection.GetValue<int>("Injuries"))
                            {
                                var dmg = p.StatCollection.GetValue<int>("Injuries") -
                                          PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count;
                                ModInit.modLog?.Info?.Write($"{p.Callsign} is missing {dmg} injuries. Rerolling.");
                                PilotInjuryManager.ManagerInstance.rollInjurySG(p, dmg, DamageType.Unknown);
                                if (ModInit.modSettings.debilSeverityThreshold >
                                    0) //now trying to add up "severity" threshold for crippled injury
                                {

                                    var injuryList = new List<Injury>();
                                    foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                                    {
                                        injuryList.AddRange(
                                            PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(
                                                x => x.injuryID == id));
                                    }

                                    var groupedLocs = injuryList.GroupBy(x => x.injuryLoc);

                                    foreach (var injuryLoc in groupedLocs)
                                    {
                                        var t = 0;
                                        foreach (var injury in injuryLoc)
                                        {
                                            t += injury.severity;
                                        }

                                        if (t > ModInit.modSettings.debilSeverityThreshold)
                                        {
                                            p.pilotDef.PilotTags.Add($"{DebilitatedPrefix}_{injuryLoc.Key}");
                                            if (ModInit.modSettings.enableLethalTorsoHead &&
                                                (injuryLoc.Key == InjuryLoc.Head ||
                                                 injuryLoc.Key == InjuryLoc.Torso))
                                            {
                                                p.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0,
                                                    "LethalInjury",
                                                    StatCollection.StatOperation.Set, true);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (result.Scope == EventScope.Commander)
                    {
                        var commander = (Pilot) obj;
                        if (string.IsNullOrEmpty(commander.FetchGUID())) return;
                        var pKey = commander.FetchGUID();

                        if (LostInjury)
                        {
                            if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count >
                                commander.StatCollection.GetValue<int>("Injuries"))
                            {
                                var dmg = PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count - commander.StatCollection.GetValue<int>("Injuries");
                                ModInit.modLog?.Info?.Write($"{commander.Callsign} has {dmg} extra injuries. Removing.");
                                for (int j = dmg - 1; j >= 0; j--)
                                {
                                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].RemoveAt(j);
                                }
                            }

                            continue;
                        }

                        if (GainedInjury)
                        {

                            if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count <
                                commander.StatCollection.GetValue<int>("Injuries"))
                            {
                                var dmg = commander.StatCollection.GetValue<int>("Injuries") -
                                          PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count;
                                ModInit.modLog?.Info?.Write($"Commander is missing {dmg} injuries. Rerolling.");
                                PilotInjuryManager.ManagerInstance.rollInjurySG(commander, dmg,
                                    DamageType.Unknown);
                                if (ModInit.modSettings.debilSeverityThreshold > 0
                                ) //now trying to add up "severity" threshold for crippled injury
                                {

                                    var injuryList = new List<Injury>();
                                    foreach (var id in PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey])
                                    {
                                        injuryList.AddRange(
                                            PilotInjuryManager.ManagerInstance.InjuryEffectsList.Where(
                                                x => x.injuryID == id));
                                    }

                                    var groupedLocs = injuryList.GroupBy(x => x.injuryLoc);

                                    foreach (var injuryLoc in groupedLocs)
                                    {
                                        var t = 0;
                                        foreach (var injury in injuryLoc)
                                        {
                                            t += injury.severity;
                                        }

                                        if (t >= ModInit.modSettings.debilSeverityThreshold)
                                        {
                                            commander.pilotDef.PilotTags.Add($"{DebilitatedPrefix}_{injuryLoc.Key}");
                                            if (ModInit.modSettings.enableLethalTorsoHead &&
                                                (injuryLoc.Key == InjuryLoc.Head ||
                                                 injuryLoc.Key == InjuryLoc.Torso))
                                            {
                                                commander.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0,
                                                    "LethalInjury",
                                                    StatCollection.StatOperation.Set, true);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "DismissPilot", new Type[] {typeof(Pilot)})]
        public static class SimGameState_DismissPilot
        {
            public static void Postfix(SimGameState __instance, Pilot p)
            {
                if (!__instance.CompanyStats.ContainsStatistic("RosterCapacityRemaining"))
                {
                    __instance.CompanyStats.AddStatistic("RosterCapacityRemaining", __instance.GetMaxMechWarriors() - __instance.PilotRoster.Count);
                }
                else
                {
                    __instance.CompanyStats.ModifyStat("SetRosterCapacityRemaining", 0, "RosterCapacityRemaining", StatCollection.StatOperation.Set,
                        __instance.GetMaxMechWarriors() - __instance.PilotRoster.Count, -1, true);
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "KillPilot", new Type[] {typeof(Pilot), typeof(bool), typeof(string), typeof(string)})]
        public static class SimGameState_KillPilot
        {
            public static void Postfix(SimGameState __instance, Pilot p, bool fromEvent = false,
                string StarSystemID = null, string causeOfDeathOverride = null)
            {
                if (!__instance.CompanyStats.ContainsStatistic("RosterCapacityRemaining"))
                {
                    __instance.CompanyStats.AddStatistic("RosterCapacityRemaining", __instance.GetMaxMechWarriors() - __instance.PilotRoster.Count);
                }
                else
                {
                    __instance.CompanyStats.ModifyStat("SetRosterCapacityRemaining", 0, "RosterCapacityRemaining", StatCollection.StatOperation.Set,
                        __instance.GetMaxMechWarriors() - __instance.PilotRoster.Count, -1, true);
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "AddPilotToRoster",
            new Type[] {typeof(PilotDef), typeof(bool), typeof(bool)})]
        public static class SGS_AddPilotToRoster_Patch
        {
            public static void Postfix(SimGameState __instance, PilotDef def, bool updatePilotDiscardPile = false,
                bool initialHiringDontSpawnMessage = false)
            {
                if (!__instance.CompanyStats.ContainsStatistic("RosterCapacityRemaining"))
                {
                    __instance.CompanyStats.AddStatistic("RosterCapacityRemaining", __instance.GetMaxMechWarriors() - __instance.PilotRoster.Count);
                }
                else
                {
                    __instance.CompanyStats.ModifyStat("SetRosterCapacityRemaining", 0, "RosterCapacityRemaining", StatCollection.StatOperation.Set,
                        __instance.GetMaxMechWarriors() - __instance.PilotRoster.Count, -1, true);
                }

                var p = __instance.PilotRoster.FirstOrDefault(x => x.pilotDef.Description.Id == def.Description.Id);
                if (p != null && !p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    // p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{__instance.GenerateSimGameUID()}");
                    p.pilotDef.PilotTags.Add($"{iGUID}{p.Description.Id}{Guid.NewGuid()}");
                    ModInit.modLog?.Info?.Write($"Added {p.Callsign} iGUID tag");
                }

                var pKey = p.FetchGUID();
                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    if (p != null) ModInit.modLog?.Info?.Write($"{p.Callsign} missing, added to pilotInjuriesMap");
                }
            }
        }

        [HarmonyPatch(typeof(AbstractActor), "InitEffectStats")]
        public static class AbstractActor_InitEffectStats_Patch
        {
            public static void Postfix(AbstractActor __instance)
            {
                var p = __instance.GetPilot();
                p.StatCollection.AddStatistic<bool>("NeedsFeedbackInjury", false);
                p.StatCollection.AddStatistic<bool>("BledOut", false);
                p.StatCollection.AddStatistic<int>("internalDmgInjuryCount", 0);
                p.StatCollection.AddStatistic<int>(MissionKilledStat, 0);
                p.StatCollection.AddStatistic<int>("MissionKilledThreshold", ModInit.modSettings.missionKillSeverityThreshold);
                p.StatCollection.AddStatistic<float>("BleedingRate", 0f);
                p.StatCollection.AddStatistic<float>("BleedingRateMulti", 1f);
                //p.StatCollection.AddStatistic<List<string>>("LastInjuryId", new List<string>());
                __instance.StatCollection.AddStatistic<bool>(ModInit.modSettings.internalDmgStatName, false);
                __instance.StatCollection.AddStatistic<bool>(ModInit.modSettings.isTorsoMountStatName, false);
                __instance.StatCollection.AddStatistic<bool>(ModInit.modSettings.OverheatInjuryStat, false);
                __instance.StatCollection.AddStatistic<bool>(ModInit.modSettings.DisableBleedingStat, false);
                __instance.StatCollection.AddStatistic<bool>(ModInit.modSettings.NullifiesInjuryEffectsStat, false);
            }
        }

        [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
        public static class CombatGameState_OnCombatGameDestroyed
        {
            // Token: 0x0600003C RID: 60 RVA: 0x00004E84 File Offset: 0x00003084
            public static void Postfix(CombatGameState __instance)
            {
                ModState.ResetStateAfterContract();
            }
        }

        //adding AI units (i.e., not commander and not your pilotroster) to pilotInjuriesMap
        [HarmonyPatch(typeof(Team), "AddUnit", new Type[] {typeof(AbstractActor)})]
        public static class Team_AddUnit
        {
            public static void Postfix(Team __instance, AbstractActor unit)
            {
                if (sim == null) return;
                //still need to make AI GUID end with aiPilotFlag
                var p = unit.GetPilot();
                p.ForceRefreshDef();
                if (!p.pilotDef.PilotTags.Any(x => x.StartsWith(iGUID)))
                {
                    var newPKey = $"{iGUID}{p.Description.Id}{Guid.NewGuid()}{aiPilotFlag}";
                    p.pilotDef.PilotTags.Add(newPKey); //changed to sys NewGuid instead of simguid for skirmish compatibility
                    
                    ModInit.modLog?.Info?.Write($"Added {p.Callsign} iGUID tag: {newPKey}");
                }

                var pKey = p.FetchGUID();
                ModInit.modLog?.Info?.Write($"Fetched {p.Callsign} iGUID");

                if (!PilotInjuryHolder.HolderInstance.combatInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.combatInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog?.Info?.Write($"{p.Callsign}, {pKey}, piloting {unit.DisplayName}, {unit.VariantName} missing, added to combatInjuriesMap");
                }

                if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                {
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    ModInit.modLog?.Info?.Write($"{p.Callsign} missing, added to pilotInjuriesMap");
                }

                PilotInjuryManager.ManagerInstance.GatherAndApplyInjuries(unit);
                ModInit.modLog?.Info?.Write($"Initializing injury effects for {p.Description?.Callsign}");

                if (PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count >
                    p.StatCollection.GetValue<int>("Injuries"))
                {
                    ModInit.modLog?.Info?.Write(
                        $"{p.Callsign}'s Injury stat < existing injuries. Adding to Injury stat.");
                    p.StatCollection.ModifyStat<int>("TBAS_Injuries", -1,
                        "Injuries",
                        StatCollection.StatOperation.Set, PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count);
                }

                ModInit.modLog?.Trace?.Write($"{p.Callsign} {p.FetchGUID()} - Is actor dead?: {p.ParentActor.IsDead} Because is incapacitated: {p.IsIncapacitated} from {p.Injuries} injuries / {p.Health} health and lethal injuries {p.LethalInjuries}.");

                p.StatCollection.Set("LethalInjury", false);
                p.StatCollection.Set("HasEjected", false);

                p.StatCollection.AddStatistic<float>("BloodBank", p.CalcBloodBank());
                p.StatCollection.AddStatistic<float>("BloodCapacity", p.CalcBloodBank());
                ModInit.modLog?.Info?.Write($"{p.Callsign} calculated BloodBank and BloodCapacity: {p.CalcBloodBank()}");

                //handle overcrowded effects
                if (unit.team.IsLocalPlayer)
                {
                    var capacity = sim.CompanyStats.GetValue<int>("RosterCapacityRemaining");
                    if (capacity > 0) return;
                    
                    var potentialEffects = new List<OvercrowdedEffect>();
                    foreach (var overCrowdedEffect in PilotInjuryManager.ManagerInstance.OvercrowdedEffectsList)
                    {
                        if (overCrowdedEffect.Threshold <= Math.Abs(capacity)) potentialEffects.Add(overCrowdedEffect);
                    }
                    if (potentialEffects.Count > 0)
                    {
                        var chosenEffect = potentialEffects.GetRandomElement();
                        foreach (EffectData effectData in chosenEffect.effects)
                        {
                            ModInit.modLog?.Info?.Write($"processing overcrowding effect {effectData.Description.Name} for {p.Callsign}_{pKey}");

                            if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                            {
                                string id = ($"OvercrowdingEffect_{p.Callsign}_{effectData.Description.Id}");

                                ModInit.modLog?.Info?.Write($"Applying {id}");
                                unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit, default(WeaponHitInfo), 1);
                            }
                        }
                    }
                }
            }
        }

        //resetting combatinjuriesMap on restart
        [HarmonyPatch(typeof(LoadTransitioning), "BeginCombatRestart", new Type[] {typeof(Contract)})]
        public static class LoadTransitioning_BeginCombatRestart_Patch
        {
            public static void Prefix(Contract __instance)
            {
                PilotInjuryHolder.HolderInstance.bloodStatForSimGame = new Dictionary<string, float>();
                PilotInjuryHolder.HolderInstance.combatInjuriesMap = new Dictionary<string, List<string>>();
                ModInit.modLog?.Info?.Write(
                    $"Resetting combatInjuriesMap due to RestartMission button. Somebody must like CTD's.");
            }
        }


        [HarmonyPatch(typeof(Contract), "CompleteContract", new Type[] {typeof(MissionResult), typeof(bool)})]
        public static class Contract_CompleteContract_Patch
        {
            [HarmonyAfter(new string[] {"us.tbone.TrainingMissions"})]
            public static void Prefix(Contract __instance, MissionResult result, bool isGoodFaithEffort)
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                if (sim == null) return;
                var playerPilots = new List<Pilot>();
                playerPilots.AddRange(sim.PilotRoster);
                playerPilots.Add(sim.Commander);
                var actors = UnityGameInstance.BattleTechGame.Combat.AllActors.Where(x => x.team.IsLocalPlayer);
                foreach (var actor in actors)
                {
                    var p = actor.GetPilot();
                    var pKey = p.FetchGUID();
                    if (string.IsNullOrEmpty(pKey)) continue;
                    if (playerPilots.All(x => x.pilotDef.Description.Id != p.pilotDef.Description.Id)) continue;
                    //                   if (p.pilotDef.PilotTags.Any(x => x.EndsWith(aiPilotFlag)))
                    //                   {
                    //                        p.pilotDef.PilotTags.Remove(DEBILITATEDTAG);
                    //                        ModInit.modLog?.Info?.Write($"Removing CrippledTag from AI pilot {p.Callsign} if present");
                    //                        var rmt = p.pilotDef.PilotTags.Where(x => x.EndsWith(aiPilotFlag));
                    //                        p.pilotDef.PilotTags.RemoveRange(rmt);
                    //                        ModInit.modLog?.Info?.Write($"Removing AI GUID Tag from AI pilot {p.Callsign} if present");
                    //                        continue;
                    //                    }
                    //now only adding to pilotInjuryMap at contract resolution instead of on the fly.
                    if (!PilotInjuryHolder.HolderInstance.pilotInjuriesMap.ContainsKey(pKey))
                    {
                        ModInit.modLog?.Info?.Write($"PilotInjuryMap doesn't have {p.Callsign} {p.Description.Id}?  That shouldnt be possible. Adding them.");
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap.Add(pKey, new List<string>());
                    }
                    if (!PilotInjuryHolder.HolderInstance.combatInjuriesMap.ContainsKey(pKey))
                    {
                        ModInit.modLog?.Info?.Write($"combatInjuriesMap doesn't have {p.Callsign} {p.Description.Id}? That shouldnt be possible. skipping anyway.");
                        continue;
                    }
                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey]
                        .AddRange(PilotInjuryHolder.HolderInstance.combatInjuriesMap[pKey]);
                    ModInit.modLog?.Info?.Write($"Adding {p.Callsign}'s combatInjuryMap to their pilotInjuryMap");

                    var replacementInjuries = new List<string>();

                    for (var index = PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count - 1; index >= 0;
 index--)
                    {
                        var injury = PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey][index];
                        var injuryDef =
                            PilotInjuryManager.ManagerInstance.InjuryEffectsList.FirstOrDefault(x =>
                                x.injuryID == injury);
                        if (injuryDef == null) continue;
                        if (!string.IsNullOrEmpty(injuryDef.injuryID_Post))
                        {
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].RemoveAt(index);
                            ModInit.modLog?.Info?.Write($"Removed {injuryDef.injuryName} with bleeding effect from {p.Callsign}");
                            replacementInjuries.Add(injuryDef.injuryID_Post);
                            ModInit.modLog?.Info?.Write($"Added {injuryDef.injuryID_Post} to {p.Callsign} for post-combat injury");
                        }
                        else
                        {
                            ModInit.modLog?.Info?.Write($"Injury {injury} not a bleeding injury, not doing post-mission replacement.");
                        }
                    }

                    PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].AddRange(replacementInjuries);

                    if (p.StatCollection.GetValue<int>("Injuries") <
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count)
                    {
                        var diff = PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count -
                                   p.StatCollection.GetValue<int>("Injuries");
                        ModInit.modLog?.Info?.Write(
                            $"Post-Mission Injuries ({p.StatCollection.GetValue<int>("Injuries")}) less than InjuryHolder count ({PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Count})");
                        PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].Reverse();
                        for (int i = diff - 1; i >= 0; i--)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"Removing {PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey][i]} from {p.Callsign}");
                            PilotInjuryHolder.HolderInstance.pilotInjuriesMap[pKey].RemoveAt(i);
                        }
                    }
                }

                PilotInjuryHolder.HolderInstance.combatInjuriesMap = new Dictionary<string, List<string>>();
                Utility.CachedTagSets = new Dictionary<string, TagSet>();
            }
        }


        [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
        public static class ResolveCompleteContract_Patch
        {
            public static bool Prepare() => ModInit.modSettings.UseSimBleedingEffects;

            public static void Postfix(SimGameState __instance)
            {
                List<Pilot> list = new List<Pilot>(__instance.PilotRoster) {__instance.Commander};
                foreach (Pilot pilot in list)
                {
                    var pKey = pilot.FetchGUID();
                    if (!PilotInjuryHolder.HolderInstance.bloodStatForSimGame.ContainsKey(pKey)) continue;

                    pilot.ApplyClosestSimGameResult();

                }
                ModInit.modLog?.Info?.Write(
                    $"Clearing bloodStatForSimGame dict!");
                PilotInjuryHolder.HolderInstance.bloodStatForSimGame = new Dictionary<string, float>();
            }
        }

        [HarmonyPatch(typeof(LanceLoadoutSlot), "OnAddItem")]
        public static class LanceLoadoutSlot_OnAddItem
        {
            public static bool Prepare() => ModInit.modSettings.pilotingReqs.Count > 0;

            [HarmonyBefore(new string[] {"io.mission.customunits"})]
            [HarmonyPriority(Priority.First)]
            public static void Postfix(LanceLoadoutSlot __instance, IMechLabDraggableItem item, bool validate,
                ref bool __result)
            {
                if (__instance.LC == null) return;
                if (__instance.SelectedPilot != null && item.ItemType == MechLabDraggableItemType.Mech)
                {
                    if (__instance.SelectedPilot.Pilot.TagReqsAreMet(item, __instance)) 
                    {
                        __result = true;
                        __instance.LC.ValidateLance();
                        return;
                    }
                    __instance.LC.ReturnItem(item);
                    __instance.selectedMech = null;
                    __result = false;
                    __instance.LC.ValidateLance();
                    return;
                }
                else if (__instance.SelectedMech != null && item.ItemType == MechLabDraggableItemType.Pilot)
                {
                    if (!(item is SGBarracksRosterSlot slot))
                    {
                        __result = true;
                        __instance.LC.ValidateLance();
                        return;
                    }

                    if (slot.Pilot.TagReqsAreMet(item, __instance))
                    {
                        __result = true;
                        __instance.LC.ValidateLance();
                        return;
                    }
                    __instance.LC.ReturnItem(item);
                    __instance.selectedPilot = null;
                    __result = false;
                    __instance.LC.ValidateLance();
                    return;
                }
                __result = true;
                return;
            }
        }

        [HarmonyPatch(typeof(SG_HiringHall_Screen), "CheckMaxBerthsWarning")]
        public static class SG_HiringHall_Screen_CheckMaxBerthsWarning
        {
            public static bool Prepare() => ModInit.modSettings.CanIntentionallyHotBunk;

            public static void Prefix(ref bool __runOriginal, SG_HiringHall_Screen __instance, ref bool __result, bool JustTest = false)
            {
                if (!__runOriginal) return;
                if (__instance.simState.PilotRoster.Count >= __instance.simState.GetMaxMechWarriors())
                {
                    if (!JustTest)
                    {
                        __instance.SetWarningText(Strings.T("Cannot Fit anymore MechWarriors in your barracks."), Array.Empty<object>());
                        __instance.WarningAreaObject.SetActive(true);
                    }
                }
                __result = false;
                __runOriginal = false;
                return;
            }
        }
    }
}