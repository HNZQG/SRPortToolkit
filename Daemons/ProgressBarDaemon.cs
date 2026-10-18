using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Hacknet;
using Hacknet.Gui;
using Pathfinder.Util;
using Pathfinder.Util.XML;

namespace SRPortToolkit.Daemon
{
    public class ProgressBarDaemon : Pathfinder.Daemon.BaseDaemon
    {
        // ---------- Basic Properties ----------
        private float progress = 0f;
        private float targetProgress = 0f;
        private float changeRate = 0f;
        private bool isChanging = false;
        private string displayTitle = "NONE";
        private int serverId = 0;
        private float lastUpdateTime = 0f;
        private string completionFlagName => $"{serverId}100FIN";

        // State thresholds
        private const float THRESHOLD_1 = 0.3f;
        private const float THRESHOLD_2 = 0.6f;
        private const float THRESHOLD_3 = 0.9f;
        private const float THRESHOLD_4 = 0.99f;

        // Colors
        private Color[] progressColors = new Color[100];
        private Color statusColorOffline = new Color(255, 80, 80);
        private Color statusColorStarting = new Color(255, 160, 80);
        private Color statusColorRunning = new Color(80, 160, 255);
        private Color statusColorAlmostDone = new Color(80, 220, 255);
        private Color statusColorNearComplete = new Color(100, 200, 255);
        private Color arrowUpColor = new Color(100, 255, 100);
        private Color arrowDownColor = new Color(255, 100, 100);

        // Fake data
        private float cpuUsage = 0f;
        private float memoryUsage = 0f;
        private float networkLatency = 0f;
        private int activeConnections = 0;
        private string systemVersion = "v2.4.1";
        private string securityLevel = "C";
        private int cacheHits = 0;
        private int cacheMisses = 0;
        private float temperature = 45f;
        private string lastBackupDate = "2026-02-21";
        private int errorCount = 0;
        private float bandwidthIn = 0f;
        private float bandwidthOut = 0f;
        private string statusMessage = "System Offline";

        // Uptime
        private float realTimeUptime = 0f;
        private bool isRunning = false;
        private DateTime? progressStartTime = null;

        // Smoothing parameters
        private float lastStatsUpdateTime = 0f;
        private Random statsRandom = new Random();
        private float targetCpuUsage = 0f;
        private float targetMemoryUsage = 0f;
        private float targetNetworkLatency = 0f;
        private int targetActiveConnections = 0;
        private float targetTemperature = 45f;
        private float targetBandwidthIn = 0f;
        private float targetBandwidthOut = 0f;
        private int targetCacheHits = 0;
        private int targetCacheMisses = 0;
        private int targetErrorCount = 0;

        // Change directions
        private bool cpuIncreasing = false;
        private bool memoryIncreasing = false;
        private bool latencyIncreasing = false;
        private bool connectionsIncreasing = false;
        private bool temperatureIncreasing = false;
        private bool bandwidthInIncreasing = false;
        private bool bandwidthOutIncreasing = false;

        [XMLStorage]
        public string Title;

        [XMLStorage]
        public int Id;

        public ProgressBarDaemon(Computer computer, string serviceName, OS opSystem)
            : base(computer, serviceName, opSystem)
        {
            progress = 0f;
            targetProgress = 0f;
            lastUpdateTime = 0f;
            lastStatsUpdateTime = 0f;
            InitializeProgressColors();
        }

        private void InitializeProgressColors()
        {
            Color[] keyColors = new Color[]
            {
                new Color(0, 0, 80), new Color(0, 40, 150), new Color(0, 80, 200),
                new Color(0, 120, 230), new Color(0, 160, 250), new Color(0, 200, 255),
                new Color(30, 220, 255), new Color(60, 235, 255), new Color(100, 245, 255),
                new Color(150, 250, 255), new Color(200, 255, 255)
            };
            for (int i = 0; i < 100; i++)
            {
                float p = i / 99f;
                float ki = p * 10f;
                int k1 = (int)ki;
                int k2 = Math.Min(k1 + 1, 10);
                float t = ki - k1;
                progressColors[i] = (k1 <= 9) ? Color.Lerp(keyColors[k1], keyColors[k2], t) : keyColors[10];
            }
        }

