using System.Threading.Tasks;

namespace CodeDeck.Linux
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await CodeDeck.Program.Main(args);
        }
    }
}