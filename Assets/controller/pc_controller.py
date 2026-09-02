import tkinter as tk
import requests
import urllib3

# multi_task_server.py runs with a self-signed HTTPS cert (ssl_context='adhoc').
# verify=False skips certificate validation, which is fine for talking to your own
# local dev server but should never be used against a real/public server.
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# server.py now auto-detects and prints its LAN IP on startup instead of
# using a fixed address - update this to match whatever it prints.
SERVER_BASE = "https://172.23.146.230:8080"

# Add a new entry here for every task registered in multi_task_server.py's tasks_state.
TASKS = [
    {"name": "sound-task", "label": "Start Sound Task"},
    {"name": "camera-task", "label": "Capture Image from Phone"},
]

def send_trigger(task_name, btn):
    url = f"{SERVER_BASE}/api/{task_name}/trigger"
    try:
        response = requests.post(url, verify=False)
        if response.status_code == 200:
            print(f"Trigger sent for '{task_name}' via server!")
            btn.config(text="Triggered!", bg="green")
        else:
            print(f"Failed to trigger '{task_name}': {response.status_code}")
            btn.config(text="Failed - retry?", bg="red")
    except Exception as e:
        print(f"Error connecting to server: {e}")
        btn.config(text="Error - retry?", bg="red")

root = tk.Tk()
root.title("Unity Multi-Task PC Controller")
root.geometry("300x150")

for task in TASKS:
    btn = tk.Button(root, text=task["label"], bg="blue", fg="white", font=("Arial", 12))
    btn.config(command=lambda t=task["name"], b=btn: send_trigger(t, b))
    btn.pack(expand=True, fill="both", padx=20, pady=10)

root.mainloop()