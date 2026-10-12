using System;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// 07's abilities as round glass bubbles at the bottom centre (Moonlight Glass, Subnautica's quick slots), and so a
    /// visible record of progression: the tether and the sonar from the start, then the leap, the cradle, the headlamp
    /// and the boost coils, each a dim padlock until earned (<see cref="IRoverAbilities"/>). The one in use (the tether
    /// while towing) or just earned wears the amber ring. When the row shows follows <see cref="AbilitySlotRules"/>.
    /// </summary>
    internal sealed class AbilitySlots
    {
        private const string SlotClass = "ability-slot";
        private const string IconClass = "ability-slot__icon";
        private const string OnClass = "ability-slot--on";
        private const string LockedClass = "ability-slot--locked";

        private readonly IRoverAbilities _abilities;
        private readonly ITetherAim _tether;
        private readonly IRoverState _rover;
        private readonly AbilitySlotRules _rules;
        private readonly Reveal _reveal;
        private readonly AbilityIcon[] _icons =
        {
            AbilityIcon.Tether, AbilityIcon.Listen, AbilityIcon.Leap, AbilityIcon.Cradle, AbilityIcon.Headlamp,
            AbilityIcon.Boost,
        };

        private readonly VisualElement[] _slots;
        private readonly AbilityIconPainter[] _painters;
        private readonly int[] _written;

        public AbilitySlots(UiLayout layout, AbilitySlotsSettings settings, IRoverAbilities abilities,
            ITetherAim tether, IRoverState rover)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _tether = tether ?? throw new ArgumentNullException(nameof(tether));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _rules = new AbilitySlotRules(settings, _icons.Length);
            _reveal = new Reveal(layout.AbilitySlots, settings.Reveal);
            _reveal.Snap(false);
            _slots = new VisualElement[_icons.Length];
            _painters = new AbilityIconPainter[_icons.Length];
            _written = new int[_icons.Length];
            layout.AbilitySlots.Clear();
            for (int i = 0; i < _icons.Length; i++)
            {
                var slot = new VisualElement { pickingMode = PickingMode.Ignore, name = SlotClass + "-" + i };
                slot.AddToClassList(SlotClass);
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList(IconClass);
                slot.Add(icon);
                layout.AbilitySlots.Add(slot);
                _slots[i] = slot;
                _painters[i] = new AbilityIconPainter(icon, AbilityIcon.Locked);
                _written[i] = -1;
            }
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True when slot <paramref name="index"/>'s ability is earned (tests and captures).</summary>
        public bool IsOwned(int index)
        {
            return _rules.IsOwned(index);
        }

        /// <summary>True when slot <paramref name="index"/> wears the amber ring (tests and captures).</summary>
        public bool IsRinged(int index)
        {
            return _rules.IsRinged(index);
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="open">False while the HUD gives way (pause, the title, the map, stargazing).</param>
        public void Tick(float deltaTime, bool open)
        {
            bool towing = _tether.State == TetherAimState.Towing;
            for (int i = 0; i < _icons.Length; i++)
            {
                AbilityIcon icon = _icons[i];
                bool owned = Owned(icon);
                _rules.Slot(i, owned, icon == AbilityIcon.Tether && towing, deltaTime);
                int state = (owned ? 1 : 0) | (_rules.IsRinged(i) ? 2 : 0);
                if (state == _written[i])
                {
                    continue;
                }

                _painters[i].Show(owned ? icon : AbilityIcon.Locked);
                _slots[i].EnableInClassList(LockedClass, !owned);
                _slots[i].EnableInClassList(OnClass, _rules.IsRinged(i));
                _written[i] = state;
            }

            _rules.Finish(_rover.Speed, deltaTime);
            _reveal.Set(open && _rules.Shown);
            _reveal.Tick(deltaTime);
        }

        private bool Owned(AbilityIcon icon)
        {
            switch (icon)
            {
                case AbilityIcon.Leap:
                    return _abilities.Has(RoverAbility.HoverJump);
                case AbilityIcon.Cradle:
                    return _abilities.Has(RoverAbility.CargoCradle);
                case AbilityIcon.Headlamp:
                    return _abilities.Has(RoverAbility.WarmHeadlamp);
                case AbilityIcon.Boost:
                    return _abilities.Has(RoverAbility.BoostCoils);
                default:
                    return true;
            }
        }
    }
}
