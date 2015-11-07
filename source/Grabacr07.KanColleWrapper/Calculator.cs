﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grabacr07.KanColleWrapper.Models;

namespace Grabacr07.KanColleWrapper
{
	internal static class Calculator
	{
		#region 制空値計算

		/// <summary>
		/// 装備と搭載数を指定して、スロット単位の制空能力を計算します。
		/// </summary>
		/// <param name="slotItem">対空能力を持つ装備。</param>
		/// <param name="onslot">搭載数。</param>
		/// <returns></returns>
		public static double CalcAirSuperiorityPotential(this SlotItem slotItem, int onslot)
		{
			if (slotItem.Info.IsAirSuperiorityFighter)
			{
				if (KanColleClient.Current.Settings.SqrtDoubleToInt)
				{
					return Math.Truncate(slotItem.Info.AA * Math.Sqrt(onslot));
				}
				else return slotItem.Info.AA * Math.Sqrt(onslot);
			}

			return 0;
		}
		/// <summary>
		/// 指定した艦の制空能力を計算します。
		/// </summary>
		public static double CalcAirSuperiorityPotential(this Ship ship)
		{
			return ship.EquippedItems
				.Select(x => x.Item.CalcAirSuperiorityPotential(x.Current))
				.Sum();
		}

		/// <summary>
		/// 指定した艦の制空能力の最小値を計算します。
		/// </summary>
		public static double CalcMinAirSuperiorityPotential(this Ship ship)
		{
			return ship.EquippedItems
				.Select(x => x.Item.CalcAirSuperiorityPotential(x.Current)
							+ x.Item.CalcMinAirecraftAdeptBonus(x.Current))
				.Sum();
		}
		/// <summary>
		/// 촉접개시율을 계산
		/// </summary>
		/// <param name="ship"></param>
		/// <returns></returns>
		public static double CalcFirstEncounterPercent(this Ship ship)
		{
			if (ship.Info.IsAirCraft)
			{
				return ship.EquippedItems
				.Select(x => x.Item.CalcFirstEncounterPercent(x.Current))
				.Sum();
			}
			return 0;
		}
		/// <summary>
		/// 장비하고 있는 함재기중에 촉접 가능 함재기의 명중률 목록을 뽑아냅니다.
		/// </summary>
		/// <param name="ship"></param>
		/// <returns></returns>
		public static List<int> HitStatusList(this Ship ship)
		{
			if (ship.Info.IsAirCraft)
			{
				List<int> templist = new List<int>();
				for (int i = 0; i < ship.EquippedItems.Count(); i++)
				{
					if (ship.EquippedItems[i].Item.Info.IsSecondEncounter)
					{
						templist.Add(ship.EquippedItems[i].Item.Info.Hit);
                    }
				}
				return templist;
			}

			return new List<int>();//항공모함이 아닌경우 빈 리스트를 반환
		}

		/// <summary>
		/// 指定した艦の制空能力の最大値を計算します。
		/// </summary>
		public static double CalcMaxAirSuperiorityPotential(this Ship ship)
		{
			return ship.EquippedItems
				.Select(x => x.Item.CalcAirSuperiorityPotential(x.Current)
							+ x.Item.CalcMaxAirecraftAdeptBonus(x.Current))
				.Sum();
		}
		/// <summary>
		/// 熟練度による制空能力ボーナス最小値を計算します。
		/// </summary>
		/// <param name="slotItem">対空能力を持つ装備。</param>
		/// <param name="onslot">搭載数。</param>
		/// <returns></returns>
		private static double CalcMinAirecraftAdeptBonus(this SlotItem slotItem, int onslot)
			=> onslot < 1 ? 0
			: slotItem.Info.Type == SlotItemType.艦上戦闘機
				? slotItem.CalcAirecraftAdeptBonusOfType() + slotItem.CalcMinInternalAirecraftAdeptBonus()
				: 0;    // 艦戦以外は簡単に吹き飛ぶので最小値としては計算に入れない

