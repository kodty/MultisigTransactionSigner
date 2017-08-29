using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharp2nem;
using CSharp2nem.Connectivity;
using CSharp2nem.Utils;
using CSharp2nem.ResponseObjects.Transaction;
using CSharp2nem.Model.AccountSetup;
using CSharp2nem.RequestClients;

namespace XEMSign
{
    internal class RuleScanner
    {
        private static Connection Con = new Connection();

        private readonly List<string> _white = new List<string>();

        private readonly List<string> _black = new List<string>();

        public RuleScanner()
        {
           var whiteList = ConfigurationManager.GetSection("accountsWhiteList")
               as MyConfigSection;

           if (whiteList != null)
               foreach (MyConfigInstanceElement b in whiteList.Instances)
               {           
                   _white.Add(StringUtils.GetResultsWithoutHyphen(b.Code));
               }
            
            var blackList = ConfigurationManager.GetSection("accountsBlackList")
                as MyConfigSection;

            if (blackList == null) return;
            {
                foreach (MyConfigInstanceElement b in blackList.Instances)
                {
                    _black.Add(StringUtils.GetResultsWithoutHyphen(b.Code));
                }
            }
        }

        internal  bool ScanTxAgainstRuleSet(Transactions.TransactionData t)
        {
            if (ConfigurationManager.AppSettings["network"] == "test") Con.SetTestnet();
            else Con.SetMainnet();

            var multisigAccInfo = new AccountClient(Con);

            var valid = true;

            multisigAccInfo.BeginGetAccountInfoFromPublicKey(ar => {


                if (_black.Count > 0 && _black.Contains(StringUtils.GetResultsWithoutHyphen(t.transaction.otherTrans.recipient)))
                {
                    Console.WriteLine("failed on blacklist");
                    valid = false;
                }
            
                if (_white.Count > 0 && !_white.Contains(StringUtils.GetResultsWithoutHyphen(t.transaction.otherTrans.recipient)))
                {
                        Console.WriteLine("failed on white list");
                        valid = false;
                }

            
                if (long.Parse(ConfigurationManager.AppSettings["maxTx"]) != 0 && t.transaction.otherTrans.amount >= long.Parse(ConfigurationManager.AppSettings["maxTx"]) * 1000000)
                {
                    Console.WriteLine("failed max transaction size");
                    valid = false;
                }

                if (long.Parse(ConfigurationManager.AppSettings["minTx"]) != 0 &&t.transaction.otherTrans.amount <= long.Parse(ConfigurationManager.AppSettings["minTx"]) * 1000000)
                {
                    Console.WriteLine("failed on min transaction size");
                    valid = false;
                }

                if (long.Parse(ConfigurationManager.AppSettings["maxBal"]) != 0 && ar.Content.Account.Balance >= long.Parse(ConfigurationManager.AppSettings["maxBal"]) * 1000000)
                {
                    Console.WriteLine("failed on max balance");
                    valid = false;
                }

                if (long.Parse(ConfigurationManager.AppSettings["minBal"]) != 0 && ar.Content.Account.Balance <= long.Parse(ConfigurationManager.AppSettings["minBal"]) * 1000000)
                {
                    Console.WriteLine("failed on min balance");
                    valid = false;
                }

                if (ConfigurationManager.AppSettings["secretCode"] != "" && t.transaction.otherTrans.message.payload != ConfigurationManager.AppSettings["secretCode"])
                {
                    Console.WriteLine("failed on secret code");
                    valid = false;
                }

                var sum1 = TxSummaryController.GetSummaryForAccount(ar.Content.Account.Address, 1);
            
                if (int.Parse(ConfigurationManager.AppSettings["maxDayTx"]) != 0 && sum1 != null && sum1.Count >= int.Parse(ConfigurationManager.AppSettings["maxDayTx"]))
                {
                    Console.WriteLine("failed on daily max transactions");
                    valid = false;
                }

                var sum7 = TxSummaryController.GetSummaryForAccount(ar.Content.Account.Address, 7);

                if (int.Parse(ConfigurationManager.AppSettings["maxWeekTx"]) != 0 && sum7 != null && sum7.Count >= int.Parse(ConfigurationManager.AppSettings["maxWeekTx"]))
                {
                    Console.WriteLine("failed on weekly max transactions");
                    valid = false;
                }

                var sum31 = TxSummaryController.GetSummaryForAccount(ar.Content.Account.Address, 31);

                if (int.Parse(ConfigurationManager.AppSettings["maxMonthTx"]) != 0 && sum31 != null && sum31.Count >= int.Parse(ConfigurationManager.AppSettings["maxMonthTx"]))
                {
                    Console.WriteLine("failed on monthly max transactions");
                    valid = false;
                }

                if (long.Parse(ConfigurationManager.AppSettings["maxDayAmount"]) != 0 && sum1 != null && sum1.Sum(tx => tx.Amount) >= long.Parse(ConfigurationManager.AppSettings["maxDayAmount"]) * 1000000)
                {
                    Console.WriteLine("failed on daily max transactions");
                    valid = false;
                }

                if (long.Parse(ConfigurationManager.AppSettings["maxWeekAmount"]) != 0 && sum7 != null && sum7.Sum(tx => tx.Amount) >= long.Parse(ConfigurationManager.AppSettings["maxWeekAmount"]) * 1000000)
                {
                    Console.WriteLine("failed on weekly max transactions");
                    valid = false;
                }
            
                if (long.Parse(ConfigurationManager.AppSettings["maxMonthAmount"]) != 0 && sum31 != null && sum31.Sum(tx => tx.Amount) >= long.Parse(ConfigurationManager.AppSettings["maxMonthAmount"]) * 1000000)
                {
                    Console.WriteLine("failed on monthly max transactions");
                    valid = false;
                }
            }, t.transaction.otherTrans.signer).AsyncWaitHandle.WaitOne();

            return valid;
        }
    }
}
