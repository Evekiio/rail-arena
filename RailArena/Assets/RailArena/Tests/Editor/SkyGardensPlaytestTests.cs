using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RailArena.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class SkyGardensPlaytestTests
    {
        private static IEnumerator Frames(int count){int end=Time.frameCount+count;double deadline=EditorApplication.timeSinceStartup+8;while(Time.frameCount<end){Assert.That(EditorApplication.timeSinceStartup,Is.LessThan(deadline));EditorApplication.QueuePlayerLoopUpdate();yield return null;}}
        private static void Advance(ArenaSession session,int ticks){var method=typeof(ArenaSession).GetMethod("AdvanceValidationTick",BindingFlags.Instance|BindingFlags.NonPublic);for(int i=0;i<ticks;i++)method.Invoke(session,null);}
        [UnityTest] public IEnumerator ConfigEditsAreOfflineScopedResettableAndVisible()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");yield return new EnterPlayMode();yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();session.StartSession(true,1,1,TargetScenario.Stationary);session.SetPaused(false);session.TogglePhysicsConfig();
            Assert.That(session.PhysicsConfigOpen&&session.Paused,Is.True);int tick=session.Match.Tick;yield return Frames(3);Assert.That(session.Match.Tick,Is.EqualTo(tick));
            var input=GameObject.Find("GRAVITY m/s2 input").GetComponent<UnityEngine.UI.InputField>();input.onEndEdit.Invoke("32");Assert.That(session.Tuning.Gravity,Is.EqualTo(32));
            input.onEndEdit.Invoke("NaN");Assert.That(session.Tuning.Gravity,Is.EqualTo(32));
            GameObject.Find("SLIDE DURATION ticks slider").GetComponent<UnityEngine.UI.Slider>().value=60;Assert.That(session.Tuning.SlideDurationTicks,Is.EqualTo(60));
            StringAssert.Contains("\"gravity\": 32",session.PhysicsConfigJson());StringAssert.Contains("offline playtest",session.PhysicsConfigJson());
            yield return BrandUiTests.Review(session.View,"Sky-PhysicsLab");
            GameObject.Find("RESET STANDARD").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Assert.That(session.Tuning.Gravity,Is.EqualTo(25));Assert.That(session.Tuning.SlideDurationTicks,Is.EqualTo(36));
            session.ClosePhysicsConfig();Assert.That(session.Paused,Is.False);
            session.Tuning.Gravity=32;int port;using(var socket=new System.Net.Sockets.UdpClient(0))port=((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
            session.HostDuel(port);Assert.That(session.IsOnline,Is.True);Assert.That(session.Tuning.Gravity,Is.EqualTo(25));session.TogglePhysicsConfig();
            Assert.That(GameObject.Find("GRAVITY m/s2 input").GetComponent<UnityEngine.UI.InputField>().interactable,Is.False);
            GameObject.Find("GRAVITY m/s2 slider").GetComponent<UnityEngine.UI.Slider>().onValueChanged.Invoke(40);Assert.That(session.Tuning.Gravity,Is.EqualTo(25));
            session.ClosePhysicsConfig();session.MainMenu();yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator AllPadsRampsAndPortalsUseActualControllers()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");yield return new EnterPlayMode();yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();session.StartSession(true,1,1,TargetScenario.Stationary);session.SetPaused(true);
            foreach(var contender in session.Actors)contender.Bot=false;while(!session.Match.IsActive)session.Match.Advance();
            var actor=session.Human;var world=session.World;session.Actors[1].Place(world.Spawns[7].position);
            Assert.That(session.Tuning.GroundSpeed,Is.EqualTo(8));Assert.That(session.Tuning.GroundAcceleration,Is.EqualTo(8));Assert.That(session.Tuning.SlideMinimumSpeed,Is.LessThan(session.Tuning.GroundSpeed*session.Tuning.SprintSpeedMultiplier));
            Assert.That(world.Pads.Length,Is.EqualTo(4));Assert.That(world.Pickups.Length,Is.EqualTo(3));Assert.That(world.Portals.Length,Is.EqualTo(2));Assert.That(world.Nodes.Length,Is.GreaterThan(100));
            foreach(var point in world.Spawns)Assert.That(Physics.CheckCapsule(point.position+Vector3.up*.42f,point.position+Vector3.up*1.38f,.34f,1<<0,QueryTriggerInteraction.Ignore),Is.False,"Spawn blocked: "+point.name);
            foreach(var pad in world.Pads)
            {
                actor.Place(pad.transform.position+Vector3.up*.08f);actor.PadReadyTick=0;actor.Command=default;int launches=session.PadLaunches;Advance(session,134);
                Assert.That(session.PadLaunches,Is.EqualTo(launches+1));Assert.That(actor.transform.position.y,Is.InRange(21.94f,22.15f),pad.name+" failed summit landing: "+actor.transform.position);Assert.That(actor.Grounded,Is.True);
            }
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})
            {
                actor.Place(new Vector3(side*26,.08f,end*8));actor.Command=new MovementCommand{Move=Vector2.up,Yaw=side<0?90:270};for(int tick=0;tick<150 && actor.transform.position.y<=3.85f;tick++)Advance(session,1);
                Assert.That(actor.transform.position.y,Is.GreaterThan(3.85f),"Ramp failed "+side+"/"+end+" at "+actor.transform.position);
            }
            foreach(var gate in world.Portals)
            {
                actor.Place(gate.transform.position+gate.transform.forward*2+Vector3.up*.08f);actor.Command=new MovementCommand{Move=Vector2.up,Yaw=Quaternion.LookRotation(-gate.transform.forward).eulerAngles.y};int serial=actor.TeleportSerial;Advance(session,22);
                Assert.That(actor.TeleportSerial,Is.EqualTo(serial+1),gate.name+" did not traverse");
                Assert.That(Vector3.Distance(actor.transform.position,gate.Exit.transform.position),Is.LessThan(6));
                Assert.That(Physics.Raycast(gate.transform.position+gate.transform.forward*3+Vector3.up*1.1f,-gate.transform.forward,out var hit,5,1<<0,QueryTriggerInteraction.Collide),Is.True);Assert.That(hit.collider.isTrigger,Is.True);
            }
            // Crown and Underpass must stay separate, with floors facing up.
            Assert.That(Physics.Raycast(new Vector3(0,1.8f,8),Vector3.down,out var ground,3,1<<0,QueryTriggerInteraction.Ignore),Is.True);Assert.That(ground.normal.y,Is.GreaterThan(.99f));
            var start=world.NearestNode(world.Spawns[0].position);var reached=new HashSet<int>{start};var queue=new Queue<int>();queue.Enqueue(start);
            while(queue.Count>0){int node=queue.Dequeue();foreach(var link in world.Links)if(link.x==node&&reached.Add(link.y))queue.Enqueue(link.y);}
            foreach(var pickup in world.Pickups)Assert.That(reached.Contains(world.NearestNode(pickup.transform.position)),Is.True,"Health route disconnected "+pickup.name);
            foreach(var pad in world.Pads)Assert.That(reached.Contains(world.NearestNode(pad.Destination)),Is.True,"Summit route disconnected");
            session.enabled=false;var capture=typeof(TwinPlatformsTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var view in new[]{("Sky-Overview",new Vector3(48,40,-55),new Vector3(0,8,0)),("Sky-Garden",new Vector3(25,2,-16),new Vector3(29,2,-16)),("Sky-Crown",new Vector3(-8,5.7f,7),new Vector3(4,5,0)),("Sky-Summit",new Vector3(6,24,-5),new Vector3(0,23,0))})
            {
                session.View.transform.position=view.Item2;session.View.transform.LookAt(view.Item3);session.View.fieldOfView=75;
                foreach(var gate in world.Portals)gate.GetComponent<PortalView>().RenderView(session.View);
                capture.Invoke(null,new object[]{session.View,view.Item1});
            }
            foreach(var renderer in world.GetComponentsInChildren<Renderer>())if(renderer.sharedMaterial!=null)Assert.That(ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader),Is.False);
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator BotsTraverseSkyGardensWithoutVoidDeaths()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");yield return new EnterPlayMode();yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();session.AutomatedHuman=true;session.StartSession(true,3,1,TargetScenario.Moving);session.SetPaused(true);
            Advance(session,3780);int portals=session.Actors.Sum(actor=>actor.TeleportSerial);
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/SkyGardens-Playable-2026-10-29"));Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"bot-traversal.txt"),$"nodes={session.World.Nodes.Length} portals={portals} pads={session.PadLaunches} voids={session.VoidDeaths}\n"+string.Join("\n",session.VoidTraces));
            Assert.That(session.VoidDeaths,Is.Zero);foreach(var actor in session.Actors)Assert.That(actor.Brain.NodesReached,Is.GreaterThan(3));Assert.That(session.PadLaunches,Is.GreaterThan(0));
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator GAndEscapeReleaseInputAndExportExactJson()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");yield return new EnterPlayMode();yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();session.StartSession(true,1,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();
            var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;var settings=InputSystem.settings;var oldInput=settings.editorInputBehaviorInPlayMode;var oldBackground=settings.backgroundBehavior;Keyboard keyboard=null;Mouse mouse=null;
            try
            {
                settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                keyboard=InputSystem.AddDevice<Keyboard>("Physics lab test keyboard");keyboard.MakeCurrent();mouse=InputSystem.AddDevice<Mouse>("Physics lab test mouse");mouse.MakeCurrent();
                int shots=session.ShotsFired;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.G));InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));yield return Frames(2);
                Assert.That(session.PhysicsConfigOpen&&session.Paused,Is.True);Assert.That(session.ShotsFired,Is.EqualTo(shots));Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));
                string folder=Path.Combine(Application.persistentDataPath,"TuningSnapshots");var before=Directory.Exists(folder)?Directory.GetFiles(folder,"*.json"):Array.Empty<string>();
                var export=(IEnumerator)typeof(ArenaHud).GetMethod("SavePhysicsConfig",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(session.GetComponent<ArenaHud>(),null);
                Assert.That(export.MoveNext(),Is.True);Assert.That(export.Current,Is.TypeOf<WaitForEndOfFrame>());(export as IDisposable)?.Dispose();
                var written=Directory.GetFiles(folder,"*.json").Except(before).ToArray();Assert.That(written.Length,Is.EqualTo(1));StringAssert.Contains("\"groundSpeed\": 8",File.ReadAllText(written[0]));File.Delete(written[0]);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState());yield return Frames(2);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.G));yield return Frames(2);Assert.That(session.PhysicsConfigOpen,Is.False);Assert.That(session.Paused,Is.False);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames(2);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.G));yield return Frames(2);Assert.That(session.PhysicsConfigOpen,Is.True);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames(2);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return Frames(2);Assert.That(session.PhysicsConfigOpen,Is.False);
            }
            finally
            {
                if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);oldKeyboard?.MakeCurrent();oldMouse?.MakeCurrent();settings.editorInputBehaviorInPlayMode=oldInput;settings.backgroundBehavior=oldBackground;
            }
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator PhysicalCameraPreservesVerticalFovAndAdsAcrossAspectRatios()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity"); yield return new EnterPlayMode(); yield return Frames(2);
            var session = Object.FindAnyObjectByType<ArenaSession>(); var camera = session.View;
            Assert.That(camera.usePhysicalProperties, Is.True);
            var setter = typeof(ArenaSession).GetMethod("SetViewFov", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach(float aspect in new[]{ 16f/9f, 21f/9f, 4f/3f }) foreach(float fov in new[]{70f,100f,110f})
            {
                camera.aspect = aspect; setter.Invoke(session, new object[]{fov});
                Assert.That(camera.projectionMatrix.m11, Is.EqualTo(1f / Mathf.Tan(fov * .5f * Mathf.Deg2Rad)).Within(.0001f));
                session.Aim.FieldOfView=fov; setter.Invoke(session, new object[]{session.Aim.ViewFov(true)});
                float expectedZoom = 1.5f / Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
                Assert.That(camera.projectionMatrix.m11, Is.EqualTo(expectedZoom).Within(.0001f));
            }
            camera.ResetAspect(); yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Restore(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}



