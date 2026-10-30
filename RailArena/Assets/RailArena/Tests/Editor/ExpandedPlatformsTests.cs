using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class ExpandedPlatformsTests
    {
        [UnityTest]
        public IEnumerator AddedRoutesHaveClearanceAndUsableCover()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/TwinPlatforms.unity"); yield return new EnterPlayMode(); yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>(); session.StartSession(true,1,1,TargetScenario.Stationary); session.SetPaused(true);
            foreach(var actor in session.Actors) actor.Bot=false;
            while(!session.Match.IsActive) session.Match.Advance();
            var world=session.World; Assert.That(world.Nodes.Length,Is.EqualTo(42));
            for(int i=24;i<world.Nodes.Length;i++)
            {
                Vector3 p=world.Nodes[i].position;
                Assert.That(Physics.Raycast(p+Vector3.up,Vector3.down,2,1<<0,QueryTriggerInteraction.Ignore),Is.True,"No support under "+i);
                Assert.That(Physics.CheckCapsule(p+Vector3.up*.46f,p+Vector3.up*1.4f,.38f,1<<0,QueryTriggerInteraction.Ignore),Is.False,"Blocked route node "+i);
                for(int goal=0;goal<world.Nodes.Length;goal++) if(goal!=i) Assert.That(world.NextNode(i,goal),Is.Not.EqualTo(i));
            }
            foreach(var link in world.Links)
            {
                if(link.x<24&&link.y<24) continue;
                Vector3 a=world.Nodes[link.x].position,b=world.Nodes[link.y].position,d=b-a;
                Assert.That(Physics.CapsuleCast(a+Vector3.up*.46f,a+Vector3.up*1.4f,.38f,d.normalized,d.magnitude,1<<0,QueryTriggerInteraction.Ignore),Is.False,$"Blocked new link {link.x}->{link.y}");
            }
            foreach(int side in new[]{-1,1})
            {
                var deck=world.transform.Find(side<0?"BLUE PLATFORM":"RED PLATFORM").Find("Floating deck").GetComponent<Collider>();
                Assert.That(deck.bounds.size.x,Is.EqualTo(44).Within(.01f)); Assert.That(deck.bounds.size.z,Is.EqualTo(56).Within(.01f));
                Assert.That(deck.GetComponent<Renderer>().sharedMaterial.mainTexture,Is.Not.Null);
                Assert.That(Physics.Raycast(new Vector3(side*51,1.5f,0),Vector3.forward,4.1f,1<<0,QueryTriggerInteraction.Ignore),Is.True,"Full-height cover must stop fire");
            }
            session.enabled=false;
            var capture=typeof(TwinPlatformsTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var view in new[]{("Expanded-Overview",new Vector3(78,52,-80),Vector3.zero),
                ("Expanded-BlueFlank",new Vector3(-57,1.7f,-20),new Vector3(-45,1.5f,12)),
                ("Expanded-RedFlank",new Vector3(58,2,-23),new Vector3(30,1.5f,10))})
            {
                session.View.transform.position=view.Item2; session.View.transform.LookAt(view.Item3);
                foreach(var gate in world.Portals) gate.GetComponent<PortalView>().RenderView(session.View);
                capture.Invoke(null,new object[]{session.View,view.Item1});
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Restore(){if(Application.isPlaying) yield return new ExitPlayMode();}
    }
}
