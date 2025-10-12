using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the entity is initialized.
/// The user is null.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(RaiseAfterAutoHandleState = true)]
public sealed partial class TriggerOnSpawnComponent : BaseTriggerOnXComponent;
