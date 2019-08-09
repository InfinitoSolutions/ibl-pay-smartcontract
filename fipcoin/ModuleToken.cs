using System;
using System.ComponentModel;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using TOK = fipcoin.ModuleToken;

namespace fipcoin
{
    //TOK
    internal class ModuleToken
    {
        internal static object ApplicationMethods(string operation, object[] args)
        {

            if (operation.Equals("TOK_Issued"))
            {
                return Issue((byte[]) args[0], (BigInteger) args[1]);
            }
            if (operation.Equals("TOK_Issued_By_Deposit"))
            {
                return IssueByDeposit((byte[])args[0], (BigInteger)args[1]);
            }
            if (operation == "TOK_Withdrawl")
            {
                return withdrawToken((byte[])args[0], (byte[])args[1],(BigInteger)args[2]);
            }
            Error("unsupported_token_function");
            return null;
        }

        //Constants
        internal static readonly byte[] Empty = { };

        internal static readonly byte[] Owner = "".ToScriptHash();

        internal static readonly byte[] NEO =
        {
            155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175,
            144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197
        };

        internal static readonly byte[] FIP_DEBIT = { 155 };
        internal static readonly byte[] FIP_CREDIT = { 166 };

        public const ulong fip_decimals = 1000000; //decided by Decimals()
        public const ulong initialSupply = 100000000 * fip_decimals;

        [DisplayName("issue")]
        public static event Action<byte[], BigInteger, BigInteger> Issued;
        [DisplayName("error")]
        public static event Action<String> Error;

        internal const string _tokenTotalSupplyStorageKey = "fip_total_supply";

        //Issue tokens to an address 
        //called only by owner of the smart contract
        private static bool Issue(byte[] account, BigInteger value)
        {
            if (!Runtime.CheckWitness(TOK.Owner)) //authorization
            {
                Error("Authorization_failed");
                return false;
            }
            if (value <= 0) //valid amount to be issued
            {
                Error("Invalid_amount_to_issue");
                return false;
            }
            bool isActiveUser = (bool) FIP.KYC("isActiveUser", new object[] {account});
            if(isActiveUser == false)
            {
                Error("Inactive_user");
                return false;
            }
            //issue  
            BigInteger currentBalance = Storage.Get(Storage.CurrentContext, account).AsBigInteger();
            Storage.Put(Storage.CurrentContext, account, currentBalance + value);
            currentBalance = Storage.Get(Storage.CurrentContext, account).AsBigInteger();
            Issued(account, value, currentBalance);
            return true;
        }
        private static bool IssueByDeposit(byte[] account, BigInteger value)
        {
            byte[] address = (byte[])FIP.KYC("getRegisteredAddress", new object[] { account, ModuleNEP5.Symbol() });
            return Issue(address, value);
        }


        /// <summary>
        /// withdraw token
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool withdrawToken(byte[] zyaddress, byte[] withdrawlAddress, BigInteger value)
        {
            if (!Runtime.CheckWitness(zyaddress))
            {
                Error("Authorization_failed");
                return false;
            }
            if (!(bool)FIP.KYC("isActiveUser", new object[] {zyaddress}))
            {
                Error("In_active_user" + zyaddress.AsString());
                return false;
            }
            byte[] id = (byte[])FIP.KYC("getRegisteredAddress", new object[] { withdrawlAddress, ModuleNEP5.Symbol() });
            if(id != zyaddress)
            {
                Error("Invalid_addresses");
                return false;
            }
            return ModuleNEP5.Transfer(zyaddress, TOK.Owner, value, 0);
        }
    }
}