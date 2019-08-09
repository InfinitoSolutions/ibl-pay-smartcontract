using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;

namespace DAppKYC
{
    internal class Whitelist : SmartContract
    {
        public enum KYC_Level { Deactive = 1, Active, Level1, Level2, Level3 };
        const string kycPrefix = "KYC";

        //Events definition
        [DisplayName("whitelist_error")]
        public static event Action<string> Error;
        [DisplayName("whitelist_errors")]
        public static event Action<string, string> Errors;
        [DisplayName("whitelist_set")]
        public static event Action<byte[], uint> KYCSet;
        [DisplayName("whitelist_new_user")]
        public static event Action<byte[]> Register;
        [DisplayName("whitelist_deactive")]
        public static event Action<byte[]> Deactive;


        /// <summary>
        /// Get KYC level of an address, 0 means not in the list yet
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        internal static uint getKYCLevel(byte[] address)
        {
            //To-do: validate address
            StorageMap kycMap = Storage.CurrentContext.CreateMap(kycPrefix);
            return (uint)kycMap.Get(address).AsBigInteger();
        }

        /// <summary>
        /// return is user is active or not
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        internal static bool isActive(byte[] address)
        {
            StorageMap kycMap = Storage.CurrentContext.CreateMap(kycPrefix);
            uint kyclevel = (uint)kycMap.Get(address).AsBigInteger();
            return (kyclevel >= (uint)KYC_Level.Active);
        }

        /// <summary>
        /// Set KYC level for a Zeyap address. 
        /// If not registered yet, create new record 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal static Boolean setKYCLevel(byte[] address, uint level)
        {
            //only by admin
            if (!Runtime.CheckWitness(KYC.Owner))
            {
                Error("401");
                return false;
            }
            var definedLevel = (KYC_Level)level; //throw exception if not finded
            StorageMap kycMap = Storage.CurrentContext.CreateMap(kycPrefix);
            uint currentLevel = (uint)kycMap.Get(address).AsBigInteger();
            if (currentLevel == level)
            {
                Error("same_level");
                return false;
            }
            kycMap.Put(address, level);
            KYCSet(address, level); //emit event
            return true;
        }

        /// <summary>
        /// Register a new user -> active user
        /// if user is existed -> throw error
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        internal static Boolean registerUser(byte[] address, byte[] depositAddr, string symbol)
        {
            //only by admin
            if(!Runtime.CheckWitness(KYC.Owner))
            {
                Error("401");
                return false;
            }
            //validate Zeyap address
            StorageMap kycMap = Storage.CurrentContext.CreateMap(kycPrefix);
            uint currentLevel = (uint)kycMap.Get(address).AsBigInteger();
            if(currentLevel >= (uint)KYC_Level.Deactive)
            {
                Errors("existed_user", address.AsString());
                return false;
            }
            kycMap.Put(address, (uint)KYC_Level.Active);
            Register(address);
            if(symbol != null &&  depositAddr.Length >= 0)
            {
                return RegisterAddresses.addDepositAddr(address, depositAddr, symbol);
            }
            return true;
        }

        /// <summary>
        /// Deactive address
        /// If deactive or not existed, throw an error
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        internal static Boolean deactiveUser(byte[]address)
        {
            //only by admin
            if (!Runtime.CheckWitness(KYC.Owner))
            {
                Error("401");
                return false;
            }
            StorageMap kycMap = Storage.CurrentContext.CreateMap(kycPrefix);
            uint currentLevel = (uint)kycMap.Get(address).AsBigInteger();
            if (currentLevel <= (uint)KYC_Level.Deactive)
            {
                Errors("already_deactive", address.AsString());
                return false;
            }
            kycMap.Put(address, (uint)KYC_Level.Deactive);
            Deactive(address);
            return true;
        }
    }
}
