using System;
using System.Drawing;
using System.Windows.Forms;
namespace RestBuddy {
  internal sealed class Coach : Form {
    private readonly System.Windows.Forms.Timer clock=new System.Windows.Forms.Timer();private DateTime finish;private Label countdown;private TextBox description;private int seconds;private Action finished;private bool reported;
    protected override bool ShowWithoutActivation{get{return true;}}
    internal static int DurationSeconds(string key){return key=="eyes"?20:key=="rest"?300:30;}
    internal Coach(CareAction action,Action onFinished){
      seconds=action.Seconds;finished=onFinished;
      Text="小歇陪你 · "+action.Title;FormBorderStyle=FormBorderStyle.FixedToolWindow;TopMost=true;ShowInTaskbar=false;Font=Style.Font(9);BackColor=Style.Bg;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(345,245);
      TableLayoutPanel root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=1,RowCount=4};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
      root.Controls.Add(Style.Label(action.Title,12),0,0);countdown=Style.Label("",24);countdown.ForeColor=Style.Green;root.Controls.Add(countdown,0,1);
      string step=action.Instructions;
      description=new TextBox{Text=step,ForeColor=Style.Ink,BackColor=Style.Bg,Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,WordWrap=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Margin=new Padding(4,8,4,4),TabStop=false};root.Controls.Add(description,0,2);
      FlowLayoutPanel actions=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true};actions.Controls.Add(Style.Button("重新计时",Restart));actions.Controls.Add(Style.Button("先收起",Close));root.Controls.Add(actions,0,3);
      clock.Interval=250;clock.Tick+=(s,e)=>UpdateClock();Shown+=(s,e)=>Restart();FormClosed+=(s,e)=>clock.Dispose();
    }
    private void Restart(){reported=false;finish=DateTime.UtcNow.AddSeconds(seconds);clock.Start();UpdateClock();}
    private void UpdateClock(){int left=Math.Max(0,(int)Math.Ceiling((finish-DateTime.UtcNow).TotalSeconds));countdown.Text=left+" 秒";if(left==0&&!reported){reported=true;clock.Stop();finished();Close();}}
  }
}
