# NPC ambient-life foundation

November 1, 2026. Shared ambient-life implementation; initial stream study and accepted generated settlements.

## Scope

Opt-in transient population of ten characters in the accepted Stream Rock Study Pass17 northeast district. New-region settlement content also uses this shared foundation; see Native_Settlements.md. Older camp-free saves do not acquire NPCs. No settlement, economy, nation, occupation or historical identity is inferred from the supplied character's clothing or vendor filename. The same character definition can use different behavior profiles.

The initial milestone implements idle, walk, wander (destination selection plus walk), and paired conversation. Conversation currently uses the supplied fold-arms clip, not a dedicated talking gesture. Gathering, hunting, sitting, schedules, resources, character import UI and permanent residents remain later milestones.

## Private asset preparation

Original supplied archives reside locally in `/Users/joelinstrum/Downloads/indian stuff`. All three audited FBXs contain a Mixamo-named rig and idle/walk clips; the fringe-dress and chief files also contain fold_arms, and warrior contains run. This study uses only the fringe-dress character. No retargeting or new animation was generated.

Run `Tools/Npc/prepare_npc_study.py` with CITYFORGE_PROJECT and the supplied download directory. It checks existing copies rather than replacing them. In Unity Edit mode run CityForge > NPC > Prepare local resident. The builder refuses to overwrite an existing definition, creates a generic animation controller and matte URP supplied-color material, and normalizes the imported model's bounds to 1.7m and prepares two editable behavior profile assets. Animator is found in the model hierarchy and root motion is disabled/baked for controller-owned movement. The supplied files and generated FBX, textures, materials, controller, prefab and definition stay in ignored SourceAssets/NpcStudy and Resources/NpcStudy. Public source is insufficient to reproduce the art without local preparation.

## Runtime architecture

- NpcDefinition: prefab, dimensions, speed and animation state mappings.
- NpcBehaviorProfile: reusable idle/wander/talk weights, durations and timeout. ResidentProfile and SocialProfile are private, editable assets assigned through the definition; profiles can be reused across character definitions.
- NpcRecord: independent ID, position, heading, activity and diagnostic counters. Records are currently transient, not serialized residents.
- NpcBrain and INpcAction: per-character action state, weighted activity selection at action boundaries, timeout and cancellation. Wander chooses a reachable point; walking is a reusable action.
- NpcLocalNavigation: fixed 33x33 one-metre grid built once against displayed terrain, study water datum, native tree trunks and nearby MeshRenderer obstacle bounds. Four-neighbor BFS prevents diagonal corner cutting. Rejects unsafe slopes and unreachable destinations. This is bounded study navigation, not a general bridge/interior/multi-district solution or a NavMesh integration.
- NpcPopulation: one scheduler, ten independent brains, two assignable profile assets, personal-space checks, paired meeting reservations, shared conversation duration, cleanup, diagnostics and camera zoom policy.

Conversations reserve both participants and separate meeting spots, check both paths, walk to the spots, face each other, and release both people on completion/cancellation/timeout. People wait when another occupies their next step; persistent congestion times out rather than tunneling through other people. Dynamic detours/crowd steering remain future work. Obstacles are sampled at initialization; moving construction would require navigation invalidation.

Scheduler runs at 30Hz at Zoom0/1 and 10Hz at Zoom2; animated renderers cull offscreen. At Zoom3–5 all visual roots are inactive and this transient study pauses, then resumes at close zoom. This is explicitly not the future abstract far-population simulation. Records and visual objects are separate as the foundation for that later step. There is no thousands-of-citizens performance claim.

## Running and checking

In Play mode open Stream Rock Study — Pass17, northeast district, then CityForge > NPC > Start study in stream district. The editor entry point is gated to this exact fixture and district. It focuses the existing camera at Zoom0 and adds a diagnostic overlay. Call NpcPopulation.SetDiagnostics(false) to hide the overlay. Leaving the district or stopping Play removes the trial. No NPC fields are written into region saves.

NpcStudyChecks.Run(outputDirectory) opens the existing fixture through the normal menu, then checks ten animated characters, suitable positions, personal space, terrain grounding, autonomous movement, completed paired conversations, unreachable paths, interruption cleanup, close/far culling, pause/resume and teardown/restart. Captures Zoom0–2 and leaves a fresh population in Play at Zoom0. It requires the existing saved fixture and never creates/replaces a save.

## Next milestones

1. User review of visibility, animation, speed and grouping at actual camera scale.
2. Refine walking/turning and add dedicated talking clips; add more supplied appearances after evaluating the first rig.
3. Add interaction locations and gathering sequences, then seating/rest and occupations.
4. Introduce versioned permanent resident records, settlement references and multi-district ownership before city residents ship.
5. Measure standalone population scaling and camera pans; add visual pooling and abstract far simulation when evidence warrants it.

## Verified candidate — November 1

Unity compiled with zero errors/warnings. Normal existing saved stream load passed. Ten residents used950 walkable cells; all ten moved more than1m; leg animation changed28.9degrees; two paired conversations completed. No invalid positions or personal-space overlaps were observed during the65-second check, and record positions exactly matched sampled terrain height. Unreachable-path rejection, paired interruption release, Zoom0–2 captures, Zoom3 visual removal/study pause, close resume, teardown and restart passed. Two meetings were cancelled/timed out across autonomous activity and the deliberate cancellation check.49 existing region files remained byte-identical with no new saves. The captured view remains a visual candidate pending user feedback; no standalone performance benchmark was run.

## Idle while stationary

NpcWalkAction now starts in idle and selects Walk only when a navigation tick actually displaces the character horizontally. A blocked route keeps its target while playing Idle, and resumes Walk when personal-space clearance allows movement. Reached waypoints are consumed in the same tick; completed routes and action exits return to Idle. Existing conversational animation remains separate. NpcStudyChecks includes a forced blocker regression that checks actual Animator Idle/Walk states after blends, clear-path resumption, explicit stopping, and arrival.

Locomotion synchronization: actual horizontal displacement determines final animation, rather than the requested action alone. Every NPC tick resolves state after action changes, then the population resolves again after meeting updates. Moving actors enter Walk immediately and evaluate that pose before visual position is synced; stationary actors resolve Idle or their stationary conversation state. Animator requests check actual current/next states, so cached strings cannot conceal a reset. Keep state across visual disable and use CullUpdateTransforms to retain state progression offscreen. NpcLocomotionChecks observes actual rendered-root displacement versus actual state and clip for every role across Zoom0/1/2, hide/show and a deliberate idle reset, plus stopped-state samples.
