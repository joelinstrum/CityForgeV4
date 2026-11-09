using UnityEngine;

namespace CityForge.World
{
    internal sealed class HarborLanding : MonoBehaviour
    {
        private Material timber, darkTimber;
        private Vector3 landingEnd;
        private Vector3 porchOutward;

        internal bool TryGetPorchContact(out Vector2 contact, out Vector2 outward)
        {
            var end = transform.TransformPoint(landingEnd);
            var direction = transform.TransformDirection(porchOutward);
            contact = new Vector2(end.x, end.z);
            outward = new Vector2(direction.x, direction.z);
            return outward.sqrMagnitude > .5f;
        }

        internal static void Build(Transform lotRoot, MeadowDistrictLandscape land, DistrictLotRecord port, Transform porch)
        {
            var water = new WildlifeWaterAccess(land);
            var frontage = PrototypeRegionView.WaterfrontWorldPoint(port, new Vector2(0, port.definition.WaterfrontStart));
            var outside = PrototypeRegionView.WaterfrontWorldPoint(port, new Vector2(0, port.definition.WaterfrontStart + 1));
            Vector2 outward = outside - frontage;
            // This socket is on the Council House's modeled waterfront porch.
            var start = porch ? porch.TransformPoint(new Vector3(-3.5f, .89f, 2.5f)) :
                new Vector3(frontage.x, Mathf.Max(land.Height(frontage), port.waterfrontDeckHeight) + .12f, frontage.y);
            var from = new Vector2(start.x, start.z);
            float bank = 0, level = 0;
            for (float distance = .5f; distance <= 48f; distance += .5f)
            {
                var point = from + outward * distance;
                if (!land.Contains(point) || land.View.HarborStoneOverlapsLot(point, .95f, port)) return;
                float ground = land.Height(point);
                if (water.Wet(point, ground, out var body, out level) && level - ground > .06f)
                {
                    if (body.kind != WaterBodyKind.Ocean && (body.kind != WaterBodyKind.River || body.isStream)) return;
                    bank = distance;
                    break;
                }
            }
            if (bank <= 0 || (bank < 1f && !porch)) return;
            bool directPorch = porch && bank <= 2f;

            var end = from + outward * (bank - .25f);
            var landing = new GameObject("Harbor timber landing").AddComponent<HarborLanding>();
            landing.transform.SetParent(lotRoot, false);
            landing.timber = Wood(new Color(.38f, .27f, .17f));
            landing.darkTimber = Wood(new Color(.25f, .18f, .12f));
            if (directPorch)
            {
                // The porch is already above navigable water. Use the modeled
                // edge itself as the berth face instead of adding another pier.
                var direction = porch.forward;
                var contact = start + direction * .35f;
                landing.landingEnd = landing.transform.InverseTransformPoint(contact);
                landing.porchOutward = landing.transform.InverseTransformDirection(direction);
                return;
            }

            var tip = new Vector3(end.x, Mathf.Max(start.y, level + .85f), end.y);
            landing.landingEnd = landing.transform.InverseTransformPoint(tip);
            landing.Deck(landing.transform, start, tip, 1.7f, land);
        }

        internal GameObject Connect(Transform vessel, HarborVesselKind kind)
        {
            var from = transform.TransformPoint(landingEnd);
            var (railX, railY, deckX, deckY, station) = kind switch
            {
                HarborVesselKind.Galleon => (2.7f, 2.85f, 1.1f, 2.12f, .4f),
                HarborVesselKind.PirateShip => (2.15f, 1.86f, 1.25f, 1.19f, -2.8f),
                _ => (1.55f, 1.08f, .68f, .92f, 0f)
            };
            float side = Vector3.Dot(from - vessel.position, vessel.right) < 0 ? -1 : 1;
            var rail = vessel.TransformPoint(new Vector3(side * railX, railY, station));
            var deck = vessel.TransformPoint(new Vector3(side * deckX, deckY, station));
            if (Vector3.Distance(from, rail) > 18f) return null;

            var gangway = new GameObject("Gangway from landing to ship deck");
            gangway.transform.SetParent(vessel, false);
            Deck(gangway.transform, from, rail, .9f, null);
            Deck(gangway.transform, rail, deck, .9f, null);
            return gangway;
        }

        private void Deck(Transform parent, Vector3 from, Vector3 to, float width, MeadowDistrictLandscape land)
        {
            var span = to - from;
            float length = span.magnitude;
            if (length < .05f) return;
            var heading = Quaternion.LookRotation(span, Vector3.up);
            var across = heading * Vector3.right;
            const float thickness = .14f;
            int boards = Mathf.Max(1, Mathf.CeilToInt(length / .42f));
            for (int i = 0; i < boards; i++)
            {
                var point = Vector3.Lerp(from, to, (i + .5f) / boards);
                Box(parent, "Plank", point - Vector3.up * (thickness * .5f), heading,
                    new Vector3(width, thickness, length / boards * .975f), i % 5 == 0 ? darkTimber : timber);
            }

            if (!land)
            {
                for (int side = -1; side <= 1; side += 2)
                    Box(parent, "Gangway curb", (from + to) * .5f + across * side * (width * .5f - .05f) +
                        Vector3.up * .045f, heading, new Vector3(.08f, .09f, length), darkTimber);
                return;
            }
            int bays = Mathf.Max(1, Mathf.CeilToInt(length / 3f));
            for (int i = 0; i <= bays; i++)
            {
                var point = Vector3.Lerp(from, to, Mathf.Lerp(.06f, .96f, i / (float)bays));
                for (int side = -1; side <= 1; side += 2)
                {
                    var top = point + across * side * (width * .5f - .15f) + Vector3.up * .24f;
                    float bottom = land.Height(new Vector2(top.x, top.z)) - .18f;
                    float height = top.y - bottom;
                    if (height < .2f) continue;
                    Box(parent, "Landing piling", top - Vector3.up * (height * .5f),
                        Quaternion.Euler(0, heading.eulerAngles.y, 0), new Vector3(.22f, height, .22f), darkTimber);
                }
            }
        }

        private static void Box(Transform parent, string name, Vector3 position, Quaternion rotation,
            Vector3 size, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            var collider = box.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            box.transform.SetParent(parent, false);
            box.transform.SetPositionAndRotation(position, rotation);
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material Wood(Color color) =>
            new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };

        private void OnDestroy()
        {
            if (timber) Destroy(timber);
            if (darkTimber) Destroy(darkTimber);
        }
    }
}
