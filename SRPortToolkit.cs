using BepInEx;
using BepInEx.Hacknet;
using Pathfinder.Executable;
using RedisBreaker;
using AutoCrackFirewall;
using SRPortToolkit.Daemon;
using System;
using SRPortToolkit.Actions;
using Pathfinder.Action;
using Pathfinder.Event.Loading; 
using Pathfinder.Event;        

namespace SRPortToolkit
{
    [BepInPlugin(ModGUID, ModName, ModVer)]
    public class SRPortToolkit : HacknetPlugin
    {
        public const string ModGUID = "com.ZQG.SRPortToolkit";
        public const string ModName = "SRPortToolkit";
        public const string ModVer = "1.1.0";

        public override bool Load()
        {
            LogDebug("Loading Ports...");
            Pathfinder.Port.PortManager.RegisterPort("IMP", "Internet Message Protocol", 143);
            Pathfinder.Port.PortManager.RegisterPort("DNS", "DNS Service", 53);
            Pathfinder.Port.PortManager.RegisterPort("RPC", "Remote Procedure Call,", 135);
            Pathfinder.Port.PortManager.RegisterPort("Redis", "Redis Service", 6379);
            Pathfinder.Port.PortManager.RegisterPort("NFS", "NFSV3 Service", 2049);
            Pathfinder.Port.PortManager.RegisterPort("LDAP", "LDAP Service", 389);
            Pathfinder.Port.PortManager.RegisterPort("ES", "Extension Service", 9400);
            Pathfinder.Port.PortManager.RegisterPort("ZS", "ZQG Service", 8235);
            LogDebug("Loading Ports Complete");

            LogDebug("Loading Daemons...");
            Pathfinder.Daemon.DaemonManager.RegisterDaemon<DatabaseDaemonPlus>();
            Pathfinder.Daemon.DaemonManager.RegisterDaemon<ProgressBarDaemon>();
            LogDebug("Loading Daemons Complete");

            LogDebug("Loading Actions...");
            ActionManager.RegisterAction<ProgressBarAction.IncreaseToTarget>("IncreaseToTarget");
            ActionManager.RegisterAction<ProgressBarAction.DecreaseToTarget>("DecreaseToTarget");
            ActionManager.RegisterAction<ProgressBarAction.Pause>("Pause");
            ActionManager.RegisterAction<ProgressBarAction.Resume>("Resume");
            ActionManager.RegisterAction<ProgressBarAction.Reset>("Reset");
            LogDebug("Loading Actions Complete");

            LogDebug("Loading Executable...");
            ExecutableManager.RegisterExecutable<AutoCrackFirewallExe>("#FIREWALL_CRACK#");
            ExecutableManager.RegisterExecutable<RedisBreakerExe>("#REDIS_EXE#");
            LogDebug("Loading Executable Complete");
            HarmonyInstance.PatchAll();

            EventManager<OSLoadedEvent>.AddHandler(OnOSLoaded);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[SRPortToolkit] Loading Complete");
            Console.WriteLine("//////////////////////////////////// ZQG //////////////////////////////////");
            Console.WriteLine("/////////////////////////////////// SR.OS /////////////////////////////////");
            Console.WriteLine("//////////////////////////////  SRPortToolkit  ////////////////////////////");
            Console.ResetColor();

            
            return true;
        }

        private void OnOSLoaded(OSLoadedEvent osEvent)
        {
            if (Hacknet.OS.DEBUG_COMMANDS)
            {
                osEvent.Os.terminal.writeLine("[SRPortToolkit] Debug mode is ENABLED.");
                osEvent.Os.terminal.writeLine("[SRPortToolkit] Please don't cheat.");
                osEvent.Os.terminal.writeLine("[SRPortToolkit] Enjoy The Extension.");
            }
            else
            {   osEvent.Os.terminal.writeLine("[SRPortToolkit] Debug mode is DISABLED.");
                osEvent.Os.terminal.writeLine("[SRPortToolkit] Enjoy The Extension.");
            }
        }

        private void LogDebug(string message)
        {
            Log.LogDebug(message);
        }
    }
}