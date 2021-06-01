using System;
using System.Threading.Tasks;

namespace swagger_fetch {
  class Program {
    public static async Task Main(string[] args) {
      try {
        var result = new Parser(args[0]);
        await result.Process();
      }

      catch (Exception e) {
        Console.WriteLine("Something is wrong - \n\n" + e.ToString());
      }
    }
  }
}
