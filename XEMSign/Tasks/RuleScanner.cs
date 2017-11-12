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
using CSharp2nem.CryptographicFunctions;
using Newtonsoft.Json;
using Chaos.NaCl;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Text.RegularExpressions;

namespace XEMSign
{
    internal class RuleScanner
    {
        private static Connection Con = new Connection();

        private readonly List<string> _white = new List<string>();

        private readonly List<string> _black = new List<string>();

        public RuleScanner()
        {
            if (ConfigurationManager.AppSettings["network"] == "test") Con.SetTestnet();
            else Con.SetMainnet();

            var whiteList = ConfigurationManager.GetSection("accountsWhiteList")
               as MyConfigSection;

           if (whiteList != null)
               foreach (MyConfigInstanceElement b in whiteList.Instances)
               {           
                   _white.Add(StringUtils.GetResultsWithoutHyphen(b.Code));
               }
            
            var blackList = ConfigurationManager.GetSection("accountsBlackList")
                as MyConfigSection;

            if (blackList != null);
            {
                foreach (MyConfigInstanceElement b in blackList.Instances)
                {
                    _black.Add(StringUtils.GetResultsWithoutHyphen(b.Code));
                }
            }

            
        }

        internal  bool ScanTxAgainstRuleSet(Transactions.TransactionData t)
        {
            

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
                
                if(ConfigurationManager.AppSettings["ICOAccountPubKey"] != "")
                {
                    valid = false;
  
                    // get all incoming transactions to the ICO deposit account
                    var txs = GetTransactions(AddressEncoding.ToEncoded(Con.GetNetworkVersion(), new CSharp2nem.Model.AccountSetup.PublicKey(ConfigurationManager.AppSettings["ICOAccountPubKey"])), null);

                    if ((t.transaction.type == 257 ? t.transaction.message?.payload : t.transaction.otherTrans?.message?.payload) != null)
                        while (!txs.Exists(e => e.meta.hash.data == Encoding.UTF8.GetString(CryptoBytes.FromHexString((t.transaction.type == 257 ? t.transaction.message?.payload : t.transaction.otherTrans?.message?.payload)))))
                        {
                            var tx = GetTransactions(AddressEncoding.ToEncoded(Con.GetNetworkVersion(), new CSharp2nem.Model.AccountSetup.PublicKey(ConfigurationManager.AppSettings["ICOAccountPubKey"])), txs[txs.Count - 1].meta.hash.data);

                            txs.AddRange(tx);
                        }
                    else
                    {
                        valid = false;
                        Console.WriteLine("failed on transaction verification: missing message hash");
                    }
                    
                    // fail if deposit hash not present in payout transaction
                    if ((t.transaction.otherTrans == null ? t.transaction.message?.payload : t.transaction.otherTrans.message?.payload) != null)
                    {
                        // find deposit transaction based on hash in message
                        var tx = txs.Single(x => x.meta.hash.data == Encoding.UTF8.GetString(CryptoBytes.FromHexString((t.transaction.type == 257 ? t.transaction.message.payload : t.transaction.otherTrans.message.payload))));

                        // initiate mosaic client
                        var mosaicClient = new NamespaceMosaicClient(Con);

                        var mosaicDivisibility = 0;

                        var mosaics = ConfigurationManager.GetSection("MosaicConfigElement") as MyMosaicConfigSection;

                        foreach (MosaicConfigElement m in mosaics.Mosaics)
                        {
                            // retrieve mosaic definition for rate calculation
                            mosaicClient.BeginGetMosaicsByNameSpace(ar3 =>
                            {
                                foreach (var m2 in t.transaction.otherTrans.mosaics)
                                {
                                    if (!(m2.mosaicId.namespaceId == m.MosaicNameSpace && m2.mosaicId.name == m.MosaicID)) continue;

                                    if (ar3.Ex != null)
                                    {
                                        Console.WriteLine(ar3.Ex);
                                    } // if no error, set mosaic divisibility
                                    else mosaicDivisibility = int.Parse(ar3.Content.Data[0].Mosaic.Properties[0].Value);

                                    var depositedAmount = Calculations.GetDepositedAmount(tx);

                                    // calculate based on pre-set currency denomination
                                    if (m.CostDenomination == "USD")
                                    {
                                        // fail if no mosaics present in ico payout transaction
                                        if (t.transaction.otherTrans.mosaics != null)
                                        {
                                            // check if the mosaic quantity paid out is correct for the deposit
                                            var amountValid = Calculations.RateCalculation(depositedAmount, mosaicDivisibility, m) == m2.quantity;

                                            // check the recipient of the payout is the signer of the deposit, fail if not.
                                            if (amountValid && t.transaction.otherTrans.recipient == AddressEncoding.ToEncoded(Con.GetNetworkVersion(), new CSharp2nem.Model.AccountSetup.PublicKey(tx.transaction.otherTrans?.signer == null ? tx.transaction.signer : tx.transaction.otherTrans.signer))) continue;
                                            else
                                            {
                                                Console.WriteLine("failed on transaction verification: amount or recipient is invalid");
                                                valid = false;
                                            }
                                        }
                                        else Console.WriteLine("failed on transaction verification: invalid or missing mosaic");

                                    }
                                    else if (m.CostDenomination == "XEM")
                                    {
                                        // set mosaic amount paid out.
                                        var q = m2.quantity;

                                        // get amount that should be paid out for the deposit
                                        var a = Calculations.BonusCalculation(m, Math.Ceiling((depositedAmount / decimal.Parse(m.MosaicCost)) / (decimal)Math.Pow(10, 6 - mosaicDivisibility)));

                                        // if incorrect, fail
                                        valid = a == q ? true : false;

                                        if (!valid) Console.WriteLine("failed on transaction verification: amounts do not match");
                                    }
                                    else Console.WriteLine("failed on transaction verification: incorrect currency");
                                }
                            }, m.MosaicNameSpace, m.MosaicID).AsyncWaitHandle.WaitOne();
                        }
                    }
                    else Console.WriteLine("failed on transaction verification: missing hash");
                }
            }, t.transaction.otherTrans.signer).AsyncWaitHandle.WaitOne();

            return valid;
        }

        private static List<Transactions.TransactionData> GetTransactions(string address, string hash = null)
        {
            var txClient = new TransactionDataClient(Con);


            var txs = txClient.EndGetTransactions(txClient.BeginGetIncomingTransactions(address, hash));


            return txs.data;
        }
    } 
}

