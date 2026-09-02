"""
Unified controller server for the second-screen game.

This used to be two separate scripts:
  - server.py    (plain HTTP, port 8080) - served cardSwipe.html /
                  controler.html / doorShake.html and forwarded their raw
                  POST body straight to Unity over UDP (joystick, swipe,
                  interact, sprint, crouch).
  - "server 1.py" (Flask, HTTPS, ports 7777 + 8080) - served the sound-task
                  / camera-task mini-game pages and their REST API
                  (/api/<task>/trigger|check-trigger|upload|status|reset),
                  polled by Unity's TaskListener.cs and triggered by
                  pc_controller.py.

They're now one Flask app with every route, so everything is hosted by a
single process/port pair instead of needing two scripts running at once.
Run with just:

    python server.py

Ports (unchanged from before, so Unity/pc_controller.py need no changes):
  - 7777 (HTTPS, self-signed 'adhoc' cert) - Unity-facing. TaskListener.cs
    already polls https://<HOST_IP>:7777/api/<task>/status.
  - 8080 (HTTPS, self-signed 'adhoc' cert) - phone/PC-facing. Serves
    cardSwipe.html / controler.html / doorShake.html (movement controls),
    /sound-task, /camera-task, and the same /api/<task>/... routes.
    pc_controller.py already POSTs to https://<HOST_IP>:8080/api/<task>/trigger.

Both ports run the exact same Flask app - the split only exists so Unity
and the phone/PC controller can be pointed at different ports if you ever
want to firewall them separately.

NOTE: cardSwipe.html, controler.html and doorShake.html used to POST to a
plain "http://" URL. Since everything now lives behind the same HTTPS
server as the camera/mic tasks (getUserMedia requires a secure context over
LAN), those three files now POST to "https://" instead. The first time you
open any page on a phone you'll need to accept the self-signed certificate
warning once (e.g. in Chrome: Advanced -> Proceed) - it's remembered after
that.
"""

import base64
import os
import socket
import sys
import threading

from flask import Flask, jsonify, render_template_string, request, send_from_directory

# ---------------------------------------------------------------------------
# Paths - work both as a plain script and frozen into an exe (PyInstaller
# extracts bundled files to sys._MEIPASS instead of next to the exe, and
# that temp folder disappears on exit, so uploads are written next to the
# exe itself instead, not into the extracted resource dir).
# ---------------------------------------------------------------------------
if getattr(sys, "frozen", False):
    RESOURCE_DIR = getattr(sys, "_MEIPASS", os.path.dirname(sys.executable))
    RUNTIME_DIR = os.path.dirname(sys.executable)
else:
    RESOURCE_DIR = os.path.dirname(os.path.abspath(__file__))
    RUNTIME_DIR = RESOURCE_DIR

UPLOAD_FOLDER = os.path.join(RUNTIME_DIR, "uploads")
os.makedirs(UPLOAD_FOLDER, exist_ok=True)

app = Flask(__name__, static_folder=None)


# ---------------------------------------------------------------------------
# Movement / swipe / interact controls (formerly server.py).
# Forwards the raw POST body straight to Unity's PhoneReceiver over UDP.
# ---------------------------------------------------------------------------
UNITY_UDP_IP = "127.0.0.1"
UNITY_UDP_PORT = 7777

udp_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

CONTROLLER_PAGES = {"cardSwipe.html", "controler.html", "doorShake.html"}


@app.route("/", methods=["GET", "POST"])
def root():
    if request.method == "POST":
        return forward_to_unity()
    # Default landing page when someone just opens the server's base URL.
    return send_from_directory(RESOURCE_DIR, "controler.html")


@app.route("/<path:filename>")
def controller_page(filename):
    if filename in CONTROLLER_PAGES:
        return send_from_directory(RESOURCE_DIR, filename)
    return jsonify({"error": "not found"}), 404


def forward_to_unity():
    data = request.get_data(as_text=True)
    print("PHONE:", data)

    try:
        udp_socket.sendto(data.encode("utf-8"), (UNITY_UDP_IP, UNITY_UDP_PORT))
    except OSError as e:
        print("Failed to forward to Unity:", e)

    return ("", 200)


# ---------------------------------------------------------------------------
# Sound / camera tasks (formerly "server 1.py").
# ---------------------------------------------------------------------------

# Add a new entry here for every task instead of writing a new server.
tasks_state = {
    "sound-task": {"completed": False, "trigger": False},
    "camera-task": {"completed": False, "trigger": False},
    # "another-task": {"completed": False, "trigger": False},
}

