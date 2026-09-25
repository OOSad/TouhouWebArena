const { WebSocketServer } = require('ws');

let nextClientId = 1;
const clients = new Map(); // clientId -> { id, ws, nickname, searching, roomId, peerId, isAlive }
const rooms = new Map();   // roomId -> { hostId, clientId }

function setupClientConnection(ws) {
  const clientId = nextClientId++;
  const clientData = {
    id: clientId,
    ws: ws,
    nickname: 'Anonymous Fairy',
    searching: false,
    password: '',
    inBotMatch: false,
    roomId: null,
    spectatingRoomId: null,
    peerId: 0,
    isAlive: true
  };
  clients.set(clientId, clientData);
  console.log(`[Signaling] Client #${clientId} connected. Total clients: ${clients.size}`);

  ws.on('pong', () => {
    clientData.isAlive = true;
  });

  ws.on('message', (messageText) => {
    try {
      const msg = JSON.parse(messageText.toString());
      handleMessage(clientData, msg);
    } catch (err) {
      console.error(`[Signaling] Invalid JSON from Client #${clientId}:`, err.message);
    }
  });

  ws.on('close', () => {
    handleDisconnect(clientData);
  });

  ws.on('error', (err) => {
    console.error(`[Signaling] Error on Client #${clientId}:`, err.message);
  });
}

function attachSignaling(target) {
  let wss;
  if (typeof target === 'number' || typeof target === 'string') {
    const port = Number(target);
    wss = new WebSocketServer({ port });
    console.log(`[Signaling] WebSocket listener active on port ${port}`);
  } else if (target && typeof target === 'object') {
    wss = new WebSocketServer({ server: target });
    console.log(`[Signaling] WebSocket attached to HTTP server.`);
  }

  if (wss) {
    wss.on('connection', setupClientConnection);
    wss.on('error', (err) => {
      if (err.code === 'EADDRINUSE') {
        console.warn(`[Signaling] Notice: Port is already in use by another instance.`);
      } else {
        console.error(`[Signaling] Server error:`, err.message);
      }
    });
  }
  return wss;
}

