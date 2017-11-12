using CSharp2nem.Connectivity;
using CSharp2nem.RequestClients;
using CSharp2nem.ResponseObjects.Transaction;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XEMSign
{
    internal static class Calculations
    {
        internal static long GetDepositedAmount(Transactions.TransactionData tx)
        {
            // get amount deposited, otherTrans = support for multisig deposits
            var depositedAmount = tx.transaction.type == 257
                ? tx.transaction.amount
                : tx.transaction.otherTrans.amount;

            // check for xem mosaic, as xem can be sent as mosaic which overrides tx.transaction.amount.
            if ((tx.transaction.mosaics ?? tx.transaction.otherTrans?.mosaics) != null)
            {
                foreach (var m in tx.transaction.mosaics ?? tx.transaction.otherTrans.mosaics)
                {
                    if (m.mosaicId.name == "xem" && m.mosaicId.namespaceId == "nem")
                    {
                        depositedAmount = m.quantity;
                    }
                }
            }

            return depositedAmount;
        }

        internal static long RateCalculation(long xem, int divisibility, MosaicConfigElement m)
        {
            decimal rate = 0.0M;

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
                var response = Http.EndGetResponse(ar);

                Stream responseStream = response.GetResponseStream();

                var currencyData = JsonConvert.DeserializeObject<List<currenyData>>(new StreamReader(responseStream).ReadToEnd());

                rate = decimal.Parse(currencyData[0].price_usd);

            }), null);

            // wait for callback to complete
            asyncResult.AsyncWaitHandle.WaitOne();

            // calculate amount of xar to return
            var amount = xem / (decimal.Parse(m.MosaicCost) / rate);

            // round up to nearest decimal based on mosaic divisibility
            amount = (Math.Ceiling(amount / (decimal)Math.Pow(10, 6 - divisibility)));

            return (long)BonusCalculation(m, amount);
        }

        internal static decimal BonusCalculation(MosaicConfigElement m, decimal amount)
        {
            var bonuses = ConfigurationManager.GetSection("MosaicBonusConfigElement") as MyBonusConfigSection;

            foreach (MosaicBonusConfigElement b in bonuses.Bonuses)
            {
                var start = DateTime.ParseExact(b.StartDateTime, "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);

                var end = DateTime.ParseExact(b.EndDateTime, "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);

                if (DateTime.Now.Ticks > start.Ticks && DateTime.Now.Ticks < end.Ticks && (m.MosaicNameSpace + ":" + m.MosaicID == b.TokenAssignedTo))
                {
                    return amount += amount / 100 * decimal.Parse(b.BonusPercent);
                }
            }

            return amount;
        }

        internal static int GetMosaicDivisibility(Connection Con, string nameSpace, string id)
        {
            var mosaicDivisibility = 0;

            var mosaicClient = new NamespaceMosaicClient(Con);

            // get mosaic definition, specifically divisbility
            mosaicClient.BeginGetMosaicsByNameSpace(ar2 =>
            {
                if (ar2.Ex != null)
                {
                    Console.WriteLine(ar2.Ex);
                }
                else mosaicDivisibility = int.Parse(ar2.Content.Data[0].Mosaic.Properties[0].Value);

            }, nameSpace, id).AsyncWaitHandle.WaitOne();

            if (mosaicDivisibility == 0)
                throw new Exception("divisbility not found");

            return mosaicDivisibility;
        }

    }
}
