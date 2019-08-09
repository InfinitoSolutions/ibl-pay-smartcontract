using System;
using System.ComponentModel;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;

namespace fipcoin
{
    //NEP
    internal class ModuleNEP5
    {
        [DisplayName("error")]
        public static event Action<String> Error;

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> OnTransferred;

        [DisplayName("commission")]
        public static event Action<byte[], BigInteger> WithdrawCommission;

        internal static object ApplicationMethods(string operation, object[] args)
        {
            if (operation.Equals("NEP_BalanceOf"))
            {
                byte[] account = (byte[])args[0];
                return Storage.Get(Storage.CurrentContext, account).AsBigInteger();
            }

            if (operation.Equals("NEP_Decimals"))
            {
                return Decimals();
            }

            if (operation.Equals("NEP_Name"))
            {
                return Name();
            }

            if (operation.Equals("NEP_Symbol"))
            {
                return Symbol();
            }
            Error("unsupported_nep_function");
            return null;
        }

        public static byte Decimals() => 8;

        public static string Name() => "Test Payment Token";

        public static string Symbol() => "btc";

        public static string Env() => "dev";

        public byte V() => 8;

        // function that is always called when someone wants to transfer tokens.
        internal static bool Transfer(byte[] from, byte[] to, BigInteger value, uint commissionRate)
        {
            if (value <= 0) {
                Error("Invalid amount to transfer");
                return false;
            }

            //if (!Runtime.CheckWitness(from) || !ModuleUtilsAndVerification.ValidateAddress(to))
            //    return false;

            if (from == to) {
                Error("Invalid receiver address");
                return true;
            }
            BigInteger fromBalance = Storage.Get(Storage.CurrentContext, from).AsBigInteger(); // retrieve balance of originating account
            if (fromBalance < value)
            {
                Error("Invalid value or balance");
                // don't transfer if funds not available
                return false;
            }

            BigInteger commissionValue = (value * commissionRate) / 10000;
            if(commissionValue*10000 < value*commissionRate)
            {
                commissionValue += 1;
            }
            //Just throw error but continue with transfer
            //if(commissionRate > 0 && commissionValue <= 0)
            //{
            //    Error("Commission_too_litte");
            //}
            SetBalanceOf(from, fromBalance - value); // remove balance from originating account
            SetBalanceOf(to, Storage.Get(Storage.CurrentContext, to).AsBigInteger() + value - commissionValue); // set new balance for destination account
            SetBalanceOf(ModuleToken.Owner, Storage.Get(Storage.CurrentContext, ModuleToken.Owner).AsBigInteger() + commissionValue);

            //Throw events - 2 events
            OnTransferred(from, to, value);
            WithdrawCommission(to, commissionValue);
            return true;
        }

        private static void SetBalanceOf(byte[] address, BigInteger newBalance)
        {
            if (newBalance <= 0)
            {
                Storage.Delete(Storage.CurrentContext, address);
            }
            else
            {
                Storage.Put(Storage.CurrentContext, address, newBalance);
            }
        }
    }
}