const { WebSocketServer } = require('ws');
const { v4: uuidv4 } = require('uuid');

const wss = new WebSocketServer({ port: 8080 });

const clients = new Map();
const queue = [];

function tryMatchPlayers() {
    if (queue.length < 2) {
        return; // Not enough players to start a match
    }

    console.log('Attempting to match players...');

    // Dequeue the first two players
    const player1Id = queue.shift();
    const player2Id = queue.shift();

    const player1Socket = clients.get(player1Id)?.socket;
    const player2Socket = clients.get(player2Id)?.socket;

    // Ensure both players are still connected
    if (!player1Socket || !player2Socket) {
        console.error('Error: One or both matched players disconnected before match could be formed.');
        // Re-queue the connected player, if any
        if (player1Socket) queue.unshift(player1Id);
        if (player2Socket) queue.unshift(player2Id);
        return;
    }

    const matchId = uuidv4();
    console.log(`Match found! ID: ${matchId}, Players: ${player1Id} vs ${player2Id}`);

    const match = {
        id: matchId,
        players: {
            [player1Id]: { socket: player1Socket, is_host: true, opponent: player2Id, character: null, locked: false },
            [player2Id]: { socket: player2Socket, is_host: false, opponent: player1Id, character: null, locked: false }
        }
    };

    clients.get(player1Id).match = match;
    clients.get(player2Id).match = match;

    // Player 1 will be the host
    const matchFoundPayload1 = {
        action: 'match_found',
        match_id: matchId,
        opponent_id: player2Id,
        opponent_nickname: clients.get(player2Id)?.nickname || 'Anonymous',
        is_host: true
    };

    // Player 2 will be the client
    const matchFoundPayload2 = {
        action: 'match_found',
        match_id: matchId,
        opponent_id: player1Id,
        opponent_nickname: clients.get(player1Id)?.nickname || 'Anonymous',
        is_host: false
    };

    player1Socket.send(JSON.stringify(matchFoundPayload1));
    player2Socket.send(JSON.stringify(matchFoundPayload2));
}

wss.on('connection', (ws) => {
    const clientId = uuidv4();
    clients.set(clientId, { socket: ws });
    console.log(`Client connected with ID: ${clientId}`);

    ws.on('message', (message) => {
        try {
            const data = JSON.parse(message);
            console.log(`Received message from ${clientId}:`, data);

            switch (data.action) {
                case 'join_queue':
                    if (!queue.includes(clientId)) {
                        queue.push(clientId);
                        console.log(`Client ${clientId} joined the queue. Queue size: ${queue.length}`);
                        tryMatchPlayers();
                    } else {
                        console.log(`Client ${clientId} is already in the queue.`);
                    }
                   break;
               case 'client_info':
                   if (clients.has(clientId)) {
                       clients.get(clientId).nickname = data.nickname;
                       console.log(`Client ${clientId} set nickname to ${data.nickname}`);
                   }
                   break;
               case 'leave_queue':
                   const index = queue.indexOf(clientId);
                   if (index > -1) {
                       queue.splice(index, 1);
                       console.log(`Client ${clientId} left the queue. Queue size: ${queue.length}`);
                   }
                   break;
               case 'char_select_update':
                   const clientData = clients.get(clientId);
                   if (clientData && clientData.match) {
                       const opponentId = clientData.match.players[clientId].opponent;
                       const opponentSocket = clients.get(opponentId)?.socket;
                       if (opponentSocket) {
                           const payload = {
                               action: 'opponent_selection_update',
                               selection: data.selection
                           };
                           opponentSocket.send(JSON.stringify(payload));
                       }
                   }
                   break;
               case 'leave_char_select':
                   const clientLeavingData = clients.get(clientId);
                   if (clientLeavingData && clientLeavingData.match) {
                       const opponentId = clientLeavingData.match.players[clientId].opponent;
                       const opponentSocket = clients.get(opponentId)?.socket;
                       if (opponentSocket) {
                           opponentSocket.send(JSON.stringify({ action: 'opponent_left_char_select' }));
                       }
                       delete clients.get(clientId).match;
                       delete clients.get(opponentId).match;
                   }
                   break;
               case 'char_select_confirm':
                   const clientConfirmingData = clients.get(clientId);
                   if (clientConfirmingData && clientConfirmingData.match) {
                       const match = clientConfirmingData.match;
                       match.players[clientId].locked = true;
                       match.players[clientId].character = data.character;

                       const opponentId = match.players[clientId].opponent;
                       const opponentData = match.players[opponentId];

                       if (opponentData.locked) {
                           const startGamePayload = {
                               action: 'start_game',
                               p1_char: match.players[clientId].is_host ? match.players[clientId].character : opponentData.character,
                               p2_char: match.players[clientId].is_host ? opponentData.character : match.players[clientId].character
                           };
                           match.players[clientId].socket.send(JSON.stringify(startGamePayload));
                           opponentData.socket.send(JSON.stringify(startGamePayload));
                       }
                   }
                   break;
               case 'char_select_unlock':
                   const clientUnlockingData = clients.get(clientId);
                   if (clientUnlockingData && clientUnlockingData.match) {
                       clientUnlockingData.match.players[clientId].locked = false;
                       console.log(`Client ${clientId} unlocked their character selection.`);
                   }
                   break;
              case 'move':
                  const movingClientData = clients.get(clientId);
                  if (movingClientData && movingClientData.match) {
                      const opponentId = movingClientData.match.players[clientId].opponent;
                      const opponentSocket = clients.get(opponentId)?.socket;
                      if (opponentSocket) {
                          const payload = {
                              action: 'player_moved',
                              position: data.position
                          };
                          opponentSocket.send(JSON.stringify(payload));
                      }
                  }
                  break;
              case 'shoot':
                  const shootingClientData = clients.get(clientId);
                  if (shootingClientData && shootingClientData.match) {
                      const opponentId = shootingClientData.match.players[clientId].opponent;
                      const opponentSocket = clients.get(opponentId)?.socket;
                      if (opponentSocket) {
                          const payload = {
                              action: 'shot_fired',
                              position: data.position
                          };
                          opponentSocket.send(JSON.stringify(payload));
                      }
                  }
                  break;
               default:
                   console.log(`Unknown action from ${clientId}: ${data.action}`);
           }
        } catch (error) {
            console.error(`Failed to parse message from ${clientId}:`, message, error);
        }
    });

    ws.on('close', () => {
        console.log(`Client disconnected: ${clientId}`);
        clients.delete(clientId);

        // Remove from queue if they were in it
        const queueIndex = queue.indexOf(clientId);
        if (queueIndex > -1) {
            queue.splice(queueIndex, 1);
            console.log(`Client ${clientId} removed from queue. Queue size: ${queue.length}`);
        }
    });

    ws.on('error', (error) => {
        console.error(`WebSocket error from client ${clientId}:`, error);
    });
});

console.log('WebSocket matchmaking server started on port 8080');