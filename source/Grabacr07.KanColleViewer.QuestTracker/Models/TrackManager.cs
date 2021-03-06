﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Reactive.Linq;

using Grabacr07.KanColleWrapper;
using Grabacr07.KanColleWrapper.Models;
using Grabacr07.KanColleWrapper.Models.Raw;

using Grabacr07.KanColleViewer.QuestTracker.Models;
using Grabacr07.KanColleViewer.QuestTracker.Models.Model;
using Grabacr07.KanColleViewer.QuestTracker.Models.EventArgs;

namespace Grabacr07.KanColleViewer.QuestTracker.Models
{
	public class TrackManager
	{
		public Assembly Assembly => Assembly.GetExecutingAssembly();

		public ObservableCollection<ITracker> trackingAvailable
		{
			get; private set;
		} = new ObservableCollection<ITracker>();

		public List<ITracker> TrackingQuests => trackingAvailable.Where(x => x.IsTracking).ToList();
		public List<ITracker> AllQuests => trackingAvailable.ToList();

		internal event EventHandler<BattleResultEventArgs> BattleResultEvent;
		internal event EventHandler<MissionResultEventArgs> MissionResultEvent;
		internal event EventHandler<PracticeResultEventArgs> PracticeResultEvent;
		internal event EventHandler RepairStartEvent;
		internal event EventHandler ChargeEvent;
		internal event EventHandler<BaseEventArgs> CreateItemEvent;
		internal event EventHandler CreateShipEvent;
		internal event EventHandler DestoryShipEvent;
		internal event EventHandler<DestroyItemEventArgs> DestoryItemEvent;
		internal event EventHandler<BaseEventArgs> PowerUpEvent;
		internal event EventHandler<BaseEventArgs> ReModelEvent;
		internal event EventHandler HenseiEvent;
		internal event EventHandler EquipEvent;

		public readonly System.EventArgs EmptyEventArg = new System.EventArgs();
		public event EventHandler QuestsEventChanged;

		private Func<bool> PreprocessCheck { get; }
		private void Preprocess(Action action)
		{
			if ((!PreprocessCheck?.Invoke()) ?? false) return;

			try { action(); }
			catch { }
		}

		public TrackManager(Func<bool> PreprocessCheck)
		{
			this.PreprocessCheck = PreprocessCheck;

			var homeport = KanColleClient.Current.Homeport;
			var proxy = KanColleClient.Current.Proxy;
			var MapInfo = new TrackerMapInfo();
			var battleTracker = new BattleTracker();

			// 편성 변경
			homeport.Organization.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(homeport.Organization.Fleets))
				{
					var fleets = homeport.Organization.Fleets.Select(x => x.Value);
					foreach (var x in fleets)
						x.State.Updated += (_, _2) => Preprocess(() => HenseiEvent?.Invoke(this, this.EmptyEventArg));
				}
			};
			// 장비 변경
			proxy.api_req_kaisou_slot_exchange_index.TryParse<kcsapi_slot_exchange_index>()
				.Subscribe(x => Preprocess(() => EquipEvent?.Invoke(this, this.EmptyEventArg)));
			proxy.api_req_kaisou_slot_deprive.TryParse<kcsapi_slot_deprive>()
				.Subscribe(x => Preprocess(() => EquipEvent?.Invoke(this, this.EmptyEventArg)));

			// 연습전 종료
			proxy.ApiSessionSource.Where(x => x.Request.PathAndQuery == "/kcsapi/api_req_practice/battle_result")
				.TryParse<kcsapi_practice_result>().Subscribe(x =>
					Preprocess(() => PracticeResultEvent?.Invoke(this, new PracticeResultEventArgs(x.Data)))
				);

			// 근대화 개수
			proxy.api_req_kaisou_powerup.TryParse<kcsapi_powerup>()
				.Subscribe(x => Preprocess(() => PowerUpEvent?.Invoke(this, new BaseEventArgs(x.Data.api_powerup_flag != 0))));

			// 개수공창 개수
			proxy.api_req_kousyou_remodel_slot.TryParse<kcsapi_remodel_slot>()
				.Subscribe(x => Preprocess(() => ReModelEvent?.Invoke(this, new BaseEventArgs(x.Data.api_remodel_flag != 0))));

			// 폐기
			proxy.api_req_kousyou_destroyitem2.TryParse<kcsapi_destroyitem2>()
				.Subscribe(x => Preprocess(() => DestoryItemEvent?.Invoke(this, new DestroyItemEventArgs(x.Request, x.Data))));

