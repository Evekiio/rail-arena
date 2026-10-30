using System;
using System.Linq;
using NUnit.Framework;
using RailArena.Simulation;
using RailArena.Networking;

namespace RailArena.Tests
{
    public sealed class CombatStatisticsTests
    {
        [Test]
        public void AccuracyKillsAndVoidPenaltiesAreDifferentMeasures()
        {
            var match=new MatchState(new[]{0,1},true);
            Assert.That(match.RecordShot(0,HitRegion.Head), Is.False);
            for(int i=0;i<CombatRules.CountdownTicks;i++)match.Advance();
            match.RecordShot(0,null); match.RecordShot(0,HitRegion.Body); match.RecordShot(0,HitRegion.Head);
            match.RecordKill(0,1); match.RecordVoidDeath(0);
            var stats=match.Statistics[0];
            Assert.That(stats.Shots,Is.EqualTo(3)); Assert.That(stats.Hits,Is.EqualTo(2));
            Assert.That(stats.Headshots,Is.EqualTo(1)); Assert.That(stats.Accuracy,Is.EqualTo(200f/3).Within(.001f));
            Assert.That(stats.Kills,Is.EqualTo(1)); Assert.That(stats.Deaths,Is.EqualTo(1));
            Assert.That(stats.VoidDeaths,Is.EqualTo(1)); Assert.That(match.Scores[0],Is.Zero);
            Assert.That(match.Statistics[1].Deaths,Is.EqualTo(1));
            Assert.That(match.Statistics[1].Accuracy,Is.Zero);
            Assert.That(match.RecordShot(99,HitRegion.Body),Is.False);
            Assert.That(match.RecordShot(0,(HitRegion)255),Is.False);
            Assert.That(match.RecordKill(0,0),Is.False);
        }

        [Test]
        public void WinningShotIsCountedBeforeResultsAndRematchStartsEmpty()
        {
            var match=new MatchState(new[]{0,1},false);
            while(match.Phase!=MatchPhase.SuddenDeath)match.Advance();
            match.RecordShot(1,HitRegion.Head); match.RecordKill(1,0);
            Assert.That(match.Phase,Is.EqualTo(MatchPhase.Results));
            Assert.That(match.Statistics[1].Shots,Is.EqualTo(1));
            Assert.That(match.Statistics[1].Headshots,Is.EqualTo(1));
            Assert.That(match.RecordShot(1,null),Is.False);
            Assert.That(match.RecordVoidDeath(0),Is.False);
            var rematch=new MatchState(new[]{0,1},false);
            Assert.That(rematch.Statistics[1].Shots,Is.Zero);
            Assert.That(rematch.Statistics[0].Deaths,Is.Zero);
        }

        [Test]
        public void FullAuthoritySnapshotsReplaceTotalsWithoutDuplicateOrLostPacketDrift()
        {
            var stats=new CombatStatistics(2,3,15,7,4,1);
            var replica=MatchState.FromAuthority(1000,MatchPhase.Playing,-1,1,0,stats,default);
            replica=MatchState.FromAuthority(1000,MatchPhase.Playing,-1,1,0,stats,default);
            Assert.That(replica.Statistics[0].Shots,Is.EqualTo(15));
            var later=new CombatStatistics(3,3,20,10,5,1);
            replica=MatchState.FromAuthority(1300,MatchPhase.Playing,-1,2,0,later,default);
            Assert.That(replica.Statistics[0].Kills,Is.EqualTo(3));
            Assert.That(replica.Statistics[0].Shots,Is.EqualTo(20));
            Assert.That(replica.Statistics[0].Accuracy,Is.EqualTo(50));
        }

        [Test]
        public void ProtocolRoundTripsStatisticsAndRejectsImpossibleCountersAndOldVersion()
        {
            var snapshot=new DuelSnapshot {Round=1,Tick=1000,Phase=MatchPhase.Playing};
            snapshot.Actors[0]=new ActorSnapshot {Life=1,Health=100,Statistics=new CombatStatistics(2,3,15,7,4,1)};
            snapshot.Actors[1]=new ActorSnapshot {Life=1,Health=100};
            var bytes=DuelProtocol.Encode(new DuelPacket {Kind=DuelMessage.Snapshot,Snapshot=snapshot});
            Assert.That(bytes.Length,Is.LessThan(DuelProtocol.MaximumDatagram));
            Assert.That(DuelProtocol.TryDecode(bytes,out var decoded),Is.True);
            Assert.That(decoded.Snapshot.Actors[0].Statistics.Headshots,Is.EqualTo(4));
            Assert.That(decoded.Snapshot.Actors[0].Statistics.VoidDeaths,Is.EqualTo(1));
            for(int i=0;i<bytes.Length;i++)Assert.That(DuelProtocol.TryDecode(bytes.Take(i).ToArray(),out _),Is.False);
            var malformed=(byte[])bytes.Clone();
            // Last actor's statistics precede the three pickup ticks in the fixed layout.
            int statisticsOffset=bytes.Length-12-24;
            BitConverter.GetBytes(-1).CopyTo(malformed,statisticsOffset+8);
            Assert.That(DuelProtocol.TryDecode(malformed,out _),Is.False);
            malformed=(byte[])bytes.Clone();
            BitConverter.GetBytes(1).CopyTo(malformed,statisticsOffset+12);
            Assert.That(DuelProtocol.TryDecode(malformed,out _),Is.False,"A hit cannot exceed zero shots.");
            bytes[4]=(byte)(DuelProtocol.Version-1);
            Assert.That(DuelProtocol.TryDecode(bytes,out _),Is.False);
            Assert.Throws<ArgumentException>(()=>new CombatStatistics(0,0,1,2,0,0));
            Assert.Throws<ArgumentException>(()=>new CombatStatistics(0,0,1,1,2,0));
            Assert.Throws<ArgumentException>(()=>new CombatStatistics(0,0,0,0,0,1));
            Assert.Throws<ArgumentException>(()=>new CombatStatistics(-1,0,0,0,0,0));
        }
    }
}
