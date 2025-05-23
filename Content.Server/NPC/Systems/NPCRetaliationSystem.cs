// SPDX-FileCopyrightText: 2023 Chronophylos <nikolai@chronophylos.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2024 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Ilya246 <57039557+Ilya246@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Server.NPC.Components;
using Content.Server.NPC.Events; // Goobstation
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

/// <summary>
///     Handles NPC which become aggressive after being attacked.
/// </summary>
public sealed class NPCRetaliationSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<NPCRetaliationComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<NPCRetaliationComponent, DisarmedEvent>(OnDisarmed);
    }

    private void OnDamageChanged(Entity<NPCRetaliationComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (args.Origin is not {} origin)
            return;

        TryRetaliate(ent, origin);
    }

    private void OnDisarmed(Entity<NPCRetaliationComponent> ent, ref DisarmedEvent args)
    {
        TryRetaliate(ent, args.Source);
    }

    public bool TryRetaliate(Entity<NPCRetaliationComponent> ent, EntityUid target,
                             bool secondary = false) // Goobstation - whether this should trigger group retaliation
    {
        // don't retaliate against inanimate objects.
        if (!HasComp<MobStateComponent>(target))
            return false;

        // don't retaliate against the same faction
        if (_npcFaction.IsEntityFriendly(ent.Owner, target))
            return false;

        _npcFaction.AggroEntity(ent.Owner, target);
        if (ent.Comp.AttackMemoryLength is {} memoryLength)
            ent.Comp.AttackMemories[target] = _timing.CurTime + memoryLength;

        // Goobstation
        var ev = new NPCRetaliatedEvent(ent, target, secondary);
        RaiseLocalEvent(ent, ev);

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NPCRetaliationComponent, FactionExceptionComponent>();
        while (query.MoveNext(out var uid, out var retaliationComponent, out var factionException))
        {
            // TODO: can probably reuse this allocation and clear it
            foreach (var entity in new ValueList<EntityUid>(retaliationComponent.AttackMemories.Keys))
            {
                if (!TerminatingOrDeleted(entity) && _timing.CurTime < retaliationComponent.AttackMemories[entity])
                    continue;

                _npcFaction.DeAggroEntity((uid, factionException), entity);
                // TODO: should probably remove the AttackMemory, thats the whole point of the ValueList right??
            }
        }
    }
}