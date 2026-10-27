using System;
using System.Drawing;
using System.Windows.Forms;
namespace RestBuddy {
  internal sealed class Coach : Form {
    private readonly System.Windows.Forms.Timer clock=new System.Windows.Forms.Timer();private DateTime finish;private Label countdown;private TextBox description;private Button done;private int seconds;
    protected override bool ShowWithoutActivation{get{return true;}}
    internal static int DurationSeconds(string key){return key=="eyes"?20:key=="blink"?15:key=="walk"||key=="stretch"?60:key=="rest"?300:30;}
    internal Coach(string key,string name,Action finished){
      seconds=DurationSeconds(key);
      Text="小歇陪你 · "+name;FormBorderStyle=FormBorderStyle.FixedToolWindow;TopMost=true;ShowInTaskbar=false;Font=Style.Font(9);BackColor=Style.Bg;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(345,245);
      TableLayoutPanel root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=1,RowCount=4};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
      root.Controls.Add(Style.Label(name,12),0,0);countdown=Style.Label("",24);countdown.ForeColor=Style.Green;root.Controls.Add(countdown,0,1);
      string step=key=="eyes"?"把目光移向远处约六米的位置，放松二十秒。可以自然眨眼，不用盯着倒计时。":key=="blink"?"轻轻、自然地完整眨眼几次。不用挤眼，也不必追着秒数做。":key=="walk"?"方便的话站起来走一会儿，暂时离开座位。注意脚下，量力而行。":key=="hands"?"双手离开键盘，轻轻张开手指，再放松。手腕保持自然，不用用力掰拉。":key=="feet"?"双脚放在地面或稳固脚踏上，背部靠稳，换个舒服的位置。":key=="posture"?"放松肩膀，背部有支撑，手腕自然，双脚稳稳放好。舒服的坐姿也可以经常换。":key=="rest"?"暂时放下电脑，给自己一点离屏的空白。倒计时结束也可以继续休息。":"让双手离开键盘，轻轻活动手指和前臂。做到舒服就好，不舒服就停。";
      description=new TextBox{Text=step,ForeColor=Style.Ink,BackColor=Style.Bg,Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,WordWrap=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Margin=new Padding(4,8,4,4),TabStop=false};root.Controls.Add(description,0,2);
      FlowLayoutPanel actions=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true};done=Style.Button("完成了",()=>{finished();Close();});done.Enabled=false;actions.Controls.Add(done);actions.Controls.Add(Style.Button("再来一次",Restart));actions.Controls.Add(Style.Button("先收起",Close));root.Controls.Add(actions,0,3);
      clock.Interval=250;clock.Tick+=(s,e)=>UpdateClock();Shown+=(s,e)=>Restart();FormClosed+=(s,e)=>clock.Dispose();
    }
    private void Restart(){done.Enabled=false;finish=DateTime.UtcNow.AddSeconds(seconds);clock.Start();UpdateClock();}
    private void UpdateClock(){int left=Math.Max(0,(int)Math.Ceiling((finish-DateTime.UtcNow).TotalSeconds));countdown.Text=left>0?left+" 秒":"这次完成了吗？";if(left==0){clock.Stop();done.Enabled=true;}}
  }
}
