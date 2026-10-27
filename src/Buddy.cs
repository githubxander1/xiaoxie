using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace RestBuddy {
  internal sealed class Buddy : ApplicationContext {
    internal readonly Options Options;
    internal readonly CareEngine Engine;
    internal readonly CareText Words;
    internal Companion Pet;
    private NotifyIcon tray;private Control dispatch;
    private Settings settings;private Speech bubble;private Coach coach;private ContentService contents;
    private bool fetching;private int dialogVersion;private double shareActive;private AutoNewsClock autoNews;private readonly Random random=new Random();private DateTime autoContentAfter=DateTime.MinValue;
    private System.Windows.Forms.Timer tick;
    private DateTime lastTick=DateTime.UtcNow,lockAt,lastDay=DateTime.Today;
    private bool locked,exiting,wasActive;
    private string previousReason="";
    internal Buddy(bool preview){
      dispatch=new Control();IntPtr handle=dispatch.Handle;
      Options=Store.Load();Engine=new CareEngine(Options);Words=new CareText(Options);contents=new ContentService(Store.Folder);autoNews=new AutoNewsClock(Options,random,DateTime.UtcNow);
      tray=new NotifyIcon{Text="小歇 · "+Options.PetName+"在这里",Icon=PetIcon(),Visible=true,ContextMenuStrip=MakeMenu()};
      tray.DoubleClick+=(s,e)=>OpenSettings();
      Pet=new Companion(this);Pet.ContextMenuStrip=MakeMenu();Pet.ApplyMode();
      SystemEvents.SessionSwitch+=SessionSwitch;
      tick=new System.Windows.Forms.Timer{Interval=5000};tick.Tick+=(s,e)=>Tick();tick.Start();
      bool first=!File.Exists(Path.Combine(Store.Folder,"settings.json"));Persist();
      if(first||preview)OpenSettings();
      previousReason=State(true);wasActive=previousReason=="在这里陪你";
      if(preview||first)Preview();
    }
    private Icon PetIcon(){using(Bitmap bitmap=new Bitmap(32,32)){using(Graphics g=Graphics.FromImage(bitmap)){g.Clear(Color.Transparent);PetPainter.Draw(g,32,0,false);}IntPtr handle=bitmap.GetHicon();try{return (Icon)Icon.FromHandle(handle).Clone();}finally{Native.DestroyIcon(handle);}}}
    internal ContextMenuStrip MakeMenu(){
      ContextMenuStrip menu=new ContextMenuStrip();
      foreach(string[] entry in new[]{new[]{"展开小猫","expanded"},new[]{"收成小点","compact"},new[]{"隐藏到托盘","hidden"}}){string mode=entry[1];ToolStripMenuItem item=new ToolStripMenuItem(entry[0]);item.Click+=(s,e)=>SetMode(mode);menu.Items.Add(item);menu.Opening+=(s,e)=>item.Checked=Options.Mode==mode;}
      menu.Items.Add(new ToolStripSeparator());menu.Items.Add("和我说句话",null,(s,e)=>Interact());menu.Items.Add("示范关怀",null,(s,e)=>Preview());
      ToolStripMenuItem play=new ToolStripMenuItem("陪我一下");play.DropDownItems.Add("摸摸小猫",null,(s,e)=>Play("pet"));play.DropDownItems.Add("一起伸懒腰",null,(s,e)=>Play("stretch"));play.DropDownItems.Add("逗我一下",null,(s,e)=>Play("joke"));play.DropDownItems.Add("给我打打气",null,(s,e)=>Play("cheer"));menu.Items.Add(play);
      menu.Items.Add("随机一句 / 台词",null,(s,e)=>ShareContent(false,true));menu.Items.Add("读条新闻",null,(s,e)=>ShareContent(true,true));
      ToolStripMenuItem exercise=new ToolStripMenuItem("健康小练习");foreach(Rule r in Options.Rules.Where(x=>x.Key!="water")){string key=r.Key;exercise.DropDownItems.Add(r.Name,null,(s,e)=>StartCoach(key));}menu.Items.Add(exercise);menu.Items.Add("关怀状态",null,(s,e)=>ShowStatus());menu.Items.Add("工位检查",null,(s,e)=>ShowWorkCheck());
      menu.Items.Add("我刚休息过",null,(s,e)=>Feedback("rested"));menu.Items.Add("我刚喝过水",null,(s,e)=>Feedback("water"));menu.Items.Add("安静一小时",null,(s,e)=>Pause());menu.Items.Add("继续关怀",null,(s,e)=>Resume());
      menu.Items.Add(new ToolStripSeparator());menu.Items.Add("关怀设置",null,(s,e)=>OpenSettings());menu.Items.Add("退出",null,(s,e)=>ExitThread());return menu;
    }
    internal void SetMode(string value){Options.Mode=value;HideBubble();Pet.ApplyMode();Persist();}
    internal void Persist(){try{Store.Save(Options);}catch{if(settings!=null&&!settings.IsDisposed)settings.State.Text="设置尚未保存，请检查目录权限";}}
    internal bool Save(){try{Store.Save(Options);Engine.Reconfigure();autoNews.Reconfigure(DateTime.UtcNow);HideBubble();Pet.ApplyMode();return true;}catch{MessageBox.Show("设置尚未保存，请检查目录权限。","小歇");return false;}}
    private static bool Inside(TimeSpan now,string start,string end){TimeSpan a=TimeSpan.Parse(start),b=TimeSpan.Parse(end);return a<b?now>=a&&now<b:now>=a||now<b;}
    internal string State(){return State(false);}
    private string State(bool ignoreQuiet){
      if(locked)return "锁屏时安静陪伴";
      DateTime now=DateTime.Now,utc=DateTime.UtcNow;
      if(!ignoreQuiet&&utc<Engine.QuietUntil)return "安静到 "+Engine.QuietUntil.ToLocalTime().ToString("HH:mm");
      if(Options.WorkHours){if(Options.Weekdays&&(now.DayOfWeek==DayOfWeek.Saturday||now.DayOfWeek==DayOfWeek.Sunday))return "今天好好休息";if(!Inside(now.TimeOfDay,Options.Start,Options.End))return "下班时间，安静陪伴";}
      if(Options.Lunch&&Inside(now.TimeOfDay,Options.LunchStart,Options.LunchEnd))return "午休时间，安静陪伴";
      if(Options.IdlePause&&Native.Idle())return "安静等你回来";
      if(Options.FullscreenPause&&Native.Fullscreen())return "全屏时安静陪伴";
      return "在这里陪你";
    }
    private void Tick(){
      DateTime now=DateTime.UtcNow;double elapsed=(now-lastTick).TotalSeconds;lastTick=now;
      if(lastDay!=DateTime.Today){lastDay=DateTime.Today;Engine.Reset(now);}
      if(elapsed>30){if(elapsed>=180)Engine.ScreenBreak(now);else Engine.ResumeBuffer(now);elapsed=0;}
      string context=State(true),state=State();bool active=context=="在这里陪你";
      if(!active&&context!=previousReason){
        HideBubble();
      }
      if(active&&!wasActive){Engine.ResumeBuffer(now);if(previousReason=="安静等你回来")Pet.LastWords="欢迎回来。如果刚才已经休息过，可以告诉我，我会调整关怀。";}
      wasActive=active;previousReason=context;
      if(active){Engine.AddActive(elapsed);if(elapsed>0&&elapsed<=30)shareActive+=elapsed;}
      tray.Text=Short(Options.PetName+" · "+state,60);
      if(settings!=null&&!settings.IsDisposed)settings.State.Text=state;
      Pet.SetMood(!active||now<Engine.QuietUntil);
      if(!active||now<Engine.QuietUntil||(bubble!=null&&!bubble.IsDisposed)||(coach!=null&&!coach.IsDisposed)||(settings!=null&&!settings.IsDisposed&&settings.Visible))return;
      if(Options.WorkStartCheck&&Options.WorkCheckDate!=DateTime.Today.ToString("yyyy-MM-dd")){Options.WorkCheckDate=DateTime.Today.ToString("yyyy-MM-dd");Persist();ShowWorkCheck();return;}
      List<Rule> rules=Engine.Suggest(now);if(rules!=null){ShowCare(rules,false);return;}
      if(now>=autoContentAfter&&autoNews.IsDue(now)&&!fetching){ShareContent(true,false);return;}
      if(Options.ShareEnabled&&!Engine.HasUrgent(now)&&!fetching&&now>=autoContentAfter&&shareActive>=Options.ShareMinutes*60)ShareContent(false,false);
    }
    internal static string Short(string value,int max){return value.Length>max?value.Substring(0,max):value;}
    private void SessionSwitch(object sender,SessionSwitchEventArgs e){
      if(exiting)return;if(dispatch.InvokeRequired){try{dispatch.BeginInvoke(new Action(()=>SessionSwitch(sender,e)));}catch(InvalidOperationException){}return;}
      if(e.Reason==SessionSwitchReason.SessionLock){locked=true;lockAt=DateTime.UtcNow;HideBubble();if(coach!=null&&!coach.IsDisposed)coach.Close();}
      if(e.Reason==SessionSwitchReason.SessionUnlock){locked=false;if((DateTime.UtcNow-lockAt).TotalMinutes>=3)Engine.ScreenBreak(DateTime.UtcNow);else Engine.ResumeBuffer(DateTime.UtcNow);}
    }
    internal void Preview(){ShowCare(new List<Rule>{Options.Rules.First(r=>r.Key=="rest"),Options.Rules.First(r=>r.Key=="water")},true);}
    internal void PreviewTopic(string key){ShowCare(new List<Rule>{Options.Rules.First(r=>r.Key==key)},true);}
    internal static string ExerciseKey(List<Rule> rules){Rule exercise=rules.FirstOrDefault(r=>r.Key=="rest")??rules.FirstOrDefault(r=>r.Key!="water");return exercise==null?"eyes":exercise.Key;}
    private void ShowCare(List<Rule> rules,bool preview){
      if(!preview)autoContentAfter=DateTime.UtcNow.AddMinutes(2);
      bool waterOnly=rules.Count==1&&rules[0].Key=="water";
      string first=waterOnly?"water":"coach";
      SpeechAction[] actions={new SpeechAction(waterOnly?"喝过水了":"一起做一下",first),new SpeechAction("做到了","done"),new SpeechAction("现在忙","busy")};
      ShowSpeech(Words.Message(rules),preview?"示范关怀":"陪你歇一下",actions,action=>{
        if(action=="ignored"){if(!preview)Engine.Ignored(DateTime.UtcNow);HideBubble();return;}
        if(action=="coach"){StartCoach(ExerciseKey(rules));return;}
        if(action=="done"){foreach(Rule r in rules)Engine.Completed(r.Key,DateTime.UtcNow);Feedback("completed");return;}
        Feedback(action);
      },36000);
      if(Options.Sound&&!preview)System.Media.SystemSounds.Asterisk.Play();
    }
    internal void Interact(){ShowSpeech(Words.Reply("hello",Engine.BusyCount),"我在呢",new[]{new SpeechAction("摸摸","pet"),new SpeechAction("新鲜事","fresh"),new SpeechAction("健康小练习","practice")},action=>{
      if(action=="pet")Play("pet");else if(action=="fresh")FreshMenu();else if(action=="practice")StartCoach("eyes");else HideBubble();
    },36000);}
    internal void Play(string kind){Pet.React(kind);ShowSpeech(Words.Play(kind),"陪我一下",new[]{new SpeechAction("再来一句","again"),new SpeechAction("一起活动","exercise"),new SpeechAction("收起","close")},action=>{if(action=="again")Play(kind);else if(action=="exercise")StartCoach("posture");else HideBubble();},36000);}
    private void FreshMenu(){ShowSpeech("想听一句台词，看看新闻，还是让我逗你笑一下？","来点新鲜的",new[]{new SpeechAction("一句话","quote"),new SpeechAction("新闻","news"),new SpeechAction("逗我一下","joke")},action=>{if(action=="quote")ShareContent(false,true);else if(action=="news")ShareContent(true,true);else if(action=="joke")Play("joke");else HideBubble();},36000);}
    internal void ShowWorkCheck(){ShowSpeech("开工前照顾一下自己的位置：屏幕有没有刺眼反光？背部和双脚有没有支撑？水杯放在顺手的位置了吗？","工位检查",new[]{new SpeechAction("检查好了","close"),new SpeechAction("今天先略过","close")},a=>HideBubble(),36000);}
    internal void ShowStatus(){ShowSpeech((Options.AutoNews&&Options.NewsEnabled?"下次自动快讯："+autoNews.DueUtc.ToLocalTime().ToString("HH:mm")+"（在可提醒时段展示）\n\n":"自动快讯已关闭\n\n")+Engine.Report()+"\n\n以上是本次会话的电脑使用参考，不代表检测到了身体状态。\n参考值到达后会综合选择关怀；新闻和语录会给健康关怀让路。","关怀状态",new[]{new SpeechAction("刚休息过","rested"),new SpeechAction("刚喝过水","water"),new SpeechAction("设置","settings")},action=>{if(action=="settings"){HideBubble();OpenSettings();}else if(action=="ignored")HideBubble();else Feedback(action);},60000);}
    internal void StartCoach(string key){
      HideBubble();if(coach!=null&&!coach.IsDisposed){coach.Show();return;}
      Rule rule=Options.Rules.FirstOrDefault(r=>r.Key==key);if(rule==null)return;
      if(Engine.QuietUntil<DateTime.UtcNow.AddMinutes(1))Engine.Quiet(DateTime.UtcNow,1);
      Pet.React("stretch");coach=new Coach(key,rule.Name,()=>{Engine.Completed(key,DateTime.UtcNow);Feedback("completed");});coach.Show();
    }
    private async Task<ContentItem> FetchNews(bool recent){
      string url=Options.FinanceMix?(random.Next(2)==0?NewsFeeds.Jin10:NewsFeeds.Wallstreet):Options.NewsUrl;
      try{return await contents.NewsAsync(url,recent);}catch{if(!Options.FinanceMix)throw;}
      return await contents.NewsAsync(url==NewsFeeds.Jin10?NewsFeeds.Wallstreet:NewsFeeds.Jin10,recent);
    }
    internal async void ShareContent(bool news,bool manual){
      if(fetching||exiting)return;
      if(news&&!Options.NewsEnabled){if(manual)ShowSpeech("新闻获取目前关闭着。想开启的话，可以在「内容」设置里选择来源。","新闻",new[]{new SpeechAction("去设置","settings"),new SpeechAction("听一句话","quote")},a=>{if(a=="settings"){HideBubble();OpenSettings();}else if(a=="quote")ShareContent(false,true);else HideBubble();},36000);return;}
      if(!news)shareActive=0;fetching=true;bool displayed=false;
      if(manual)ShowSpeech(news?"我去看看这一批新闻标题，稍等一下。":"我去找一句话，很快回来。","找点新鲜的",new[]{new SpeechAction("先收起","close")},a=>HideBubble(),20000);
      int ticket=dialogVersion;
      try{
        ContentItem item=await (news?FetchNews(!manual):contents.QuoteAsync(Options.QuoteKind,Options.OnlineQuotes));
        if(exiting||ticket!=dialogVersion||(!manual&&(State()!="在这里陪你"||(settings!=null&&!settings.IsDisposed&&settings.Visible))))return;
        if(news){contents.MarkSeen(item);autoNews.Consumed(DateTime.UtcNow);}
        ShowSpeech(item.Display(),news?"新闻标题":"一句话",new[]{new SpeechAction("换一条","next"),new SpeechAction(ContentService.SafeUrl(item.Url)?"看来源":"收起","source"),new SpeechAction("先收起","close")},a=>{
          if(a=="next")ShareContent(news,true);else if(a=="source"&&ContentService.SafeUrl(item.Url)){try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.Url){UseShellExecute=true});}catch{}}else HideBubble();
        },60000);displayed=true;
      }catch{if(manual&&!exiting&&ticket==dialogVersion)ShowSpeech("这次没取到新的标题。网络恢复或新闻更新后再试试，也可以换一个来源。","稍后再来",new[]{new SpeechAction("听一句话","quote"),new SpeechAction("收起","close")},a=>{if(a=="quote")ShareContent(false,true);else HideBubble();},36000);}
      finally{fetching=false;if(news&&!manual&&!displayed)autoNews.Retry(DateTime.UtcNow);}
    }
    internal void Feedback(string action){DateTime now=DateTime.UtcNow;
      if(action=="busy")Engine.Busy(now);else if(action=="rested")Engine.Rest(now);else if(action=="water")Engine.Drank(now);else if(action=="going")Engine.Going(now);
      Pet.React(action=="going"?"stretch":"pet");Pet.LastWords=Words.Reply(action,Engine.BusyCount);ShowSpeech(Pet.LastWords,"收到啦",new[]{new SpeechAction("好","close")},a=>HideBubble(),6500);
      tray.Text=Short(Options.PetName+" · "+State(),60);
    }
    private void ShowSpeech(string message,string caption,SpeechAction[] actions,Action<string> handler,int duration){
      HideBubble();bubble=new Speech(this,message,caption,actions,handler,duration);if(settings!=null&&!settings.IsDisposed&&settings.Visible)bubble.Owner=settings;bubble.Show();Pet.LastWords=message;
    }
    private void HideBubble(){dialogVersion++;if(bubble!=null&&!bubble.IsDisposed)bubble.Close();bubble=null;}
    internal void Pause(){Engine.Quiet(DateTime.UtcNow,60);HideBubble();Tick();}
    internal void Resume(){Engine.Resume(DateTime.UtcNow);Tick();}
    internal void OpenSettings(){autoContentAfter=DateTime.UtcNow.AddSeconds(30);if(settings==null||settings.IsDisposed)settings=new Settings(this);settings.Show();settings.WindowState=FormWindowState.Normal;settings.Activate();}
    protected override void ExitThreadCore(){if(exiting)return;exiting=true;tick.Stop();SystemEvents.SessionSwitch-=SessionSwitch;HideBubble();if(coach!=null&&!coach.IsDisposed)coach.Close();if(settings!=null)settings.Close();Pet.AllowClose=true;Pet.Close();Persist();Icon icon=tray.Icon;tray.Visible=false;tray.Dispose();icon.Dispose();dispatch.Dispose();base.ExitThreadCore();}
  }
}
