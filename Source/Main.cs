using HarmonyLib;
using HugsLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
// ReSharper disable UnusedMember.Global

namespace NoJobAuthors
{
    public class Main : ModBase
    {
        public override string ModIdentifier => "NoJobAuthors";
    }


    [HarmonyPatch(typeof(WorkGiver_DoBill), "ClosestUnfinishedThingForBill")]
    public static class WorkGiver_DoBill_ClosestUnfinishedThingForBill_Patch
    {
        [HarmonyPrefix]
        public static bool ClosestUnfinishedThingForBill(ref UnfinishedThing __result, Pawn pawn, Bill_ProductionWithUft bill)
        {
            bool Validator(Thing t) => !t.IsForbidden(pawn) &&
                                       ((UnfinishedThing)t).Recipe == bill.recipe &&
                                       ((UnfinishedThing)t).ingredients.TrueForAll(x => bill.IsFixedOrAllowedIngredient(x.def)) &&
                                       pawn.CanReserve(t);

            var thingReq = ThingRequest.ForDef(bill.recipe.unfinishedThingDef);
            var traverseParams = TraverseParms.For(pawn, pawn.NormalMaxDanger());

            __result = (UnfinishedThing)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, thingReq, PathEndMode.InteractionCell, traverseParams, validator: Validator);
        #if DEBUG
            Log.Message("This is closest unfinished thing for bill");
        #endif
            return false;
        }
    }

    [HarmonyPatch(typeof(WorkGiver_DoBill), "StartOrResumeBillJob")]
    public static class WorkGiver_DoBill_StartOrResumeBillJob_Patch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> StartOrResumeBillJob(IEnumerable<CodeInstruction> instructions)
        {
            var arr = instructions.ToArray();
        #if DEBUG
            Log.Message("Start or resume bill patch op codes thing");
        #endif
            for (var index = 0; index < arr.Length; index++)
            {
                if (arr[index + 0].opcode == OpCodes.Ldloc_S &&
                    arr[index + 1].opcode == OpCodes.Callvirt &&
                    arr[index + 2].opcode == OpCodes.Ldarg_1 &&
                    arr[index + 3].opcode == OpCodes.Bne_Un)

                {
                    yield return new CodeInstruction(OpCodes.Nop);
                    yield return new CodeInstruction(OpCodes.Nop);
                    yield return new CodeInstruction(OpCodes.Nop);
                    yield return new CodeInstruction(OpCodes.Nop);
                    index += 3;
                }
                else {
                #if DEBUG
                    Log.Message("We made it to start or resume bill job else statement");
                #endif
                    yield return arr[index];

                }
            }
        }
    }

    [HarmonyPatch(typeof(WorkGiver_DoBill), "FinishUftJob")]
    public static class WorkGiver_DoBill_FinishUftJob_Patch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FinishUftJob(IEnumerable<CodeInstruction> instructions)
        {
        #if DEBUG
            Log.Message("FinishUftJob Start Patch");
        #endif
            var arr = instructions.ToArray();
            for (var index = 0; index < arr.Length; index++)
            {
                if (arr[index + 0].opcode == OpCodes.Ldarg_1 &&
                    arr[index + 1].opcode == OpCodes.Callvirt &&
                    arr[index + 2].opcode == OpCodes.Ldarg_0 &&
                    arr[index + 3].opcode == OpCodes.Beq_S)
                {
                    yield return new CodeInstruction(OpCodes.Nop);
                    yield return new CodeInstruction(OpCodes.Nop);
                    yield return new CodeInstruction(OpCodes.Nop);
                    yield return new CodeInstruction(OpCodes.Br, arr[index + 3].operand);
                    index += 3;
                }
                else {
                #if DEBUG
                    Log.Message("FinishUftJob end else Patch");
                #endif
                    yield return arr[index];

                }
            }
        }
    }

    [HarmonyPatch(typeof(UnfinishedThing), "get_Creator")]
    public static class UnfinishedThing_GetCreator_Patch
    {
        [HarmonyPrefix]
        public static bool Creator(ref Pawn __result)
        {
            __result = null;
        #if  DEBUG
            Log.Message("Set Creator to null");
        #endif
            return false;
        }
    }

    [HarmonyPatch(typeof(UnfinishedThing), "set_Creator")]
    public static class UnfinishedThing_SetCreator_Patch
    {
        private static readonly AccessTools.FieldRef<UnfinishedThing, string> _creatorName = AccessTools.FieldRefAccess<UnfinishedThing, string>("creatorName");

        [HarmonyPostfix]
        public static void Creator(UnfinishedThing __instance)
        {
            _creatorName(__instance) = "Everyone";
        #if DEBUG
            Log.Message("I just set the creator to everyone");
        #endif
        }
    }
}