SOUND_TASK_HTML = """
<!DOCTYPE html>
<html>
<head>
    <title>Sound Task</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body { font-family: Arial, sans-serif; text-align: center; margin-top: 50px; background: #222; color: #fff; }
        #db-display { font-size: 48px; font-weight: bold; margin: 20px 0; color: #28a745; }
        button { padding: 12px 24px; font-size: 18px; background: #28a745; color: white; border: none; border-radius: 5px; }
        #success { display: none; font-size: 24px; color: #28a745; margin-top: 20px; }
    </style>
</head>
<body>
    <h2>Sound Task</h2>
    <div id="db-display">-- dB</div>
    <button id="listen-btn" onclick="startListening()">Start Listening</button>
    <div id="success">Task Completed!</div>
    <script>
        const TASK = "sound-task"; // <-- identifies this page's task to the server
        const dbDisplay = document.getElementById('db-display');
        const successDiv = document.getElementById('success');
        const VOLUME_THRESHOLD = 0.1;
        let audioContext, analyser, dataArray, listening = false, taskDone = false;

        async function initMic() {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const source = audioContext.createMediaStreamSource(stream);
            analyser = audioContext.createAnalyser();
            analyser.fftSize = 2048;
            source.connect(analyser);
            dataArray = new Float32Array(analyser.fftSize);
            requestAnimationFrame(updateMeter);
        }

        function updateMeter() {
            if (analyser) {
                analyser.getFloatTimeDomainData(dataArray);
                let peak = 0;
                for (let i = 0; i < dataArray.length; i++) {
                    const abs = Math.abs(dataArray[i]);
                    if (abs > peak) peak = abs;
                }
                const db = peak > 0.0000001 ? 20 * Math.log10(peak) : -160;
                dbDisplay.textContent = db.toFixed(1) + ' dB';
                if (listening && !taskDone && peak >= VOLUME_THRESHOLD) completeTask();
            }
            requestAnimationFrame(updateMeter);
        }

        function startListening() { listening = true; }

        function completeTask() {
            taskDone = true;
            successDiv.style.display = 'block';
            fetch(`/api/${TASK}/upload`, { method: 'POST' });
        }

        setInterval(() => {
            fetch(`/api/${TASK}/check-trigger`).then(r => r.json()).then(data => {
                if (data.trigger && !listening && !taskDone) listening = true;
            });
        }, 1000);

        initMic();
    </script>
</body>
</html>
"""

CAMERA_TASK_HTML = """
<!DOCTYPE html>
<html>
<head>
    <title>Camera Task</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body { font-family: Arial, sans-serif; text-align: center; margin-top: 50px; background: #222; color: #fff; }
        video, canvas { width: 100%; max-width: 400px; border-radius: 8px; }
        button { padding: 12px 24px; font-size: 18px; background: #28a745; color: white; border: none; border-radius: 5px; cursor: pointer; margin-top: 15px; }
        button:disabled { background: #555; cursor: not-allowed; }
        #success { display: none; font-size: 24px; color: #28a745; margin-top: 20px; }
        canvas { display: none; }
        #cam-status { font-size: 14px; color: #ffcc00; margin-top: 10px; min-height: 20px; }
    </style>
</head>
<body>
    <h2>Camera Task</h2>
    <div id="camera-container">
        <video id="video" autoplay playsinline muted></video>
        <div id="cam-status">Requesting camera access...</div>
        <br>
        <button id="capture-btn" onclick="captureImage()" disabled>Capture Image</button>
    </div>
    <canvas id="canvas"></canvas>
    <div id="success">Task Completed!</div>

    <script>
        const TASK = "camera-task"; // <-- identifies this page's task to the server
        const video = document.getElementById('video');
        const canvas = document.getElementById('canvas');
        const successDiv = document.getElementById('success');
        const camStatus = document.getElementById('cam-status');
        const captureBtn = document.getElementById('capture-btn');
        let cameraReady = false, taskDone = false;

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            camStatus.textContent = 'Camera not available: this page needs HTTPS (or localhost).';
        } else {
            navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } })
                .then(stream => { video.srcObject = stream; })
                .catch(err => {
                    camStatus.textContent = 'Camera error: ' + err.message + ' (often means the page needs HTTPS)';
                });
        }

        video.addEventListener('loadedmetadata', () => {
            if (video.videoWidth > 0 && video.videoHeight > 0) {
                cameraReady = true;
                camStatus.textContent = '';
                captureBtn.disabled = false;
            }
        });

        function captureImage() {
            if (taskDone || !cameraReady || video.videoWidth === 0) {
                camStatus.textContent = 'Camera not ready yet — cannot capture a blank image.';
                return;
            }

            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;
            canvas.getContext('2d').drawImage(video, 0, 0, canvas.width, canvas.height);
            const imageDataUrl = canvas.toDataURL('image/jpeg');

            fetch(`/api/${TASK}/upload`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ image: imageDataUrl })
            })
            .then(res => res.json())
            .then(data => {
                if (data.status === 'success') {
                    taskDone = true;
                    if (video.srcObject) video.srcObject.getTracks().forEach(t => t.stop());
                    video.style.display = 'none';
                    captureBtn.style.display = 'none';
                    canvas.style.display = 'block';
                    successDiv.style.display = 'block';
                }
            });
        }

        // Poll server: PC-triggered capture, namespaced like sound-task.
        setInterval(() => {
            fetch(`/api/${TASK}/check-trigger`).then(r => r.json()).then(data => {
                if (data.trigger && !taskDone) captureImage();
            });
        }, 1000);
    </script>
</body>
</html>
"""