        // ---------- Update Methods ----------
        private void UpdateUptime()
        {
            if (os == null) return;
            bool wasRunning = isRunning;
            isRunning = progress > 0.001f;
            if (!wasRunning && isRunning)
                progressStartTime = DateTime.Now;
            if (isRunning && progressStartTime.HasValue)
                realTimeUptime = (float)(DateTime.Now - progressStartTime.Value).TotalHours;
            else if (!isRunning)
                progressStartTime = null;
        }

        private void UpdateFakeStats()
        {
            if (os == null) return;
            float currentTime = (float)os.timer;
            if (lastStatsUpdateTime == 0f) { lastStatsUpdateTime = currentTime; return; }
            float delta = currentTime - lastStatsUpdateTime;
            if (delta >= 0.3f)
            {
                lastStatsUpdateTime = currentTime;
                float pf = progress;
                targetCpuUsage = MathHelper.Clamp(pf * 70f + (float)(statsRandom.NextDouble() * 20 - 5), 0f, 88f);
                targetMemoryUsage = MathHelper.Clamp(pf * 6.5f + (float)(statsRandom.NextDouble() * 2.5 - 0.8), 0f, 8f);
                targetNetworkLatency = MathHelper.Clamp(pf * 45f + (float)(statsRandom.NextDouble() * 25 - 5), 0f, 75f);
                targetActiveConnections = Math.Max(0, Math.Min(45, (int)(pf * 28f + statsRandom.Next(-5, 15))));
                if (pf < 0.01f)
                {
                    targetBandwidthIn = (float)(statsRandom.NextDouble() * 0.2);
                    targetBandwidthOut = (float)(statsRandom.NextDouble() * 0.1);
                }
                else
                {
                    float bwBase = pf * 20f;
                    targetBandwidthIn = MathHelper.Clamp(bwBase + (float)(statsRandom.NextDouble() * 12 - 4), 0f, 30f);
                    targetBandwidthOut = MathHelper.Clamp(bwBase * 0.7f + (float)(statsRandom.NextDouble() * 8 - 2), 0f, 22f);
                }
                if (pf > 0.1f)
                {
                    targetCacheHits = (int)(pf * 2000 + statsRandom.Next(100, 500));
                    targetCacheMisses = (int)(pf * 80 + statsRandom.Next(5, 30));
                }
                else
                {
                    targetCacheHits = statsRandom.Next(0, 50);
                    targetCacheMisses = statsRandom.Next(0, 10);
                }
                float baseTemp = 45f;
                float wave1 = (float)Math.Sin(currentTime * 0.5) * 1.2f;
                float wave2 = (float)Math.Cos(currentTime * 0.3) * 0.8f;
                float noise = (float)(statsRandom.NextDouble() * 1.0 - 0.5);
                targetTemperature = MathHelper.Clamp(baseTemp + wave1 + wave2 + noise, 43f, 47f);

                // Error count
                if (pf >= 0.9f)
                {
                    if (statsRandom.NextDouble() > 0.6f)
                        targetErrorCount = Math.Min(15, errorCount + statsRandom.Next(1, 4));
                    else if (statsRandom.NextDouble() > 0.8f && errorCount > 0)
                        targetErrorCount = Math.Max(0, errorCount - 1);
                    else
                        targetErrorCount = errorCount;
                }
                else if (pf >= 0.8f)
                {
                    if (statsRandom.NextDouble() > 0.95f)
                        targetErrorCount = Math.Min(5, errorCount + statsRandom.Next(1, 3));
                    else if (statsRandom.NextDouble() > 0.9f && errorCount > 0)
                        targetErrorCount = Math.Max(0, errorCount - 1);
                    else
                        targetErrorCount = errorCount;
                }
                else
                {
                    if (statsRandom.NextDouble() > 0.98f)
                        targetErrorCount = Math.Min(3, errorCount + 1);
                    else if (statsRandom.NextDouble() > 0.9f && errorCount > 0)
                        targetErrorCount = errorCount - 1;
                    else
                        targetErrorCount = Math.Max(0, errorCount - (statsRandom.NextDouble() > 0.7f ? 1 : 0));
                }
                UpdateStatusByProgress();
            }

            float smooth = 0.15f;
            float oldCpu = cpuUsage, oldMem = memoryUsage, oldLat = networkLatency;
            int oldConn = activeConnections;
            float oldTemp = temperature, oldBwIn = bandwidthIn, oldBwOut = bandwidthOut;

            cpuUsage += (targetCpuUsage - cpuUsage) * smooth;
            memoryUsage += (targetMemoryUsage - memoryUsage) * smooth;
            networkLatency += (targetNetworkLatency - networkLatency) * smooth;
            if (activeConnections != targetActiveConnections)
            {
                int step = Math.Abs(targetActiveConnections - activeConnections) > 3 ? 2 : 1;
                activeConnections += (activeConnections < targetActiveConnections) ? step : -step;
            }
            temperature += (targetTemperature - temperature) * smooth;
            bandwidthIn += (targetBandwidthIn - bandwidthIn) * smooth;
            bandwidthOut += (targetBandwidthOut - bandwidthOut) * smooth;

            cpuIncreasing = cpuUsage > oldCpu;
            memoryIncreasing = memoryUsage > oldMem;
            latencyIncreasing = networkLatency > oldLat;
            connectionsIncreasing = activeConnections > oldConn;
            temperatureIncreasing = temperature > oldTemp;
            bandwidthInIncreasing = bandwidthIn > oldBwIn;
            bandwidthOutIncreasing = bandwidthOut > oldBwOut;

            if (errorCount != targetErrorCount)
                errorCount += (errorCount < targetErrorCount) ? 1 : -1;
            if (cacheHits < targetCacheHits)
                cacheHits += Math.Max(1, (targetCacheHits - cacheHits) / 10);
            if (cacheMisses < targetCacheMisses)
                cacheMisses += Math.Max(1, (targetCacheMisses - cacheMisses) / 10);
        }

