using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The Gameplay domain's single entry in the bootstrap's system list (after World, Rover and Audio). Resolves the
    /// world, rover and camera services, creates the wallet, initialises the gameplay parts in dependency order
    /// (relics, scrap, sonar), registers the read-only services UI uses and the save sections, and owns the shared
    /// glow meshes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplaySystem : MonoBehaviour, IGameSystem
    {
        [Tooltip("SoftGlow materials (Generated/Gameplay/GameplayVisuals.asset).")]
        [SerializeField] private GameplayVisuals _visuals;

        [SerializeField] private RelicField _relics;
        [SerializeField] private ScrapField _scrap;
        [SerializeField] private SonarSystem _sonar;

        private readonly List<IDisposable> _saveTokens = new List<IDisposable>();
        private GlowMeshSet _meshes;

        public ScrapWallet Wallet { get; private set; }

        public RelicField Relics => _relics;

        public ScrapField Scrap => _scrap;

        public SonarSystem Sonar => _sonar;

        internal void Wire(GameplayVisuals visuals, RelicField relics, ScrapField scrap, SonarSystem sonar)
        {
            _visuals = visuals;
            _relics = relics;
            _scrap = scrap;
            _sonar = sonar;
        }

        public void Initialize(GameContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            string problem = WiringProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(GameplaySystem)}: {problem}", this);
                enabled = false;
                return;
            }

            _meshes = new GlowMeshSet();
            Wallet = new ScrapWallet(context.Events);
            var services = new GameplayServices(context.Events, context.Input, context.Get<ITerrainQuery>(),
                context.Get<IWorldLayout>(), context.Get<IRoverState>(), context.Get<IRoverRig>(),
                context.Get<IViewCamera>(), Wallet, _visuals, _meshes);
            context.Register<IScrapWallet>(Wallet);

            if (!_relics.Initialize(services) || !_scrap.Initialize(services, _relics.Sites) ||
                !_sonar.Initialize(services, _relics))
            {
                enabled = false;
                return;
            }

            RegisterSaveSections(context.Get<ISaveService>());
        }

        private void RegisterSaveSections(ISaveService save)
        {
            _saveTokens.Add(save.Register(new SaveSection<WalletSaveData>(GameplaySaveKeys.Wallet,
                GameplaySaveKeys.WalletVersion, Wallet.Capture, Wallet.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<ScrapSaveData>(GameplaySaveKeys.Scrap,
                GameplaySaveKeys.ScrapVersion, _scrap.Capture, _scrap.Restore)));
            _saveTokens.Add(save.Register(new SaveSection<RelicsSaveData>(GameplaySaveKeys.Relics,
                GameplaySaveKeys.RelicsVersion, _relics.Capture, RestoreRelics)));
        }

        private void RestoreRelics(RelicsSaveData data)
        {
            _relics.Restore(data);
            _sonar.RefreshDiscoveredSites();
        }

        private string WiringProblem()
        {
            if (_visuals == null)
            {
                return "GameplayVisuals is not assigned (run Gameplay/Materials).";
            }

            string visuals = _visuals.Validate();
            if (visuals != null)
            {
                return "GameplayVisuals: " + visuals;
            }

            return _relics == null ? "RelicField is not assigned."
                : _scrap == null ? "ScrapField is not assigned."
                : _sonar == null ? "SonarSystem is not assigned."
                : null;
        }

        private void OnDestroy()
        {
            foreach (IDisposable token in _saveTokens)
            {
                token.Dispose();
            }

            _saveTokens.Clear();
            _meshes?.Dispose();
        }
    }
}
