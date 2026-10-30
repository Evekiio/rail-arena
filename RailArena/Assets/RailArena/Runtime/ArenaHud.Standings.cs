using System.Linq;
using System.Globalization;
using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private sealed class StandingRow
        {
            public GameObject Root;
            public UnityEngine.UI.Text Rank,Name,Points,Kd,Accuracy,Hits,Headshots;
        }

        private sealed class StandingsPanel
        {
            public GameObject Root;
            public StandingRow[] Rows = new StandingRow[Simulation.CombatRules.MaximumContenders];
        }

        private StandingsPanel liveStandings;

        private StandingsPanel CreateStandings(Transform parent,string name,string title,Vector2 min,Vector2 max)
        {
            var panel = new StandingsPanel { Root = Graphic(parent,name,min,max,new Color(.08f,.12f,.16f,.97f).linear).gameObject };
            Graphic(panel.Root.transform,"Accent",new Vector2(0,.996f),Vector2.one,cyan);
            Text(panel.Root.transform,"Title",title,25,new Vector2(.025f,.86f),new Vector2(.98f,.98f)).color=cyan;
            string[] labels={"#","CONTENDER","PTS","K / D","ACC","HITS / SHOTS","HS"};
            float[] starts={.025f,.09f,.36f,.45f,.57f,.70f,.91f};
            float[] ends={.075f,.35f,.44f,.56f,.69f,.90f,.985f};
            for(int i=0;i<labels.Length;i++)
                Text(panel.Root.transform,"Header "+i,labels[i],15,new Vector2(starts[i],.75f),new Vector2(ends[i],.84f));
            for(int i=0;i<panel.Rows.Length;i++)
            {
                float y=.61f-i*.14f;
                var root=Graphic(panel.Root.transform,"StandingRow"+i,new Vector2(.02f,y),new Vector2(.985f,y+.12f),Color.clear).gameObject;
                UnityEngine.UI.Text Cell(string cell,float left,float right)
                {
                    var value=Text(root.transform,cell,"",20,new Vector2(left,0),new Vector2(right,1));
                    value.alignment=TextAnchor.MiddleLeft;
                    value.resizeTextForBestFit=true; value.resizeTextMinSize=12; value.resizeTextMaxSize=20;
                    return value;
                }
                panel.Rows[i]=new StandingRow { Root=root,Rank=Cell("Rank",.005f,.055f),Name=Cell("Name",.073f,.342f),
                    Points=Cell("Points",.352f,.435f),Kd=Cell("KillsDeaths",.445f,.560f),Accuracy=Cell("Accuracy",.57f,.695f),
                    Hits=Cell("HitsShots",.705f,.915f),Headshots=Cell("Headshots",.925f,1) };
            }
            Text(panel.Root.transform,"Legend","PTS INCLUDE VOID PENALTIES / HS = HEAD HITS",14,new Vector2(.025f,.025f),new Vector2(.98f,.12f));
            return panel;
        }

        private void RefreshStandings(StandingsPanel panel)
        {
            var ranked=Session.Match.Scores.OrderByDescending(entry=>entry.Value).ThenBy(entry=>entry.Key).ToArray();
            int rank=0;
            for(int i=0;i<panel.Rows.Length;i++)
            {
                var row=panel.Rows[i]; row.Root.SetActive(i<ranked.Length);
                if(i>=ranked.Length)continue;
                var entry=ranked[i];
                if(i==0 || entry.Value!=ranked[i-1].Value)rank=i+1;
                var actor=Session.Actors.First(candidate=>candidate.Id==entry.Key);
                var stats=Session.Match.Statistics[entry.Key];
                row.Root.GetComponent<UnityEngine.UI.Image>().color=(entry.Key==Session.LocalPlayerId?new Color(.08f,.30f,.34f,.92f):new Color(.11f,.17f,.21f,.8f)).linear;
                row.Rank.text=rank.ToString(); row.Name.text=actor.DisplayName;
                row.Name.color=entry.Key==Session.Match.Winner?amber:entry.Key==Session.LocalPlayerId?cyan:Color.white;
                row.Points.text=entry.Value.ToString(); row.Kd.text=$"{stats.Kills} / {stats.Deaths}";
                row.Accuracy.text=stats.Accuracy.ToString("0.0",CultureInfo.InvariantCulture)+"%";
                row.Hits.text=$"{stats.Hits} / {stats.Shots}"; row.Headshots.text=stats.Headshots.ToString();
            }
        }
    }
}
