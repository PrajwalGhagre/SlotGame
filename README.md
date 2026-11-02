# Lucky Slots: Unity Slot Machine

A 3 reel slot machine made in Unity 2022.3 LTS for the Underpin Services Unity Developer Intern assignment.
It uses the art from the provided asset pack and follows the reference design (three reels, the middle row is the payline, JACKPOT machine with a lever).

**Play in the browser:** https://YOUR-GITHUB-USERNAME.github.io/REPO-NAME/Build/WebGL/

## Game Overview

- Pick a bet with the arrow buttons (5, 10, 25, 50 or 100 coins). You start with 1,000 coins.
- Press **SPIN**, pull the **lever**, or hit **Space**.
- The reels start one after another, spin, and stop left to right.
- You win when **all three reels show the same symbol on the middle line**. The payout is `bet x multiplier` from the paytable.
- Press SPIN (or Space) again while the reels are moving to stop them early.
- **AUTO** keeps spinning until you turn it off or run out of coins. **PAYTABLE** shows every payout and rule.
- If you run out of coins you can refill to 1,000.
- **M** mutes the sound.

### Paytable (3 in a row on the middle line)

| Symbol | Pays |
| --- | --- |
| WILD x3 | 100x bet |
| Lucky Seven | 30x bet |
| BAR | 15x bet |
| Bell | 5x bet |
| Cherry | 2x bet |

A win that needed a WILD pays double. I ran 1,000,000 simulated spins to check the maths: the return to player is about 97% and roughly 1 spin in 8 wins.

## How to Run the WebGL Build

**Online:** open the GitHub Pages link at the top of this file.

**Locally:** browsers block WebGL builds opened straight from disk (`file://`), so serve the folder with any small web server:

```bash
cd Build/WebGL
python -m http.server 8000
# then open http://localhost:8000 in Chrome, Edge or Firefox
```

**From the Unity project:**

1. Open the project folder with Unity Hub (Unity 2022.3 LTS).
2. Open `Assets/Scenes/SlotMachine.unity` and press Play.
3. To make a new WebGL build use **Tools > Slot Game > Build WebGL** (needs the WebGL Build Support module). It writes to `Build/WebGL` with gzip compression and decompression fallback switched on, so the build also works on GitHub Pages.

## Bonus Features

- **WILD symbol:** stands in for any symbol on the payline. "Bell, WILD, Bell" counts as three Bells, and wins that use a WILD pay x2. Three WILDs is the top prize (100x).
- **Free spins:** 3 or more WILDs anywhere in the window (not only on the payline) give 5 free spins. Free spins play on their own, do not cost coins, and every win during them is doubled. Hitting the trigger again during free spins adds more spins. At the end a popup shows the total won.
- **Anticipation:** when the first two reels can still make a win (or one more WILD would trigger free spins), the last reel spins a bit longer and glows, like real machines do.
- **Stop early:** pressing SPIN again while the reels move makes them stop quickly on the same result.
- **Auto spin**, **paytable popup**, **big win popup** with a coin count up, and **out of coins** refill dialog.
- **Sound effects** for the lever, spinning, reel stops, wins, free spins and errors.

The pack did not include a WILD symbol or any sounds, so I drew a simple pixel art WILD star in the same palette and made short retro sound effects for the game.

## Thought Process / Approach

### 1. Keep the rules away from the visuals

I split the code into two assemblies:

- `SlotGame.Core` is plain C# with **no UnityEngine reference** (`noEngineReferences` is on in its asmdef). It holds everything that decides money: RNG, reel strips, spin results, paytable, payout calculation, wallet, free spins and the round flow (`SlotGameSession`).
- `SlotGame.Runtime` is the Unity side: ScriptableObject configs, reel animation, UI, audio and the controller.

The result of a spin is decided **before** the reels move. `SlotGameSession.PlayRound()` takes the bet, rolls the RNG, works out the win and pays it. The `SlotMachineController` then only plays that result back. The animation can never change the outcome, which is also how real slot machines work. Because the core has no Unity code, I could unit test it and simulate a million spins in about a second.

### 2. RNG

