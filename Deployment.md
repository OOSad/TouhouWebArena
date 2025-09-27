# Server Deployment & Infrastructure Plan (Local)

This document outlines the plan for running the headless matchmaking server on a local machine for development, testing, and self-hosting.

## 1. Hosting Environment

*   **Environment:** Your local development machine (Windows, macOS, or Linux).
*   **Requirements:** Node.js and npm must be installed. You can download them from the official [Node.js website](https://nodejs.org/).

## 2. Running the Server

1.  **Install Dependencies:**
    *   Open a terminal or command prompt.
    *   Navigate into the `server` directory.
    *   Run `npm install` to install the `ws` and `uuid` packages. This only needs to be done once.

2.  **Start the Server:**
    *   While inside the `server` directory, run the following command:
        ```bash
        node server.js
        ```
    *   The server is now running. You should see a confirmation message in the console (e.g., "WebSocket server started on port 8080").
    *   To stop the server, press `Ctrl + C` in the terminal.

## 3. Networking

*   **Local Access:** Game clients running on the **same machine** can connect to the server using the address `ws://localhost:8080` (or the configured port).
*   **External Access (Port Forwarding):** As you mentioned, to allow other players over the internet to connect, you will need to:
    1.  **Configure Port Forwarding** on your router. You need to forward the server's port (e.g., 8080) to the local IP address of the machine running the server.
    2.  **Provide Your Public IP:** Players will then connect using your public IP address, like `ws://YOUR_PUBLIC_IP:8080`.
*   **Security:** Since this is a self-hosted setup, you are responsible for your own network security. For this project's scope, simply ensuring no other critical services are exposed is sufficient. Using SSL/TLS (for a `wss://` connection) is significantly more complex in a self-hosted environment and is not necessary for this stage.