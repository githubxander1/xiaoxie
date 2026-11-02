using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.Win32;

namespace RestBuddy {
  internal sealed class Settings : Form {
    internal Label State;private readonly Buddy buddy;
    private TextBox name,editor,newsUrl,builtins,sourceDetails;private ComboBox quoteKind,newsPreset;private CheckBox shares,onlineQuotes,newsEnabled;private NumericUpDown shareMinutes,newsMinutes;private CheckBox autoNews,newsJitter;private ComboBox tone,frequency,mode,topic;private TabControl tabs;
    private readonly Dictionary<string,CheckBox> toggles=new Dictionary<string,CheckBox>();
    private readonly Dictionary<string,NumericUpDown> minutes=new Dictionary<string,NumericUpDown>();
    private readonly Dictionary<string,Label> careStatus=new Dictionary<string,Label>();
    private readonly Dictionary<string,string> custom;
    private CheckBox work,week,lunch,idle,fullscreen,sound,startup,workCheck;
    private DateTimePicker begin,end,lbegin,lend;private int editing=-1;
    internal Settings(Buddy value){
      buddy=value;custom=new Dictionary<string,string>(buddy.Options.CustomMessages);
      Text="小歇 v"+System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3)+" · 设置";Font=Style.Font(9);BackColor=Style.Bg;StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(440,360);
      TableLayoutPanel root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(8),ColumnCount=1,RowCount=3};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
      State=Style.Label(buddy.State(),10);State.ForeColor=Style.Green;root.Controls.Add(State,0,0);
      tabs=new TabControl{Dock=DockStyle.Fill,Margin=new Padding(0,3,0,3)};root.Controls.Add(tabs,0,1);
      FlowLayoutPanel companion=FlowPage(tabs,"陪伴");

