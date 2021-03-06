﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grabacr07.KanColleWrapper;
using Grabacr07.KanColleWrapper.Models;
using Grabacr07.KanColleViewer.QuestTracker.Models.Extensions;

namespace Grabacr07.KanColleViewer.QuestTracker.Models.Tracker
{
	/// <summary>
	/// 수뢰전대 남서쪽으로!
	/// </summary>
	internal class Bm3 : ITracker
	{
		private readonly int max_count = 1;
		private int count;

		public event EventHandler ProcessChanged;

		int ITracker.Id => 257;
		public QuestType Type => QuestType.Monthly;
		public bool IsTracking { get; set; }

		private System.EventArgs emptyEventArgs = new System.EventArgs();

		public void RegisterEvent(TrackManager manager)
		{
			manager.BattleResultEvent += (sender, args) =>
			{
				if (!IsTracking) return;

				if (args.MapWorldId != 1 || args.MapAreaId != 4) return; // 1-4
				if ("敵機動部隊" != args.EnemyName) return; // boss
				if (args.Rank != "S") return;

				var fleet = KanColleClient.Current.Homeport.Organization.Fleets.FirstOrDefault(x => x.Value.IsInSortie).Value;

				if (fleet?.Ships[0]?.Info.ShipType.Id != 3) return; // 기함 경순양함 이외
				if (fleet?.Ships.Any(x => x.Info.ShipType.Id != 2 && x.Info.ShipType.Id != 3) ?? false) return; // 구축함, 경순양함 이외 함종
				if (fleet?.Ships.Count(x => x.Info.ShipType.Id == 3) > 3) return; // 경순양함 3척 이상
				if (fleet?.Ships.Count(x => x.Info.ShipType.Id == 2) < 3) return; // 구축함 3척 미만

				count = count.Add(1).Max(max_count);

				ProcessChanged?.Invoke(this, emptyEventArgs);
			};
		}

		public void ResetQuest()
		{
			count = 0;
			ProcessChanged?.Invoke(this, emptyEventArgs);
		}

		public int GetProgress()
		{
			return count * 100 / max_count;
		}

		public string GetProgressText()
		{
			return count >= max_count ? "완료" : "1-4 보스전 S 승리 " + count.ToString() + " / " + max_count.ToString();
		}

		public string SerializeData()
		{
			return count.ToString();
		}

		public void DeserializeData(string data)
		{
			count = 0;
			int.TryParse(data, out count);
		}
	}
}
