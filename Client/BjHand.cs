using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack
{
	public class BjHand
	{
		public BjHand() {
			Cards = new List<PlayingCard>();
			Bet = 0;
		}

		public BjHand( int bet ) {
			Cards = new List<PlayingCard>();
			Bet = bet;
		}

		public BjHand( int bet, PlayingCard card ) {
			Cards = new List<PlayingCard> {card};
			Bet = bet;
		}

		public BjHand( int bet, List<PlayingCard> cards ) {
			Cards = cards;
			Bet = bet;
		}

		public List<PlayingCard> Cards { get; set; }
		public int Bet { get; set; }
		public bool IsActive { get; set; }
		public bool ActionFinished => IsBlackjack || IsStanding || IsBusted || IsDoubleDown;

		public bool IsDoubleDown { get; set; }

		public bool IsStanding { get; set; }

		/// <summary>
		///     Gets the maximum value.
		/// </summary>
		/// <value>
		///     The maximum value.
		/// </value>
		public int MaxValue
		{
			get {
				int value = Value;
				if( ContainsAce )
					if( value + 10 <= 21 )
						return value + 10;

				return value;
			}
		}

		/// <summary>
		///     Gets a value indicating whether this instance is busted.
		/// </summary>
		/// <value>
		///     <c>true</c> if this instance is busted; otherwise, <c>false</c>.
		/// </value>
		public bool IsBusted
		{
			get {
				if( Cards == null ) return false;
				return Cards.Sum( c => c.Value ) > 21;
			}
		}

		/// <summary>
		///     Gets a value indicating whether this instance is blackjack.
		/// </summary>
		/// <value>
		///     <c>true</c> if this instance is blackjack; otherwise, <c>false</c>.
		/// </value>
		public bool IsBlackjack => Cards.Count == 2 && ContainsAce && MaxValue == 21;

		/// <summary>
		///     Gets a value indicating whether [contains ace].
		/// </summary>
		/// <value>
		///     <c>true</c> if [contains ace]; otherwise, <c>false</c>.
		/// </value>
		public bool ContainsAce
		{
			get {
				if( Cards == null ) return false;
				return Cards.Any( c => c.Face == CardFace.Ace );
			}
		}

		/// <summary>
		///     Gets the value.
		/// </summary>
		/// <value>
		///     The value.
		/// </value>
		private int Value
		{
			get {
				if( Cards == null ) return 0;
				int value = 0;
				foreach( var card in Cards ) value = value + card.Value;

				return value;
			}
		}

		/// <summary>
		///     Converts to string.
		/// </summary>
		/// <returns>
		///     A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString() {
			string hand = "";
			foreach( var card in Cards ) {
				hand += $"{card}";
				if( Cards.Last() != card ) hand += " ";
			}

			return hand;
		}

		/// <summary>
		///     Deals the cards.
		/// </summary>
		/// <param name="shoe">The shoe.</param>
		/// <param name="numberOfCards">The number of cards.</param>
		/// <returns></returns>
		public bool DealCards( BjShoe shoe, int numberOfCards ) {
			var hand = shoe.DealCards( numberOfCards );
			if( hand == null ) return false;
			if( Cards == null ) Cards = new List<PlayingCard>();

			Cards.AddRange( hand );
			return true;
		}

		/// <summary>
		///     Doubles down.
		/// </summary>
		/// <param name="shoe">The shoe.</param>
		/// <returns></returns>
		public bool DoubleDown( BjShoe shoe ) {
			if( Cards.Count != 2 ) return false;
			if( !DealCards( shoe, 1 ) ) return false;
			Bet = Bet + Bet;
			IsDoubleDown = true;
			return true;
		}

		/// <summary>
		///     Gets the hand value string.
		/// </summary>
		/// <returns></returns>
		public string GetHandValueString() {
			int minHandValue = Value;

			if( ContainsAce )
				if( minHandValue + 10 <= 21 )
					return minHandValue + 10 == 21 ? "21" : $"{minHandValue}/{minHandValue + 10}";

			return minHandValue.ToString();
		}

		/// <summary>
		///     Hits the hand.
		/// </summary>
		/// <param name="shoe">The shoe.</param>
		/// <returns></returns>
		public bool HitHand( BjShoe shoe ) {
			return DealCards( shoe, 1 );
		}

		/// <summary>
		///     Splits the hand.
		/// </summary>
		/// <param name="shoe">The shoe.</param>
		/// <param name="player">The player.</param>
		/// <returns></returns>
		public bool SplitHand( BjShoe shoe, BjPlayer player ) {
			if( Cards.Count != 2 ) return false;

			BjHand hand = null;
			var card1 = Cards[0];
			var card2 = Cards[1];
			if( card1 != null && card2 != null
			                  && card1.Face == card2.Face )
				hand = new BjHand( Bet, card2 );

			if( hand == null ) return false;

			if( !Cards.Remove( hand.Cards.FirstOrDefault() ) || !HitHand( shoe ) || !hand.HitHand( shoe ) )
				return false;

			player.CurrentHands.Add( hand );
			return true;
		}

		/// <summary>
		///     Determines whether [is winning hand] [the specified dealer value].
		/// </summary>
		/// <param name="dealerValue">The dealer value.</param>
		/// <returns>
		///     <c>true</c> if [is winning hand] [the specified dealer value]; otherwise, <c>false</c>.
		/// </returns>
		public bool IsWinningHand( int dealerValue ) {
			bool dealerBusted = dealerValue > 21;
			return !IsBusted && !IsPush( dealerValue ) && MaxValue > dealerValue || !IsBusted && dealerBusted;
		}

		/// <summary>
		///     Determines whether the specified dealer value is push.
		/// </summary>
		/// <param name="dealerValue">The dealer value.</param>
		/// <returns>
		///     <c>true</c> if the specified dealer value is push; otherwise, <c>false</c>.
		/// </returns>
		public bool IsPush( int dealerValue ) {
			return !IsBusted && MaxValue == dealerValue;
		}

		public void DebugSetHand( int numberOfCards ) {
			for( int i = 0; i < numberOfCards; i++ ) {
				var values = Enum.GetValues( typeof(CardSuit) );
				var suit = (CardSuit)values.GetValue( Rand.GetRange( 0, values.Length ) );

				values = Enum.GetValues( typeof(CardFace) );
				var face = (CardFace)values.GetValue( Rand.GetRange( 0, values.Length ) );

				Cards.Add( new PlayingCard( suit, face ) );
			}
		}

		public void DebugSplit( int numberOfCards ) {
			Cards = new List<PlayingCard>();

			for( int i = 0; i < numberOfCards; i++ ) {
				var values = Enum.GetValues( typeof(CardSuit) );
				var suit = (CardSuit)values.GetValue( Rand.GetRange( 0, values.Length ) );

				var face = CardFace.Six;

				Cards.Add( new PlayingCard( suit, face ) );
			}
		}
	}
}