        private void UpdateStatusByProgress()
        {
            if (progress <= 0.01f) { securityLevel = "C"; statusMessage = "System Offline"; }
            else if (progress < 0.2f) { securityLevel = "C+"; statusMessage = "Basic Services Starting"; }
            else if (progress < 0.4f) { securityLevel = "B-"; statusMessage = "Core Modules Loading"; }
            else if (progress < 0.6f) { securityLevel = "B"; statusMessage = "Configuration Optimization in Progress"; }
            else if (progress < 0.8f) { securityLevel = "B+"; statusMessage = "Performance Tuning"; }
            else if (progress < 0.9f) { securityLevel = "A-"; statusMessage = "System Stable"; }
            else if (progress < 0.95f) { securityLevel = "A"; statusMessage = "Optimization Nearly Complete"; }
            else if (progress < 0.99f) { securityLevel = "A"; statusMessage = "Error Rate Rising - Check"; }
            else { securityLevel = "A+"; statusMessage = "System Operating Normally"; }
        }

        // ---------- Loading ----------
        public override void LoadFromXml(ElementInfo info)
        {
            base.LoadFromXml(info);
            if (!string.IsNullOrEmpty(Title)) displayTitle = Title;
            serverId = Id;
        }

        public override void loadInit() => base.loadInit();

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            UpdateProgress();
            UpdateFakeStats();
            UpdateUptime();

            base.draw(bounds, sb);
            DrawBackground(bounds, sb);

            Rectangle contentRect = new Rectangle(bounds.X + 40, bounds.Y + 40, bounds.Width - 80, bounds.Height - 80);

            // Title
            string titleText = $"[#{serverId:D2}] {displayTitle}";
            if (isChanging && changeRate != 0f)
            {
                float remaining = CalculateRemainingSeconds();
                if (remaining >= 0)
                {
                    if (remaining >= 60)
                        titleText += $" | Remaining {(int)(remaining / 60)}min {(int)(remaining % 60)}sec";
                    else
                        titleText += $" | Remaining {remaining:F1}sec";
                }
            }
            Vector2 ts = GuiData.font.MeasureString(titleText);
            TextItem.doFontLabel(new Vector2(contentRect.X + (contentRect.Width - ts.X) / 2, contentRect.Y), titleText, GuiData.font, Color.White);

