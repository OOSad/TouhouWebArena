const { WebSocketServer, WebSocket } = require('../server/node_modules/ws');
const { attachSignaling } = require('../server/signaling_server.js');

const TEST_PORT = 9876;

async function runTests() {
  console.log('[TEST] Starting Signaling Password Matchmaking Test Suite...');

  // Start temporary test server
  const server = attachSignaling(TEST_PORT);
  await new Promise(r => setTimeout(r, 200));

  function createClient(nickname) {
    return new Promise((resolve, reject) => {
      const ws = new WebSocket(`ws://127.0.0.1:${TEST_PORT}`);
      const messages = [];

      ws.on('open', () => {
        ws.send(JSON.stringify({ type: 'hello', nickname }));
        resolve({
          ws,
          nickname,
          messages,
          send(obj) { ws.send(JSON.stringify(obj)); },
          waitFor(predicate, timeoutMs = 2000) {
            return new Promise((res, rej) => {
              const start = Date.now();
              const interval = setInterval(() => {
                for (const m of messages) {
                  if (predicate(m)) {
                    clearInterval(interval);
                    return res(m);
                  }
                }
                if (Date.now() - start > timeoutMs) {
                  clearInterval(interval);
                  rej(new Error(`Timeout waiting for message on ${nickname}`));
                }
              }, 20);
            });
          },
          close() { ws.close(); }
        });
      });

      ws.on('message', (data) => {
        try {
          messages.push(JSON.parse(data.toString()));
        } catch (e) {}
      });

      ws.on('error', reject);
    });
  }

  try {
    const alice = await createClient('Alice');
    const bob = await createClient('Bob');
    const cirno = await createClient('Cirno');
    const reimu = await createClient('Reimu');
    const marisa = await createClient('Marisa');

    console.log('  -> 5 test clients connected.');

    // 1. Alice sets searching with password "SecretRoom"
    alice.send({ type: 'set_searching', searching: true, password: 'SecretRoom' });

    // 2. Cirno sets searching with password "IceFairy"
    cirno.send({ type: 'set_searching', searching: true, password: 'IceFairy' });

    // Wait a brief moment - neither should match
    await new Promise(r => setTimeout(r, 150));
    let aliceMatched = alice.messages.some(m => m.type === 'match_formed');
    let cirnoMatched = cirno.messages.some(m => m.type === 'match_formed');
    if (aliceMatched || cirnoMatched) {
      throw new Error('FAIL: Alice or Cirno matched prematurely with different passwords!');
    }
    console.log('  [PASS] Alice and Cirno with different passwords do not match.');

    // 3. Bob sets searching with matching password "secretroom" (case-insensitive test)
    bob.send({ type: 'set_searching', searching: true, password: 'secretroom' });

    const aliceMatch = await alice.waitFor(m => m.type === 'match_formed');
    const bobMatch = await bob.waitFor(m => m.type === 'match_formed');

    if (aliceMatch.opponentName !== 'Bob' && bobMatch.opponentName !== 'Alice') {
      throw new Error(`FAIL: Unexpected match pair: Alice vs ${aliceMatch.opponentName}, Bob vs ${bobMatch.opponentName}`);
    }
    console.log('  [PASS] Alice and Bob paired successfully using matching password ("SecretRoom" / "secretroom")!');

    // Verify Cirno is still waiting and unmatched
    cirnoMatched = cirno.messages.some(m => m.type === 'match_formed');
    if (cirnoMatched) {
      throw new Error('FAIL: Cirno matched unexpectedly!');
    }
    console.log('  [PASS] Cirno remains unmatched in her private room.');

    // Verify lobby_list sent to Cirno marks Alice and Bob as in_match: true and room_has_password: true
    const cirnoLobby = cirno.messages.filter(m => m.type === 'lobby_list').pop();
    if (!cirnoLobby) {
      throw new Error('FAIL: Cirno did not receive lobby_list broadcast after match formed!');
    }
    const aliceEntry = cirnoLobby.players.find(p => p.name === 'Alice');
    const bobEntry = cirnoLobby.players.find(p => p.name === 'Bob');
    if (!aliceEntry || !aliceEntry.in_match || !aliceEntry.room_has_password) {
      throw new Error(`FAIL: Alice in lobby_list not marked in_match: ${JSON.stringify(aliceEntry)}`);
    }
    if (!bobEntry || !bobEntry.in_match || !bobEntry.room_has_password) {
      throw new Error(`FAIL: Bob in lobby_list not marked in_match: ${JSON.stringify(bobEntry)}`);
    }
    console.log('  [PASS] Active matched players correctly broadcasted as in_match with room password protection.');

    // 4. Test public queue matching (empty password)
    reimu.send({ type: 'set_searching', searching: true, password: '' });
    await new Promise(r => setTimeout(r, 100));

    // Reimu should not match Cirno
    let reimuMatched = reimu.messages.some(m => m.type === 'match_formed');
    if (reimuMatched) {
      throw new Error('FAIL: Reimu in public queue matched with Cirno in private room!');
    }
    console.log('  [PASS] Public queue player does not match private room player.');

    // Marisa joins public queue
    marisa.send({ type: 'set_searching', searching: true, password: '   ' }); // whitespace treated as empty
    const reimuMatch = await reimu.waitFor(m => m.type === 'match_formed');
    const marisaMatch = await marisa.waitFor(m => m.type === 'match_formed');

    if (reimuMatch.opponentName !== 'Marisa' && marisaMatch.opponentName !== 'Reimu') {
      throw new Error(`FAIL: Unexpected public match pair: Reimu vs ${reimuMatch.opponentName}, Marisa vs ${marisaMatch.opponentName}`);
    }
    console.log('  [PASS] Reimu and Marisa matched successfully in the public queue!');

    // 5. Test Bot Match status broadcast
    cirno.send({ type: 'set_bot_match', in_bot_match: true });
    const botLobby = await cirno.waitFor(m => {
      if (m.type !== 'lobby_list') return false;
      const cEntry = m.players.find(p => p.name === 'Cirno');
      return cEntry && cEntry.in_bot_match === true;
    });
    const cirnoBotEntry = botLobby.players.find(p => p.name === 'Cirno');
    if (!cirnoBotEntry || !cirnoBotEntry.in_bot_match || cirnoBotEntry.is_searching) {
      throw new Error(`FAIL: Cirno not properly marked in_bot_match: ${JSON.stringify(cirnoBotEntry)}`);
    }
    console.log('  [PASS] Bot match status correctly broadcasted in lobby_list (in_bot_match: true).');

    cirno.send({ type: 'set_bot_match', in_bot_match: false });
    const clearedLobby = await cirno.waitFor(m => {
      if (m.type !== 'lobby_list') return false;
      const cEntry = m.players.find(p => p.name === 'Cirno');
      return cEntry && cEntry.in_bot_match === false;
    });
    console.log('  [PASS] Bot match status cleared successfully when returning to browse/lobby.');

    // 6. Test Spectator Mode
    // Check that Reimu's public match in room_4_5 is listed with can_spectate and room_id
    const lobbyWithMatch = await cirno.waitFor(m => {
      if (m.type !== 'lobby_list') return false;
      const rEntry = m.players.find(p => p.name === 'Reimu');
      return rEntry && rEntry.can_spectate === true && rEntry.room_id === 'room_4_5';
    });
    console.log('  [PASS] Active match correctly broadcasted with room_id and can_spectate: true.');

    // Cirno joins as spectator to room_4_5
    cirno.send({ type: 'spectate_room', roomId: 'room_4_5' });
    const specJoined = await cirno.waitFor(m => m.type === 'spectate_joined');
    const hostNotified = await reimu.waitFor(m => m.type === 'spectator_joined');

    if (specJoined.roomId !== 'room_4_5' || specJoined.p1Name !== 'Reimu' || specJoined.p2Name !== 'Marisa') {
      throw new Error(`FAIL: Unexpected spectate_joined data: ${JSON.stringify(specJoined)}`);
    }
    if (hostNotified.spectatorCount !== 1) {
      throw new Error(`FAIL: Host spectator count mismatch: ${hostNotified.spectatorCount}`);
    }
    console.log('  [PASS] Spectator joined room successfully; host received notification.');

    // Host sends spectator_broadcast event
    reimu.send({
      type: 'spectator_broadcast',
      event: {
        type: 'match_init',
        p1_char: 'reimu',
        p2_char: 'marisa',
        stage_id: 'bamboo_road',
        p1_wins: 1,
        p2_wins: 0,
        current_round: 2,
        is_round_active: true
      }
    });
    const specInitEvent = await cirno.waitFor(m => m.type === 'spectator_event' && m.event && m.event.type === 'match_init');
    if (specInitEvent.event.p1_char !== 'reimu' || specInitEvent.event.stage_id !== 'bamboo_road' || specInitEvent.event.is_round_active !== true) {
      throw new Error(`FAIL: Spectator received invalid match_init: ${JSON.stringify(specInitEvent)}`);
    }
    console.log('  [PASS] Spectator received broadcast match_init event cleanly from host.');

    // Spectator leaves
    cirno.send({ type: 'leave_spectate' });
    const hostLeftNotified = await reimu.waitFor(m => m.type === 'spectator_left');
    if (hostLeftNotified.spectatorCount !== 0) {
      throw new Error(`FAIL: Expected 0 spectators remaining: ${hostLeftNotified.spectatorCount}`);
    }
    console.log('  [PASS] Spectator cleanly left room; host notified.');

    // A second spectator joins (Alice), verify that Alice immediately receives the cached matchState in spectate_joined!
    alice.send({ type: 'spectate_room', roomId: 'room_4_5' });
    const aliceJoined = await alice.waitFor(m => m.type === 'spectate_joined');
    if (!aliceJoined.matchState || aliceJoined.matchState.p1_char !== 'reimu' || aliceJoined.matchState.stage_id !== 'bamboo_road' || aliceJoined.matchState.current_round !== 2) {
      throw new Error(`FAIL: Second spectator did not receive cached matchState: ${JSON.stringify(aliceJoined)}`);
    }
    console.log('  [PASS] Subsequent spectator immediately received cached matchState on join.');

    alice.send({ type: 'leave_spectate' });
    await reimu.waitFor(m => m.type === 'spectator_left');

    // Close all clients
    alice.close();
    bob.close();
    cirno.close();
    reimu.close();
    marisa.close();

    server.close();
    console.log('\n==============================================');
    console.log(' ALL SIGNALING PASSWORD, BOT & SPECTATOR TESTS PASSED!');
    console.log('==============================================\n');
    process.exit(0);
  } catch (err) {
    console.error('TEST FAILED:', err);
    server.close();
    process.exit(1);
  }
}

runTests();
