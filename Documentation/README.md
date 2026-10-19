# CityForge V4 — Design documentation

**CityForge is a city builder about place, time, and memory.** You begin with a landscape that already has a past, build a region over centuries, and eventually become the steward and curator of what your cities have become. **History passes through your city**: the calendar changes the world, while the conditions and decisions in each city determine how it responds.

This folder is the design source of truth for V4. It records the direction agreed in the concept discussion, plus questions that still need decisions. It is not a claim that any described system already exists. V4 starts clean; V3 is a reference for behavior, assets, and lessons, not a project to copy wholesale.

**V4 foundation:** a clean Unity 6 project using URP, PBR materials, and a genuine 3D world. The familiar elevated orthographic/dimetric view is the default camera choice, not a limitation of the assets or world. [Initial scaffold](03_Technical/Initial_Scaffold.md), [asset conventions](03_Technical/Asset_Conventions.md), and the [scalability plan](03_Technical/Scalability_Plan.md) record the implementation baseline. The concept images remain visual acceptance targets as the world develops.

For current implementation and the approved biome assets, read the [main development handoff](../HANDOFF.md) first.

## Read in this order

1. [Vision](00_Vision/CityForge_Vision.md) and [design principles](00_Vision/Design_Principles.md) establish the identity and guardrails.
2. [Visual target](00_Vision/Visual_Target.md) describes the look to pursue; the [goal-image gallery](Goal_Images/README.md) contains the concept images used for review. The [opening menu](00_Vision/Opening_Menu.md) has its own target and implementation notes.
3. Gameplay documents describe the player's experience and the simulation boundaries.
4. Content documents describe the historical setting and modding direction.
5. Technical documents frame investigations and the deliberate use of V3.

## Documentation index

| Area | Documents |
|---|---|
| Vision | [CityForge Vision](00_Vision/CityForge_Vision.md) · [Design Principles](00_Vision/Design_Principles.md) · [Visual Target](00_Vision/Visual_Target.md) · [Opening Menu](00_Vision/Opening_Menu.md) |
| Gameplay | [Time and History](01_Gameplay/Time_and_History.md) · [City Memory and Chronicle](01_Gameplay/City_Memory_and_Chronicle.md) · [Economy and Resources](01_Gameplay/Economy_and_Resources.md) · [Infrastructure](01_Gameplay/Infrastructure.md) · [Culture and Preservation](01_Gameplay/Culture_and_Preservation.md) · [Governor Reputation](01_Gameplay/Governor_Reputation.md) · [Regional Development](01_Gameplay/Regional_Development.md) |
| Content | [Historical Context](02_Content/Historical_Context.md) · [Eras and Timeline](02_Content/Eras_and_Timeline.md) · [Buildings and Architecture](02_Content/Buildings_and_Architecture.md) · [Modding](02_Content/Modding.md) |
| Technical | [Initial Scaffold](03_Technical/Initial_Scaffold.md) · [Asset Conventions](03_Technical/Asset_Conventions.md) · [Scalability Plan](03_Technical/Scalability_Plan.md) · [Rendering Vision](03_Technical/Rendering_Vision.md) · [V4 Architecture](03_Technical/V4_Architecture.md) · [V3 Migration Reference](03_Technical/V3_Migration_Reference.md) |
| Visual references | [Goal Images](Goal_Images/README.md) — ten original concept images and an overview. |

## Status convention

**Direction** marks a principle to protect. **Illustrative example** shows the intended feel without fixing numbers, dates, names, or formulas. **Open question** identifies work for later design or prototyping. This distinction matters especially for eras, score thresholds, historical content, and rendering technology.

## What CityForge is not

It is not an Anno-style inventory chain, a Civilization-style race to invent future technology early, a utility maintenance simulator, or a war game. It is a city builder where geography, economics, institutions, changing technology, and the memory of places create meaningful decisions without taking attention away from building, decorating, expanding, customizing, and observing.
