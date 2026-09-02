using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Put this on a persistent object in your first scene (e.g. the same NetworkManager
// that has HostDiscovery/PhoneReceiver). In a built Windows game it launches
// ControllerServer.exe - the standalone build of Assets/controller/server.py, placed
// next to the game exe by ControllerServerBuildStep - as a hidden background process,
// so the phone controller pages (controler.html etc) are served without the player
// having to start Python/anything manually. The process is killed when the game quits.
//
// Does nothing in the editor or on non-Windows platforms; run server.py directly
// (or via preview_start-style tooling) for in-editor testing instead.
public class ControllerServerLauncher : MonoBehaviour
{
    [Tooltip("Filename of the bundled server exe, expected next to the game's own exe.")]
    public string exeName = "ControllerServer.exe";

    [Tooltip("Also try to launch this when running the game from the editor (useful for quick testing). Requires ControllerServer.exe to already exist next to Application.dataPath's project folder - usually not set up in-editor, leave off unless you know what you're doing.")]
    public bool launchInEditor = false;

    private static Process serverProcess;

    void Awake()
    {
        // Singleton-ish: don't spawn a second server if this object exists in
        // more than one loaded scene / survives a scene reload.
        if (serverProcess != null && !serverProcess.HasExited)
        {
            return;
        }

        if (Application.isEditor && !launchInEditor)
        {
            return;
        }

        if (Application.platform != RuntimePlatform.WindowsPlayer &&
            !(Application.isEditor && Application.platform == RuntimePlatform.WindowsEditor))
        {
            Debug.Log("[ControllerServerLauncher] Not on Windows, skipping (bundled server is a .exe).");
            return;
        }

        // Survive scene loads so we don't relaunch (or lose track of) the server
        // process every time a new scene comes in.
        DontDestroyOnLoad(gameObject);

        StartServer();
    }

    void StartServer()
    {
        try
        {
            string gameDir = Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath));
            string exePath = Path.Combine(gameDir ?? ".", exeName);

            if (!File.Exists(exePath))
            {
                Debug.LogWarning("[ControllerServerLauncher] " + exePath + " not found - controller pages won't be hosted.");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            serverProcess = Process.Start(startInfo);
            Debug.Log("[ControllerServerLauncher] Started " + exeName + " (pid " + serverProcess?.Id + ")");
        }
        catch (Exception e)
        {
            Debug.LogError("[ControllerServerLauncher] Failed to start server: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        StopServer();
    }

    static void StopServer()
    {
        if (serverProcess == null) return;

        try
        {
            if (!serverProcess.HasExited)
            {
                serverProcess.Kill();
                serverProcess.WaitForExit(2000);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ControllerServerLauncher] Failed to stop server: " + e.Message);
        }
        finally
        {
            serverProcess = null;
        }
    }
}
