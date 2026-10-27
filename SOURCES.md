# 来源与接入说明

## 在线内容

### 一言

- API 文档：https://developer.hitokoto.cn/sentence/
- 官方使用示例：https://developer.hitokoto.cn/sentence/demo.html
- 开源框架：https://github.com/hitokoto-osc/hitokoto-api
- 官方语句包：https://github.com/hitokoto-osc/sentences-bundle
- 关于开源范围的说明：https://developer.hitokoto.cn/introduce.html

客户端调用公开 HTTPS API，不附带密钥。公共接口的可用性取决于提供方；“有开源框架和句库”不等同于当前线上服务的全部代码公开。语录文本和作者、作品信息来自接口，不由小歇编造。内容权利归各自权利人，不随客户端代码重新许可。

### 新闻 RSS / Atom

- IT之家 RSS：https://www.ithome.com/rss/
- BBC中文 RSS：https://feeds.bbci.co.uk/zhongwen/simp/rss.xml

直接解析来源公开提供的标题、链接、日期。新闻内容本身不因可免费获取就成为开源内容。界面保留来源和原文入口，不镜像全文。

### 财经快讯（公开 RSS 中转）

- 金十：`https://rsshub.rssforever.com/jin10`
- 华尔街见闻：`https://rsshub.rssforever.com/wallstreetcn/live/global`
- RSSHub 金十路由实现：https://github.com/DIYgod/RSSHub/blob/master/lib/routes/jin10/index.ts
- 金十官方旧免费引用页的停服通知：https://south.jin10.com/example/websiteiframe.html

金十官方通知：旧免费引用页及关联接口自 2025 年 12 月 1 日停止服务。本程序通过第三方公开 RSS 中转读取快讯，不冒充官方 API，也不声明该中转永久免费或实时可靠。2026 年 10 月 27 日实测两条中转均能取得含发布时间的数据；期间华尔街见闻有一次超时，复测恢复，财经混合模式会在失败时尝试另一来源。

这里只读取公开条目的文本、时间、编号及链接。没有文章链接时使用来源页，并以唯一编号去重。新闻权利归各自来源，开源聚合程序不等于新闻内容本身开源。

### 自定义开源热榜服务

- DailyHotApi：https://github.com/imsyy/DailyHotApi （MIT）
- RSSHub：https://github.com/DIYgod/RSSHub （以项目当前 LICENSE 为准）

客户端支持 DailyHotApi 的 data 数组（title/url）和 RSSHub 生成的 RSS / Atom。可填写自行部署的地址；本程序不附带、不部署这些服务，也不保证他人公共实例永久可用。未复制这些项目的源代码。

## 健康关怀参考

- 美国眼科学会：数字屏幕、眨眼与离屏休息：https://www.aao.org/eye-health/diseases/computer-usage
- 美国眼科学会：20-20-20 用眼休息建议：https://www.aao.org/eye-health/tips-prevention/should-you-be-worried-about-blue-light
- OSHA：电脑工位姿势、支撑、变换姿势、手部伸展和走动：https://www.osha.gov/etools/computer-workstations/positions

小歇将这些一般性建议转写为短提醒和可选小练习，不提供诊断或治疗方案。饮水按个人需要提示，不强制统一饮水量或次数。提醒间隔是用户偏好，不声称具有医疗检测依据。

## 本地内容与图形

小猫图形由 GDI+ 代码绘制；办公室小段子、鼓励语和离线短句为本项目编写，没有冒充名人作品。图形、文案与客户端源码按本项目 LICENSE 提供；在线来源内容除外。