            // Progress bar background
            Rectangle bgRect = new Rectangle(contentRect.X, contentRect.Y + 60, contentRect.Width, 40);
            sb.Draw(Utils.white, bgRect, new Color(30, 30, 30));

            // Progress bar fill
            if (progress > 0f)
            {
                int totalW = contentRect.Width;
                int progW = (int)(totalW * Math.Min(progress, 1f));
                for (int i = 0; i < 100; i++)
                {
                    float segStart = i / 100f;
                    float segEnd = (i + 1) / 100f;
                    int startX = contentRect.X + (int)(totalW * segStart);
                    int endX = contentRect.X + (int)(totalW * segEnd);
                    int segW = endX - startX;
                    if (startX < contentRect.X + progW)
                    {
                        int drawW = segW;
                        if (startX + drawW > contentRect.X + progW)
                            drawW = contentRect.X + progW - startX;
                        if (drawW > 0)
                        {
                            Rectangle segRect = new Rectangle(startX, contentRect.Y + 60, drawW, 40);
                            Color col = GetProgressColor((i + 0.5f) / 100f);
                            sb.Draw(Utils.white, segRect, col);
                        }
                    }
                }
            }

            // Progress bar border
            Rectangle border = new Rectangle(contentRect.X, contentRect.Y + 60, contentRect.Width, 40);
            sb.Draw(Utils.white, border, new Color(100, 100, 100) * 0.5f);

            // Percentage text
            string pct = $"{(progress * 100):F1}%";
            Vector2 pctS = GuiData.font.MeasureString(pct);
            float pctY = contentRect.Y + 60 + (40 - pctS.Y) / 2;
            TextItem.doFontLabel(
                new Vector2(contentRect.X + (contentRect.Width - pctS.X) / 2, pctY),
                pct, GuiData.font, Color.White
            );

            // ---------- Status text ----------
            string statusText;
            Color statusColor;
            if (progress <= 0f) { statusText = "Offline"; statusColor = statusColorOffline; }
            else if (progress < THRESHOLD_1) { statusText = "Preloading"; float t = progress / THRESHOLD_1; statusColor = Color.Lerp(statusColorOffline, statusColorStarting, t); }
            else if (progress < THRESHOLD_2) { statusText = "Configuring"; float t = (progress - THRESHOLD_1) / (THRESHOLD_2 - THRESHOLD_1); statusColor = Color.Lerp(statusColorStarting, statusColorRunning, t); }
            else if (progress < THRESHOLD_3) { statusText = "Loading"; float t = (progress - THRESHOLD_2) / (THRESHOLD_3 - THRESHOLD_2); statusColor = Color.Lerp(statusColorRunning, statusColorAlmostDone, t); }
            else if (progress < THRESHOLD_4) { statusText = "Almost Ready"; float t = (progress - THRESHOLD_3) / (THRESHOLD_4 - THRESHOLD_3); statusColor = Color.Lerp(statusColorAlmostDone, statusColorNearComplete, t); }
            else { statusText = "System Operating Normally"; statusColor = statusColorRunning; }
            Vector2 ss = GuiData.smallfont.MeasureString(statusText);
            TextItem.doFontLabel(new Vector2(contentRect.X + (contentRect.Width - ss.X) / 2, contentRect.Y + 145), statusText, GuiData.smallfont, statusColor);

            // ---------- Change rate ----------
            if (isChanging)
            {
                string chg = $"Change Rate: {Math.Abs(changeRate) * 100:F1}%/s";
                Vector2 cs = GuiData.tinyfont.MeasureString(chg);
                TextItem.doFontLabel(new Vector2(contentRect.X + contentRect.Width - cs.X - 10, contentRect.Y + 175), chg, GuiData.tinyfont, new Color(150, 150, 150));
            }

            DrawProgressTickMarks(contentRect, sb);

