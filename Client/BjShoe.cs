using System;
using System.Collections.Generic;
using CitizenFX.Core;
using Common;
using Roleplay.SharedClasses;

namespace Roleplay.Client.Classes.Institutions.GamblingGames.Blackjack
{
	public class BjShoe
	{
		public Stack<PlayingCard> ShoeCards;

		public BjShoe( int numberOfDecks ) {
			LoadNewShoe( numberOfDecks );
		}

		/// <summary>
		///     Loads the new shoe.
		/// </summary>
		/// <param name="numberOfDecks">The number of decks.</param>
		public void LoadNewShoe( int numberOfDecks ) {
			numberOfDecks = MathUtil.Clamp( numberOfDecks, 2, 8 );

			var tempShoe = new List<PlayingCard>();
			for( int i = 0; i < numberOfDecks; i++ )
				foreach( CardSuit suit in Enum.GetValues( typeof(CardSuit) ) )
				foreach( CardFace face in Enum.GetValues( typeof(CardFace) ) )
					tempShoe.Add( new PlayingCard( suit, face ) );
			tempShoe.Shuffle();

			ShoeCards = new Stack<PlayingCard>( tempShoe );
		}

		/// <summary>
		///     Deals the cards.
		/// </summary>
		/// <param name="numberOfCards">The number of cards.</param>
		/// <returns></returns>
		public List<PlayingCard> DealCards( int numberOfCards ) {
			if( ShoeCards.Count < numberOfCards ) {
				BaseScript.TriggerEvent( "Chat.Message", "[DealCards]", StandardColours.InfoHEX,
					$"Only {ShoeCards.Count} remain in the shoe, cannot deal {numberOfCards}. Shuffle the shoe first." );
				return null;
			}

			var cards = new List<PlayingCard>();
			for( int i = 0; i < numberOfCards; i++ ) cards.Add( ShoeCards.Pop() );

			return cards;
		}

		/// <summary>
		///     Shoes the has cards to complete action.
		/// </summary>
		/// <param name="action">The action.</param>
		/// <returns></returns>
		public bool ShoeHasCardsToCompleteAction( BjActionsEnum action ) {
			switch( action ) {
			case BjActionsEnum.Hit:
				return ShoeCards.Count >= 1;
			case BjActionsEnum.Stand:
				return true;
			case BjActionsEnum.DoubleDown:
				return ShoeCards.Count >= 1;
			case BjActionsEnum.Split:
				return ShoeCards.Count >= 2;
			}

			return false;
		}
	}
}