using Content.Client._RMC14.Medical.Scanner;
using Content.Shared._RMC14.Medical.BodyScanner;
using Robust.Client.Player;

namespace Content.Client._RMC14.Medical.BodyScanner;

public sealed class BodyScannerSystem : SharedBodyScannerSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    [ViewVariables]
    private HealthScannerWindow? _scanWindow;
    private HealthScannerUiData? _scanUiData;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<BodyScannerConsoleScanEvent>(OnBodyScannerConsoleScan);
    }

    private void OnBodyScannerConsoleScan(BodyScannerConsoleScanEvent ev)
    {
        _scanUiData ??= new HealthScannerUiData(EntityManager, _player);

        if (_scanWindow is { IsOpen: true })
            _scanWindow.Close();

        _scanWindow = new HealthScannerWindow();
        _scanWindow.Title = Loc.GetString("rmc-health-analyzer-title");
        _scanUiData.PopulateHealthScan(_scanWindow, ev.ScanState);
    }
}
