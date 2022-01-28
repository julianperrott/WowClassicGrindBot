using Game;
using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using TextCopy;

namespace Core
{
    public class ExecGameCommand
    {
        private readonly ILogger logger;
        private readonly WowProcessInput wowProcessInput;

        public ExecGameCommand(ILogger logger, WowProcessInput wowProcessInput)
        {
            this.logger = logger;
            this.wowProcessInput = wowProcessInput;
        }

        public void Run(string content)
        {
            wowProcessInput.SetForegroundWindow();
            logger.LogInformation(content);

            ClipboardService.SetText(content);
            Thread.Sleep(50);

            // Open chat inputbox
            wowProcessInput.KeyPress(ConsoleKey.Enter, 50);

            // Send Paste keys
            wowProcessInput.PasteFromClipboard();
            Thread.Sleep(250);

            //
            wowProcessInput.KeyPress(ConsoleKey.Enter, 50);
            Thread.Sleep(250);
        }
    }
}
