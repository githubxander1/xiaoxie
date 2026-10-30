using System;
using System.Drawing;
using System.Windows.Forms;
namespace RestBuddy {
  internal sealed class Coach : Form {
    private readonly System.Windows.Forms.Timer clock=new System.Windows.Forms.Timer();private DateTime finish;private Label countdown;private TextBox description;private int seconds;private Action finished;private bool reported;private bool automatic;private DateTime begins;private Label heading;private string title;
    protected override bool ShowWithoutActivation{get{return true;}}
    internal static int DurationSeconds(string key){return key=="eyes"?20:key=="rest"?300:30;}
    internal Coach(CareAction action,Action onFinished,bool auto=false,Rectangle anchor=default(Rectangle)){
      automatic=auto;title=action.Title;
      seconds=action.Seconds;finished=onFinished;
      Text="小歇陪你 · "+action.Title;FormBorderStyle=FormBorderStyle.FixedToolWindow;TopMost=true;ShowInTaskbar=false;Font=Style.Font(9);BackColor=Style.Bg;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(345,245);
      if(automatic){
        AutoScaleMode=AutoScaleMode.Dpi;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;ClientSize=new Size(280,92);Padding=new Padding(10);Opacity=0.94;
        Button dismiss=Style.Button("×",Close);dismiss.Dock=DockStyle.Right;dismiss.Width=25;dismiss.AutoSize=false;dismiss.Padding=Padding.Empty;dismiss.Margin=Padding.Empty;dismiss.FlatAppearance.BorderSize=0;dismiss.AccessibleName="关闭本次提醒";Controls.Add(dismiss);
        Panel body=new Panel{Dock=DockStyle.Fill};Controls.Add(body);body.BringToFront();heading=new Label{Text=action.Title,Dock=DockStyle.Top,Height=24,ForeColor=Style.Green,AutoEllipsis=true};body.Controls.Add(heading);countdown=new Label{Dock=DockStyle.Fill,ForeColor=Style.Ink,TextAlign=ContentAlignment.MiddleLeft};body.Controls.Add(countdown);countdown.BringToFront();
        Shown+=(s,e)=>{Rectangle area=Screen.FromRectangle(anchor).WorkingArea;Location=new Point(Math.Max(area.Left,Math.Min(anchor.Right-Width,area.Right-Width-12)),Math.Max(area.Top,Math.Min(anchor.Top-Height-8,area.Bottom-Height-12)));};
      }else{
      TableLayoutPanel root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=1,RowCount=4};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
      root.Controls.Add(Style.Label(action.Title,12),0,0);countdown=Style.Label("",24);countdown.ForeColor=Style.Green;root.Controls.Add(countdown,0,1);
      string step=action.Instructions;
      description=new TextBox{Text=step,ForeColor=Style.Ink,BackColor=Style.Bg,Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,WordWrap=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Margin=new Padding(4,8,4,4),TabStop=false};root.Controls.Add(description,0,2);
      FlowLayoutPanel actions=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true};actions.Controls.Add(Style.Button("重新计时",Restart));actions.Controls.Add(Style.Button("先收起",Close));root.Controls.Add(actions,0,3);
      }
      clock.Interval=100;clock.Tick+=(s,e)=>UpdateClock();Shown+=(s,e)=>Restart();FormClosed+=(s,e)=>clock.Dispose();
    }
    private void Restart(){reported=false;begins=DateTime.UtcNow.AddSeconds(automatic?3:0);finish=begins.AddSeconds(seconds);clock.Start();UpdateClock();}
    private void UpdateClock(){int left=Math.Max(0,(int)Math.Ceiling((finish-DateTime.UtcNow).TotalSeconds));if(automatic){int prepare=Math.Max(0,(int)Math.Ceiling((begins-DateTime.UtcNow).TotalSeconds));heading.Text=title;countdown.Text=prepare>0?prepare+" 秒后开始 · "+seconds+" 秒":("剩余 "+(left>=60?(left/60)+":"+(left%60).ToString("00"):left+" 秒"));}else countdown.Text=left+" 秒";if(left==0&&!reported){reported=true;clock.Stop();finished();Close();}}
  }
}
