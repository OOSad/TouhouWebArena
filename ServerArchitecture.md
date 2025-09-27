# Headless Server Architecture & Matchmaking Logic

## 1. Core Technology

*   **Runtime:** Node.js
*   **Communication Protocol:** WebSockets (`ws` library)
*   **Architecture:** A single, lightweight, event-driven process. No complex room management is needed; the server will manage a global list of connections and a simple queue.

## 2. Server Responsibilities

The server's role is minimal, aligning with the client-authoritative model. Its only responsibilities are:
1.  **Accepting WebSocket connections** from game clients.
2.  **Managing a player queue:** Adding players who wish to find a match.
3.  **Pairing players:** Identifying two players in the queue and matching them together.
4.  **Notifying players:** Informing the paired players that a match has been found, and providing necessary opponent details for them to establish a direct peer-to-peer (P2P) connection.

The server **WILL NOT** be involved in:
*   Game state synchronization.
*   In-game physics or logic.
*   Relaying in-match data between players.
*   Result validation or storage.

## 3. Matchmaking Flow

This logic ensures that multiple pairs of players can be matched concurrently without interfering with each other.

1.  **Client Connects:** A player's game client establishes a WebSocket connection to the server. The server assigns a unique ID to this connection.
2.  **Client Joins Queue:** The client sends a `join_queue` message.
3.  **Server Logic:**
    *   The server receives the `join_queue` message and adds the player's unique ID to a `waiting_players` queue (e.g., a simple array).
    *   The server immediately checks if there are two or more players in the `waiting_players` queue.
    *   **If >= 2 players:** The server takes the first two players from the queue.
    *   It generates a unique `match_id` (e.g., using a UUID library).
    *   It creates a "match found" payload containing the `match_id` and the details of both players.
    *   It sends this payload to both matched players.
    *   The server's job for these two players is now complete.
4.  **Client-Side:** Upon receiving the "match found" message, the clients will use the provided information to establish their own P2P connection (e.g., using Godot's high-level multiplayer API with WebRTC).

## 4. Diagram

```mermaid
sequenceDiagram
    participant P1_Client as Player 1 Client
    participant Server
    participant P2_Client as Player 2 Client

    P1_Client->>Server: Establishes WebSocket Connection
    Server-->>P1_Client: Connection Opened [ID: player123]
    P1_Client->>Server: Sends { "action": "join_queue" }
    Server->>Server: Adds "player123" to queue. Queue: ["player123"]

    P2_Client->>Server: Establishes WebSocket Connection
    Server-->>P2_Client: Connection Opened [ID: player456]
    P2_Client->>Server: Sends { "action": "join_queue" }
    Server->>Server: Adds "player456" to queue. Queue: ["player123", "player456"]
    Server->>Server: Found a pair! Creates Match ID: "match-xyz"
    
    Server-->>P1_Client: Sends { "action": "match_found", "match_id": "match-xyz", "opponent_id": "player456" }
    Server-->>P2_Client: Sends { "action": "match_found", "match_id": "match-xyz", "opponent_id": "player123" }

    P1_Client-X->>P2_Client: P2P Connection (WebRTC)
    P2_Client-X->>P1_Client: P2P Connection (WebRTC)