using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Web.Script.Serialization;
namespace RestBuddy {
  public sealed class ReminderRecord {
    public string Id,Kind,Text,Source,Url;public DateTime ShownUtc;public DateTime? PublishedUtc;public bool SourceLinkOnly;
  }
  internal sealed class ReminderHistory {
    internal const int Limit=500;private readonly string file;private List<ReminderRecord> records=new List<ReminderRecord>();internal string Error="";internal event Action Changed;
    internal ReminderHistory(string folder){file=Path.Combine(folder,"reminder-history.json");if(File.Exists(file)){try{records=Read(file);}catch{try{records=Read(file+".bak");}catch{Error="记录文件无法读取，旧文件已保留。";}}}Trim();}
    private List<ReminderRecord> Read(string path){return new JavaScriptSerializer{MaxJsonLength=8*1024*1024}.Deserialize<List<ReminderRecord>>(File.ReadAllText(path))??new List<ReminderRecord>();}
    private void Trim(){records=records.Where(r=>r!=null&&!String.IsNullOrWhiteSpace(r.Text)).OrderByDescending(r=>r.ShownUtc).Take(Limit).ToList();}
    internal List<ReminderRecord> Find(string kind,string query){return records.Where(r=>(kind=="全部"||r.Kind==kind)&&(String.IsNullOrWhiteSpace(query)||(r.Text+" "+r.Source).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)).ToList();}
    internal void Add(string kind,string text,string source,string url,DateTime? published,bool sourceOnly){if(String.IsNullOrWhiteSpace(text))return;records.Insert(0,new ReminderRecord{Id=Guid.NewGuid().ToString("N"),Kind=kind,Text=text.Length>5000?text.Substring(0,5000):text,Source=source,Url=ContentService.SafeUrl(url)?url:null,PublishedUtc=published,ShownUtc=DateTime.UtcNow,SourceLinkOnly=sourceOnly});Trim();Save();if(Changed!=null)Changed();}
    internal void Clear(){records.Clear();Save();try{if(Error.Length==0&&File.Exists(file+".bak"))File.Copy(file,file+".bak",true);}catch{Error="记录已清空，备份暂未更新。";}if(Changed!=null)Changed();}
    private void Save(){try{if(Error.StartsWith("记录文件无法"))return;Directory.CreateDirectory(Path.GetDirectoryName(file));string temp=file+".tmp";File.WriteAllText(temp,new JavaScriptSerializer{MaxJsonLength=8*1024*1024}.Serialize(records));if(File.Exists(file))File.Replace(temp,file,file+".bak");else File.Move(temp,file);Error="";}catch{Error="记录暂未保存到磁盘。";}}
  }
  internal sealed class HistoryWindow:Form {
    private readonly ReminderHistory history;private ComboBox kind;private TextBox search,details;private ListView list;private Button open;private Label status;private ReminderRecord selected;
    internal HistoryWindow(ReminderHistory value){history=value;Text="提醒记录";Font=Style.Font(9);BackColor=Style.Bg;ClientSize=new Size(510,380);MinimumSize=new Size(430,330);StartPosition=FormStartPosition.CenterScreen;ShowInTaskbar=false;AutoScaleMode=AutoScaleMode.Dpi;
      TableLayoutPanel root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(8),ColumnCount=1,RowCount=4};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,55));root.RowStyles.Add(new RowStyle(SizeType.Percent,45));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
      FlowLayoutPanel filters=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=false};kind=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=78};kind.Items.AddRange(new[]{"全部","新闻","语录","健康"});kind.SelectedIndex=0;search=new TextBox{Width=240};search.AccessibleName="搜索记录内容或来源";filters.Controls.Add(kind);filters.Controls.Add(search);filters.Controls.Add(Style.Label("搜索",9));root.Controls.Add(filters,0,0);
      list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false};list.Columns.Add("展示时间",110);list.Columns.Add("类型",44);list.Columns.Add("内容",300);root.Controls.Add(list,0,1);
      details=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=Style.Bg,WordWrap=true};root.Controls.Add(details,0,2);
      FlowLayoutPanel footer=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};open=Style.Button("打开原文",OpenSource);footer.Controls.Add(open);footer.Controls.Add(Style.Button("清空记录",()=>{if(MessageBox.Show(this,"清空所有提醒记录？","提醒记录",MessageBoxButtons.YesNo)==DialogResult.Yes)history.Clear();}));status=Style.Label("",9);footer.Controls.Add(status);root.Controls.Add(footer,0,3);
      list.SelectedIndexChanged+=(s,e)=>SelectRecord();list.DoubleClick+=(s,e)=>OpenSource();kind.SelectedIndexChanged+=(s,e)=>RefreshRecords();search.TextChanged+=(s,e)=>RefreshRecords();history.Changed+=RefreshRecords;FormClosed+=(s,e)=>history.Changed-=RefreshRecords;RefreshRecords();
    }
    private void RefreshRecords(){string id=selected==null?null:selected.Id;list.BeginUpdate();list.Items.Clear();foreach(ReminderRecord r in history.Find(kind.Text,search.Text)){ListViewItem item=new ListViewItem(new[]{r.ShownUtc.ToLocalTime().ToString("MM-dd HH:mm:ss"),r.Kind,r.Text.Replace("\r","").Replace("\n"," ")});item.Tag=r;list.Items.Add(item);if(r.Id==id)item.Selected=true;}if(list.SelectedItems.Count==0&&list.Items.Count>0)list.Items[0].Selected=true;list.EndUpdate();SelectRecord();status.Text=history.Error.Length>0?history.Error:list.Items.Count+" 条 · 最多 500 条";}
    private void SelectRecord(){selected=list.SelectedItems.Count==0?null:(ReminderRecord)list.SelectedItems[0].Tag;details.Text=selected==null?"暂无记录；从此版本开始保存已展示内容。":selected.Text+"\r\n\r\n来源："+selected.Source+"\r\n展示："+selected.ShownUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")+(selected.PublishedUtc.HasValue?"\r\n发布："+selected.PublishedUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm"):"")+(!String.IsNullOrEmpty(selected.Url)?"\r\n"+selected.Url:"");open.Enabled=selected!=null&&ContentService.SafeUrl(selected.Url);open.Text=selected!=null&&selected.SourceLinkOnly?"打开来源页":"打开原文";}
    private void OpenSource(){if(selected==null||!ContentService.SafeUrl(selected.Url))return;try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(selected.Url){UseShellExecute=true});}catch{status.Text="暂时无法打开链接";}}
  }
}
