using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// After every Windows build, copies the pre-built ControllerServer.exe (produced by
// Assets/controller/build_server_exe.bat via PyInstaller) next to the game's exe, so
// ControllerServerLauncher can find and start it at runtime.
//
// Run build_server_exe.bat whenever server.py or the controller HTML files change -
// this step only copies the already-built exe, it does not invoke PyInstaller itself.
public class ControllerServerBuildStep : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    private const string SourceExe = "Assets/controller/dist/ControllerServer.exe";

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows &&
            report.summary.platform != BuildTarget.StandaloneWindows64)
        {
            return;
        }

        if (!File.Exists(SourceExe))
        {
            Debug.LogWarning(
                "[ControllerServerBuildStep] " + SourceExe + " not found - skipping copy. " +
                "Run Assets/controller/build_server_exe.bat first so the game can host the controller pages.");
            return;
        }

        string buildDir = Path.GetDirectoryName(report.summary.outputPath);
        string destExe = Path.Combine(buildDir, "ControllerServer.exe");

        File.Copy(SourceExe, destExe, overwrite: true);
        Debug.Log("[ControllerServerBuildStep] Copied ControllerServer.exe to " + destExe);
    }
}
