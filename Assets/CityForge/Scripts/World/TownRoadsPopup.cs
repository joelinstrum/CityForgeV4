using System;
using CityForge.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private void BuildRoadsPopup()
        {
            if(repaveTarget!=null)newStreetTreeStyle=repaveTarget.streetTreeStyle;
            const string artPath = "UI/TownConstruction/";
            var categoriesArt = Resources.Load<Texture2D>(artPath + "RoadCategories");
            var surfacesArt = Resources.Load<Texture2D>(artPath + "RoadSurfaces");
            var concept = Resources.Load<Texture2D>(artPath + "RoadConcept");
            var panel = FounderRect(transportModal, "Roads construction", Vector2.zero, new Vector2(1510, 876));
            StyleTownPanel(panel, new Color(.047f, .084f, .113f, .99f));

            if (concept)
                TownArtwork(panel, "Roads heading", concept, new Rect(58, 29, 642, 93),
                    new Vector2(-466, 395), new Vector2(499, 72));
            else
                FounderText(panel, "Transport – Roads", new Vector2(-466, 395), new Vector2(499, 72), 35).color = TownGold;
            var close = TownButton(panel, lotFont, "×", 1456, -20, 36, CloseTransportModal);
            close.Select();

            var eras = CityForgeTimeline.Eras;
            int districtYear = TownYear(SelectedDistrict);
            int eraIndex = Math.Max(0, Array.FindIndex(eras, era =>
                districtYear >= era.FirstYear && districtYear <= era.LastYear));
            int industrialIndex = Array.FindIndex(eras, era => era.id == "industrial");
            var eraLabel = FounderText(panel, "", new Vector2(375, 395), new Vector2(224, 42), 23);
            eraLabel.color = TownGold;

            Rule("Header divider", new Vector2(0, 351), new Vector2(1508, 1));
            Rule("Category divider", new Vector2(-455, 12), new Vector2(1, 664));
            Rule("Preview divider", new Vector2(290, 12), new Vector2(1, 664));
            Rule("Footer divider", new Vector2(0, -337), new Vector2(1508, 1));

            var categoryTitle = FounderText(panel, "Roads", new Vector2(-83, 309), new Vector2(710, 43), 32);
            categoryTitle.alignment = TextAnchor.MiddleLeft;
            var summary = FounderText(panel, "Overland connections for trade, industry, and regional travel.",
                new Vector2(-83, 269), new Vector2(710, 36), 20);
            summary.alignment = TextAnchor.MiddleLeft;

            var categoryNames = new[] { "Roads", "Streets", "Avenues", "Highways", "Paths & Trails", "Specialty" };
            int[] categoryTop = { 19, 166, 308, 445, 575, 710, 852 };
            var categoryButtons = new Button[categoryNames.Length];
            var categoryFaces = new RawImage[categoryNames.Length];
            int[] surfaceLeft = { 12, 305, 597, 888, 1180, 1474, 1769 };
            var surfaceButtons = new Button[RoadSurfaces.Length];
            var surfaceFaces = new RawImage[RoadSurfaces.Length];
            var detailTitle = FounderText(panel, "", new Vector2(520, 309), new Vector2(402, 43), 30);
            detailTitle.color = TownGold;
            var preview = FounderRect(panel, "Road preview", new Vector2(520, 171), new Vector2(396, 188))
                .gameObject.AddComponent<RawImage>();
            preview.raycastTarget = false;
            var details = FounderText(panel, "", new Vector2(520, -16), new Vector2(390, 128), 21);
            details.alignment = TextAnchor.UpperLeft;
            var placeHint = FounderText(panel, "Hover for details  ·  Click a surface to place it",
                new Vector2(0, -388), new Vector2(760, 40), 21);
            placeHint.color = TownGold;

            var treeHeading=FounderText(panel,"STREET TREES",new Vector2(520,-145),new Vector2(390,30),21);treeHeading.color=TownGold;
            bool treeChoiceMade=repaveTarget!=null;
            var treeChoices=new Button[StreetTreePalette.Styles.Length+1];
            for(int t=0;t<treeChoices.Length;t++)
            {
                int choice=t;string id=t==0?null:StreetTreePalette.Styles[t-1].Id;
                treeChoices[t]=TownButton(panel,lotFont,t==0?"No trees":StreetTreePalette.Styles[t-1].Label,1064+(t%2)*202,-600-(t/2)*45,194,()=>{treeChoiceMade=true;newStreetTreeStyle=id;RefreshTreeChoices();});
                treeChoices[t].gameObject.name="Road tree style · "+(id??"none");
            }
            void RefreshTreeChoices(){for(int t=0;t<treeChoices.Length;t++)MarkTownButton(treeChoices[t],t==0?string.IsNullOrEmpty(newStreetTreeStyle):newStreetTreeStyle==StreetTreePalette.Styles[t-1].Id);}
            RefreshTreeChoices();
            int hoveredSurface = -1;
            int selectedCategory = 0;
            for (int i = 0; i < categoryNames.Length; i++)
            {
                int index = i;
                var button = TownButton(panel, lotFont, categoryNames[i], 23, -114 - i * 69, 252,
                    () => SelectCategory(index));
                categoryButtons[i] = button;
                categoryFaces[i] = RoadButtonArtwork(button, categoriesArt, CategoryCrop(i, false), 60);
            }

            for (int i = 0; i < RoadSurfaces.Length; i++)
            {
                int index = i;
                var button = TownButton(panel, lotFont, RoadSurfaces[i].Label, 322 + i % 3 * 240, -199 - i / 3 * 267, 219,
                    () => {if(IsDividedBoulevard(RoadSurfaces[index].Id)&&!treeChoiceMade&&string.IsNullOrEmpty(newStreetTreeStyle))newStreetTreeStyle="city-tree-small";ChooseRoadSurface(index);});
                surfaceButtons[i] = button;
                if (i < HistoricalRoadSurfaceCount)
                    surfaceFaces[i] = RoadButtonArtwork(button, surfacesArt, SurfaceCrop(i, false), 247);
                else
                {
                    var caption = button.GetComponentInChildren<Text>();
                    caption.fontSize = 22;
                    if (IsFoundersStreet(RoadSurfaces[i].Id) || IsModernRoadSurface(RoadSurfaces[i].Id))
                    {
                        var texture = StreetTexture(i);
                        if (texture)
                        {
                            var swatch = new GameObject("Street material", typeof(RectTransform), typeof(RawImage));
                            swatch.transform.SetParent(button.transform, false);
                            var rect = (RectTransform)swatch.transform;
                            rect.anchorMin = new Vector2(0, .32f); rect.anchorMax = Vector2.one;
                            rect.offsetMin = new Vector2(12, 10); rect.offsetMax = new Vector2(-12, -12);
                            swatch.GetComponent<RawImage>().texture = texture;
                            swatch.GetComponent<RawImage>().raycastTarget = false;
                            caption.rectTransform.anchorMin = Vector2.zero;
                            caption.rectTransform.anchorMax = new Vector2(1, .32f);
                            caption.rectTransform.offsetMin = new Vector2(8, 4); caption.rectTransform.offsetMax = new Vector2(-8, 0);
                        }
                    }
                }
                var hover = button.gameObject.AddComponent<EventTrigger>();
                AddPreviewEvent(EventTriggerType.PointerEnter, () => PreviewSurface(index));
                AddPreviewEvent(EventTriggerType.PointerExit, () => ClearPreview(index));
                AddPreviewEvent(EventTriggerType.Select, () => PreviewSurface(index));
                AddPreviewEvent(EventTriggerType.Deselect, () => ClearPreview(index));

                void AddPreviewEvent(EventTriggerType type, Action action)
                {
                    var entry = new EventTrigger.Entry { eventID = type };
                    entry.callback.AddListener(_ => action());
                    hover.triggers.Add(entry);
                }
            }

            var align = TownButton(panel, lotFont, "Snap to Tile Centers", 25, -802, 226, null);
            MarkTownButton(align, snapNewRoadsToTileCenters);
            align.onClick.AddListener(() =>
            {
                snapNewRoadsToTileCenters = !snapNewRoadsToTileCenters;
                MarkTownButton(align, snapNewRoadsToTileCenters);
            });
            TownButton(panel, lotFont, showRoadPaths ? "Hide Road Paths" : "Show Road Paths",
                1276, -802, 208, () =>
                {
                    CloseTransportModal();
                    SetRoadPaths(!showRoadPaths);
                });

            var previousEra = TownButton(panel, lotFont, "‹", 976, -25, 42, null);
            var nextEra = TownButton(panel, lotFont, "›", 1243, -25, 42, null);
            previousEra.gameObject.name = "Previous road era";
            nextEra.gameObject.name = "Next road era";
            previousEra.onClick.AddListener(() => ShowEra(eraIndex - 1));
            nextEra.onClick.AddListener(() => ShowEra(eraIndex + 1));
            ShowEra(eraIndex);

            void ShowEra(int index)
            {
                eraIndex = Mathf.Clamp(index, 0, eras.Length - 1);
                eraLabel.text = eras[eraIndex].Name;
                previousEra.interactable = eraIndex > 0;
                nextEra.interactable = eraIndex < eras.Length - 1;
                SelectCategory(selectedCategory);
            }

            void SelectCategory(int index)
            {
                selectedCategory = index;
                for (int i = 0; i < categoryButtons.Length; i++)
                    MarkRoadButton(categoryButtons[i], categoryFaces[i], CategoryCrop(i, i == index), i == index);

                bool laterEra = industrialIndex >= 0 && eraIndex > industrialIndex;
                bool roads = index == 0;
                bool streets = index == 1;
                bool avenues = index == 2;
                bool modernEra=eraIndex>=Array.FindIndex(eras,e=>e.id=="modern");
                int roadCount = HistoricalRoadSurfaceCount + (laterEra ? 1 : 0);
                categoryTitle.text = categoryNames[index];
                summary.text = roads ? "Overland connections for trade, industry, and regional travel."
                    : streets ? "Local streets for built-up districts."
                    : avenues && modernEra ? "Wide avenues and divided boulevards with planted medians." : "No construction options are available for this category in this era.";

                int columns = streets || (roads && laterEra) ? 4 : 3;
                int slot=0;
                float width = columns == 4 ? 163 : 219;
                float height = streets ? 154 : columns == 4 ? 194 : 247;
                for (int i = 0; i < surfaceButtons.Length; i++)
                {
                    bool visible = (roads && (i < roadCount || modernEra && IsModernRoadSurface(RoadSurfaces[i].Id) && !IsModernStreet(RoadSurfaces[i].Id))) || (streets && (IsFoundersStreet(RoadSurfaces[i].Id) || (laterEra && i == PavedStreetIndex) || modernEra && IsModernStreet(RoadSurfaces[i].Id)&&!IsAvenue(RoadSurfaces[i].Id))) || (avenues&&modernEra&&IsAvenue(RoadSurfaces[i].Id));
                    surfaceButtons[i].gameObject.SetActive(visible);
                    if (!visible) continue;
                    int position = slot++;
                    var rect = (RectTransform)surfaceButtons[i].transform;
                    rect.anchoredPosition = new Vector2(322 + position % columns * (720 / columns),
                        -199 - position / columns * (height + 20));
                    rect.sizeDelta = new Vector2(width, height);
                    if (streets)
                    {
                        var caption = surfaceButtons[i].GetComponentInChildren<Text>();
                        caption.resizeTextForBestFit = true;
                        caption.resizeTextMinSize = 14;
                        caption.resizeTextMaxSize = 18;
                    }
                    if (surfaceFaces[i]) surfaceFaces[i].rectTransform.sizeDelta = rect.sizeDelta;
                }

                treeHeading.text=avenues?"TREES · CURBSIDE / MEDIAN":"STREET TREES";
                treeHeading.gameObject.SetActive(streets||avenues&&modernEra);foreach(var treeChoice in treeChoices)treeChoice.gameObject.SetActive(streets||avenues&&modernEra);
                placeHint.text=streets?"Choose paving · Trees fill available sidewalk space.":"Hover for details · Click a surface to place it";
                placeHint.gameObject.SetActive(roads || streets || avenues&&modernEra);
                PreviewSurface(-1);
            }

            void ClearPreview(int index)
            {
                if (hoveredSurface == index) PreviewSurface(-1);
            }

            void PreviewSurface(int index)
            {
                hoveredSurface = index;
                for (int i = 0; i < surfaceButtons.Length; i++)
                    MarkRoadButton(surfaceButtons[i], surfaceFaces[i],
                        i < HistoricalRoadSurfaceCount ? SurfaceCrop(i, i == index) : default, i == index);
                detailTitle.gameObject.SetActive(index >= 0);
                details.gameObject.SetActive(index >= 0);
                if (index < 0)
                {
                    preview.gameObject.SetActive(false);
                    return;
                }
                detailTitle.text = RoadSurfaces[index].Label;
                details.text = RoadSurfaces[index].Description;
                var texture = IsFoundersStreet(RoadSurfaces[index].Id) || IsModernRoadSurface(RoadSurfaces[index].Id) ? StreetTexture(index) : index >= HistoricalRoadSurfaceCount ? null
                    : index == 3 && concept ? concept : surfacesArt;
                preview.gameObject.SetActive(texture != null);
                if (!texture) return;
                preview.texture = texture;
                if (IsFoundersStreet(RoadSurfaces[index].Id) || IsModernRoadSurface(RoadSurfaces[index].Id))
                {
                    preview.uvRect = new Rect(0, 0, 1, 1);
                    ((RectTransform)preview.transform).sizeDelta = new Vector2(396, 188);
                    return;
                }
                var crop = texture == concept ? new Rect(1332, 216, 492, 203)
                    : new Rect(surfaceLeft[index] + 20, 105, 250, 204);
                preview.uvRect = TownArtworkUV(texture, crop);
                ((RectTransform)preview.transform).sizeDelta = texture == concept
                    ? new Vector2(396, 164) : new Vector2(230, 188);
            }

            Texture2D StreetTexture(int index) => RoadSurfaces[index].Id == FoundersStreetId
                ? Resources.Load<Texture2D>("Lots/PathMaterials/WagonRoadAlbedo")
                : DistrictRoadStyle(RoadSurfaces[index].Id).Texture;

            Rect CategoryCrop(int index, bool active) => new Rect(active ? 914 : 261, categoryTop[index],
                603, categoryTop[index + 1] - categoryTop[index]);

            Rect SurfaceCrop(int index, bool active) => new Rect(surfaceLeft[index], active ? 460 : 77,
                surfaceLeft[index + 1] - surfaceLeft[index], 348);

            void Rule(string name, Vector2 position, Vector2 size)
            {
                var image = FounderRect(panel, name, position, size).gameObject.AddComponent<Image>();
                image.color = new Color(.58f, .46f, .27f);
                image.raycastTarget = false;
            }
        }

        private RawImage RoadButtonArtwork(Button button, Texture2D atlas, Rect crop, float height)
        {
            SkinTownButton(button, crop, height, atlas);
            return button.targetGraphic as RawImage;
        }

        private void MarkRoadButton(Button button, RawImage face, Rect crop, bool active)
        {
            if (face) face.uvRect = TownArtworkUV((Texture2D)face.texture, crop);
            else MarkTownButton(button, active);
        }
    }
}
