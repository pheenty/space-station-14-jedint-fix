using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Stories.Garrote;

namespace Content.Server.Stories.Garrote;

public sealed class GarroteSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GarroteComponent, AfterInteractEvent>(OnGarroteAttempt);
        SubscribeLocalEvent<GarroteComponent, GarroteDoAfterEvent>(OnGarroteDoAfter);
    }

    private void OnGarroteAttempt(EntityUid uid, GarroteComponent comp, ref AfterInteractEvent args)
    {
        if (args.User == args.Target
        || !args.CanReach
        || !HasComp<BodyComponent>(args.Target)
        || !HasComp<DamageableComponent>(args.Target)
        || !TryComp<MobStateComponent>(args.Target, out var mobstate)) return;

        if (TryComp<WieldableComponent>(uid, out var wieldable) && !wieldable.Wielded)
        {
            var message = Loc.GetString("wieldable-component-requires", ("item", uid));
            _popupSystem.PopupEntity(message, uid, args.User);
            return;
        }

        if (!(mobstate.CurrentState == MobState.Alive && HasComp<RespiratorComponent>(args.Target)))
        {
            var message = Loc.GetString("garrote-component-doesnt-breath", ("target", args.Target));
            _popupSystem.PopupEntity(message, args.Target.Value, args.User);
            return;
        }

        if (!IsBehind(args.User, args.Target.Value, comp.MinAngleFromFace) && _actionBlocker.CanInteract(args.Target.Value, null))
        {
            var message = Loc.GetString("garrote-component-must-be-behind", ("target", args.Target));
            _popupSystem.PopupEntity(message, args.Target.Value, args.User);
            return;
        }

        var messagetarget = Loc.GetString("garrote-component-started-target", ("user", args.User));
        _popupSystem.PopupEntity(messagetarget, args.User, args.Target.Value, PopupType.LargeCaution);

        var messageothers = Loc.GetString("garrote-component-started-others", ("user", args.User), ("target", args.Target));
        _popupSystem.PopupEntity(messageothers, args.User, Filter.PvsExcept(args.Target.Value), true, PopupType.MediumCaution);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, comp.DoAfterTime, new GarroteDoAfterEvent(), uid, target: args.Target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DuplicateCondition = DuplicateConditions.SameTool
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs)) return;

        ProtoId<EmotePrototype> emote = "Cough";
        _chatSystem.TryEmoteWithChat(args.Target.Value, emote, ChatTransmitRange.HideChat, ignoreActionBlocker: true);

        _stun.TryStun(args.Target.Value, 2*comp.DoAfterTime, true); // multiplying time by 2 to prevent mispredictons
        _statusEffect.TryAddStatusEffect<MutedComponent>(args.Target.Value, "Muted", 2*comp.DoAfterTime, true);
    }

    private void OnGarroteDoAfter(EntityUid uid, GarroteComponent comp, GarroteDoAfterEvent args)
    {
        if (args.Target == null
        || !TryComp<DamageableComponent>(args.Target, out var damageable)
        || !TryComp<RespiratorComponent>(args.Target, out var respirator)
        || !TryComp<MobStateComponent>(args.Target, out var mobstate))
            return;

        if (args.Cancelled || mobstate.CurrentState != MobState.Alive) return;

        DamageSpecifier damage = new(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), comp.Damage); // TODO: unhardcode asphyxiation?
        _damageable.TryChangeDamage(args.Target, damage, false, origin: args.User);

        var saturationDelta = respirator.MinSaturation - respirator.Saturation;
        _respirator.UpdateSaturation(args.Target.Value, saturationDelta, respirator);

        _stun.TryStun(args.Target.Value, 2*comp.DoAfterTime, true);
        _statusEffect.TryAddStatusEffect<MutedComponent>(args.Target.Value, "Muted", 2*comp.DoAfterTime, true);

        args.Repeat = true;
    }

    private bool IsBehind(EntityUid user, EntityUid target, float minAngleFromFace)
    {
        if (!TryComp(target, out TransformComponent? targetTransform)) return false;
        var targetLocalCardinal = targetTransform.LocalRotation.GetCardinalDir().ToAngle();
        var cardinalDifference = targetLocalCardinal - targetTransform.LocalRotation;
        var targetRotation = _transform.GetWorldRotation(target);
        var targetRotationCardinal = targetRotation + cardinalDifference;
        var userRelativeRotation = (_transform.GetWorldPosition(user) - _transform.GetWorldPosition(target)).Normalized().ToWorldAngle().FlipPositive();
        var targetRotationDegrees = targetRotationCardinal.Opposite().Reduced().FlipPositive().Degrees;
        var userRotationDegrees = userRelativeRotation.Reduced().FlipPositive().Degrees;
        var angleFromFace = Math.Abs(Math.Abs(targetRotationDegrees - userRotationDegrees) - 180);
        return angleFromFace >= minAngleFromFace;
    }
}
