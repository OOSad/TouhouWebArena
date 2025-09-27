# Headless Matchmaking Server - Technical Specification

## 1. Project Setup

*   **Language:** JavaScript (Node.js)
*   **Package Manager:** npm
*   **Main File:** `server.js`
*   **Dependencies:**
    *   `ws`: For WebSocket server functionality.
    *   `uuid`: For generating unique match IDs.

### 1.1. Directory Structure

A new directory named `server` will be created in the project root to house all server-side code and dependencies.

```
/
|-- server/
|   |-- node_modules/
|   |-- package.json
|   |-- package-lock.json
|   `-- server.js
|-- MainMenu/
|-- Queue/
`-- project.godot
...
```

## 2. Implementation Steps

### 2.1. Initialization

1.  Create the `server` directory.
2.  Inside the `server` directory, run `npm init -y` to create a `package.json` file.
3.  Install dependencies by running `npm install ws uuid`.

### 2.2. `server.js` Implementation

The `server.js` file will contain all the logic.

1.  **Import Dependencies:** Import the `WebSocketServer` from `ws` and `v4` as `uuidv4` from `uuid`.
2.  **Server Instantiation:** Create a new `WebSocketServer` instance on a specified port (e.g., 8080).
3.  **Data Structures:**
    *   `clients`: A `Map` to store connected clients, mapping a unique ID to the WebSocket object.
    *   `queue`: An `Array` to store the IDs of clients waiting for a match.
4.  **Connection Handling:**
    *   Set up a `connection` event listener on the WebSocket server.
    *   When a client connects:
        *   Generate a unique ID for the client using `uuidv4()`.
        *   Store the client's WebSocket object in the `clients` map with its ID.
        *   Log the new connection.
        *   Set up `message` and `close` event listeners for this specific client.
5.  **Message Handling:**
    *   When a `message` is received:
        *   Parse the incoming JSON message.
        *   Use a `switch` statement on the `action` property of the message.
        *   **Case `'join_queue'`:**
            *   Add the client's ID to the `queue`.
            *   Log that the client has joined the queue.
            *   Call the `tryMatchPlayers()` function.
6.  **`tryMatchPlayers()` Function:**
    *   This function checks if there are two or more players in the `queue`.
    *   If so, it dequeues the first two players.
    *   It generates a new `match_id`.
    *   It constructs the `match_found` message payload for each player.
    *   It sends the message to both clients via their WebSocket objects (retrieved from the `clients` map).
    *   It logs the successful match.
7.  **Disconnection Handling:**
    *   When a `close` event occurs:
        *   Remove the client from the `clients` map.
        *   **Crucially, remove the client from the `queue` if they were in it.** This prevents a disconnected player from blocking a match.
        *   Log the disconnection.
8.  **Server Start:** Log a message indicating the server has started and is listening on its port.