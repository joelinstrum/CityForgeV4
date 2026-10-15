# Opening Menu

## Visual target

The supplied [menu concept image](../Goal_Images/MenuPageConcept.png) is the reference for the V4 opening screen. A golden-hour river city fills the frame; the CityForge mark sits at the upper left; five prominent dark-and-gold actions run below it. Settings and Exit sit together at the lower right so they are available without competing with the main actions. The actual scene uses the separate source background, logo, and icon sheets under `Assets/CityForge/Art/UI/Menu/`.

The layout is built as Unity UI over the artwork, with the image maintaining its aspect ratio and menu elements scaling from a 1672 × 941 reference frame. The logo and menu entries are separate interactive elements rather than a flat screenshot. Cinzel is bundled for the menu labels with its OFL license under `Assets/CityForge/Fonts/`.

## Current implementation

`Assets/CityForge/Scenes/Gameplay/MainMenu.unity` is the first scene in Build Settings. It contains:

- Open Region, Create Region, Lot Editor, Water Systems, and Mods buttons.
- Settings and Exit buttons at the lower right.
- A Settings panel with a fullscreen/windowed toggle and master audio steps, saved between launches.
- A reusable 75% modal for Open Region and Create Region, with a rounded gold border,
  a hero icon and header, a clipped scrolling body, and a footer. It closes through
  its X, the backdrop, or Escape. Its prefab is `Assets/CityForge/Prefabs/PF_Modal_75.prefab`.
- An information panel that explains the remaining development entries. Lot Editor
  remains outside the modal flow because its eventual interface will fill the screen.
- Mouse and keyboard button navigation; Escape closes an open panel.

The five development entries currently open explanatory panels. They should be connected to their real flows as region persistence, creation, editing, water, and mod systems are implemented. Settings and Exit already perform their stated actions. In the editor, Exit stops Play mode; in a build, it quits the application.

## Review questions

- Does the composition remain readable on narrower and wider displays?
- Should the foreground city image remain static, or gain restrained movement or atmospheric effects later?
- When the first real flow is implemented, what should its return-to-menu behavior be?
