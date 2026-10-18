using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mortal.Core;

namespace DiceMaster;

internal static class FlagTranslations
{
	public static string TranslateDevNote(string mydevnote, int myvalue)
	{
		string text = null;
		if (LooksLikeFlagId(mydevnote))
		{
			text = mydevnote;
			MissionManagerData instance = MissionManagerData.Instance;
			FlagData flagData = ((instance != null) ? instance.GetFlag(mydevnote) : null);
			if (flagData != null && !string.IsNullOrEmpty(flagData.DevNote))
			{
				mydevnote = flagData.DevNote;
			}
		}
		mydevnote = NormalizeDevNoteKey(mydevnote);
		string text2 = null;
		string text3 = text ?? mydevnote;
		string text4 = "";
		switch (mydevnote)
		{
		case "旗標暫存1":
			text2 = "임시플래그1";
			text3 = "A00001";
			break;
		case "旗標暫存2":
			text2 = "임시플래그2";
			text3 = "A00002";
			break;
		case "旗標暫存3":
			text2 = "임시플래그3";
			text3 = "A00003";
			break;
		case "旗標暫存4":
			text2 = "임시플래그4";
			text3 = "A00004";
			break;
		case "旗標暫存5":
			text2 = "임시플래그5";
			text3 = "A00005";
			break;
		case "旗標暫存6":
			text2 = "임시플래그6";
			text3 = "A00006";
			break;
		case "任務行動次數檢查":
			text2 = "임무 행동 횟수 검사";
			text3 = "ActionCheck01";
			break;
		case "後山挑戰龍湘次數":
			text2 = "뒷산 용상 도전 횟수";
			text3 = "Back_02_501_01";
			break;
		case "觸發登崖眺望":
			text2 = "절벽 오르기 조망 발동";
			text3 = "Back_Work_M_02";
			break;
		case "通用戰役結果":
			text2 = "공통 전역 결과";
			text3 = "BattleResult_001";
			break;
		case "通用決鬥成敗":
			text2 = "공통 결투 성공/실패";
			text3 = "C00000";
			break;
		case "煎藥決鬥大師兄成敗":
			text2 = "약 달이기 대사형 결투 성공/실패";
			text3 = "C00001";
			break;
		case "決鬥龍大俠成敗":
			text2 = "용대협 결투 성공/실패";
			text3 = "C00002";
			break;
		case "決鬥萬里鵬程 - 使用回合次數結果":
			text2 = "만리붕정 결투 - 사용 턴 횟수 결과";
			text3 = "C00003";
			break;
		case "決鬥閻關三煞成敗":
			text2 = "염관삼살 결투 성공/실패";
			text3 = "C00004";
			break;
		case "毛二壯決鬥結果":
			text2 = "왕이장 결투 결과";
			text3 = "C00005";
			break;
		case "小梅練武場決鬥次數":
			text2 = "우소매 연무장 결투 횟수";
			text3 = "C30000";
			break;
		case "練武場團隊次數":
			text2 = "연무장 단체 훈련 횟수";
			text3 = "Center_Free_01";
			break;
		case "劇本1-4-3-5 選擇「沒錯，我就是葉雲舟。」":
			text2 = "시나리오 1-4-3-5 「그래, 내가 바로 엽운주다.」 선택";
			text3 = "D00001";
			break;
		case "試玩版":
			text2 = "체험판";
			text3 = "Demo";
			break;
		case "出門採買發生成功次數":
			text2 = "외출 구매 성공 횟수";
			text3 = "Door_Work_S_01";
			break;
		case "招募弟子遇見飛石幫被毆打一頓":
			text2 = "제자 모집 중 비석방 조우 후 구타당함";
			text3 = "E00001";
			break;
		case "葉雲舟0號線，亂拳打死閻關三煞":
			text2 = "엽운주 0번 루트, 난권으로 염관삼살 격살";
			text3 = "E00002";
			break;
		case "與飛石幫和解":
			text2 = "비석방과 화해";
			text3 = "E00003";
			break;
		case "曾跟劉顎一起襲擊南嵩山":
			text2 = "일찍이 유악과 함께 남숭산 습격";
			text3 = "E00004";
			break;
		case "點蒼劍鬼":
			text2 = "점창검귀";
			text3 = "E00005";
			break;
		case "葉雲裳病情":
			text2 = "엽운상 병세";
			text3 = "E00006";
			break;
		case "飛石幫來訪時選擇招待":
			text2 = "비석방 방문 시 접대 선택";
			text3 = "E00007";
			break;
		case "曾經被嵩山派抓走":
			text2 = "일찍이 숭산파에 잡혀감";
			text3 = "E00008";
			break;
		case "辣眼郎中":
			text2 = "날안낭중";
			text3 = "E00009";
			break;
		case "李大夫對你友善":
			text2 = "이 의원이 당신에게 우호적임";
			text3 = "E00010";
			break;
		case "龍湘欠你一頓飯":
			text2 = "용상이 당신에게 밥 한 끼 빚짐";
			text3 = "E00011";
			break;
		case "打算還龍湘錢":
			text2 = "용상에게 돈을 갚을 생각임";
			text3 = "E00012";
			break;
		case "大師兄不在唐門":
			text2 = "대사형 당문 부재중";
			text3 = "E10000";
			break;
		case "二師兄不在唐門":
			text2 = "이사형 당문 부재중";
			text3 = "E10001";
			break;
		case "三師兄為代掌門":
			text2 = "삼사형 장문 대리";
			text3 = "E10002";
			break;
		case "掌門人不在唐門":
			text2 = "장문인 당문 부재중";
			text3 = "E10003";
			break;
		case "小師妹不在唐門":
			text2 = "소사매 당문 부재중";
			text3 = "E10004";
			break;
		case "四師兄不在唐門":
			text2 = "사사형 당문 부재중";
			text3 = "E10005";
			break;
		case "三師兄不在唐門":
			text2 = "삼사형 당문 부재중";
			text3 = "E10006";
			break;
		case "主角不在唐門":
			text2 = "주인공 당문 부재중";
			text3 = "E11000";
			break;
		case "葉雲裳在唐門":
			text2 = "엽운상 당문 체류 중";
			text3 = "E12000";
			break;
		case "上官螢在唐門":
			text2 = "상관형 당문 체류 중";
			text3 = "E12001";
			break;
		case "小菊在唐門":
			text2 = "위국 당문 체류 중";
			text3 = "E12002";
			break;
		case "小梅在唐門":
			text2 = "우소매 당문 체류 중";
			text3 = "E12003";
			break;
		case "小竹在唐門":
			text2 = "욱죽 당문 체류 중";
			text3 = "E12004";
			break;
		case "龍湘在唐門":
			text2 = "용상 당문 체류 중";
			text3 = "E12005";
			break;
		case "小蘭在唐門":
			text2 = "하후란 당문 체류 중";
			text3 = "E12006";
			break;
		case "葉雲舟在唐門":
			text2 = "엽운주 당문 체류 중";
			text3 = "E12100";
			break;
		case "假趙活在唐門":
			text2 = "가짜 조활 당문 체류 중";
			text3 = "E12101";
			break;
		case "馬富貴在唐門":
			text2 = "이부귀 당문 체류 중";
			text3 = "E12102";
			break;
		case "福韞在唐門":
			text2 = "복온 당문 체류 중";
			text3 = "E12103";
			break;
		case "畫中仙在唐門":
			text2 = "화중선 당문 체류 중";
			text3 = "E12104";
			break;
		case "唐衫在唐門":
			text2 = "당삼 당문 체류 중";
			text3 = "E12105";
			break;
		case "葉雲裳曾經在唐門":
			text2 = "엽운상 과거 당문 체류";
			text3 = "E12200";
			break;
		case "收留小梅":
			text2 = "우소매를 거둠";
			text3 = "E13001";
			break;
		case "唐大鯨加入唐門":
			text2 = "당대경 당문 가입";
			text3 = "E13002";
			break;
		case "唐嬌嬌加入唐門":
			text2 = "당교교 당문 가입";
			text3 = "E13003";
			break;
		case "收留小竹":
			text2 = "욱죽을 거둠";
			text3 = "E13004";
			break;
		case "唐杉加入唐門":
			text2 = "당삼 당문 가입";
			text3 = "E13005";
			break;
		case "唐衫正式加入唐門":
			text2 = "당삼 정식으로 당문 가입";
			text3 = "E13006";
			break;
		case "樊嘯天加入唐門":
			text2 = "번소천 당문 가입";
			text3 = "E13007";
			break;
		case "龍湘加入唐門":
			text2 = "용상 당문 가입";
			text3 = "E13008";
			break;
		case "葉雲舟加入唐門":
			text2 = "엽운주 당문 가입";
			text3 = "E13009";
			break;
		case "福韞加入唐門":
			text2 = "복온 당문 가입";
			text3 = "E13010";
			break;
		case "馬富貴加入唐門":
			text2 = "이부귀 당문 가입";
			text3 = "E13011";
			break;
		case "解無塵加入唐門":
			text2 = "해무진 당문 가입";
			text3 = "E13012";
			break;
		case "丹霞子加入唐門":
			text2 = "단하자 당문 가입";
			text3 = "E13013";
			break;
		case "葉雲裳死亡":
			text2 = "엽운상 사망";
			text3 = "E14000";
			break;
		case "福韞死亡":
			text2 = "복온 사망";
			text3 = "E14001";
			break;
		case "小蘭死亡":
			text2 = "하후란 사망";
			text3 = "E14002";
			break;
		case "釋明死亡":
			text2 = "석명 사망";
			text3 = "E14003";
			break;
		case "是否有捉譚霸刀":
			text2 = "담패도를 잡았는지 여부";
			text3 = "E20001";
			break;
		case "口嚼茶":
			text2 = "입으로 씹은 차";
			text3 = "E20002";
			break;
		case "揍過方震天":
			text2 = "방진천을 팬 적 있음";
			text3 = "E20003";
			break;
		case "支線：乞丐與和尚完成":
			text2 = "서브퀘스트: 거지와 중 완료";
			text3 = "E20004";
			break;
		case "揍福韞":
			text2 = "복온을 팸";
			text3 = "E20005";
			break;
		case "樊嘯天拜師夏侯蘭":
			text2 = "번소천 하후란에게 배사";
			text3 = "E21001";
			break;
		case "支線：萬壽屍心丹完成":
			text2 = "서브퀘스트: 만수시심단 완료";
			text3 = "E21002";
			break;
		case "六道之緣・畜生":
			text2 = "육도의 연・축생";
			text3 = "E21003";
			break;
		case "支線：小竹回家完成":
			text2 = "서브퀘스트: 욱죽의 귀가 완료";
			text3 = "E21004";
			break;
		case "葉雲裳專屬的隱藏屬性，會決定她看待主角的角度":
			text2 = "엽운상 전용 숨겨진 속성, 주인공을 보는 그녀의 관점을 결정함";
			text3 = "G00001";
			break;
		case "小師妹對你的信賴度":
			text2 = "소사매의 당신에 대한 신뢰도";
			text3 = "G00002";
			break;
		case "大師兄對龍湘的好感\n最終決定大師兄會否和龍湘在一起。\r":
			text3 = "G00003";
			switch (myvalue)
			{
			case 0:
				text4 = "주인공이 용상을 공략하지 않으면, 용상이 대사형을 따라다님";
				break;
			case 1:
				text4 = "주인공이 용상을 공략하면, 대사형이 양보함";
				break;
			case 2:
				text4 = "주인공이 용상을 공략하면, 대사형이 당신과 결투함";
				break;
			}
			text2 = "대사형의 용상에 대한 호감도, 최종적으로 대사형이 용상과 이어질지 여부를 결정(" + text4 + ")\r\n";
			break;
		case "向龍湘告白":
			text2 = "용상에게 고백";
			text3 = "G00004";
			break;
		case "已經認識上官螢":
			text2 = "이미 상관형을 알고 있음";
			text3 = "G10001";
			break;
		case "認識葉雲裳":
			text2 = "엽운상을 알고 있음";
			text3 = "G10002";
			break;
		case "認識龍湘":
			text2 = "용상을 알고 있음";
			text3 = "G10003";
			break;
		case "認識小梅":
			text2 = "우소매를 알고 있음";
			text3 = "G10004";
			break;
		case "認識小蘭":
			text2 = "하후란을 알고 있음";
			text3 = "G10005";
			break;
		case "認識小竹":
			text2 = "욱죽을 알고 있음";
			text3 = "G10006";
			break;
		case "認識小菊":
			text2 = "위국을 알고 있음";
			text3 = "G10007";
			break;
		case "認識瑞杏":
			text2 = "서행을 알고 있음";
			text3 = "G10008";
			break;
		case "攻略小師妹":
			text2 = "소사매 공략";
			text3 = "G11000";
			break;
		case "攻略上官螢":
			text2 = "상관형 공략";
			text3 = "G11001";
			break;
		case "攻略葉雲裳":
			text2 = "엽운상 공략";
			text3 = "G11002";
			break;
		case "攻略龍湘":
			text2 = "용상 공략";
			text3 = "G11003";
			break;
		case "攻略小梅":
			text2 = "우소매 공략";
			text3 = "G11004";
			break;
		case "攻略小蘭":
			text2 = "하후란 공략";
			text3 = "G11005";
			break;
		case "攻略小竹":
			text2 = "욱죽 공략";
			text3 = "G11006";
			break;
		case "攻略小菊":
			text2 = "위국 공략";
			text3 = "G11007";
			break;
		case "攻略瑞杏":
			text2 = "서행 공략";
			text3 = "G11008";
			break;
		case "變心次數":
			text2 = "변심 횟수";
			text3 = "G20000";
			break;
		case "認識瑞笙\\n":
			text2 = "서생을 알고 있음\n";
			text3 = "G30000";
			break;
		case "認識劉顎":
			text2 = "유악을 알고 있음";
			text3 = "G30001";
			break;
		case "認識解無塵":
			text2 = "해무진을 알고 있음";
			text3 = "G30002";
			break;
		case "認識申屠龍":
			text2 = "신도룡을 알고 있음";
			text3 = "G30003";
			break;
		case "認識千面人魔":
			text2 = "천면인마를 알고 있음";
			text3 = "G30004";
			break;
		case "認識葉雲舟":
			text2 = "엽운주를 알고 있음";
			text3 = "G30005";
			break;
		case "認識宋悲":
			text2 = "송비를 알고 있음";
			text3 = "G30006";
			break;
		case "認識龍淵":
			text2 = "용연을 알고 있음";
			text3 = "G30007";
			break;
		case "認識丹霞子":
			text2 = "단하자를 알고 있음";
			text3 = "G30008";
			break;
		case "認識樊嘯天":
			text2 = "번소천을 알고 있음";
			text3 = "G30009";
			break;
		case "認識毛二壯":
			text2 = "왕이장을 알고 있음";
			text3 = "G30010";
			break;
		case "認識萬里鵬程":
			text2 = "만리붕정을 알고 있음";
			text3 = "G30011";
			break;
		case "認識福韞":
			text2 = "복온을 알고 있음";
			text3 = "G30012";
			break;
		case "認識馬富貴":
			text2 = "이부귀를 알고 있음";
			text3 = "G30013";
			break;
		case "青城派好感度":
			text2 = "청성파 호감도";
			text3 = "H00001";
			break;
		case "全真教好感度":
			text2 = "전진교 호감도";
			text3 = "H00002";
			break;
		case "峨嵋派好感度":
			text2 = "아미파 호감도";
			text3 = "H00003";
			break;
		case "崆峒派好感度":
			text2 = "공동파 호감도";
			text3 = "H00004";
			break;
		case "嵩山派好感度":
			text2 = "숭산파 호감도";
			text3 = "H00005";
			break;
		case "點蒼派好感度":
			text2 = "점창파 호감도";
			text3 = "H00006";
			break;
		case "南宮世家好感度":
			text2 = "남궁세가 호감도";
			text3 = "H01001";
			break;
		case "錦香宮好感度":
			text2 = "금향궁 호감도";
			text3 = "H01002";
			break;
		case "上官世家好感度":
			text2 = "상관세가 호감도";
			text3 = "H01003";
			break;
		case "丐幫好感度":
			text2 = "개방 호감도";
			text3 = "H02001";
			break;
		case "飛石幫好感度":
			text2 = "비석방 호감도";
			text3 = "H02002";
			break;
		case "泥教好感度":
			text2 = "니교 호감도";
			text3 = "H02003";
			break;
		case "天道":
			text2 = "천도";
			text3 = "H03001";
			break;
		case "餓鬼道":
			text2 = "아귀도";
			text3 = "H03002";
			break;
		case "地獄道":
			text2 = "지옥도";
			text3 = "H03003";
			break;
		case "畜生道":
			text2 = "축생도";
			text3 = "H03004";
			break;
		case "人道":
			text2 = "인간도";
			text3 = "H03005";
			break;
		case "修羅道":
			text2 = "수라도";
			text3 = "H03006";
			break;
		case "千面人魔好感度":
			text2 = "천면인마 호감도";
			text3 = "H04001";
			break;
		case "唐嬌嬌好感":
			text2 = "당교교 호감";
			text3 = "H04002";
			break;
		case "進入葉雲舟0號線":
			text2 = "엽운주 0번 루트 진입";
			text3 = "M00001";
			break;
		case "見石公遠，幫大師兄說話":
			text2 = "석공원을 만나 대사형을 변호함";
			text3 = "M00002";
			break;
		case "提及過樊嘯天":
			text2 = "번소천을 언급한 적 있음";
			text3 = "M00003";
			break;
		case "打劫山賊":
			text2 = "산적 약탈";
			text3 = "M00004";
			break;
		case "亂畫圖賣給耕陽讀書齋":
			text2 = "그림을 아무렇게나 그려 경양독서재에 판매";
			text3 = "M00005";
			break;
		case "二師兄擔任考官次數":
			text2 = "이사형 시험관 담당 횟수";
			text3 = "M00006";
			break;
		case "三師兄擔任考官次數":
			text2 = "삼사형 시험관 담당 횟수";
			text3 = "M00007";
			break;
		case "段考勝利次數":
			text2 = "단련 시험 승리 횟수";
			text3 = "M00008";
			break;
		case "上次段考決鬥敗北":
			text2 = "지난 단련 시험 결투 패배";
			text3 = "M00009";
			break;
		case "上官螢曾被夏侯蘭綁架":
			text2 = "상관형, 하후란에게 납치된 적 있음";
			text3 = "M00010";
			break;
		case "雪山飛鴻爪":
			text2 = "설산비홍조";
			text3 = "M00011";
			break;
		case "揍假趙活":
			text2 = "가짜 조활을 팸";
			text3 = "M00012";
			break;
		case "屍弟":
			text2 = "시제";
			text3 = "M00013";
			break;
		case "亂丟垃圾":
			text2 = "쓰레기 무단 투기";
			text3 = "M00014";
			break;
		case "葉雲舟 木劍":
			text2 = "엽운주 목검";
			text3 = "M00015";
			break;
		case "曾戰勝過龍湘":
			text2 = "일찍이 용상에게 승리한 적 있음";
			text3 = "M00016";
			break;
		case "曾戰勝過解無塵":
			text2 = "일찍이 해무진에게 승리한 적 있음";
			text3 = "M00017";
			break;
		case "曾被解無塵打敗過":
			text2 = "일찍이 해무진에게 패배한 적 있음";
			text3 = "M00018";
			break;
		case "正心堂打坐痊癒":
			text2 = "정심당 좌선으로 완치";
			text3 = "M00019";
			break;
		case "龍湘被石灰粉弄眼":
			text2 = "용상 석회가루에 눈을 다침";
			text3 = "M00020";
			break;
		case "支線：龍湘炒菜有觸發":
			text2 = "서브퀘스트: 용상의 요리 발동됨";
			text3 = "M00021";
			break;
		case "龍湘被革職":
			text2 = "용상 파직됨";
			text3 = "M00022";
			break;
		case "龍湘被大師兄親":
			text2 = "용상, 대사형에게 입맞춤 당함";
			text3 = "M00023";
			break;
		case "觸發假趙活我有帶秘笈的次數":
			text2 = "가짜 조활 '비급 가져왔어' 발동 횟수";
			text3 = "M00024";
			break;
		case "毆打上官螢":
			text2 = "상관형 구타";
			text3 = "M00025";
			break;
		case "晁和被上官螢抓":
			text2 = "조화, 상관형에게 잡힘";
			text3 = "M00026";
			break;
		case "聽三師兄說過嶺南派的事情":
			text2 = "삼사형에게 영남파 이야기를 들은 적 있음";
			text3 = "M00028";
			break;
		case "唐門收留葉式兄妹":
			text2 = "당문, 엽씨 남매를 거둠";
			text3 = "M10001";
			break;
		case "與葉雲舟對立":
			text2 = "엽운주와 대립";
			text3 = "M10002";
			break;
		case "曾被劉顎綁架":
			text2 = "일찍이 유악에게 납치된 적 있음";
			text3 = "M10004";
			break;
		case "護送上官螢":
			text2 = "상관형 호위";
			text3 = "M10005";
			break;
		case "大師兄學會喇逼雕":
			text2 = "대사형 라비조 습득";
			text3 = "M10006";
			break;
		case "後山蒙面人，主角說的話":
			text2 = "뒷산 복면인, 주인공의 말";
			text3 = "M10007";
			break;
		case "段考勝利":
			text2 = "단련 시험 승리";
			text3 = "M10008";
			break;
		case "段考考官\r":
			text3 = "M10009";
			switch (myvalue)
			{
			case 1:
				text4 = "장문인";
				break;
			case 2:
				text4 = "삼사형";
				break;
			case 3:
				text4 = "이사형";
				break;
			case 4:
				text4 = "대사형";
				break;
			}
			text2 = "단련시험관(" + text4 + ")";
			break;
		case "龍吟功秘笈":
			text2 = "용음공 비급";
			text3 = "M10010";
			break;
		case "龍湘沒死":
			text2 = "용상 죽지 않음";
			text3 = "M10011";
			break;
		case "惜花令對大師兄評價\r":
			text3 = "M10012";
			switch (myvalue)
			{
			case 1:
				text4 = "대사형을 변호한다";
				break;
			case 2:
				text4 = "대사형을 변호하지 않는다";
				break;
			}
			text2 = "석화령의 대사형에 대한 평가(" + text4 + ")";
			break;
		case "初見焚香":
			text2 = "처음으로 향을 피움";
			text3 = "M10013";
			break;
		case "趕走了上官螢的僕人":
			text2 = "상관형의 하인을 쫓아냄";
			text3 = "M10014";
			break;
		case "曾在閻關三煞時選擇逃跑":
			text2 = "일찍이 염관삼살 조우 시 도망 선택";
			text3 = "M10015";
			break;
		case "與龍湘姊弟相稱":
			text2 = "용상과 남매처럼 지냄";
			text3 = "M10016";
			break;
		case "觸發雪山行":
			text2 = "설산행 발동";
			text3 = "M10017";
			break;
		case "雪山行的蓋房子次數":
			text2 = "설산행 집 짓기 횟수";
			text3 = "M10018";
			break;
		case "雪山行樹屋等級":
			text2 = "설산행 나무집 등급";
			text3 = "M10019";
			break;
		case "追加遭遇事件：夏侯蘭的敵人(小蘭死亡)":
			text2 = "추가 조우 사건: 하후란의 적 (하후란 사망)";
			text3 = "M10020";
			break;
		case "追加遭遇事件：夏侯蘭的敵人(小蘭存活)":
			text2 = "추가 조우 사건: 하후란의 적 (하후란 생존)";
			text3 = "M10021";
			break;
		case "天王座來訪":
			text2 = "천왕좌 방문";
			text3 = "M10022";
			break;
		case "支持自己當掌門":
			text2 = "자신이 장문인이 되는 것을 지지";
			text3 = "M10023";
			break;
		case "小師妹南宮家宴參戰":
			text3 = "M20001";
			if (myvalue == 1)
			{
				text4 = "참전";
			}
			text2 = "소사매 남궁가 연회 참전(" + text4 + ")";
			break;
		case "已經知曉了杏花仙的存在":
			text2 = "이미 행화선의 존재를 알고 있음";
			text3 = "M20002";
			break;
		case "南宮家宴採納結論":
			text3 = "M20003";
			switch (myvalue)
			{
			case 1:
				text4 = "대사형";
				break;
			case 2:
				text4 = "이사형";
				break;
			case 3:
				text4 = "삼사형";
				break;
			}
			text2 = "남궁가 연회 채택결론(" + text4 + ")";
			break;
		case "萬壽屍心丹":
			text2 = "만수시심단";
			text3 = "M20004";
			break;
		case "陰陽調和治法":
			text2 = "음양조화 치료법";
			text3 = "M20005";
			break;
		case "三師兄字畫，你也有貢獻":
			text2 = "삼사형 글씨와 그림, 당신도 공헌함";
			text3 = "M20006";
			break;
		case "辛儒回歸唐門":
			text2 = "신유 당문 복귀";
			text3 = "M20007";
			break;
		case "留學\\n1: 青城\\n2: 崆峒":
		case "留學\n1: 青城\n2: 崆峒":
		case "留學\r\n1: 青城\r\n2: 崆峒":
		case "留學":
		case "留学":
			text3 = "M20008";
			text4 = myvalue switch
			{
				1 => "청성", 
				2 => "공동", 
				_ => "", 
			};
			text2 = (string.IsNullOrEmpty(text4) ? "유학" : ("유학(" + text4 + ")"));
			break;
		case "南宮來客 糾紛":
			text2 = "남궁 손님 분쟁";
			text3 = "M20009";
			break;
		case "掌門病情":
			text2 = "장문인 병세";
			text3 = "M20010";
			break;
		case "崆峒進的門派\r":
		case "崆峒進的門派":
		case "崆峒進的門派\n1.飛天門\n2.奪魄門\n3.玄功門\n4.鐵拳門":
		case "崆峒进的门派":
		case "崆峒进的门派\n1.飞天门\n2.夺魄门\n3.玄功门\n4.铁拳门":
			text3 = "M20011";
			switch (myvalue)
			{
			case 1:
				text4 = "비천문";
				break;
			case 2:
				text4 = "탈백문";
				break;
			case 3:
				text4 = "현공문";
				break;
			case 4:
				text4 = "철권문";
				break;
			}
			text2 = "공동에서 들어간 문파(" + text4 + ")";
			break;
		case "小梅邀請":
			text2 = "우소매 초대";
			text3 = "M20012";
			break;
		case "搶劫\t":
			text2 = "강도질\t";
			text3 = "M20013";
			break;
		case "死亡擂台解說":
			text2 = "사망 토너먼트 해설";
			text3 = "M20014";
			break;
		case "死亡擂台積分":
			text2 = "사망 토너먼트 점수";
			text3 = "M20015";
			break;
		case "燒烤山味":
			text2 = "산나물 구이";
			text3 = "M20016";
			break;
		case "造訪鐵拳門次數":
			text2 = "철권문 방문 횟수";
			text3 = "M20017";
			break;
		case "小說":
			text2 = "소설";
			text3 = "M20018";
			break;
		case "詩詞":
			text2 = "시사";
			text3 = "M20019";
			break;
		case "古文":
			text2 = "고문";
			text3 = "M20020";
			break;
		case "造訪奪魄森林":
			text2 = "탈백 삼림 방문";
			text3 = "M20021";
			break;
		case "眼睛不安分":
			text2 = "눈이 정숙하지 못함";
			text3 = "M20022";
			break;
		case "前仆後繼":
			text2 = "앞사람이 쓰러지면 뒷사람이 계속함";
			text3 = "M20023";
			break;
		case "第一次遇到搶劫":
			text2 = "처음으로 강도질을 만남";
			text3 = "M20024";
			break;
		case "捉捕譚霸刀時曾見過大師兄用飛燕流星翎粉碎畫舫":
			text2 = "담패도 체포 시 대사형이 비연유성령으로 화방선을 부수는 것을 본 적 있음";
			text3 = "M20025";
			break;
		case "小師妹守護者":
			text2 = "소사매 수호자";
			text3 = "M20026";
			break;
		case "唐門留學 離家出走":
			text2 = "당문 유학 중 가출";
			text3 = "M20027";
			break;
		case "離家出走路線\r":
			text3 = "M20028";
			switch (myvalue)
			{
			case 1:
				text4 = "폐허 사원";
				break;
			case 2:
				text4 = "객잔";
				break;
			}
			text2 = "가출 경로(" + text4 + ")";
			break;
		case "陋習":
			text2 = "나쁜 습관";
			text3 = "M20029";
			break;
		case "和小菊下過棋":
			text2 = "위국과 바둑을 둔 적 있음";
			text3 = "M20030";
			break;
		case "修仙次數":
			text2 = "신선 수련 횟수";
			text3 = "M20031";
			break;
		case "唐門留學練武場次數":
			text2 = "당문 유학 중 연무장 이용 횟수";
			text3 = "M20032";
			break;
		case "飛天門好感":
			text2 = "비천문 호감";
			text3 = "M21000";
			break;
		case "玄功門好感":
			text2 = "현공문 호감";
			text3 = "M21001";
			break;
		case "小竹攜帶虎戰車":
			text2 = "욱죽 호전차 휴대";
			text3 = "M30001";
			break;
		case "第一鏢行":
			text2 = "제일표행";
			text3 = "M30002";
			break;
		case "樊嘯天被關":
			text2 = "번소천 감금됨";
			text3 = "M30003";
			break;
		case "救樊嘯天的狗":
			text2 = "번소천의 개를 구함";
			text3 = "M30004";
			break;
		case "官兵大戰丐幫的結果\r":
			text3 = "M30005";
			switch (myvalue)
			{
			case 1:
				text4 = "관군 우세";
				break;
			case 2:
				text4 = "개방 우세";
				break;
			case 3:
				text4 = "타 문파 인원";
				break;
			}
			text2 = "관군 대 개방 전투 결과(" + text4 + ")";
			break;
		case "唐門丐幫戰役獲得勝利":
			text2 = "당문-개방 전역 승리";
			text3 = "M30006";
			break;
		case "抓獲丐幫數量\r":
			text3 = "M30007";
			switch (myvalue)
			{
			case 1:
				text4 = "2할";
				break;
			case 2:
				text4 = "5할";
				break;
			}
			text2 = "체포한 개방 인원 수(" + text4 + ")";
			break;
		case "被丐幫綁架":
			text2 = "개방에게 납치됨";
			text3 = "M30008";
			break;
		case "南宮淺強化":
			text2 = "남궁천 강화";
			text3 = "M30009";
			break;
		case "南宮淺黑化":
			text2 = "남궁천 흑화";
			text3 = "M30010";
			break;
		case "戰役追加葉雲舟召喚技":
			text2 = "전역에 엽운주 소환 기술 추가";
			text3 = "M30011";
			break;
		case "虎戰車命名":
			text2 = "호전차 명명";
			text3 = "M30012";
			break;
		case "上官螢參戰江陵大戰":
			text2 = "상관형 강릉대전 참전";
			text3 = "M30013";
			break;
		case "玄功傳承":
			text2 = "현공 전승";
			text3 = "M30014";
			break;
		case "瑞笙全屬性強化":
			text2 = "서생 모든 능력치 강화";
			text3 = "M30015";
			break;
		case "解無塵全屬性強化":
			text2 = "해무진 모든 능력치 강화";
			text3 = "M30016";
			break;
		case "聽聞毛二壯的策略":
			text2 = "왕이장의 책략을 들음";
			text3 = "M30017";
			break;
		case "小菊出戰毛二壯":
			text2 = "위국, 왕이장과 대결";
			text3 = "M30018";
			break;
		case "觸發大師兄前往金國刺殺皇帝":
			text2 = "대사형 금나라 황제 암살 시도 발동";
			text3 = "M30019";
			break;
		case "已知崆峒秘聞":
			text2 = "공동 비사를 알고 있음";
			text3 = "M30020";
			break;
		case "小菊的悲劇":
			text2 = "위국의 비극";
			text3 = "M30021";
			break;
		case "隨南宮恆訪問丐幫":
			text2 = "남궁형과 함께 개방 방문";
			text3 = "M30022";
			break;
		case "縱放丐幫":
			text2 = "개방을 방면함";
			text3 = "M30023";
			break;
		case "A書事件得到大師兄幫助":
			text2 = "A서 사건에서 대사형의 도움을 받음";
			text3 = "M30024";
			break;
		case "毛二壯曾經在江陵大戰使用過金剛神槍指":
			text2 = "왕이장이 일찍이 강릉대전에서 금강신창지를 사용한 적 있음";
			text3 = "M30025";
			break;
		case "觸發笙活雙俠分支":
			text2 = "생사쌍협 분기 발동";
			text3 = "M30026";
			break;
		case "身有鐵琵琶功，卻在江陵大戰後拒絕加入崆峒派":
			text2 = "철비파공을 익혔으나, 강릉대전 후 공동파 가입 거절";
			text3 = "M30027";
			break;
		case "飛俠千里行":
			text2 = "비협천리행";
			text3 = "M30028";
			break;
		case "主角參戰江陵大戰":
			text2 = "주인공 강릉대전 참전";
			text3 = "M30029";
			break;
		case "向瑞杏許願":
			text2 = "서행에게 소원 빌기";
			text3 = "M30030";
			break;
		case "毛二壯幫南宮淺打通任督二脈":
			text2 = "왕이장이 남궁천의 임독이맥을 뚫어줌";
			text3 = "M30035";
			break;
		case "上官螢的救命恩人":
			text2 = "상관형의 생명의 은인";
			text3 = "M30036";
			break;
		case "上官螢來到江陵":
			text2 = "상관형 강릉 도착";
			text3 = "M30037";
			break;
		case "葉雲裳閒談：碧潤糕開啟":
			text2 = "엽운상 잡담: 벽윤고 활성화";
			text3 = "M30038";
			break;
		case "江陵大戰觸發瑞笙劇情":
			text2 = "강릉대전 서생 이벤트 발동";
			text3 = "M30039";
			break;
		case "與飛石幫進入交戰":
			text2 = "비석방과 교전 상태 돌입";
			text3 = "M40001";
			break;
		case "與崆峒派進入交戰":
			text2 = "공동파와 교전 상태 돌입";
			text3 = "M40002";
			break;
		case "與點蒼派進入交戰":
			text2 = "점창파와 교전 상태 돌입";
			text3 = "M40003";
			break;
		case "與丐幫進入交戰":
			text2 = "개방과 교전 상태 돌입";
			text3 = "M40004";
			break;
		case "假趙活事件":
			text2 = "가짜 조활 사건";
			text3 = "M40005";
			break;
		case "上官螢的口才":
			text2 = "상관형의 말솜씨";
			text3 = "M40006";
			break;
		case "唐杉心冷":
			text2 = "당삼 실망함";
			text3 = "M40007";
			break;
		case "曾經提案嚐試與飛石幫和解":
			text2 = "일찍이 비석방과 화해 시도 제안";
			text3 = "M40008";
			break;
		case "觸發接掌練丹房事件":
			text2 = "연단방 인수 사건 발동";
			text3 = "M40009";
			break;
		case "大師兄痊癒":
			text2 = "대사형 완치";
			text3 = "M40010";
			break;
		case "知道大師兄有受傷":
			text2 = "대사형이 다친 것을 알고 있음";
			text3 = "M40011";
			break;
		case "知道石公遠以前是山賊":
			text2 = "석공원이 과거 산적이었음을 알고 있음";
			text3 = "M40012";
			break;
		case "朱嬸蔥肉包子":
			text2 = "주씨 아주머니 파고기만두";
			text3 = "M40013";
			break;
		case "決戰臥雲岡回返同行者\r":
		case "決戰臥雲岡回返同行者":
		case "決戰臥雲崗回返同行者\r":
		case "決戰臥雲崗回返同行者":
		case "决战卧云冈回返同行者\r":
		case "决战卧云冈回返同行者":
		case "决战耻雲冈回返同行者\r":
		case "决战耻雲冈回返同行者":
			text3 = "M40014";
			switch (myvalue)
			{
			case 1:
				text4 = "용상";
				break;
			case 2:
				text4 = "복온";
				break;
			}
			text2 = "결전 와운강 귀환 동행자(" + text4 + ")";
			break;
		case "決戰臥雲崗大師兄施展金剛神槍指":
			text2 = "결전 와운강 대사형 금강신창지 시전";
			text3 = "M40015";
			break;
		case "龍湘全屬性強化":
			text2 = "용상 모든 능력치 강화";
			text3 = "M40016";
			break;
		case "觸發店小二事件":
			text2 = "점소이 사건 발동";
			text3 = "M40017";
			break;
		case "小梅與主角們同行回唐門":
			text2 = "우소매 주인공 일행과 당문으로 귀환";
			text3 = "M40018";
			break;
		case "觸發女飛賊追殺事件":
			text2 = "여비적 추격 사건 발동";
			text3 = "M40019";
			break;
		case "臥雲崗回程結果\r":
			text3 = "M40020";
			switch (myvalue)
			{
			case 1:
				text4 = "금오상인에게 머리를 맞음";
				break;
			case 2:
				text4 = "신도룡에게 내력을 흡수당함";
				break;
			}
			text2 = "와운강 귀환 결과(" + text4 + ")";
			break;
		case "戰勝金烏上人":
			text2 = "금오상인에게 승리";
			text3 = "M40021";
			break;
		case "申屠龍吸取內力":
			text2 = "신도룡 내력 흡수";
			text3 = "M40022";
			break;
		case "申屠龍內力大幅縮減":
			text2 = "신도룡 내력 대폭 감소";
			text3 = "M40023";
			break;
		case "申屠龍屬性變為陽":
			text2 = "신도룡 속성 양으로 변경";
			text3 = "M40024";
			break;
		case "小梅內力大幅下降":
			text2 = "우소매 내력 대폭 하락";
			text3 = "M40025";
			break;
		case "見過成吉思汗":
			text2 = "칭기즈 칸을 만난 적 있음";
			text3 = "M40026";
			break;
		case "龍湘中毒值":
			text2 = "용상 중독 수치";
			text3 = "M40027";
			break;
		case "福韞中毒值":
			text2 = "복온 중독 수치";
			text3 = "M40028";
			break;
		case "小梅給解藥\\n":
			text2 = "우소매 해독제 제공\n";
			text3 = "M40029";
			break;
		case "說得唐守鴻":
			text2 = "당수홍 설득 성공";
			text3 = "M40030";
			break;
		case "治癒量":
			text2 = "치유량";
			text3 = "M40031";
			break;
		case "開啟煉丹房治癒判斷":
			text2 = "연단방 치유 판정 활성화";
			text3 = "M40032";
			break;
		case "大師兄活":
			text2 = "대사형 생존";
			text3 = "M40033";
			break;
		case "大師兄倖存":
			text2 = "대사형 생존";
			text3 = "M40034";
			break;
		case "與青城派進入交戰":
			text2 = "청성파와 교전 상태 돌입";
			text3 = "M40035";
			break;
		case "決鬥臥雲崗路線":
		case "決鬥臥雲崗路線\n1 不跟隨大師兄\n2 跟隨大師兄":
			text3 = "M40036";
			switch (myvalue)
			{
			case 1:
				text4 = "대사형을 따르지 않음";
				break;
			case 2:
				text4 = "대사형을 따름";
				break;
			}
			text2 = "결투 와운강 루트(" + text4 + ")";
			break;
		case "發生三門會談且小梅幫忙趕走敵人":
			text2 = "삼문회담 발생 및 우소매가 적 격퇴 지원";
			text3 = "M40037";
			break;
		case "龍湘的免死金牌":
			text2 = "용상의 면사금패";
			text3 = "M40038";
			break;
		case "雲裳病情":
			text2 = "운상 병세";
			text3 = "M40039";
			break;
		case "決戰臥雲崗大師兄勝敗":
			text2 = "결전 와운강 대사형 승패";
			text3 = "M40040";
			break;
		case "幫大師兄療傷":
			text2 = "대사형 치료 지원";
			text3 = "M40041";
			break;
		case "小梅贈刀":
			text2 = "우소매 칼 선물";
			text3 = "M40042";
			break;
		case "天命-是你藏起來不讓喝的猴兒酒":
			text2 = "천명 - 네가 숨겨서 못 마시게 한 후아주";
			text3 = "M40043";
			break;
		case "觸發小師妹約束分支":
			text2 = "소사매 약속 분기 발동";
			text3 = "M40044";
			break;
		case "4-2中辦菜的那個師兄被招回唐門":
			text2 = "4-2 요리 담당 사형 당문으로 소환됨";
			text3 = "M40045";
			break;
		case "三門會談與金烏上人交手過":
			text2 = "삼문회담에서 금오상인과 교전한 적 있음";
			text3 = "M40046";
			break;
		case "內戰時上官螢有幫助唐門":
			text2 = "내전 시 상관형이 당문을 도움";
			text3 = "M40047";
			break;
		case "崆峒地牢救小梅":
			text2 = "공동 지하 감옥에서 우소매 구출";
			text3 = "M40048";
			break;
		case "幫石公遠和石夫人和好":
			text2 = "석공원과 석부인 화해 도움";
			text3 = "M40049";
			break;
		case "請泥教弟子來唐門避難":
			text2 = "니교 제자들을 당문으로 피난 요청";
			text3 = "M40050";
			break;
		case "派遣唐衫陪同大師兄":
			text2 = "당삼을 대사형과 동행하도록 파견";
			text3 = "M40051";
			break;
		case "拿走大師兄的猴兒酒":
			text2 = "대사형의 후아주를 가져감";
			text3 = "M40052";
			break;
		case "捉住唐守鴻":
			text2 = "당수홍을 붙잡음";
			text3 = "M40053";
			break;
		case "主角對唐衫友善":
			text2 = "주인공, 당삼에게 우호적";
			text3 = "M40054";
			break;
		case "唐門會議追加選項：前往崆峒 ":
			text2 = "당문 회의 추가 선택지: 공동으로 이동 ";
			text3 = "M50001";
			break;
		case "5-1勸離前往的場所次數 練武場+後山":
			text2 = "5-1 이탈 권유 장소 방문 횟수 연무장+뒷산";
			text3 = "M50002";
			break;
		case "小師妹出嫁與三師兄決鬥":
			text2 = "소사매 출가 및 삼사형과 결투";
			text3 = "M50003";
			break;
		case "大地圖事件追加江湖少俠追殺":
			text2 = "월드맵 사건 추가: 강호 소협 추격";
			text3 = "M50004";
			break;
		case "南宮深的幹話":
			text2 = "남궁심의 허튼소리";
			text3 = "M50005";
			break;
		case "唐門會議選項：與南宮世家結盟":
			text2 = "당문 회의 선택지: 남궁세가와 동맹";
			text3 = "M50006";
			break;
		case "南宮淺拜師夏侯蘭":
			text2 = "남궁천 하후란에게 배사";
			text3 = "M50007";
			break;
		case "解無塵VS南宮深\r":
			text3 = "M50008";
			switch (myvalue)
			{
			case 1:
				text4 = "남궁심 승";
				break;
			case 2:
				text4 = "해무진 승";
				break;
			}
			text2 = "해무진 VS 남궁심(" + text4 + ")";
			break;
		case "迎戰崆峒 小師妹參戰":
			text2 = "공동파 요격 시 소사매 참전";
			text3 = "M50009";
			break;
		case "迎戰崆峒 瑞笙參戰":
			text2 = "공동파 요격 시 서생 참전";
			text3 = "M50010";
			break;
		case "迎戰崆峒 葉雲舟參戰":
			text2 = "공동파 요격 시 엽운주 참전";
			text3 = "M50011";
			break;
		case "口嚼茶事件中小菊被燙":
			text2 = "입으로 씹은 차 사건 중 위국이 데임";
			text3 = "M50012";
			break;
		case "決鬥崆峒派\r":
			text3 = "M50013";
			switch (myvalue)
			{
			case 1:
				text4 = "집단 구타";
				break;
			case 2:
				text4 = "금오상인과 단독 결투";
				break;
			}
			text2 = "공동파 결투(" + text4 + ")";
			break;
		case "被金烏上人敲頭":
			text2 = "금오상인에게 머리를 맞음";
			text3 = "M50014";
			break;
		case "飛天弟子顏疆死亡":
			text2 = "비천 제자 안강 사망";
			text3 = "M50015";
			break;
		case "瑞笙好感度-20":
			text2 = "서생 호감도 -20";
			text3 = "M50016";
			break;
		case "第一次決鬥金烏上人結果":
			text2 = "첫 번째 금오상인 결투 결과";
			text3 = "M50017";
			break;
		case "金烏上人死亡":
			text2 = "금오상인 사망";
			text3 = "M50018";
			break;
		case "小師妹飛踢":
			text2 = "소사매 날아차기";
			text3 = "M50019";
			break;
		case " 地獄道來訪":
			text2 = " 지옥도 방문";
			text3 = "M50020";
			break;
		case "開啟唐門會議提案 雪山行":
			text2 = "당문 회의 제안 '설산행' 활성화";
			text3 = "M50021";
			break;
		case "夏侯蘭樹屋規格\r":
			text3 = "M50022";
			switch (myvalue)
			{
			case 1:
				text4 = "보통";
				break;
			case 2:
				text4 = "고급";
				break;
			}
			text2 = "하후란 나무집 규격(" + text4 + ")";
			break;
		case "委託四師兄的事情\r":
			text3 = "M60001";
			switch (myvalue)
			{
			case 1:
				text4 = "소사매가 잘 지내는지 알고 싶다";
				break;
			case 2:
				text4 = "화산에 신선이 있고, 신선에게 영단이 있다고 들었다";
				break;
			}
			text2 = "사사형에게 부탁한 일(" + text4 + ")";
			break;
		case "與南宮世家結盟":
			text2 = "남궁세가와 동맹";
			text3 = "M60002";
			break;
		case "小師妹同行武林大會":
			text2 = "소사매 무림대회 동행";
			text3 = "M60003";
			break;
		case "小梅同行武林大會":
			text2 = "우소매 무림대회 동행";
			text3 = "M60004";
			break;
		case "小竹同行武林大會":
			text2 = "욱죽 무림대회 동행";
			text3 = "M60005";
			break;
		case "葉雲舟同行武林大會":
			text2 = "엽운주 무림대회 동행";
			text3 = "M60006";
			break;
		case "葉雲裳同行武林大會":
			text2 = "엽운상 무림대회 동행";
			text3 = "M60007";
			break;
		case "葉雲舟煎藥俠曾觸發":
			text2 = "엽운주 약 달이는 협객 이벤트 발동된 적 있음";
			text3 = "M60008";
			break;
		case "大地圖追加遭遇事件：新通緝犯":
			text2 = "월드맵 추가 조우 사건: 새로운 현상수배범";
			text3 = "M60009";
			break;
		case "宋悲同行武林大會":
			text2 = "송비 무림대회 동행";
			text3 = "M60010";
			break;
		case "福韞同行武林大會":
			text2 = "복온 무림대회 동행";
			text3 = "M60011";
			break;
		case "嵩山派同行武林大會":
			text2 = "숭산파 무림대회 동행";
			text3 = "M60012";
			break;
		case "龍湘同行武林大會":
			text2 = "용상 무림대회 동행";
			text3 = "M60013";
			break;
		case "樊嘯天同行武林大會":
			text2 = "번소천 무림대회 동행";
			text3 = "M60014";
			break;
		case "成為崆峒掌派人":
			text2 = "공동 장문인 됨";
			text3 = "M60015";
			break;
		case "前往武林大會路線\r":
		case "前往武林大會路線":
		case "前往武林大會路線\n1. 委任師弟帶隊，獨行江陵\n2. 前往錦香宮作客":
			text3 = "M60016";
			switch (myvalue)
			{
			case 1:
				text4 = "사제에게 인솔을 맡기고, 홀로 강릉으로 감";
				break;
			case 2:
				text4 = "금향궁에 손님으로 방문";
				break;
			}
			text2 = "무림대회 이동 경로(" + text4 + ")";
			break;
		case "龍湘的反應":
			text2 = "용상의 반응";
			text3 = "M60017";
			break;
		case "開啟正心堂養成：葉雲裳彈琴":
			text2 = "정심당 육성 활성화: 엽운상 거문고 연주";
			text3 = "M60018";
			break;
		case "開啟中間階段選項：挑戰畫中仙":
			text2 = "중간 단계 선택지 활성화: 화중선 도전";
			text3 = "M60019";
			break;
		case "解除金烏上人或申屠龍的DEBUFF":
			text2 = "금오상인 또는 신도룡의 디버프 해제";
			text3 = "M60020";
			break;
		case "與畫中仙決鬥":
			text2 = "화중선과 결투";
			text3 = "M60021";
			break;
		case "觸發大漠行":
			text2 = "대사막행 발동";
			text3 = "M60022";
			break;
		case "小梅全屬性弱化":
			text2 = "우소매 모든 능력치 약화";
			text3 = "M60023";
			break;
		case "小梅練玄燭心經":
			text2 = "우소매 현촉심경 수련";
			text3 = "M60024";
			break;
		case "小梅屬性提升，學會一趾神通":
			text2 = "우소매 능력치 상승, 일지신통 습득";
			text3 = "M60025";
			break;
		case "上官螢遠嫁日本":
			text2 = "상관형 일본으로 원정 시집";
			text3 = "M60026";
			break;
		case "六戒刀":
			text2 = "육계도";
			text3 = "M60027";
			break;
		case "知道溫夫人是人間道法王":
			text2 = "온부인이 인간도 법왕임을 알고 있음";
			text3 = "M60028";
			break;
		case "挑戰上官隼":
			text2 = "상관준 도전";
			text3 = "M60029";
			break;
		case "葉雲舟代表唐門出戰":
			text2 = "엽운주 당문 대표로 출전";
			text3 = "M60030";
			break;
		case "上官螢的可能性":
			text2 = "상관형의 가능성";
			text3 = "M60031";
			break;
		case "主角打瑞笙":
			text2 = "주인공 서생과 대결";
			text3 = "M60032";
			break;
		case "挑戰上官隼勝敗\r":
		case "挑戰上官隼勝敗":
		case "挑戰上官隼勝敗\n1. 勝利\n2. 失敗":
			text3 = "M60033";
			switch (myvalue)
			{
			case 1:
				text4 = "승리";
				break;
			case 2:
				text4 = "실패";
				break;
			}
			text2 = "상관준 도전 승패(" + text4 + ")";
			break;
		case "前往終南山":
			text2 = "종남산으로 이동";
			text3 = "M60034";
			break;
		case "龍之憂鬱中觸發龍湘的心事":
			text2 = "용의 우울 중 용상의 속마음 발동";
			text3 = "M60035";
			break;
		case "唐門收容龍湘":
			text2 = "당문 용상 수용";
			text3 = "M60036";
			break;
		case "帶小梅拜見錦香宮":
			text2 = "우소매를 데리고 금향궁 배알";
			text3 = "M60037";
			break;
		case "武林大會選擇支持對象\r":
		case "武林大會選擇支持對象":
		case "武林大會選擇支持對象\n1. 正道\n2. 錦香宮":
		case "武林大会选择支持对象":
		case "武林大会选择支持对象\n1. 正道\n2. 锦香宫":
			text3 = "M60038";
			switch (myvalue)
			{
			case 1:
				text4 = "정도";
				break;
			case 2:
				text4 = "금향궁";
				break;
			}
			text2 = "무림대회 지지 대상 선택(" + text4 + ")";
			break;
		case "武林大會決鬥南宮深":
			text2 = "무림대회 남궁심 결투";
			text3 = "M60039";
			break;
		case "武林大會拜訪錦香宮":
			text2 = "무림대회 금향궁 방문";
			text3 = "M60040";
			break;
		case "錦香宮行被趕出來":
			text2 = "금향궁 방문 중 쫓겨남";
			text3 = "M60041";
			break;
		case "龍湘 天罡伏魔劍":
			text2 = "용상 천강복마검";
			text3 = "M60042";
			break;
		case "龍湘 龍淵七絕":
			text2 = "용상 용연칠절";
			text3 = "M60043";
			break;
		case "決鬥袁無憲":
			text2 = "원무헌 결투";
			text3 = "M60044";
			break;
		case "戰勝袁無憲與荊紅":
			text2 = "원무헌과 형홍에게 승리";
			text3 = "M60045";
			break;
		case "致哀中與南宮深談話":
			text2 = "애도 중 남궁심과 대화";
			text3 = "M60046";
			break;
		case "投票":
			text2 = "투표";
			text3 = "M60047";
			break;
		case "全真支持":
			text2 = "전진교 지지";
			text3 = "M60048";
			break;
		case "嵩山支持":
			text2 = "숭산파 지지";
			text3 = "M60049";
			break;
		case "青城支持":
			text2 = "청성파 지지";
			text3 = "M60050";
			break;
		case "點蒼支持":
			text2 = "점창파 지지";
			text3 = "M60051";
			break;
		case "峨嵋支持":
			text2 = "아미파 지지";
			text3 = "M60052";
			break;
		case "崆峒支持":
			text2 = "공동파 지지";
			text3 = "M60053";
			break;
		case "南宮支持":
			text2 = "남궁세가 지지";
			text3 = "M60054";
			break;
		case "丐幫支持":
			text2 = "개방 지지";
			text3 = "M60055";
			break;
		case "飛石幫支持":
			text2 = "비석방 지지";
			text3 = "M60056";
			break;
		case "晁和死亡":
			text2 = "조화 사망";
			text3 = "M60057";
			break;
		case "地獄道行":
			text2 = "지옥도행";
			text3 = "M60058";
			break;
		case "投降正派":
			text2 = "정도에 항복";
			text3 = "M60059";
			break;
		case "傷亡結果\r":
			text3 = "M60060";
			switch (myvalue)
			{
			case 1:
				text4 = "사상자 다수 발생";
				break;
			case 2:
				text4 = "사망자 및 부상자 발생";
				break;
			case 3:
				text4 = "다행히 사망자 없음";
				break;
			}
			text2 = "사상자 결과(" + text4 + ")";
			break;
		case "與上官螢有約":
			text2 = "상관형과 약속 있음";
			text3 = "M60061";
			break;
		case "選擇投降":
			text2 = "항복 선택";
			text3 = "M60062";
			break;
		case "下樓喝酒":
			text2 = "아래층으로 내려가 술 마시기";
			text3 = "M60063";
			break;
		case "小菊喝醉":
			text2 = "위국 취함";
			text3 = "M60064";
			break;
		case "有任何人幫忙備馬":
			text2 = "누군가 말 준비를 도움";
			text3 = "M60065";
			break;
		case "回程說點什麼\r":
			text3 = "M60066";
			switch (myvalue)
			{
			case 1:
				text4 = "위협";
				break;
			case 2:
				text4 = "좋은 말로 설득";
				break;
			case 3:
				text4 = "노래 부르기";
				break;
			}
			text2 = "귀환길에 할 말(" + text4 + ")";
			break;
		case "向四師兄要求土特產":
			text2 = "사사형에게 토산품 요구";
			text3 = "M60067";
			break;
		case "向鄒博指控申屠龍":
			text2 = "추박에게 신도룡 고발";
			text3 = "M60068";
			break;
		case "武林大會選擇接納唐衫":
			text2 = "무림대회에서 당삼 수용 선택";
			text3 = "M60069";
			break;
		case "小師妹有同行風雨山並跟瑞笙見過面":
			text2 = "소사매 풍우산 동행 및 서생과 만난 적 있음";
			text3 = "M60070";
			break;
		case "武林大會對飛石幫友善":
			text2 = "무림대회에서 비석방에게 우호적";
			text3 = "M60071";
			break;
		case "主角在武林大會支持錦香宮與上官隼對抗":
			text2 = "주인공 무림대회에서 금향궁 지지 및 상관준과 대항";
			text3 = "M60072";
			break;
		case "觸發四師兄歸來":
			text2 = "사사형 귀환 발동";
			text3 = "M70001";
			break;
		case "檄文結果\r":
			text3 = "M70002";
			switch (myvalue)
			{
			case 1:
				text4 = "무림공적";
				break;
			case 2:
				text4 = "조정 명령";
				break;
			}
			text2 = "격문 결과(" + text4 + ")";
			break;
		case "小梅村曝光":
			text2 = "우소매촌 노출";
			text3 = "M70003";
			break;
		case "可成立西武林盟":
			text2 = "서무림맹 창설 가능";
			text3 = "M70004";
			break;
		case "與泥教結盟":
			text2 = "니교와 동맹";
			text3 = "M70005";
			break;
		case "和平之路":
			text2 = "평화의 길";
			text3 = "M70006";
			break;
		case "唐門會議選項開啟：暗殺盟主":
			text2 = "당문 회의 선택지 활성화: 맹주 암살";
			text3 = "M70007";
			break;
		case "訪問小菊":
			text2 = "위국 방문";
			text3 = "M70008";
			break;
		case "遺策":
			text2 = "남겨진 계책";
			text3 = "M70009";
			break;
		case "圍攻泥教":
			text2 = "니교 포위 공격";
			text3 = "M70010";
			break;
		case "票數：退隱江湖":
			text2 = "투표수: 강호 은퇴";
			text3 = "M70011";
			break;
		case "票數：戰他娘親":
			text2 = "투표수: 빌어먹을 것들과 싸운다";
			text3 = "M70012";
			break;
		case "票數：響應武林盟":
			text2 = "투표수: 무림맹에 호응";
			text3 = "M70013";
			break;
		case "三師兄的決策\r":
			text3 = "M70014";
			switch (myvalue)
			{
			case 1:
				text4 = "강호 은퇴";
				break;
			case 2:
				text4 = "빌어먹을 것들과 싸운다";
				break;
			case 3:
				text4 = "무림맹에 호응";
				break;
			}
			text2 = "삼사형의 결정(" + text4 + ")";
			break;
		case "你的決策\r":
			text3 = "M70015";
			switch (myvalue)
			{
			case 1:
				text4 = "강호 은퇴";
				break;
			case 2:
				text4 = "빌어먹을 것들과 싸운다";
				break;
			case 3:
				text4 = "무림맹에 호응";
				break;
			case 4:
				text4 = "서무림맹";
				break;
			}
			text2 = "당신의 결정(" + text4 + ")";
			break;
		case "小師妹的票":
			text2 = "소사매의 표";
			text3 = "M70016";
			break;
		case "四師兄的決策\r":
			text3 = "M70017";
			switch (myvalue)
			{
			case 1:
				text4 = "강호 은퇴";
				break;
			case 2:
				text4 = "빌어먹을 것들과 싸운다";
				break;
			case 3:
				text4 = "무림맹에 호응";
				break;
			}
			text2 = "사사형의 결정(" + text4 + ")";
			break;
		case "票數相同決鬥勝敗\r":
			text3 = "M70018";
			switch (myvalue)
			{
			case 1:
				text4 = "패";
				break;
			case 2:
				text4 = "승";
				break;
			}
			text2 = "투표수 동률 시 결투 승패(" + text4 + ")";
			break;
		case "退隱江湖":
			text2 = "강호 은퇴";
			text3 = "M70019";
			break;
		case "小梅提出兇殘的意見":
			text2 = "우소매 흉악한 의견 제시";
			text3 = "M70020";
			break;
		case "曾經在前往武林大會的旅途中答應老僧人去嵩山":
			text2 = "일찍이 무림대회로 가는 길에 노승에게 숭산에 가기로 약속함";
			text3 = "M70021";
			break;
		case "四師兄出資上官螢":
			text2 = "사사형 상관형에게 자금 지원";
			text3 = "M70022";
			break;
		case "龍湘技能 勇氣":
			text2 = "용상 기술 용기";
			text3 = "M70023";
			break;
		case "已派人至青城派":
			text2 = "청성파에 사람 파견 완료";
			text3 = "M70024";
			break;
		case "已派人至崆峒派":
			text2 = "공동파에 사람 파견 완료";
			text3 = "M70025";
			break;
		case "已派人至峨嵋派":
			text2 = "아미파에 사람 파견 완료";
			text3 = "M70026";
			break;
		case "已派人至丐幫":
			text2 = "개방에 사람 파견 완료";
			text3 = "M70027";
			break;
		case "指派四師兄狀態\r":
			text3 = "M70028";
			switch (myvalue)
			{
			case 1:
				text4 = "청성파";
				break;
			case 2:
				text4 = "공동파";
				break;
			case 3:
				text4 = "개방";
				break;
			case 4:
				text4 = "중소 문파";
				break;
			}
			text2 = "사사형 파견 상태(" + text4 + ")";
			break;
		case "福韞出使峨嵋派":
			text2 = "복온 아미파에 사절로 파견";
			text3 = "M70029";
			break;
		case "小梅出使崆峒派":
			text2 = "우소매 공동파에 사절로 파견";
			text3 = "M70030";
			break;
		case "小竹出使崆峒派":
			text2 = "욱죽 공동파에 사절로 파견";
			text3 = "M70031";
			break;
		case "小菊出使崆峒派":
			text2 = "위국 공동파에 사절로 파견";
			text3 = "M70032";
			break;
		case "上官螢寫信至崆峒派":
			text2 = "상관형 공동파에 편지 발송";
			text3 = "M70033";
			break;
		case "唐門弟子出使崆峒派":
			text2 = "당문 제자 공동파에 사절로 파견";
			text3 = "M70034";
			break;
		case "主角提議初始門派\r":
			text3 = "M70035";
			switch (myvalue)
			{
			case 1:
				text4 = "청성파";
				break;
			case 2:
				text4 = "아미파";
				break;
			case 3:
				text4 = "개방";
				break;
			}
			text2 = "주인공 초기 문파 제안(" + text4 + ")";
			break;
		case "唐門弟子出使青城派":
			text2 = "당문 제자 청성파에 사절로 파견";
			text3 = "M70036";
			break;
		case "唐衫出使峨嵋派":
			text2 = "당삼 아미파에 사절로 파견";
			text3 = "M70037";
			break;
		case "三師兄出使峨嵋派":
			text2 = "삼사형 아미파에 사절로 파견";
			text3 = "M70038";
			break;
		case "暫時獲得掌門令牌":
			text2 = "임시로 장문 영패 획득";
			text3 = "M70039";
			break;
		case "成功幫助上官螢收買滄幫":
			text2 = "상관형의 창방 매수 성공적으로 도움";
			text3 = "M70040";
			break;
		case "練武場特訓：葉雲舟加入敵對陣營":
			text2 = "연무장 특별 훈련: 엽운주 적대 진영 합류";
			text3 = "M70041";
			break;
		case "練武場特訓：龍湘加入敵對陣營":
			text2 = "연무장 특별 훈련: 용상 적대 진영 합류";
			text3 = "M70042";
			break;
		case "練武場特訓：四師兄加入敵方陣營":
			text2 = "연무장 특별 훈련: 사사형 적대 진영 합류";
			text3 = "M70043";
			break;
		case "支線：極端揍狗拳":
			text2 = "서브퀘스트: 극단적 주구권";
			text3 = "M70044";
			break;
		case "說服丐幫結果\r":
			text3 = "M70045";
			switch (myvalue)
			{
			case 1:
				text4 = "이부귀 협조";
				break;
			case 2:
				text4 = "실패";
				break;
			case 3:
				text4 = "개방 당문 가입";
				break;
			}
			text2 = "개방 설득 결과(" + text4 + ")";
			break;
		case "說服峨嵋派結果\r":
			text3 = "M70046";
			switch (myvalue)
			{
			case 1:
				text4 = "성공";
				break;
			case 2:
				text4 = "실패";
				break;
			case 3:
				text4 = "양측 모두 돕지 않음";
				break;
			}
			text2 = "아미파 설득 결과(" + text4 + ")";
			break;
		case "唐衫的支援\r":
			text2 = "당삼의 지원";
			text3 = "M70047";
			break;
		case "說服青城派結果":
			text3 = "M70048";
			switch (myvalue)
			{
			case 1:
				text4 = "성공";
				break;
			case 2:
				text4 = "실패";
				break;
			}
			text2 = "청성파 설득 결과(" + text4 + ")";
			break;
		case "說服中小門派結果\r":
			text3 = "M70049";
			switch (myvalue)
			{
			case 1:
				text4 = "성공";
				break;
			case 2:
				text4 = "실패";
				break;
			case 3:
				text4 = "관망";
				break;
			}
			text2 = "중소 문파 설득 결과(" + text4 + ")";
			break;
		case "說服崆峒派結果\r":
			text3 = "M70050";
			switch (myvalue)
			{
			case 1:
				text4 = "성공";
				break;
			case 2:
				text4 = "실패";
				break;
			}
			text2 = "공동파 설득 결과(" + text4 + ")";
			break;
		case "設陷阱":
			text2 = "함정 설치";
			text3 = "M70051";
			break;
		case "頑抗路線":
			text2 = "완강 저항 루트";
			text3 = "M70052";
			break;
		case "瑞笙弱化":
			text2 = "서생 약화";
			text3 = "M70053";
			break;
		case "發生修羅場事件":
			text2 = "수라장 사건 발생";
			text3 = "M70054";
			break;
		case "應對之策提出西武林盟時(學問>60)&&(道德>中庸)":
			text2 = "대응책으로 서무림맹 제안 시 (학문>60)&&(도덕>중용)";
			text3 = "M70055";
			break;
		case "飛石幫支持西武林盟":
			text2 = "비석방 서무림맹 지지";
			text3 = "M70056";
			break;
		case "8-1 你的對策\r":
			text3 = "M80001";
			switch (myvalue)
			{
			case 1:
				text4 = "복온";
				break;
			case 2:
				text4 = "반론";
				break;
			case 3:
				text4 = "할 말 없음";
				break;
			}
			text2 = "8-1 당신의 대응책(" + text4 + ")";
			break;
		case "對策":
			text2 = "대응책";
			text3 = "M80002";
			break;
		case "小師妹的保護":
			text2 = "소사매의 보호";
			text3 = "M80003";
			break;
		case "上官衰弱":
			text2 = "상관세가 쇠약";
			text3 = "M80004";
			break;
		case "小梅改邪歸正":
			text2 = "우소매 개과천선";
			text3 = "M80005";
			break;
		case "護衛三師兄第一個刺客\r":
			text3 = "M80006";
			switch (myvalue)
			{
			case 1:
				text4 = "담패도";
				break;
			case 2:
				text4 = "방진천";
				break;
			case 3:
				text4 = "당삼";
				break;
			}
			text2 = "삼사형 호위 첫 번째 자객(" + text4 + ")";
			break;
		case "護衛三師兄第二個刺客\r":
			text3 = "M80007";
			switch (myvalue)
			{
			case 1:
				text4 = "금오상인";
				break;
			case 2:
				text4 = "화룡선군";
				break;
			case 3:
				text4 = "석명";
				break;
			case 4:
				text4 = "점소이";
				break;
			}
			text2 = "삼사형 호위 두 번째 자객(" + text4 + ")";
			break;
		case "護衛三師兄 小師妹同行":
			text2 = "삼사형 호위 시 소사매 동행";
			text3 = "M80008";
			break;
		case "錦香宮支援唐門":
			text2 = "금향궁 당문 지원";
			text3 = "M80009";
			break;
		case "唐門出擊的結果1\r":
			text3 = "M80010";
			switch (myvalue)
			{
			case 1:
				text4 = "금향궁 서무림맹 가입";
				break;
			case 2:
				text4 = "맞받아 소리침";
				break;
			case 3:
				text4 = "소리침 당함";
				break;
			case 4:
				text4 = "문 열고 요격";
				break;
			}
			text2 = "당문 출격 결과1(" + text4 + ")";
			break;
		case "東西武林盟在唐門的決策\r":
			text3 = "M80011";
			switch (myvalue)
			{
			case 1:
				text4 = "삼사형 호위";
				break;
			case 2:
				text4 = "당문 출격";
				break;
			}
			text2 = "동서무림맹 당문 내 결정(" + text4 + ")";
			break;
		case "西武林盟戰役人數":
			text2 = "서무림맹 전역 인원수";
			text3 = "M80012";
			break;
		case "東武林盟戰役人數":
			text2 = "동무림맹 전역 인원수";
			text3 = "M80013";
			break;
		case "錦香宮失望":
			text2 = "금향궁 실망";
			text3 = "M80014";
			break;
		case "發生圍攻唐門":
			text2 = "당문 포위 공격 발생";
			text3 = "M80015";
			break;
		case "圍攻唐門弟子出走結果\r":
			text3 = "M80016";
			switch (myvalue)
			{
			case 1:
				text4 = "아무도 떠나지 않음";
				break;
			case 2:
				text4 = "일부 떠남";
				break;
			case 3:
				text4 = "대부분 떠남";
				break;
			}
			text2 = "당문 포위 공격 시 제자 이탈 결과(" + text4 + ")";
			break;
		case "武林盟的消耗\r":
			text3 = "M80017";
			switch (myvalue)
			{
			case 1:
				text4 = "경미한 손실";
				break;
			case 2:
				text4 = "심각한 손실";
				break;
			case 3:
				text4 = "괴멸적 타격";
				break;
			}
			text2 = "무림맹의 소모(" + text4 + ")";
			break;
		case "小蘭VS向無憂":
			text2 = "하후란 VS 상무우";
			text3 = "M80018";
			break;
		case "龍湘VS向無憂":
			text2 = "용상 VS 상무우";
			text3 = "M80019";
			break;
		case "葉雲裳勸退向無憂":
			text2 = "엽운상, 상무우 설득하여 물러나게 함";
			text3 = "M80020";
			break;
		case "向無憂戰三師兄":
			text2 = "상무우, 삼사형과 대결";
			text3 = "M80021";
			break;
		case "上官隼敗":
			text2 = "상관준 패배";
			text3 = "M80022";
			break;
		case "已知山上情況":
			text2 = "산 위 상황을 알고 있음";
			text3 = "M80023";
			break;
		case "小蘭VS上官隼":
			text2 = "하후란 VS 상관준";
			text3 = "M80024";
			break;
		case "龍湘VS上官隼":
			text2 = "용상 VS 상관준";
			text3 = "M80025";
			break;
		case "小師妹VS上官隼":
			text2 = "소사매 VS 상관준";
			text3 = "M80026";
			break;
		case "南宮深敗":
			text2 = "남궁심 패배";
			text3 = "M80027";
			break;
		case "唐門大勢已去":
			text2 = "당문의 대세는 이미 기욺";
			text3 = "M80028";
			break;
		case "龍湘覺醒":
			text2 = "용상 각성";
			text3 = "M80029";
			break;
		case "三師兄重傷":
			text2 = "삼사형 중상";
			text3 = "M80030";
			break;
		case "大師兄歸來":
			text2 = "대사형 귀환";
			text3 = "M80031";
			break;
		case "瑞笙來訪 大俠的意思":
			text2 = "서생 방문, 대협의 뜻";
			text3 = "M80032";
			break;
		case "武林群俠投降":
			text2 = "무림 군협 항복";
			text3 = "M80033";
			break;
		case "尋找龍湘":
			text2 = "용상 찾기";
			text3 = "M80034";
			break;
		case "尋找小蘭":
			text2 = "하후란 찾기";
			text3 = "M80035";
			break;
		case "大師兄復活的原因":
			text2 = "대사형 부활 원인";
			text3 = "M80036";
			break;
		case "大師兄你是怎麼回事":
			text2 = "대사형 당신은 어떻게 된 겁니까";
			text3 = "M80037";
			break;
		case "瑞笙來訪時把瑞笙趕走":
			text2 = "서생 방문 시 서생을 쫓아냄";
			text3 = "M80038";
			break;
		case "丹霞子退隱":
			text2 = "단하자 은퇴";
			text3 = "M80039";
			break;
		case "關心小師妹":
			text2 = "소사매 걱정하기";
			text3 = "M80040";
			break;
		case "進入最後決戰":
			text2 = "최종 결전 진입";
			text3 = "M80041";
			break;
		case "捅死瑞笙":
			text2 = "서생을 찔러 죽임";
			text3 = "M80042";
			break;
		case "西武林盟 丐幫 馬富貴":
			text2 = "서무림맹 개방 이부귀";
			text3 = "M81000";
			break;
		case "西武林盟 丐幫 ":
			text2 = "서무림맹 개방 ";
			text3 = "M81001";
			break;
		case "西武林盟 青城 鄒博":
			text2 = "서무림맹 청성 추박";
			text3 = "M81002";
			break;
		case "西武林盟 青城 部分":
			text2 = "서무림맹 청성 일부";
			text3 = "M81003";
			break;
		case "西武林盟 峨嵋派":
			text2 = "서무림맹 아미파";
			text3 = "M81004";
			break;
		case "西武林盟 嵩山十八銅人":
			text2 = "서무림맹 숭산 십팔동인";
			text3 = "M81005";
			break;
		case "西武林盟 錦香宮":
			text2 = "서무림맹 금향궁";
			text3 = "M81006";
			break;
		case "連環火箭":
			text2 = "연환화전";
			text3 = "M81007";
			break;
		case "滄幫的背叛":
			text2 = "창방의 배신";
			text3 = "M81008";
			break;
		case "四師兄的陰損伎倆":
			text2 = "사사형의 음흉한 계략";
			text3 = "M81009";
			break;
		case "鄒博":
			text2 = "추박";
			text3 = "M81010";
			break;
		case "畫中仙":
			text2 = "화중선";
			text3 = "M81011";
			break;
		case "小梅 守護唐門":
			text2 = "우소매 당문 수호";
			text3 = "M81012";
			break;
		case "東武林盟 丐幫 馬富貴":
			text2 = "동무림맹 개방 이부귀";
			text3 = "M82000";
			break;
		case "東武林盟 丐幫 ":
			text2 = "동무림맹 개방 ";
			text3 = "M82001";
			break;
		case "東武林盟 南宮世家":
			text2 = "동무림맹 남궁세가";
			text3 = "M82002";
			break;
		case "東武林盟 上官世家":
			text2 = "동무림맹 상관세가";
			text3 = "M82003";
			break;
		case "東武林盟 全真派":
			text2 = "동무림맹 전진파";
			text3 = "M82004";
			break;
		case "東武林盟 崆峒派 金烏上人":
			text2 = "동무림맹 공동파 금오상인";
			text3 = "M82005";
			break;
		case "東武林盟 崆峒派":
			text2 = "동무림맹 공동파";
			text3 = "M82006";
			break;
		case "東武林盟 嵩山派":
			text2 = "동무림맹 숭산파";
			text3 = "M82007";
			break;
		case "東武林盟 點蒼派":
			text2 = "동무림맹 점창파";
			text3 = "M82008";
			break;
		case "東武林盟 錦香宮":
			text2 = "동무림맹 금향궁";
			text3 = "M82009";
			break;
		case "已贖回外堡":
			text2 = "외성 되찾음";
			text3 = "N00001";
			break;
		case "已贖回練功塔":
			text2 = "연공탑 되찾음";
			text3 = "N00002";
			break;
		case "二週目":
			text2 = "2회차";
			text3 = "NewGamePlus";
			break;
		case "被關次數":
			text2 = "감금된 횟수";
			text3 = "Prison_01";
			break;
		case "序章旗標\\n":
		case "序章旗標\n":
			text2 = "프롤로그 플래그\n";
			text3 = "Prologue";
			break;
		case "完成支線任務-反間諜":
			text2 = "서브 임무 완료 - 반간첩";
			text3 = "S0002";
			break;
		case "煎藥保存成敗":
			text2 = "약 달이기 보존 성공/실패";
			text3 = "S0002_01";
			break;
		case "觸發稻草人支線":
			text2 = "허수아비 서브퀘스트 발동";
			text3 = "S0014_01";
			break;
		case "拜夏侯蘭為師":
			text2 = "하후란을 스승으로 모심";
			text3 = "S0014_02";
			break;
		case "煉丹房支線成功":
			text2 = "연단방 서브퀘스트 성공";
			text3 = "S0015_01";
			break;
		case "觸發龍之憂鬱":
			text2 = "용의 우울 발동";
			text3 = "S0019_01";
			break;
		case "觸發勸離事件":
			text2 = "이탈 권유 사건 발동";
			text3 = "S0501_01";
			break;
		case "帳務先生":
			text2 = "회계 선생";
			text3 = "S2001_01";
			break;
		case "觸發上官螢在唐門支線":
			text2 = "상관형 당문 체류 서브퀘스트 발동";
			text3 = "S2002_01";
			break;
		case "絕世雞腿事件":
			text2 = "절세 닭다리 사건";
			text3 = "S2003_01";
			break;
		case "開啟絕世雞腿完結篇":
			text2 = "절세 닭다리 완결편 활성화";
			text3 = "S2003_02";
			break;
		case "已觸發霸凌1":
			text2 = "괴롭힘1 이미 발동됨";
			text3 = "S8400";
			break;
		case "已觸發偷懶怪1":
			text2 = "게으름뱅이1 이미 발동됨";
			text3 = "S9001";
			break;
		case "已觸發同門關懷":
			text2 = "동문 관심 이미 발동됨";
			text3 = "S9100";
			break;
		case "是否下雨":
			text2 = "비가 오는지 여부";
			text3 = "S9200";
			break;
		case "支持肩甲狀態 ":
			text3 = "S9904_01";
			switch (myvalue)
			{
			case 0:
				text4 = "알수없음";
				break;
			case 1:
				text4 = "지지";
				break;
			case 2:
				text4 = "반대";
				break;
			}
			text2 = "견갑 지지 상태(" + text4 + ")";
			break;
		case "大師兄談論南宮世家":
			text2 = "대사형 남궁세가에 대해 언급";
			text3 = "S9904_02";
			break;
		case "是否觸發通緝葉雲舟":
			text2 = "엽운주 현상수배 발동 여부";
			text3 = "S9907_01";
			break;
		case "通用數值 - 信心":
			text2 = "공통 수치 - 신심";
			text3 = "T0001";
			break;
		case "通用數值 - 下一回合減少行動回合數":
			text2 = "공통 수치 - 다음 턴 행동 턴 수 감소";
			text3 = "T0002";
			break;
		case "段考難度":
			text2 = "단련 시험 난이도";
			text3 = "T0003";
			break;
		case "決鬥泛用事件旗標1":
			text2 = "결투 범용 사건 플래그1";
			text3 = "TC0001";
			break;
		case "弟子房四師兄閒聊":
			text2 = "제자방 사사형 잡담";
			text3 = "Talk_Brother4";
			break;
		case "唐芳歸來":
			text2 = "당방 귀환";
			text3 = "E12106";
			break;
		case "破廟 資訊量":
			text2 = "폐허 사원 정보량";
			text3 = "M20033";
			break;
		case "與唐大鯨決鬥後續\r":
			text3 = "M20035";
			switch (myvalue)
			{
			case 1:
				text4 = "승리";
				break;
			case 2:
				text4 = "이사형 엄호";
				break;
			case 3:
				text4 = "엽운상 엄호";
				break;
			}
			text2 = "당대경과 결투 후(" + text4 + ")";
			break;
		case "離家出走時三師兄有下山送錢":
			text2 = "가출 시 삼사형이 산을 내려와 돈을 줌";
			text3 = "M20036";
			break;
		case "你回程的遭遇\r":
			text3 = "M20037";
			switch (myvalue)
			{
			case 1:
				text4 = "멧돼지";
				break;
			case 2:
				text4 = "산적";
				break;
			case 3:
				text4 = "소사매";
				break;
			}
			text2 = "당신의 귀환길 조우(" + text4 + ")";
			break;
		case "被葉雲裳張貼通緝令":
			text2 = "엽운상에 의해 현상수배됨";
			text3 = "M20038";
			break;
		case "青城留學 累積功勳":
			text2 = "청성 유학 중 공훈 누적";
			text3 = "M30040";
			break;
		case "青城留學 散步":
			text2 = "청성 유학 중 산책";
			text3 = "M30041";
			break;
		case "青城留學 特殊遭遇":
			text2 = "청성 유학 중 특별 조우";
			text3 = "M30042";
			break;
		case "青城留學 觸發舊識":
			text2 = "청성 유학 중 옛 지인 조우 발동";
			text3 = "M30043";
			break;
		case "青城留學 觸發嘉勉":
			text2 = "청성 유학 중 격려 발동";
			text3 = "M30044";
			break;
		case "青城留學 勒索過青城弟子":
			text2 = "청성 유학 중 청성 제자 갈취한 적 있음";
			text3 = "M30045";
			break;
		case "青城留學 觸發鬼神之相":
			text2 = "청성 유학 중 귀신의 상 발동";
			text3 = "M30046";
			break;
		case "青城留學 觸發鄒博歸來":
			text2 = "청성 유학 중 추박 귀환 발동";
			text3 = "M30047";
			break;
		case "青城留學 觸發庸才":
			text2 = "청성 유학 중 평범한 재능 발동";
			text3 = "M30048";
			break;
		case "青城留學 修練次數":
			text2 = "청성 유학에서 수련 횟수";
			text3 = "M30049";
			break;
		case "青城留學 申屠龍送主角秘笈":
			text2 = "청성 유학 중 신도룡이 주인공에게 비급을 주었다";
			text3 = "M30050";
			break;
		case "青城留學 與申屠龍切磋過":
			text2 = "청성 유학 중 신도룡과 겨뤄본 적이 있다";
			text3 = "M30051";
			break;
		case "青城留學 雲裳給申屠龍的秘笈\r\n1. 雪球\r\n2. 盤古\r\n3. 斬天":
		case "青城留學 雲裳給申屠龍的秘笈\n1. 雪球\n2. 盤古\n3. 斬天":
		case "青城留學 雲裳給申屠龍的秘笈":
			text3 = "M30052";
			text4 = myvalue switch
			{
				1 => "눈싸움 교전수칙", 
				2 => "반고개천파", 
				3 => "참천흑익쌍룡검", 
				_ => "", 
			};
			text2 = (string.IsNullOrEmpty(text4) ? "청성 유학 중 운상이 신도룡에게 준 비급" : ("청성 유학 중 운상이 신도룡에게 준 비급(" + text4 + ")"));
			break;
		case "青城留學 葉雲裳回唐門":
			text2 = "청성 유학 중 엽운상 당문 복귀";
			text3 = "M30053";
			break;
		case "青城留學 申屠龍散步遭遇襲擊":
			text2 = "청성 유학 중 신도룡 산책 시 습격 조우";
			text3 = "M30054";
			break;
		case "青城留學 散步遭遇申屠龍機率上升":
			text2 = "청성 유학 중 산책 시 신도룡 조우 확률 상승";
			text3 = "M30055";
			break;
		case "青城留學 聽段氏世系譜表":
			text2 = "청성 유학 중 단씨 세계보를 들음";
			text3 = "M30056";
			break;
		case "青城留學 與段智秀打架過":
			text2 = "청성 유학 중 단지수와 싸운 적 있음";
			text3 = "M30057";
			break;
		case "青城留學 幕後主使":
			text2 = "청성 유학 배후 주동자";
			text3 = "M30058";
			break;
		case "青城留學 比試人選中，申屠龍在場":
			text2 = "청성 유학 중 비무 인선에 신도룡이 있음";
			text3 = "M30061";
			break;
		case "青城留學 趙逵上山":
			text2 = "청성 유학 중 조규가 산에 오름";
			text3 = "M30062";
			break;
		case "青城留學跟牛頭馬面打過":
			text2 = "청성 유학 중 우두마면과 싸운 적 있음";
			text3 = "M30064";
			break;
		case "啟動修羅場事件":
			text2 = "수라장 사건이 시작되었다";
			text3 = "M70059";
			break;
		case "開啟陪小師妹":
			text2 = "소사매와 함께하기를 시작했다";
			text3 = "M70060";
			break;
		case "開啟陪葉雲裳":
			text2 = "엽운상과 함께하기를 시작했다";
			text3 = "M70061";
			break;
		case "開啟陪上官螢":
			text2 = "상관형과 함께하기를 시작했다";
			text3 = "M70062";
			break;
		case "開啟陪龍湘":
			text2 = "용상과 함께하기를 시작했다";
			text3 = "M70063";
			break;
		case "開啟陪小蘭":
			text2 = "하후란과 함께하기를 시작했다";
			text3 = "M70064";
			break;
		case "支線劍聖極招":
			text2 = "서브퀘스트: 검성극초";
			text3 = "M80048";
			break;
		case "決戰前下山":
			text2 = "결전 전 하산";
			text3 = "M80051";
			break;
		case "決戰前後山":
			text2 = "결전 후 뒷산";
			text3 = "M80052";
			break;
		case "金烏上人成為掌派人":
			text2 = "금오상인 장파인 됨";
			text3 = "M50023";
			break;
		}
		if (text2 != null)
		{
			return text2;
		}
		string text5 = null;
		string text6 = text3;
		if (string.IsNullOrEmpty(text6) || !LooksLikeFlagId(text6))
		{
			text6 = text;
		}
		if (!string.IsNullOrEmpty(text6))
		{
			text5 = GetLocalizedSystemTextForTranslations($"Flag/Desc/{text6}_{myvalue}", null, "플래그값설명", $"{text6}_{myvalue}");
			if (string.IsNullOrEmpty(text5))
			{
				text5 = GetLocalizedSystemTextForTranslations("Flag/Name/" + text6, null, "플래그명", text6);
			}
		}
		if (!string.IsNullOrEmpty(text5))
		{
			return text5;
		}
		string text7 = TryTranslateStructuredDevNote(mydevnote, myvalue);
		if (!string.IsNullOrEmpty(text7))
		{
			return text7;
		}
		string text8 = UiTextMapper.ApplyTranslations(mydevnote);
		if (!string.IsNullOrEmpty(text8) && text8 != mydevnote)
		{
			return text8;
		}
		string text9 = text3;
		if (string.IsNullOrEmpty(text9) || !LooksLikeFlagId(text9))
		{
			text9 = text;
		}
		string localizationKey = (string.IsNullOrEmpty(text9) ? mydevnote : ("Flag/Name/" + text9));
		UntranslatedLogger.LogUntranslated("플래그_최종실패", localizationKey, $"{mydevnote} ({myvalue})");
		return $"{mydevnote} ({myvalue})";
	}

