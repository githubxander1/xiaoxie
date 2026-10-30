using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;
using Microsoft.Win32;

[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("RestBuddy.Checks")]
[assembly:System.Reflection.AssemblyTitle("小歇")]
[assembly:System.Reflection.AssemblyVersion("0.5.4.0")]
namespace RestBuddy {
  public sealed class Rule {
    public string Key,Name,Message;public int Minutes;public bool Enabled;
    public Rule(){}
    public Rule(string key,string name,string message,int minutes,bool enabled){Key=key;Name=name;Message=message;Minutes=minutes;Enabled=enabled;}
  }
  public sealed class Options {
    public List<Rule> Rules=new List<Rule>{
      new Rule("eyes","用眼休息","看看远处，自然眨几次眼睛。",20,true),
      new Rule("posture","姿势放松","放松肩膀和双手，检查背部与双脚支撑。",30,true),
      new Rule("rest","离座休息","起身走走，给自己五分钟离屏休息。",60,true),
      new Rule("water","饮水","按自己的需要，喝几口水。",60,true)};
    public int Version=7;
    public bool DiverseNews=true;
    public bool WorkStartCheck=false;public string WorkCheckDate="";
    public bool AutoNews=true,NewsJitter=true,FinanceMix=true;public int NewsMinutes=10;
    public bool ShareEnabled=true,OnlineQuotes=true,NewsEnabled=true;
    public int ShareMinutes=90;
    public string QuoteKind="随机",NewsUrl=NewsFeeds.Jin10;
    public string PetName="小歇",Tone="温柔",Frequency="适中",Mode="expanded";
    public int PetSize=84,PetX=-1,PetY=-1;
    public Dictionary<string,string> CustomMessages=new Dictionary<string,string>();
    public bool WorkHours=true,Weekdays=true,Lunch=true,Sound=false,IdlePause=true,FullscreenPause=true;
    public string Start="09:00",End="18:00",LunchStart="12:00",LunchEnd="13:30";
  }
  internal static class Store {
    internal static string Folder;internal static string Error="";
    internal static JavaScriptSerializer Json=new JavaScriptSerializer();
    internal static Options Load(){
      Folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Data");
      try{Directory.CreateDirectory(Folder);string probe=Path.Combine(Folder,".write-"+Guid.NewGuid().ToString("N"));File.WriteAllText(probe,"");File.Delete(probe);}
      catch{Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RestBuddy");Directory.CreateDirectory(Folder);}
      string path=Path.Combine(Folder,"settings.json");Options value=null;
      if(File.Exists(path))try{value=Json.Deserialize<Options>(File.ReadAllText(path));}catch{
        try{value=Json.Deserialize<Options>(File.ReadAllText(path+".bak"));}catch{Error="设置无法读取，已使用默认值";}
      }
      if(value==null)value=new Options();
      CareCatalog.Normalize(value);
      if(String.IsNullOrWhiteSpace(value.PetName))value.PetName="小歇";value.PetName=value.PetName.Trim().Substring(0,Math.Min(12,value.PetName.Trim().Length));
      if(!new[]{"温柔","俏皮","简短"}.Contains(value.Tone))value.Tone="温柔";
      if(!new[]{"轻声","适中","积极"}.Contains(value.Frequency))value.Frequency="适中";
      if(!new[]{"expanded","compact","hidden"}.Contains(value.Mode))value.Mode="expanded";
      value.PetSize=Math.Max(56,Math.Min(120,value.PetSize));
      value.ShareMinutes=Math.Max(30,Math.Min(360,value.ShareMinutes));
      if(!new[]{"随机","诗词","文学","动漫","影视"}.Contains(value.QuoteKind))value.QuoteKind="随机";
      if(!ContentService.SafeUrl(value.NewsUrl))value.NewsUrl="https://www.ithome.com/rss/";
      if(value.Version<4){value.AutoNews=true;value.NewsMinutes=10;value.NewsJitter=true;if(value.NewsUrl==NewsFeeds.IT){value.NewsUrl=NewsFeeds.Jin10;value.FinanceMix=true;}else value.FinanceMix=false;}
      if(value.Version<7)value.DiverseNews=value.FinanceMix;
      value.NewsMinutes=Math.Max(2,Math.Min(180,value.NewsMinutes));value.Version=7;
      if(!ValidTime(value.Start))value.Start="09:00";if(!ValidTime(value.End))value.End="18:00";
      if(!ValidTime(value.LunchStart))value.LunchStart="12:00";if(!ValidTime(value.LunchEnd))value.LunchEnd="13:30";
      return value;
    }
    internal static bool ValidTime(string value){TimeSpan t;return TimeSpan.TryParseExact(value,"hh\\:mm",null,out t)&&t>=TimeSpan.Zero&&t<TimeSpan.FromDays(1);}
    internal static void Save(Options options){string path=Path.Combine(Folder,"settings.json"),temp=path+".tmp";File.WriteAllText(temp,Json.Serialize(options));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}
  }
  internal static class Native {
    [StructLayout(LayoutKind.Sequential)]internal struct Input {public uint cbSize,dwTime;}
    [StructLayout(LayoutKind.Sequential)]internal struct Rect {public int Left,Top,Right,Bottom;}
    [DllImport("user32.dll")]internal static extern bool GetLastInputInfo(ref Input info);
    [DllImport("user32.dll")]internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]internal static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern int GetClassName(IntPtr hwnd,System.Text.StringBuilder text,int count);
    [DllImport("user32.dll")]internal static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")]internal static extern bool DestroyIcon(IntPtr icon);
    internal static bool Idle(){Input input=new Input{cbSize=(uint)Marshal.SizeOf(typeof(Input))};return GetLastInputInfo(ref input)&&unchecked((uint)Environment.TickCount-input.dwTime)>=180000;}
    internal static bool Fullscreen(){IntPtr h=GetForegroundWindow();if(h==IntPtr.Zero)return false;System.Text.StringBuilder name=new System.Text.StringBuilder(256);GetClassName(h,name,256);if(new[]{"Progman","WorkerW","Shell_TrayWnd"}.Contains(name.ToString()))return false;Rect r;if(!GetWindowRect(h,out r))return false;Rectangle screen=Screen.FromHandle(h).Bounds;return r.Left<=screen.Left&&r.Top<=screen.Top&&r.Right>=screen.Right&&r.Bottom>=screen.Bottom;}
  }
  internal static class Style {
    internal static Color Ink=Color.FromArgb(38,56,49),Green=Color.FromArgb(23,90,73),Muted=Color.FromArgb(111,126,116),Bg=Color.FromArgb(249,251,248);
    private static readonly Dictionary<float,Font> Fonts=new Dictionary<float,Font>();
    internal static Font Font(float size){Font font;if(!Fonts.TryGetValue(size,out font)){font=new Font("Microsoft YaHei UI",size);Fonts[size]=font;}return font;}
    internal static Button Button(string text,Action action){Button b=new Button{Text=text,AutoSize=true,Height=32,FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=Green,Margin=new Padding(4),Padding=new Padding(8,2,8,2),Cursor=Cursors.Hand};b.FlatAppearance.BorderColor=Color.FromArgb(220,229,219);b.Click+=(s,e)=>action();return b;}
    internal static Label Label(string text,int size){return new Label{Text=text,AutoSize=true,ForeColor=Ink,Font=Font(size),Margin=new Padding(4,8,4,8)};}
  }
  internal static class Program {
    [STAThread]internal static void Main(string[] args){bool created;using(Mutex mutex=new Mutex(true,@"Local\RestBuddy.v010",out created)){if(!created){MessageBox.Show("小歇已在系统托盘运行。","小歇");return;}try{System.Net.ServicePointManager.SecurityProtocol=System.Net.SecurityProtocolType.Tls12;Native.SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Buddy(args.Contains("--preview")));}catch(Exception e){MessageBox.Show("无法启动："+e.Message,"小歇");}finally{mutex.ReleaseMutex();}}}
  }
}
