using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XEMSign
{
    public class TxSummary
    {
        public string AccAddress { get; set; }
        public long Amount { get; set; }
        public DateTime DateOfTx { get; set; }

    }
}
