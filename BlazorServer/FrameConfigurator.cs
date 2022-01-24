using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Core;
using System.Threading;
using SharedLib;
using Game;

namespace BlazorServer
{
    public sealed class FrameConfigurator : IDisposable
    {
        private readonly ILogger logger;

        private readonly DataConfig dataConfig;

        private WowProcess? wowProcess;
        private WowScreen? wowScreen;

        private readonly AddonConfig addonConfig;
        private readonly AddonConfigurator addonConfigurator;

        public DataFrameMeta dataFrameMeta { get; private set; } = DataFrameMeta.Empty;

        public List<DataFrame> dataFrames { get; private set; } = new List<DataFrame>();

        private IAddonDataProvider? addonDataProvider;
        public AddonReader? AddonReader { get; private set; }

        public bool Saved { get; private set; }
        public bool AddonNotVisible { get; private set; }

        public string ImageBase64 { private set; get; } = "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==";

        private Thread? screenshotThread;
        private CancellationTokenSource? cts;

        private const int interval = 500;
        private int counter;

        public event EventHandler? OnUpdate;

        public FrameConfigurator(ILogger logger)
        {
            this.logger = logger;

            dataConfig = DataConfig.Load();
            addonConfig = AddonConfig.Load();
            addonConfigurator = new AddonConfigurator(logger, addonConfig);
        }

        public void Dispose()
        {
            cts?.Cancel();
            wowScreen?.Dispose();
        }

        private void ScreenshotRefreshThread()
        {
            while (cts != null && !cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (wowProcess != null && wowScreen != null)
                    {
                        if (dataFrameMeta == DataFrameMeta.Empty)
                        {
                            AddonNotVisible = false;
                            dataFrameMeta = GetDataFrameMeta();

                            OnUpdate?.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            var temp = GetDataFrameMeta();
                            if (temp != DataFrameMeta.Empty && temp.rows != dataFrameMeta.rows)
                            {
                                AddonNotVisible = true;
                                dataFrameMeta = DataFrameMeta.Empty;

                                OnUpdate?.Invoke(this, EventArgs.Empty);
                            }
                        }

                        if (dataFrameMeta != DataFrameMeta.Empty)
                        {
                            wowScreen.GetRectangle(out var screenRect);

                            if (screenRect.Location.X < 0 || screenRect.Location.Y < 0)
                            {
                                logger.LogWarning($"Client window outside of the visible area of the screen {screenRect.Location}");
                                return;
                            }

                            var addonRect = dataFrameMeta.EstimatedSize(screenRect);

                            if (!addonRect.IsEmpty &&
                                addonRect.Width <= screenRect.Size.Width &&
                                addonRect.Height <= screenRect.Size.Height &&
                                addonRect.Height < 50) // this one just arbitrary number for sanity check
                            {
                                var screenshot = wowScreen.GetBitmap(addonRect.Width, addonRect.Height);
                                if (screenshot != null)
                                {
                                    UpdatePreview(screenshot);

                                    if (dataFrameMeta == DataFrameMeta.Empty)
                                    {
                                        dataFrameMeta = DataFrameConfiguration.GetMeta(screenshot);
                                    }

                                    if (dataFrames.Count != dataFrameMeta.frames)
                                    {
                                        dataFrames = DataFrameConfiguration.CreateFrames(dataFrameMeta, screenshot);
                                    }
                                    screenshot.Dispose();

                                    if (dataFrames.Count == dataFrameMeta.frames)
                                    {
                                        if (AddonReader != null)
                                        {
                                            AddonReader.Dispose();
                                            addonDataProvider = null;
                                        }

                                        if (addonDataProvider == null)
                                        {
                                            addonDataProvider = new AddonDataProvider(wowScreen, dataFrames);
                                        }

                                        AddonReader = new AddonReader(logger, dataConfig, addonDataProvider);
                                    }

                                    OnUpdate?.Invoke(this, EventArgs.Empty);
                                }
                            }
                            else
                            {
                                AddonNotVisible = true;
                                dataFrameMeta = DataFrameMeta.Empty;

                                OnUpdate?.Invoke(this, EventArgs.Empty);
                            }
                        }
                    }
                    else
                    {
                        wowProcess = new WowProcess();
                        wowScreen = new WowScreen(logger, wowProcess);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, e.StackTrace);
                    AddonNotVisible = true;
                    dataFrameMeta = DataFrameMeta.Empty;

                    OnUpdate?.Invoke(this, EventArgs.Empty);
                }

                counter++;
                Thread.Sleep(interval);
            }

            cts?.Dispose();
            cts = null;
        }

        private DataFrameMeta GetDataFrameMeta()
        {
            System.Drawing.Point location = System.Drawing.Point.Empty;
            wowScreen?.GetPosition(out location);
            if (location.X < 0)
            {
                logger.LogWarning($"Client window outside of the visible area of the screen by {location}");
                return DataFrameMeta.Empty;
            }

            var screenshot = wowScreen?.GetBitmap(5, 5);
            if (screenshot == null) return DataFrameMeta.Empty;
            return DataFrameConfiguration.GetMeta(screenshot);
        }

