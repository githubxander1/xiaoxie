using System;
namespace RestBuddy {
  internal static class NewsFeeds {
    internal const string Jin10="https://rsshub.rssforever.com/jin10";
    internal const string Wallstreet="https://rsshub.rssforever.com/wallstreetcn/live/global";
    internal const string BBC="https://feeds.bbci.co.uk/zhongwen/simp/rss.xml";
    internal const string IT="https://www.ithome.com/rss/";
    internal const string World="https://www.chinanews.com.cn/rss/world.xml",Life="https://www.chinanews.com.cn/rss/life.xml";
    internal static readonly string[] Diverse={IT,World,Life,Jin10};
    internal static readonly string[] Names={"财经混合 · 金十 + 华尔街见闻","财经 · 金十快讯","财经 · 华尔街见闻快讯","综合 · BBC中文","科技 · IT之家","自定义 RSS / 热榜","多元轮换 · 科技 / 国际 / 生活 / 财经","国际 · 中新网","生活 · 中新网"};
    internal static string Url(int index){return index==7?World:index==8?Life:index==2?Wallstreet:index==3?BBC:index==4?IT:Jin10;}
    internal static int Index(Options o){return o.DiverseNews?6:o.FinanceMix?0:o.NewsUrl==World?7:o.NewsUrl==Life?8:o.NewsUrl==Jin10?1:o.NewsUrl==Wallstreet?2:o.NewsUrl==BBC?3:o.NewsUrl==IT?4:5;}
  }
  // A separate wall clock: health feedback never resets the news deadline.
  internal sealed class AutoNewsClock {
    private readonly Options options;private readonly Random random;private int intervalUsed;private bool jitterUsed;
    internal DateTime DueUtc {get;private set;}
    internal AutoNewsClock(Options value,Random source,DateTime now){options=value;random=source;intervalUsed=value.NewsMinutes;jitterUsed=value.NewsJitter;DueUtc=now.AddMinutes(1);}
    internal bool IsDue(DateTime now){return options.AutoNews&&options.NewsEnabled&&now>=DueUtc;}
    internal void Consumed(DateTime now){intervalUsed=options.NewsMinutes;jitterUsed=options.NewsJitter;double jitter=options.NewsJitter?0.8+random.NextDouble()*0.4:1;DueUtc=now.AddMinutes(Math.Max(2,options.NewsMinutes)*jitter);}
    internal void Reconfigure(DateTime now){if(intervalUsed!=options.NewsMinutes||jitterUsed!=options.NewsJitter)Consumed(now);}
    internal void Retry(DateTime now){DueUtc=now.AddMinutes(3);}
  }
}
