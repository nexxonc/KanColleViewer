﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grabacr07.KanColleWrapper.Models
{
	/// <summary>
	/// 速力を示す識別子を定義します。
	/// </summary>
	public enum ShipSpeed
	{
		/// <summary>
		/// 不動。(基地等)
		/// </summary>
		Immovable = 0,

		/// <summary>
		/// 低速。
		/// </summary>
		Low = 5,

		/// <summary>
		/// 高速。
		/// </summary>
		Fast = 10,

		/// <summary>
		/// 高速+。
		/// </summary>
		FastPlus = 15,

		/// <summary>
		/// 最速。
		/// </summary>
		SuperFast = 20,
	}
}
