using System;
using System.IO;
using RailArena.Simulation;
using UnityEngine;
namespace RailArena
{
    public sealed partial class ArenaSession
    {
        public bool PhysicsConfigOpen { get; private set; }
        private MovementTuning ownedTuning;
        private string defaultTuning;
        private bool configWasPaused;
        private void InitializePhysicsConfig()
        {
            ownedTuning=Instantiate(Tuning);ownedTuning.name="Local playtest physics";Tuning=ownedTuning;
            defaultTuning=JsonUtility.ToJson(Tuning);
        }
        public void TogglePhysicsConfig()
        {
            if(PhysicsConfigOpen) { ClosePhysicsConfig();return; }
            configWasPaused=Paused;PhysicsConfigOpen=true;
            if(!IsOnline) SetPaused(true);
            else { if(Human!=null) Human.Command=new MovementCommand{Yaw=yaw,Pitch=pitch};SetPaused(true); }
            hud.ShowPhysicsConfig();
        }
        public void ClosePhysicsConfig()
        {
            if(!PhysicsConfigOpen)return;
            PhysicsConfigOpen=false;SetPaused(configWasPaused);hud.HidePhysicsConfig();
        }
        public void ResetPhysicsConfig()
        {
            if(IsOnline)return;
            JsonUtility.FromJsonOverwrite(defaultTuning,Tuning);
            foreach(var actor in Actors)actor.Slide=default;
        }
        [Serializable] private sealed class PhysicsSnapshot
        {
            public string createdUtc,buildIdentity,map,scope;
            public int tickRate=60;
            public float standingHeight=1.8f,crouchHeight=1.2f,radius=.36f,standingEye=1.58f,crouchEye=.98f;
            public MovementTuningValues values;
        }
        [Serializable] private sealed class MovementTuningValues
        {
            public float groundSpeed,groundAcceleration,airAcceleration,friction,stopSpeed,jumpSpeed,gravity,sprintMultiplier,walkMultiplier,crouchMultiplier;
            public int slideDurationTicks,slideCooldownTicks;
            public float slideMinimumSpeed,slideDeceleration,slideSteeringDegrees,stepOffset,slopeLimit,skinWidth,padFlightMultiplier;
        }
        public string PhysicsConfigJson()
        {
            var t=Tuning;
            return JsonUtility.ToJson(new PhysicsSnapshot{createdUtc=DateTime.UtcNow.ToString("O"),buildIdentity=Networking.BuildIdentity.Fingerprint,map=World.LayoutName,scope=IsOnline?"online standard values (read only)":"offline playtest; session only",
                values=new MovementTuningValues{groundSpeed=t.GroundSpeed,groundAcceleration=t.GroundAcceleration,airAcceleration=t.AirAcceleration,friction=t.Friction,stopSpeed=t.StopSpeed,jumpSpeed=t.JumpSpeed,gravity=t.Gravity,sprintMultiplier=t.SprintSpeedMultiplier,walkMultiplier=t.WalkSpeedMultiplier,crouchMultiplier=t.CrouchSpeedMultiplier,slideDurationTicks=t.SlideDurationTicks,slideCooldownTicks=t.SlideCooldownTicks,slideMinimumSpeed=t.SlideMinimumSpeed,slideDeceleration=t.SlideDeceleration,slideSteeringDegrees=t.SlideSteeringDegrees,stepOffset=t.StepOffset,slopeLimit=t.SlopeLimit,skinWidth=t.SkinWidth,padFlightMultiplier=t.PadFlightMultiplier}},true);
        }
    }
}
