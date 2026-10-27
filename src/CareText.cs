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
      List<string> custom=new List<string>();foreach(Rule rule in rules){string text;if(options.CustomMessages.TryGetValue(rule.Key,out text)&&!String.IsNullOrWhiteSpace(text)){string[] lines=text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray();if(lines.Length>0)custom.Add(Pick("custom"+rule.Key,lines));}}
      if(custom.Count>0)return String.Join("\n",custom);
      bool water=rules.Any(r=>r.Key=="water"),move=rules.Any(r=>new[]{"walk","rest","stretch"}.Contains(r.Key));
      if(water&&move)return Pick("combined"+options.Tone,options.Tone=="俏皮"?new[]{"水杯和椅子都在等你：一个想见你，一个想放假。起来接点水，顺便走走？","给工作按个小暂停吧。你去接水走两步，我替你守着桌面。"}:options.Tone=="简短"?new[]{"起来走走，顺便接点水。","接杯水，离开座位片刻。"}:new[]{"这段忙完了吗？起来接点水，顺便走两步。","陪我歇一小会儿吧。去接点水，也让身体换个姿势。"});
      string key=rules[0].Key;string[] choices;
      bool playful=options.Tone=="俏皮",brief=options.Tone=="简短";
      switch(key){
        case "eyes":choices=brief?new[]{"看看远处，让眼睛歇会。","先把视线移开屏幕吧。"}:playful?new[]{"屏幕交给我看着，你去看看窗外？","眼睛今天也很努力，给它放个小假吧。"}:new[]{"先把目光移开屏幕，看看远处大约二十秒，好吗？","这段看完，就让眼睛歇一会儿吧。","窗外有什么新鲜事吗？把目光借给远处一小会儿。"};break;
        case "posture":choices=brief?new[]{"放松肩膀，换个坐姿。","调整坐姿，让身体舒服些。"}:playful?new[]{"检查一下肩膀有没有悄悄耸起来？放松它，再换个舒服的坐姿。","和椅子重新谈谈坐姿吧，找个舒服的位置。"}:new[]{"肩膀还放松吗？换个舒服的坐姿吧。","忙的时候容易忘记身体。轻轻放松肩膀，调整一下坐姿。"};break;
        case "water":choices=brief?new[]{"想喝点水吗？","方便的话，喝几口水。"}:playful?new[]{"水杯发来一条好友申请，要不要见见它？","工作的小间隙，要不要顺手端一下水杯？"}:new[]{"手边有水吗？方便的话喝几口，按自己的需要来。","忙完手上这一步，想不想喝点水？","我在这儿陪着你。也记得照顾一下自己，喝点水吧。"};break;
        case "walk":choices=brief?new[]{"起身走两步吧。","离开座位活动一下。"}:playful?new[]{"椅子申请短暂独处，你起来走两步？","给桌面一次想你的机会，起身转一圈吧。"}:new[]{"坐着忙了有一阵。方便的话，起来走两步吧。","事情可以稍微等一等。站起来换换位置，陪自己歇一小会儿。"};break;
        case "stretch":choices=brief?new[]{"轻轻活动肩膀和手腕。","换个姿势，舒展一下。"}:playful?new[]{"让手指和前臂轻轻舒展一下，舒服就好，不用用力掰拉。","键盘先交给我，给肩膀和手腕一点活动时间？"}:new[]{"试着轻轻活动肩膀和手腕，做到舒服就好。","让双手暂时离开键盘，轻轻舒展一下吧。"};break;
        case "rest":choices=brief?new[]{"休息几分钟再继续吧。","放下手头的事，歇一会。"}:playful?new[]{"你的电量也值得被照顾。去歇会儿，我负责看桌面。","这场和工作的拉锯战，可以先按一下暂停。"}:new[]{"给自己留几分钟离屏休息吧，放下鼠标，让手和眼睛都缓一缓。","这段告一段落的话，就完整歇一会儿吧。我在这里等你。"};break;
        case "blink":choices=brief?new[]{"自然完整地眨几次眼睛。","眼睛也歇一下，轻轻眨眼。"}:playful?new[]{"和我一起慢慢眨几次眼睛？让眼睑自然合上，不用挤眼。","这次互动叫：眨眨眼。轻轻合上，再自然睁开就好。"}:new[]{"看屏幕时容易忘记眨眼。轻轻、完整地眨几次吧。","让眼睑自然合上再睁开，轻轻眨几次眼睛。","趁这个小间隙，自然地眨眨眼，再看看远处。"};break;
        case "hands":choices=brief?new[]{"松开鼠标，轻轻活动手指。","双手离开键盘片刻。"}:playful?new[]{"鼠标也想歇会儿。松开它，让手指自然张开再放松。","手指今天也在出力。暂停敲字，给它们一点活动时间吧。"}:new[]{"让双手暂时离开键盘和鼠标，轻轻张开手指，再放松。","留意一下手腕有没有一直折着。放松双手，让手腕自然些。","轻轻活动手指和前臂，到舒服的位置就好，不要用力掰拉。"};break;
        case "feet":choices=brief?new[]{"双脚有支撑，换个舒服的位置。","背部靠稳，双脚放松落地。"}:playful?new[]{"椅子下面的小世界也照顾一下：让双脚踏实落地，换换位置。","脚脚想找个落脚点。地面或稳固脚踏都可以，别一直悬着。"}:new[]{"看看双脚是否稳稳放在地面或脚踏上，找个放松的位置。","背部有支撑，双脚也有支撑。坐久了就轻轻换换位置。","检查一下腿和脚有没有一直紧绷，换个舒服的位置吧。"};break;
        default:choices=brief?new[]{"如有需要，去趟洗手间。","照顾一下自己的需要吧。"}:new[]{"如果有需要，就去趟洗手间吧，手上的事可以等一下。","也问问身体有没有别的需要。想去洗手间的话，现在可以歇一会。"};break;
      }
      string message=Pick(key+options.Tone,choices);
      if(rules.Count>1&&rules[1].Key=="posture")message+=" 顺便放松一下肩膀。";
      if(rules.Count>1&&rules[1].Key=="eyes")message+=" 也看看远处。";
      if(rules.Count>1&&rules[1].Key=="blink")message+=" 顺便自然完整地眨几次眼睛。";
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
      if(action=="completed")return "收到，这个小动作完成啦。我会单独更新这一项。";
      return Pick("hello",options.Tone=="简短"?new[]{"我在。需要安静陪伴，还是歇会儿？","照顾好自己，工作慢慢来。"}:new[]{"我在呢。想歇一会儿，还是让我安静陪着？","今天也在认真忙呀。你想歇会儿的时候，来找我就好。","摸摸收到。希望你今天工作顺利，也留一点时间给自己。"});
    }
  }
}
