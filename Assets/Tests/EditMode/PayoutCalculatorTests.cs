using NUnit.Framework;
using SlotGame.Core.Payouts;
using SlotGame.Core.Spin;
using SlotGame.Core.Symbols;

namespace SlotGame.Tests
{
    public class PayoutCalculatorTests
    {
        private const SymbolType C = SymbolType.Cherry;
        private const SymbolType B = SymbolType.Bell;
        private const SymbolType R = SymbolType.Bar;
        private const SymbolType S = SymbolType.Seven;
        private const SymbolType W = SymbolType.Wild;

        private PayoutCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = TestData.CreateCalculator();
        }

        [Test]
        public void ThreeOfAKind_OnPayline_PaysSymbolMultiplier()
        {
            SpinResult spin = TestData.Window(new[] { C, B, R }, new[] { S, S, S }, new[] { B, C, B });

            WinResult win = _calculator.Evaluate(spin, 10);

            Assert.IsTrue(win.IsWin);
            Assert.AreEqual(S, win.WinningSymbol);
            Assert.IsFalse(win.UsedWild);
            Assert.AreEqual(10 * 30, win.Payout);
            Assert.AreEqual(3, win.WinningPositions.Count);
        }

        [Test]
        public void MixedSymbols_OnPayline_IsALoss()
        {
            SpinResult spin = TestData.Window(new[] { S, S, S }, new[] { C, B, C }, new[] { S, S, S });

            WinResult win = _calculator.Evaluate(spin, 10);

            Assert.IsFalse(win.IsWin);
            Assert.AreEqual(0, win.Payout);
        }

        [Test]
        public void MatchingSymbols_OffThePayline_DoNotPay()
        {
            // Three sevens on the top row only, the payline is the middle row.
            SpinResult spin = TestData.Window(new[] { S, S, S }, new[] { C, B, R }, new[] { C, C, C });

            Assert.IsFalse(_calculator.Evaluate(spin, 10).IsWin);
        }

        [Test]
        public void Wild_SubstitutesForMissingSymbol_AndDoublesTheWin()
        {
            SpinResult spin = TestData.Window(new[] { C, C, C }, new[] { B, W, B }, new[] { C, C, C });

            WinResult win = _calculator.Evaluate(spin, 10);

            Assert.IsTrue(win.IsWin);
            Assert.AreEqual(B, win.WinningSymbol);
            Assert.IsTrue(win.UsedWild);
            Assert.AreEqual(10 * 5 * 2, win.Payout);
        }

        [Test]
        public void TwoWildsAndOneSymbol_PayThatSymbol()
        {
            SpinResult spin = TestData.Window(new[] { C, C, C }, new[] { W, R, W }, new[] { C, C, C });

            WinResult win = _calculator.Evaluate(spin, 5);

            Assert.AreEqual(R, win.WinningSymbol);
            Assert.AreEqual(5 * 15 * 2, win.Payout);
        }

        [Test]
        public void ThreeWilds_PayTheWildPrize_WithoutExtraMultiplier()
        {
            SpinResult spin = TestData.Window(new[] { C, B, C }, new[] { W, W, W }, new[] { C, B, C });

            WinResult win = _calculator.Evaluate(spin, 10);

            Assert.AreEqual(W, win.WinningSymbol);
            Assert.IsFalse(win.UsedWild);
            Assert.AreEqual(10 * 100, win.Payout);
        }

        [Test]
        public void WildWithTwoDifferentSymbols_IsALoss()
        {
            SpinResult spin = TestData.Window(new[] { C, C, C }, new[] { B, W, C }, new[] { C, C, C });

            Assert.IsFalse(_calculator.Evaluate(spin, 10).IsWin);
        }

        [Test]
        public void ThreeScattersAnywhere_TriggerFreeSpins()
        {
            SpinResult spin = TestData.Window(new[] { W, C, B }, new[] { C, B, C }, new[] { B, W, W });

            Assert.AreEqual(3, _calculator.FindScatters(spin).Count);
            Assert.IsTrue(_calculator.TriggersFreeSpins(spin));
        }

        [Test]
        public void Anticipation_WhenFirstTwoReelsMatch()
        {
            SpinResult open = TestData.Window(new[] { C, C, C }, new[] { B, B, C }, new[] { C, C, C });
            SpinResult openWithWild = TestData.Window(new[] { C, C, C }, new[] { W, R, C }, new[] { C, C, C });
            SpinResult closed = TestData.Window(new[] { C, C, C }, new[] { B, R, B }, new[] { C, C, C });

            Assert.IsTrue(AnticipationRule.ShouldAnticipateLastReel(open, BonusRules.Default));
            Assert.IsTrue(AnticipationRule.ShouldAnticipateLastReel(openWithWild, BonusRules.Default));
            Assert.IsFalse(AnticipationRule.ShouldAnticipateLastReel(closed, BonusRules.Default));
        }

        [Test]
        public void TwoScatters_DoNotTriggerFreeSpins()
        {
            SpinResult spin = TestData.Window(new[] { W, C, B }, new[] { C, B, C }, new[] { B, C, W });

            Assert.IsFalse(_calculator.TriggersFreeSpins(spin));
        }
    }
}
