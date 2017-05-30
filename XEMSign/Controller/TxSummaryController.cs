using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace XEMSign
{
    internal static class TxSummaryController
    {
       
        internal static List<TxSummary> GetSummaryForAccount(string acc, int days = 0)
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var specificFolder = Path.Combine(folder, "XEMSign");

            var path = Path.Combine(specificFolder, "XEMSignJsonData.txt");

            var data = File.ReadAllText(path);

            var summaries = JsonConvert.DeserializeObject<List<TxSummary>>(data);
           
            return days > 0 ? summaries.Where(e => e.DateOfTx > DateTime.Now.AddDays(-days)).ToList() : summaries;
        }

        internal static void AddSummaryForAccount(TxSummary summary)
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var specificFolder = Path.Combine(folder, "XEMSign");

            var path = Path.Combine(specificFolder, "XEMSignJsonData.txt");

            var data = File.ReadAllText(path);

            var summaries = JsonConvert.DeserializeObject<List<TxSummary>>(data);
            
            summaries.Add(summary);
            
            var json = JsonConvert.SerializeObject(summaries);
           
            File.WriteAllText(path, json);
        }

        internal static void CleanSummaries()
        {
            
        }
    }
}
