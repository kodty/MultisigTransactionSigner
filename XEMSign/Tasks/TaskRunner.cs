using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using CSharp2nem;
using Newtonsoft.Json;
using CSharp2nem.CryptographicFunctions;
using CSharp2nem.Model.AccountSetup;
using CSharp2nem.Connectivity;
using CSharp2nem.RequestClients;
using CSharp2nem.ResponseObjects.Transaction;
using CSharp2nem.Model.DataModels;

namespace XEMSign
{
    internal class KeyLastCheckedPair
    {
        internal  List<string> CheckedHash { get; set; }
        internal PrivateKeyAccountClient Acc { get; set; }
    }

    internal class AccountScanner
    {
        private static readonly Connection Con = new Connection();
        internal void ScanAccounts()
        {
            var privateKeys = ConfigurationManager.GetSection("accountsPrivate") as MyConfigSection;

            if (ConfigurationManager.AppSettings["network"] == "test") Con.SetTestnet();
            else Con.SetMainnet();

            List<KeyLastCheckedPair> listOfCosigs = new List<KeyLastCheckedPair>();

            foreach (MyConfigInstanceElement e in privateKeys.Instances)
            {
                var pair = new KeyLastCheckedPair();

                pair.CheckedHash = new List<string>();

                pair.Acc = new PrivateKeyAccountClientFactory(Con).FromPrivateKey(e.Code);

                listOfCosigs.Add(pair);
            }

            while (true)
            {
                foreach (var pair in listOfCosigs)
                {
                    try
                    {
                        var client = new TransactionDataClient(Con);

                        client.BeginGetUnconfirmedTransactions(ar => {
                            try {

                                if (pair.CheckedHash.Count > 0 && ar.Content.data.Count > 0)
                                {
                                    pair.CheckedHash.RemoveAll(e => ar.Content.data.All(i => e != i.meta.data));
                                }


                                foreach (var t in ar.Content.data)
                                {

                                    if (t.transaction.type != 4100 || t.transaction?.otherTrans?.type != 257 || pair.CheckedHash.Contains(t.meta.data)) continue;

                                    if (t.transaction.signer == pair.Acc.PublicKey.Raw) continue;

                                    Console.WriteLine();

                                    Console.WriteLine("checking transaction: \n" + t.meta.data);

                                    pair.CheckedHash.Add(t.meta.data);

                                    Console.WriteLine("checked");

                                    if (new RuleScanner().ScanTxAgainstRuleSet(t))
                                        SignTransaction(
                                            acc: pair.Acc,
                                            t: t,
                                            multisigAcc: AddressEncoding.ToEncoded(
                                                network: Con.GetNetworkVersion(),
                                                publicKey: new PublicKey(t.transaction.otherTrans.signer)));
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }

                            
                        },pair.Acc.Address.Encoded).AsyncWaitHandle.WaitOne();          
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }
                 

                   
                }     
            }
        }

        private static void SignTransaction(PrivateKeyAccountClient acc, Transactions.TransactionData t, string multisigAcc)
        {
            Console.WriteLine("signing transaction");
            try
            {
                  acc.BeginSignatureTransactionAsync(ar =>{
                     
                      try {
                         
                          if (ar.Content.Code == 1)
                          {
                              var sum = new TxSummary()
                              {
                                  AccAddress = multisigAcc,
                                  DateOfTx = DateTime.Now,
                                  Amount = t.transaction.otherTrans.amount
                              };

                              TxSummaryController.AddSummaryForAccount(sum);
                          }
                          else Console.WriteLine(ar.Content.Code);
                          Console.WriteLine(ar.Content.Message);
                          Console.WriteLine();
                          Console.WriteLine();
                      }
                      catch (Exception ex)
                      {
                          Console.WriteLine(ex);
                      }
                      

                  }, new MultisigSignatureTransactionData
                  {
                      Deadline = int.Parse(ConfigurationManager.AppSettings["deadline"]) == 0
                      ? 82800 : int.Parse(ConfigurationManager.AppSettings["deadline"]),
                      TransactionHash = t.meta.data,
                      MultisigAddress = new Address(multisigAcc),
                    

                  }).AsyncWaitHandle.WaitOne();
                 
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                SignTransaction(acc, t, multisigAcc);      
            }  
        }
    }
}
