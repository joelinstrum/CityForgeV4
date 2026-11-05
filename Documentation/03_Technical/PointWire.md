Deferred completion: set DeferCompletion=true and ProcessingMessage for a consumer
that performs expensive work. CompletePath then returns true when queued; Processing
is immediately true, the custom cursor/guides disappear, and a modal operation overlay
renders before CompletionRequested runs. ProcessingChanged can suspend consumer input.
Validation errors retain the guide; Completed still fires only on successful acceptance.
Cancel/teardown releases the pending operation and restores input. Default false keeps
existing synchronous closed/lake behavior. RiverRouteTool uses this option.

Lake completion now automatically dismisses the tool, clears the blue guides and
restores the default cursor. The lake remains; no Escape press is needed. This is
a lake-adapter Completed handler, not a global PointWire behavior.

# PointWire: reusable click / marker / wire interaction

Reference this utility as **PointWire**, or invoke `$cityforge-pointwire` in Codex.
The installed skill lives at ~/.codex/skills/cityforge-pointwire/SKILL.md.

PointWirePath owns guide state in world XZ: TryAdd, TryClose, Undo, Clear, Points and
Closed. Points is a read-only view without a duplicated closing point. It rejects
nonfinite/duplicate points, caps at256 markers and requires three points to close.

PointWireSurface is an abstract adapter for Available, CanEdit, Collider, Contains
and Height. It can wrap a terrain or another collider without depending on region,
forest, water, or lake types. PointWireTool.Initialize(camera,hud,surface,color)
builds the interaction. Configure Label, CompletedHint and AllowClosedUndo before
Arm(). Cancel() discards transient state and restores cursor/collider ownership.

CompletionRequested receives guide points and returns a validation error, or
null/empty to accept. It runs before closure; a rejected path stays editable and
shows the message. Completed fires after acceptance. Domain-specific validation,
mutation, persistence and undo belong in the consumer adapter/service. For domain
mutations, disable closed undo unless a real domain undo is implemented. Cancelling
an accepted sketch does not undo its already-created object.

PointWireGeometry supplies RoundClosed (bounded Chaikin corner cutting), IsSimple,
Area, Contains and SignedDistance. It never alters the guide. Consumers choose
whether to use curves, straight edges or another interpretation. It is independent
of rendering and saved regions; units follow its supplied coordinates. RoundClosed
caps subdivision at three passes. OpenPath is the explicit Enter-to-finish mode: at least two markers, Finished=true
and Closed=false on acceptance. CompletePath is the shared runtime/test entry point.
SnapPoint transforms preview/placed points; RibbonWidth previews a proposed channel.
DrawingHint supplies consumer-specific help. See Player_Waterways.md for RiverRouteTool.

PointWireTool handles blue/default configurable cursor, terrain-draped strings,
larger first marker,24 screen-pixel start snapping, release gating after menu
clicks, UI raycast blocking, focus/save/loading guards, Escape and Backspace. Camera
navigation remains live. The overlay projects through the supplied camera only;
marker layer2 excludes them from the approved lake reflection probe. The code-native
PointWireGraphic requires CanvasRenderer and HUD-sized bounds. It ignores UI raycasts.
Up to32 ground samples per guide edge (16 when drawing three ribbon lines) keep
the UI mesh within its vertex budget. Projection draws without per-segment list allocations.

LakeOutlineTool demonstrates integration: its DistrictSurface adapter wraps the
active district; toolbar events arm/cancel; CreatePlayerLake consumes completion.
It disables closed undo because closing commits a persistent lake. Do not duplicate
its input or overlay implementation for another tool.

Validation exercised pure path operations, invalid polygons, actual menu/completion
callbacks, closed/live-string renders, basin creation, save/reopen, stale caches,
fresh generation with exact outside-forest positions, and separate lake/pond bodies.
Use disposable snapshots under Library/ and restore the user checkpoint after checks.
