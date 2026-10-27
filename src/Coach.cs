using System;
using System.Drawing;
using System.Windows.Forms;
namespace RestBuddy {
  internal sealed class Coach : Form {
    private readonly System.Windows.Forms.Timer clock=new System.Windows.Forms.Timer();private DateTime finish;private Label countdown;private TextBox description;private Button done;private int seconds;
    protected override bool ShowWithoutActivation{get{return true;}}
    internal static int DurationSeconds(string key){return key=="eyes"?20:key=="rest"?300:30;}
    internal Coach(string key,string name,Action finished){
      seconds=DurationSeconds(key);
      Text="小歇陪你 · "+name;FormBorderStyle=FormBorderStyle.FixedToolWindow;TopMost=true;ShowInTaskbar=false;Font=Style.Font(9);BackColor=Style.Bg;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(345,245);
      TableLayoutPanel root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=1,RowCount=4};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
      root.Controls.Add(Style.Label(name,12),0,0);countdown=Style.Label("",24);countdown.ForeColor=Style.Green;root.Controls.Add(countdown,0,1);
      string step=key=="eyes"?"把目光移向远处约六米的位置，放松二十秒。可以自然眨眼，不用盯着倒计时。":key=="rest"?"放下电脑，起身走一走，留五分钟离屏休息。注意脚下，按自己的情况活动，休息好了再回来。":"放松肩膀，背部和双脚有支撑。双手离开键盘，轻轻活动手指和前臂，做到舒服就好。";
      description=new TextBox{Text=step,ForeColor=Style.Ink,BackColor=Style.Bg,Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,WordWrap=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Margin=new Padding(4,8,4,4),TabStop=false};root.Controls.Add(description,0,2);
      FlowLayoutPanel actions=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true};done=Style.Button("完成了",()=>{finished();Close();});done.Enabled=false;actions.Controls.Add(done);actions.Controls.Add(Style.Button("再来一次",Restart));actions.Controls.Add(Style.Button("先收起",Close));root.Controls.Add(actions,0,3);
      clock.Interval=250;clock.Tick+=(s,e)=>UpdateClock();Shown+=(s,e)=>Restart();FormClosed+=(s,e)=>clock.Dispose();
    }
    private void Restart(){done.Enabled=false;finish=DateTime.UtcNow.AddSeconds(seconds);clock.Start();UpdateClock();}
    private void UpdateClock(){int left=Math.Max(0,(int)Math.Ceiling((finish-DateTime.UtcNow).TotalSeconds));countdown.Text=left>0?left+" 秒":"这次完成了吗？";if(left==0){clock.Stop();done.Enabled=true;}}
  }
}
