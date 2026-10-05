using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PhaseArena
{
    // Tool creation, clearance and physical impulses. Run progression stays in the director.
    public sealed class ArenaTools
    {
        readonly ArenaDirector world;
        public ArenaTools(ArenaDirector world) { this.world=world; }
        ArenaTuning tuning => world.tuning;
        BodyRegistry registry => world.Registry;
        List<ThermoBody> bodies => world.bodies;
        ThermoBody projectilePrefab => world.projectilePrefab;
        ThermoBody shieldPrefab => world.shieldPrefab;
        Transform entities => world.entities;
        Sprite fireSprite => world.fireSprite;
        Sprite iceSprite => world.iceSprite;
        RunMetrics metrics => world.metrics;
        public bool UsesStoneMagic(ThermoBody caster) => world.Has(WorldLaw.StoneMagic) && (caster.kind==BodyKind.Player || caster.kind==BodyKind.Enemy);
        public Sprite ProjectileSprite(ThermoBody caster,float heat) => UsesStoneMagic(caster) ? world.worldGenerator.rockPrefab.visual.sprite : heat>0 ? fireSprite : iceSprite;
        public ProjectileProfile Profile(ThermoBody caster,float stoneCharge=0)
        {
            if(UsesStoneMagic(caster))
            {
                var rock=world.worldGenerator.rockPrefab;
                float scale=Mathf.Lerp(1,Mathf.Max(1,WorldLayout.TypicalRockScale*tuning.stoneMaximumRockScale),Mathf.Clamp01(stoneCharge));
                var visualScale=rock.visual.transform.lossyScale;
                var size=rock.visual.sprite.bounds.size;
                return new ProjectileProfile(rock.radius*scale,rock.baseMass*scale*scale,
                    new Vector2(size.x*Mathf.Abs(visualScale.x),size.y*Mathf.Abs(visualScale.y))*scale);
            }
            var circle=projectilePrefab.GetComponent<CircleCollider2D>();
            float radius=circle!=null ? circle.radius*Mathf.Max(Mathf.Abs(projectilePrefab.transform.lossyScale.x),Mathf.Abs(projectilePrefab.transform.lossyScale.y)) : projectilePrefab.radius;
            return new ProjectileProfile(radius,tuning.spellMass,Vector2.one*radius*2);
        }
        RaycastHit2D[] spawnHits=new RaycastHit2D[32];
        int CastSpawnPath(Vector2 origin,float radius,Vector2 direction,float distance)
        {
            while(true)
            {
                int count=Physics2D.CircleCastNonAlloc(origin,radius,direction,spawnHits,distance,(1<<8)|(1<<10)|(1<<11)|(1<<12));
                if(count<spawnHits.Length) return count;
                System.Array.Resize(ref spawnHits,spawnHits.Length*2);
            }
        }
        void Pulse(Vector2 p,Color c,float radius,float duration=.45f) => world.Pulse(p,c,radius,duration);
        void Feedback(Vector2 p,string s,Color c) => world.Feedback(p,s,c);
        public ThermoBody Shoot(ThermoBody caster,Vector2 direction,float heat,float stoneCharge=0)
        {
            if(!world.SimulationActive || direction.sqrMagnitude<.01f) return null;
            direction.Normalize();
            var profile=Profile(caster,stoneCharge);
            Vector2 spawn;
            if(!TryGetProjectileSpawnPosition(caster,direction,out spawn,profile.Radius)) return null;
            // Registration order is creation order. Both sides share the FIFO cap.
            while(registry.ProjectileCount>=Mathf.Max(1,tuning.maximumProjectiles))
            {
                var oldest=registry.OldestProjectile;
                oldest.gameObject.SetActive(false); UnityEngine.Object.Destroy(oldest.gameObject);
            }
            var shot=UnityEngine.Object.Instantiate(projectilePrefab,spawn,Quaternion.identity,entities);
            shot.gameObject.layer=8;
            bool stone=UsesStoneMagic(caster);
            bool playerShot=caster.kind==BodyKind.Player;
            shot.owner=caster;
            shot.shotHeat=heat*(playerShot ? Mathf.Max(0,tuning.playerProjectileHeatMultiplier) : 1);
            shot.temperature=playerShot ? (heat>0 ? tuning.playerFireProjectileTemperature : tuning.playerIceProjectileTemperature) : heat>0 ? 150 : -120;
            shot.baseMass=profile.Mass;
            shot.maxHealth=tuning.projectileHealth; shot.health=shot.maxHealth;
            shot.lifetime=0;
            shot.visual.sprite=ProjectileSprite(caster,heat);
            shot.baseColor=heat>0 ? new Color(1,.75f,.35f) : new Color(.65f,.94f,1);
            if(stone)
            {
                shot.radius=profile.Radius;
                var rootScale=shot.transform.lossyScale;
                shot.GetComponent<CircleCollider2D>().radius=profile.Radius/Mathf.Max(.01f,Mathf.Max(Mathf.Abs(rootScale.x),Mathf.Abs(rootScale.y)));
                var size=shot.visual.sprite.bounds.size;
                var parentScale=shot.visual.transform.parent.lossyScale;
                shot.visual.transform.localScale=new Vector3(profile.VisualSize.x/Mathf.Max(.01f,size.x*Mathf.Abs(parentScale.x)),profile.VisualSize.y/Mathf.Max(.01f,size.y*Mathf.Abs(parentScale.y)),1);
                float enlargement=profile.Radius/Mathf.Max(.01f,projectilePrefab.radius);
                if(shot.heatRing!=null) shot.heatRing.transform.localScale*=enlargement;
                shot.healthBarSize.x=profile.Radius*2/Mathf.Max(.01f,Mathf.Abs(rootScale.x));
                var back=shot.transform.Find("Health Back");
                if(back!=null) { var backScale=back.localScale; backScale.x=shot.healthBarSize.x; back.localScale=backScale; }
            }
            shot.ThermalStep(0);
            Physics2D.IgnoreCollision(shot.GetComponent<Collider2D>(),caster.GetComponent<Collider2D>());
            foreach(var b in bodies) if(b!=null && b.kind==BodyKind.Shield && b.owner==caster) b.GetComponent<ShieldFollower>().AllowExit(shot);
            world.StartCoroutine(RestoreCasterCollision(shot,caster));
            shot.Body.velocity=Vector2.zero;
            if(caster.kind!=BodyKind.Player) shot.ApplyForce(direction*(caster.kind==BodyKind.Boss ? tuning.bossProjectileImpulse : tuning.enemyProjectileImpulse),ForceMode2D.Impulse);
            metrics.shots++;
            if(caster.kind!=BodyKind.Player && ArenaAudio.Instance!=null) ArenaAudio.Instance.PlayWorld(ArenaAudio.Instance.enemyShot,spawn,.6f);
            return shot;
        }
        public Vector2 ProjectileSpawnPosition(ThermoBody caster,Vector2 direction,float stoneCharge=0)
        {
            Vector2 position;
            TryGetProjectileSpawnPosition(caster,direction,out position,Profile(caster,stoneCharge).Radius);
            return position;
        }
        public bool TryGetProjectileSpawnPosition(ThermoBody caster,Vector2 direction,out Vector2 position,float shotRadius=-1)
        {
            var bounds=caster.Collider.bounds;
            Vector2 origin=bounds.center;
            position=origin;
            if(direction.sqrMagnitude<.01f) return false;
            direction.Normalize();
            // Use actual world-space geometry, including user-edited offset/scale.
            var circle=caster.Collider as CircleCollider2D;
            float casterExtent=circle!=null ? Mathf.Max(bounds.extents.x,bounds.extents.y)
                : Mathf.Abs(direction.x)*bounds.extents.x+Mathf.Abs(direction.y)*bounds.extents.y;
            if(shotRadius<0) shotRadius=Profile(caster).Radius;
            float minimumDistance=casterExtent+shotRadius+.06f;
            float spawnDistance=minimumDistance+.12f;
            position=origin+direction*spawnDistance; // Preview stays ahead even if creation is blocked.
            // Shorten against obstacles only while the ball still fits outside the caster.
            int hits=CastSpawnPath(origin,shotRadius,direction,spawnDistance);
            for(int i=0;i<hits;i++)
            {
                var hit=spawnHits[i];
                var body=hit.collider.GetComponentInParent<ThermoBody>();
                if(body==caster || (body!=null && body.kind==BodyKind.Shield && body.owner==caster)) continue;
                spawnDistance=Mathf.Min(spawnDistance,hit.distance-.03f);
            }
            if(spawnDistance<minimumDistance) return false;
            position=origin+direction*spawnDistance;
            return true;
        }
        IEnumerator RestoreCasterCollision(ThermoBody shot,ThermoBody caster)
        {
            while(shot!=null && caster!=null)
            {
                var separation=shot.Collider.Distance(caster.Collider);
                if(separation.isValid && !separation.isOverlapped && separation.distance>.03f) break;
                yield return new WaitForFixedUpdate();
            }
            if(shot!=null && caster!=null) Physics2D.IgnoreCollision(shot.Collider,caster.Collider,false);
        }
        public void CreateShields(ThermoBody caster)
        {
            // The pressure pulse acts before the physical walls form. It changes
            // velocity through a real impulse and gives enemy AI time to recover.
            foreach(var b in bodies.ToArray())
            {
                if(b==null || b.Dead || !b.IsEnemy || b.Body.bodyType!=RigidbodyType2D.Dynamic) continue;
                Vector2 delta=b.Body.position-caster.Body.position;
                if(delta.magnitude>tuning.shieldRadius+b.radius+.75f) continue;
                Vector2 away=delta.sqrMagnitude>.001f ? delta.normalized : Vector2.up;
                float outward=Vector2.Dot(b.Body.velocity,away);
                b.ApplyForce(away*Mathf.Max(0,tuning.shieldRepelSpeed-outward)*b.Mass,ForceMode2D.Impulse);
                var ai=b.GetComponent<EnemyBrain>(); if(ai!=null) ai.InterruptAttack(.75f);
            }
            Pulse(caster.Body.position,new Color(.7f,.5f,1),2.3f,tuning.shieldFormationDelay);
            world.StartCoroutine(FormShields(caster));
        }
        IEnumerator FormShields(ThermoBody caster)
        {
            yield return new WaitForSeconds(tuning.shieldFormationDelay);
            if(caster==null || caster.Dead || !world.SimulationActive) yield break;
            for(int i=0;i<4;i++)
            {
                Vector2 d=i==0 ? Vector2.right : i==1 ? Vector2.left : i==2 ? Vector2.up : Vector2.down;
                var s=UnityEngine.Object.Instantiate(shieldPrefab,caster.Body.position+d*tuning.shieldRadius,Quaternion.Euler(0,0,i<2 ? 90 : 0),entities);
                s.owner=caster; s.lifetime=tuning.shieldLifetime; s.GetComponent<ShieldFollower>().offset=d*tuning.shieldRadius;
                Physics2D.IgnoreCollision(s.GetComponent<Collider2D>(),caster.GetComponent<Collider2D>());
                foreach(var b in bodies) if(b!=null && (b.kind==BodyKind.Rock || b.kind==BodyKind.Projectile) && (b.lastInteractor==caster || b.owner==caster) &&
                    Vector2.Distance(b.Body.position,caster.Body.position)<tuning.shieldRadius+b.radius+.2f)
                    s.GetComponent<ShieldFollower>().AllowExit(b);
                // A wall-pinned enemy can still leave the enclosure after the
                // pulse; its shield collision resumes only after it is outside.
                foreach(var b in bodies) if(b!=null && !b.Dead && b.IsEnemy &&
                    Vector2.Distance(b.Body.position,caster.Body.position)<tuning.shieldRadius+b.radius+.2f)
                    s.GetComponent<ShieldFollower>().AllowExit(b);
            }
            Pulse(caster.Body.position,new Color(.7f,.5f,1),1.5f);
        }
        public ThermoBody Melee(ThermoBody caster,Vector2 direction)
        {
            if(direction.sqrMagnitude<.01f) return null;
            direction.Normalize(); Vector2 origin=caster.Collider.bounds.center;
            Vector2 center=origin+direction*.85f; ThermoBody nearest=null; float best=10;
            foreach(var col in Physics2D.OverlapCircleAll(center,1.15f,(1<<8)|(1<<10)))
            {
                var target=col.GetComponentInParent<ThermoBody>();
                if(target==null || target==caster || target.Dead || target.kind==BodyKind.Shield) continue;
                Vector2 delta=col.ClosestPoint(origin)-origin;
                if(delta.sqrMagnitude<.01f) delta=(Vector2)col.bounds.center-origin;
                if(Vector2.Dot(delta.normalized,direction)<.4f || delta.magnitude>1.65f) continue;
                if(delta.magnitude<best) { best=delta.magnitude; nearest=target; }
            }
            Pulse(center,new Color(1,.88f,.6f),.65f,.18f);
            if(nearest!=null)
            {
                if(nearest.Body.bodyType==RigidbodyType2D.Dynamic) nearest.ApplyForce(direction*tuning.staffImpulse,ForceMode2D.Impulse);
                nearest.Damage(tuning.staffDamage,"法杖钝击",DamageKind.Staff); nearest.lastInteractor=caster;
                if(nearest.kind==BodyKind.Rock || nearest.kind==BodyKind.Projectile)
                    foreach(var shield in bodies) if(shield!=null && shield.kind==BodyKind.Shield && shield.owner==caster)
                        shield.GetComponent<ShieldFollower>().AllowExit(nearest);
                Feedback(nearest.Body.position,nearest.IsEnemy ? "法杖钝击 "+Mathf.RoundToInt(tuning.staffDamage)+" + 推动" : "推动 → 撞击",new Color(1,.86f,.55f));
            }
            return nearest;
        }
    }
}
