using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using VEF;
using VEF.AnimalBehaviours;
using VEF.Genes;
using Verse;

namespace LTS_DracnirGenes
{
    public class Verb_CastAbilityDash : Verb_CastAbilityJump
    {
        public override ThingDef JumpFlyerDef
        {
            get
            {
                return ThingDef.Named("LTS_DashPawnFlier");
            }
        }
    }

    //[StaticConstructorOnStartup]
    //public static class HarmonyPatches
    //{
    //    static HarmonyPatches()
    //    {
    //        Harmony harmony = new Harmony("rimworld.LTS.DracnirGenes");
    //        //Harmony.DEBUG = true;
    //        harmony.PatchAll();
    //        //harmony.PatchAll(Assembly.GetExecutingAssembly());
    //    }
    //}

    public class DracnirGene : Gene
    {
        public override void Tick()
        {
            base.Tick();
            if (ticksRemaining <= 0)
            {
                pawn.genes.RemoveGene(this);
            }
            else
                ticksRemaining--;
        }
        public int ticksRemaining = 300;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksRemaining, "ticksRemaining", 3600);
        }
    }

    public class CompProperties_AbilityGeneStealerBite : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityGeneStealerBite()
        {
            this.compClass = typeof(CompAbilityEffect_GeneStealerBite);
        }
        //public override IEnumerable<string> ExtraStatSummary()
        //{
        //    yield return "AbilityHemogenGain".Translate() + ": " + (this.hemogenGain * 100f).ToString("F0");
        //    yield break;
        //}

        public int numberOfGenes;
        public ThoughtDef thoughtDefToGiveTarget;
        public ThoughtDef opinionThoughtDefToGiveTarget;
        public IntRange bloodFilthToSpawnRange;
        public int geneLifespanTicks = 300;
    }

    public class CompAbilityEffect_GeneStealerBite : CompAbilityEffect
    {
        public new CompProperties_AbilityGeneStealerBite Props
        {
            get
            {
                return (CompProperties_AbilityGeneStealerBite)this.props;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn == null)
            {
                return;
            }
            //SanguophageUtility.DoBite(this.parent.pawn, pawn, this.Props.hemogenGain, this.Props.nutritionGain, this.Props.targetBloodLoss, this.Props.resistanceGain, this.Props.bloodFilthToSpawnRange, this.Props.thoughtDefToGiveTarget, this.Props.opinionThoughtDefToGiveTarget);
            List<GeneDef> genesForStealing = new List<GeneDef>();
            foreach (Gene gene in pawn.genes.Endogenes)
            {
                if (DefGenerator_GenerateImpliedDefs_PostResolve_Patch.ValidGene(gene.def) && !genesForStealing.Contains(gene.def))//if the gene is valid for stealing and not already in the list.
                    genesForStealing.Add(gene.def);
            }
            foreach (Gene gene in pawn.genes.Xenogenes)
            {
                if (DefGenerator_GenerateImpliedDefs_PostResolve_Patch.ValidGene(gene.def) && !genesForStealing.Contains(gene.def))//if the gene is valid for stealing and not already in the list.
                    genesForStealing.Add(gene.def);
            }
            //Log.Message("genesForStealing.Count: " + genesForStealing.Count);
            //make list of dracnir genes on creation after all, 
            //check if any of the dracnir genes defnames contain the genesForStealing defnames (create list of overlaps directly if possible)
            //up to Props.numberOfGenes times, while there are genesForStealing left, add the corresponding Dracnir gene and remove it from the genesForStealing list
            //wait, no, *all* genes in genesForStealing already have a corresponding Dracnir gene in StaticCollections.allDracnirGenes, so we just need 3 random genes from genesForStealing

            for (int i = 0; i < Props.numberOfGenes; i++)//for each gene we want to steal
            {
                //Log.Message("Trying to get gene");
                if (genesForStealing.Count > 0)
                {
                    GeneDef geneDef = genesForStealing.RandomElement();
                    //Log.Message("Selected gene: " + geneDef.defName);
                    //foreach (GeneDef geneinlist in genesForStealing)
                    //    Log.Message("pre-remove gene: " + geneinlist.defName);
                    genesForStealing.Remove(geneDef);
                    //foreach (GeneDef geneinlist in genesForStealing)
                    //    Log.Message("post-remove gene: " + geneinlist.defName);
                    Gene dracnirGene = parent.pawn.genes.AddGene(StaticCollections.allDracnirGenes.Where(g => g.defName == geneDef.defName + "_DracnirCopy").First(), true);
                    (dracnirGene as DracnirGene).ticksRemaining = Props.geneLifespanTicks;
                }
                //Log.Message("ginished gatting gene");
            }
            if (Props.thoughtDefToGiveTarget != null)//thought
            {
                Pawn_NeedsTracker needs2 = pawn.needs;
                if (needs2 != null)
                {
                    Need_Mood mood = needs2.mood;
                    if (mood != null)
                    {
                        ThoughtHandler thoughts = mood.thoughts;
                        if (thoughts != null)
                        {
                            MemoryThoughtHandler memories = thoughts.memories;
                            if (memories != null)
                            {
                                memories.TryGainMemory((Thought_Memory)ThoughtMaker.MakeThought(Props.thoughtDefToGiveTarget), this.parent.pawn);
                            }
                        }
                    }
                }
            }
            if (Props.opinionThoughtDefToGiveTarget != null)//opinion
            {
                Pawn_NeedsTracker needs3 = pawn.needs;
                if (needs3 != null)
                {
                    Need_Mood mood2 = needs3.mood;
                    if (mood2 != null)
                    {
                        ThoughtHandler thoughts2 = mood2.thoughts;
                        if (thoughts2 != null)
                        {
                            MemoryThoughtHandler memories2 = thoughts2.memories;
                            if (memories2 != null)
                            {
                                memories2.TryGainMemory((Thought_Memory)ThoughtMaker.MakeThought(Props.opinionThoughtDefToGiveTarget), this.parent.pawn);
                            }
                        }
                    }
                }
            }
            pawn.health.AddHediff(HediffDefOf.BloodfeederMark, ExecutionUtility.ExecuteCutPart(pawn), null, null);//bite mark
            int randomInRange = Props.bloodFilthToSpawnRange.RandomInRange;
            for (int i = 0; i < randomInRange; i++)//blood splatter
            {
                IntVec3 c = pawn.Position;
                if (randomInRange > 1 && Rand.Chance(0.8888f))
                {
                    c = pawn.Position.RandomAdjacentCell8Way();
                }
                if (c.InBounds(pawn.MapHeld))
                {
                    FilthMaker.TryMakeFilth(c, pawn.MapHeld, pawn.RaceProps.BloodDef, pawn.LabelShort, 1, FilthSourceFlags.None);
                }
            }
        }
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return this.Valid(target, false);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null)
            {
                return false;
            }
            if (!AbilityUtility.ValidateMustBeHumanOrWildMan(pawn, throwMessages, this.parent))
            {
                return false;
            }
            return true;
        }
    }





    public class DracnirGenes_Mod : Mod
    {
        public DracnirGenes_Mod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("rimworld.LTS.DracnirGenes");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PostResolve))]
    class DefGenerator_GenerateImpliedDefs_PostResolve_Patch
    {
        [HarmonyPostfix]
        public static void DefGenerator_GenerateImpliedDefs_PostResolve_Postfix()
        {
            //if (values.EnumerableNullOrEmpty())
            //{
            //    return values;
            //}
            List<GeneDef> values = DefDatabase<GeneDef>.AllDefsListForReading.ToList();

            //List<GeneDef> resultingList = values.ToList();
            List<GeneDef> resultingList = new List<GeneDef>();

            List<GeneDef> validGenes = DefDatabase<GeneDef>.AllDefsListForReading.Where(x => ValidGene(x)).ToList();

            //foreach (GeneDef generatedGene in GeneDefGenerator.ImpliedGeneDefs(false).Where(x => ValidGene(x)))
            //{
            //    Log.Message(generatedGene.defName);
            //    validGenes.Add(generatedGene);
            //}

            validGenes.Concat((List<GeneDef>)GenDefDatabase.GetAllDefsInDatabaseForDef(typeof(GeneDef)));

            if (!validGenes.NullOrEmpty())
            {
                foreach (var geneDef in validGenes)
                {
                    resultingList.Add(GetDracnirGene(geneDef));

                }
            }
            //return resultingList;

            foreach (GeneDef geneDef in resultingList)
            {
                DefGenerator.AddImpliedDef(geneDef);
            }
        }

        public static bool ValidGene(GeneDef geneDef)
        {
            bool archogeneCheck = geneDef.biostatArc == 0;
            //bool nonCosmeticCheck = geneDef.displayCategory == GeneCategoryDef.
            bool positiveCheck = geneDef.biostatMet < 0;//must be negative
            bool prerequisiteCheck = geneDef.prerequisite == null;
            return archogeneCheck && positiveCheck && prerequisiteCheck && !geneDef.defName.Contains("Randomizer") && !geneDef.defName.Contains("VREA_") && !geneDef.defName.Contains("VREW_Pollution") && !geneDef.defName.Contains("_Astrogene") && geneDef != GeneDefOf.Inbred;
        }

        private static GeneDef GetDracnirGene(GeneDef geneDef)
        {
            GeneDef clonedGene = (GeneDef)Clone(geneDef);

            clonedGene.defName = geneDef.defName + "_DracnirCopy";
            clonedGene.geneClass = typeof(DracnirGene);
            clonedGene.selectionWeight = 0;
            clonedGene.biostatCpx = 0;
            clonedGene.biostatMet = 0;

            clonedGene.displayOrderInCategory += 99999;

            var existingGeneExtension = clonedGene.GetModExtension<GeneExtension>();
            if (existingGeneExtension != null)
            {
                clonedGene.modExtensions.Remove(existingGeneExtension);
                var clonedGeneExtension = (GeneExtension)Clone(existingGeneExtension);

                clonedGeneExtension.backgroundPathXenogenes = "UI/GeneBackground_DracnirGene";
                clonedGeneExtension.backgroundPathXenogenes = "UI/GeneBackground_DracnirGene";
                clonedGeneExtension.disableGeneExtraction = true;

                clonedGene.modExtensions.Add(clonedGeneExtension);

            }
            else
            {
                //clonedGene.modExtensions ??= new List<DefModExtension>();
                if (clonedGene.modExtensions == null)
                    clonedGene.modExtensions = new List<DefModExtension>();
                clonedGene.modExtensions.Add(new GeneExtension
                {
                    backgroundPathXenogenes = "UI/GeneBackground_DracnirGene",
                    backgroundPathEndogenes = "UI/GeneBackground_DracnirGene",
                    disableGeneExtraction = true,
                });
            }

            clonedGene.canGenerateInGeneSet = false;
            clonedGene.ResolveDefNameHash();
            StaticCollections.allDracnirGenes.Add(clonedGene);

            return clonedGene;
        }
        public static object Clone(object obj)
        {
            var cloneMethod = obj.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return cloneMethod.Invoke(obj, null);
        }
    }
    //[HarmonyPatch(typeof(GeneDefGenerator), nameof(GeneDefGenerator.ImpliedGeneDefs))]
    //class GeneDefGenerator_ImpliedGeneDefs_Patch
    //{
    //    [HarmonyPostfix]
    //    public static IEnumerable<GeneDef> GeneDefGenerator_ImpliedGeneDefs_Postfix(IEnumerable<GeneDef> values)
    //    {
    //        if (values.EnumerableNullOrEmpty())
    //        {
    //            return values;
    //        }

    //        List<GeneDef> resultingList = values.ToList();

    //        List<GeneDef> validGenes = DefDatabase<GeneDef>.AllDefsListForReading.Where(x => ValidGene(x)).ToList();

    //        //foreach (GeneDef generatedGene in GeneDefGenerator.ImpliedGeneDefs(false).Where(x => ValidGene(x)))
    //        //{
    //        //    Log.Message(generatedGene.defName);
    //        //    validGenes.Add(generatedGene);
    //        //}

    //        validGenes.Concat((List<GeneDef>)GenDefDatabase.GetAllDefsInDatabaseForDef(typeof(GeneDef)));

    //        if (!validGenes.NullOrEmpty())
    //        {
    //            foreach (var geneDef in validGenes)
    //            {
    //                resultingList.Add(GetDracnirGene(geneDef));

    //            }
    //        }
    //        return resultingList;
    //    }

    //    public static bool ValidGene(GeneDef geneDef)
    //    {
    //        bool archogeneCheck = geneDef.biostatArc == 0;
    //        //bool nonCosmeticCheck = geneDef.displayCategory == GeneCategoryDef.
    //        bool positiveCheck = geneDef.biostatMet < 0;//must be negative
    //        bool prerequisiteCheck = geneDef.prerequisite == null;
    //        return archogeneCheck && positiveCheck && prerequisiteCheck && !geneDef.defName.Contains("Randomizer") && !geneDef.defName.Contains("VREA_") && !geneDef.defName.Contains("VREW_Pollution") && !geneDef.defName.Contains("_Astrogene");
    //    }

    //    private static GeneDef GetDracnirGene(GeneDef geneDef)
    //    {
    //        GeneDef clonedGene = (GeneDef)Clone(geneDef);

    //        clonedGene.defName = geneDef.defName + "_DracnirCopy";
    //        clonedGene.geneClass = typeof(DracnirGene);
    //        clonedGene.selectionWeight = 0;
    //        clonedGene.biostatCpx = 0;
    //        clonedGene.biostatMet = 0;

    //        clonedGene.displayOrderInCategory += 99999;

    //        var existingGeneExtension = clonedGene.GetModExtension<GeneExtension>();
    //        if (existingGeneExtension != null)
    //        {
    //            clonedGene.modExtensions.Remove(existingGeneExtension);
    //            var clonedGeneExtension = (GeneExtension)Clone(existingGeneExtension);

    //            clonedGeneExtension.backgroundPathXenogenes = "UI/GeneBackground_DracnirGene";
    //            clonedGeneExtension.backgroundPathXenogenes = "UI/GeneBackground_DracnirGene";
    //            clonedGeneExtension.disableGeneExtraction = true;

    //            clonedGene.modExtensions.Add(clonedGeneExtension);

    //        }
    //        else
    //        {
    //            //clonedGene.modExtensions ??= new List<DefModExtension>();
    //            if (clonedGene.modExtensions == null)
    //                clonedGene.modExtensions = new List<DefModExtension>();
    //            clonedGene.modExtensions.Add(new GeneExtension
    //            {
    //                backgroundPathXenogenes = "UI/GeneBackground_DracnirGene",
    //                backgroundPathEndogenes = "UI/GeneBackground_DracnirGene",
    //                disableGeneExtraction = true,
    //            });
    //        }

    //        clonedGene.canGenerateInGeneSet = false;
    //        clonedGene.ResolveDefNameHash();
    //        StaticCollections.allDracnirGenes.Add(clonedGene);

    //        return clonedGene;
    //    }
    //    public static object Clone(object obj)
    //    {
    //        var cloneMethod = obj.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
    //        return cloneMethod.Invoke(obj, null);
    //    }
    //}
    [StaticConstructorOnStartup]
    public static class StaticCollections
    {
        public static HashSet<GeneDef> allDracnirGenes = new HashSet<GeneDef>();
        static StaticCollections()
        {
        }
    }
}
