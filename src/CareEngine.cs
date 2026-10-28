using System;
using System.Linq;
using System.Collections.Generic;
namespace RestBuddy {
  internal sealed class CareEngine {
    private readonly Options options;private readonly Random random;
    private readonly Dictionary<string,double> effort=new Dictionary<string,double>(),threshold=new Dictionary<string,double>();
    private readonly Dictionary<string,DateTime> offered=new Dictionary<string,DateTime>();
    private readonly Dictionary<string,DateTime> lastOffered=new Dictionary<string,DateTime>();
    private readonly Dictionary<string,int> shown=new Dictionary<string,int>();
    internal DateTime QuietUntil=DateTime.MinValue;
    internal DateTime CooldownUntil {get;private set;}
    private DateTime lastPrompt=DateTime.MinValue,feedbackBuffer=DateTime.MinValue;
    private string lastPrimary="";
    internal int BusyCount;
    internal CareEngine(Options value):this(value,new Random(),DateTime.UtcNow){}
    internal CareEngine(Options value,Random generator,DateTime now){options=value;random=generator;Reset(now);}
    private int Spacing {get{return options.Frequency=="轻声"?20:options.Frequency=="积极"?6:10;}}
    private double Factor {get{return options.Frequency=="轻声"?1.15:options.Frequency=="积极"?0.9:1;}}
    private double Threshold(Rule r){return r.Minutes*60*(r.Key=="eyes"?1:Factor*(0.9+random.NextDouble()*0.2));}
    private void Renew(string key){Rule r=options.Rules.First(x=>x.Key==key);effort[key]=0;shown[key]=0;offered[key]=DateTime.MinValue;lastOffered[key]=DateTime.MinValue;threshold[key]=Threshold(r);}
    internal void Reset(DateTime now){foreach(Rule r in options.Rules)Renew(r.Key);CooldownUntil=now.AddMinutes(3);lastPrompt=DateTime.MinValue;feedbackBuffer=DateTime.MinValue;lastPrimary="";BusyCount=0;}
    internal void Reconfigure(){foreach(Rule r in options.Rules){if(!effort.ContainsKey(r.Key))Renew(r.Key);else threshold[r.Key]=Threshold(r);}}
    internal void AddActive(double seconds){if(seconds<=0||seconds>30)return;foreach(Rule r in options.Rules)if(r.Enabled)effort[r.Key]+=seconds;}
    internal double MinutesFor(string key){return effort.ContainsKey(key)?effort[key]/60:0;}
    internal bool HasDue {get{return options.Rules.Any(r=>r.Enabled&&effort[r.Key]>=threshold[r.Key]);}}
    internal bool HasUrgent(DateTime now){return options.Rules.Any(r=>r.Enabled&&(r.Key=="eyes"||r.Key=="rest")&&effort[r.Key]>=threshold[r.Key]*1.7&&now>=offered[r.Key]);}
    internal void Rest(DateTime now){foreach(string key in new[]{"eyes","posture","rest"})if(effort.ContainsKey(key))Renew(key);CooldownUntil=now.AddMinutes(4);BusyCount=0;}
    internal void ScreenBreak(DateTime now){if(effort.ContainsKey("eyes"))Renew("eyes");ResumeBuffer(now);}
    internal void Drank(DateTime now){Renew("water");CooldownUntil=now.AddMinutes(2);feedbackBuffer=CooldownUntil;BusyCount=0;}
    internal void Completed(string key,DateTime now){if(key=="rest"){Rest(now);feedbackBuffer=now.AddMinutes(2);return;}if(effort.ContainsKey(key))Renew(key);CooldownUntil=now.AddMinutes(2);feedbackBuffer=CooldownUntil;BusyCount=0;}
    internal void ResumeBuffer(DateTime now){if(CooldownUntil<now.AddMinutes(2))CooldownUntil=now.AddMinutes(2);}
    internal void Quiet(DateTime now,int minutes){QuietUntil=now.AddMinutes(minutes);CooldownUntil=QuietUntil;}
    internal void Resume(DateTime now){QuietUntil=DateTime.MinValue;CooldownUntil=now.AddMinutes(2);}
    internal void Busy(DateTime now){BusyCount=Math.Min(4,BusyCount+1);Quiet(now,20+BusyCount*10);}
    // Not answering a bubble is not evidence of resting or asking for an hour of silence.
    internal void Ignored(DateTime now){if(CooldownUntil<now.AddMinutes(2))CooldownUntil=now.AddMinutes(2);}
    internal void Going(DateTime now){Quiet(now,5);}
    private double Priority(string key){return key=="eyes"?0.6:key=="rest"?0.45:0;}
    private double Rank(Rule r,DateTime now){double ratio=effort[r.Key]/threshold[r.Key];return Math.Min(4,lastOffered[r.Key]==DateTime.MinValue?ratio:(now-lastOffered[r.Key]).TotalSeconds/threshold[r.Key])+Priority(r.Key)+(shown[r.Key]==0&&ratio>=1?1.5:0);}
    private Rule PickPrimary(List<Rule> due,DateTime now){
      if(due.Count==0)return null;List<Rule> choices=due.Where(r=>r.Key!=lastPrimary).ToList();if(choices.Count==0)choices=due;
      double total=choices.Sum(r=>Math.Max(0.1,Rank(r,now)));double draw=random.NextDouble()*total;
      foreach(Rule rule in choices){draw-=Math.Max(0.1,Rank(rule,now));if(draw<=0)return rule;}return choices.Last();
    }
    internal List<Rule> Suggest(DateTime now){
      if(now<QuietUntil||now<feedbackBuffer)return null;
      Rule eyes=options.Rules.FirstOrDefault(r=>r.Key=="eyes"&&r.Enabled&&effort[r.Key]>=threshold[r.Key]&&now>=offered[r.Key]);
      bool eyeOverdue=eyes!=null&&(offered["eyes"]==DateTime.MinValue?effort["eyes"]>=threshold["eyes"]+120:now>=offered["eyes"].AddMinutes(2));
      bool ordinaryReady=now>=CooldownUntil&&now>=lastPrompt.AddMinutes(Spacing);
      if(!ordinaryReady&&!(eyeOverdue&&now>=lastPrompt.AddMinutes(2)))return null;
      Rule primary=PickPrimary(options.Rules.Where(r=>r.Enabled&&effort[r.Key]>=threshold[r.Key]&&now>=offered[r.Key]).ToList(),now);
      if(primary==null)return null;List<Rule> result=new List<Rule>{primary};string second=null;
      if(primary.Key=="rest")second=Eligible("water",0.8,now)?"water":null;
      if(primary.Key=="water")second=Eligible("rest",0.85,now)?"rest":null;
      if(primary.Key=="eyes"){Rule companion=options.Rules.Where(r=>r.Key!="eyes"&&Eligible(r.Key,0.85,now)).OrderByDescending(r=>Rank(r,now)).FirstOrDefault();second=companion==null?null:companion.Key;}
      if(eyes!=null&&primary.Key!="eyes")second="eyes";
      if(second!=null)result.Add(options.Rules.First(r=>r.Key==second));
      foreach(Rule r in result){shown[r.Key]++;lastOffered[r.Key]=now;offered[r.Key]=now.AddMinutes(r.Key=="eyes"?r.Minutes:Math.Max(5,Math.Min(25,r.Minutes*0.65)));}
      lastPrimary=primary.Key;lastPrompt=now;CooldownUntil=now.AddMinutes(Spacing);return result;
    }
    private bool Eligible(string key,double ratio,DateTime now){return options.Rules.Any(r=>r.Key==key&&r.Enabled)&&effort[key]>=threshold[key]*ratio&&now>=offered[key];}
    internal string Report(){return String.Join("\n",options.Rules.Where(r=>r.Enabled).Select(r=>r.Name+"：本轮电脑使用约 "+Math.Floor(MinutesFor(r.Key))+" 分钟"));}
  }
}
