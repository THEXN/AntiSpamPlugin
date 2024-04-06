using System;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace AntiSpamPlugin
{
    [ApiVersion(2, 1)]
    public class AntiSpamPlugin : TerrariaPlugin
    {
        private readonly Dictionary<int, PacketStats> playerStats = new Dictionary<int, PacketStats>();
        private DateTime lastDetectionTime = DateTime.Now;
        public static Configuration Config;

        public AntiSpamPlugin(Main game) : base(game)
        {
            LoadConfig();
        }

        public override void Initialize()
        {
            GeneralHooks.ReloadEvent += ReloadConfig;
            ServerApi.Hooks.NetSendData.Register(this, OnSendData);
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        }

        private static void LoadConfig()
        {
            Config = Configuration.Read(Configuration.FilePath);
            Config.Write(Configuration.FilePath);
        }

        private static void ReloadConfig(ReloadEventArgs args)
        {
            LoadConfig();
            args.Player?.SendSuccessMessage("[{0}] 重新加载配置完毕。", typeof(AntiSpamPlugin).Name);
        }

        private void OnSendData(SendDataEventArgs args)
        {
            if (args.remoteClient < 0 || args.remoteClient >= 255)
                return;

            int playerIndex = args.remoteClient;

            if (!playerStats.TryGetValue(playerIndex, out PacketStats stats))
            {
                stats = new PacketStats();
                playerStats[playerIndex] = stats;
            }

            stats.AddPacket();
        }

        private void OnGameUpdate(EventArgs args)
        {
            double elapsedSecondsDetection = (DateTime.Now - lastDetectionTime).TotalSeconds;

            if (elapsedSecondsDetection >= Config.检测时间)
            {
                lastDetectionTime = DateTime.Now;

                foreach (var kvp in new Dictionary<int, PacketStats>(playerStats))
                {
                    int playerIndex = kvp.Key;
                    PacketStats stats = kvp.Value;

                    if (stats.PacketCount > Config.最多包数量)
                    {
                        KickPlayer(playerIndex);
                    }
                    stats.Reset();
                }
            }
        }

        private void KickPlayer(int playerIndex)
        {
            TSPlayer tsplayer = TShock.Players[playerIndex];
            string playerName = tsplayer?.Name ?? "Unknown";

            string kickMessage = "由于发送过多数据包，您已被踢出服务器。";
            tsplayer?.SendErrorMessage(kickMessage);
            tsplayer?.Kick("由于发送过多数据包，您已被踢出服务器。");
        }

        private class PacketStats
        {
            private int packetCount;
            private DateTime lastResetTime;

            public int PacketCount => packetCount;
            public double ElapsedSeconds => (DateTime.Now - lastResetTime).TotalSeconds;

            public void AddPacket()
            {
                packetCount++;
            }

            public void Reset()
            {
                packetCount = 0;
                lastResetTime = DateTime.Now;
            }
        }
    }
}



