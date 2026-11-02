using System.Collections.Generic;
using SlotGame.Core.Payouts;
using SlotGame.Core.Symbols;
using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>
    /// The single config asset for the whole game. The bootstrap loads it from
    /// Resources/SlotGame/SlotMachineConfig, so the scene works even if nothing
    /// is wired in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "SlotMachineConfig", menuName = "Slot Game/Slot Machine Config", order = 1)]
    public sealed class SlotMachineConfig : ScriptableObject
    {
        /// <summary>Path used with Resources.Load.</summary>
        public const string ResourcesPath = "SlotGame/SlotMachineConfig";

        [Header("Symbols")]
        [SerializeField] private List<SymbolDefinition> symbols = new List<SymbolDefinition>();
        [SerializeField] private SymbolType wildSymbol = SymbolType.Wild;

        [Header("Reels")]
        [SerializeField, Range(3, 5)] private int reelCount = 3;
        [Tooltip("Rows visible in each reel window. Must be odd so the payline sits in the middle.")]
        [SerializeField, Range(1, 5)] private int visibleRows = 3;
        [Tooltip("Seed for the reel strip LAYOUT (which symbol is where). Not the spin RNG.")]
        [SerializeField] private int stripLayoutSeed = 1234;

        [Header("RNG")]
        [Tooltip("Tick this to get the same spins every time you press Play (for debugging).")]
        [SerializeField] private bool useFixedSpinSeed;
        [SerializeField] private int spinSeed = 42;

        [Header("Wallet")]
        [SerializeField, Min(0)] private int startingBalance = 1000;
        [SerializeField] private int[] betLevels = { 5, 10, 25, 50, 100 };
        [SerializeField, Min(0)] private int startingBetIndex = 1;

        [Header("Bonus")]
        [Tooltip("Wins that need a Wild are multiplied by this.")]
        [SerializeField, Min(1)] private int wildAssistMultiplier = 2;
        [SerializeField, Min(1)] private int scattersForFreeSpins = 3;
        [SerializeField, Min(0)] private int freeSpinsAwarded = 5;
        [SerializeField, Min(1)] private int freeSpinWinMultiplier = 2;
        [Tooltip("A win of at least this many times the bet shows the BIG WIN popup.")]
        [SerializeField, Min(1)] private int bigWinBetMultiple = 15;

        [Header("Presentation")]
        [SerializeField] private SpinAnimationProfile animationProfile;
        [SerializeField] private MachineArt art = new MachineArt();
        [SerializeField] private SoundLibrary sounds = new SoundLibrary();

        public IReadOnlyList<SymbolDefinition> Symbols => symbols;
        public SymbolType WildSymbol => wildSymbol;
        public int ReelCount => reelCount;
        public int VisibleRows => visibleRows;
        public int StripLayoutSeed => stripLayoutSeed;
        public bool UseFixedSpinSeed => useFixedSpinSeed;
        public int SpinSeed => spinSeed;
        public int StartingBalance => startingBalance;
        public IReadOnlyList<int> BetLevels => betLevels;
        public int StartingBetIndex => startingBetIndex;
        public int WildAssistMultiplier => wildAssistMultiplier;
        public int BigWinBetMultiple => bigWinBetMultiple;
        public SpinAnimationProfile AnimationProfile => animationProfile;
        public MachineArt Art => art;
        public SoundLibrary Sounds => sounds;

        public List<SymbolRule> BuildSymbolRules()
        {
            var rules = new List<SymbolRule>(symbols.Count);
            foreach (SymbolDefinition definition in symbols)
            {
                if (definition != null)
                {
                    rules.Add(definition.ToRule());
                }
            }

            return rules;
        }

        public BonusRules BuildBonusRules()
        {
            return new BonusRules(wildSymbol, wildSymbol, scattersForFreeSpins, freeSpinsAwarded, freeSpinWinMultiplier);
        }

        public SymbolDefinition GetDefinition(SymbolType type)
        {
            foreach (SymbolDefinition definition in symbols)
            {
                if (definition != null && definition.SymbolType == type)
                {
                    return definition;
                }
            }

            return null;
        }

        /// <summary>Returns a list of problems, empty when the config is usable.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            var seen = new HashSet<SymbolType>();

            if (symbols == null || symbols.Count < 2) problems.Add("Add at least two symbols.");
            if (symbols != null)
            {
                foreach (SymbolDefinition definition in symbols)
                {
                    if (definition == null)
                    {
                        problems.Add("The symbol list has an empty slot.");
                        continue;
                    }

                    if (!seen.Add(definition.SymbolType)) problems.Add($"{definition.SymbolType} is listed twice.");
                    if (definition.Sprite == null) problems.Add($"{definition.name} has no sprite.");
                }
            }

            if (visibleRows % 2 == 0) problems.Add("Visible rows must be an odd number.");
            if (betLevels == null || betLevels.Length == 0) problems.Add("Add at least one bet level.");
            if (animationProfile == null) problems.Add("Assign a Spin Animation Profile.");
            return problems;
        }

        private void OnValidate()
        {
            if (visibleRows % 2 == 0)
            {
                visibleRows = Mathf.Max(1, visibleRows - 1);
            }

            if (betLevels != null && betLevels.Length > 0)
            {
                startingBetIndex = Mathf.Clamp(startingBetIndex, 0, betLevels.Length - 1);
            }
        }
    }
}
