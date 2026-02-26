using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Medical.Autodoc;
using Content.Shared._RMC14.Medical.Surgery.Steps.Parts;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.Autodoc;

public sealed class AutodocSystem : SharedAutodocSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly RMCPulseSystem _rmcPulse = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private readonly List<ProtoId<ReagentPrototype>> _reagentRemovalBuffer = [];
    private readonly HashSet<string> _nonTransferableLookup = [];

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutodocConsoleComponent, AfterActivatableUIOpenEvent>(OnConsoleUIOpened);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleBruteBuiMsg>(OnConsoleToggleBrute);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleBurnBuiMsg>(OnConsoleToggleBurn);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleBloodBuiMsg>(OnConsoleToggleBlood);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleDialysisBuiMsg>(OnConsoleToggleDialysis);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleToxinBuiMsg>(OnConsoleToggleToxin);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleCloseIncisionsBuiMsg>(OnConsoleToggleCloseIncisions);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleRemoveShrapnelBuiMsg>(OnConsoleToggleRemoveShrapnel);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleInternalBleedingBuiMsg>(OnConsoleToggleInternalBleeding);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleBrokenBoneBuiMsg>(OnConsoleToggleBrokenBone);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleOrganDamageBuiMsg>(OnConsoleToggleOrganDamage);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleLarvaBuiMsg>(OnConsoleToggleLarva);
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
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.HealingBrute = !autodoc.Comp.HealingBrute;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleBurn(Entity<AutodocConsoleComponent> console, ref AutodocToggleBurnBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.HealingBurn = !autodoc.Comp.HealingBurn;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleBlood(Entity<AutodocConsoleComponent> console, ref AutodocToggleBloodBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.BloodTransfusion = !autodoc.Comp.BloodTransfusion;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleDialysis(Entity<AutodocConsoleComponent> console, ref AutodocToggleDialysisBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.Filtering = !autodoc.Comp.Filtering;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleToxin(Entity<AutodocConsoleComponent> console, ref AutodocToggleToxinBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.HealingToxin = !autodoc.Comp.HealingToxin;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleCloseIncisions(Entity<AutodocConsoleComponent> console, ref AutodocToggleCloseIncisionsBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.CloseIncisions = !autodoc.Comp.CloseIncisions;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleRemoveShrapnel(Entity<AutodocConsoleComponent> console, ref AutodocToggleRemoveShrapnelBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.RemoveShrapnel = !autodoc.Comp.RemoveShrapnel;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleInternalBleeding(Entity<AutodocConsoleComponent> console, ref AutodocToggleInternalBleedingBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.InternalBleeding = !autodoc.Comp.InternalBleeding;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleBrokenBone(Entity<AutodocConsoleComponent> console, ref AutodocToggleBrokenBoneBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.BrokenBone = !autodoc.Comp.BrokenBone;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleOrganDamage(Entity<AutodocConsoleComponent> console, ref AutodocToggleOrganDamageBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.OrganDamage = !autodoc.Comp.OrganDamage;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleLarva(Entity<AutodocConsoleComponent> console, ref AutodocToggleLarvaBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.RemoveLarva = !autodoc.Comp.RemoveLarva;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleStartSurgery(Entity<AutodocConsoleComponent> console, ref AutodocStartSurgeryBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        if (autodoc.Comp.Occupant == null)
            return;

        // Check if any surgery is queued
        if (autodoc.Comp is
            {
                HealingBrute: false, HealingBurn: false, CloseIncisions: false, RemoveShrapnel: false,
                BloodTransfusion: false, Filtering: false, HealingToxin: false,
                InternalBleeding: false, BrokenBone: false, OrganDamage: false, RemoveLarva: false,
            })
        {
            return;
        }

        autodoc.Comp.IsSurgeryInProgress = true;
        autodoc.Comp.NextTick = _timing.CurTime + autodoc.Comp.TickDelay;
        autodoc.Comp.CurrentSurgeryType = AutodocSurgeryType.None;
        Dirty(autodoc);
        UpdateSurgeryVisuals(autodoc);
        _audio.PlayPvs(autodoc.Comp.SurgeryStepSound, autodoc);
        UpdateUI(console);
    }

    private void OnConsoleClear(Entity<AutodocConsoleComponent> console, ref AutodocClearBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc, true))
            return;

        autodoc.Comp.HealingBrute = false;
        autodoc.Comp.HealingBurn = false;
        autodoc.Comp.HealingToxin = false;
        autodoc.Comp.BloodTransfusion = false;
        autodoc.Comp.Filtering = false;
        autodoc.Comp.RemoveLarva = false;
        autodoc.Comp.CloseIncisions = false;
        autodoc.Comp.RemoveShrapnel = false;
        autodoc.Comp.InternalBleeding = false;
        autodoc.Comp.BrokenBone = false;
        autodoc.Comp.OrganDamage = false;
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

    private bool TryGetLinkedAutodoc(Entity<AutodocConsoleComponent> console, out Entity<AutodocComponent> autodoc, bool checkSurgeryInProgress = false)
    {
        autodoc = default;
        if (console.Comp.LinkedAutodoc is not { } autodocId || !TryComp(autodocId, out AutodocComponent? autodocComp))
            return false;

        autodoc = (autodocId, autodocComp);

        return !checkSurgeryInProgress || !autodoc.Comp.IsSurgeryInProgress;
    }

    private bool HasLarva(EntityUid occupant)
    {
        return TryComp<VictimInfectedComponent>(occupant, out var infected) && !infected.IsBursting;
    }

    private void PerformLarvaExtraction(EntityUid uid, AutodocComponent autodoc, EntityUid occupant)
    {
        if (!TryComp<VictimInfectedComponent>(occupant, out var infected))
        {
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-unneeded"), uid);
            autodoc.RemoveLarva = false;
            autodoc.CurrentSurgeryType = AutodocSurgeryType.None;
            Dirty(uid, autodoc);
            return;
        }

        if (infected.IsBursting)
        {
            autodoc.RemoveLarva = false;
            autodoc.CurrentSurgeryType = AutodocSurgeryType.None;
            Dirty(uid, autodoc);
            return;
        }

        infected.RootsCut = true;

        if (infected.SpawnedLarva != null)
            QueueDel(infected.SpawnedLarva.Value);

        RemComp<VictimInfectedComponent>(occupant);
        _audio.PlayPvs(autodoc.SurgeryStepSound, uid);

        autodoc.RemoveLarva = false;
        autodoc.CurrentSurgeryType = AutodocSurgeryType.None;
        Dirty(uid, autodoc);
    }

    private bool HasOpenIncisions(EntityUid occupant)
    {
        foreach (var part in _body.GetBodyChildren(occupant))
        {
            if (HasComp<CMIncisionOpenComponent>(part.Id))
                return true;
        }
        return false;
    }

    private void PerformCloseIncisions(EntityUid uid, AutodocComponent autodoc, EntityUid occupant)
    {
        var closedAny = false;
        foreach (var part in _body.GetBodyChildren(occupant))
        {
            if (HasComp<CMIncisionOpenComponent>(part.Id))
            {
                RemComp<CMIncisionOpenComponent>(part.Id);
                RemCompDeferred<CMBleedersClampedComponent>(part.Id);
                RemCompDeferred<CMSkinRetractedComponent>(part.Id);
                RemCompDeferred<CMRibcageOpenComponent>(part.Id);
                closedAny = true;
            }
        }

        if (closedAny)
            _audio.PlayPvs(autodoc.SurgeryStepSound, uid);
        else
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-unneeded"), uid);

        autodoc.CloseIncisions = false;
        autodoc.CurrentSurgeryType = AutodocSurgeryType.None;
        Dirty(uid, autodoc);
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
        FixedPoint2 bloodLevel = 0;
        var bloodPercent = 0f;
        var pulse = string.Empty;
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
                bloodLevel = bloodSol.Volume;
                var bloodMax = bloodSol.MaxVolume;
                bloodPercent = bloodMax > 0 ? (bloodLevel / bloodMax).Float() * 100f : 0f;

                pulse = _rmcPulse.TryGetPulseReading(occupant.Value, true, out _);
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
            bloodLevel,
            bloodPercent,
            pulse,
            autodoc.Comp.IsSurgeryInProgress,
            autodoc.Comp.HealingBrute,
            autodoc.Comp.HealingBurn,
            autodoc.Comp.HealingToxin,
            autodoc.Comp.BloodTransfusion,
            autodoc.Comp.Filtering,
            totalReagents,
            autodoc.Comp.RemoveLarva,
            autodoc.Comp.CloseIncisions,
            autodoc.Comp.RemoveShrapnel,
            autodoc.Comp.InternalBleeding,
            autodoc.Comp.BrokenBone,
            autodoc.Comp.OrganDamage);

        _ui.SetUiState(console.Owner, AutodocUIKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
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
                autodoc.CurrentSurgeryType = AutodocSurgeryType.None;
                Dirty(uid, autodoc);
                UpdateSurgeryVisuals((uid, autodoc));
                _popup.PopupEntity(Loc.GetString("rmc-autodoc-patient-dead"), uid);
                _audio.PlayPvs(autodoc.AutoEjectDeadSound, uid);
                TryEjectOccupant((uid, autodoc), occupant);
                continue;
            }

            // Life support: keep patient alive during surgery
            if (TryComp<DamageableComponent>(occupant, out var damageable))
            {
                if (damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup) > 0)
                {
                    var healing = _rmcDamageable.DistributeHealingCached(occupant, ToxinGroup, 0.25);
                    _damageable.TryChangeDamage(occupant, healing, true, false);
                }

                if (damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup) > 0)
                {
                    var healing = _rmcDamageable.DistributeHealingCached(occupant, AirlossGroup, damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup));
                    _damageable.TryChangeDamage(occupant, healing, true, false);
                }
            }

            var anyTreatmentRemaining = false;
            if (autodoc.HealingBrute)
            {
                if (damageable != null && damageable.DamagePerGroup.GetValueOrDefault(BruteGroup) > 0)
                {
                    var healing = _rmcDamageable.DistributeHealingCached(occupant, BruteGroup, autodoc.BruteHealAmount);
                    _damageable.TryChangeDamage(occupant, healing, true, false);
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
                if (damageable != null && damageable.DamagePerGroup.GetValueOrDefault(BurnGroup) > 0)
                {
                    var healing = _rmcDamageable.DistributeHealingCached(occupant, BurnGroup, autodoc.BurnHealAmount);
                    _damageable.TryChangeDamage(occupant, healing, true, false);
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
                if (damageable != null && damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup) > 0)
                {
                    var healing = _rmcDamageable.DistributeHealingCached(occupant, ToxinGroup, autodoc.ToxinHealAmount);
                    _damageable.TryChangeDamage(occupant, healing, true, false);
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
                if (_rmcBloodstream.TryGetChemicalSolution(occupant, out var chemSolEnt, out var chemSol))
                {
                    // Build non-transferable lookup for O(1) checks
                    _nonTransferableLookup.Clear();
                    foreach (var reagent in autodoc.NonTransferableReagents)
                    {
                        _nonTransferableLookup.Add(reagent);
                    }

                    _reagentRemovalBuffer.Clear();
                    foreach (var reagentQuantity in chemSol.Contents)
                    {
                        if (!_nonTransferableLookup.Contains(reagentQuantity.Reagent.Prototype))
                            _reagentRemovalBuffer.Add(reagentQuantity.Reagent.Prototype);
                    }

                    foreach (var reagent in _reagentRemovalBuffer)
                    {
                        _solution.RemoveReagent(chemSolEnt, reagent, autodoc.DialysisAmount);
                    }

                    // Check if dialysis is complete
                    var hasTransferableReagents = false;
                    foreach (var reagentQuantity in chemSol.Contents)
                    {
                        if (!_nonTransferableLookup.Contains(reagentQuantity.Reagent.Prototype) && reagentQuantity.Quantity > 0)
                        {
                            hasTransferableReagents = true;
                            break;
                        }
                    }

                    if (hasTransferableReagents)
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

            if (autodoc.CurrentSurgeryType != AutodocSurgeryType.None)
            {
                // Surgery is in progress - waiting for completion
                anyTreatmentRemaining = true;
                if (time >= autodoc.SurgeryCompleteAt)
                {
                    // Time expired - perform the surgery
                    switch (autodoc.CurrentSurgeryType)
                    {
                        case AutodocSurgeryType.LarvaExtraction:
                            PerformLarvaExtraction(uid, autodoc, occupant);
                            break;
                        case AutodocSurgeryType.CloseIncision:
                            PerformCloseIncisions(uid, autodoc, occupant);
                            break;
                    }
                    // Note: The Perform methods reset CurrentSurgeryType to None
                }
            }
            // If no surgery in progress, check if we should start one
            else if (autodoc.RemoveLarva)
            {
                var hasLarva = HasLarva(occupant);
                autodoc.CurrentSurgeryType = AutodocSurgeryType.LarvaExtraction;
                autodoc.SurgeryCompleteAt = time + (hasLarva
                    ? autodoc.ScalpelDuration + autodoc.HemostatDuration + autodoc.RemoveObjectDuration
                    : autodoc.UnneededDelay);
                anyTreatmentRemaining = true;

                if (hasLarva)
                    _popup.PopupEntity(Loc.GetString("rmc-autodoc-larva-starting"), uid);

                Dirty(uid, autodoc);
            }
            else if (autodoc.CloseIncisions)
            {
                var hasIncisions = HasOpenIncisions(occupant);
                autodoc.CurrentSurgeryType = AutodocSurgeryType.CloseIncision;
                autodoc.SurgeryCompleteAt = time + (hasIncisions
                    ? autodoc.CauteryDuration
                    : autodoc.UnneededDelay);
                anyTreatmentRemaining = true;

                if (hasIncisions)
                    _popup.PopupEntity(Loc.GetString("rmc-autodoc-incisions-starting"), uid);

                Dirty(uid, autodoc);
            }

            if (!anyTreatmentRemaining)
            {
                autodoc.IsSurgeryInProgress = false;
                autodoc.CurrentSurgeryType = AutodocSurgeryType.None;
                Dirty(uid, autodoc);
                UpdateSurgeryVisuals((uid, autodoc));
                _audio.PlayPvs(autodoc.SurgeryCompleteSound, uid);
                _popup.PopupEntity(Loc.GetString("rmc-autodoc-complete"), uid);
            }
        }
    }
}
