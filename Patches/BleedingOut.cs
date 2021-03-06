﻿using System;
using BattleTech;
using Harmony;
using System.Linq;
using BattleTech.UI;
using TisButAScratch.Framework;
using UnityEngine;
using Text = Localize.Text;

namespace TisButAScratch.Patches
{
    public class BleedingOut
    {

        [HarmonyPatch(typeof(AbstractActor), "OnActivationEnd",
            new Type[] {typeof(string), typeof(int)})]
        public static class AbstractActor_OnActivationEnd
        {
            public static void Prefix(AbstractActor __instance)
            {
                if (__instance == null) return;
                var p = __instance.GetPilot();
                var pKey = p.FetchGUID();
                ModInit.modLog.LogMessage(
                    $"Actor {p.Callsign} {pKey} ending turn.");
                var effects = __instance.Combat.EffectManager.GetAllEffectsTargeting(__instance);
                if (effects.Count == 0) return;
                var continuator = false;
                foreach (var effect in effects)
                {
                    if (effect.EffectData?.Description?.Id == null)
                    {
                        ModInit.modLog.LogMessage(
                            $"Effect {effect?.EffectData} had null description");
                        continue;
                    }

                    if (effect.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix))
                    {
                        continuator = true;
                        break;
                    }
                }
                if (!continuator) return;

                var baseRate = p.GetBleedingRate();
                var multi = p.GetBleedingRateMulti();
                var bleedRate = baseRate * multi;
                ModInit.modLog.LogMessage(
                    $"OnActivationEnd: {p.Callsign}_{pKey} bleeding out at rate of {bleedRate}/activation from base {baseRate} * multi {multi}!");
                var bloodBank = p.GetBloodBank();
                ModInit.modLog.LogMessage(
                    $"{p.Callsign}_{pKey}: Current bloodBank at {bloodBank}!");
                var newbloodBank = bloodBank - bleedRate;
                p.SetBloodBank(newbloodBank);
                ModInit.modLog.LogMessage(
                    $"{p.Callsign}_{pKey}: BloodBank set to {p.GetBloodBank()}");

                if (newbloodBank <= 0)
                {
                    if (__instance.WasEjected) return;
                    
                    p.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "BledOut",
                        StatCollection.StatOperation.Set, true);
                    ModInit.modLog.LogMessage(
                        $"{p.Callsign}_{pKey} has bled out!");

                    __instance.FlagForDeath("Bled Out", DeathMethod.PilotKilled, DamageType.Unknown, 1, 1, p.FetchGUID(), true);

                    if (ModInit.modSettings.BleedingOutLethal) p.StatCollection.ModifyStat<bool>("TBAS_Injuries", 0, "LethalInjury",
                        StatCollection.StatOperation.Set, true);
                    __instance.HandleDeath(p.FetchGUID()); // added handledeath for  bleeding out
                    return;
                }

                if (ModInit.modSettings.UseBleedingEffects && bleedRate > 0)
                {
                    p.ApplyClosestBleedingEffect();
                    //probably should handle/apply bleeding out effects here
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUD), "OnActorSelected",
            new Type[] {typeof(AbstractActor)})]
        public static class CombatHUD_OnActorSelected_Patch
        {
            public static void Postfix(CombatHUD __instance, AbstractActor actor)
            {
                var effects = __instance.Combat.EffectManager.GetAllEffectsTargeting(actor);
                var continuator = false;
                foreach (var effect in effects)
                {
                    if (effect.EffectData?.Description?.Id == null)
                    {
                        ModInit.modLog.LogMessage(
                            $"Effect {effect?.EffectData} had null description");
                        continue;
                    }

                    if (effect.EffectData.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix))
                    {
                        continuator = true;
                        break;
                    }
                }
                if (!continuator) return;
                var p = actor.GetPilot();
                var pKey = p.FetchGUID();
                var baseRate = p.GetBleedingRate();
                var multi = p.GetBleedingRateMulti();
                var bleedRate = baseRate * multi;
                ModInit.modLog.LogMessage(
                    $"OnActorSelected: {p.Callsign}_{pKey} bleeding out at rate of {bleedRate}/activation from base {baseRate} * multi {multi}!");
                var durationInfo = Mathf.CeilToInt(p.GetBloodBank() / (bleedRate) -1); 

                ModInit.modLog.LogMessage(
                    $"At OnActorSelected: Found bleeding effect(s) for {actor.GetPilot().Callsign}, processing time to bleedout for display: {durationInfo} activations remain");

                var eject = "";
                if (durationInfo <= 0)
                {
                    eject = "EJECT NOW OR DIE!";
                }

                var txt = new Text("<color=#FF0000>Pilot is bleeding out! {0} activations remaining! {1}</color=#FF0000>",
                    new object[]
                    {
                        durationInfo,
                        eject
                    });

                actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(actor, txt, FloatieMessage.MessageNature.PilotInjury, false)));

