using System;
using System.Linq;
using System.Collections.Generic;
namespace RestBuddy {
  internal static class CareCatalog {
    private static readonly Dictionary<string,string[]> Groups=new Dictionary<string,string[]>{
      {"eyes",new[]{"eyes","blink"}},
      {"posture",new[]{"posture","hands","feet","stretch"}},
      {"rest",new[]{"rest","walk"}},
      {"water",new[]{"water"}}
    };
    internal static void Normalize(Options value){
      List<Rule> saved=value.Rules??new List<Rule>();List<Rule> current=new Options().Rules;
      Dictionary<string,string> oldText=value.CustomMessages??new Dictionary<string,string>();var migrated=new Dictionary<string,string>();
      foreach(Rule rule in current){
        string[] aliases=Groups[rule.Key];List<Rule> matches=aliases.Select(key=>saved.FirstOrDefault(x=>x!=null&&x.Key==key)).Where(x=>x!=null).ToList();
        if(matches.Count>0){rule.Enabled=matches.Any(x=>x.Enabled);Rule preferred=matches.FirstOrDefault(x=>x.Enabled)??matches[0];rule.Minutes=Math.Max(1,Math.Min(480,preferred.Minutes));}
        else if(saved.Count>0)rule.Enabled=false;
        List<string> lines=new List<string>();foreach(string key in aliases){string text;if(oldText.TryGetValue(key,out text)&&!String.IsNullOrWhiteSpace(text))lines.AddRange(text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).Where(x=>x.Length>0));}
        if(lines.Count>0)migrated[rule.Key]=String.Join("\n",lines.Distinct());
      }
      value.Rules=current;value.CustomMessages=migrated;
    }
  }
}
