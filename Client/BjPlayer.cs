using System.Collections.Generic;
using System.Linq;
using Common;
using Roleplay.SharedClasses;

namespace Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack
{
	public class BjPlayer
	{
		private const int MaxHands = 10;

		public BjPlayer( int netId, string name, int position, int chips ) {
			NetId = netId;
			Name = name;
			Position = position;
			Chips = chips;
			CurrentHands = new List<BjHand>();
		}

		public string Name { get; set; }
		public int NetId { get; set; }
		public int Handle { get; set; }
		public int Position { get; set; }
		public List<BjHand> CurrentHands { get; set; }
		public int CurrentBet { get; set; }
		public int Chips { get; set; }
		public int HandInsurance { get; set; }

		public int CurrentWager
		{
			get {
				int total = 0;
				foreach( var hand in CurrentHands ) total = total + hand.Bet;

				return total;
			}
		}

		/// <summary>
		///     Gets the get active hand.
		/// </summary>
		/// <value>
		///     The get active hand.
		/// </value>
		public BjHand GetActiveHand => CurrentHands.FirstOrDefault( h => h.IsActive && !h.ActionFinished );

		/// <summary>
		///     Handles the round payout.
		/// </summary>
		/// <param name="dealerValue">The dealer value.</param>
		/// <param name="blackjackBonus">The blackjack bonus.</param>
		/// <param name="dealerHasBlackjack">if set to <c>true</c> [dealer has blackjack].</param>
		public void HandleRoundPayout( int dealerValue, float blackjackBonus, bool dealerHasBlackjack ) {
			foreach( var hand in CurrentHands )
				if( dealerHasBlackjack ) {
					if( HandInsurance > 0 ) {
						Chips = Chips + HandInsurance * 2;
					}

					HandInsurance = 0;
				}
				else if( hand.IsBusted ) {
					// Do nothing
				}
				else if( hand.IsPush( dealerValue ) ) {
					Chips = Chips + hand.Bet;
				}
				else if( hand.IsBlackjack ) {
					Chips = Chips + hand.Bet + (int)(hand.Bet * blackjackBonus);
				}
				else if( hand.IsWinningHand( dealerValue ) ) {
					Chips = Chips + hand.Bet + hand.Bet;
				}
		}

		/// <summary>
		/// Performs the action.
		/// </summary>
		/// <param name="action">The action.</param>
		/// <param name="shoe">The shoe.</param>
		/// <returns></returns>
		public bool PerformAction( BjActionsEnum action, BjShoe shoe ) {
			if( GetActiveHand == null ) return false;
			var success = false;
			switch( action ) {
			case BjActionsEnum.DealNewHand:
				success = true;
				break;
			case BjActionsEnum.Hit:
				success = IsHitSuccess( GetActiveHand, shoe );
				break;
			case BjActionsEnum.Stand:
				GetActiveHand.IsStanding = true;
				success = true;
				break;
			case BjActionsEnum.DoubleDown:
				if( CurrentHands.Count >= MaxHands ) return false;
				success = IsDoubleDownSuccess( GetActiveHand, shoe );
				if( success ) {
					Chips = Chips - GetActiveHand.Bet;
				}
				GetActiveHand.IsDoubleDown = success;
				break;
			case BjActionsEnum.Split:
				if( CurrentHands.Count >= MaxHands ) return false;
				success = IsSplitSuccess( GetActiveHand, shoe );
				if( success ) {
					Chips = Chips - GetActiveHand.Bet;
				}
				break;
			}

			Log.Info($"PerformAction:hand={GetActiveHand},action={action},success={success}" );

			return success;
		}

		/// <summary>
		///     Deals the new round.
		/// </summary>
		/// <param name="shoe">The shoe.</param>
		/// <returns></returns>
		public bool DealNewRound( BjShoe shoe ) {
			var hand = new BjHand( CurrentBet );
			if( !hand.HitHand( shoe ) ) return false;

			CurrentHands = new List<BjHand> {hand};
			return true;
		}

		/// <summary>
		///     Determines whether [is hit success] [the specified hand].
		/// </summary>
		/// <param name="hand">The hand.</param>
		/// <param name="shoe">The shoe.</param>
		/// <returns>
		///     <c>true</c> if [is hit success] [the specified hand]; otherwise, <c>false</c>.
		/// </returns>
		public bool IsHitSuccess( BjHand hand, BjShoe shoe ) {
			if( !HandExists( hand ) ) return false;
			return hand.HitHand( shoe );
		}

		/// <summary>
		///     Determines whether [is double down success] [the specified hand].
		/// </summary>
		/// <param name="hand">The hand.</param>
		/// <param name="shoe">The shoe.</param>
		/// <returns>
		///     <c>true</c> if [is double down success] [the specified hand]; otherwise, <c>false</c>.
		/// </returns>
		public bool IsDoubleDownSuccess( BjHand hand, BjShoe shoe ) {
			if( !HandExists( hand ) ) {
				return false;
			}
			return hand.HitHand( shoe );
		}

		/// <summary>
		///     Determines whether [is split success] [the specified hand].
		/// </summary>
		/// <param name="hand">The hand.</param>
		/// <param name="shoe">The shoe.</param>
		/// <returns>
		///     <c>true</c> if [is split success] [the specified hand]; otherwise, <c>false</c>.
		/// </returns>
		public bool IsSplitSuccess( BjHand hand, BjShoe shoe ) {
			if( !HandExists( hand ) ) return false;
			return hand.SplitHand( shoe, this );
		}

		/// <summary>
		///     Hands the exists.
		/// </summary>
		/// <param name="hand">The hand.</param>
		/// <returns></returns>
		private bool HandExists( BjHand hand ) {
			if( !CurrentHands.Contains( hand ) ) {
				Log.Error( "CheckHand: hand missing from CurrentHands." );
				return false;
			}

			return true;
		}

		public void DebugHands( int numberOfhands ) {
			for( int i = 0; i < numberOfhands; i++ ) {
				var hand = new BjHand( 10 );
				int number = Rand.GetRange( 2, 8 );
				hand.DebugSetHand( number );
				CurrentHands.Add( hand );
			}
		}
	}
}