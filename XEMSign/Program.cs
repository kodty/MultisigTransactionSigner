using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace XEMSign
{
    class Program
    {
        private static Task T { get; set; }
        private static AccountScanner AccountScanner { get; set; }
        static void Main(string[] args)
        {

            Console.WriteLine(
                "Scanning for transactions... " +
                "Any pending transactions will be displayed here, " +
                "if they were signed not, and why.\n\n " +
                "Hit any key to exit."
                );

            var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var specificFolder = Path.Combine(folder, "XEMSign");

            if (!Directory.Exists(specificFolder))
            {             
                Directory.CreateDirectory(specificFolder);                 
            }

            if (!File.Exists(Path.Combine(specificFolder, "XEMSignJsonData.txt")))
            {
                var jsonList = new List<TxSummary>();

                var json = JsonConvert.SerializeObject(jsonList);
               
                File.WriteAllText(Path.Combine(specificFolder, "XEMSignJsonData.txt"), json);
            }

            AccountScanner = new AccountScanner();

            T = Task.Run(() => AccountScanner.ScanAccounts());

            Console.ReadLine();
        }
    }
}
