using Hacknet;
using Pathfinder.Action;
using Pathfinder.Util;
using SRPortToolkit.Daemon;

namespace SRPortToolkit.Actions
{
    public class ProgressBarAction
    {
        // 按指定速率增加到目标值
        public class IncreaseToTarget : DelayablePathfinderAction
        {
            [XMLStorage]
            public string targetCompId;

            [XMLStorage]
            public int daemonId;

            [XMLStorage]
            public float targetPercent;  // 目标百分比

            [XMLStorage]
            public float ratePerSecond;  // 每秒增加的百分比

            public override void Trigger(OS os)
            {
                Computer comp = ComputerLookup.FindById(targetCompId);
                if (comp == null) return;

                foreach (var daemon in comp.daemons)
                {
                    if (daemon is ProgressBarDaemon progDaemon && progDaemon.Id == daemonId)
                    {
                        progDaemon.IncreaseToTarget(targetPercent, ratePerSecond);
                        break;
                    }
                }
            }
        }

        // 按指定速率减少到目标值
        public class DecreaseToTarget : DelayablePathfinderAction
        {
            [XMLStorage]
            public string targetCompId;

            [XMLStorage]
            public int daemonId;

            [XMLStorage]
            public float targetPercent;  // 目标百分比

            [XMLStorage]
            public float ratePerSecond;  // 每秒减少的百分比

            public override void Trigger(OS os)
            {
                Computer comp = ComputerLookup.FindById(targetCompId);
                if (comp == null) return;

                foreach (var daemon in comp.daemons)
                {
                    if (daemon is ProgressBarDaemon progDaemon && progDaemon.Id == daemonId)
                    {
                        progDaemon.DecreaseToTarget(targetPercent, ratePerSecond);
                        break;
                    }
                }
            }
        }

        // 暂停变化
        public class Pause : DelayablePathfinderAction
        {
            [XMLStorage]
            public string targetCompId;

            [XMLStorage]
            public int daemonId;

            public override void Trigger(OS os)
            {
                Computer comp = ComputerLookup.FindById(targetCompId);
                if (comp == null) return;

                foreach (var daemon in comp.daemons)
                {
                    if (daemon is ProgressBarDaemon progDaemon && progDaemon.Id == daemonId)
                    {
                        progDaemon.Pause();
                        break;
                    }
                }
            }
        }

        // 继续变化
        public class Resume : DelayablePathfinderAction
        {
            [XMLStorage]
            public string targetCompId;

            [XMLStorage]
            public int daemonId;

            public override void Trigger(OS os)
            {
                Computer comp = ComputerLookup.FindById(targetCompId);
                if (comp == null) return;

                foreach (var daemon in comp.daemons)
                {
                    if (daemon is ProgressBarDaemon progDaemon && progDaemon.Id == daemonId)
                    {
                        progDaemon.Resume();
                        break;
                    }
                }
            }
        }

        // 重置到0%
        public class Reset : DelayablePathfinderAction
        {
            [XMLStorage]
            public string targetCompId;

            [XMLStorage]
            public int daemonId;

            public override void Trigger(OS os)
            {
                Computer comp = ComputerLookup.FindById(targetCompId);
                if (comp == null) return;

                foreach (var daemon in comp.daemons)
                {
                    if (daemon is ProgressBarDaemon progDaemon && progDaemon.Id == daemonId)
                    {
                        progDaemon.Reset();
                        break;
                    }
                }
            }
        }
    }
}