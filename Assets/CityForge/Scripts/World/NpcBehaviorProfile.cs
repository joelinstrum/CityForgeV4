using UnityEngine;
namespace CityForge.World
{
    [CreateAssetMenu(menuName="CityForge/NPC/Behavior profile")]
    public sealed class NpcBehaviorProfile : ScriptableObject
    {
        [Min(0)] public float idleWeight=3,wanderWeight=5,talkWeight=2;
        public Vector2 idleSeconds=new Vector2(3,9),talkSeconds=new Vector2(5,10);
        [Min(1)] public float activityTimeout=40;
        public float Duration(Vector2 range,System.Random random)=>Mathf.Lerp(Mathf.Max(.1f,range.x),Mathf.Max(range.x,range.y),(float)random.NextDouble());
    }
}
