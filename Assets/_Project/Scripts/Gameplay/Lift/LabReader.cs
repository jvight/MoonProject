using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// The lab's reader (docs/features/M3-19 §5; ruling 14: a machine 07 feeds with its beam). With the turntable at
    /// rest facing it and a blueprint tube riding on 07's back, 07's beam feeds the tube into the slot at lens height;
    /// once it is in, the reader has taken it (<see cref="BlueprintDelivered"/>, saved) and decodes it while its reel
    /// spins and its paper tape runs out toward the board (<see cref="ReaderDecode"/>), then the kit is decoded
    /// (<see cref="FindDecoded"/>, saved) and the hand-off begins (<see cref="DecodeHandOff"/>): the lamp pulse runs
    /// down to the bay, where Kenji's plotter draws the sheet. The decode is the machine's: it finishes whether 07
    /// stays, turns away or rides down. One tube at a time. Nothing allocates per frame.
    /// </summary>
    internal sealed class LabReader
    {
        private const float FullTurn = 360f;

        private readonly Transform _slot;
        private readonly Transform _reel;
        private readonly Transform _tape;
        private readonly Vector3 _tapeScale;
        private readonly Blueprints _blueprints;
        private readonly ProgressionTuning _tuning;
        private readonly EventBus _events;
        private readonly ISaveService _save;
        private readonly ReaderDecode _decode;
        private readonly RepairBeam _beam;

        /// <param name="slot">Lab/Station_Reader/Slot: where the tube goes in, at 07's lens height.</param>
        /// <param name="reel">Lab/Station_Reader/Reel: spins about its local Z while it decodes.</param>
        /// <param name="tape">Lab/Station_Reader/Tape: its local X scale 0..1 is how much has run out.</param>
        public LabReader(Transform parent, Transform slot, Transform reel, Transform tape, Material beam,
            Blueprints blueprints, ProgressionTuning tuning, EventBus events, ISaveService save)
        {
            _slot = slot != null ? slot : throw new ArgumentNullException(nameof(slot));
            _reel = reel != null ? reel : throw new ArgumentNullException(nameof(reel));
            _tape = tape != null ? tape : throw new ArgumentNullException(nameof(tape));
            _tapeScale = tape.localScale;
            _blueprints = blueprints ?? throw new ArgumentNullException(nameof(blueprints));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _decode = new ReaderDecode(tuning.ReaderFeedSeconds, tuning.ReaderDecodeSeconds);
            _beam = new RepairBeam("ReaderFeed", parent, beam, 0f, 0f);
        }

        public ReaderPhase Phase => _decode.Phase;

        /// <summary>07's beam is feeding a tube in: 07 stays until it is in.</summary>
        public bool Feeding => _decode.Phase == ReaderPhase.Feeding;

        /// <summary>A tube rides on 07's back and the reader is free to take it.</summary>
        public bool Wants => _blueprints.IsCarrying && _decode.Phase == ReaderPhase.Idle;

        /// <summary>How far the decode has come, 0..1.</summary>
        public float DecodeProgress => _decode.DecodeProgress;

        public Vector3 SlotPosition => _slot.position;

        /// <summary>A loaded game: a tube the reader had taken decodes again from the start.</summary>
        public void Restore()
        {
            string reading = _blueprints.Reading;
            if (reading != null && _decode.Phase == ReaderPhase.Idle)
            {
                _decode.Resume(reading);
            }
        }

        /// <param name="faced">The turntable rests facing the reader with 07 on it.</param>
        /// <param name="eye">07's lens, where the feeding beam leaves from.</param>
        public void Step(bool faced, Vector3 eye, float now, float deltaTime)
        {
            if (faced && Wants)
            {
                _decode.Feed(_blueprints.NextCarried);
            }

            switch (_decode.Step(deltaTime))
            {
                case ReaderStep.Taken:
                    _blueprints.Take();
                    _save.SaveNow();
                    break;
                case ReaderStep.Decoded:
                    _blueprints.Decode(_decode.Kit);
                    _events.Publish(new DecodeHandOff(_decode.Kit, _tuning.HandOffSeconds));
                    _save.SaveNow();
                    break;
            }

            _beam.Step(Feeding, true, eye, _slot.position, now, deltaTime);
            bool decoding = _decode.Phase == ReaderPhase.Decoding;
            if (decoding)
            {
                _reel.localRotation *= Quaternion.Euler(0f, 0f, _tuning.ReelTurnsPerSecond * FullTurn * deltaTime);
            }

            float tape = decoding ? _decode.DecodeProgress : _blueprints.AnyDecoded ? 1f : 0f;
            _tape.localScale = new Vector3(tape, _tapeScale.y, _tapeScale.z);
        }
    }
}
