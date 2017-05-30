using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using CSharp2nem;
using Newtonsoft.Json;


namespace XEMSign
{
    internal class KeyLastCheckedPair
    {
        internal  List<string> CheckedHash { get; set; }
        internal VerifiableAccount Acc { get; set; }
    }

    internal class AccountScanner
    {
        private static readonly Connection Con = new Connection();
        internal async void ScanAccounts()
        {
            var privateKeys = ConfigurationManager.GetSection("accountsPrivate") as MyConfigSection;

            Con.SetTestNet();

            List<KeyLastCheckedPair> listOfCosigs = new List<KeyLastCheckedPair>();

            foreach (MyConfigInstanceElement e in privateKeys.Instances)
            {
                var pair = new KeyLastCheckedPair();

                pair.CheckedHash = new List<string>();

                pair.Acc = new AccountFactory(Con).FromPrivateKey(e.Code);

                listOfCosigs.Add(pair);
            }

            while (true)
            {
                foreach (var pair in listOfCosigs)
                {
                    Transactions.All transactions;

                    try
                    {
                        transactions = await pair.Acc.GetUnconfirmedTransactionsAsync();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }
                 

                   if (pair.CheckedHash.Count > 0 && transactions.data.Count > 0)
                   {
                       pair.CheckedHash.RemoveAll(e => transactions.data.All(i => e != i.meta.data));
                   }
                    

                    foreach (var t in transactions.data)
                    {
                        
                        if (t.transaction.type != 4100 || pair.CheckedHash.Contains(t.meta.data)) continue;

                        Console.WriteLine();

                        Console.WriteLine("checking transaction: \n" + t.meta.data);

                        pair.CheckedHash.Add(t.meta.data);

                        Console.WriteLine("checked");

                        if (new RuleScanner().ScanTxAgainstRuleSet(t, pair.Acc))
                            SignTransaction(
                                acc: pair.Acc, 
                                t: t, 
                                multisigAcc: AddressEncoding.ToEncoded(
                                    network: 0x90, 
                                    publicKey: new PublicKey(t.transaction.otherTrans.signer)));
                    }
                }     
            }
        }

        private static async void SignTransaction(VerifiableAccount acc, Transactions.TransactionData t, string multisigAcc)
        {
            Console.WriteLine("signing transaction");
            try
            {
                var a = await acc.SignMultisigTransaction(new MultisigSignatureTransactionData
                {
                    Deadline = int.Parse(ConfigurationManager.AppSettings["deadline"]) == 0
                 ? 82800 : int.Parse(ConfigurationManager.AppSettings["deadline"]),
                    TransactionHash = t.meta.data,
                    MultisigAddress = new Address(multisigAcc)
                });
                if (a.Code == 1)
                {
                    var sum = new TxSummary()
                    {
                        AccAddress = multisigAcc,
                        DateOfTx = DateTime.Now,
                        Amount = t.transaction.otherTrans.amount
                    };                  
                    TxSummaryController.AddSummaryForAccount(sum);
                }

                Console.WriteLine(a.Message);
                Console.WriteLine();
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                SignTransaction(acc, t, multisigAcc);      
            }  
        }
    }
}