            // ---------- Stats ----------
            int statsStartY = contentRect.Y + 220;
            int colWidth = contentRect.Width / 2 - 30;
            int currentY = statsStartY;
            Rectangle sep = new Rectangle(contentRect.X, statsStartY - 15, contentRect.Width, 2);
            sb.Draw(Utils.white, sep, new Color(80, 80, 80) * 0.5f);

            int leftValX = contentRect.X + 170;
            DrawStatItem(sb, contentRect.X, leftValX, ref currentY, "CPU Usage", $"{cpuUsage:F1}%", GetValueColor(cpuUsage, 75f, 55f), true, cpuIncreasing);
            DrawStatItem(sb, contentRect.X, leftValX, ref currentY, "Memory Usage", $"{memoryUsage:F1} GB", GetValueColor(memoryUsage, 6.5f, 4.5f), true, memoryIncreasing);
            DrawStatItem(sb, contentRect.X, leftValX, ref currentY, "Network Latency", $"{networkLatency:F0} ms", GetLatencyColor(networkLatency), true, latencyIncreasing);
            DrawStatItem(sb, contentRect.X, leftValX, ref currentY, "Active Connections", $"{activeConnections}", GetConnectionColor(activeConnections), true, connectionsIncreasing);
            DrawStatItem(sb, contentRect.X, leftValX, ref currentY, "System Version", systemVersion, new Color(180, 180, 180), false, false);
            DrawStatItem(sb, contentRect.X, leftValX, ref currentY, "Security Level", securityLevel, GetSecurityColor(securityLevel), false, false);

            currentY = statsStartY;
            int rightColX = contentRect.X + colWidth + 100;
            int rightValX = rightColX + 200;

            string uptimeText = (realTimeUptime < 1.0f) ? $"{ (int)(realTimeUptime * 60) } min" : $"{ (int)realTimeUptime } h { (int)((realTimeUptime - (int)realTimeUptime) * 60) } min";
            DrawStatItem(sb, rightColX, rightValX, ref currentY, "Uptime", uptimeText, isRunning ? Color.LightGreen : new Color(150, 150, 150), false, false);

            float hitRate = (cacheHits + cacheMisses) > 0 ? (cacheHits * 100f / (cacheHits + cacheMisses)) : 0;
            DrawStatItem(sb, rightColX, rightValX, ref currentY, "Cache Hit Rate", $"{hitRate:F1}%", GetCacheHitColor(hitRate), false, false);
            DrawStatItem(sb, rightColX, rightValX, ref currentY, "Temperature", $"{temperature:F1}°C", GetTemperatureColor(temperature), true, temperatureIncreasing);

            string bwText = (progress < 0.01f) ? "0/0 Mbps" : $"{bandwidthIn:F1}/{bandwidthOut:F1} Mbps";
            TextItem.doFontLabel(new Vector2(rightColX, currentY), "Bandwidth :", GuiData.tinyfont, new Color(120, 120, 120));
            Vector2 bws = GuiData.tinyfont.MeasureString(bwText);
            TextItem.doFontLabel(new Vector2(rightValX - bws.X, currentY), bwText, GuiData.tinyfont, GetBandwidthColor(bandwidthIn, bandwidthOut));
            string a1 = bandwidthInIncreasing ? "↑" : "↓";
            string a2 = bandwidthOutIncreasing ? "↑" : "↓";
            Color c1 = bandwidthInIncreasing ? arrowUpColor : arrowDownColor;
            Color c2 = bandwidthOutIncreasing ? arrowUpColor : arrowDownColor;
            TextItem.doFontLabel(new Vector2(rightValX + 10, currentY), a1, GuiData.tinyfont, c1);
            TextItem.doFontLabel(new Vector2(rightValX + 25, currentY), a2, GuiData.tinyfont, c2);
            currentY += 25;

            DrawStatItem(sb, rightColX, rightValX, ref currentY, "Last Backup", lastBackupDate, new Color(150, 150, 150), false, false);
            Color errCol = errorCount > 0 ? (errorCount > 5 ? Color.Red : Color.Orange) : Color.LightGreen;
            DrawStatItem(sb, rightColX, rightValX, ref currentY, "Error Count", errorCount.ToString(), errCol, false, false);

