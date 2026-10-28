using System;
using System.Linq;
using System.Collections.Generic;
namespace RestBuddy {
  internal sealed class CareAction {
    internal readonly string Id,Key,Title,Prompt,Instructions;internal readonly int Seconds;
    internal CareAction(string id,string key,string title,string prompt,string instructions,int seconds){Id=id;Key=key;Title=title;Prompt=prompt;Instructions=instructions;Seconds=seconds;}
  }
  internal sealed class CareActionPicker {
    private readonly Random random;private readonly Dictionary<string,string> previous=new Dictionary<string,string>();private string lastPractice="";
    private static readonly CareAction[] Actions={
      new CareAction("look","eyes","看看远处","把目光借给远处二十秒吧，也自然眨几次眼睛。","把目光移向远处约六米的位置，放松二十秒。自然眨眼，不用盯着倒计时。",20),
      new CareAction("window","eyes","眼睛小休息","先把视线移开屏幕，看看窗外或远处，轻轻眨眨眼。","视线离开屏幕，选择远处舒服的位置看看，保持自然眨眼。",20),
      new CareAction("shoulders","posture","放松肩膀","放松一下肩膀，换个舒服的坐姿。","留意肩膀有没有耸起，自然放松。轻轻换个坐姿，做到舒服就好。",30),
      new CareAction("hands","posture","双手放松","鼠标先歇会儿，轻轻张开手指，再放松。","双手离开键盘和鼠标，轻轻张开手指再放松。手腕保持自然，不用用力掰拉。",30),
      new CareAction("support","posture","检查支撑","背部靠稳，让双脚也有个舒服的落脚点。","背部有支撑，双脚稳稳放在地面或脚踏上。可以轻轻调整位置。",30),
      new CareAction("arms","posture","轻轻舒展","让手指和前臂轻轻活动一下，舒服就好。","手离开键盘，轻轻活动手指和前臂，到舒服的位置即可，不舒服就停。",30),
      new CareAction("walk","rest","离座走走","起来走两步，留五分钟给自己离屏休息。","放下电脑，起身走一走，按自己的情况活动。留五分钟离屏时间，休息好了再回来。",300),
      new CareAction("pause","rest","离屏小休息","给工作一个小暂停，离开屏幕歇五分钟吧。","暂时离开电脑，可以换个位置、起身走动，按自己的情况休息五分钟。",300)
    };
    internal CareActionPicker(Random source){random=source;}
    internal CareAction Next(string key){
      var pool=Actions.Where(x=>x.Key==key).ToList();if(pool.Count==0)return null;string last;previous.TryGetValue(key,out last);
      var alternatives=pool.Where(x=>x.Id!=last).ToList();if(alternatives.Count>0)pool=alternatives;CareAction selected=pool[random.Next(pool.Count)];previous[key]=selected.Id;return selected;
    }
    internal CareAction NextPractice(IEnumerable<Rule> rules){
      var keys=rules.Where(r=>r.Enabled&&r.Key!="water"&&Actions.Any(x=>x.Key==r.Key)).Select(r=>r.Key).Distinct().ToList();if(keys.Count==0)return null;
      var alternatives=keys.Where(x=>x!=lastPractice).ToList();if(alternatives.Count>0)keys=alternatives;lastPractice=keys[random.Next(keys.Count)];return Next(lastPractice);
    }
  }
}
