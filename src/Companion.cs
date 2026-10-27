using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RestBuddy {
  internal static class PetPainter {
    internal static GraphicsPath Round(RectangleF r,float radius){GraphicsPath p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    internal static void Draw(Graphics g,int size,double phase,bool sleepy){
      g.SmoothingMode=SmoothingMode.AntiAlias;GraphicsState saved=g.Save();g.ScaleTransform(size/96f,size/96f);g.TranslateTransform(0,(float)Math.Sin(phase)*0.8f);
      using(Pen outline=new Pen(Color.FromArgb(50,92,77),2.2f))using(Brush fur=new SolidBrush(Color.FromArgb(246,244,226)))using(Brush green=new SolidBrush(Color.FromArgb(101,145,114)))using(Brush blush=new SolidBrush(Color.FromArgb(229,181,165))){
        outline.LineJoin=LineJoin.Round;outline.StartCap=LineCap.Round;outline.EndCap=LineCap.Round;
        using(GraphicsPath tail=new GraphicsPath()){tail.AddBezier(70,78,91,78,90,48,82,56);using(Pen stroke=new Pen(Color.FromArgb(101,145,114),8)){stroke.StartCap=LineCap.Round;stroke.EndCap=LineCap.Round;g.DrawPath(stroke,tail);}}
        g.FillEllipse(green,25,53,48,35);g.DrawEllipse(outline,25,53,48,35);
        using(GraphicsPath head=new GraphicsPath()){
          head.AddLine(20,33,18,9);head.AddBezier(18,9,20,6,30,15,37,22);head.AddBezier(37,22,43,19,53,19,59,22);head.AddBezier(59,22,66,15,76,6,78,9);head.AddLine(78,9,77,34);head.AddBezier(77,34,88,55,74,67,49,68);head.AddBezier(49,68,23,68,9,55,20,33);head.CloseFigure();g.FillPath(fur,head);g.DrawPath(outline,head);
        }
        using(Brush ears=new SolidBrush(Color.FromArgb(230,190,167))){g.FillPolygon(ears,new[]{new PointF(23,16),new PointF(25,31),new PointF(33,25)});g.FillPolygon(ears,new[]{new PointF(73,16),new PointF(70,31),new PointF(62,25)});}
        bool blink=sleepy||Math.Sin(phase*0.42)>0.985;
        if(blink){g.DrawArc(outline,31,39,9,7,10,160);g.DrawArc(outline,57,39,9,7,10,160);}else{using(Brush eye=new SolidBrush(Color.FromArgb(50,79,64))){g.FillEllipse(eye,34,39,4,7);g.FillEllipse(eye,59,39,4,7);}}
        g.FillEllipse(blush,24,48,10,5);g.FillEllipse(blush,66,48,10,5);
        g.DrawLine(outline,46,49,50,49);g.DrawBezier(outline,48,50,48,57,42,56,42,53);g.DrawBezier(outline,48,50,48,57,54,56,54,53);
        g.FillEllipse(fur,29,77,16,10);g.FillEllipse(fur,53,77,16,10);g.DrawArc(outline,29,77,16,10,0,180);g.DrawArc(outline,53,77,16,10,0,180);
        using(Brush leaf=new SolidBrush(Color.FromArgb(206,225,169))){g.FillEllipse(leaf,43,63,10,7);g.DrawLine(outline,48,64,48,72);}
      }g.Restore(saved);
    }
  }
  internal sealed class Companion : Form {
    private readonly Buddy buddy;private readonly System.Windows.Forms.Timer animation=new System.Windows.Forms.Timer();private ToolTip hint=new ToolTip();
    private double phase;private string reaction="";private DateTime reactionUntil;private bool sleepy,dragged,down;private Point press,origin;
    internal bool AllowClose;internal string LastWords="我在这里陪你。";
    protected override bool ShowWithoutActivation{get{return true;}}
    internal Companion(Buddy value){
      buddy=value;Text="小歇 · 桌面伙伴";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;BackColor=Color.Magenta;TransparencyKey=Color.Magenta;DoubleBuffered=true;Cursor=Cursors.Hand;
      animation.Interval=180;animation.Tick+=(s,e)=>{phase+=0.16;Invalidate();};
      MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;down=true;dragged=false;press=Cursor.Position;origin=Location;Capture=true;};
      MouseMove+=(s,e)=>{if(!down)return;Point now=Cursor.Position;int dx=now.X-press.X,dy=now.Y-press.Y;if(Math.Abs(dx)+Math.Abs(dy)>5)dragged=true;if(dragged)Location=new Point(origin.X+dx,origin.Y+dy);};
      MouseUp+=(s,e)=>{if(e.Button!=MouseButtons.Left||!down)return;down=false;Capture=false;if(dragged){Clamp();buddy.Options.PetX=Left;buddy.Options.PetY=Top;buddy.Persist();}else if(buddy.Options.Mode=="compact")buddy.SetMode("expanded");else buddy.Interact();};
      MouseWheel+=(s,e)=>{if(buddy.Options.Mode!="expanded")return;buddy.Options.PetSize=Math.Max(56,Math.Min(120,buddy.Options.PetSize+(e.Delta>0?8:-8)));ApplyMode();buddy.Persist();};
      MouseEnter+=(s,e)=>hint.SetToolTip(this,LastWords+"\n右键：收起、隐藏、关怀设置");
      SystemEventsHook(true);
    }
    private void SystemEventsHook(bool add){if(add)Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplayChanged;else Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplayChanged;}
    private void DisplayChanged(object sender,EventArgs e){if(IsDisposed)return;try{BeginInvoke(new Action(()=>Clamp()));}catch(InvalidOperationException){}}
    internal void React(string kind){reaction=kind;reactionUntil=DateTime.UtcNow.AddSeconds(5);Invalidate();}
    internal void SetMood(bool quiet){if(sleepy!=quiet){sleepy=quiet;Invalidate();}}
    internal void ApplyMode(){
      animation.Stop();if(buddy.Options.Mode=="hidden"){Hide();return;}
      int size=buddy.Options.Mode=="compact"?28:buddy.Options.PetSize;Size=new Size(size,size);
      if(buddy.Options.PetX==-1&&buddy.Options.PetY==-1){Rectangle area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Right-size-22,area.Bottom-size-18);}else Location=new Point(buddy.Options.PetX,buddy.Options.PetY);
      Clamp();Show();if(buddy.Options.Mode=="expanded")animation.Start();Invalidate();
    }
    private void Clamp(){Rectangle area=Screen.FromPoint(new Point(Left+Width/2,Top+Height/2)).WorkingArea;Left=Math.Max(area.Left,Math.Min(Left,area.Right-Width));Top=Math.Max(area.Top,Math.Min(Top,area.Bottom-Height));}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(buddy.Options.Mode=="compact"){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(Brush b=new SolidBrush(Style.Green))e.Graphics.FillEllipse(b,2,2,24,24);using(Pen p=new Pen(Color.FromArgb(232,243,218),1.6f)){e.Graphics.DrawLines(p,new[]{new Point(10,17),new Point(14,12),new Point(18,17)});}}else {bool reacting=DateTime.UtcNow<reactionUntil;PetPainter.Draw(e.Graphics,Width,phase,reacting?reaction=="pet":sleepy);
      if(reacting){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;if(reaction=="stretch"){using(Pen p=new Pen(Style.Green,2)){e.Graphics.DrawLine(p,Width*0.22f,Height*0.67f,Width*0.12f,Height*0.5f);e.Graphics.DrawLine(p,Width*0.78f,Height*0.67f,Width*0.88f,Height*0.5f);}}else {using(Brush b=new SolidBrush(Color.FromArgb(200,109,118)))e.Graphics.DrawString("♥",Style.Font(10),b,new PointF(Width*0.75f,Height*0.13f));}}}}
    protected override void OnFormClosing(FormClosingEventArgs e){if(!AllowClose&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;buddy.SetMode("hidden");}base.OnFormClosing(e);}
    protected override void OnFormClosed(FormClosedEventArgs e){animation.Dispose();hint.Dispose();SystemEventsHook(false);base.OnFormClosed(e);}
  }
  internal sealed class SpeechAction {
    internal string Label,Key;internal SpeechAction(string label,string key){Label=label;Key=key;}
  }
  internal sealed class Speech : Form {
    private readonly Buddy buddy;private readonly System.Windows.Forms.Timer lifetime=new System.Windows.Forms.Timer();private bool closing;
    protected override bool ShowWithoutActivation{get{return true;}}
    internal Speech(Buddy value,string message,string caption,SpeechAction[] actions,Action<string> handler,int duration){
      buddy=value;string petName=buddy==null?"小歇":buddy.Options.PetName;Text=petName+" · "+caption;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;Font=Style.Font(9);BackColor=Style.Bg;AutoScaleMode=AutoScaleMode.Dpi;Padding=new Padding(16,12,16,12);
      float factor=1;using(Graphics measure=CreateGraphics())factor=measure.DpiX/96f;
      int width=(int)(380*factor);int measured=TextRenderer.MeasureText(message,Font,new Size(width-(int)(68*factor),Int32.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl|TextFormatFlags.NoPadding).Height;
      Rectangle working=Screen.FromPoint(Cursor.Position).WorkingArea;
      ClientSize=new Size(Math.Min(width,working.Width-24),Math.Min(Math.Max((int)(190*factor),measured+(int)(142*factor)),Math.Min((int)(510*factor),working.Height-30)));
      TableLayoutPanel root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
      TableLayoutPanel top=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=2};top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,26));Label name=Style.Label(petName,11);name.ForeColor=Style.Green;top.Controls.Add(name,0,0);Button dismiss=Style.Button("×",()=>handler("ignored"));dismiss.AutoSize=false;dismiss.Size=new Size(24,24);dismiss.Padding=Padding.Empty;dismiss.Margin=Padding.Empty;dismiss.FlatAppearance.BorderSize=0;top.Controls.Add(dismiss,1,0);root.Controls.Add(top,0,0);
      TextBox content=new TextBox{Text=message.Replace("\n","\r\n").Replace("\r\r\n","\r\n"),ForeColor=Style.Ink,BackColor=Style.Bg,Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,WordWrap=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Margin=new Padding(4,10,4,10),TabStop=false};content.AccessibleName="完整内容，可滚动阅读";root.Controls.Add(content,0,1);
      FlowLayoutPanel buttons=new FlowLayoutPanel{AutoSize=true,WrapContents=true,Dock=DockStyle.Fill};foreach(SpeechAction action in actions){string key=action.Key;Button b=Style.Button(action.Label,()=>handler(key));b.Font=Style.Font(9);b.Padding=new Padding(6,3,6,3);buttons.Controls.Add(b);}root.Controls.Add(buttons,0,2);
      content.MouseEnter+=(s,e)=>{if(!closing)lifetime.Stop();};content.MouseLeave+=(s,e)=>{if(!IsDisposed&&!closing)lifetime.Start();};
      buttons.MouseEnter+=(s,e)=>{if(!closing)lifetime.Stop();};buttons.MouseLeave+=(s,e)=>{if(!IsDisposed&&!closing)lifetime.Start();};
      lifetime.Interval=duration;lifetime.Tick+=(s,e)=>{lifetime.Stop();handler("ignored");};Shown+=(s,e)=>{Place();lifetime.Start();};FormClosed+=(s,e)=>lifetime.Dispose();Resize+=(s,e)=>Round();Round();
    }
    private void Round(){if(Width<30||Height<30)return;using(GraphicsPath path=PetPainter.Round(new RectangleF(0,0,Width-1,Height-1),12)){Region old=Region;Region=new Region(path);if(old!=null)old.Dispose();}}
    private void Place(){Rectangle area;Point location;
      if(buddy!=null&&buddy.Pet!=null&&buddy.Pet.Visible){Rectangle pet=buddy.Pet.Bounds;area=Screen.FromRectangle(pet).WorkingArea;location=new Point(pet.Right-Width,pet.Top-Height-8);if(location.Y<area.Top)location.Y=pet.Bottom+8;}
      else{area=Screen.FromPoint(Cursor.Position).WorkingArea;location=new Point(area.Right-Width-16,area.Bottom-Height-16);}
      Location=new Point(Math.Max(area.Left,Math.Min(location.X,area.Right-Width)),Math.Max(area.Top,Math.Min(location.Y,area.Bottom-Height)));
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(GraphicsPath path=PetPainter.Round(new RectangleF(0.5f,0.5f,Width-2,Height-2),12))using(Pen p=new Pen(Color.FromArgb(212,224,211)))e.Graphics.DrawPath(p,path);}
    protected override void OnFormClosing(FormClosingEventArgs e){closing=true;lifetime.Stop();base.OnFormClosing(e);}
  }
}
