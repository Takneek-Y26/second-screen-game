using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Wire this to a UI Toggle ("Benchmark Mode"). Flipping it on shows a "stats for nerds"
// style overlay box with FPS/frame time/memory plus live network benchmark numbers from
// NetworkBenchmark (if one is assigned), and enables that component so it starts pinging.
// Flipping it off hides the panel and stops the benchmark traffic.
public class StatsForNerdsUI : MonoBehaviour
{
    [Header("Wiring")]
    public Toggle benchmarkToggle;
    public GameObject statsPanel;      // the box shown/hidden by the toggle
    public TMP_Text statsText;         // TextMeshPro label inside the box
    public NetworkBenchmark benchmark; // optional; leave null if you only want FPS/perf stats

    [Header("Update rate")]
    [Tooltip("How often the overlay text refreshes, in seconds. Keep it low but nonzero so building the string doesn't cost real frame time.")]
    public float refreshInterval = 0.25f;

    private float refreshTimer;
    private readonly StringBuilder sb = new StringBuilder(256);

    // FPS smoothing
    private float fpsAccum;
    private int fpsFrames;
    private float smoothedFps;
    private float smoothedFrameMs;

    void Awake()
    {
        if (benchmarkToggle != null)
            benchmarkToggle.onValueChanged.AddListener(OnToggleChanged);

        // Start hidden/off until the switch is flipped.
        SetActive(benchmarkToggle != null && benchmarkToggle.isOn);
    }

    void OnDestroy()
    {
        if (benchmarkToggle != null)
            benchmarkToggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    void OnToggleChanged(bool isOn)
    {
        SetActive(isOn);
    }

    void SetActive(bool isOn)
    {
        if (statsPanel != null) statsPanel.SetActive(isOn);
        if (benchmark != null) benchmark.enabled = isOn;
    }

    void Update()
    {
        // Accumulate FPS every frame regardless of refresh cadence, so the readout
        // reflects a real average rather than a single-frame spike.
        fpsAccum += 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        fpsFrames++;

        if (statsPanel == null || !statsPanel.activeSelf || statsText == null) return;

        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < refreshInterval) return;
        refreshTimer = 0f;

        smoothedFps = fpsAccum / fpsFrames;
        smoothedFrameMs = 1000f / Mathf.Max(smoothedFps, 0.0001f);
        fpsAccum = 0f;
        fpsFrames = 0;

        RenderText();
    }

    void RenderText()
    {
        sb.Clear();
        sb.AppendLine("<b>STATS FOR NERDS</b>");
        sb.Append("FPS: ").Append(smoothedFps.ToString("0")).Append("  (").Append(smoothedFrameMs.ToString("0.0")).AppendLine(" ms)");
        sb.Append("Resolution: ").Append(Screen.width).Append("x").AppendLine(Screen.height.ToString());
        sb.Append("Mem (managed): ").Append((System.GC.GetTotalMemory(false) / (1024f * 1024f)).ToString("0.0")).AppendLine(" MB");
        sb.Append("Platform: ").AppendLine(Application.platform.ToString());

        if (benchmark != null)
        {
            var s = benchmark.CurrentStats;
            sb.AppendLine();
            sb.AppendLine("<b>NETWORK BENCHMARK</b>");
            sb.Append("Target: ").Append(benchmark.targetIP).Append(":").AppendLine(benchmark.targetPort.ToString());
            sb.Append("Protocol: ").AppendLine(benchmark.protocol.ToString());
            sb.Append("Sent: ").Append(s.sent).Append("   Recv: ").Append(s.received).Append("   Lost: ").AppendLine(s.lost.ToString());
            sb.Append("Loss: ").Append(s.lossPercent.ToString("0.0")).AppendLine(" %");
            sb.Append("RTT: ").Append(s.lastRttMs.ToString("0")).Append(" ms (avg ").Append(s.avgRttMs.ToString("0"))
              .Append(", min ").Append(s.minRttMs.ToString("0")).Append(", max ").Append(s.maxRttMs.ToString("0")).AppendLine(")");
            sb.Append("Jitter: ").Append(s.jitterMs.ToString("0.0")).AppendLine(" ms");
        }

        statsText.text = sb.ToString();
    }
}