- All randomness goes through an `IRandomProvider` interface. `SystemRandomProvider` wraps `System.Random` and can take a seed, so a session can be replayed exactly (tick **Use Fixed Spin Seed** in the config). Tests use a scripted provider to force exact outcomes.
- Each reel has a visible **reel strip** (Cherry x3, Bell x3, BAR x2, Seven x2, WILD x1) and every stop on it has a **weight**. The RNG picks a stop with `WeightedRandomPicker`, which uses cumulative weights and a binary search. This is the "virtual reel" idea from real machines: the strip the player sees stays short and tidy while the odds are tuned separately.
- Strips are shuffled once with a fixed layout seed (Fisher-Yates) and the builder makes sure the same symbol never sits next to itself.
- The symbols above and below the payline are the real neighbours on the strip, so what you see is always consistent with the result.

### 3. Reel animation

The reel does not move a long column of sprites. Each reel keeps 5 cells (3 visible rows plus one hidden cell above and below) and one number, its **position** in symbols. Every frame the cells are placed from that number and given the right sprite, so the reel looks endless with only 5 Images.

A spin goes through a small state machine in `ReelView`:

1. **Wind up:** a tiny pull back, like a reel being kicked.
2. **Accelerate:** speed follows an AnimationCurve up to max speed.
3. **Spin:** constant speed, with a slight vertical stretch on the symbols as a cheap motion blur.
4. **Stop:** the reels are told to stop one by one with a short gap (staggered), the last one later if anticipation is on.
5. **Ease out and bounce:** the reel slows down with an ease-out curve, goes slightly past the result, then settles back onto it.

The tricky part is landing exactly on the RNG result without a visible jump. `ReelStopPlanner` (in Core, with unit tests) handles it: when a stop is requested it "splices" the strip just above the top edge of the window. Cells you can already see keep their symbols and every cell that has not appeared yet uses a new offset chosen so the landing cell shows the result. The ease-out curve starts with slope 3, so the stop duration is `3 x distance / speed`, which keeps the speed continuous and avoids a sudden brake. All the timings live in `Assets/Animations/ReelSpinProfile.asset` and can be tuned in the Inspector while playing.

### 4. Data driven setup

- `SymbolDefinition` assets hold each symbol's sprite, copies on the strip, weight, payout and glow colour.
- `SlotMachineConfig` (in `Resources/SlotGame`) holds the symbol list, reel and row count, RNG seed options, wallet (start balance, bet levels), bonus settings, art, sounds and the animation profile.
- The paytable popup is built from the same assets, so the numbers on screen always match the real maths.

### 5. Scene setup with almost no manual wiring

The scene has one object, `SlotGame`, with `SlotGameBootstrap`. It loads the config, builds the logic objects and creates the machine view through `SlotMachineViewFactory`, which places every part using measurements taken from the art. **Tools > Slot Game > Generate Machine Prefab** saves the same hierarchy as `Assets/Prefabs/SlotMachine.prefab` and makes the scene use it, so it can also be edited visually. There are no singletons; all dependencies are passed in by the bootstrap.

### 6. Testing

EditMode tests (Window > General > Test Runner) cover:

- payout rules (three of a kind, losses, matches off the payline, wild substitution, three wilds, scatter count)
- weighted RNG distribution and determinism with a seed
- reel strip building (copies, no neighbour duplicates, wrapping)
- the reel stop maths (the landing cell always shows the target, the splice is never visible)
- wallet rules and a full game session including free spins
- an RTP sanity check over 200,000 spins

**Tools > Slot Game > Simulate RTP** runs a million spins with the real config and prints the RTP and hit rate.

## Project Structure

```
Assets/
  Animations/      ReelSpinProfile (reel timing and curves), TitlePulse clip
  Art/             Machine and symbol sprites from the asset pack (+ the WILD symbol)
  Data/Symbols/    SymbolDefinition assets
  Prefabs/         SlotMachine prefab (generated from the factory)
  Resources/       SlotMachineConfig
  Scenes/          SlotMachine.unity
  Scripts/
    Core/          Pure C#: Random, Reels, Spin, Payouts, Economy, Bonus, Session, Motion
    Runtime/       Unity side: Data, Reels, Gameplay, View, UI, Audio
    Editor/        Sprite import rules, Tools menu (prefab, RTP simulation, WebGL build)
  Sounds/          Sound effects
  Tests/EditMode/  Unit tests
  UI/              Buttons, panels, popup and background sprites
Build/WebGL/       WebGL build
Docs/              Reference design from the asset pack
```

## What I Would Add Next

- More paylines (top, bottom and diagonals) using the same calculator.
- Object pooled particle coins for big wins.
- Saving the balance with PlayerPrefs.
- A proper sprite atlas to cut draw calls.
