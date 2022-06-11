

using System.Collections.Generic;
using System.Numerics;

namespace LibreLancer
{
    public class SPlayerComponent : GameComponent
    {
        public Queue<InputUpdatePacket> Inputs = new Queue<InputUpdatePacket>();
        public int SequenceApplied = 0;
        
        public Player Player { get; private set; }
        public SPlayerComponent(Player player, GameObject parent) : base(parent)
        {
            this.Player = player;
        }

        public void Killed()
        {
            Player.Killed();
        }

        public override void Update(double time)
        {
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var phys))
            {
                if (Inputs.Count > 10) { //Skip an update
                    FLLog.Info("Server", $"Receiving updates too fast, throwing away #{Inputs.Peek().Sequence}");
                    Inputs.Dequeue();
                }
                if (Inputs.Count >= 5)
                {
                    var input = Inputs.Dequeue();
                    if (Player.InTradelane)
                    {
                        phys.Steering = Vector3.Zero;
                        phys.CurrentStrafe = StrafeControls.None;
                        phys.EnginePower = 0;
                        phys.ThrustEnabled = false;
                        phys.CruiseEnabled = false;
                    }
                    else
                    {
                        phys.Steering = input.Steering;
                        phys.CurrentStrafe = input.Strafe;
                        phys.EnginePower = input.Throttle;
                        phys.ThrustEnabled = input.Thrust;
                        phys.CruiseEnabled = input.Cruise;
                    }
                    phys.Tick = input.Sequence;
                    SequenceApplied = input.Sequence;
                } else
                {
                    FLLog.Info("Server", $"No input to apply, repeating #{SequenceApplied}");
                }
            }
        }
    }
}