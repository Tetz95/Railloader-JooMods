using Game;
using Game.Messages;
using Game.State;
using Network;
using Railloader;
using Serilog;
using System.Threading;

namespace TimeSyncMod
{
    public class TimeSyncMod : PluginBase
    {
        Serilog.ILogger logger = Log.ForContext<TimeSyncMod>();

        private readonly Timer? syncTimer;

        // Mods are constructed on Unity's main thread; game state may only be touched there
        private readonly SynchronizationContext? mainThread;

        public TimeSyncMod(IModdingContext moddingContext, IModDefinition self)
        {
            logger.Information("Registering time sync command!");

            moddingContext.RegisterConsoleCommand(new TimeSyncCommand());

            mainThread = SynchronizationContext.Current;
            if (mainThread == null)
            {
                logger.Warning("No main thread context, automatic time sync disabled");
                return;
            }

            syncTimer = new Timer(_ => mainThread.Post(SyncTimes, null), null, 1000 * 60 * 10, 1000 * 60 * 10);
        }

        public void SyncTimes(object stateInfo)
        {
            if (!Multiplayer.IsHost || Multiplayer.Mode != ConnectionMode.MultiplayerServer)
            {
                return;
            }

            GameDateTime timeCursor = TimeWeather.Now;
            StateManager.ApplyLocal(new SetTimeOfDay((float)timeCursor.TotalSeconds));
            logger.Information("Synced time to all clients");
        }
    }
}
