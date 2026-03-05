using Content.Shared._RMC14.Medical.Scanner;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.BodyScanner;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBodyScannerSystem))]
public sealed partial class BodyScannerConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedBodyScanner;

    [DataField]
    public TimeSpan UpdateAt;

    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1);

    // TODO RMC14 Medical records?
    [DataField]
    public HealthScannerBuiState? LastScanSnapshot;
}
