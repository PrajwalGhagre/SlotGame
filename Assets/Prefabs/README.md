# Prefabs

`SlotMachine.prefab` (the whole machine: reels, HUD, popups) is generated from code
so it always matches the art and the config:

Tools > Slot Game > Generate Machine Prefab

The game also runs without the prefab: if the bootstrap has no prefab assigned,
SlotMachineViewFactory builds the same hierarchy at runtime.
