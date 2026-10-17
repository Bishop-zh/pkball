using UnityEngine;
namespace PKBall.Prototype
{
    public sealed class PrototypeProjectile : MonoBehaviour
    {
        private PrototypeActor owner;
        private PrototypeAttackPayload payload;
        private SpriteRenderer sprite;private Renderer sphere;private float size;
        public bool IsPersistentSword {get;private set;}
        public PrototypeActor Owner => owner;
        public const float PersistentPierceInterval=PrototypeSwordFlight.PierceInterval;
        private TrailRenderer trail;private Material trailMaterial;
        private void OnDestroy(){if(trailMaterial!=null){if(Application.isPlaying)Destroy(trailMaterial);else DestroyImmediate(trailMaterial);}}
        public void MakePersistent(int index)
        {
            IsPersistentSword=true;transform.localScale=Vector3.one*Mathf.Max(.01f,payload.visual==null?.7f:payload.visual.projectileSize);
            string persistentIcon=string.IsNullOrEmpty(payload.visual?.projectileIconId)?"green_flying_sword":payload.visual.projectileIconId;
            sprite.sprite=PrototypeUiSkin.SpriteFor("Icons/"+persistentIcon);sprite.enabled=sprite.sprite!=null;sphere.enabled=!sprite.enabled;
            if(sprite.enabled)sprite.transform.localScale=Vector3.one/Mathf.Max(sprite.sprite.bounds.size.x,sprite.sprite.bounds.size.y);
            if(trail==null){trail=gameObject.AddComponent<TrailRenderer>();trailMaterial=new Material(Shader.Find("Sprites/Default"));trailMaterial.hideFlags=HideFlags.DontSave;trail.sharedMaterial=trailMaterial;trail.time=.24f;trail.startWidth=.1f;trail.endWidth=0;trail.startColor=new Color(.3f,1,.8f,.85f);trail.endColor=new Color(.3f,1,.8f,0);}
            trail.Clear();trail.enabled=true;trail.autodestruct=false;
        }
        public void Fire(PrototypeBattle activeBattle,PrototypeActor shooter,Vector3 origin,PrototypeActor victim,float travelSpeed,PrototypeAttackPayload snapshot)
        {
            IsPersistentSword=false;if(trail!=null){trail.Clear();trail.enabled=false;}
            owner=shooter;payload=snapshot;
            if(sphere==null)sphere=GetComponent<Renderer>();
            if(sprite==null){var go=new GameObject("ProjectileSprite");go.transform.SetParent(transform,false);sprite=go.AddComponent<SpriteRenderer>();sprite.sortingOrder=22;}
            var style=snapshot.visual?.projectileVisual??PrototypeProjectileVisual.Orb;size=snapshot.visual?.projectileSize??.18f;
            string icon=style==PrototypeProjectileVisual.Arrow?"spear":style==PrototypeProjectileVisual.Sword?"greatsword":style==PrototypeProjectileVisual.Bolt?"crossbow":null;
            if(!string.IsNullOrEmpty(snapshot.visual?.projectileIconId))icon=snapshot.visual.projectileIconId;
            sprite.sprite=icon==null?null:PrototypeUiSkin.SpriteFor("Icons/"+icon);sprite.enabled=sprite.sprite!=null;sprite.color=Color.white;
            sphere.enabled=!sprite.enabled;transform.localScale=Vector3.one*size;
            if(sprite.enabled)sprite.transform.localScale=Vector3.one/Mathf.Max(sprite.sprite.bounds.size.x,sprite.sprite.bounds.size.y);
            transform.position=origin;gameObject.SetActive(true);
        }
        public void Present(Vector3 position, Vector3 targetPosition)
        {
            if (IsPersistentSword && Vector3.Distance(transform.position, position)>4) trail?.Clear();
            transform.position = position;
            var desired = targetPosition - position;
            if (sprite.enabled) sprite.transform.rotation = Quaternion.Euler(90, Mathf.Atan2(desired.x, desired.z) * Mathf.Rad2Deg, 0);
        }
    }
}
