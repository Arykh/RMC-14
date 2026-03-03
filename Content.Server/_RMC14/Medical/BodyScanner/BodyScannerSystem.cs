using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Medical.BodyScanner;
using Content.Shared._RMC14.Medical.HUD;
using Content.Shared._RMC14.Medical.Scanner;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.Temperature;
using Content.Shared.FixedPoint;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.BodyScanner;

public sealed class BodyScannerSystem : SharedBodyScannerSystem
{
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly RMCPulseSystem _rmcPulse = default!;
    [Dependency] private readonly SharedRMCTemperatureSystem _rmcTemperature = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyScannerConsoleComponent, AfterActivatableUIOpenEvent>(OnConsoleUIOpened);
        SubscribeLocalEvent<BodyScannerConsoleComponent, OpenChangeHolocardUIEvent>(OnConsoleOpenChangeHolocard);
    }

    private void OnConsoleUIOpened(Entity<BodyScannerConsoleComponent> console, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(console);
    }

    private void OnConsoleOpenChangeHolocard(Entity<BodyScannerConsoleComponent> console, ref OpenChangeHolocardUIEvent args)
    {
        var localOwner = GetEntity(args.Owner);
        var localTarget = GetEntity(args.Target);
        _ui.OpenUi(localTarget, HolocardChangeUIKey.Key, localOwner);
    }

    private void UpdateUI(Entity<BodyScannerConsoleComponent> console)
    {
        if (!_ui.IsUiOpen(console.Owner, HealthScannerUIKey.Key))
            return;

        if (!TryGetLinkedScanner(console, out var scanner))
            return;

        if (scanner.Comp.Occupant is not { } target)
        {
            _ui.SetUiState(
                console.Owner,
                HealthScannerUIKey.Key,
                new HealthScannerBuiState(NetEntity.Invalid, 0, 0, null, string.Empty, null, false));
            return;
        }

        if (TerminatingOrDeleted(target))
            return;

        FixedPoint2 blood = 0;
        FixedPoint2 maxBlood = 0;
        if (_rmcBloodstream.TryGetBloodSolution(target, out var bloodstream))
        {
            blood = bloodstream.Volume;
            maxBlood = bloodstream.MaxVolume;
        }

        _rmcBloodstream.TryGetChemicalSolution(target, out _, out var chemicals);
        _rmcTemperature.TryGetCurrentTemperature(target, out var temperature);

        var pulse = _rmcPulse.TryGetPulseReading(target, true, out _);
        var bleeding = _rmcBloodstream.IsBleeding(target);

        var state = new HealthScannerBuiState(
            GetNetEntity(target),
            blood,
            maxBlood,
            temperature,
            pulse,
            chemicals,
            bleeding,
            HealthScanDetailLevel.BodyScan);

        _ui.SetUiState(console.Owner, HealthScannerUIKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var consoles = EntityQueryEnumerator<BodyScannerConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var console))
        {
            if (!_ui.IsUiOpen(uid, HealthScannerUIKey.Key))
                continue;

            if (time < console.UpdateAt)
                continue;

            console.UpdateAt = time + console.UpdateCooldown;
            UpdateUI((uid, console));
        }
    }

    private bool TryGetLinkedScanner(Entity<BodyScannerConsoleComponent> console, out Entity<BodyScannerComponent> scanner)
    {
        scanner = default;
        if (console.Comp.LinkedBodyScanner is not { } linkedId || !TryComp(linkedId, out BodyScannerComponent? comp))
        {
            return false;
        }

        scanner = (linkedId, comp);
        return true;
    }
}