			// 해체
			proxy.api_req_kousyou_destroyship.TryParse<kcsapi_destroyship>()
				.Subscribe(x => Preprocess(() => DestoryShipEvent?.Invoke(this, this.EmptyEventArg)));

			// 건조
			proxy.api_req_kousyou_createship.TryParse<kcsapi_createship>()
				.Subscribe(x => Preprocess(() => CreateShipEvent?.Invoke(this, this.EmptyEventArg)));

			// 개발
			proxy.api_req_kousyou_createitem.TryParse<kcsapi_createitem>()
				.Subscribe(x => Preprocess(() => CreateItemEvent?.Invoke(this, new BaseEventArgs(x.Data.api_create_flag != 0))));

			// 보급
			proxy.api_req_hokyu_charge.TryParse<kcsapi_charge>()
				.Subscribe(x => Preprocess(() => ChargeEvent?.Invoke(this, this.EmptyEventArg)));

			// 입거
			proxy.ApiSessionSource.Where(x => x.Request.PathAndQuery == "/kcsapi/api_req_nyukyo/start")
				.Subscribe(x => Preprocess(() => RepairStartEvent?.Invoke(this, this.EmptyEventArg)));

			// 원정
			proxy.api_req_mission_result.TryParse<kcsapi_mission_result>()
				.Subscribe(x => Preprocess(() => MissionResultEvent?.Invoke(this, new MissionResultEventArgs(x.Data))));

			// 출격 (시작, 진격)
			proxy.api_req_map_start.TryParse<kcsapi_map_start>()
				.Subscribe(x => Preprocess(() => MapInfo.Reset(x.Data.api_maparea_id, x.Data.api_mapinfo_no, x.Data.api_no)));
			proxy.api_req_map_next.TryParse<kcsapi_map_start>()
				.Subscribe(x => Preprocess(() => MapInfo.Next(x.Data.api_maparea_id, x.Data.api_mapinfo_no, x.Data.api_no)));

			// 통상 - 주간전
			proxy.api_req_sortie_battle.TryParse<kcsapi_sortie_battle>()
				.Subscribe(x => Preprocess(() => battleTracker.BattleProcess(x.Data)));

			// 통상 - 야전
			proxy.ApiSessionSource.Where(x => x.Request.PathAndQuery == "/kcsapi/api_req_battle_midnight/battle")
				.TryParse<kcsapi_battle_midnight_battle>()
				.Subscribe(x => Preprocess(() => battleTracker.BattleProcess(x.Data)));

			// 통상 - 개막야전
			proxy.ApiSessionSource.Where(x => x.Request.PathAndQuery == "/kcsapi/api_req_battle_midnight/sp_midnight")
				.TryParse<kcsapi_battle_midnight_sp_midnight>()
				.Subscribe(x => Preprocess(() => battleTracker.BattleProcess(x.Data)));

			// 전투 종료 (연합함대 포함)
			proxy.api_req_sortie_battleresult.TryParse<kcsapi_battleresult>()
				.Subscribe(x => Preprocess(() => BattleResultEvent?.Invoke(this, new BattleResultEventArgs(MapInfo.AfterCombat(), battleTracker.enemyShips, x.Data))));
			proxy.api_req_combined_battle_battleresult.TryParse<kcsapi_combined_battle_battleresult>()
				.Subscribe(x => Preprocess(() => BattleResultEvent?.Invoke(this, new BattleResultEventArgs(MapInfo.AfterCombat(), battleTracker.enemyShips, x.Data))));


			// Register all trackers
			trackingAvailable = new ObservableCollection<ITracker>(trackingAvailable.OrderBy(x => x.Id));
			trackingAvailable.CollectionChanged += (sender, e) =>
			{
				if (e.Action != NotifyCollectionChangedAction.Add)
					return;

				foreach (ITracker tracker in e.NewItems)
				{
					tracker.RegisterEvent(this);
					tracker.ResetQuest();
					tracker.ProcessChanged += ((x, y) =>
					{
						try
						{
							QuestsEventChanged?.Invoke(this, EmptyEventArg);
						}
						catch { }
					});
				}
			};
		}

		public void RefreshTrackers()
		{
			Preprocess(() => HenseiEvent?.Invoke(this, EmptyEventArg));
			Preprocess(() => EquipEvent?.Invoke(this, EmptyEventArg));
			QuestsEventChanged?.Invoke(this, EmptyEventArg);
		}
	}
}
