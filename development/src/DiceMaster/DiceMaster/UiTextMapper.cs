using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;
using DiceMaster.EmbeddedTranslations;

namespace DiceMaster;

public static class UiTextMapper
{
	private const int MaxTranslationCacheSize = 4096;

	private static readonly Dictionary<string, string> exactReplacements;

	private static readonly List<KeyValuePair<string, string>> orderedReplacements;

	private static readonly List<Tuple<Regex, MatchEvaluator>> regexRules;

	private static readonly Dictionary<string, string> translationCache;

	private static readonly object translationCacheLock;

	private static readonly Dictionary<string, string> chineseMonthNumerals;

	private static readonly Dictionary<char, long> chineseCharToLong;

	private static readonly Dictionary<char, int> digitValues;

	static UiTextMapper()
	{
		exactReplacements = new Dictionary<string, string>
		{
			{ "目前時間", "현재 시간" },
			{ "序章旗標", "프롤로그 플래그" },
			{ "隨機值_0_100", "랜덤값:0~100" },
			{ "隨機值_", "랜덤값:" },
			{ "福韞 講法", "복온 강법" },
			{ "假趙活 請客", "가짜 조활이 한턱 쏘다!" },
			{ "掌門米糊糊", "장문인 미음" },
			{ "小師妹出嫁後消滅", "소사매 출가 후 소멸" },
			{ "馬富貴 胸口碎大石", "이부귀 가슴으로 큰 돌 부수기" },
			{ "馬富貴 弄蛇成功", "이부귀 뱀 조종 성공" },
			{ "馬富貴 弄蛇失敗", "이부귀 뱀 조종 실패" },
			{ "馬富貴 弄蛇大成功", "이부귀 뱀 조종 대성공" },
			{ "馬富貴 釣魚", "이부귀 낚시" },
			{ "福韞 化緣", "복온 시주" },
			{ "無敵號與一團廢紙", "무적호와 폐지 한 뭉치" },
			{ "馬富貴 獵人", "이부귀 사냥꾼" },
			{ "馬富貴獵人失敗", "이부귀 사냥꾼 실패" },
			{ "馬富貴 挖礦人", "이부귀 광부" },
			{ "登崖眺望", "절벽에 올라 멀리 바라보기" },
			{ "葉雲舟採藥", "엽운주 약초 캐기" },
			{ "0號劇情中，選擇「既然雲裳如今也算是我妹妹……」", "0번 시나리오에서 「운상이 이제 내 여동생이나 다름없으니……」 선택" },
			{ "藏寶圖中，江湖少俠已經還過錢了", "보물지도에서 강호소협은 이미 돈을 갚았다" },
			{ "記住了羅世巒", "라세만을 기억함" },
			{ "決戰臥雲岡回返同行者", "결전 와운강 귀환 동행자" },
			{ "決戰臥雲崗回返同行者", "결전 와운강 귀환 동행자" },
			{ "决战卧云冈回返同行者", "결전 와운강 귀환 동행자" },
			{ "决战耻雲冈回返同行者", "결전 와운강 귀환 동행자" },
			{ "龍湘的挑戰", "용상의 도전" },
			{ "小梅亂入", "소매 난입" },
			{ "龍湘挑戰", "용상 도전" },
			{ "龍湘服務生", "용상 종업원" },
			{ "超團結", "초단결" },
			{ "挑戰", "도전" },
			{ "挑战", "도전" },
			{ "勝敗", "승패" },
			{ "胜败", "승패" },
			{ "勝利", "승리" },
			{ "胜利", "승리" },
			{ "留學", "유학" },
			{ "留学", "유학" },
			{ "青城留學", "청성 유학" },
			{ "崆峒留學", "공동 유학" },
			{ "唐門留學", "당문 유학" },
			{ "青城派留學", "청성파 유학" },
			{ "崆峒派留學", "공동파 유학" },
			{ "留學青城", "청성 유학" },
			{ "儒學", "유학" },
			{ "儒学", "유학" },
			{ "佛學", "불학" },
			{ "佛学", "불학" },
			{ "道學", "도학" },
			{ "道学", "도학" },
			{ "技能_儒學", "기능_유학" },
			{ "技能_儒学", "기능_유학" },
			{ "技能_佛學", "기능_불학" },
			{ "技能_佛学", "기능_불학" },
			{ "技能_道學", "기능_도학" },
			{ "技能_道学", "기능_도학" },
			{ "秘笈等級_", "비급레벨_" },
			{ "秘笈等级_", "비급레벨_" },
			{ "秘籍等级_", "비급레벨_" },
			{ "開發項目等級_", "개발항목레벨_" },
			{ "開發項目等级_", "개발항목레벨_" },
			{ "开发项目等級_", "개발항목레벨_" },
			{ "开发项目等级_", "개발항목레벨_" },
			{ "開發項目", "개발항목" },
			{ "开发项目", "개발항목" },
			{ "開發項目_", "개발항목_" },
			{ "开发项目_", "개발항목_" },
			{ "武器", "무기" },
			{ "書籍_羅漢拳譜", "서적_나한권보" },
			{ "書籍_金剛腿", "서적_금강퇴" },
			{ "技能_", "기능_" },
			{ "書籍_", "서적_" },
			{ "武功_", "무공_" },
			{ "雜物_", "잡물_" },
			{ "雑物_", "잡물_" },
			{ "貴重品_", "귀중품_" },
			{ "任務_", "임무_" },
			{ "事件_", "이벤트_" },
			{ "劇情_", "스토리_" },
			{ "旗標_", "플래그_" },
			{ "稱號_", "칭호_" },
			{ "天賦_", "재능_" },
			{ "流派_", "유파_" },
			{ "門派_", "문파_" },
			{ "門派好感度_", "문파호감도_" },
			{ "陣營_", "진영_" },
			{ "關係_", "관계_" },
			{ "好感度：", "호감도:" },
			{ "好感度:", "호감도:" },
			{ "好感度_", "호감도_" },
			{ "任意飲食物品", "임의 음식/음료 물품" },
			{ "任何酒", "임의 술" },
			{ "任何音波功", "임의 음파공" },
			{ "好感度_福韞", "호감도_복온" },
			{ "書籍_金鐘罩", "서적_금종조" },
			{ "技能_兵器損毀", "기능_병기손괴" },
			{ "大失敗", "대실패" },
			{ "特殊", "특수" },
			{ "失敗", "실패" },
			{ "傷亡結果", "사상 결과" },
			{ "回程說點什麼", "귀환길에서 할 말" },
			{ "檄文結果", "격문 결과" },
			{ "死傷慘重", "사상자 다수" },
			{ "有人死，有人傷", "사망자와 부상자가 있음" },
			{ "萬幸無人送命", "다행히 사망자는 없음" },
			{ "威脅", "위협" },
			{ "好言相勸", "좋은 말로 설득" },
			{ "唱歌", "노래함" },
			{ "武林公敵", "무림의 공적" },
			{ "朝廷號令", "조정의 호령" },
			{ "其他", "기타" },
			{ "正常", "일반" },
			{ "大成功", "대성공" },
			{ "成功", "성공" },
			{ "下旬", "하순" },
			{ "上旬", "상순" },
			{ "中旬", "중순" },
			{ "變心", "변심" },
			{ "掌門人", "장문인" },
			{ "大師兄", "대사형" },
			{ "二師兄", "이사형" },
			{ "三師兄", "삼사형" },
			{ "四師兄", "사사형" },
			{ "小師妹", "소사매" },
			{ "心上人", "연인" },
			{ "葉雲舟", "엽운주" },
			{ "葉雲裳", "엽운상" },
			{ "瑞杏", "서행" },
			{ "上官螢", "상관형" },
			{ "上官萤", "상관형" },
			{ "上官隼", "상관준" },
			{ "虞小梅", "우소매" },
			{ "小梅", "소매" },
			{ "夏侯蘭", "하후란" },
			{ "郁竹", "욱죽" },
			{ "魏菊", "위국" },
			{ "龍湘", "용상" },
			{ "樊嘯天", "번소천" },
			{ "王二壯", "왕이장" },
			{ "南宮深", "남궁심" },
			{ "南宮淺", "남궁천" },
			{ "瑞笙", "서생" },
			{ "李富貴", "이부귀" },
			{ "解無塵", "해무진" },
			{ "申屠龍", "신도룡" },
			{ "唐大鯨", "당대경" },
			{ "唐大鲸", "당대경" },
			{ "趙活", "조활" },
			{ "赵活", "조활" },
			{ "晁和", "조화" },
			{ "溫夫人", "온부인" },
			{ "温夫人", "온부인" },
			{ "向無憂", "향무우" },
			{ "向无忧", "향무우" },
			{ "鄒博", "추박" },
			{ "邹博", "추박" },
			{ "唐衫", "당삼" },
			{ "福韞", "복온" },
			{ "劉顎", "유악" },
			{ "刘颚", "유악" },
			{ "唐門", "당문" },
			{ "崆峒派", "공동파" },
			{ "青城派", "청성파" },
			{ "點蒼派", "점창파" },
			{ "点苍派", "점창파" },
			{ "峨嵋派", "아미파" },
			{ "峨眉派", "아미파" },
			{ "嵩山派", "숭산파" },
			{ "全真教", "전진교" },
			{ "丐幫", "개방" },
			{ "丐帮", "개방" },
			{ "飛石幫", "비석방" },
			{ "飞石帮", "비석방" },
			{ "錦香宮", "금향궁" },
			{ "锦香宫", "금향궁" },
			{ "上官世家", "상관세가" },
			{ "南宮世家", "남궁세가" },
			{ "滄幫", "창방" },
			{ "沧帮", "창방" },
			{ "玄功門", "현공문" },
			{ "鐵拳門", "철권문" },
			{ "铁拳门", "철권문" },
			{ "飛天門", "비천문" },
			{ "飞天门", "비천문" },
			{ "奪魄門", "탈백문" },
			{ "夺魄门", "탈백문" },
			{ "崆峒進的門派", "공동에서 들어간 문파" },
			{ "崆峒进的门派", "공동에서 들어간 문파" },
			{ "西武林盟", "서무림맹" },
			{ "東武林盟", "동무림맹" },
			{ "东武林盟", "동무림맹" },
			{ "武林盟", "무림맹" },
			{ "極樂教", "극락교" },
			{ "极乐教", "극락교" },
			{ "당중령", "장문인" },
			{ "당포의", "대사형" },
			{ "당쟁", "이사형" },
			{ "당승", "삼사형" },
			{ "당유원", "사사형" },
			{ "당묵령", "소사매" },
			{ "cook", "요리실력" },
			{ "廚藝", "요리실력" },
			{ "단합", "단결력" },
			{ "貢獻度", "공헌도" },
			{ "心相", "심상" },
			{ "M00022", "용상 파직됨" },
			{ "M00014", "쓰레기 무단 투기" },
			{ "M20009", "남궁 손님 분쟁" },
			{ "馬富貴", "이부귀" },
			{ "持有江湖快報", "강호쾌보 보유" },
			{ "中藥蜜餞", "한방정과" },
			{ "乖乖水", "순종수" },
			{ "南蝥礜石散", "남모여석산" },
			{ "姜記肉包", "강씨네 고기만두" },
			{ "峨嵋雪芽", "아미설아" },
			{ "湘妃魚糕", "상비어고" },
			{ "燒刀子", "소도자" },
			{ "甘蔗", "사탕수수" },
			{ "睡丹", "수면단" },
			{ "穿心龍膽湯", "천심용담탕" },
			{ "納溪梅嶺", "납계매령" },
			{ "羊羔酒", "양고주" },
			{ "肉乾", "육포" },
			{ "肉餡饅頭", "고기만두" },
			{ "胡椒粉丸", "호초분환" },
			{ "色紙", "색지" },
			{ "萬靈油", "만령유" },
			{ "玉霄神匕", "옥소신비" },
			{ "切魚刀", "절어도" },
			{ "切鱼刀", "절어도" },
			{ "瀝泉小劍", "역천소검" },
			{ "沥泉小剑", "역천소검" },
			{ "彼岸仙香", "피안선향" },
			{ "蒙頂露芽", "몽정로아" },
			{ "蘇合香酒", "소합향주" },
			{ "豆沙包", "팥앙금만두" },
			{ "降真香", "강진향" },
			{ "鹽腌山豬肉", "염장 멧돼지 고기" },
			{ "糾紛", "분쟁" },
			{ "不滿", "불만" },
			{ "滿", "만족" }
		};
		LoadEmbeddedTranslations(exactReplacements);
		LoadStoryMappingCharacterNames(exactReplacements);
		LoadRelationshipCharacterNames(exactReplacements);
		LoadCharacterIdMappings(exactReplacements);
		LoadBookNameMappings(exactReplacements);
		LoadDevelopNameMappings(exactReplacements);
		orderedReplacements = new List<KeyValuePair<string, string>>(exactReplacements);
		orderedReplacements.Sort((KeyValuePair<string, string> left, KeyValuePair<string, string> right) => right.Key.Length.CompareTo(left.Key.Length));
		translationCache = new Dictionary<string, string>(StringComparer.Ordinal);
		translationCacheLock = new object();
		regexRules = new List<Tuple<Regex, MatchEvaluator>>();
		chineseMonthNumerals = new Dictionary<string, string>
		{
			{ "正", "1" },
			{ "一", "1" },
			{ "二", "2" },
			{ "三", "3" },
			{ "四", "4" },
			{ "五", "5" },
			{ "六", "6" },
			{ "七", "7" },
			{ "八", "8" },
			{ "九", "9" },
			{ "十", "10" },
			{ "十一", "11" },
			{ "十二", "12" }
		};
		chineseCharToLong = new Dictionary<char, long>
		{
			{ '零', 0L },
			{ '一', 1L },
			{ '二', 2L },
			{ '三', 3L },
			{ '四', 4L },
			{ '五', 5L },
			{ '六', 6L },
			{ '七', 7L },
			{ '八', 8L },
			{ '九', 9L },
			{ '十', 10L },
			{ '百', 100L },
			{ '千', 1000L },
			{ '万', 10000L },
			{ '萬', 10000L },
			{ '亿', 100000000L },
			{ '億', 100000000L }
		};
		digitValues = new Dictionary<char, int>
		{
			{ '零', 0 },
			{ '一', 1 },
			{ '二', 2 },
			{ '三', 3 },
			{ '四', 4 },
			{ '五', 5 },
			{ '六', 6 },
			{ '七', 7 },
			{ '八', 8 },
			{ '九', 9 }
		};
		regexRules.Add(Tuple.Create<Regex, MatchEvaluator>(new Regex("([正一二三四五六七八九十]{1,2})月", RegexOptions.Compiled | RegexOptions.CultureInvariant), ConvertChineseMonth));
		regexRules.Add(Tuple.Create<Regex, MatchEvaluator>(new Regex("隨機值_(\\d+)_(\\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant), ConvertRandomRange));
		regexRules.Add(Tuple.Create<Regex, MatchEvaluator>(new Regex("(?:ext\\.)?GetFlag\\(\\s*['\\\"]([^'\\\"]+)['\\\"]\\s*\\)", RegexOptions.Compiled | RegexOptions.CultureInvariant), ConvertGetFlagExpression));
		regexRules.Add(Tuple.Create<Regex, MatchEvaluator>(new Regex("(?:ext\\.)?GetDevelop(?:Level|d)\\(\\s*['\\\"]([^'\\\"]+)['\\\"]\\s*\\)", RegexOptions.Compiled | RegexOptions.CultureInvariant), ConvertGetDevelopExpression));
		regexRules.Add(Tuple.Create<Regex, MatchEvaluator>(new Regex("(?:第)?([零一二三四五六七八九十百千萬億万亿]+)年", RegexOptions.Compiled | RegexOptions.CultureInvariant), ConvertChineseYear));
		regexRules.Add(Tuple.Create<Regex, MatchEvaluator>(new Regex("([零一二三四五六七八九十百千萬億万亿]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant), ConvertGeneralChineseNumeralString));
	}

	private static string ConvertRandomRange(Match match)
	{
		string value = match.Groups[1].Value;
		string value2 = match.Groups[2].Value;
		return "랜덤값:" + value + "~" + value2;
	}

	private static void LoadEmbeddedTranslations(Dictionary<string, string> replacements)
	{
		if (replacements != null)
		{
			LoadPairsFromEmbeddedResource("DiceMaster.Translations._Substitutions.txt", replacements);
			LoadPairsFromEmbeddedResource("DiceMaster.Translations.ModTranslation.txt", replacements);
			LoadPairsFromEmbeddedResource("DiceMaster.Translations.TriggerTranslation1.0.5v.txt", replacements);
		}
	}

	private static void LoadPairsFromEmbeddedResource(string resourceName, Dictionary<string, string> replacements)
	{
		string[] array = Display.ReadEmbeddedLines(resourceName);
		foreach (string text in array)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			string text2 = text.TrimStart('\ufeff').Trim();
			if (string.IsNullOrWhiteSpace(text2) || text2.StartsWith("//") || text2.StartsWith("#"))
			{
				continue;
			}
			int num = text2.IndexOf('=');
			if (num > 0 && num < text2.Length - 1)
			{
				string text3 = text2.Substring(0, num).Trim();
				string text4 = text2.Substring(num + 1).Trim();
				if (text3.StartsWith("Note:"))
				{
					text3 = text3.Substring("Note:".Length).Trim();
				}
				if (text4.StartsWith("참고:"))
				{
					text4 = text4.Substring("참고:".Length).Trim();
				}
				AddReplacementWithEscapedVariants(replacements, text3, text4);
			}
		}
	}

	private static void LoadStoryMappingCharacterNames(Dictionary<string, string> replacements)
	{
		try
		{
			string path = Path.Combine(Paths.GameRootPath, "DataTable", "StoryMappingItem");
			string text = Path.Combine(Paths.GameRootPath, "StringTable.csv");
			if (!Directory.Exists(path) || !File.Exists(text))
			{
				return;
			}
			Dictionary<string, string> dictionary = LoadCharacterNamesFromStringTable(text);
			if (dictionary.Count == 0)
			{
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			for (int i = 0; i < files.Length; i++)
			{
				string json = File.ReadAllText(files[i]);
				string valueByJsonKey = GetValueByJsonKey(json, "Key");
				string valueByJsonKey2 = GetValueByJsonKey(json, "Value");
				if (!string.IsNullOrEmpty(valueByJsonKey) && !string.IsNullOrEmpty(valueByJsonKey2) && ContainsHan(valueByJsonKey) && dictionary.TryGetValue(valueByJsonKey2, out var value) && !string.IsNullOrWhiteSpace(value))
				{
					AddReplacementIfMissing(replacements, valueByJsonKey, value.Trim());
				}
			}
		}
		catch (Exception ex)
		{
			if (OptionInfoPatch.DisplayRefreshDepth != 0)
			{
				throw;
			}
			DicePlugin.LogWarning("UiTextMapper: StoryMappingItem 이름 매핑 로드 실패 - " + ex.Message);
		}
	}

	private static void LoadRelationshipCharacterNames(Dictionary<string, string> replacements)
	{
		try
		{
			string path = Path.Combine(Paths.GameRootPath, "DataTable", "RelationshipStat");
			string text = Path.Combine(Paths.GameRootPath, "StringTable.csv");
			if (!Directory.Exists(path) || !File.Exists(text))
			{
				return;
			}
			Dictionary<string, string> dictionary = LoadCharacterNamesFromStringTable(text);
			if (dictionary.Count == 0)
			{
				return;
			}
			string[] files = Directory.GetFiles(path, "Relationship_*.json");
			for (int i = 0; i < files.Length; i++)
			{
				Match match = Regex.Match(Path.GetFileNameWithoutExtension(files[i]), "^Relationship_([^_]+)_(.+)$");
				if (match.Success)
				{
					string key = NormalizeCharacterIdKey(match.Groups[1].Value);
					string value = match.Groups[2].Value;
					if (dictionary.TryGetValue(key, out var value2) && !string.IsNullOrWhiteSpace(value2))
					{
						AddReplacementIfMissing(replacements, value, value2.Trim());
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (OptionInfoPatch.DisplayRefreshDepth != 0)
			{
				throw;
			}
			DicePlugin.LogWarning("UiTextMapper: RelationshipStat 이름 매핑 로드 실패 - " + ex.Message);
		}
	}

	private static void LoadCharacterIdMappings(Dictionary<string, string> replacements)
	{
		try
		{
			string text = Path.Combine(Paths.GameRootPath, "StringTable.csv");
			if (!File.Exists(text))
			{
				return;
			}
			foreach (KeyValuePair<string, string> item in LoadCharacterNamesFromStringTable(text))
			{
				if (!string.IsNullOrWhiteSpace(item.Value))
				{
					AddReplacementIfMissing(replacements, item.Key, item.Value.Trim());
				}
			}
		}
		catch (Exception ex)
		{
			if (OptionInfoPatch.DisplayRefreshDepth != 0)
			{
				throw;
			}
			DicePlugin.LogWarning("UiTextMapper: Character ID 매핑 로드 실패 - " + ex.Message);
		}
	}

	private static void LoadBookNameMappings(Dictionary<string, string> replacements)
	{
		try
		{
			string text = Path.Combine(Paths.GameRootPath, "StringTable.csv");
			string path = Path.Combine(Paths.GameRootPath, "DataTable", "Book");
			if (!File.Exists(text))
			{
				return;
			}
			Dictionary<string, string> dictionary = LoadBookNamesFromStringTable(text);
			if (dictionary.Count == 0)
			{
				return;
			}
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				if (!string.IsNullOrWhiteSpace(item.Value))
				{
					string text2 = item.Value.Trim();
					AddReplacementIfMissing(replacements, "GetBookLevel('" + item.Key + "')", "비급레벨(" + text2 + ")");
					AddReplacementIfMissing(replacements, "ext.GetBookLevel('" + item.Key + "')", "비급레벨(" + text2 + ")");
					AddReplacementIfMissing(replacements, "GetBookLevel(\"" + item.Key + "\")", "비급레벨(" + text2 + ")");
					AddReplacementIfMissing(replacements, "ext.GetBookLevel(\"" + item.Key + "\")", "비급레벨(" + text2 + ")");
					AddReplacementIfMissing(replacements, "秘笈等級_" + item.Key, "비급레벨_" + text2);
					AddReplacementIfMissing(replacements, "秘笈等级_" + item.Key, "비급레벨_" + text2);
					AddReplacementIfMissing(replacements, "秘籍等级_" + item.Key, "비급레벨_" + text2);
				}
			}
			if (!Directory.Exists(path))
			{
				return;
			}
			string[] files = Directory.GetFiles(path, "Book*.json");
			for (int i = 0; i < files.Length; i++)
			{
				Match match = Regex.Match(Path.GetFileNameWithoutExtension(files[i]), "^Book(\\d+)_(.+)$");
				if (match.Success)
				{
					string value = match.Groups[1].Value;
					string value2 = match.Groups[2].Value;
					if (!string.IsNullOrWhiteSpace(value2) && dictionary.TryGetValue(value, out var value3) && !string.IsNullOrWhiteSpace(value3))
					{
						string text3 = value3.Trim();
						AddReplacementIfMissing(replacements, value2, text3);
						AddReplacementIfMissing(replacements, "Book" + value + "_" + value2, "Book" + value + "_" + text3);
						AddReplacementIfMissing(replacements, "秘笈等級_" + value2, "비급레벨_" + text3);
						AddReplacementIfMissing(replacements, "秘笈等级_" + value2, "비급레벨_" + text3);
						AddReplacementIfMissing(replacements, "秘籍等级_" + value2, "비급레벨_" + text3);
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (OptionInfoPatch.DisplayRefreshDepth != 0)
			{
				throw;
			}
			DicePlugin.LogWarning("UiTextMapper: Book 이름 매핑 로드 실패 - " + ex.Message);
		}
	}

	private static Dictionary<string, string> LoadCharacterNamesFromStringTable(string stringTablePath)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (string item in File.ReadLines(stringTablePath))
		{
			if (!string.IsNullOrEmpty(item) && item.StartsWith("Character/", StringComparison.Ordinal) && TryParseStringTableLine(item, out var key, out var value) && !string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
			{
				string key2 = NormalizeCharacterIdKey(key.Substring("Character/".Length));
				if (!dictionary.ContainsKey(key2))
				{
					dictionary[key2] = value;
				}
			}
		}
		return dictionary;
	}

	private static Dictionary<string, string> LoadBookNamesFromStringTable(string stringTablePath)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (string item in File.ReadLines(stringTablePath))
		{
			if (!string.IsNullOrEmpty(item) && item.StartsWith("Book/Name/", StringComparison.Ordinal) && TryParseStringTableLine(item, out var key, out var value) && !string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
			{
				string text = key.Substring("Book/Name/".Length).Trim();
				if (text.Length != 0 && !dictionary.ContainsKey(text))
				{
					dictionary[text] = value;
				}
			}
		}
		return dictionary;
	}

	private static void LoadDevelopNameMappings(Dictionary<string, string> replacements)
	{
		try
		{
			string path = Path.Combine(Paths.GameRootPath, "DataTable", "UpgradeItemData");
			if (!Directory.Exists(path))
			{
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			foreach (string path2 in files)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path2);
				if (!string.IsNullOrEmpty(fileNameWithoutExtension) && (fileNameWithoutExtension.StartsWith("開發項目_", StringComparison.Ordinal) || fileNameWithoutExtension.StartsWith("开发项目_", StringComparison.Ordinal)))
				{
					string text = GetValueByJsonKey(File.ReadAllText(path2), "_note");
					if (string.IsNullOrWhiteSpace(text))
					{
						text = ExtractDevelopNoteFromKey(fileNameWithoutExtension);
					}
					string text2 = text?.Trim() ?? string.Empty;
					string text3 = text2;
					if (string.IsNullOrWhiteSpace(text2))
					{
						text2 = fileNameWithoutExtension;
					}
					if (exactReplacements.TryGetValue(text2, out var value) && !string.IsNullOrWhiteSpace(value))
					{
						text2 = value.Trim();
					}
					string value2 = "개발항목(" + text2 + ")";
					AddReplacementIfMissing(replacements, fileNameWithoutExtension, value2);
					string key = fileNameWithoutExtension.Replace("開發項目_", "开发项目_");
					AddReplacementIfMissing(replacements, key, value2);
					AddReplacementIfMissing(replacements, "GetDevelopLevel('" + fileNameWithoutExtension + "')", value2);
					AddReplacementIfMissing(replacements, "ext.GetDevelopLevel('" + fileNameWithoutExtension + "')", value2);
					AddReplacementIfMissing(replacements, "GetDevelopd('" + fileNameWithoutExtension + "')", value2);
					AddReplacementIfMissing(replacements, "ext.GetDevelopd('" + fileNameWithoutExtension + "')", value2);
					AddReplacementIfMissing(replacements, "GetDevelopLevel(\"" + fileNameWithoutExtension + "\")", value2);
					AddReplacementIfMissing(replacements, "ext.GetDevelopLevel(\"" + fileNameWithoutExtension + "\")", value2);
					AddReplacementIfMissing(replacements, "GetDevelopd(\"" + fileNameWithoutExtension + "\")", value2);
					AddReplacementIfMissing(replacements, "ext.GetDevelopd(\"" + fileNameWithoutExtension + "\")", value2);
					if (!string.IsNullOrWhiteSpace(text3))
					{
						string value3 = "개발항목레벨_" + text2;
						AddReplacementIfMissing(replacements, "開發項目等級_" + text3, value3);
						AddReplacementIfMissing(replacements, "開發項目等级_" + text3, value3);
						AddReplacementIfMissing(replacements, "开发项目等級_" + text3, value3);
						AddReplacementIfMissing(replacements, "开发项目等级_" + text3, value3);
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (OptionInfoPatch.DisplayRefreshDepth != 0)
			{
				throw;
			}
			DicePlugin.LogWarning("UiTextMapper: 개발 항목 이름 매핑 로드 실패 - " + ex.Message);
		}
	}

	private static string ExtractDevelopNoteFromKey(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return string.Empty;
		}
		int num = key.IndexOf('_');
		if (num < 0 || num + 1 >= key.Length)
		{
			return key;
		}
		int num2 = key.IndexOf('_', num + 1);
		if (num2 < 0 || num2 + 1 >= key.Length)
		{
			return key;
		}
		return key.Substring(num2 + 1);
	}

	private static string NormalizeCharacterIdKey(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return string.Empty;
		}
		id = id.Trim();
		if (id.Length == 0)
		{
			return string.Empty;
		}
		return char.ToLowerInvariant(id[0]) + id.Substring(1);
	}

	private static bool TryParseStringTableLine(string line, out string key, out string value)
	{
		key = string.Empty;
		value = string.Empty;
		if (string.IsNullOrEmpty(line))
		{
			return false;
		}
		int num = line.IndexOf(',');
		if (num <= 0 || num >= line.Length - 1)
		{
			return false;
		}
		key = line.Substring(0, num).TrimStart('\ufeff').Trim();
		string text = line.Substring(num + 1).Trim();
		if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
		{
			value = text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
		}
		else
		{
			value = text;
		}
		return !string.IsNullOrEmpty(key);
	}

	private static string GetValueByJsonKey(string json, string key)
	{
		Match match = Regex.Match(json ?? string.Empty, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
		if (!match.Success)
		{
			return string.Empty;
		}
		return match.Groups[1].Value;
	}

	private static bool ContainsHan(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		foreach (char c in value)
		{
			if (c >= '一' && c <= '鿿')
			{
				return true;
			}
		}
		return false;
	}

	private static void AddReplacementWithEscapedVariants(Dictionary<string, string> replacements, string key, string value)
	{
		if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
		{
			AddReplacementIfMissing(replacements, key, value);
			string key2 = key.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\r", "\n");
			string value2 = value.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\r", "\n");
			AddReplacementIfMissing(replacements, key2, value2);
		}
	}

	private static void AddReplacementIfMissing(Dictionary<string, string> replacements, string key, string value)
	{
		if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value) && !replacements.ContainsKey(key))
		{
			replacements[key] = value;
		}
	}

	private static string ConvertChineseMonth(Match match)
	{
		string value = match.Groups[1].Value;
		if (chineseMonthNumerals.TryGetValue(value, out var value2))
		{
			return value2 + "월";
		}
		return match.Value;
	}

	private static string ConvertChineseYear(Match match)
	{
		if (TryParseChineseAdvanced(match.Groups[1].Value, out var result))
		{
			return result + "년";
		}
		return match.Value;
	}

	private static string ConvertGetFlagExpression(Match match)
	{
		string value = match.Groups[1].Value;
		if (string.IsNullOrWhiteSpace(value))
		{
			return match.Value;
		}
		string flagName = DataHelper.GetFlagName(value);
		if (string.IsNullOrWhiteSpace(flagName) || flagName == value)
		{
			return match.Value;
		}
		string text = flagName.Replace("\r", " ").Replace("\n", " ").Trim();
		string text2 = "(" + value + ")";
		if (!text.EndsWith(text2, StringComparison.Ordinal))
		{
			text += text2;
		}
		return text;
	}

	private static string ConvertGetDevelopExpression(Match match)
	{
		string value = match.Groups[1].Value;
		if (string.IsNullOrWhiteSpace(value))
		{
			return match.Value;
		}
		if (exactReplacements.TryGetValue(value, out var value2) && !string.IsNullOrWhiteSpace(value2))
		{
			return value2;
		}
		string text = ExtractDevelopNoteFromKey(value);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = value;
		}
		if (exactReplacements.TryGetValue(text, out var value3) && !string.IsNullOrWhiteSpace(value3))
		{
			text = value3;
		}
		return "개발항목(" + text + ")";
	}

	private static string ConvertGeneralChineseNumeralString(Match match)
	{
		if (TryParseChineseAdvanced(match.Groups[1].Value, out var result))
		{
			return result.ToString();
		}
		return match.Value;
	}

	public static bool TryParseChineseAdvanced(string chineseNumber, out long result)
	{
		result = 0L;
		if (string.IsNullOrEmpty(chineseNumber))
		{
			return false;
		}
		long num = 0L;
		long num2 = 0L;
		long num3 = -1L;
		for (int i = 0; i < chineseNumber.Length; i++)
		{
			char c = chineseNumber[i];
			if (digitValues.TryGetValue(c, out var value))
			{
				if (value == 0)
				{
					num3 = 0L;
					continue;
				}
				num = value;
				num3 = value;
				continue;
			}
			switch (c)
			{
			case '十':
			{
				long num4;
				switch (num3)
				{
				case 1L:
				case 2L:
				case 3L:
				case 4L:
				case 5L:
				case 6L:
				case 7L:
				case 8L:
				case 9L:
					num4 = num3 * 10;
					break;
				default:
					num4 = 10L;
					break;
				case -1L:
				case 0L:
					num4 = ((num == 0L && num2 == 0L && result == 0L && i == 0) ? 10 : (num + 10));
					break;
				}
				num = num4;
				num2 += num;
				num = 0L;
				num3 = 10L;
				continue;
			}
			case '百':
				num = ((num3 <= 0 || num3 >= 10) ? 100 : (num3 * 100));
				num2 += num;
				num = 0L;
				num3 = 100L;
				continue;
			case '千':
				num = ((num3 <= 0 || num3 >= 10) ? 1000 : (num3 * 1000));
				num2 += num;
				num = 0L;
				num3 = 1000L;
				continue;
			default:
				switch (c)
				{
				case '亿':
				case '億':
					num2 += num;
					result += num2 * 100000000;
					num2 = 0L;
					num = 0L;
					num3 = 100000000L;
					continue;
				default:
					return false;
				case '万':
					break;
				}
				break;
			case '萬':
				break;
			}
			num2 += num;
			result += num2 * 10000;
			num2 = 0L;
			num = 0L;
			num3 = 10000L;
		}
		num2 += num;
		result += num2;
		return true;
	}

	public static string ApplyTranslations(string text)
	{
		text = Display.PreTranslate(text);
		if (string.IsNullOrEmpty(text))
		{
			return Display.PostTranslate(text);
		}
		if (TryGetCachedTranslation(text, out var translated))
		{
			return Display.PostTranslate(translated);
		}
		if (exactReplacements.TryGetValue(text, out var value))
		{
			CacheTranslation(text, value);
			return Display.PostTranslate(value);
		}
		string text2 = text;
		if (exactReplacements != null && exactReplacements.Count > 0)
		{
			foreach (KeyValuePair<string, string> orderedReplacement in orderedReplacements)
			{
				text2 = text2.Replace(orderedReplacement.Key, orderedReplacement.Value);
			}
		}
		if (regexRules != null && regexRules.Count > 0)
		{
			foreach (Tuple<Regex, MatchEvaluator> regexRule in regexRules)
			{
				text2 = regexRule.Item1.Replace(text2, regexRule.Item2);
			}
		}
		CacheTranslation(text, text2);
		return Display.PostTranslate(text2);
	}

	private static bool TryGetCachedTranslation(string source, out string translated)
	{
		if (source != null && source.Contains("GetFlag"))
		{
			translated = null;
			return false;
		}
		lock (translationCacheLock)
		{
			return translationCache.TryGetValue(source, out translated);
		}
	}

	private static void CacheTranslation(string source, string translated)
	{
		if (source != null && source.Contains("GetFlag"))
		{
			return;
		}
		lock (translationCacheLock)
		{
			if (translationCache.Count >= 4096)
			{
				translationCache.Clear();
			}
			translationCache[source] = translated;
		}
	}
}
