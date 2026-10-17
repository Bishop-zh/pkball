using System.Collections.Generic;
namespace PKBall.Prototype
{
    public struct PrototypeScheduledAttack
    {
        public PrototypeActor source,target;
        public PrototypeAttackPayload payload;
        public float hitAt;
        internal long sequence;
    }
    // Binary heap ordered by intended hit time, then release order. No per-hit coroutine or arbitrary queue delay.
    public sealed class PrototypeAttackScheduler
    {
        private readonly List<PrototypeScheduledAttack> heap=new List<PrototypeScheduledAttack>(128);
        private long sequence;
        private static bool Before(PrototypeScheduledAttack a,PrototypeScheduledAttack b) =>
            a.hitAt<b.hitAt || (a.hitAt==b.hitAt && a.sequence<b.sequence);
        public void Add(PrototypeScheduledAttack attack)
        {
            attack.sequence=sequence++;int index=heap.Count;heap.Add(attack);
            while(index>0)
            {
                int parent=(index-1)/2;if(!Before(attack,heap[parent])) break;
                heap[index]=heap[parent];index=parent;
            }
            heap[index]=attack;
        }
        public bool TryTake(float now,out PrototypeScheduledAttack attack)
        {
            if(heap.Count==0 || heap[0].hitAt>now) {attack=default;return false;}
            attack=heap[0];var last=heap[heap.Count-1];heap.RemoveAt(heap.Count-1);
            if(heap.Count==0) return true;
            int index=0;
            while(index*2+1<heap.Count)
            {
                int child=index*2+1;
                if(child+1<heap.Count && Before(heap[child+1],heap[child])) child++;
                if(!Before(heap[child],last)) break;
                heap[index]=heap[child];index=child;
            }
            heap[index]=last;return true;
        }
        public void Clear() {heap.Clear();sequence=0;}
    }
}
