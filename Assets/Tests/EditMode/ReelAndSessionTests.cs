using System.Collections.Generic;
using NUnit.Framework;
using SlotGame.Core.Bonus;
using SlotGame.Core.Economy;
using SlotGame.Core.Motion;
using SlotGame.Core.Payouts;
using SlotGame.Core.Random;
using SlotGame.Core.Reels;
using SlotGame.Core.Session;
using SlotGame.Core.Spin;
using SlotGame.Core.Symbols;

namespace SlotGame.Tests
{
    public class ReelStripTests
    {
        [Test]
        public void Strip_ContainsEveryCopy()
        {
            ReelStrip strip = ReelStripBuilder.Build(TestData.DefaultRules, new SystemRandomProvider(1));

            var counts = new Dictionary<SymbolType, int>();
            foreach (SymbolType symbol in strip.Symbols)
            {
                counts.TryGetValue(symbol, out int count);
                counts[symbol] = count + 1;
            }

            foreach (SymbolRule rule in TestData.DefaultRules)
            {
                Assert.AreEqual(rule.CopiesOnStrip, counts[rule.Type], rule.Type.ToString());
            }
        }

        [Test]
        public void Strip_HasNoSymbolNextToItself()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                ReelStrip strip = ReelStripBuilder.Build(TestData.DefaultRules, new SystemRandomProvider(seed));
                for (int i = 0; i < strip.Length; i++)
                {
                    Assert.AreNotEqual(strip.GetSymbol(i), strip.GetSymbol(i + 1), $"seed {seed}, index {i}");
                }
            }
        }

        [Test]
        public void SameLayoutSeed_GivesSameStrip()
        {
            ReelStrip a = ReelStripBuilder.Build(TestData.DefaultRules, new SystemRandomProvider(5));
            ReelStrip b = ReelStripBuilder.Build(TestData.DefaultRules, new SystemRandomProvider(5));

            CollectionAssert.AreEqual(a.Symbols, b.Symbols);
        }

        [Test]
        public void WrapIndex_HandlesNegativeAndLargeValues()
        {
            ReelStrip strip = ReelStripBuilder.Build(TestData.DefaultRules, new SystemRandomProvider(3));

            Assert.AreEqual(strip.Length - 1, strip.WrapIndex(-1));
            Assert.AreEqual(0, strip.WrapIndex(strip.Length));
            Assert.AreEqual(strip.GetSymbol(2), strip.GetSymbol(2 + strip.Length * 7));
        }

        [Test]
        public void Window_ShowsStripNeighboursAroundTheStop()
        {
            SpinOutcomeGenerator generator = TestData.CreateGenerator(new SystemRandomProvider(1));
            SpinResult result = generator.CreateResult(new[] { 4, 0, 7 });

            for (int reel = 0; reel < 3; reel++)
            {
                ReelStrip strip = generator.Strips[reel];
                int stop = result.StopIndices[reel];
                Assert.AreEqual(strip.GetSymbol(stop + 1), result.GetSymbol(reel, 0), "top row");
                Assert.AreEqual(strip.GetSymbol(stop), result.GetSymbol(reel, 1), "payline");
                Assert.AreEqual(strip.GetSymbol(stop - 1), result.GetSymbol(reel, 2), "bottom row");
            }
        }
    }

    public class ReelStopPlannerTests
    {
        [Test]
        public void LandingSlot_ShowsTheTargetSymbol()
        {
            var random = new SystemRandomProvider(11);
            for (int i = 0; i < 2000; i++)
            {
                double position = random.NextDouble() * 1000.0;
                int stripLength = random.NextInt(5, 30);
                int target = random.NextInt(0, stripLength);
                int extra = random.NextInt(0, 4);

                ReelStopPlan plan = ReelStopPlanner.Plan(position, 20f, target, stripLength, 3, extra, 0.2f, 0.2f, 3f);

                int landed = ReelStopPlanner.SlotToStripIndex((long)plan.LandingPosition, plan.NewIndexOffset, stripLength);
                Assert.AreEqual(target, landed);
            }
        }

        [Test]
        public void Splice_HappensAboveEverythingThePlayerCanSee()
        {
            const int rows = 3;
            ReelStopPlan plan = ReelStopPlanner.Plan(10.4, 20f, 3, 11, rows, 0, 0.2f, 0.2f, 3f);

            long currentTopBufferSlot = 10 + rows / 2 + 1;
            Assert.Greater(plan.SpliceFrom, currentTopBufferSlot);

            // The lowest visible row at landing must already use the new mapping.
            long lowestVisibleAtLanding = (long)plan.LandingPosition - (rows - 1 - rows / 2);
            Assert.GreaterOrEqual(lowestVisibleAtLanding, plan.SpliceFrom);
            Assert.Greater(plan.Distance, 0.0);
        }

        [Test]
        public void Duration_MatchesSpinSpeed_WhenInsideLimits()
        {
            ReelStopPlan plan = ReelStopPlanner.Plan(0.0, 20f, 0, 11, 3, 2, 0f, 0.1f, 10f);

            // Ease-out-cubic starts with slope 3, so this keeps the speed continuous.
            Assert.AreEqual(3.0 * plan.Distance / 20.0, plan.Duration, 0.0001);
        }

        [Test]
        public void Easing_StartsAtZeroAndEndsAtOne()
        {
            Assert.AreEqual(0f, Easing.OutCubic(0f), 1e-6);
            Assert.AreEqual(1f, Easing.OutCubic(1f), 1e-6);
            Assert.AreEqual(1f, Easing.OutBack(1f), 1e-5);
            Assert.AreEqual(0.5f, Easing.SmoothStep(0.5f), 1e-6);
        }
    }

    public class WalletTests
    {
        [Test]
        public void PlacingABet_TakesCoins()
        {
            var wallet = new Wallet(100, new[] { 10, 20, 50 });

            Assert.IsTrue(wallet.TryPlaceBet());
            Assert.AreEqual(90, wallet.Balance);
        }

        [Test]
        public void CanNotBetMoreThanBalance()
        {
            var wallet = new Wallet(15, new[] { 10, 20 }, 1);

            Assert.IsFalse(wallet.CanAffordBet);
            Assert.IsFalse(wallet.TryPlaceBet());
            Assert.AreEqual(15, wallet.Balance);
        }

        [Test]
        public void BetLevels_AreClamped()
        {
            var wallet = new Wallet(100, new[] { 1, 5, 10 });

            wallet.DecreaseBet();
            Assert.AreEqual(1, wallet.CurrentBet);

            wallet.IncreaseBet();
            wallet.IncreaseBet();
            wallet.IncreaseBet();
            Assert.AreEqual(10, wallet.CurrentBet);
            Assert.IsTrue(wallet.IsAtMaxBet);
        }

        [Test]
        public void Events_AreRaised()
        {
            var wallet = new Wallet(100, new[] { 1, 5 });
            int lastBalance = -1;
            int lastBet = -1;
            wallet.BalanceChanged += value => lastBalance = value;
            wallet.BetChanged += value => lastBet = value;

            wallet.IncreaseBet();
            wallet.AddWinnings(20);

            Assert.AreEqual(5, lastBet);
            Assert.AreEqual(120, lastBalance);
        }
    }

    public class SlotGameSessionTests
    {
        [Test]
        public void Round_ChargesBet_AndPaysWin()
        {
            SpinOutcomeGenerator generator = TestData.CreateGenerator(new SystemRandomProvider(3));
            var wallet = new Wallet(1000, new[] { 10 });
            var session = new SlotGameSession(wallet, generator, TestData.CreateCalculator(), BonusRules.Default);

            SpinRoundResult round = session.PlayRound();

            Assert.AreEqual(1000 - 10 + round.TotalPayout, wallet.Balance);
            Assert.AreEqual(wallet.Balance, round.BalanceAfter);
            Assert.IsFalse(round.WasFreeSpin);
        }

        [Test]
        public void FreeSpin_DoesNotChargeTheBet_AndUsesTheMultiplier()
        {
            SpinOutcomeGenerator generator = TestData.CreateGenerator(new SystemRandomProvider(8));
            var wallet = new Wallet(1000, new[] { 10 });
            var freeSpins = new FreeSpinsTracker();
            freeSpins.Award(2, 2);
            var session = new SlotGameSession(wallet, generator, TestData.CreateCalculator(), BonusRules.Default, freeSpins);

            SpinRoundResult round = session.PlayRound();

            Assert.IsTrue(round.WasFreeSpin);
            Assert.AreEqual(2, round.AppliedMultiplier);
            Assert.AreEqual(round.Win.Payout * 2, round.TotalPayout);
            Assert.AreEqual(1000 + round.TotalPayout, wallet.Balance);
        }

        [Test]
        public void NoCoins_MeansNoRound()
        {
            SpinOutcomeGenerator generator = TestData.CreateGenerator(new SystemRandomProvider(1));
            var wallet = new Wallet(5, new[] { 10 });
            var session = new SlotGameSession(wallet, generator, TestData.CreateCalculator(), BonusRules.Default);

            Assert.IsFalse(session.CanPlayRound);
            Assert.Throws<System.InvalidOperationException>(() => session.PlayRound());
        }

        [Test]
        public void Simulation_ReturnsAPlausibleRtp()
        {
            SpinOutcomeGenerator generator = TestData.CreateGenerator(new SystemRandomProvider(2024));
            RtpSimulator.Report report = RtpSimulator.Run(generator, TestData.CreateCalculator(), BonusRules.Default, 200000);
            TestContext.WriteLine(report.ToString());

            // The paytable is tuned for roughly 90-100% RTP. This catches a broken paytable or RNG.
            Assert.That(report.Rtp, Is.InRange(0.80, 1.10), report.ToString());
            Assert.That(report.HitRate, Is.InRange(0.05, 0.40), report.ToString());
        }
    }
}
