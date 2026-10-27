using System;
using System.Linq;
using System.Collections.Generic;
namespace RestBuddy {
  internal sealed class CareText {
    private readonly Options options;private readonly Random random=new Random();
    private readonly Dictionary<string,int> last=new Dictionary<string,int>();
    internal CareText(Options value){options=value;}
    private string Pick(string key,string[] lines){int n=random.Next(lines.Length);int previous;if(lines.Length>1&&last.TryGetValue(key,out previous)&&n==previous)n=(n+1)%lines.Length;last[key]=n;return lines[n].Replace("{名字}",options.PetName);}
    internal string Message(List<Rule> rules){
      if(rules==null||rules.Count==0)return "找个合适的时机，照顾一下自己。";
      bool hasCustom=rules.Any(r=>options.CustomMessages.ContainsKey(r.Key)&&!String.IsNullOrWhiteSpace(options.CustomMessages[r.Key]));
      if(hasCustom){if(rules.Count>1)return String.Join("\n",rules.Select(r=>Message(new List<Rule>{r})));string[] custom=options.CustomMessages[rules[0].Key].Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray();if(custom.Length>0)return Pick("custom"+rules[0].Key,custom);}
      bool playful=options.Tone=="俏皮",brief=options.Tone=="简短";string key=rules[0].Key;string[] choices;
      if(rules.Any(r=>r.Key=="rest")&&rules.Any(r=>r.Key=="water"))return Pick("combined"+options.Tone,brief?new[]{"离屏休息五分钟，顺便按需喝点水。","起来走走，歇五分钟，也照顾一下饮水需要。"}:playful?new[]{"水杯想见你，椅子想放假。起来接点水，留五分钟走走歇歇？","你去接水走走，离屏歇五分钟。我替你看着桌面。"}:new[]{"这段忙完了吗？起来走一走，给自己五分钟离屏休息，顺便按需喝点水。","陪自己歇五分钟吧。离开座位走走，也看看要不要补点水。"});
      switch(key){
        case "eyes":choices=brief?new[]{"看看远处二十秒，自然眨几次眼睛。","目光离开屏幕二十秒，也轻轻眨眨眼。"}:playful?new[]{"屏幕交给我看着，你看看远处二十秒，再自然眨几次眼睛？","眼睛的小假期到了。看看远方，轻轻眨眼，不用盯着我倒计时。","和小猫做个眨眼约定：先看看远处，再轻轻合上眼睑，自然睁开。"}:new[]{"让目光离开屏幕，看看远处大约二十秒，也自然完整地眨几次眼睛。","这段看完，给眼睛一点远方吧。看看远处二十秒，再轻轻眨眨眼。","抬眼看看远处，好吗？目光放远片刻，也记得自然眨眼。"};break;
        case "posture":choices=brief?new[]{"放松肩膀，换个舒服的坐姿。","双手离开键盘片刻，轻轻活动手指。","背部靠稳，双脚有支撑。"}:playful?new[]{"检查一下肩膀有没有悄悄耸起来？放松它，找个舒服的位置。","鼠标也想歇会。松开它，让手指轻轻张开再放松。","椅子下面也照顾一下：让双脚踏实落地，背部舒服地靠稳。","爪爪舒展时间。手腕放自然，轻轻活动手指和前臂就好。"}:new[]{"肩膀还放松吗？换个舒服的坐姿，背部靠稳，双脚有支撑。","双手暂时离开键盘和鼠标，轻轻张开手指，再放松。","留意一下手腕有没有一直折着，轻轻活动手指和前臂，做到舒服就好。","检查一下背部和双脚的支撑，也让肩膀松一松。","舒服的姿势也可以换一换。轻轻调整椅背或双脚的位置吧。"};break;
        case "rest":choices=brief?new[]{"起身走走，离屏休息五分钟。","放下电脑，留五分钟给自己走动和休息。"}:playful?new[]{"椅子申请五分钟独处。你去走走歇会儿，我负责看桌面。","给工作按个小暂停。离开座位走两步，歇五分钟再回来。","小猫值班，人类休息。起来走一走，给自己五分钟离屏时间吧。"}:new[]{"这段告一段落的话，起来走一走，留五分钟给自己离屏休息。","放下鼠标，离开座位走走吧。歇五分钟，再慢慢继续。","方便的话，给自己一次完整的小休息。起身走动，离屏歇五分钟。"};break;
        case "water":choices=brief?new[]{"想喝点水吗？按自己的需要来。","方便的话喝几口水。"}:playful?new[]{"水杯发来一条好友申请，要不要见见它？","工作的小间隙，要不要顺手端一下水杯？"}:new[]{"手边有水吗？方便的话喝几口，按自己的需要来。","忙完手上这一步，想不想喝点水？","也照顾一下自己的饮水需要吧，有需要就喝几口。"};break;
        default:return "找个合适的时机，照顾一下自己。";
      }
      string message=Pick(key+options.Tone,choices);
      if(rules.Count>1){string second=rules[1].Key;if(second=="posture")message+=" 顺便放松肩膀和双手，检查一下支撑。";else if(second=="eyes")message+=" 也看看远处，自然眨几次眼睛。";else if(second=="rest")message+=" 方便的话，也起身走走，离屏休息五分钟。";else if(second=="water")message+=" 有需要的话，顺便喝几口水。";}
      return message;
    }
    internal string Play(string kind){
      if(kind=="pet")return Pick("pet",new[]{"摸摸收到，今天这只小猫的心情是晴天。","呼噜呼噜……谢谢你的摸摸，也记得对自己温柔一点。","额头贴贴。今天也很高兴能陪你待一会儿。","收到一份摸摸，回赠你一个小小的好心情。","小猫往你的手心蹭了一下，然后认真坐好。"});
      if(kind=="stretch")return Pick("stretch",new[]{"小猫先伸个懒腰。你也可以轻轻张开双手，再放松。","爪爪伸直——好啦。换你动动手指，做到舒服就好。","和我一起从键盘上抬起手，轻轻舒展一下？"});
      if(kind=="joke")return Pick("joke",new[]{"小猫的工位守则：键盘要暖，水杯要稳，人类要记得休息。","如果工作有进度条，我希望休息也能算在里面。","今天的桌面巡逻报告：鼠标没有逃跑，猫猫没有偷懒。","小猫面试被问擅长什么。我说：在你忙的时候安静，在你累的时候出现。","猫猫给今天的你发了一张优惠券：允许自己慢一点。"});
      return Pick("cheer",new[]{"先做好眼前这一小步，剩下的慢慢来。","今天不必每一分钟都很高效，也可以有喘口气的空隙。","累了就歇会儿，回来还能从刚才的地方继续。","今天已经做过的事情，也值得被看见。","我在这儿陪你。忙碌之外，也给自己留点余地。","把一大堆事情拆小一点，先完成最顺手的那一步。"});
    }
    internal string Reply(string action,int busy){
      if(action=="busy")return busy>1?"知道啦，你还在忙。我会安静更久，等你方便。":"好，我安静一阵。忙完也记得照顾自己。";
      if(action=="rested")return "收到，按你刚才的休息反馈更新啦。我晚一点再提醒。";
      if(action=="water")return "收到，记下这次饮水反馈了。我晚点再问。";
      if(action=="going")return "好，先歇五分钟。我会安静等你；休息好了也可以告诉我。";
      if(action=="completed")return "收到，这次完成啦。相关的关怀计时已经更新。";
      return Pick("hello",options.Tone=="简短"?new[]{"我在。需要安静陪伴，还是歇会儿？","照顾好自己，工作慢慢来。"}:new[]{"我在呢。想歇一会儿，还是让我安静陪着？","今天也在认真忙呀。你想歇会儿的时候，来找我就好。","摸摸收到。希望你今天工作顺利，也留一点时间给自己。"});
    }
  }
}