function handleMessage(client, msg) {
  switch (msg.type) {
    case 'hello':
      client.nickname = (msg.nickname || 'Anonymous Fairy').trim() || 'Anonymous Fairy';
      if (client.nickname.endsWith('::bot')) {
        client.inBotMatch = true;
      }
      console.log(`[Signaling] Client #${client.id} identified as "${client.nickname}"`);
      // Send welcome acknowledging client id
      sendTo(client, { type: 'welcome', clientId: client.id });
      broadcastPlayerList();
      break;

    case 'update_nickname':
      client.nickname = (msg.nickname || 'Anonymous Fairy').trim() || 'Anonymous Fairy';
      if (client.nickname.endsWith('::bot')) {
        client.inBotMatch = true;
      } else if (!msg.in_bot_match && client.inBotMatch && !client.nickname.endsWith('::bot')) {
        client.inBotMatch = false;
      }
      broadcastPlayerList();
      break;

    case 'set_searching':
      client.searching = Boolean(msg.searching);
      client.password = client.searching ? ((msg.password || '').trim()) : '';
      if (client.searching) {
        client.inBotMatch = false;
      }
      console.log(`[Signaling] Client #${client.id} (${client.nickname}) searching: ${client.searching}${client.password ? ' [Password: ' + client.password + ']' : ' [Public]'}`);
      broadcastPlayerList();

      if (client.searching && !client.roomId) {
        checkMatchmaking();
      }
      break;

    case 'set_bot_match':
      client.inBotMatch = Boolean(msg.in_bot_match);
      if (client.inBotMatch) {
        client.searching = false;
        client.password = '';
      }
      console.log(`[Signaling] Client #${client.id} (${client.nickname}) in_bot_match: ${client.inBotMatch}`);
      broadcastPlayerList();
      break;

    case 'signal':
      const sigType = (msg.data && msg.data.type) || 'unknown';
      if (!client.roomId) {
        console.warn(`[Signaling] Client #${client.id} (${client.nickname}) sent signal '${sigType}' without active room.`);
        return;
      }
      const room = rooms.get(client.roomId);
      if (!room) {
        console.warn(`[Signaling] Room ${client.roomId} not found for Client #${client.id}`);
        return;
      }

      const targetId = (room.hostId === client.id) ? room.clientId : room.hostId;
      const targetClient = clients.get(targetId);
      if (targetClient && targetClient.ws.readyState === 1) {
        console.log(`[Signaling] Forwarding '${sigType}' from Client #${client.id} (${client.nickname}) -> Client #${targetId} (${targetClient.nickname})`);
        sendTo(targetClient, {
          type: 'signal',
          data: msg.data
        });
      } else {
        console.warn(`[Signaling] Target Client #${targetId} unavailable to receive signal '${sigType}'`);
      }
      break;

    case 'leave_match':
      cleanupRoom(client);
      cleanupSpectator(client);
      client.searching = false;
      client.password = '';
      client.inBotMatch = false;
      broadcastPlayerList();
      break;

    case 'spectate_room': {
      const reqRoomId = msg.roomId;
      const targetRoom = rooms.get(reqRoomId);
      if (!targetRoom) {
        sendTo(client, { type: 'spectate_failed', reason: 'Match not found or already ended.' });
        return;
      }
      if (targetRoom.password && targetRoom.password !== (msg.password || '').trim().toLowerCase()) {
        sendTo(client, { type: 'spectate_failed', reason: 'Incorrect room password.' });
        return;
      }
      cleanupSpectator(client);
      if (!targetRoom.spectators) targetRoom.spectators = new Set();
      targetRoom.spectators.add(client.id);
      client.spectatingRoomId = reqRoomId;
      client.searching = false;

      const hostClient = clients.get(targetRoom.hostId);
      const opponentClient = clients.get(targetRoom.clientId);
      const p1Name = hostClient ? hostClient.nickname : 'Player 1';
      const p2Name = opponentClient ? opponentClient.nickname : 'Player 2';

      console.log(`[Signaling] Client #${client.id} (${client.nickname}) started spectating room ${reqRoomId} (${p1Name} vs ${p2Name})`);

      sendTo(client, {
        type: 'spectate_joined',
        roomId: reqRoomId,
        p1Name: p1Name,
        p2Name: p2Name,
        spectatorCount: targetRoom.spectators.size,
        matchState: targetRoom.matchState || null,
        matchSeed: targetRoom.matchSeed || 99991
      });

      if (hostClient && hostClient.ws.readyState === 1) {
        sendTo(hostClient, {
          type: 'spectator_joined',
          spectatorId: client.id,
          spectatorCount: targetRoom.spectators.size
        });
      }
      broadcastPlayerList();
      break;
    }

    case 'leave_spectate':
      cleanupSpectator(client);
      broadcastPlayerList();
      break;

    case 'spectator_broadcast': {
      if (!client.roomId) return;
      const r = rooms.get(client.roomId);
      if (r) {
        if (msg.event && typeof msg.event === 'object') {
          if (msg.event.type === 'match_init') {
            if (msg.event.match_seed) {
              r.matchSeed = msg.event.match_seed;
            }
            r.matchState = {
              p1_char: msg.event.p1_char || 'youmu',
              p2_char: msg.event.p2_char || 'marisa',
              stage_id: msg.event.stage_id || 'bamboo_road',
              p1_wins: msg.event.p1_wins || 0,
              p2_wins: msg.event.p2_wins || 0,
              current_round: msg.event.current_round || 1,
              is_round_active: Boolean(msg.event.is_round_active)
            };
          } else if (msg.event.type === 'round_transition') {
            if (!r.matchState) r.matchState = {};
            r.matchState.p1_wins = msg.event.p1_wins != null ? msg.event.p1_wins : 0;
            r.matchState.p2_wins = msg.event.p2_wins != null ? msg.event.p2_wins : 0;
            r.matchState.current_round = msg.event.next_round || 1;
            r.matchState.is_round_active = false;
          } else if (msg.event.type === 'round_started') {
            if (!r.matchState) r.matchState = {};
            r.matchState.is_round_active = true;
            if (msg.event.current_round) {
              r.matchState.current_round = msg.event.current_round;
            }
          }
        }

        if (r.spectators && r.spectators.size > 0) {
          const payload = {
            type: 'spectator_event',
            event: msg.event
          };
          for (const specId of r.spectators) {
            const specClient = clients.get(specId);
            if (specClient && specClient.ws.readyState === 1) {
              sendTo(specClient, payload);
            }
          }
        }
      }
      break;
    }

    default:
      console.warn(`[Signaling] Unknown message type: ${msg.type}`);
  }
}

function checkMatchmaking() {
  const groups = new Map();

  for (const c of clients.values()) {
    if (c.searching && !c.roomId && c.ws.readyState === 1) {
      const key = (c.password || '').trim().toLowerCase();
      if (!groups.has(key)) {
        groups.set(key, []);
      }
      groups.get(key).push(c);
    }
  }

  let matchMade = false;

  for (const [key, pool] of groups.entries()) {
    while (pool.length >= 2) {
      const host = pool.shift();
      const client = pool.shift();

      const roomId = `room_${host.id}_${client.id}`;
      host.roomId = roomId;
      host.peerId = 1; // Host is Peer 1 in Godot multiplayer mesh
      host.searching = false;

      client.roomId = roomId;
      client.peerId = 2; // Client is Peer 2 in Godot multiplayer mesh
      client.searching = false;

      const matchSeed = Math.floor(Math.random() * 1000000) + 1000;
      rooms.set(roomId, { hostId: host.id, clientId: client.id, password: key, spectators: new Set(), matchState: null, matchSeed: matchSeed });

      console.log(`[Signaling] Match created in ${roomId} (${key ? 'Room: ' + key : 'Public'}): ${host.nickname} (Host/P1) vs ${client.nickname} (Client/P2) [Seed: ${matchSeed}]`);

      // Notify Host
      sendTo(host, {
        type: 'match_formed',
        role: 'host',
        myPeerId: 1,
        remotePeerId: 2,
        opponentName: client.nickname,
        matchSeed: matchSeed
      });

      // Notify Client
      sendTo(client, {
        type: 'match_formed',
        role: 'client',
        myPeerId: 2,
        remotePeerId: 1,
        opponentName: host.nickname,
        matchSeed: matchSeed
      });

      matchMade = true;
    }
  }

  if (matchMade) {
    broadcastPlayerList();
  }
}