		/// <summary>
		/// 촉접 개시율을 계산합니다. 이 값은 모두 합산되서 사용됩니다. 함상정찰기와 수상정찰기 한정
		/// </summary>
		/// <param name="slotItem"></param>
		/// <param name="onslot"></param>
		/// <returns></returns>
		private static double CalcFirstEncounterPercent(this SlotItem slotItem, int onslot)
		{
			if (slotItem.Info.IsFirstEncounter)
			{
				return slotItem.Info.ViewRange * 0.04 * Math.Sqrt(onslot);
			}

			return 0;
		}

		/// <summary>
		/// 熟練度による制空能力ボーナス最大値を計算します。
		/// </summary>
		/// <param name="slotItem">対空能力を持つ装備。</param>
		/// <param name="onslot">搭載数。</param>
		/// <returns></returns>
		private static double CalcMaxAirecraftAdeptBonus(this SlotItem slotItem, int onslot)
			=> onslot < 1 ? 0
			: slotItem.CalcAirecraftAdeptBonusOfType() + slotItem.CalcMaxInternalAirecraftAdeptBonus();

		/// <summary>
		/// 各表記熟練度に対応した機種別熟練度ボーナスを計算します。
		/// </summary>
		/// <param name="slotItem"></param>
		/// <returns></returns>
		private static int CalcAirecraftAdeptBonusOfType(this SlotItem slotItem)
			=> slotItem.Info.Type == SlotItemType.艦上戦闘機
				? slotItem.Adept == 1 ? 0
				: slotItem.Adept == 2 ? 2
				: slotItem.Adept == 3 ? 5
				: slotItem.Adept == 4 ? 9
				: slotItem.Adept == 5 ? 14
				: slotItem.Adept == 6 ? 14
				: slotItem.Adept == 7 ? 22
				: 0 // Adept == 0
			: slotItem.Info.Type == SlotItemType.水上爆撃機
				? slotItem.Adept == 1 ? 0
				: slotItem.Adept == 2 ? 1
				: slotItem.Adept == 3 ? 1
				: slotItem.Adept == 4 ? 1
				: slotItem.Adept == 5 ? 3
				: slotItem.Adept == 6 ? 3
				: slotItem.Adept == 7 ? 6
				: 0 // Adept == 0
			: 0;

		/// <summary>
		/// 各表記熟練度に対応した艦載機内部熟練度ボーナスの最小値を計算します。
		/// </summary>
		/// <param name="slotItem"></param>
		/// <returns></returns>
		private static double CalcMinInternalAirecraftAdeptBonus(this SlotItem slotItem)
		{
			if (KanColleClient.Current.Settings.SqrtDoubleToInt)
			{
				return Math.Truncate(Math.Sqrt((slotItem.Adept != 0 ? (slotItem.Adept - 1) * 15 + 10 : 0) / 10));
			}
			else return Math.Sqrt((slotItem.Adept != 0 ? (slotItem.Adept - 1) * 15 + 10 : 0) / 10);
		}

		/// <summary>
		/// 各表記熟練度に対応した艦載機内部熟練度ボーナスの最大値を計算します。
		/// </summary>
		/// <param name="slotItem"></param>
		/// <returns></returns>
		private static double CalcMaxInternalAirecraftAdeptBonus(this SlotItem slotItem)
		{
			if (KanColleClient.Current.Settings.SqrtDoubleToInt)
			{
				return Math.Truncate(Math.Sqrt((slotItem.Adept != 0 ? (slotItem.Adept - 1) * 15 + 24 : 9) / 10));
			}
			else return Math.Sqrt((slotItem.Adept != 0 ? (slotItem.Adept - 1) * 15 + 24 : 9) / 10);
		}

		#endregion

		public static double CalcViewRange(this Fleet fleet)
		{
			return ViewRangeCalcLogic.Get(KanColleClient.Current.Settings.ViewRangeCalcType).Calc(fleet.Ships);
		}

		public static bool IsHeavilyDamage(this LimitedValue hp)
		{
			return (hp.Current / (double)hp.Maximum) <= 0.25;
		}

		/// <summary>
		/// 現在のシーケンスから護衛退避した艦娘を除きます。
		/// </summary>
		public static IEnumerable<Ship> WithoutEvacuated(this IEnumerable<Ship> ships)
		{
			return ships.Where(ship => !ship.Situation.HasFlag(ShipSituation.Evacuation) && !ship.Situation.HasFlag(ShipSituation.Tow));
		}
	}
}