# WebSocket Communication Protocol

All communication between the Godot client and the Node.js server will happen via JSON-formatted messages. Each message must have an `action` property that defines its purpose.

## 1. Client-to-Server Messages

### 1.1. `join_queue`
*   **Action:** `join_queue`
*   **Description:** Sent by the client when the player wants to enter the matchmaking queue.
*   **Payload:** None
*   **Example:**
    ```json
    {
      "action": "join_queue"
    }
    ```

### 1.2. `client_info`
*   **Action:** `client_info`
*   **Description:** Sent by the client to provide the server with information about the client, such as their nickname.
*   **Payload:**
   *   `nickname` (string): The client's nickname.
*   **Example:**
   ```json
   {
     "action": "client_info",
     "nickname": "Reimu"
   }
   ```

### 1.3. `leave_queue`
*   **Action:** `leave_queue`
*   **Description:** Sent by the client when the player wants to leave the matchmaking queue.
*   **Payload:** None
*   **Example:**
   ```json
   {
     "action": "leave_queue"
   }
   ```

## 2. Server-to-Client Messages

### 2.1. `match_found`
*   **Action:** `match_found`
*   **Description:** Sent by the server to two clients when they have been successfully paired.
*   **Payload:**
    *   `match_id` (string): A unique identifier for the match.
   *   `opponent_id` (string): The unique ID of the opponent.
   *   `opponent_nickname` (string): The nickname of the opponent.
   *   `is_host` (boolean): A boolean flag to determine which of the two players will act as the host for the P2P connection. This simplifies the P2P connection logic, as one player creates the game and the other joins.
*   **Example (sent to the player who will be host):**
    ```json
    {
     "action": "match_found",
     "match_id": "a1b2c3d4-e5f6-7890-g1h2-i3j4k5l6m7n8",
     "opponent_id": "z9y8x7w6-v5u4-t3s2-r1q0-p9o8n7m6l5k4",
     "opponent_nickname": "Marisa",
     "is_host": true
   }
   ```
*   **Example (sent to the player who will join):**
   ```json
   {
     "action": "match_found",
     "match_id": "a1b2c3d4-e5f6-7890-g1h2-i3j4k5l6m7n8",
     "opponent_id": "k4l5m6n7-o8p9-q0r1-s2t3-u4v5w6x7y8z9",
     "opponent_nickname": "Reimu",
     "is_host": false
   }