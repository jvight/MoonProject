using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Everything the UI reads or drives, resolved once from the GameContext (or handed in by a test). The UI never
    /// mutates game state except through these services.
    /// </summary>
    internal sealed class UiServices
    {
        private readonly GameContext _context;
        private IDatabank _databank;
        private ISupplyPurposes _purposes;
        private IAnswerStatus _answer;
        private ICallSource _call;

        public UiServices(EventBus events, InputReader input, IViewCamera view, IRoverState rover, IAudioSettings audio,
            ILookSettings look, IFeelSettings feel, ISaveService save, IMaterialStock materials,
            IInteractionHints hints, ISalvageStatus salvage, IGameFlow flow, ISettingsFile settings,
            IRoverBattery battery, IBeamStatus beam, ICargoHold hold, IMaterialCatalog catalog,
            IRoverSystems systems, IRoverFaceScreen faceScreen = null, BuildField buildField = null,
            ISunlight sun = null, IWeldEnergy weldEnergy = null, IDatabank databank = null, GameContext context = null)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            View = view ?? throw new ArgumentNullException(nameof(view));
            Rover = rover ?? throw new ArgumentNullException(nameof(rover));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            Look = look ?? throw new ArgumentNullException(nameof(look));
            Feel = feel ?? throw new ArgumentNullException(nameof(feel));
            Save = save ?? throw new ArgumentNullException(nameof(save));
            Materials = materials ?? throw new ArgumentNullException(nameof(materials));
            Hints = hints ?? throw new ArgumentNullException(nameof(hints));
            Salvage = salvage ?? throw new ArgumentNullException(nameof(salvage));
            Flow = flow ?? throw new ArgumentNullException(nameof(flow));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Battery = battery ?? throw new ArgumentNullException(nameof(battery));
            Beam = beam ?? throw new ArgumentNullException(nameof(beam));
            Hold = hold ?? throw new ArgumentNullException(nameof(hold));
            Systems = systems ?? throw new ArgumentNullException(nameof(systems));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            FaceScreen = faceScreen;
            BuildField = buildField;
            Sun = sun;
            WeldEnergy = weldEnergy;
            _databank = databank;
            _context = context;
        }

        public EventBus Events { get; }

        public InputReader Input { get; }

        public IViewCamera View { get; }

        public IRoverState Rover { get; }

        public IAudioSettings Audio { get; }

        public ILookSettings Look { get; }

        public IFeelSettings Feel { get; }

        public ISaveService Save { get; }

        public IMaterialStock Materials { get; }

        public IInteractionHints Hints { get; }

        public ISalvageStatus Salvage { get; }

        /// <summary>07's hold: the cargo screen's grid.</summary>
        public ICargoHold Hold { get; }

        /// <summary>07's systems: the boot readout's lines and the igniter fit on the socket screen.</summary>
        public IRoverSystems Systems { get; }

        /// <summary>What each material is called (the pickup feed and the cargo screen).</summary>
        public IMaterialCatalog Catalog { get; }

        /// <summary>07's CRT face screen in the world (optional in isolated UI test rigs).</summary>
        public IRoverFaceScreen FaceScreen { get; }

        /// <summary>07's freeform base building coordinator (0.5.7 slice 6, BUILD-MODE).</summary>
        public BuildField BuildField { get; }

        public IGameFlow Flow { get; }

        public ISettingsFile Settings { get; }

        /// <summary>07's battery (docs/features/M3-20): the HUD arc reads it.</summary>
        public IRoverBattery Battery { get; }

        /// <summary>Whether 07's beam is at work: the HUD arc hides meanwhile.</summary>
        public IBeamStatus Beam { get; }

        /// <summary>The sky: the Sun's arc reads it (optional in isolated UI test rigs).</summary>
        public ISunlight Sun { get; }

        /// <summary>What a weld takes from 07's battery: the build menu says it (optional in isolated test rigs).</summary>
        public IWeldEnergy WeldEnergy { get; }

        /// <summary>
        /// Where the answer to the call stands (the hold ring at the mast reads it), from the GameContext, looked up
        /// until Gameplay has registered it (false meanwhile, as in isolated test rigs).
        /// </summary>
        public bool TryGetAnswer(out IAnswerStatus answer)
        {
            if (_answer == null && _context != null)
            {
                _context.TryGet(out _answer);
            }

            answer = _answer;
            return answer != null;
        }

        /// <summary>
        /// What each of the opening's sources is for (the pickup notice's purpose), from the GameContext, looked up on
        /// the first pickup (Gameplay registers it; false while nothing has, as in isolated test rigs).
        /// </summary>
        public bool TryGetSupplyPurposes(out ISupplyPurposes purposes)
        {
            if (_purposes == null && _context != null)
            {
                _context.TryGet(out _purposes);
            }

            purposes = _purposes;
            return purposes != null;
        }

        /// <summary>
        /// 07's scan archive: the one handed in, else the GameContext's, looked up when the databank page first opens
        /// (Gameplay registers it; false while nothing has).
        /// </summary>
        public bool TryGetDatabank(out IDatabank databank)
        {
            if (_databank == null && _context != null)
            {
                _context.TryGet(out _databank);
            }

            databank = _databank;
            return databank != null;
        }

        /// <summary>
        /// The call from the sky, from the GameContext, looked up when the call is first heard (Gameplay registers it;
        /// false while nothing has).
        /// </summary>
        public bool TryGetCall(out ICallSource call)
        {
            if (_call == null && _context != null)
            {
                _context.TryGet(out _call);
            }

            call = _call;
            return call != null;
        }

        /// <summary>
        /// Resolves every service; a missing one throws (a system is missing from the bootstrap order).
        /// </summary>
        public static UiServices From(GameContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.TryGet(out IRoverFaceScreen faceScreen);
            context.TryGet(out BuildField buildField);
            context.TryGet(out ISunlight sun);
            IWeldEnergy weldEnergy = buildField != null ? new BatteryWeldEnergy(context.Get<IBuildEnergy>()) : null;

            return new UiServices(context.Events, context.Input, context.Get<IViewCamera>(), context.Get<IRoverState>(),
                context.Get<IAudioSettings>(), context.Get<ILookSettings>(), context.Get<IFeelSettings>(),
                context.Get<ISaveService>(),
                context.Get<IMaterialStock>(), context.Get<IInteractionHints>(), context.Get<ISalvageStatus>(),
                context.Get<IGameFlow>(), context.Get<ISettingsFile>(), context.Get<IRoverBattery>(),
                context.Get<IBeamStatus>(), context.Get<ICargoHold>(), context.Get<IMaterialCatalog>(),
                context.Get<IRoverSystems>(), faceScreen, buildField, sun, weldEnergy, context: context);
        }
    }
}