            // Footer status bar
            int footerY = contentRect.Y + contentRect.Height - 20;
            Rectangle footerBg = new Rectangle(contentRect.X, footerY - 5, contentRect.Width, 35);
            sb.Draw(Utils.white, footerBg, new Color(30, 30, 30) * 0.8f);
            string curTime = DateTime.Now.ToString("HH:mm:ss");
            string footText = $"System Status: {statusMessage} | {curTime}";
            Vector2 fs = GuiData.tinyfont.MeasureString(footText);
            if (fs.X > contentRect.Width - 20)
            {
                footText = $"{statusMessage} | {curTime}";
                fs = GuiData.tinyfont.MeasureString(footText);
                if (fs.X > contentRect.Width - 20)
                {
                    string shortStatus = statusMessage.Length > 20 ? statusMessage.Substring(0, 17) + "..." : statusMessage;
                    footText = $"{shortStatus} | {curTime}";
                }
            }
            TextItem.doFontLabel(new Vector2(contentRect.X + (contentRect.Width - fs.X) / 2, footerY), footText, GuiData.tinyfont, new Color(140, 140, 140));
        }

        // ---------- Drawing ----------
        private void DrawStatItem(SpriteBatch sb, int colX, int valX, ref int y, string label, string value,
            Color valueColor, bool showArrow, bool increasing)
        {
            TextItem.doFontLabel(new Vector2(colX, y), label + ":", GuiData.tinyfont, new Color(120, 120, 120));
            Vector2 vs = GuiData.tinyfont.MeasureString(value);
            TextItem.doFontLabel(new Vector2(valX - vs.X, y), value, GuiData.tinyfont, valueColor);
            if (showArrow)
            {
                string arrow = increasing ? "↑" : "↓";
                Color ac = increasing ? arrowUpColor : arrowDownColor;
                TextItem.doFontLabel(new Vector2(valX + 10, y), arrow, GuiData.tinyfont, ac);
            }
            y += 25;
        }

        private void DrawProgressTickMarks(Rectangle contentRect, SpriteBatch sb)
        {
            for (int i = 0; i <= 10; i++)
            {
                int x = contentRect.X + (int)(contentRect.Width * (i / 10f));
                Rectangle tick = new Rectangle(x - 1, contentRect.Y + 60, 2, 40);
                sb.Draw(Utils.white, tick, new Color(200, 200, 200) * 0.3f);
                if (i % 2 == 0)
                {
                    string mark = $"{i * 10}%";
                    Vector2 ms = GuiData.tinyfont.MeasureString(mark);
                    TextItem.doFontLabel(new Vector2(x - ms.X / 2, contentRect.Y + 105), mark, GuiData.tinyfont, new Color(150, 150, 150) * 0.5f);
                }
            }
        }

        private void DrawBackground(Rectangle bounds, SpriteBatch sb)
        {
            Color bg = new Color(20, 20, 20);
            Color line = new Color(40, 40, 40);
            sb.Draw(Utils.white, bounds, bg);
            for (int x = bounds.X; x < bounds.X + bounds.Width; x += 30)
                sb.Draw(Utils.white, new Rectangle(x, bounds.Y, 1, bounds.Height), line);
            for (int y = bounds.Y; y < bounds.Y + bounds.Height; y += 30)
                sb.Draw(Utils.white, new Rectangle(bounds.X, y, bounds.Width, 1), line);
        }

        // ---------- Progress Logic ----------
        private void UpdateProgress()
        {
            if (os == null) return;
            float currentTime = (float)os.timer;
            float prev = progress;
            if (lastUpdateTime == 0f) { lastUpdateTime = currentTime; return; }
            float dt = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;
            if (dt > 0.1f) dt = 0.1f;
            if (isChanging && Math.Abs(progress - targetProgress) > 0.0001f)
            {
                float change = changeRate * dt;
                if (progress < targetProgress)
                    progress = Math.Min(progress + change, targetProgress);
                else if (progress > targetProgress)
                    progress = Math.Max(progress - change, targetProgress);
                if (Math.Abs(progress - targetProgress) <= 0.0001f)
                {
                    progress = targetProgress;
                    isChanging = false;
                }
            }
            CheckAndUpdateCompletionFlag(prev);
        }

