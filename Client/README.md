# Blackjack
- Going too far from a game after you have joined/started it will force you to quit.
- Commands: */bj create|start|find|join|bet|chips|insure|hud|list|kick|end*
  - **create** - create a new blackjack game at your current location
    - Usage: */bj create [dealer name] [minimum bet] [number of decks] [blackjack bonus] e.g. /bj create Charlie Chips 10 6 1.5*
  - **start** - start a created game
    - Usage: */bj start*
  - **find**  - find nearby blackjack games
    - Usage: */bj find*
  - **join** - join a blackjack game as a player
    - Usage: */bj join [game id] [player name] e.g. /bj join 1 Pistol Pete*
  - **bet** - set your current bet.  Note: remains the same until you change it.  Need to bet 0 to sit out.
    - Usage: */bj bet [bet amount] e.g. /bj bet 60*
  - **chips** - set a players chip amount as dealer
    - Usage: */bj chips [player position] [chip amount] e.g. /bj chips 1 5000*
  - **insure** - buy insurance when offered
    - Usage: */bj insure 100*
  - **hud** - toggle a hud
    - Usage: */bj hud info|controls|players|hands*
      - info - *wager window* (default: on)
      - controls - *instructions in lower right corner* (default: on)
      - players - *other players hands* (default: on)
      - hands - *your additional hands* (default: off)
  - **list** - list current players in your game
    - Usage: */bj list*
  - **kick** - kick a player from the table
    - Usage: */bj kick [player position] e.g. /bj kick 3*
  - **end** - end your current game
    - Usage: */bj end*
    
## Player
- **find** and **join** a game.  Make sure to get chips from the dealer and set your bet before the round starts.
- When it is your turn you will be able to signal the dealer with the listed controls if you can't verbally tell them your action
- Insurance pays 2:1 and is limited to 1/2 your original bet.  

## Dealer
- **create** and **start** a game
- Give **chips** to the active players to that they can place their bets before the round starts
- Follow on-screen instructions using the controls
 
