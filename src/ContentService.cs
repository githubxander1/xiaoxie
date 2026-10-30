using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace RestBuddy {
  public sealed class ContentItem {
    public string Kind,Text,Source,Url,QuoteKind,Id;public bool SourceLinkOnly;public DateTime? PublishedUtc;public DateTime FetchedUtc;public bool Cached;
    internal string Display(){string time=PublishedUtc.HasValue?"发布 "+PublishedUtc.Value.ToLocalTime().ToString("MM-dd HH:mm"):"获取 "+FetchedUtc.ToLocalTime().ToString("MM-dd HH:mm");return Text+"\n\n— "+Source+(Kind=="news"?" · "+time:"")+(Cached?" · 本地缓存":"");}
  }
  public sealed class NewsBatch {public string Url;public DateTime FetchedUtc;public List<ContentItem> Items=new List<ContentItem>();}
  public sealed class ContentCache {public List<NewsBatch> Batches=new List<NewsBatch>();public List<ContentItem> Quotes=new List<ContentItem>(),News=new List<ContentItem>();public string NewsUrl="";public List<string> Seen=new List<string>();}
  internal sealed class ContentService {
    private readonly string file;private ContentCache cache=new ContentCache();private readonly object gate=new object();private readonly Random random=new Random();private string lastQuote="";
    internal ContentService(string directory){file=Path.Combine(directory,"content-cache.json");try{ContentCache saved=Json().Deserialize<ContentCache>(File.ReadAllText(file));if(saved!=null){cache=saved;if(cache.Quotes==null)cache.Quotes=new List<ContentItem>();if(cache.News==null)cache.News=new List<ContentItem>();if(cache.Seen==null)cache.Seen=new List<string>();cache.Quotes=cache.Quotes.Where(Valid).Take(40).ToList();cache.News=cache.News.Where(Valid).Take(100).ToList();cache.Seen=cache.Seen.Take(200).ToList();if(cache.Batches==null)cache.Batches=new List<NewsBatch>();cache.Batches=cache.Batches.Where(x=>x!=null&&SafeUrl(x.Url)&&x.Items!=null).Take(5).ToList();foreach(NewsBatch batch in cache.Batches)batch.Items=batch.Items.Where(Valid).Take(60).ToList();}}catch{}}
    private static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=2*1024*1024,RecursionLimit=64};}
    private static bool Valid(ContentItem x){return x!=null&&!String.IsNullOrWhiteSpace(x.Text)&&x.Text.Length<=5000&&(String.IsNullOrEmpty(x.Url)||SafeUrl(x.Url));}
    internal static bool SafeUrl(string value){Uri uri;return Uri.TryCreate(value,UriKind.Absolute,out uri)&&(uri.Scheme==Uri.UriSchemeHttp||uri.Scheme==Uri.UriSchemeHttps)&&String.IsNullOrEmpty(uri.UserInfo);}
    private void Save(){try{string tmp=file+".tmp";File.WriteAllText(tmp,Json().Serialize(cache),Encoding.UTF8);if(File.Exists(file))File.Replace(tmp,file,null);else File.Move(tmp,file);}catch{}}
    internal Task<ContentItem> QuoteAsync(string kind,bool online){return Task.Run(()=>{lock(gate){
      if(online)try{string category=kind=="诗词"?"i":kind=="文学"?"d":kind=="动漫"?"a":kind=="影视"?"h":"d&c=i&c=k";
        ContentItem item=ParseQuote(Download("https://v1.hitokoto.cn/?c="+category+"&min_length=5&max_length=60"),kind,DateTime.UtcNow);
        cache.Quotes.RemoveAll(x=>x.Text==item.Text);cache.Quotes.Insert(0,item);cache.Quotes=cache.Quotes.Take(40).ToList();Save();lastQuote=item.Text;return item;
      }catch{}
      ContentItem[] saved=cache.Quotes.Where(x=>x.QuoteKind==kind&&x.Text!=lastQuote).ToArray();if(saved.Length>0){ContentItem item=saved[random.Next(saved.Length)];item.Cached=true;lastQuote=item.Text;return item;}
      string[] local={"忙碌的一天，也可以留一点空白给自己。","事情一件件做，休息也可以一点点来。","今天的你，值得被温柔对待。","先把肩膀放松，再和世界慢慢商量。","给眼睛一点远方，给自己一点余地。","慢一点也没关系，照顾好自己也是正事。"};
      string[] choices=local.Where(x=>x!=lastQuote).ToArray();lastQuote=choices[random.Next(choices.Length)];return new ContentItem{Kind="quote",Text=lastQuote,Source="小歇原创短句",FetchedUtc=DateTime.UtcNow,QuoteKind=kind};
    }});}
    internal static string Identity(string text){using(var sha=System.Security.Cryptography.SHA256.Create())return "id:"+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text??""))).Replace("-","");}
    internal static List<ContentItem> Candidates(IEnumerable<ContentItem> items,IEnumerable<string> seen,bool recent,DateTime now,int maxAgeHours=8){HashSet<string> used=new HashSet<string>(seen);return items.Where(x=>!used.Contains(x.Id??x.Url)&&!(x.SourceLinkOnly==false&&used.Contains(x.Url))&&(!recent||(!x.PublishedUtc.HasValue?(now-x.FetchedUtc).TotalMinutes<20:(now-x.PublishedUtc.Value).TotalHours<maxAgeHours))).OrderByDescending(x=>x.PublishedUtc??x.FetchedUtc).ToList();}
    internal Task<ContentItem> NewsAsync(string url,bool randomRecent=false,int maxAgeHours=8){return Task.Run(()=>{lock(gate){
      if(!SafeUrl(url))throw new InvalidOperationException("请填写有效的 RSS 或热榜地址。");
      NewsBatch batch=cache.Batches.FirstOrDefault(x=>x.Url==url);bool failed=false;
      if(batch==null||(DateTime.UtcNow-batch.FetchedUtc).TotalMinutes>=3)try{
        List<ContentItem> news=ParseNews(Download(url),url,DateTime.UtcNow);if(news.Count==0)throw new InvalidDataException();
        batch=new NewsBatch{Url=url,FetchedUtc=DateTime.UtcNow,Items=news.Take(60).ToList()};cache.Batches.RemoveAll(x=>x.Url==url);cache.Batches.Insert(0,batch);cache.Batches=cache.Batches.Take(5).ToList();Save();
      }catch{failed=true;}
      if(batch==null||batch.Items.Count==0)throw new InvalidOperationException("这次没取到快讯，过会儿再试，或换一个来源。");
      List<ContentItem> pool=Candidates(batch.Items,cache.Seen,randomRecent,DateTime.UtcNow,maxAgeHours);if(pool.Count==0)throw new InvalidOperationException("暂时没有新的近期快讯。");
      ContentItem item=randomRecent?pool[random.Next(Math.Min(15,pool.Count))]:pool[0];item.Cached=failed;return item;
    }});}
    internal void MarkSeen(ContentItem item){lock(gate){cache.Seen.Insert(0,item.Id??item.Url);cache.Seen=cache.Seen.Distinct().Take(200).ToList();Save();}}
    internal static string Download(string url){
      if(!SafeUrl(url))throw new InvalidDataException("无效地址");System.Diagnostics.Stopwatch elapsed=System.Diagnostics.Stopwatch.StartNew();HttpWebRequest request=(HttpWebRequest)WebRequest.Create(url);request.UserAgent="RestBuddy/0.4 (+desktop companion)";request.Timeout=8000;request.ReadWriteTimeout=8000;request.AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate;request.MaximumAutomaticRedirections=4;
      using(WebResponse response=request.GetResponse())using(Stream input=response.GetResponseStream())using(MemoryStream bytes=new MemoryStream()){
        byte[] buffer=new byte[8192];int count;while((count=input.Read(buffer,0,buffer.Length))>0){if(elapsed.ElapsedMilliseconds>12000)throw new IOException("来源响应超时");if(bytes.Length+count>1024*1024)throw new InvalidDataException("来源内容过大");bytes.Write(buffer,0,count);}
        Encoding encoding=Encoding.UTF8;string charset=((HttpWebResponse)response).CharacterSet;try{if(!String.IsNullOrEmpty(charset)&&charset!="ISO-8859-1")encoding=Encoding.GetEncoding(charset);}catch{}return encoding.GetString(bytes.ToArray()).TrimStart('\uFEFF');
      }
    }
    private static string Field(IDictionary<string,object> value,string name){object found;return value.TryGetValue(name,out found)&&found!=null?Convert.ToString(found,CultureInfo.InvariantCulture):"";}
    private static string Clean(string text){return Regex.Replace(WebUtility.HtmlDecode(text??""),"<[^>]*>"," ").Trim();}
    internal static ContentItem ParseQuote(string json,string kind,DateTime now){
      IDictionary<string,object> value=Json().DeserializeObject(json) as IDictionary<string,object>;if(value==null)throw new InvalidDataException();string text=Clean(Field(value,"hitokoto"));if(String.IsNullOrWhiteSpace(text)||text.Length>500)throw new InvalidDataException();
      string from=Clean(Field(value,"from")),who=Clean(Field(value,"from_who"));string source=(String.IsNullOrWhiteSpace(who)?"":who+" · ")+(String.IsNullOrWhiteSpace(from)?"来源未注明":from)+" / 一言";
      string uuid=Field(value,"uuid");return new ContentItem{Kind="quote",Text=text,Source=source,Url=String.IsNullOrWhiteSpace(uuid)?"https://hitokoto.cn/":"https://hitokoto.cn/?uuid="+Uri.EscapeDataString(uuid),QuoteKind=kind,FetchedUtc=now};
    }
    private static DateTime? Date(string value){DateTimeOffset result;if(DateTimeOffset.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.AllowWhiteSpaces,out result))return result.UtcDateTime;return null;}
    private static string Element(XElement parent,string name){XElement element=parent.Elements().FirstOrDefault(e=>e.Name.LocalName==name);return element==null?"":element.Value;}
    internal static List<ContentItem> ParseNews(string body,string url,DateTime now){
      if(!SafeUrl(url))throw new InvalidDataException();List<ContentItem> result=new List<ContentItem>();
      if(body.TrimStart().StartsWith("<")){
        XmlReaderSettings settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1024*1024};
        XDocument document;using(XmlReader reader=XmlReader.Create(new StringReader(body),settings))document=XDocument.Load(reader);
        XElement root=document.Root;if(root==null)throw new InvalidDataException();XElement channel=root.Elements().FirstOrDefault(x=>x.Name.LocalName=="channel")??root;string source=Clean(Element(channel,"title"));if(String.IsNullOrEmpty(source))source=new Uri(url).Host;string sourceLink=Element(channel,"link");
        if(source.Contains("金十")||source.Contains("华尔街见闻"))source+="（RSS中转）";
        foreach(XElement item in root.Descendants().Where(x=>x.Name.LocalName=="item"||x.Name.LocalName=="entry").Take(100)){
          string title=Clean(Element(item,"title")),link=Element(item,"link");if(!SafeUrl(link)){XElement atom=item.Elements().FirstOrDefault(x=>x.Name.LocalName=="link"&&((string)x.Attribute("rel")==null||(string)x.Attribute("rel")=="alternate"));link=atom==null?"":(string)atom.Attribute("href");}
          if(!SafeUrl(link)&&!String.IsNullOrWhiteSpace(link)){Uri relative;if(Uri.TryCreate(new Uri(url),link,out relative))link=relative.AbsoluteUri;}
          string guid=Element(item,"guid");if(String.IsNullOrWhiteSpace(guid))guid=Element(item,"id");
          bool fallback=false;if(String.IsNullOrWhiteSpace(link)&&!String.IsNullOrWhiteSpace(guid)&&SafeUrl(sourceLink)){link=sourceLink;fallback=true;}
          string id=Identity(url+"|"+(String.IsNullOrWhiteSpace(guid)?link+"|"+title:guid));
          DateTime? date=Date(Element(item,"pubDate"))??Date(Element(item,"published"))??Date(Element(item,"updated"));
          if(title.Length>0&&title.Length<=5000&&SafeUrl(link)&&!result.Any(x=>x.Id==id))result.Add(new ContentItem{Id=id,SourceLinkOnly=fallback,Kind="news",Text=title,Source=source,Url=link,PublishedUtc=date,FetchedUtc=now});
        }
      }else{
        IDictionary<string,object> root=Json().DeserializeObject(body) as IDictionary<string,object>;object data;if(root==null||!root.TryGetValue("data",out data)||!(data is IEnumerable))throw new InvalidDataException();string source=Clean(Field(root,"name"));if(String.IsNullOrWhiteSpace(source))source=new Uri(url).Host+" 热榜";
        foreach(object raw in (IEnumerable)data){IDictionary<string,object> item=raw as IDictionary<string,object>;if(item==null)continue;string title=Clean(Field(item,"title")),link=Field(item,"url");if(String.IsNullOrWhiteSpace(link))link=Field(item,"mobileUrl");if(title.Length>0&&title.Length<=1000&&SafeUrl(link)&&!result.Any(x=>x.Url==link))result.Add(new ContentItem{Id=Identity(url+"|"+link),Kind="news",Text=title,Source=source,Url=link,FetchedUtc=now});if(result.Count>=100)break;}
      }
      return result;
    }
  }
}
