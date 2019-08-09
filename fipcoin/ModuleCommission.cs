using System;
using System.ComponentModel;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework;
using System.Numerics;

namespace fipcoin
{
    internal class ModuleCommission
    {
        internal static object ApplicationMethods(string operation, object[] args)
        {

            if (operation.Equals("COM_ChangeDirectPComm"))
            {
                return changeCommission("directP", (uint)args[0]);
            }
            if (operation.Equals("COM_ChangeSinglePComm"))
            {
                return changeCommission("singleP", (uint)args[0]);
            }
            if (operation == "COM_ChangeSchedulePComm")
            {
                return changeCommission("scheduleP", (uint)args[0]);
            }
            if (operation == "COM_ChangeWithdrawComm")
            {
                return changeCommission("withdraw", (uint)args[0]);
            }
            if (operation == "COM_GetDirectPComm")
            {
                return getCommission("directP");
            }
            if (operation == "COM_GetSinglePComm")
            {
                return getCommission("singleP");
            }
            if (operation == "COM_GetSchedulePComm")
            {
                return getCommission("scheduleP");
            }
            if (operation == "COM_GetWithdrawComm")
            {
                return getCommission("withdraw");
            }
            Error("unsupported_commission_function");
            return null;
        }

        [DisplayName("error")]
        public static event Action<String> Error;
        [DisplayName("commission_change")]
        public static event Action<String,BigInteger> Commision;
        private static object changeCommission(string type, uint percent)
        {
            if (!Runtime.CheckWitness(FIP.Admin))
            {
                Error("401");
                return false;
            }
            StorageMap storage = Storage.CurrentContext.CreateMap("comm");
            if(percent < 0)
            {
                Error("Invalid_commission");
                return false;
            }
            storage.Put(type,percent);
            Commision(type, percent);
            return true;
        }

        internal static object getCommission(string type)
        {
            StorageMap storage = Storage.CurrentContext.CreateMap("comm");
            return (uint)storage.Get(type).AsBigInteger();
        }
    }
}
