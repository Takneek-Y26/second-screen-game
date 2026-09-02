# Operation Midsem — Takneek Zenith 

## 📌 1. Overview & Team Information
- **Team Name:** Pool Peshwas
- **Game Name:** Operation Midsem
- **Supported Devices:** 1 Primary PC + 2-3 Smartphones
- **Communication Protocol:** HTTP, UDP

### Team Members
- Karan Sogar - 250507 (Y25)
- Ashutosh Choudhary - 250226 (Y25)
- Yash Sinha - 251205 (Y25)
- Nikhil - 250714 (Y25)
- Tarun Kumar - 265030 (Y26)
- Neilan Ghosh - 265023 (Y26)
- Geddam Karthikeya Ram Rishit - 260405 (Y26)

---

## 🛠️ 2. System & Hardware Requirements
* **Primary Device (PC/Laptop):** Windows/Linux/Mac
* **Smartphone(s):** Latest Android/iOS version, sensor permissions required (Camera, Gyroscope, Mic, etc.).
* **Network:** Local Wi-Fi network enabled.

---

## 🚀 3. How to Build & Run the Game

### Step 1: Primary Game Application
1. Clone the repository using `git clone`.
2. Build executable or run via Unity.
3. Open Command Prompt, and use the `ipconfig` command to find the IPv4 Address. 
4. Update the IPv4 Address in the line `const SERVER = "http://<YOUR_IPv4_ADDRESS>:7777"` in controler.html
5. Open Command Prompt in the folder containing controler.html, and use `python server.py` to run the Python server.
6. Launch the main game executable.

### Step 2: Smartphone Application
1. Go to `http://<YOUR_IPv4_ADDRESS>:7777/controler.html/`. 
---

## 📡 4. Communication Architecture

### Protocol Choice & Design Rationale
* **Protocol:** HTTP for smartphone to server, UDP for real-time server to game communication.
* **Why this choice?** The architecture matches protocol strengths to specific network requirements:

**HTTP (Phone to Server)**: Handles setup and control. Easy firewall/NAT traversal on mobile networks, reliable request processing, and simple integration for room joining, pairing, and stateless commands.

**UDP (Server to Game)**: Handles real-time action. Delivers high-frequency state updates with minimal latency by eliminating TCP's retransmission delays and head-of-line blocking

### Connection Flow
1. Primary game hosts a local server on Port `7777`.
2. Smartphone connects via HTTP
3. Handshake protocol exchanges client ID and sensor capabilities.

---

## 📊 5. Benchmark Interface Documentation (Crucial for 25 Points)

> **Note for Organizers:** Use this section to run the standalone communication benchmark.

### Endpoint Details
* **Protocol:** UDP
* **Target IP:** `172.23.146.230`
* **Target Port:** `7777`
* **Mode Switch:** Launch with `--benchmark` flag or select "Benchmark Mode" on the main launcher UI.

### Benchmark Message Specs
* **Standard Test Message Format:**
  ```json
  {
    "msg_id": 1001,
    "timestamp": 1725300000,
    "payload": "ping_test"
  }
