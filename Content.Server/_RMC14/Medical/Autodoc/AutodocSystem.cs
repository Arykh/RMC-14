using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Medical.Autodoc;
using Content.Shared._RMC14.Medical.Surgery.Steps.Parts;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
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
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutodocConsoleComponent, AfterActivatableUIOpenEvent>(OnConsoleUIOpened);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleBruteBuiMsg>(OnConsoleToggleBrute);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleBurnBuiMsg>(OnConsoleToggleBurn);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleToxinBuiMsg>(OnConsoleToggleToxin);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleBloodBuiMsg>(OnConsoleToggleBlood);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleDialysisBuiMsg>(OnConsoleToggleDialysis);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleLarvaBuiMsg>(OnConsoleToggleLarva);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleIncisionsBuiMsg>(OnConsoleToggleIncisions);
        SubscribeLocalEvent<AutodocConsoleComponent, AutodocToggleShrapnelBuiMsg>(OnConsoleToggleShrapnel);
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

    private void OnConsoleToggleLarva(Entity<AutodocConsoleComponent> console, ref AutodocToggleLarvaBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        if (autodoc.Comp.IsSurgeryInProgress)
            return;

        autodoc.Comp.RemoveLarva = !autodoc.Comp.RemoveLarva;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleIncisions(Entity<AutodocConsoleComponent> console, ref AutodocToggleIncisionsBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        if (autodoc.Comp.IsSurgeryInProgress)
            return;

        autodoc.Comp.CloseIncisions = !autodoc.Comp.CloseIncisions;
        Dirty(autodoc);
        UpdateUI(console);
    }

    private void OnConsoleToggleShrapnel(Entity<AutodocConsoleComponent> console, ref AutodocToggleShrapnelBuiMsg args)
    {
        if (!TryGetLinkedAutodoc(console, out var autodoc))
            return;

        if (autodoc.Comp.IsSurgeryInProgress)
            return;

        autodoc.Comp.RemoveShrapnel = !autodoc.Comp.RemoveShrapnel;
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
        if (autodoc.Comp is { HealingBrute: false, HealingBurn: false, HealingToxin: false, BloodTransfusion: false, Filtering: false, RemoveLarva: false, CloseIncisions: false, RemoveShrapnel: false })
        {
            return;
        }

        autodoc.Comp.IsSurgeryInProgress = true;
        autodoc.Comp.NextTick = _timing.CurTime + autodoc.Comp.TickDelay;
        autodoc.Comp.CurrentSurgeryType = AutodocSurgeryType.None;
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
        autodoc.Comp.RemoveLarva = false;
        autodoc.Comp.CloseIncisions = false;
        autodoc.Comp.RemoveShrapnel = false;
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

    private bool HasLarva(EntityUid occupant)
    {
        return TryComp<VictimInfectedComponent>(occupant, out var infected) && !infected.IsBursting;
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

    private bool HasShrapnel(EntityUid occupant)
    {
        if (TryComp<EmbeddedContainerComponent>(occupant, out var embedded))
        {
            return embedded.EmbeddedObjects.Count > 0;
        }
        return false;
    }

    private void PerformLarvaExtraction(EntityUid uid, AutodocComponent autodoc, EntityUid occupant)
    {
        if (!TryComp<VictimInfectedComponent>(occupant, out var infected))
        {
            autodoc.RemoveLarva = false;
            autodoc.CurrentSurgeryType = AutodocSurgeryType.None;
            Dirty(uid, autodoc);
            return;
        }

        if (infected.IsBursting)
        {
            autodoc.RemoveLarva = false;
            autodoc.CurrentSurgeryType = AutodocSurgeryType.None;
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-larva-bursting"), uid);
            Dirty(uid, autodoc);
            return;
        }

        infected.RootsCut = true;

        if (infected.SpawnedLarva != null)
        {
            QueueDel(infected.SpawnedLarva.Value);
        }

        RemComp<VictimInfectedComponent>(occupant);

        _popup.PopupEntity(Loc.GetString("rmc-autodoc-larva-removed"), uid);
        _audio.PlayPvs(autodoc.SurgeryStepSound, uid);

        autodoc.RemoveLarva = false;
        autodoc.CurrentSurgeryType = AutodocSurgeryType.None;
        Dirty(uid, autodoc);
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
        {
            _popup.PopupEntity(Loc.GetString("rmc-autodoc-incisions-closed"), uid);
            _audio.PlayPvs(autodoc.SurgeryStepSound, uid);
        }

        autodoc.CloseIncisions = false;
        autodoc.CurrentSurgeryType = AutodocSurgeryType.None;
        Dirty(uid, autodoc);
    }

    private void PerformShrapnelRemoval(EntityUid uid, AutodocComponent autodoc, EntityUid occupant)
    {
        if (!TryComp<EmbeddedContainerComponent>(occupant, out var embedded) || embedded.EmbeddedObjects.Count == 0)
        {
            autodoc.RemoveShrapnel = false;
            autodoc.CurrentSurgeryType = AutodocSurgeryType.None;
            Dirty(uid, autodoc);
            return;
        }

        EntityUid? toRemove = null;
        foreach (var obj in embedded.EmbeddedObjects)
        {
            toRemove = obj;
            break;
        }

        if (toRemove != null)
        {
            embedded.EmbeddedObjects.Remove(toRemove.Value);
            _xform.SetCoordinates(toRemove.Value, Transform(uid).Coordinates);

            _popup.PopupEntity(Loc.GetString("rmc-autodoc-shrapnel-removed"), uid);
            _audio.PlayPvs(autodoc.SurgeryStepSound, uid);

            if (embedded.EmbeddedObjects.Count > 0)
            {
                autodoc.SurgeryCompleteAt = _timing.CurTime + autodoc.ShrapnelRemovalTime;
                return;
            }
        }

        autodoc.RemoveShrapnel = false;
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
        var hasBlood = false;
        FixedPoint2 bloodLevel = 0;
        var bloodPercent = 0f;
        var pulse = 0;
        FixedPoint2 totalReagents = 0;
        var hasLarva = false;
        var hasOpenIncisions = false;
        var hasShrapnel = false;
        var surgeryProgressTime = 0f;

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

                bruteLoss = damageable.DamagePerGroup.GetValueOrDefault("Brute").Float();
                burnLoss = damageable.DamagePerGroup.GetValueOrDefault("Burn").Float();
                toxinLoss = damageable.DamagePerGroup.GetValueOrDefault("Toxin").Float();
                oxyLoss = damageable.DamagePerGroup.GetValueOrDefault("Airloss").Float();
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

            hasLarva = HasLarva(occupant.Value);
            hasOpenIncisions = HasOpenIncisions(occupant.Value);
            hasShrapnel = HasShrapnel(occupant.Value);

            if (autodoc.Comp.CurrentSurgeryType != AutodocSurgeryType.None && autodoc.Comp.SurgeryCompleteAt > _timing.CurTime)
            {
                var totalTime = autodoc.Comp.CurrentSurgeryType switch
                {
                    AutodocSurgeryType.LarvaExtraction => autodoc.Comp.LarvaExtractionTime,
                    AutodocSurgeryType.CloseIncision => autodoc.Comp.CloseIncisionTime,
                    AutodocSurgeryType.ShrapnelRemoval => autodoc.Comp.ShrapnelRemovalTime,
                    _ => TimeSpan.FromSeconds(1)
                };
                var remaining = autodoc.Comp.SurgeryCompleteAt - _timing.CurTime;
                surgeryProgressTime = 1f - (float)(remaining.TotalSeconds / totalTime.TotalSeconds);
                surgeryProgressTime = Math.Clamp(surgeryProgressTime, 0f, 1f);
            }
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
            autodoc.Comp.CurrentSurgeryType,
            surgeryProgressTime,
            autodoc.Comp.HealingBrute,
            autodoc.Comp.HealingBurn,
            autodoc.Comp.HealingToxin,
            autodoc.Comp.BloodTransfusion,
            autodoc.Comp.Filtering,
            totalReagents,
            autodoc.Comp.RemoveLarva,
            autodoc.Comp.CloseIncisions,
            autodoc.Comp.RemoveShrapnel,
            hasLarva,
            hasOpenIncisions,
            hasShrapnel);

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
                continue;
            }

            var anyTreatmentRemaining = false;
            if (autodoc.HealingBrute)
            {
                if (TryComp<DamageableComponent>(occupant, out var damageable) && damageable.DamagePerGroup.GetValueOrDefault("Brute") > 0)
                {
                    var healing = _rmcDamageable.DistributeHealingCached(occupant, "Brute", autodoc.BruteHealAmount);
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
                if (TryComp<DamageableComponent>(occupant, out var damageable) && damageable.DamagePerGroup.GetValueOrDefault("Burn") > 0)
                {
                    var healing = _rmcDamageable.DistributeHealingCached(occupant, "Burn", autodoc.BurnHealAmount);
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
                if (TryComp<DamageableComponent>(occupant, out var damageable) && damageable.DamagePerGroup.GetValueOrDefault("Toxin") > 0)
                {
                    var healing = _rmcDamageable.DistributeHealingCached(occupant, "Toxin", autodoc.ToxinHealAmount);
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
                _rmcBloodstream.RemoveBloodstreamToxins(occupant, autodoc.DialysisAmount);
                if (_rmcBloodstream.TryGetChemicalSolution(occupant, out _, out var chemSol))
                {
                    var hasToxins = false;
                    foreach (var toxinContent in chemSol.Contents)
                    {
                        if (toxinContent.Quantity <= 0)
                            continue;
                        hasToxins = true;
                        break;
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

            if (autodoc.CurrentSurgeryType != AutodocSurgeryType.None)
            {
                anyTreatmentRemaining = true;
                if (time >= autodoc.SurgeryCompleteAt)
                {
                    switch (autodoc.CurrentSurgeryType)
                    {
                        case AutodocSurgeryType.LarvaExtraction:
                            PerformLarvaExtraction(uid, autodoc, occupant);
                            break;
                        case AutodocSurgeryType.CloseIncision:
                            PerformCloseIncisions(uid, autodoc, occupant);
                            break;
                        case AutodocSurgeryType.ShrapnelRemoval:
                            PerformShrapnelRemoval(uid, autodoc, occupant);
                            break;
                    }
                }
            }
            else
            {
                if (autodoc.RemoveLarva && HasLarva(occupant))
                {
                    autodoc.CurrentSurgeryType = AutodocSurgeryType.LarvaExtraction;
                    autodoc.SurgeryCompleteAt = time + autodoc.LarvaExtractionTime;
                    anyTreatmentRemaining = true;
                    _popup.PopupEntity(Loc.GetString("rmc-autodoc-larva-starting"), uid);
                    Dirty(uid, autodoc);
                }
                else if (autodoc.CloseIncisions && HasOpenIncisions(occupant))
                {
                    autodoc.CurrentSurgeryType = AutodocSurgeryType.CloseIncision;
                    autodoc.SurgeryCompleteAt = time + autodoc.CloseIncisionTime;
                    anyTreatmentRemaining = true;
                    _popup.PopupEntity(Loc.GetString("rmc-autodoc-incisions-starting"), uid);
                    Dirty(uid, autodoc);
                }
                else if (autodoc.RemoveShrapnel && HasShrapnel(occupant))
                {
                    autodoc.CurrentSurgeryType = AutodocSurgeryType.ShrapnelRemoval;
                    autodoc.SurgeryCompleteAt = time + autodoc.ShrapnelRemovalTime;
                    anyTreatmentRemaining = true;
                    _popup.PopupEntity(Loc.GetString("rmc-autodoc-shrapnel-starting"), uid);
                    Dirty(uid, autodoc);
                }
                else
                {
                    if (autodoc.RemoveLarva && !HasLarva(occupant))
                        autodoc.RemoveLarva = false;
                    if (autodoc.CloseIncisions && !HasOpenIncisions(occupant))
                        autodoc.CloseIncisions = false;
                    if (autodoc.RemoveShrapnel && !HasShrapnel(occupant))
                        autodoc.RemoveShrapnel = false;
                }
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
