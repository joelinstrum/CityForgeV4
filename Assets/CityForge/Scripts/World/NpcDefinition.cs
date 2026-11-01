using UnityEngine;
namespace CityForge.World
{
    [CreateAssetMenu(menuName="CityForge/NPC/Character definition")]
    public sealed class NpcDefinition : ScriptableObject
    {
        public GameObject prefab;
        public NpcBehaviorProfile[] profiles;
        [Min(.1f)] public float height=1.7f;
        [Min(.1f)] public float radius=.35f;
        [Min(.1f)] public float walkSpeed=1.1f;
        public string idleState="Idle",walkState="Walk",talkState="FoldArms";
    }
    // A record is independent of its replaceable animated representation. Study records are transient.
    public sealed class NpcRecord
    {
        public string id;
        public Vector3 position;
        public float heading;
        public string activity="Idle",lastFailure="";
        public int completedActivities,completedWalks,completedTalks;
    }
}