        private void CheckAndUpdateCompletionFlag(float prev)
        {
            if (os?.Flags == null) return;
            bool was = prev >= 0.9999f;
            bool now = progress >= 0.9999f;
            if (!was && now && !os.Flags.HasFlag(completionFlagName))
                os.Flags.AddFlag(completionFlagName);
            else if (was && !now && os.Flags.HasFlag(completionFlagName))
                os.Flags.RemoveFlag(completionFlagName);
        }

        private float CalculateRemainingSeconds()
        {
            if (!isChanging || changeRate == 0f) return -1f;
            float rem = Math.Abs(targetProgress - progress);
            float rate = Math.Abs(changeRate);
            return (rate > 0) ? rem / rate : -1f;
        }

        private Color GetProgressColor(float p)
        {
            int idx = (int)(p * 99);
            if (idx >= 99) return progressColors[99];
            if (idx < 0) return progressColors[0];
            float exact = p * 100f;
            float frac = exact - (int)exact;
            if (frac < 0.01f || idx >= 98) return progressColors[idx];
            return Color.Lerp(progressColors[idx], progressColors[idx + 1], frac);
        }

        // ---------- Colors ----------
        private Color GetValueColor(float value, float danger, float warning)
        {
            if (value > danger) return Color.Red;
            if (value > warning) return Color.Orange;
            if (value > warning * 0.6f) return Color.Yellow;
            return Color.LightGreen;
        }

        private Color GetLatencyColor(float latency)
        {
            if (latency > 60) return Color.Red;
            if (latency > 45) return Color.Orange;
            if (latency > 30) return Color.Yellow;
            return Color.LightGreen;
        }

        private Color GetConnectionColor(int connections)
        {
            if (connections > 35) return Color.Red;
            if (connections > 25) return Color.Orange;
            if (connections > 15) return Color.Yellow;
            return Color.Cyan;
        }

        private Color GetTemperatureColor(float temp)
        {
            if (temp > 62) return Color.Red;
            if (temp > 57) return Color.Orange;
            if (temp > 52) return Color.Yellow;
            return Color.Cyan;
        }

        private Color GetSecurityColor(string level)
        {
            switch (level)
            {
                case "A+": case "A": return Color.Green;
                case "A-": case "B+": return Color.LightGreen;
                case "B": return Color.Yellow;
                case "B-": case "C+": return Color.Orange;
                case "C": return Color.OrangeRed;
                default: return Color.Red;
            }
        }

        private Color GetCacheHitColor(float rate)
        {
            if (rate > 97f) return Color.Green;
            if (rate > 92f) return Color.LightGreen;
            if (rate > 85f) return Color.Yellow;
            if (rate > 70f) return Color.Orange;
            return Color.OrangeRed;
        }

        private Color GetBandwidthColor(float inBW, float outBW)
        {
            float total = inBW + outBW;
            if (total > 22f) return Color.OrangeRed;
            if (total > 16f) return Color.Orange;
            if (total > 10f) return Color.Yellow;
            return Color.LightBlue;
        }

        // ---------- Control Methods ----------
        public void IncreaseToTarget(float targetPercent, float ratePerSecond)
        {
            targetProgress = MathHelper.Clamp(targetPercent / 100f, 0f, 1f);
            changeRate = ratePerSecond / 100f;
            isChanging = true;
        }

        public void DecreaseToTarget(float targetPercent, float ratePerSecond)
        {
            targetProgress = MathHelper.Clamp(targetPercent / 100f, 0f, 1f);
            changeRate = -ratePerSecond / 100f;
            isChanging = true;
        }

        public void Pause() => isChanging = false;

        public void Resume()
        {
            if (Math.Abs(progress - targetProgress) > 0.0001f)
                isChanging = true;
        }

        public void Reset()
        {
            float prev = progress;
            progress = 0f;
            targetProgress = 0f;
            isChanging = false;
            lastUpdateTime = os?.timer ?? 0f;
            isRunning = false;
            progressStartTime = null;
            realTimeUptime = 0f;
            CheckAndUpdateCompletionFlag(prev);
        }
    }
}