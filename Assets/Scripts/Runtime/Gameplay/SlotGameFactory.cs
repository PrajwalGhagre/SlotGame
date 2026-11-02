using System.Collections.Generic;
using SlotGame.Core.Economy;
using SlotGame.Core.Payouts;
using SlotGame.Core.Random;
using SlotGame.Core.Reels;
using SlotGame.Core.Session;
using SlotGame.Core.Spin;
using SlotGame.Core.Symbols;
using SlotGame.Data;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// Turns the config asset into the plain C# game objects (strips, RNG,
    /// paytable, wallet, session). Shared by the runtime bootstrap and the
    /// editor RTP simulation, so both always use exactly the same setup.
    /// </summary>
    public static class SlotGameFactory
    {
        /// <summary>Prime number used to give every reel its own layout seed.</summary>
        private const int ReelSeedStep = 7919;

        public static List<ReelStrip> BuildStrips(SlotMachineConfig config)
        {
            List<SymbolRule> rules = config.BuildSymbolRules();
            var strips = new List<ReelStrip>(config.ReelCount);

            for (int reel = 0; reel < config.ReelCount; reel++)
            {
                // Seeded on purpose: the strip layout is part of the game design and
                // must be the same every session. Only the spin RNG is truly random.
                var layoutRandom = new SystemRandomProvider(config.StripLayoutSeed + reel * ReelSeedStep);
                strips.Add(ReelStripBuilder.Build(rules, layoutRandom));
            }

            return strips;
        }

        public static IRandomProvider CreateSpinRandom(SlotMachineConfig config)
        {
            return config.UseFixedSpinSeed ? new SystemRandomProvider(config.SpinSeed) : new SystemRandomProvider();
        }

        public static PayoutCalculator CreateCalculator(SlotMachineConfig config)
        {
            var paytable = new Paytable(config.BuildSymbolRules(), config.WildAssistMultiplier);
            return new PayoutCalculator(paytable, config.BuildBonusRules());
        }

        public static SlotGameSession CreateSession(SlotMachineConfig config, IReadOnlyList<ReelStrip> strips,
            IRandomProvider spinRandom)
        {
            var generator = new SpinOutcomeGenerator(strips, config.VisibleRows, spinRandom);
            var wallet = new Wallet(config.StartingBalance, config.BetLevels, config.StartingBetIndex);
            return new SlotGameSession(wallet, generator, CreateCalculator(config), config.BuildBonusRules());
        }

        /// <summary>Runs the RTP simulation with the real config (used from the Tools menu).</summary>
        public static RtpSimulator.Report Simulate(SlotMachineConfig config, int paidSpins)
        {
            List<ReelStrip> strips = BuildStrips(config);
            var generator = new SpinOutcomeGenerator(strips, config.VisibleRows, new SystemRandomProvider());
            return RtpSimulator.Run(generator, CreateCalculator(config), config.BuildBonusRules(), paidSpins);
        }
    }
}
