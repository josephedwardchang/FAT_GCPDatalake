using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAT_GCPdatalake
{
    public interface IBusinessLogic
    {
        bool SaveToDB(string strData, string strDevId);
    }
}
