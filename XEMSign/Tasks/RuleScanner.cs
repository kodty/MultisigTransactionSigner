using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharp2nem;

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
                   _white.Add(b.Code.GetResultsWithoutHyphen());
               }
            
            var blackList = ConfigurationManager.GetSection("accountsBlackList")
                as MyConfigSection;

            if (blackList == null) return;
            {
                foreach (MyConfigInstanceElement b in blackList.Instances)
                {
                    _black.Add(b.Code.GetResultsWithoutHyphen());
                }
            }
        }

        internal  bool ScanTxAgainstRuleSet(Transactions.TransactionData t, VerifiableAccount acc)
        {
            Con.SetTestNet();

            var multisigAccInfo = new AccountFactory(Con).FromPublicKey(t.transaction.otherTrans.signer);

            var accInfo = multisigAccInfo.GetAccountInfoAsync().Result;

            if (_black.Count > 0 && _black.Contains(t.transaction.otherTrans.recipient.GetResultsWithoutHyphen()))
            {
                Console.WriteLine("failed on blacklist");
                return false;
            }
            
            if (_white.Count > 0 && !_white.Contains(t.transaction.otherTrans.recipient.GetResultsWithoutHyphen()))
            {
                Console.WriteLine("failed on white list");
                return false;
            }

            
            if (long.Parse(ConfigurationManager.AppSettings["maxTx"]) != 0 && t.transaction.otherTrans.amount >= long.Parse(ConfigurationManager.AppSettings["maxTx"]) * 1000000)
            {
                Console.WriteLine("failed max transaction size");
                return false;
            }

            if (long.Parse(ConfigurationManager.AppSettings["minTx"]) != 0 &&t.transaction.otherTrans.amount <= long.Parse(ConfigurationManager.AppSettings["minTx"]) * 1000000)
            {
                Console.WriteLine("failed on min transaction size");
                return false;
            }

            if (long.Parse(ConfigurationManager.AppSettings["maxBal"]) != 0 && accInfo.Account.Balance >= long.Parse(ConfigurationManager.AppSettings["maxBal"]) * 1000000)
            {
                Console.WriteLine("failed on max balance");
                return false;
            }

            if (long.Parse(ConfigurationManager.AppSettings["minBal"]) != 0 && accInfo.Account.Balance <= long.Parse(ConfigurationManager.AppSettings["minBal"]) * 1000000)
            {
                Console.WriteLine("failed on min balance");
                return false;
            }

            if (ConfigurationManager.AppSettings["secretCode"] != "" && t.transaction.otherTrans.message.payload != ConfigurationManager.AppSettings["secretCode"])
            {
                Console.WriteLine("failed on secret code");
                return false;
            }

            var sum1 = TxSummaryController.GetSummaryForAccount(multisigAccInfo.Address.Encoded, 1);
            
            if (int.Parse(ConfigurationManager.AppSettings["maxDayTx"]) != 0 && sum1 != null && sum1.Count >= int.Parse(ConfigurationManager.AppSettings["maxDayTx"]))
            {
                Console.WriteLine("failed on daily max transactions");
                return false;
            }

            var sum7 = TxSummaryController.GetSummaryForAccount(multisigAccInfo.Address.Encoded, 7);

            if (int.Parse(ConfigurationManager.AppSettings["maxWeekTx"]) != 0 && sum7 != null && sum7.Count >= int.Parse(ConfigurationManager.AppSettings["maxWeekTx"]))
            {
                Console.WriteLine("failed on weekly max transactions");
                return false;
            }

            var sum31 = TxSummaryController.GetSummaryForAccount(multisigAccInfo.Address.Encoded, 31);

            if (int.Parse(ConfigurationManager.AppSettings["maxMonthTx"]) != 0 && sum31 != null && sum31.Count >= int.Parse(ConfigurationManager.AppSettings["maxMonthTx"]))
            {
                Console.WriteLine("failed on monthly max transactions");
                return false;
            }

            if (long.Parse(ConfigurationManager.AppSettings["maxDayAmount"]) != 0 && sum1 != null && sum1.Sum(tx => tx.Amount) >= long.Parse(ConfigurationManager.AppSettings["maxDayAmount"]) * 1000000)
            {
                Console.WriteLine("failed on daily max transactions");
                return false;
            }

            if (long.Parse(ConfigurationManager.AppSettings["maxWeekAmount"]) != 0 && sum7 != null && sum7.Sum(tx => tx.Amount) >= long.Parse(ConfigurationManager.AppSettings["maxWeekAmount"]) * 1000000)
            {
                Console.WriteLine("failed on weekly max transactions");
                return false;
            }
            
            if (long.Parse(ConfigurationManager.AppSettings["maxMonthAmount"]) != 0 && sum31 != null && sum31.Sum(tx => tx.Amount) >= long.Parse(ConfigurationManager.AppSettings["maxMonthAmount"]) * 1000000)
            {
                Console.WriteLine("failed on monthly max transactions");
                return false;
            }

            return true;
        }
    }
}
