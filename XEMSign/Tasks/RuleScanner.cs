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
                
                    var TransactionDataClient = new TransactionDataClient(Con);
                                
                    // get all incoming transactions to the ICO deposit account
                    TransactionDataClient.BeginGetIncomingTransactions(ar2 => {

                        // fail if deposit hash not present in payout transaction
                        if ((t.transaction.otherTrans == null ? t.transaction.message?.payload : t.transaction.otherTrans.message?.payload) != null)
                        {
                            // find deposit transaction based on hash in message
                            var tx = ar2.Content.data.Single(x => x.meta.hash.data == Encoding.UTF8.GetString(CryptoBytes.FromHexString((t.transaction.type == 257 ? t.transaction.message.payload : t.transaction.otherTrans.message.payload))));

                            // initiate mosaic client
                            var mosaicClient = new NamespaceMosaicClient(Con);

                            var mosaicDivisibility = 0;

                            // retrieve mosaic definition for rate calculation
                            mosaicClient.BeginGetMosaicsByNameSpace(ar3 =>
                            {
                                if (ar3.Ex != null)
                                {
                                    Console.WriteLine(ar2.Ex);
                                } // if no error, set mosaic divisibility
                                else mosaicDivisibility = int.Parse(ar3.Content.Data[0].Mosaic.Properties[0].Value);

                                var depositedAmount = tx.transaction.type == 257
                                            ? tx.transaction.amount
                                            : tx.transaction.otherTrans.amount;

                                if ((tx.transaction.mosaics ?? tx.transaction.otherTrans.mosaics) != null)
                                {
                                    foreach (var m in tx.transaction.mosaics ?? tx.transaction.otherTrans.mosaics)
                                    {
                                        if (m.mosaicId.name == "xem" && m.mosaicId.namespaceId == "nem")
                                        {
                                            depositedAmount = m.quantity;
                                        }
                                    }
                                }

                                // calculate based on pre-set currency denomination
                                if (ConfigurationManager.AppSettings["currency"] == "USD")
                                {
                                    // fail if no mosaics present in ico payout transaction
                                    if (t.transaction.otherTrans.mosaics != null )
                                    {
                                        // check if the mosaic quantity paid out is correct for the deposit
                                        var amountValid = RateCalculation(depositedAmount, mosaicDivisibility) == t.transaction.otherTrans.mosaics[0].quantity;

                                        // check the recipient of the payout is the signer of the deposit, fail if not.
                                        if (amountValid && t.transaction.otherTrans.recipient == AddressEncoding.ToEncoded(Con.GetNetworkVersion(), new PublicKey(tx.transaction.otherTrans?.signer == null ? tx.transaction.signer : tx.transaction.otherTrans.signer)))
                                        {
                                            valid = true;

                                        }
                                        else Console.WriteLine("failed on transaction verification: amount or recipient is invalid");
                                    }
                                    else Console.WriteLine("failed on transaction verification: invalid or missing mosaic");
                                    
                                }
                                else if (ConfigurationManager.AppSettings["currency"] == "XEM")
                                {
                                    // set mosaic amount paid out.
                                    var q = t.transaction.otherTrans.mosaics[0].quantity;

                                    // get amount that should be paid out for the deposit
                                    var a = long.Parse(ConfigurationManager.AppSettings["currency"]) / depositedAmount;

                                    // if incorrect, fail
                                    valid = a == q ? true : false;

                                    if (!valid) Console.WriteLine("failed on transaction verification: amounts do not match");
                                }

                            }, ConfigurationManager.AppSettings["namespace"], ConfigurationManager.AppSettings["ID"]).AsyncWaitHandle.WaitOne();

                        }
                        else Console.WriteLine("failed on transaction verification: missing hash");


                    }, AddressEncoding.ToEncoded(Con.GetNetworkVersion(), new PublicKey(ConfigurationManager.AppSettings["ICOAccountPubKey"]))).AsyncWaitHandle.WaitOne();
                }
            }, t.transaction.otherTrans.signer).AsyncWaitHandle.WaitOne();
            
            return valid;
        }

        private static long RateCalculation(long xem, int divisibility)
        {
            double rate = 0.0;

            UriBuilder uri = new UriBuilder()
            {
                Host = "api.coinmarketcap.com",
                Path = "/v1/ticker/nem",
                Query = "convert=USD"
            };

            var Con2 = new Connection(uri);

            var Http = (HttpWebRequest)WebRequest.Create(Con2.Uri.Uri.AbsoluteUri);

            Http.Accept = "application/json";

            var asyncResult = new ManualAsyncResult2();

            Http.BeginGetResponse(asyncResult.WrapHandler(ar =>
            {
                try
                {
                    var response = Http.EndGetResponse(ar);

                    Stream responseStream = response.GetResponseStream();

                    var currencyData = JsonConvert.DeserializeObject<List<currenyData>>(new StreamReader(responseStream).ReadToEnd());

                    rate = double.Parse(currencyData[0].price_usd);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }), null);

            // wait for callback to complete
            asyncResult.AsyncWaitHandle.WaitOne();

            // calculate amount of xar to return
            var amount = xem / (double.Parse(ConfigurationManager.AppSettings["cost"]) / rate);

            // round up to nearest decimal based on mosaic divisibility
            amount = (Math.Ceiling(amount / Math.Pow(10, 6 - divisibility)));
           
            return (long)BonusCalculation(amount);
        }

        private static double BonusCalculation(double amount)
        {
            return DateTime.Now < DateTime.Parse(ConfigurationManager.AppSettings["bonusDate1"]) ? amount + (amount / 100 * long.Parse(ConfigurationManager.AppSettings["bonus1"]))
                 : DateTime.Now < DateTime.Parse(ConfigurationManager.AppSettings["bonusDate2"]) ? amount + (amount / 100 * long.Parse(ConfigurationManager.AppSettings["bonus2"]))
                 : DateTime.Now < DateTime.Parse(ConfigurationManager.AppSettings["bonusDate3"]) ? amount + (amount / 100 * long.Parse(ConfigurationManager.AppSettings["bonus3"]))
                 : amount;
        }
    }

    public class currenyData
    {
        public string id { get; set; }
        public string name { get; set; }
        public string symbol { get; set; }
        public string rank { get; set; }
        public string price_usd { get; set; }
        public string price_btc { get; set; }
        [JsonProperty("24h_volume_usd")]
        public string volumeusd { get; set; }
        public string market_cap_usd { get; set; }
        public string available_supply { get; set; }
        public string total_supply { get; set; }
        public string percent_change_1h { get; set; }
        public string percent_change_24h { get; set; }
        public string percent_change_7d { get; set; }
        public string last_updated { get; set; }
    }
}

