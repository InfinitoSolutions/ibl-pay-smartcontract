using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;

/******************************************************************
 * smart contract to managing tokens supported by Zeyap
 * token status: Active, Deactive or Blank (not in the list of supported yet)
 * Following functions:
 * isSupported() -> check if Zeyap is supporting the token
 * addToken() -> add a new token or reactiv an existed deactivated token
 * removeToken() -> make an activated token status to deactivated
 * getNumberofSupportedTokens() -> return the number of active tokens supported by 
 *                                 Zeyap
 * ****************************************************************/

namespace DAppKYC
{
    internal class SupportedTokens : SmartContract
    {
        //Events definition
        [DisplayName("tokens_error")]
        public static event Action<string> Error;
        [DisplayName("tokens_deactive")]
        public static event Action<string> Deactived;
        [DisplayName("tokens_add")]
        public static event Action<string> AddToken;
        [DisplayName("tokens_active")]
        public static event Action<string> Activated;
        //End of events definition


        enum TokenStatus { Blank, Active, Deactive};


        /// <summary>
        /// Check if a token is supported: added and active
        /// </summary>
        /// <param name="tokenSymbol"></param>
        /// <returns></returns>
        internal static Boolean isSupported(string tokenSymbol)
        {
            //Throw error if token symbol is empty
            //string formattedSymbol = tokenSymbol.ToUpper();
            int tokenStatus = getTokenStatus(tokenSymbol);
            return (tokenStatus == (int)TokenStatus.Active);
        }


        /// <summary>
        /// Add a new token to the list
        /// if token status is deactive -> reactive it
        /// </summary>
        /// <param name="tokenSymbol"></param>
        /// <returns></returns> true if a new token is added and deactive tokens is revoked
        internal static Boolean addToken(string tokenSymbol)
        {
            //only by admin
            if (!Runtime.CheckWitness(KYC.Owner))
            {
                Error("401");
                return false;
            }
            //Throw error if token symbol is empty
            //string recordedSymbol = tokenSymbol.ToUpper();
            //Find if symbol is in the list
            int status = getTokenStatus(tokenSymbol);
            if (status == (int)TokenStatus.Active) //existed && active
            {
                Error("existed_token");
                return false;
            } 
            if(status == (int)TokenStatus.Blank) // not existed
            {
                addNewToken(tokenSymbol);
                AddToken(tokenSymbol);
                return true;
            }
            if(status == (int)TokenStatus.Deactive) //deactivate
            {
                activeToken(tokenSymbol);
                Activated(tokenSymbol);
                return true;
            }
            else //if not fall in any case
            {
                Error("system_error");
                return false;
            }
        } 

        /// <summary>
        /// Deactive token if token is actived
        /// </summary>
        /// <param name="tokenSymbol"></param>
        /// <returns></returns>
        internal static Boolean removeToken(string tokenSymbol)
        {
            //only by admin
            if (!Runtime.CheckWitness(KYC.Owner))
            {
                Error("401");
                return false;
            }
            //string recordedSymbol = tokenSymbol.ToUpper();
            int status = getTokenStatus(tokenSymbol);
            if(status != (int)TokenStatus.Active)
            {
                Error("not_existed");
                return false;
            }

            //deactive token
            deactiveToken(tokenSymbol);
            Deactived(tokenSymbol);
            return false;
        }

        /// <summary>
        /// Get total 
        /// </summary>
        /// <returns></returns>
        internal static uint getNumberofActiveTokens()
        {
            StorageMap zeyaptokens = Storage.CurrentContext.CreateMap("zeyap_token");
            return (uint)zeyaptokens.Get("active").AsBigInteger();
        }



        /// <summary>
        /// Get number of deactive tokens
        /// </summary>
        /// <returns></returns>
        internal static uint getNumOfDeactiveTokens()
        {
            StorageMap zeyaptokens = Storage.CurrentContext.CreateMap("zeyap_token");
            return (uint)zeyaptokens.Get("deactive").AsBigInteger();
        }

        /// <summary>
        /// Get status of token, defined in TokenStatus
        /// </summary>
        /// <param name="formattedTokens"></param>
        /// <returns></returns>
        internal static int getTokenStatus(string formattedTokens)
        {
            StorageMap zeyaptokens = Storage.CurrentContext.CreateMap("zeyap_token");
            return (int)zeyaptokens.Get(formattedTokens).AsBigInteger();
        }
        
        /// <summary>
        /// Change status of an token from ACTIVE
        /// to DEACTIVE
        /// </summary>
        /// <param name="formattedTokens"></param>
        private static void deactiveToken(string formattedTokens)
        {
            StorageMap zeyaptokens = Storage.CurrentContext.CreateMap("zeyap_token");
            uint totalDeactive = (uint)zeyaptokens.Get("deactive").AsBigInteger() + 1;
            uint totalActive = (uint)zeyaptokens.Get("active").AsBigInteger() - 1;
            zeyaptokens.Put(formattedTokens, (int)TokenStatus.Deactive);
            zeyaptokens.Put("deactive", totalDeactive);
            zeyaptokens.Put("active", totalActive);
        }

        /// <summary>
        /// Add completely new token to supported tokens
        /// </summary>
        /// <param name="formattedTokens"></param>
        private static void addNewToken(string formattedTokens)
        {
            StorageMap zeyaptokens = Storage.CurrentContext.CreateMap("zeyap_token");
            uint totalActive = (uint)zeyaptokens.Get("active").AsBigInteger() + 1;
            zeyaptokens.Put(formattedTokens, (int)TokenStatus.Active);
            zeyaptokens.Put("active", totalActive);
        }

        /// <summary>
        /// re-active a deactivated tokens
        /// </summary>
        /// <param name="formattedTokens"></param>
        private static void activeToken(string formattedTokens)
        {
            StorageMap zeyaptokens = Storage.CurrentContext.CreateMap("zeyap_token");
            uint totalDeactive = (uint)zeyaptokens.Get("deactive").AsBigInteger() - 1;
            uint totalActive = (uint)zeyaptokens.Get("active").AsBigInteger() + 1;
            zeyaptokens.Put(formattedTokens, (int)TokenStatus.Active);
            zeyaptokens.Put("deactive", totalDeactive);
            zeyaptokens.Put("active", totalActive);
        }
    }
}
