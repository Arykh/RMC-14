using System.Linq;
using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Medical.Sleeper;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
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

namespace Content.Server._RMC14.Medical.Sleeper;

public sealed class SleeperSystem : SharedSleeperSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly RMCPulseSystem _rmcPulse = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedRMCTemperatureSystem _rmcTemperature = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private readonly List<ProtoId<ReagentPrototype>> _reagentRemovalBuffer = [];

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SleeperConsoleComponent, AfterActivatableUIOpenEvent>(OnConsoleUIOpened);
        SubscribeLocalEvent<SleeperConsoleComponent, SleeperInjectChemicalBuiMsg>(OnConsoleInjectChemical);
        SubscribeLocalEvent<SleeperConsoleComponent, SleeperToggleFilterBuiMsg>(OnConsoleToggleFilter);
        SubscribeLocalEvent<SleeperConsoleComponent, SleeperEjectBuiMsg>(OnConsoleEject);
        SubscribeLocalEvent<SleeperConsoleComponent, SleeperAutoEjectDeadBuiMsg>(OnConsoleAutoEjectDead);
    }

    private void OnConsoleUIOpened(Entity<SleeperConsoleComponent> console, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(console);
    }

    private void OnConsoleInjectChemical(Entity<SleeperConsoleComponent> console, ref SleeperInjectChemicalBuiMsg args)
    {
        if (console.Comp.LinkedSleeper is not { } sleeperId || !TryComp(sleeperId, out SleeperComponent? sleeper))
            return;

        if (sleeper.Occupant is not { } occupant)
            return;

        if (_mobState.IsDead(occupant))
            return;

        if (!sleeper.InjectionAmounts.Contains(args.Amount))
            return;

        var availableChemicals = console.Comp.IsUpgraded ? sleeper.UpgradedChemicals : sleeper.AvailableChemicals;
        var emergencyChemicals = console.Comp.IsUpgraded ? sleeper.UpgradedEmergencyChemicals : sleeper.EmergencyChemicals;

        var isAvailable = availableChemicals.Contains(args.Chemical);
        var isEmergency = emergencyChemicals.Contains(args.Chemical);
        if (!isAvailable && !isEmergency)
            return;

        if (isEmergency && !isAvailable)
        {
            if (!TryComp<DamageableComponent>(occupant, out var damageable) || damageable.TotalDamage <= sleeper.PercentHealthThreshold)
                return;
        }

        if (!_rmcBloodstream.TryGetChemicalSolution(occupant, out var chemSolEnt, out var chemSol))
            return;

        var reagent = new ReagentId(args.Chemical, null);
        var currentAmount = chemSol.GetReagentQuantity(reagent);
        if (currentAmount + args.Amount > sleeper.MaxChemical)
            return;

        _solution.TryAddReagent(chemSolEnt, args.Chemical, args.Amount);

        UpdateUI(console);
    }

    private void OnConsoleToggleFilter(Entity<SleeperConsoleComponent> console, ref SleeperToggleFilterBuiMsg args)
    {
        if (console.Comp.LinkedSleeper is not { } sleeperId || !TryComp(sleeperId, out SleeperComponent? sleeper))
            return;

        ToggleDialysis((sleeperId, sleeper));
        UpdateUI(console);
    }

    private void OnConsoleEject(Entity<SleeperConsoleComponent> console, ref SleeperEjectBuiMsg args)
    {
        if (console.Comp.LinkedSleeper is not { } sleeperId || !TryComp(sleeperId, out SleeperComponent? sleeper))
            return;

        if (sleeper.Occupant is { } occupant)
            EjectOccupant((sleeperId, sleeper), occupant);

        UpdateUI(console);
    }

    private void OnConsoleAutoEjectDead(Entity<SleeperConsoleComponent> console, ref SleeperAutoEjectDeadBuiMsg args)
    {
        if (console.Comp.LinkedSleeper is not { } sleeperId || !TryComp(sleeperId, out SleeperComponent? sleeper))
            return;

        sleeper.AutoEjectDead = args.Enabled;
        Dirty(sleeperId, sleeper);

        if (args.Enabled && sleeper.Occupant is { } occupant && _mobState.IsDead(occupant))
        {
            _audio.PlayPvs(sleeper.AutoEjectDeadSound, sleeperId);
            EjectOccupant((sleeperId, sleeper), occupant);
        }

        UpdateUI(console);
    }

    private void UpdateUI(Entity<SleeperConsoleComponent> console)
    {
        if (!_ui.IsUiOpen(console.Owner, SleeperUIKey.Key))
            return;

        // If no sleeper is connected, the UI shouldn't be open (handled by ActivatableUIOpenAttemptEvent)
        if (console.Comp.LinkedSleeper is not { } sleeperId || !TryComp(sleeperId, out SleeperComponent? sleeper))
            return;

        var occupant = sleeper.Occupant;
        NetEntity? netOccupant = null;
        string? occupantName = null;
        var occupantState = SleeperOccupantMobState.None;
        var health = 0f;
        var maxHealth = 0f;
        FixedPoint2 totalDamage = 0;
        var bruteLoss = 0f;
        var burnLoss = 0f;
        var toxinLoss = 0f;
        var oxyLoss = 0f;
        FixedPoint2 bloodLevel = 0;
        var bloodPercent = 0f;
        var pulse = string.Empty;
        var bodyTemp = 0f;
        var emergencyHealthThreshold = 0f;
        FixedPoint2 totalReagents = 0;
        Solution? cachedChemSol = null;

        if (occupant != null)
        {
            if (TryComp<DamageableComponent>(occupant, out var damageable))
            {
                netOccupant = GetNetEntity(occupant.Value);
                occupantName = Identity.Name(occupant.Value, EntityManager);

                if (_mobState.IsDead(occupant.Value))
                    occupantState = SleeperOccupantMobState.Dead;
                else if (_mobState.IsCritical(occupant.Value))
                    occupantState = SleeperOccupantMobState.Critical;
                else
                    occupantState = SleeperOccupantMobState.Alive;

                totalDamage = damageable.TotalDamage;

                if (_mobThreshold.TryGetThresholdForState(occupant.Value, MobState.Critical, out var critThreshold) &&
                    _mobThreshold.TryGetThresholdForState(occupant.Value, MobState.Dead, out var deadThreshold))
                {
                    maxHealth = (float)critThreshold;
                    health = (float)(critThreshold - totalDamage);
                    emergencyHealthThreshold = (float)(deadThreshold - deadThreshold * sleeper.PercentHealthThreshold);
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

            _rmcTemperature.TryGetCurrentTemperature(occupant.Value, out bodyTemp);

            // Cache chemical solution to avoid repeated lookups in the loop
            if (_rmcBloodstream.TryGetChemicalSolution(occupant.Value, out _, out cachedChemSol))
                totalReagents = cachedChemSol.Volume;
        }

        var isUpgraded = console.Comp.IsUpgraded;
        var availableChemicals = isUpgraded ? sleeper.UpgradedChemicals : sleeper.AvailableChemicals;
        var emergencyChemicals = isUpgraded ? sleeper.UpgradedEmergencyChemicals : sleeper.EmergencyChemicals;

        var isEmergency = totalDamage >= emergencyHealthThreshold;
        var totalChemCount = availableChemicals.Length;
        if (isEmergency)
            totalChemCount += emergencyChemicals.Length;

        // Build chemical list - always show AvailableChemicals
        var chemicals = new List<SleeperChemicalData>(totalChemCount);
        foreach (var chemId in availableChemicals)
        {
            AddChemicalToList(chemicals, chemId, occupant, cachedChemSol, true);
        }

        if (isEmergency)
        {
            foreach (var chemId in emergencyChemicals)
            {
                // Skip any duplicates in EmergencyChemicals that are already in AvailableChemicals
                if (availableChemicals.Contains(chemId))
                    continue;

                AddChemicalToList(chemicals, chemId, occupant, cachedChemSol, true);
            }
        }

        var state = new SleeperBuiState(
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
            bodyTemp,
            sleeper.IsFiltering,
            totalReagents,
            sleeper.DialysisStartedReagentVolume,
            sleeper.AutoEjectDead,
            sleeper.MaxChemical,
            emergencyHealthThreshold,
            chemicals.ToArray(),
            sleeper.InjectionAmounts);

        _ui.SetUiState(console.Owner, SleeperUIKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var consoles = EntityQueryEnumerator<SleeperConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var console))
        {
            if (!_ui.IsUiOpen(uid, SleeperUIKey.Key))
                continue;

            if (time < console.UpdateAt)
                continue;

            console.UpdateAt = time + console.UpdateCooldown;
            UpdateUI((uid, console));
        }

        var sleepers = EntityQueryEnumerator<SleeperComponent>();
        while (sleepers.MoveNext(out var uid, out var sleeper))
        {
            if (sleeper.Occupant == null)
                continue;

            if (sleeper.AutoEjectDead && _mobState.IsDead(sleeper.Occupant.Value))
            {
                _audio.PlayPvs(sleeper.AutoEjectDeadSound, uid);
                sleeper.AutoEjectDead = false;
                Dirty(uid, sleeper);
                EjectOccupant((uid, sleeper), sleeper.Occupant.Value);
                continue;
            }

            if (!sleeper.IsFiltering)
                continue;

            if (time < sleeper.NextDialysisTick)
                continue;

            sleeper.NextDialysisTick = time + sleeper.DialysisTickDelay;

            // Perform dialysis
            if (!_rmcBloodstream.TryGetChemicalSolution(sleeper.Occupant.Value, out var chemSolEnt, out var chemSol))
                continue;

            if (sleeper.DialysisStartedReagentVolume == 0)
            {
                sleeper.DialysisStartedReagentVolume = chemSol.Volume;
                Dirty(uid, sleeper);
            }

            var dialysisAmount = sleeper.DialysisAmount;
            if (sleeper.LinkedConsole is { } linkedConsoleId &&
                TryComp<SleeperConsoleComponent>(linkedConsoleId, out var linkedConsole) &&
                linkedConsole.IsUpgraded)
            {
                dialysisAmount = sleeper.UpgradedDialysisAmount;
            }

            _reagentRemovalBuffer.Clear();
            foreach (var reagentQuantity in chemSol.Contents)
            {
                if (!sleeper.NonTransferableReagents.Contains(reagentQuantity.Reagent.Prototype))
                    _reagentRemovalBuffer.Add(reagentQuantity.Reagent.Prototype);
            }

            foreach (var reagent in _reagentRemovalBuffer)
            {
                _solution.RemoveReagent(chemSolEnt, reagent, dialysisAmount);
            }

            // Check if dialysis is complete
            var hasTransferableReagents = false;
            foreach (var reagentQuantity in chemSol.Contents)
            {
                if (!sleeper.NonTransferableReagents.Contains(reagentQuantity.Reagent.Prototype) && reagentQuantity.Quantity > 0)
                {
                    hasTransferableReagents = true;
                    break;
                }
            }

            if (!hasTransferableReagents)
            {
                sleeper.IsFiltering = false;
                sleeper.DialysisStartedReagentVolume = 0;
                _audio.PlayPvs(sleeper.DialysisCompleteSound, uid);
                Dirty(uid, sleeper);
            }
        }
    }

    private void AddChemicalToList(
        List<SleeperChemicalData> chemicals,
        ProtoId<ReagentPrototype> chemId,
        EntityUid? occupant,
        Solution? cachedChemSol,
        bool injectable)
    {
        if (!_rmcReagent.TryIndex(chemId, out var reagentProto))
            return;

        FixedPoint2 occupantAmount = 0;
        var overdosing = false;
        var odWarning = false;
        if (cachedChemSol != null)
        {
            var reagent = new ReagentId(chemId, null);
            occupantAmount = cachedChemSol.GetReagentQuantity(reagent);

            if (reagentProto.Overdose != null)
            {
                if (occupantAmount >= reagentProto.Overdose)
                    overdosing = true;
                else if (occupantAmount + 10 > reagentProto.Overdose)
                    odWarning = true;
            }
        }

        chemicals.Add(new SleeperChemicalData(
            reagentProto.LocalizedName,
            chemId,
            occupantAmount,
            injectable && occupant != null,
            overdosing,
            odWarning));
    }
}
