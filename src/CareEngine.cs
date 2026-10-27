using System;
using System.Linq;
using System.Collections.Generic;
namespace RestBuddy {
  internal sealed class CareEngine {
    private readonly Options options;private readonly Random random;
    private readonly Dictionary<string,double> effort=new Dictionary<string,double>(),threshold=new Dictionary<string,double>();
    private readonly Dictionary<string,DateTime> offered=new Dictionary<string,DateTime>();
    private readonly Dictionary<string,int> shown=new Dictionary<string,int>();
    internal DateTime QuietUntil=DateTime.MinValue;
    internal DateTime CooldownUntil {get;private set;}
    internal int BusyCount;
    internal CareEngine(Options value):this(value,new Random(),DateTime.UtcNow){}
    internal CareEngine(Options value,Random generator,DateTime now){options=value;random=generator;Reset(now);}
    private int Spacing {get{return options.Frequency=="轻声"?20:options.Frequency=="积极"?6:10;}}
    private double Factor {get{return options.Frequency=="轻声"?1.15:options.Frequency=="积极"?0.9:1;}}
    private void Renew(string key){Rule r=options.Rules.First(x=>x.Key==key);effort[key]=0;shown[key]=0;offered[key]=DateTime.MinValue;threshold[key]=r.Minutes*60*Factor*(0.9+random.NextDouble()*0.2);}
    internal void Reset(DateTime now){foreach(Rule r in options.Rules)Renew(r.Key);CooldownUntil=now.AddMinutes(3);BusyCount=0;}
    internal void Reconfigure(){foreach(Rule r in options.Rules){if(!effort.ContainsKey(r.Key))Renew(r.Key);else threshold[r.Key]=r.Minutes*60*Factor*(0.9+random.NextDouble()*0.2);}}
    internal void AddActive(double seconds){if(seconds<=0||seconds>30)return;foreach(Rule r in options.Rules)if(r.Enabled)effort[r.Key]+=seconds;}
    internal double MinutesFor(string key){return effort.ContainsKey(key)?effort[key]/60:0;}
    internal bool HasDue {get{return options.Rules.Any(r=>r.Enabled&&effort[r.Key]>=threshold[r.Key]);}}
    internal bool HasUrgent(DateTime now){return options.Rules.Any(r=>r.Enabled&&(r.Key=="eyes"||r.Key=="walk"||r.Key=="rest")&&effort[r.Key]>=threshold[r.Key]*1.7&&now>=offered[r.Key]);}
    internal void Rest(DateTime now){foreach(string key in new[]{"eyes","blink","posture","walk","stretch","rest","hands","feet"})if(effort.ContainsKey(key))Renew(key);CooldownUntil=now.AddMinutes(4);BusyCount=0;}
    internal void ScreenBreak(DateTime now){foreach(string key in new[]{"eyes","blink"})if(effort.ContainsKey(key))Renew(key);ResumeBuffer(now);}
    internal void Drank(DateTime now){Renew("water");CooldownUntil=now.AddMinutes(2);BusyCount=0;}
    internal void Completed(string key,DateTime now){if(effort.ContainsKey(key))Renew(key);CooldownUntil=now.AddMinutes(2);BusyCount=0;}
    internal void ResumeBuffer(DateTime now){if(CooldownUntil<now.AddMinutes(2))CooldownUntil=now.AddMinutes(2);}
    internal void Quiet(DateTime now,int minutes){QuietUntil=now.AddMinutes(minutes);CooldownUntil=QuietUntil;}
    internal void Resume(DateTime now){QuietUntil=DateTime.MinValue;CooldownUntil=now.AddMinutes(2);}
    internal void Busy(DateTime now){BusyCount=Math.Min(4,BusyCount+1);Quiet(now,20+BusyCount*10);}
    // Not answering a bubble is not evidence of resting or asking for an hour of silence.
    internal void Ignored(DateTime now){if(CooldownUntil<now.AddMinutes(2))CooldownUntil=now.AddMinutes(2);}
    internal void Going(DateTime now){Quiet(now,5);}
    private double Priority(string key){return key=="eyes"?0.6:key=="walk"||key=="rest"?0.45:key=="blink"?0.25:0;}
    internal List<Rule> Suggest(DateTime now){
      if(now<QuietUntil||now<CooldownUntil)return null;
      Rule primary=options.Rules.Where(r=>r.Enabled&&effort[r.Key]>=threshold[r.Key]&&now>=offered[r.Key]).OrderByDescending(r=>Math.Min(4,effort[r.Key]/threshold[r.Key])+Priority(r.Key)-shown[r.Key]*0.25).FirstOrDefault();
      if(primary==null)return null;List<Rule> result=new List<Rule>{primary};string second=null;
      if(primary.Key=="walk"||primary.Key=="rest"||primary.Key=="stretch")second=Eligible("water",0.8,now)?"water":Eligible("eyes",0.85,now)?"eyes":null;
      if(primary.Key=="water")second=Eligible("walk",0.85,now)?"walk":null;
      if(primary.Key=="eyes")second=Eligible("blink",0.7,now)?"blink":Eligible("posture",0.85,now)?"posture":null;
      if(second!=null)result.Add(options.Rules.First(r=>r.Key==second));
      foreach(Rule r in result){shown[r.Key]++;offered[r.Key]=now.AddMinutes(Math.Max(5,Math.Min(25,r.Minutes*0.65)));}
      CooldownUntil=now.AddMinutes(Spacing);return result;
    }
    private bool Eligible(string key,double ratio,DateTime now){return options.Rules.Any(r=>r.Key==key&&r.Enabled)&&effort[key]>=threshold[key]*ratio&&now>=offered[key];}
    internal string Report(){return String.Join("\n",options.Rules.Where(r=>r.Enabled).Select(r=>r.Name+"：本轮电脑使用约 "+Math.Floor(MinutesFor(r.Key))+" 分钟"));}
  }
}
