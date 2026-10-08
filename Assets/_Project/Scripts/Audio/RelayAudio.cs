using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The relay network's sounds (M3-06), following Gameplay's <see cref="RelayCued"/> beat: the amber part tone
    /// when its part is picked up, the beam's stitching at the mast until the part slots home with a clack, the long
    /// creak as the mast straightens and its lamp warming. When a mast comes online (<see cref="RelayRestored"/>),
    /// home answers: a short old-radio motif from the direction of the node it links to, as the ground pulse arrives
    /// there. Near a lit mast its lamp (<see cref="RelayNode.LampPosition"/>) hums faintly. Initialised by
    /// <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RelayAudio : MonoBehaviour
    {
        private const string CompletePartLabel = "complete";
        private const int SubscriptionCount = 2;

        // Answers that can wait at once (a chain of masts coming online together).
        private const int AnswerSlots = 4;

        [Tooltip("Assets/_Project/Data/Audio/RelayAudioTuning.asset.")]
        [SerializeField] private RelayAudioTuning _tuning;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private readonly LoopFader _stitchFader = new LoopFader();
        private readonly float[] _answerIn = new float[AnswerSlots];
        private readonly Vector3[] _answerFrom = new Vector3[AnswerSlots];
        private AudioDirector _director;
        private IStationReach _reach;
        private IRoverState _rover;
        private CueHandle _part;
        private CueHandle _slot;
        private CueHandle _creak;
        private CueHandle _lampWarm;
        private CueHandle _link;
        private AudioSource _stitch;
        private AudioSource _answer;
        private AudioSource _mastHum;
        private float _stitchCueVolume;
        private float _humCueVolume;
        private int _completePart;

        internal AudioSource StitchSource => _stitch;

        internal AudioSource AnswerSource => _answer;

        internal AudioSource MastHumSource => _mastHum;

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(RelayAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            _part = director.Resolve(AudioCueIds.FriendPart);
            _slot = director.Resolve(AudioCueIds.BellTapeSlot);
            _creak = director.Resolve(AudioCueIds.RelayMastCreak);
            _lampWarm = director.Resolve(AudioCueIds.RelayLampWarm);
            _link = director.Resolve(AudioCueIds.RelayLink);
            CueHandle stitch = director.Resolve(AudioCueIds.FriendStitch);
            CueHandle hum = director.Resolve(AudioCueIds.RoverLampHum);
            if (!_part.IsValid || !_slot.IsValid || !_creak.IsValid || !_lampWarm.IsValid || !_link.IsValid ||
                !stitch.IsValid || !hum.IsValid || !TryFindCompletePart(director.Library.GetCue(_part)))
            {
                enabled = false;
                return;
            }

            _director = director;
            _reach = context.Get<IStationReach>();
            _rover = context.Get<IRoverState>();
            _stitch = director.CreateLoopSource(transform, "Stitch", stitch, 1f);
            _stitchCueVolume = director.Library.GetCue(stitch).VolumeMax;
            _answer = director.CreateLoopSource(transform, "Answer", default, _tuning.LinkSpatialBlend);
            _answer.loop = false;
            _mastHum = director.CreateLoopSource(transform, "MastHum", hum, 1f);
            _mastHum.rolloffMode = AudioRolloffMode.Linear;
            _mastHum.minDistance = _tuning.MastHumNear;
            _mastHum.maxDistance = _tuning.MastHumFar;
            _mastHum.pitch = _tuning.MastHumPitch;
            _humCueVolume = director.Library.GetCue(hum).VolumeMax;
            for (int i = 0; i < AnswerSlots; i++)
            {
                _answerIn[i] = -1f;
            }

            _subscriptions[0] = context.Events.Subscribe<RelayCued>(OnRelayCued);
            _subscriptions[1] = context.Events.Subscribe<RelayRestored>(OnRelayRestored);
        }

        internal void Wire(RelayAudioTuning tuning)
        {
            _tuning = tuning;
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float sfx = _director.Buses.Effective(AudioBus.Sfx) * _director.WorldGain;
            _stitchFader.Step(dt, _tuning.StitchFadeIn, _tuning.StitchFadeOut);
            Drive(_stitch, _stitchFader.Gain * _tuning.StitchVolume * _stitchCueVolume * sfx);
            for (int i = 0; i < AnswerSlots; i++)
            {
                if (_answerIn[i] < 0f)
                {
                    continue;
                }

                _answerIn[i] -= dt;
                if (_answerIn[i] <= 0f)
                {
                    _answerIn[i] = -1f;
                    Answer(_answerFrom[i]);
                }
            }

            bool near = TryNearestLitMast(out Vector3 lamp);
            if (near)
            {
                _mastHum.transform.position = lamp;
            }

            Drive(_mastHum, near ? _tuning.MastHumVolume * _humCueVolume * sfx : 0f);
        }

        private void OnRelayCued(RelayCued cued)
        {
            switch (cued.Cue)
            {
                case RelayCue.PartCollected:
                    _director.PlayVariantAt(_part, _completePart, cued.Position);
                    break;
                case RelayCue.Stitched:
                    _stitch.transform.position = cued.Position;
                    _stitchFader.FadeIn();
                    break;
                case RelayCue.PartSlotted:
                    _stitchFader.FadeOut();
                    _director.PlayAt(_slot, cued.Position);
                    break;
                case RelayCue.Straightened:
                    _director.PlayAt(_creak, cued.Position);
                    break;
                case RelayCue.LampWarmed:
                    _director.PlayAt(_lampWarm, cued.Position);
                    break;
                default:
                    Debug.LogError($"{nameof(RelayAudio)}: no sound for relay cue {cued.Cue}.", this);
                    break;
            }
        }

        private void OnRelayRestored(RelayRestored restored)
        {
            if (!TryFindNode(restored.LinkedNodeId, out RelayNode linked))
            {
                Debug.LogError($"{nameof(RelayAudio)}: '{restored.RelayId}' links to '{restored.LinkedNodeId}', " +
                               "which is not in the station's reach.", this);
                return;
            }

            int slot = FreeAnswerSlot();
            _answerIn[slot] = Mathf.Max(0f, restored.PulseSeconds);
            _answerFrom[slot] = linked.Position;
        }

        private bool TryFindNode(string id, out RelayNode found)
        {
            for (int i = 0; i < _reach.NodeCount; i++)
            {
                RelayNode node = _reach.GetNode(i);
                if (string.Equals(node.Id, id, StringComparison.Ordinal))
                {
                    found = node;
                    return true;
                }
            }

            found = default;
            return false;
        }

        /// <summary>Plays home's answer a little way from 07 toward <paramref name="linked"/>.</summary>
        private void Answer(Vector3 linked)
        {
            Vector3 from = _rover.Position;
            Vector3 toward = linked - from;
            toward.y = 0f;
            Vector3 direction = toward.sqrMagnitude > 1e-4f ? toward.normalized : Vector3.forward;
            _answer.transform.position = from + direction * _tuning.LinkDistance;
            _director.PlayOn(_answer, _link, 1f);
        }

        private int FreeAnswerSlot()
        {
            int soonest = 0;
            for (int i = 0; i < AnswerSlots; i++)
            {
                if (_answerIn[i] < 0f)
                {
                    return i;
                }

                if (_answerIn[i] < _answerIn[soonest])
                {
                    soonest = i;
                }
            }

            return soonest;
        }

        private bool TryNearestLitMast(out Vector3 lamp)
        {
            lamp = default;
            Vector3 rover = _rover.Position;
            float best = _tuning.MastHumFar;
            bool found = false;

            // Index 0 is home (the tower has its own sounds): masts only.
            for (int i = 1; i < _reach.NodeCount; i++)
            {
                RelayNode node = _reach.GetNode(i);
                float distance = Vector3.Distance(rover, node.LampPosition);
                if (node.Lit && distance < best)
                {
                    best = distance;
                    lamp = node.LampPosition;
                    found = true;
                }
            }

            return found;
        }

        private bool TryFindCompletePart(AudioCue part)
        {
            for (int i = 0; i < part.ClipCount; i++)
            {
                if (part.GetVariantLabel(i) == CompletePartLabel)
                {
                    _completePart = i;
                    return true;
                }
            }

            Debug.LogError($"{nameof(RelayAudio)}: cue '{AudioCueIds.FriendPart}' has no '{CompletePartLabel}' " +
                           "variant.", this);
            return false;
        }

        private static void Drive(AudioSource loop, float volume)
        {
            loop.volume = volume;
            if (volume > 0f && !loop.isPlaying)
            {
                loop.Play();
            }
            else if (volume <= 0f && loop.isPlaying)
            {
                loop.Stop();
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
                _subscriptions[i] = null;
            }
        }
    }
}
