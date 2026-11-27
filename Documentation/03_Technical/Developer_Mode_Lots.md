# In-game developer Lots

In-game Developer Mode and Lots — November 27

Use the top-right Mode: Player / Mode: Developer button to switch. The developer-only Lots folder icon sits directly beneath it. It is available in an active district, including before the normal city/town founding gates. Government > Game Mode uses the same mode state and stays synchronized. The existing saved-region developerMode field remains authoritative; no new saved schema was added.

Lots reads all saved playable, growable and template lots from the local Lot Editor library. Search by name or use the type filter. Asset-invalid lots remain visible but disabled. A growable preview fills a compatible building when available; selections are cloned and converted to a placed playable instance without editing the library blueprint. The selected lot enters the existing ghost, rotation and placement controller. Tab rotates; Escape cancels. Normal physical placement checks remain. Placed lots can be kept with Save Region.

Developer selections bypass the pending-founder placement branch while the picker or developer draft is active. Switching to Player closes that picker or cancels an unfinished developer placement; existing placed lots remain. Ordinary founder placement, player demand rules, terrain choice, and normal saved-lot placement remain on their existing paths.

Twenty-three isolated native checks passed for the actual HUD/buttons, district gating, saved-lot library/type coverage, template clone, growable materialization, actual picker rows/search/filter, missing assets, placement handoff, pending-founder isolation, cancellation, mode exit/stale callbacks, shared Government setter, mode JSON roundtrip, and restored Player demand behavior. The native UI was captured and visually reviewed. The harness used only a disposable library under work/; it did not edit normal lots or player saves. The broad existing RegionEventChecks suite was not included in these 23 focused checks (its synchronous assignment assertion failed when invoked inside Play mode).

Production sources match the tested QA copy. The new partial lives in Scripts/World/DeveloperLotTools.cs, with small integration changes in PrototypeRegionView.cs, PlayerGrowth.cs, FounderPlacement.cs and SavedLotPlacement.cs. The test harness is compiled under UNITY_EDITOR in the disposable project's World folder to access internal runtime APIs. No live game was stopped or player save written.