      name=new TextBox{Text=buddy.Options.PetName,MaxLength=12,Width=200};companion.Controls.Add(Row("名字",name));
      tone=Combo(new[]{"温柔","俏皮","简短"},buddy.Options.Tone);companion.Controls.Add(Row("说话语气",tone));
      frequency=Combo(new[]{"轻声","适中","积极"},buddy.Options.Frequency);companion.Controls.Add(Row("关怀频率",frequency));
      mode=Combo(new[]{"展开小猫","收成小点","隐藏到托盘"},buddy.Options.Mode=="compact"?"收成小点":buddy.Options.Mode=="hidden"?"隐藏到托盘":"展开小猫");companion.Controls.Add(Row("显示方式",mode));
      workCheck=Check("每天开工时检查工位",buddy.Options.WorkStartCheck);companion.Controls.Add(workCheck);
      FlowLayoutPanel care=FlowPage(tabs,"关怀");
      TableLayoutPanel grid=new TableLayoutPanel{AutoSize=true,Width=392,ColumnCount=3,Margin=Padding.Empty};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,92));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,80));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
      grid.Controls.Add(Style.Label("关怀主题",9),0,0);grid.Controls.Add(Style.Label("间隔/分",9),1,0);grid.Controls.Add(Style.Label("当前状态",9),2,0);int row=1;
      foreach(Rule r in buddy.Options.Rules){CheckBox enabled=Check(r.Name,r.Enabled);enabled.Margin=new Padding(4,4,4,4);NumericUpDown n=new NumericUpDown{Minimum=1,Maximum=480,Value=r.Minutes,Width=70,Margin=new Padding(4,3,4,3)};grid.Controls.Add(enabled,0,row);grid.Controls.Add(n,1,row);Label progress=Style.Label("",9);progress.ForeColor=Style.Muted;progress.Margin=new Padding(4,5,0,4);grid.Controls.Add(progress,2,row);careStatus[r.Key]=progress;toggles[r.Key]=enabled;minutes[r.Key]=n;enabled.CheckedChanged+=(s,e)=>UpdateCareStatus();n.ValueChanged+=(s,e)=>UpdateCareStatus();row++;}care.Controls.Add(grid);
      UpdateCareStatus();frequency.SelectedIndexChanged+=(s,e)=>UpdateCareStatus();
      Label reference=Style.Label("按活跃时间累计；状态每 5 秒更新，修改后保存生效。",9);reference.Margin=new Padding(4,3,4,0);reference.ForeColor=Style.Muted;care.Controls.Add(reference);
      TabPage messages=new TabPage("文案"){BackColor=Style.Bg,Padding=new Padding(6)};tabs.TabPages.Add(messages);
      TableLayoutPanel writing=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6};writing.RowStyles.Add(new RowStyle(SizeType.AutoSize));writing.RowStyles.Add(new RowStyle(SizeType.AutoSize));writing.RowStyles.Add(new RowStyle(SizeType.Percent,40));writing.RowStyles.Add(new RowStyle(SizeType.AutoSize));writing.RowStyles.Add(new RowStyle(SizeType.AutoSize));writing.RowStyles.Add(new RowStyle(SizeType.Percent,60));messages.Controls.Add(writing);
      topic=Combo(buddy.Options.Rules.Select(r=>r.Name).ToArray(),buddy.Options.Rules[0].Name);writing.Controls.Add(topic,0,0);
      Label help=Style.Label("每行一句；留空使用内置文案；可写 {名字}",9);help.ForeColor=Style.Muted;writing.Controls.Add(help,0,1);
      editor=new TextBox{Multiline=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,MaxLength=4000,AcceptsReturn=true,BorderStyle=BorderStyle.FixedSingle};writing.Controls.Add(editor,0,2);writing.Controls.Add(Style.Button("使用内置文案",()=>editor.Text=""),0,3);
      builtins=new TextBox{Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,BackColor=Style.Bg};writing.Controls.Add(Style.Label("内置动作与文案（只读）",9),0,4);writing.Controls.Add(builtins,0,5);
      topic.SelectedIndexChanged+=(s,e)=>LoadTopic();tone.SelectedIndexChanged+=(s,e)=>RefreshBuiltins();LoadTopic();
      FlowLayoutPanel content=FlowPage(tabs,"内容");
      newsEnabled=Check("允许获取新闻 / 快讯",buddy.Options.NewsEnabled);content.Controls.Add(newsEnabled);
      autoNews=Check("自动随机展示一条快讯",buddy.Options.AutoNews);content.Controls.Add(autoNews);
      newsMinutes=new NumericUpDown{Minimum=2,Maximum=180,Value=buddy.Options.NewsMinutes,Width=130};content.Controls.Add(Row("快讯分钟",newsMinutes));
      newsJitter=Check("时间略微随机（间隔上下浮动两成）",buddy.Options.NewsJitter);content.Controls.Add(newsJitter);
      newsPreset=Combo(NewsFeeds.Names,NewsFeeds.Names[NewsFeeds.Index(buddy.Options)]);newsPreset.Width=245;content.Controls.Add(Row("新闻来源",newsPreset));
      newsUrl=new TextBox{Text=buddy.Options.NewsUrl,Width=340,Margin=new Padding(4,8,4,8),MaxLength=2048,Visible=newsPreset.SelectedIndex==5};content.Controls.Add(newsUrl);
      newsPreset.SelectedIndexChanged+=(s,e)=>{if(newsPreset.SelectedIndex!=5)newsUrl.Text=NewsFeeds.Url(newsPreset.SelectedIndex);newsUrl.Visible=newsPreset.SelectedIndex==5;UpdateSources();};newsUrl.TextChanged+=(s,e)=>UpdateSources();
      sourceDetails=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Width=370,Height=105,BackColor=Style.Bg};content.Controls.Add(sourceDetails);UpdateSources();
      FlowLayoutPanel sourceButtons=new FlowLayoutPanel{AutoSize=true,WrapContents=true};sourceButtons.Controls.Add(Style.Button("保存并读新闻",()=>{Apply();if(IsDisposed)buddy.ShareContent(true,true);}));sourceButtons.Controls.Add(Style.Button("保存并听语录",()=>{Apply();if(IsDisposed)buddy.ShareContent(false,true);}));content.Controls.Add(sourceButtons);
      Label sourceHint=Style.Label("接口无需密钥；修改后点保存生效",9);sourceHint.ForeColor=Style.Muted;content.Controls.Add(sourceHint);
      shares=Check("偶尔自动分享语录",buddy.Options.ShareEnabled);content.Controls.Add(shares);
      shareMinutes=new NumericUpDown{Minimum=30,Maximum=360,Value=buddy.Options.ShareMinutes,Width=130};content.Controls.Add(Row("语录分钟",shareMinutes));
      onlineQuotes=Check("在线语录：一言（无密钥）",buddy.Options.OnlineQuotes);content.Controls.Add(onlineQuotes);
      quoteKind=Combo(new[]{"随机","诗词","文学","动漫","影视"},buddy.Options.QuoteKind);content.Controls.Add(Row("语录类别",quoteKind));
      TextBox quoteSource=new TextBox{Multiline=true,ReadOnly=true,Width=370,Height=64,ScrollBars=ScrollBars.Vertical,Text="一言 API：https://v1.hitokoto.cn/\r\n类别：随机 d/i/k，诗词 i，文学 d，动漫 a，影视 h；5–60 字。\r\n失败时使用缓存或内置短句；最多缓存 40 条。",BackColor=Style.Bg};content.Controls.Add(quoteSource);
      FlowLayoutPanel timing=FlowPage(tabs,"时段");
      work=Check("仅工作时段提醒",buddy.Options.WorkHours);timing.Controls.Add(work);timing.Controls.Add(TimeRow(buddy.Options.Start,buddy.Options.End,out begin,out end));
      week=Check("仅周一至周五",buddy.Options.Weekdays);timing.Controls.Add(week);
      lunch=Check("午休时安静",buddy.Options.Lunch);timing.Controls.Add(lunch);timing.Controls.Add(TimeRow(buddy.Options.LunchStart,buddy.Options.LunchEnd,out lbegin,out lend));
      idle=Check("三分钟未操作时暂停提醒",buddy.Options.IdlePause);fullscreen=Check("全屏时安静",buddy.Options.FullscreenPause);sound=Check("提示音",buddy.Options.Sound);startup=Check("开机启动",StartupEnabled());foreach(Control c in new Control[]{idle,fullscreen,sound,startup})timing.Controls.Add(c);
      FlowLayoutPanel buttons=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=false};buttons.Controls.Add(Style.Button("保存",Apply));buttons.Controls.Add(Style.Button("示范关怀",()=>PreviewDraft()));buttons.Controls.Add(Style.Button("提醒记录",buddy.OpenHistory));root.Controls.Add(buttons,0,2);
      tabs.SelectedIndexChanged+=(s,e)=>{float scale=DeviceDpi/96f;ClientSize=new Size((int)(440*scale),(int)((tabs.SelectedIndex==1?340:tabs.SelectedIndex==0?360:420)*scale));};
    }
    internal void UpdateCareStatus(){foreach(Rule r in buddy.Options.Rules){Label label;if(!careStatus.TryGetValue(r.Key,out label))continue;bool dirty=toggles[r.Key].Checked!=r.Enabled||(int)minutes[r.Key].Value!=r.Minutes||frequency.Text!=buddy.Options.Frequency;label.Text=dirty?"修改待保存":buddy.Engine.StatusFor(r.Key);}}
    private void UpdateSources(){if(sourceDetails==null)return;int i=newsPreset.SelectedIndex;string urls=i==6?"科技 · IT之家\r\n"+NewsFeeds.IT+"\r\n国际 · 中新网\r\n"+NewsFeeds.World+"\r\n生活 · 中新网\r\n"+NewsFeeds.Life+"\r\n财经 · 金十（RSSHub中转）\r\n"+NewsFeeds.Jin10+"\r\n开源项目 · HelloGitHub\r\n"+NewsFeeds.HelloGitHub+"\r\n项目正文：raw.githubusercontent.com/521xueweihan/HelloGitHub/master/content/HelloGitHub{期号}.md":i==0?NewsFeeds.Jin10+"\r\n"+NewsFeeds.Wallstreet:i==5?newsUrl.Text:NewsFeeds.Url(i);if(i==9)urls+="\r\n官方 RSS 确定最新期号；官方 GitHub content/HelloGitHub{期号}.md 读取项目。\r\n最新一期随机未读，不按小时过滤；打开 GitHub 仓库。";sourceDetails.Text=urls+"\r\n"+(i==9?"已读项目跳过，最新一期读完后等待更新。":i==6?"依次轮换，类内随机未读；失败跳过。新闻时效 24 小时，生活 72 小时；开源为最新一期随机未读。":"财经混合随机双源；单源自动选近 8 小时未读。")+"\r\n新闻缓存 3 分钟，HelloGitHub 6 小时；最多 5 源×60 条，已读 200 条。";}
    private void Stash(){if(editing>=0)custom[buddy.Options.Rules[editing].Key]=editor.Text;}
    private void LoadTopic(){Stash();editing=topic.SelectedIndex;if(editing<0)return;string text;editor.Text=custom.TryGetValue(buddy.Options.Rules[editing].Key,out text)?text:"";RefreshBuiltins();}
    private void RefreshBuiltins(){if(editing<0||builtins==null)return;string key=buddy.Options.Rules[editing].Key;Options display=new Options{Tone=tone.Text};builtins.Text=CareActionPicker.BuiltIns(key)+"\r\n\r\n主题文案 · "+tone.Text+"\r\n"+String.Join("\r\n",new CareText(display).BuiltInLines(key));}
    private void PreviewDraft(){Stash();string oldName=buddy.Options.PetName,oldTone=buddy.Options.Tone;Dictionary<string,string> oldCustom=buddy.Options.CustomMessages;
      try{buddy.Options.PetName=String.IsNullOrWhiteSpace(name.Text)?"小歇":name.Text.Trim();buddy.Options.Tone=tone.Text;buddy.Options.CustomMessages=custom;if(tabs.SelectedIndex==2)buddy.PreviewTopic(buddy.Options.Rules[editing].Key);else buddy.Preview();}
      finally{buddy.Options.PetName=oldName;buddy.Options.Tone=oldTone;buddy.Options.CustomMessages=oldCustom;}
    }
    private FlowLayoutPanel FlowPage(TabControl tabs,string title){TabPage page=new TabPage(title){BackColor=Style.Bg,Padding=new Padding(6)};tabs.TabPages.Add(page);FlowLayoutPanel flow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};page.Controls.Add(flow);return flow;}
    private Control Row(string title,Control control){FlowLayoutPanel row=new FlowLayoutPanel{AutoSize=true,WrapContents=false,Margin=new Padding(0,1,0,1)};Label label=Style.Label(title,9);label.AutoSize=false;label.Width=78;label.Height=24;row.Controls.Add(label);control.Margin=new Padding(3,3,3,1);row.Controls.Add(control);return row;}
    private ComboBox Combo(string[] entries,string selected){ComboBox box=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=200};box.Items.AddRange(entries);box.SelectedItem=selected;if(box.SelectedIndex<0)box.SelectedIndex=0;return box;}
    private CheckBox Check(string title,bool selected){return new CheckBox{Text=title,Checked=selected,AutoSize=true,Margin=new Padding(4,6,4,6)};}
    private Control TimeRow(string a,string b,out DateTimePicker first,out DateTimePicker second){FlowLayoutPanel row=new FlowLayoutPanel{AutoSize=true,WrapContents=false};first=Picker(a);second=Picker(b);row.Controls.Add(first);row.Controls.Add(Style.Label("至",9));row.Controls.Add(second);return row;}
    private DateTimePicker Picker(string value){return new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="HH:mm",ShowUpDown=true,Width=95,Value=DateTime.Today.Add(TimeSpan.Parse(value)),Margin=new Padding(3,4,3,4)};}
    private bool StartupEnabled(){try{using(RegistryKey key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))return key!=null&&key.GetValue("RestBuddy")!=null;}catch{return false;}}
    private void Apply(){
      if(work.Checked&&begin.Value.ToString("HH:mm")==end.Value.ToString("HH:mm")){MessageBox.Show("工作开始和结束时间不能相同。","小歇");return;}
      if(lunch.Checked&&lbegin.Value.ToString("HH:mm")==lend.Value.ToString("HH:mm")){MessageBox.Show("午休开始和结束时间不能相同。","小歇");return;}
      if(newsEnabled.Checked&&!ContentService.SafeUrl(newsUrl.Text.Trim())){MessageBox.Show("请填写 http 或 https 的 RSS / 热榜地址。","小歇");return;}
      Stash();Options o=buddy.Options;o.WorkStartCheck=workCheck.Checked;o.AutoNews=autoNews.Checked;o.NewsMinutes=(int)newsMinutes.Value;o.NewsJitter=newsJitter.Checked;o.FinanceMix=newsPreset.SelectedIndex==0;o.DiverseNews=newsPreset.SelectedIndex==6;o.ShareEnabled=shares.Checked;o.ShareMinutes=(int)shareMinutes.Value;o.OnlineQuotes=onlineQuotes.Checked;o.NewsEnabled=newsEnabled.Checked;o.QuoteKind=quoteKind.Text;o.NewsUrl=newsUrl.Text.Trim();o.PetName=String.IsNullOrWhiteSpace(name.Text)?"小歇":name.Text.Trim();o.Tone=tone.Text;o.Frequency=frequency.Text;o.Mode=mode.SelectedIndex==1?"compact":mode.SelectedIndex==2?"hidden":"expanded";o.CustomMessages=new Dictionary<string,string>(custom);
      foreach(Rule r in o.Rules){r.Enabled=toggles[r.Key].Checked;r.Minutes=(int)minutes[r.Key].Value;}
      o.WorkHours=work.Checked;o.Weekdays=week.Checked;o.Lunch=lunch.Checked;o.IdlePause=idle.Checked;o.FullscreenPause=fullscreen.Checked;o.Sound=sound.Checked;o.Start=begin.Value.ToString("HH:mm");o.End=end.Value.ToString("HH:mm");o.LunchStart=lbegin.Value.ToString("HH:mm");o.LunchEnd=lend.Value.ToString("HH:mm");
      if(!buddy.Save())return;
      try{if(StartupEnabled()!=startup.Checked)using(RegistryKey key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){if(startup.Checked)key.SetValue("RestBuddy","\""+System.Reflection.Assembly.GetExecutingAssembly().Location+"\"");else key.DeleteValue("RestBuddy",false);}}catch{MessageBox.Show("关怀设置已保存，开机启动未能修改。","小歇");return;}Close();
    }
  }
}