function cleanupRoom(client) {
  if (!client.roomId) return;
  const room = rooms.get(client.roomId);
  if (room) {
    const targetId = (room.hostId === client.id) ? room.clientId : room.hostId;
    const opponent = clients.get(targetId);
    if (opponent) {
      opponent.roomId = null;
      opponent.peerId = 0;
      opponent.password = '';
      sendTo(opponent, { type: 'opponent_left' });
    }
    if (room.spectators && room.spectators.size > 0) {
      for (const specId of room.spectators) {
        const spec = clients.get(specId);
        if (spec) {
          spec.spectatingRoomId = null;
          sendTo(spec, { type: 'spectate_match_ended' });
        }
      }
      room.spectators.clear();
    }
    rooms.delete(client.roomId);
  }
  client.roomId = null;
  client.peerId = 0;
  client.password = '';
}

function cleanupSpectator(client) {
  if (!client.spectatingRoomId) return;
  const room = rooms.get(client.spectatingRoomId);
  if (room && room.spectators) {
    room.spectators.delete(client.id);
    const hostClient = clients.get(room.hostId);
    if (hostClient && hostClient.ws.readyState === 1) {
      sendTo(hostClient, {
        type: 'spectator_left',
        spectatorId: client.id,
        spectatorCount: room.spectators.size
      });
    }
  }
  client.spectatingRoomId = null;
}

function handleDisconnect(client) {
  console.log(`[Signaling] Client #${client.id} (${client.nickname}) disconnected.`);
  cleanupRoom(client);
  cleanupSpectator(client);
  clients.delete(client.id);
  broadcastPlayerList();
}

function broadcastPlayerList() {
  // Send current queue/browse state to all connected clients
  for (const recipient of clients.values()) {
    if (recipient.ws.readyState !== 1) continue;

    const recipientPw = (recipient.password || '').trim().toLowerCase();

    const list = [];
    for (const c of clients.values()) {
      const clientPw = (c.password || '').trim().toLowerCase();
      const hasPassword = Boolean(clientPw);
      const isLocal = (c.id === recipient.id);

      // Matches password if both are searching with the same non-empty password,
      // or if both are searching in public (empty password)
      const matchesPassword = (c.searching && recipient.searching && (
        (hasPassword && clientPw === recipientPw) ||
        (!hasPassword && !recipientPw)
      ));

      const inMatch = Boolean(c.roomId);
      let roomHasPassword = false;
      let roomMatchesPassword = false;
      let spectatorCount = 0;
      let roomId = null;
      let canSpectate = false;
      if (inMatch) {
        const room = rooms.get(c.roomId);
        const roomPw = (room && room.password) ? room.password : clientPw;
        roomHasPassword = Boolean(roomPw);
        roomMatchesPassword = roomHasPassword && (roomPw === recipientPw);
        if (room) {
          spectatorCount = room.spectators ? room.spectators.size : 0;
          canSpectate = !roomHasPassword || roomMatchesPassword;
          if (canSpectate) {
            roomId = c.roomId;
          }
        }
      }

      const isBotMatch = Boolean(c.inBotMatch) || (typeof c.nickname === 'string' && c.nickname.endsWith('::bot'));
      list.push({
        name: c.nickname,
        is_searching: c.searching,
        in_match: inMatch,
        in_bot_match: isBotMatch,
        room_has_password: roomHasPassword,
        room_matches_password: roomMatchesPassword,
        is_local: isLocal,
        has_password: hasPassword,
        matches_password: matchesPassword,
        room_id: roomId,
        spectator_count: spectatorCount,
        can_spectate: canSpectate
      });
    }

    sendTo(recipient, {
      type: 'lobby_list',
      players: list
    });
  }
}

function sendTo(client, payload) {
  if (client.ws && client.ws.readyState === 1) {
    client.ws.send(JSON.stringify(payload));
  }
}

// Keep-alive heartbeat ping every 25s
const heartbeatInterval = setInterval(() => {
  for (const client of clients.values()) {
    if (!client.isAlive) {
      console.log(`[Signaling] Terminating dead connection: Client #${client.id}`);
      client.ws.terminate();
      continue;
    }
    client.isAlive = false;
    client.ws.ping();
  }
}, 25000);

if (require.main === module) {
  const PORT = process.env.PORT || 8910;
  console.log(`[Signaling] Touhou Web Arena Signaling Server running on port ${PORT}`);
  attachSignaling(PORT);
}

module.exports = {
  attachSignaling,
  clients,
  rooms
};

