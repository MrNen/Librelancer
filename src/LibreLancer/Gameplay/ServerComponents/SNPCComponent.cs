using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.AI;

namespace LibreLancer
{
    public class SNPCComponent : GameComponent
    {
        public AiState CurrentState;
        public NetShipLoadout Loadout;
        private NPCManager manager;

        public Action<GameObject, GameObject> ProjectileHitHook;

        public List<GameObject> HostileNPCs = new List<GameObject>();

        public GameData.Pilot Pilot;
        
        public void OnProjectileHit(GameObject attacker)
        {
            ProjectileHitHook?.Invoke(Parent, attacker);
        }

        public SNPCComponent(GameObject parent, NPCManager manager) : base(parent)
        {
            this.manager = manager;
        }

        public void Killed()
        {
            manager.Despawn(Parent);
        }
        public void Docked()
        {
            manager.Despawn(Parent);
        }

        private GameObject attack;
        public void Attack(GameObject tgt)
        {
            SetState(new AiAttackState(tgt));
        }

        public void SetState(AiState state)
        {
            this.CurrentState = state;
            state?.OnStart(Parent, this);
        }

        private double fireTimer;
        
        public override void FixedUpdate(double time)
        {
            CurrentState?.Update(Parent, this, time);
            //Attack hostile
            GameObject shootAt = null;
            var myPos = Parent.WorldTransform.Translation;
            foreach (var other in Parent.GetWorld().SpatialLookup
                .GetNearbyObjects(Parent, myPos, 5000))
            {
                if (Vector3.Distance(other.WorldTransform.Translation, myPos) < 5000 &&
                    HostileNPCs.Contains(other))
                {
                    shootAt = other;
                    break;
                }
            }
            
            if (shootAt != null && Parent.TryGetComponent<WeaponControlComponent>(out var weapons))
            {
                var dist = Vector3.Distance(shootAt.WorldTransform.Translation, myPos);
                var range = weapons.GetMaxRange() * 0.95f;
                if (dist < range)
                {
                    fireTimer -= time;
                    weapons.AimPoint = Vector3.Transform(Vector3.Zero, shootAt.WorldTransform);
                    if (fireTimer <= 0)
                    {
                        weapons.FireAll();
                        fireTimer = Pilot?.Gun?.FireIntervalTime ?? 0;
                    }
                }
                else {
                    if (CurrentState == null && Parent.TryGetComponent<AutopilotComponent>(out var ap)) {
                        if (ap.CurrentBehaviour == AutopilotBehaviours.None)
                        {
                            ap.GotoObject(shootAt, false, 1, range * 0.5f);
                        }
                    }
                }
            }
            else
            {
                fireTimer = Pilot?.Gun?.FireIntervalTime ?? 0;
            }
        }

        public void DockWith(GameObject tgt)
        {
            SetState(new AiDockState(tgt));
        }
    }
}