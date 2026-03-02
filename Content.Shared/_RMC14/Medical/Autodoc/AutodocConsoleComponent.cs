using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Autodoc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAutodocSystem))]
public sealed partial class AutodocConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedAutodoc;

    [DataField]
    public TimeSpan UpdateAt;

    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> SkillRequired = new() { ["RMCSkillSurgery"] = 1 };

    /// <summary>
    /// Set of research upgrade tiers currently installed on this console.
    /// Each tier independently unlocks a specific surgery type.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<AutodocUpgradeTier> InstalledUpgrades = new();
}