        public async Task ToggleManualConfig()
        {
            if (cts == null)
            {
                cts = new CancellationTokenSource();
                screenshotThread = new Thread(ScreenshotRefreshThread);
                screenshotThread.Start();
            }
            else
            {
                cts?.Cancel();
            }

            await Task.Delay(0);
        }

        public async Task<bool> FinishManualConfig()
        {
            await Task.Delay(0);

            var version = addonConfigurator?.GetInstalledVersion();
            if (version == null) return false;

            if (dataFrames.Count != dataFrameMeta.frames)
            {
                return false;
            }

            if (wowScreen == null) return false;
            wowScreen.GetRectangle(out var rect);

            DataFrameConfiguration.SaveConfiguration(rect, version, dataFrameMeta, dataFrames);
            Saved = true;

            OnUpdate?.Invoke(this, EventArgs.Empty);

            return true;
        }

        public async Task<bool> StartAutoConfig()
        {
            if (wowProcess == null)
                wowProcess = new WowProcess();

            if (wowScreen == null && wowProcess != null)
                wowScreen = new WowScreen(logger, wowProcess);

            if (wowProcess == null) return false;
            logger.LogInformation("Found WowProcess");

            if (wowScreen == null) return false;
            wowScreen.GetPosition(out var location);

            if (location.X < 0)
            {
                logger.LogWarning($"Please make sure the client window does not outside of the visible area! Currently outside by {location}");
                return false;
            }

            wowScreen.GetRectangle(out var rect);
            logger.LogInformation($"Found WowScreen Location: {location} - Size: {rect}");

            var wowProcessInput = new WowProcessInput(logger, wowProcess);
            var execGameCommand = new ExecGameCommand(logger, wowProcessInput);

            var version = addonConfigurator?.GetInstalledVersion();
            if (version == null) return false;
            logger.LogInformation($"Addon installed. Version {version}");

            wowProcessInput.SetForegroundWindow();
            await Task.Delay(100);

            var meta = GetDataFrameMeta();
            if (meta == DataFrameMeta.Empty || meta.hash == 0)
            {
                logger.LogInformation("Enter configuration mode.");

                await ToggleInGameConfiguration(execGameCommand);
                await Wait();
                meta = GetDataFrameMeta();
            }

            if (meta == DataFrameMeta.Empty)
            {
                logger.LogWarning("Unable to enter configuration mode! You most likely running the game with admin privileges! Please restart the game without it!");
                return false;
            }

            logger.LogInformation($"DataFrameMeta: hash: {meta.hash} | spacing: {meta.spacing} | size: {meta.size} | rows: {meta.rows} | frames: {meta.frames}");

            var size = meta.EstimatedSize(rect);
            if (size.Height > 50 || size.IsEmpty)
            {
                logger.LogWarning($"Something is worng. esimated size: {size}.");
                return false;
            }

            var screenshot = wowScreen.GetBitmap(size.Width, size.Height);
            if (screenshot == null) return false;

            logger.LogInformation($"Found cells - {rect} - estimated size {size}");

            UpdatePreview(screenshot);

            OnUpdate?.Invoke(this, EventArgs.Empty);
            await Wait();

            var dataFrames = DataFrameConfiguration.CreateFrames(meta, screenshot);
            if (dataFrames.Count != meta.frames)
            {
                return false;
            }

            logger.LogInformation($"Exit configuration mode.");
            await ToggleInGameConfiguration(execGameCommand);
            await Wait();

            addonDataProvider?.Dispose();
            AddonReader?.Dispose();

            addonDataProvider = new AddonDataProvider(wowScreen, dataFrames);
            AddonReader = new AddonReader(logger, dataConfig, addonDataProvider);

            if (!ResolveClass())
            {
                logger.LogError("Unable to find class.");
                return false;
            }

            logger.LogInformation("Found Class!");

            OnUpdate?.Invoke(this, EventArgs.Empty);
            await Wait();

            DataFrameConfiguration.SaveConfiguration(rect, version, meta, dataFrames);
            Saved = true;

            logger.LogInformation($"Frame configuration was successful! Configuration saved!");

            OnUpdate?.Invoke(this, EventArgs.Empty);
            await Wait();

            return true;
        }

        private async Task ToggleInGameConfiguration(ExecGameCommand execGameCommand)
        {
            await execGameCommand.Run($"/{addonConfig.Command}");
        }

        private void UpdatePreview(System.Drawing.Bitmap screenshot)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                screenshot.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                this.ImageBase64 = Convert.ToBase64String(ms.ToArray());
            }
        }


        public bool ResolveClass()
        {
            if (AddonReader != null)
            {
                AddonReader.Refresh();
                return Enum.GetValues(typeof(PlayerClassEnum)).Cast<PlayerClassEnum>().Contains(AddonReader.PlayerReader.Class);
            }
            return false;
        }

        public async Task Wait()
        {
            if (cts != null)
            {
                var temp = counter;
                do
                {
                    await Task.Delay(100);
                } while (temp == counter);
            }
            else
            {
                await Task.Delay(interval);
            }
        }

    }
}
