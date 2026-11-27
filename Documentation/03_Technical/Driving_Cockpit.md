# Driving cockpit overlay

Entering the existing first-person ride mode now attaches a screen-space vintage cockpit. Exiting hides both cockpit and ride instructions. Camera pose, FOV, driving physics and navigation are unchanged; steering is exposed as read-only telemetry for the overlay.

## Inspector configuration

Select `Assets/CityForge/Resources/Driving/Cockpit/Settings.asset`. Each of engine vibration, turning sway, road vibration, inertia and time-of-day lighting has its own enable switch and intensity controls. Defaults are intentionally restrained: motion is capped at 4.5 reference pixels at 1080p and 0.15 degrees. Disable all four motion effects for a completely stationary cockpit. The runtime camera component also accepts a settings asset and sprite override.

Road movement responds to travel and sampled ground-height changes; it does not currently distinguish road materials. Effects use unscaled time to match the existing driving loop. Interior tint follows the region's existing clock, including transitions, with dim cool night defaults.

## Rendering and input

Cockpit canvas uses Screen Space Overlay with sorting order -100. Existing game HUD at order 0 remains above it; equivalent ride speed/control instructions use order 10. The original OnGUI instructions remain a fallback if the sprite is missing. Neither new canvas has a GraphicRaycaster; all graphics and CanvasGroups reject input interception.

The supplied PNG is imported unchanged as an uncompressed Full Rect UI sprite with alpha transparency, no mipmaps, Clamp wrapping and no NPOT resizing. Its central windshield is transparent. Uniform contain scaling preserves the complete 2:1 cockpit frame. Opaque dark extensions fill unused screen space at different aspect ratios and overlap the outer rim to avoid edge gaps during motion.

## Private asset import

The PNG and settings asset remain private generated project content. To reinstall, use `CityForge > Driving > Import Cockpit PNG…`; the importer verifies transparency and preserves an existing settings asset. Source code remains independent of the private PNG and safely falls back to the existing drive HUD when absent.

## Validation

Disposable Unity 6 QA scene exercises actual BeginRide, StepRide and EndRide against isolated terrain and townhouse prefabs; no player regions or saves are modified. Checks cover sprite settings, input pass-through, motion bounds and independent toggles, unchanged camera state and driving behavior, clock tint, canvas ordering, repeated entry and cleanup. Layout geometry is checked at six aspect ratios including portrait and 32:9. Rendered evidence is captured separately at 4:3, 16:9 and ultrawide, plus night.

Validation completed November 27, 2026: 291 assertions passed, including four actual Game view captures with verified pixel dimensions. These checks use a disposable scene rather than a player save.

## Art Deco era and Model T audio — November 27 follow-up

The vintage overlay is now exclusive to the selected district's effective Art Deco appearance era (stable catalog ID `modern`). This follows the same district appearance override and year fallback as buildings and traffic. Other eras keep the existing first-person drive view and fallback instructions. Switching era while riding hides/restores this presentation without ending the drive.

The existing private `Audio/CitySounds/car-model-t` MP3 loops quietly while Art Deco driving is active. Defaults: 0.12 volume, gentle fade-in, non-spatial audio using the existing listener and global audio controls. Exit, other eras, disable and teardown stop it. Inspector settings expose Engine Audio, Engine Volume, Engine Fade Seconds and an optional Engine Sound override for this Art Deco presentation. Other eras have no assigned cockpit or engine sound yet.
