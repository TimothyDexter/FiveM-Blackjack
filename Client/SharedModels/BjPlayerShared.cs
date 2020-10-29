using System.Collections.Generic;

namespace Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack.SharedModels
{
	public class BjPlayerShared
	{
		private const int MaxHands = 10;

		public BjPlayerShared( int netId, string name, int position, int chips ) {
			NetId = netId;
			Name = name;
			Position = position;
			Chips = chips;
			CurrentHands = new List<BjHandShared>();
		}

		public string Name { get; set; }
		public int NetId { get; set; }
		public int Position { get; set; }
		public List<BjHandShared> CurrentHands { get; set; }
		public int CurrentBet { get; set; }
		public int Chips { get; set; }
		public int HandInsurance { get; set; }
	}
}