using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

/// <summary>
///     Marks a locked entity storage (e.g. locker/cabinet) that blocks all manual interaction until hijack occurs.
///     The storage automatically unlocks and opens.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCStorageSystem))]
public sealed partial class RMCLockerOnlyOpenOnHijackComponent : Component
{
    /// <summary>
    ///     True if the hijack event has already fired for this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Hijacked;
}
