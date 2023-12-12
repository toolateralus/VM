A tiny test of an online game.

The game does basically nothing right now, it just bounces cube projectiles back and forth over the network.

## Usage

**Solo**:
- Run the game.
- Set the 'Opponent' and 'YourChannel' fields to the same channel number, for example, 0.
- Press W or S to shoot. Only one bullet is allowed at a time.

**Two Player**:

#### *On one computer*:
- Open two instances of the app, set ones output channel to the other's input channel, and vice versa then fire.
- This is bugged. It freezes when two clients make a shot at the same time, so you have to send the shot before starting the second client. 
- With some trickery, it is possible.
- To do said trickery, open one app. set the channels to `1` and `0`
- Quickly, fire a shot. before that shot reaches the border of the app, start another instance. the default channels are `0` and `1` so it will recieve this one shot.
#### *On two computers*:
- **User 0 (Host)**:
    - Open a terminal, run the 'host' command, 'ip' command, then the 'connect <ip>' command, to connect to your own server.
    - Open the game, set the 'Opponent' and 'YourChannel' fields to the appropriate values. For this example, we will use 0 and 1 on this end.
- **User 1 (Client)**:
    - Open a terminal, then the 'connect <ip>' command, to the host server. You will need to know this IP ahead of time. (IPV4)
    - Open the game, set the 'Opponent' and 'YourChannel' fields to the appropriate values. For this example, we will use 1 and 0 on this end.
    - Either of the players can now shoot while no cubes are already present in their scene.




If successful, the bullet will bounce up and down off the bounds of the screen, changing colors. Each time a node bounces, it would have been sent over the network to the other player.

This is currently very unreliable but a big start to the network system. Many bugs & crashes are expected, and no security is in place.
