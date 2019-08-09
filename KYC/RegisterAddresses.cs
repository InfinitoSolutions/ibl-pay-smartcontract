using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;

namespace DAppKYC
{
    class RegisterAddresses : SmartContract
    {
        [DisplayName("registeredAddr_error")]
        public static event Action<string> Error;
        [DisplayName("registeredAddr_create")]
        public static event Action<byte[], byte[], string> Added;
        [DisplayName("registeredAddr_remove")]
        public static event Action<byte[], string> Removed;
        [DisplayName("registeredAddr_update")]
        public static event Action<byte[], byte[], string> Updated;


        /*Deposit Addresses*/
        /// <summary>
        /// Add new deposit address to a certain registered address & active user
        /// </summary>
        /// <param name="zyAddress"></param>
        /// <param name="registeredAddr"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        internal static Boolean addDepositAddr(byte[] zyAddress, byte[] registeredAddr, string symbol)
        {
            //even admin or user can addDeposit address
            //only affect on active Account
            bool valid = preModifyDeposit(zyAddress);
            if (!valid) return false;
            //validate crypto supported
            //string formattedSymbol = symbol.ToUpper();
            bool isSupported = SupportedTokens.isSupported(symbol);
            if (!isSupported)
            {
                Error("unsupported_token");
                return false;
            }
            StorageMap depositAddr = Storage.CurrentContext.CreateMap(registeredAddr.AsString());
            byte[] store = depositAddr.Get(symbol);
            if (store != null)
            {
                Error("registered");
                return false;
            }
            depositAddr.Put(symbol, zyAddress);
            Added(zyAddress, registeredAddr, symbol);
            return true;
        }

        /// <summary>
        /// Remove a deposit address from registered address
        /// </summary>
        /// <param name="address"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        internal static Boolean removeDepositAddr(byte[] address, string symbol)
        {
            StorageMap store = Storage.CurrentContext.CreateMap(address.AsString());
            byte[] owner = store.Get(symbol);
            if(owner == null)
            {
                Error("404");
                return false;
            }
            //only owner and admin can make remove deposit address
            //only affect active user
            bool valid = preModifyDeposit(owner);
            if (!valid) return false;

            store.Delete(symbol); //throw exception 
            Removed(address, symbol);
            return true;
        }

        /// <summary>
        /// Update deposut addreee
        /// </summary>
        /// <param name="oldAddr"></param>
        /// <param name="newAddr"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        internal static Boolean updateDepositAddr(byte[] oldAddr, byte[] newAddr, string symbol)
        {
            //to-do: update deposit address and validate new addr
            StorageMap store = Storage.CurrentContext.CreateMap(oldAddr.AsString());
            //string formattedSymbol = symbol.ToUpper();
            byte[] addr = store.Get(symbol);
            if(addr == null)
            {
                Error("404");
                return false;
            }
            //only admin and user can call
            // affect only actiive user
            bool valid = preModifyDeposit(addr);
            if (!valid) return false;

            //update address
            store.Delete(symbol);
            StorageMap newstore = Storage.CurrentContext.CreateMap(newAddr.AsString());
            store.Put(symbol,addr);
            Updated(oldAddr, newAddr, symbol);
            return true;
        }

        /// <summary>
        /// Check if an address ia a qualified deposit address
        /// </summary>
        /// <param name="address"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        internal static Boolean isDepositAddr(byte[] address, string symbol)
        {
            StorageMap store = Storage.CurrentContext.CreateMap(address.AsString());
            //string formattedSymbol = symbol.ToUpper();
            byte[] addr = store.Get(symbol);
            return validateUser(addr);
        }

        /// <summary>
        /// return a registred address (created by Zeyap)
        /// </summary>
        /// <param name="depositAddr"></param>
        /// <param name="symbol"></param>
        /// <returns></returns> null if no address can be found
        internal static byte[] getRegisteredAddr(byte[]depositAddr, string symbol)
        {
            //string formattedSymbol = symbol.ToUpper();
            StorageMap store = Storage.CurrentContext.CreateMap(depositAddr.AsString());
            return store.Get(symbol);
        }
        /// <summary>
        /// Check if a user's status is at least Active
        /// </summary>
        /// <param name="zyAddress"></param>
        /// <returns></returns>
        private static Boolean validateUser(byte[] zyAddress)
        {
            uint kyc = Whitelist.getKYCLevel(zyAddress);
            if (kyc <= (uint)Whitelist.KYC_Level.Deactive)
            {
                return false;
            }
            return true;
        }  
        
        private static Boolean preModifyDeposit(byte[] owner)
        {
            if(!Runtime.CheckWitness(owner) && !Runtime.CheckWitness(KYC.Owner)) {
                Error("401");
                return false;
            }
            if(!validateUser(owner))
            {
                Error("invalid_user");
                return false;
            }
            return true;
        }
    }
}