# --- Page routes: one per task, all on the same server/IP/port ---

@app.route('/sound-task')
def sound_task_page():
    return render_template_string(SOUND_TASK_HTML)

@app.route('/camera-task')
def camera_task_page():
    return render_template_string(CAMERA_TASK_HTML)


# --- Generic API routes, shared by all tasks, namespaced by <task_name> ---

@app.route('/api/<task_name>/trigger', methods=['POST'])
def trigger(task_name):
    if task_name not in tasks_state:
        return jsonify({"error": "unknown task"}), 404
    tasks_state[task_name]["trigger"] = True
    print(f"[SERVER] Trigger sent for '{task_name}'")
    return jsonify({"status": "triggered"})

@app.route('/api/<task_name>/check-trigger', methods=['GET'])
def check_trigger(task_name):
    if task_name not in tasks_state:
        return jsonify({"error": "unknown task"}), 404
    val = tasks_state[task_name]["trigger"]
    tasks_state[task_name]["trigger"] = False
    return jsonify({"trigger": val})

@app.route('/api/<task_name>/upload', methods=['POST'])
def upload(task_name):
    if task_name not in tasks_state:
        return jsonify({"error": "unknown task"}), 404

    # Tasks like sound-task POST with no body at all — that's fine, completion
    # alone is the signal. Tasks like camera-task send {"image": "<dataURL>"},
    # so only touch request.json if a JSON body was actually sent.
    data = request.get_json(silent=True)
    if data and data.get('image'):
        try:
            header, encoded = data['image'].split(",", 1)
            image_bytes = base64.b64decode(encoded)
        except (ValueError, base64.binascii.Error):
            return jsonify({"error": "invalid image data"}), 400

        file_path = os.path.join(UPLOAD_FOLDER, f'{task_name}.jpg')
        with open(file_path, 'wb') as f:
            f.write(image_bytes)
        print(f"[SERVER] Task '{task_name}' image saved to {file_path}")

    tasks_state[task_name]["completed"] = True
    print(f"[SERVER] Task '{task_name}' completed!")
    return jsonify({"status": "success"})

@app.route('/api/<task_name>/status', methods=['GET'])
def status(task_name):
    if task_name not in tasks_state:
        return jsonify({"error": "unknown task"}), 404
    return jsonify({"completed": tasks_state[task_name]["completed"]})

@app.route('/api/<task_name>/reset', methods=['POST'])
def reset(task_name):
    if task_name not in tasks_state:
        return jsonify({"error": "unknown task"}), 404
    tasks_state[task_name]["completed"] = False
    tasks_state[task_name]["trigger"] = False
    print(f"[SERVER] Task '{task_name}' reset.")
    return jsonify({"status": "reset"})


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def get_lan_ip():
    """Best-effort guess at this machine's LAN (Wi-Fi/Ethernet) IP. Only used
    as a fallback below if HOST_IP can't be bound (e.g. you moved to a
    different network and forgot to update it). Doesn't actually send any
    traffic - just asks the OS which local interface it would use to reach
    an external address."""
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(("8.8.8.8", 80))
        return s.getsockname()[0]
    except OSError:
        return "127.0.0.1"
    finally:
        s.close()


# Pinned to this PC's current Wi-Fi IP so it matches the SERVER constant
# hardcoded in controler.html / cardSwipe.html / doorShake.html and the
# SERVER_BASE in pc_controller.py. If you switch networks and this address
# changes (check with `ipconfig`), update it here and in those files.
HOST_IP = "172.23.146.230"

UNITY_PORT = 7777  # Unity's TaskListener.cs polling + PhoneReceiver UDP forward target
PHONE_PORT = 8080  # phone control/task pages + pc_controller.py triggers

if __name__ == '__main__':
    bind_ip = HOST_IP
    if get_lan_ip() != HOST_IP:
        print(f"WARNING: this machine's current LAN IP doesn't match HOST_IP ({HOST_IP}).")
        print(f"         Detected {get_lan_ip()} instead - update HOST_IP in server.py")
        print(f"         (and the SERVER constant in the html files) if that's wrong.")

    print(f"Controller server starting at {bind_ip}")
    print(f"  Phone pages:  https://{bind_ip}:{PHONE_PORT}/controler.html  (also cardSwipe.html, doorShake.html, sound-task, camera-task)")
    print(f"  Unity API:    https://{bind_ip}:{UNITY_PORT}/api/<task>/status")

    def run_unity_listener():
        app.run(host=bind_ip, port=UNITY_PORT, ssl_context='adhoc', use_reloader=False)

    threading.Thread(target=run_unity_listener, daemon=True).start()
    app.run(host=bind_ip, port=PHONE_PORT, ssl_context='adhoc', use_reloader=False)