//                }
            }
        }

        private static CombatHUDStatusPanel theInstance;

        [HarmonyPatch(typeof(CombatHUDStatusPanel), "ShowEffectStatuses")]
        public static class CombatHUDStatusPanel_ShowEffectStatuses
        {
            public static void Prefix(CombatHUDStatusPanel __instance, AbstractActor actor,
                AbilityDef.SpecialRules specialRulesFilter, Vector3 worldPos)
            {
                if (__instance != null)
                    theInstance = __instance;
            }
        }

        [HarmonyPatch(typeof(CombatHUDStatusPanel), "ProcessDetailString",
            new Type[] {typeof(EffectData), typeof(int)})]
        public static class CombatHUDStatusPanel_ProcessDetailString
        {
            public static void Postfix(CombatHUDStatusPanel __instance, ref Text __result, EffectData effect,
                int numDuplicateEffects)
            {
                var em = UnityGameInstance.BattleTechGame.Combat.EffectManager; 
                
                //CombatHUD chud = (CombatHUD) Traverse.Create(__instance).("HUD").GetValue();
                // var em = chud.Combat.EffectManager;
                if (!(theInstance.DisplayedCombatant is AbstractActor actor)) return;
                if (effect?.Description?.Id == null)
                {
                    ModInit.modLog.LogMessage(
                        $"Effect {effect} had null description");
                    return;
                }

                if (!effect.Description.Id.EndsWith(ModInit.modSettings.BleedingOutSuffix)) return;
                // var effectsList = em.GetAllEffectsCreatedBy(__instance.DisplayedCombatant.GUID);
                var effectsList = em.GetAllEffectsTargeting(actor);

                if (effectsList.Count <= 0) return;

                var p = actor.GetPilot();
                var pKey = p.FetchGUID();
                var baseRate = p.GetBleedingRate();
                var multi = p.GetBleedingRateMulti();
                var bleedRate = baseRate * multi;
                ModInit.modLog.LogMessage(
                    $"ProcessDetailString: {p.Callsign}_{pKey} bleeding out at rate of {bleedRate}/activation from base {baseRate} * multi {multi}!");
                var durationInfo = Mathf.CeilToInt(p.GetBloodBank() / (bleedRate) - 1);

                ModInit.modLog.LogMessage(
                    $"At ProcessDetailString: Found bleeding effect(s) for {actor.GetPilot().Callsign}, processing time to bleedout for display: {durationInfo} activations remain");
                var tgtEffect = effectsList.FirstOrDefault(x => x.EffectData == effect);
                if (tgtEffect == null) return;
                var eject = "";
                if (durationInfo <= 0)
                {
                    eject = "EJECT NOW OR DIE!";
                }
                var txt = new Text("\n<color=#FF0000>Pilot is bleeding out! {0} activations remaining! {1}</color=#FF0000>", new object[]
                {
                    durationInfo,
                    eject
                });

                __result.AppendLine(txt);
            }
        }
    }
}