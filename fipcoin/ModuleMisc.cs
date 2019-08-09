using NEP = fipcoin.ModuleNEP5;
using Neo.SmartContract.Framework.Services.Neo;
using System.ComponentModel;
using System;

namespace fipcoin
{
    internal class ModuleMisc
    {
        internal static object ApplicationMethods(string operation, object[] args)
        {
            //TODO: NEP5 old call here

            if (operation.Equals("decimals"))
            {
                return NEP.Decimals();
            }
            if (operation.Equals("name"))
            {
                return NEP.Name();
            }
            if (operation.Equals("symbol"))
            {
                return NEP.Symbol();
            }
            if (operation.Equals("balanceOf"))
            {
                return Storage.Get(Storage.CurrentContext, (byte[])args[0]);
            }
            Error("unsupported__function");
            return null;
        }
        [DisplayName("error")]
        public static event Action<String> Error;
    }
}
