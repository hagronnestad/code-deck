using CodeDeck.PluginAbstractions;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class CounterStrikeGlobalOffensiveNetCon : CodeDeckPlugin
{
    public class ExecuteCommand : Tile
    {
        [Setting] public string HostName { get; set; } = "127.0.0.1";
        [Setting] public int NetConPort { get; set; }
        [Setting] public string? Command { get; set; }

        public override async Task Init(CancellationToken cancellationToken)
        {
            Text = $"CS:GO\nExecCmd";
            await Task.CompletedTask;
        }

        public override async Task OnTilePressUp(CancellationToken cancellationToken)
        {
            if (Command is null) return;

            try
            {
                ShowIndicator = true;

                // Give ShowIndicator time to show for some visual feedback
                await Task.Delay(100, cancellationToken);

                // Connect to netcon and send the command
                using var c = new TcpClient();
                await c.ConnectAsync(HostName, NetConPort, cancellationToken);
                using var s = c.GetStream();
                using var tw = new StreamWriter(s);
                tw.AutoFlush = true;
                await tw.WriteLineAsync(new StringBuilder(Command), cancellationToken);
                c.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");
            }
            finally
            {
                ShowIndicator = false;
            }
        }
    }
}
