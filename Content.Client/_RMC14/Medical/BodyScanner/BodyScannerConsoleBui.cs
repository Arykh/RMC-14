using Content.Shared._RMC14.Medical.BodyScanner;
using Content.Shared._RMC14.Medical.Scanner;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Medical.BodyScanner;

[UsedImplicitly]
public sealed class BodyScannerConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private BodyScannerConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BodyScannerConsoleWindow>();
        _window.Title = Loc.GetString("rmc-body-scanner-window-title");
        _window.SetBui(this);

        if (State is HealthScannerBuiState state)
            _window.UpdateState(state);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is HealthScannerBuiState scannerState)
            _window?.UpdateState(scannerState);
    }

    public void Eject()
    {
        SendMessage(new BodyScannerEjectBuiMsg());
    }
}
