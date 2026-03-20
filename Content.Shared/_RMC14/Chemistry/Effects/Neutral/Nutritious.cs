using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Nutritious : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var updatedFactor = NutrimentFactor + Potency;
        return $"Restores [color=green]{updatedFactor * Potency}[/color] nutrients to the body and satiates hunger";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var mobStateSystem = args.EntityManager.System<MobStateSystem>();
        if (mobStateSystem.IsDead(args.TargetEntity))
            return;

        var hungerSys = args.EntityManager.System<HungerSystem>();
        var updatedFactor = NutrimentFactor + Potency;
        hungerSys.ModifyHunger(args.TargetEntity, updatedFactor * Potency);
    }
}