	private static string TryTranslateStructuredDevNote(string source, int state)
	{
		if (string.IsNullOrEmpty(source) || source.IndexOf('\n') < 0)
		{
			return null;
		}
		string[] array = source.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			return null;
		}
		string text = array[0].Trim();
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		string text2 = UiTextMapper.ApplyTranslations(text);
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		for (int i = 1; i < array.Length; i++)
		{
			string text3 = array[i].Trim();
			if (!string.IsNullOrEmpty(text3))
			{
				Match match = Regex.Match(text3, "^([0-9]+)\\s*[\\.:：、\\)\\]]\\s*(.+)$");
				if (match.Success && int.TryParse(match.Groups[1].Value, out var result))
				{
					dictionary[result] = match.Groups[2].Value.Trim();
				}
			}
		}
		if (dictionary.TryGetValue(state, out var value))
		{
			string text4 = UiTextMapper.ApplyTranslations(value);
			if (!(text2 != text) && !(text4 != value))
			{
				return null;
			}
			if (!string.IsNullOrEmpty(text4))
			{
				return text2 + "(" + text4 + ")";
			}
			return text2;
		}
		if (!(text2 != text))
		{
			return null;
		}
		return text2;
	}

	private static bool LooksLikeFlagId(string value)
	{
		if (string.IsNullOrEmpty(value) || value.Length < 2 || !char.IsLetter(value[0]))
		{
			return false;
		}
		bool result = false;
		for (int i = 1; i < value.Length; i++)
		{
			char c = value[i];
			if (char.IsDigit(c))
			{
				result = true;
			}
			else if (c != '_')
			{
				return false;
			}
		}
		return result;
	}

	private static string NormalizeDevNoteKey(string raw)
	{
		if (string.IsNullOrEmpty(raw))
		{
			return raw ?? string.Empty;
		}
		return raw.Replace("\r\n", "\n").Replace('\r', '\n').Replace("決鬪", "決鬥")
			.Replace("决斗", "決鬥")
			.Replace("决鬪", "決鬥")
			.Replace("挑战", "挑戰")
			.Replace("胜败", "勝敗")
			.Replace("胜利", "勝利")
			.Replace("失败", "失敗")
			.Replace("卧", "臥")
			.Replace("岗", "崗")
			.Replace("冈", "崗")
			.Replace("岡", "崗")
			.Replace("凤", "鳳")
			.Replace("万", "萬")
			.Replace("鳳雲萬", "臥雲崗")
			.Replace("凤云万", "臥雲崗")
			.Replace("耻雲", "臥雲")
			.Replace("耻云", "臥雲")
			.Replace("不跟随", "不跟隨")
			.Replace("跟随", "跟隨")
			.Replace("留学", "留學")
			.Replace("儒学", "儒學")
			.Replace("佛学", "佛學")
			.Replace("道学", "道學");
	}

	private static string GetLocalizedSystemTextForTranslations(string keyToAttempt, string defaultValue, string categoryForLog, string originalIdentifierIfKeyFailed = null)
	{
		LocalizationManager instance = LocalizationManager.Instance;
		if (((instance != null) ? instance.LocaleResolver : null) != null && !string.IsNullOrEmpty(keyToAttempt))
		{
			string text = LocalizationManager.Instance.LocaleResolver.GetString(keyToAttempt);
			if (!string.IsNullOrEmpty(text) && text != keyToAttempt)
			{
				return text;
			}
			if (text == keyToAttempt || string.IsNullOrEmpty(text))
			{
				UntranslatedLogger.LogUntranslated(categoryForLog, keyToAttempt, string.IsNullOrEmpty(originalIdentifierIfKeyFailed) ? keyToAttempt : originalIdentifierIfKeyFailed);
			}
		}
		return defaultValue;
	}
}
