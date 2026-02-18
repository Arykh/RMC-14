using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Medical.Autodoc;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.Autodoc;

public sealed class AutodocSystem : SharedAutodocSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly RMCPulseSystem _rmcPulse = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private const string BruteGroup = "Brute";
    private const string BurnGroup = "Burn";
    private const string ToxinGroup = "Toxin";
    private const string AirlossGroup = "Airloss";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutodocConsoleComponent, AfterActivatableUIOpenEvent>(OnConsoleUIOpened);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleBruteBuiMsg>(OnConsoleToggleBrute);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleBurnBuiMsg>(OnConsoleToggleBurn);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleToxinBuiMsg>(OnConsoleToggleToxin);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleBloodBuiMsg>(OnConsoleToggleBlood);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleDialysisBuiMsg>(OnConsoleToggleDialysis);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocStartSurgeryBuiMsg>(OnConsoleStartSurgery);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocClearBuiMsg>(OnConsoleClear);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocEjectBuiMsg>(OnConsoleEject);
    }

    private void OnConsoleUIOpened(Entity<AutodocConsoleComponent> console, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(console);
    }

    private void OnConsoleToggleBrute(Entity<AutodocConsoleComponent> console, ref AutodocToggleBruteBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        if (autodoc.Comp.IsSurgeryInProgress)
            return;

        autodoc.Comp.HealingBrute = !autodoc.Comp.HealingBrute;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleBurn(Entity<AutodocConsoleComponent> console, ref AutodocToggleBurnBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        if (autodoc.Comp.IsSurgeryInProgress)
            return;

        autodoc.Comp.HealingBurn = !autodoc.Comp.HealingBurn;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleToxin(Entity<AutodocConsoleComponent> console, ref AutodocToggleToxinBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        if (autodoc.Comp.IsSurgeryInProgress)
            return;

        autodoc.Comp.HealingToxin = !autodoc.Comp.HealingToxin;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleBlood(Entity<AutodocConsoleComponent> console, ref AutodocToggleBloodBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        if (autodoc.Comp.IsSurgeryInProgress)
            return;

        autodoc.Comp.BloodTransfusion = !autodoc.Comp.BloodTransfusion;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleDialysis(Entity<AutodocConsoleComponent> console, ref AutodocToggleDialysisBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        if (autodoc.Comp.IsSurgeryInProgress)
            return;

        autodoc.Comp.Filtering = !autodoc.Comp.Filtering;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleStartSurgery(Entity<AutodocConsoleComponent> console, ref AutodocStartSurgeryBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        if (autodoc.Comp.Occupant == null)
            return;

        if (autodoc.Comp.IsSurgeryInProgress)
            return;

        // Check if any surgery is queued
        if (!autodoc.Comp.HealingBrute && !autodoc.Comp.HealingBurn &&
            !autodoc.Comp.HealingToxin && !autodoc.Comp.BloodTransfusion &&
            !autodoc.Comp.Filtering)
        {
            return;
        }

        autodoc.Comp.IsSurgeryInProgress = true;
        autodoc.Comp.NextTick = _timing.CurTime + autodoc.Comp.TickDelay;
        Dirty(autodoc);
        UpdateSurgeryVisuals(autodoc);
        _audio.PlayPvs(autodoc.Comp.SurgeryStartSound, autodoc);
        UpdateUI(console);
    }

    private void OnConsoleClear(Entity<AutodocConsoleComponent> console, ref AutodocClearBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        if (autodoc.Comp.IsSurgeryInProgress)
            return;

        autodoc.Comp.HealingBrute = false;
        autodoc.Comp.HealingBurn = false;
        autodoc.Comp.HealingToxin = false;
        autodoc.Comp.BloodTransfusion = false;
        autodoc.Comp.Filtering = false;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleEject(Entity<AutodocConsoleComponent> console, ref AutodocEjectBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        if (autodoc.Comp.Occupant is { } occupant)
            TryEjectOccupant(autodoc, occupant, args.Actor);

        UpdateUI(console);
    }

    private bool TryGetLinkedAutodoc(Entity<AutodocConsoleComponent> console, out Entity<AutodocComponent> autodoc)
    {
        autodoc = default;
        if (console.Comp.LinkedAutodoc is not { } autodocId ||
            !TryComp(autodocId, out AutodocComponent? autodocComp))
            return false;

        autodoc = (autodocId, autodocComp);
        return true;
    }

    private void UpdateUI(Entity<AutodocConsoleComponent> console)
    {
        if (!_ui.IsUiOpen(console.Owner, AutodocUIKey.Key))
            return;

        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        var occupant = autodoc.Comp.Occupant;
        NetEntity? netOccupant = null;
        string? occupantName = null;
        var occupantState = AutodocOccupantMobState.None;
        var health = 0f;
        var maxHealth = 0f;
        var bruteLoss = 0f;
        var burnLoss = 0f;
        var toxinLoss = 0f;
        var oxyLoss = 0f;
        var hasBlood = false;
        FixedPoint2 bloodLevel = 0;
        var bloodPercent = 0f;
        var pulse = 0;
        FixedPoint2 totalReagents = 0;

        if (occupant != null)
        {
            if (TryComp<DamageableComponent>(occupant, out var damageable))
            {
                netOccupant = GetNetEntity(occupant.Value);
                occupantName = Identity.Name(occupant.Value, EntityManager);

                if (_mobState.IsDead(occupant.Value))
                    occupantState = AutodocOccupantMobState.Dead;
                else if (_mobState.IsCritical(occupant.Value))
                    occupantState = AutodocOccupantMobState.Critical;
                else
                    occupantState = AutodocOccupantMobState.Alive;

                var totalDamage = damageable.TotalDamage;

                if (_mobThreshold.TryGetThresholdForState(occupant.Value, MobState.Critical, out var critThreshold))
                {
                    maxHealth = (float) critThreshold;
                    health = (float) (critThreshold - totalDamage);
                }

                bruteLoss = damageable.DamagePerGroup.GetValueOrDefault(BruteGroup).Float();
                burnLoss = damageable.DamagePerGroup.GetValueOrDefault(BurnGroup).Float();
                toxinLoss = damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup).Float();
                oxyLoss = damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup).Float();
            }

            if (TryComp<BloodstreamComponent>(occupant, out var blood) &&
                blood.BloodSolution != null &&
                _solution.TryGetSolution(occupant.Value, blood.BloodSolutionName, out _, out var bloodSol))
            {
                hasBlood = true;
                bloodLevel = bloodSol.Volume;
                var bloodMax = bloodSol.MaxVolume;
                bloodPercent = bloodMax > 0 ? (bloodLevel / bloodMax).Float() * 100f : 0f;

                pulse = _rmcPulse.GetPulseValue(occupant.Value, true);
            }

            if (_solution.TryGetSolution(occupant.Value, "chemicals", out _, out var chemSol))
                totalReagents = chemSol.Volume;
        }

        var state = new AutodocBuiState(
            netOccupant,
            occupantName,
            occupantState,
            health,
            maxHealth,
            bruteLoss,
            burnLoss,
            toxinLoss,
            oxyLoss,
            hasBlood,
            bloodLevel,
            bloodPercent,
            pulse,
            autodoc.Comp.IsSurgeryInProgress,
            autodoc.Comp.HealingBrute,
            autodoc.Comp.HealingBurn,
            autodoc.Comp.HealingToxin,
            autodoc.Comp.BloodTransfusion,
            autodoc.Comp.Filtering,
            totalReagents);

        _ui.SetUiState(console.Owner, AutodocUIKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        // Update consoles UI periodically
        var consoles = EntityQueryEnumerator<AutodocConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var console))
        {
            if (!_ui.IsUiOpen(uid, AutodocUIKey.Key))
                continue;

            if (time < console.UpdateAt)
                continue;

            console.UpdateAt = time + console.UpdateCooldown;
            UpdateUI((uid, console));
        }

        // Process autodocs
        var autodocs = EntityQueryEnumerator<AutodocComponent>();
        while (autodocs.MoveNext(out var uid, out var autodoc))
        {
            if (autodoc.Occupant == null)
                continue;

            var occupant = autodoc.Occupant.Value;
            if (!autodoc.IsSurgeryInProgress)
                continue;

            if (time < autodoc.NextTick)
                continue;

            autodoc.NextTick = time + autodoc.TickDelay;

            if (_mobState.IsDead(occupant))
            {
                autodoc.IsSurgeryInProgress = false;
                Dirty(uid, autodoc);
                UpdateSurgeryVisuals((uid, autodoc));
                continue;
            }

            var anyTreatmentRemaining = false;
            if (autodoc.HealingBrute)
            {
                if (TryComp<DamageableComponent>(occupant, out var damageable) &&
                    damageable.DamagePerGroup.GetValueOrDefault(BruteGroup) > 0)
                {
                    var healSpec = new DamageSpecifier();
                    if (_proto.TryIndex<DamageGroupPrototype>(BruteGroup, out var bruteGroup))
                    {
                        foreach (var type in bruteGroup.DamageTypes)
                        {
                            healSpec.DamageDict[type] = -autodoc.HealAmount;
                        }
                    }
                    _damageable.TryChangeDamage(occupant, healSpec, true, false);
                    anyTreatmentRemaining = true;
                }
                else
                {
                    autodoc.HealingBrute = false;
                    Dirty(uid, autodoc);
                }
            }

            if (autodoc.HealingBurn)
            {
                if (TryComp<DamageableComponent>(occupant, out var damageable) &&
                    damageable.DamagePerGroup.GetValueOrDefault(BurnGroup) > 0)
                {
                    var healSpec = new DamageSpecifier();
                    if (_proto.TryIndex<DamageGroupPrototype>(BurnGroup, out var burnGroup))
                    {
                        foreach (var type in burnGroup.DamageTypes)
                        {
                            healSpec.DamageDict[type] = -autodoc.HealAmount;
                        }
                    }
                    _damageable.TryChangeDamage(occupant, healSpec, true, false);
                    anyTreatmentRemaining = true;
                }
                else
                {
                    autodoc.HealingBurn = false;
                    Dirty(uid, autodoc);
                }
            }

            if (autodoc.HealingToxin)
            {
                if (TryComp<DamageableComponent>(occupant, out var damageable) &&
                    damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup) > 0)
                {
                    var healSpec = new DamageSpecifier();
                    if (_proto.TryIndex<DamageGroupPrototype>(ToxinGroup, out var toxinGroup))
                    {
                        foreach (var type in toxinGroup.DamageTypes)
                        {
                            healSpec.DamageDict[type] = -autodoc.ToxinHealAmount;
                        }
                    }
                    _damageable.TryChangeDamage(occupant, healSpec, true, false);
                    anyTreatmentRemaining = true;
                }
                else
                {
                    autodoc.HealingToxin = false;
                    Dirty(uid, autodoc);
                }
            }

            if (autodoc.BloodTransfusion)
            {
                if (TryComp<BloodstreamComponent>(occupant, out var blood) &&
                    _solution.TryGetSolution(occupant, blood.BloodSolutionName, out var bloodSolEnt, out var bloodSol) &&
                    bloodSol.Volume < bloodSol.MaxVolume)
                {
                    // Add blood directly
                    _solution.TryAddReagent(bloodSolEnt.Value, blood.BloodReagent, autodoc.BloodTransfusionAmount);
                    anyTreatmentRemaining = true;
                }
                else
                {
                    autodoc.BloodTransfusion = false;
                    Dirty(uid, autodoc);
                }
            }

            if (autodoc.Filtering)
            {
                _rmcBloodstream.RemoveBloodstreamToxins(occupant, autodoc.DialysisAmount);
                if (_rmcBloodstream.TryGetChemicalSolution(occupant, out _, out var chemSol))
                {
                    var hasToxins = false;
                    foreach (var content in chemSol.Contents)
                    {
                        if (content.Quantity > 0)
                        {
                            hasToxins = true;
                            break;
                        }
                    }

                    if (hasToxins)
                        anyTreatmentRemaining = true;
                    else
                    {
                        autodoc.Filtering = false;
                        Dirty(uid, autodoc);
                    }
                }
                else
                {
                    autodoc.Filtering = false;
                    Dirty(uid, autodoc);
                }
            }

            if (!anyTreatmentRemaining)
            {
                autodoc.IsSurgeryInProgress = false;
                Dirty(uid, autodoc);
                UpdateSurgeryVisuals((uid, autodoc));
                _audio.PlayPvs(autodoc.SurgeryCompleteSound, uid);
            }
        }
    }
}
