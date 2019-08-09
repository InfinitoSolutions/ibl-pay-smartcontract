using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.ComponentModel;

namespace DAppKYC
{
    public class KYC : SmartContract
    {

        //Events definition
        [DisplayName("kyc_error")]
        public static event Action<string> Error;
        [DisplayName("kyc_function_call")]
        public static event Action<string> FunctionCall;

        //admin account
        internal static readonly byte[] Owner = "OwnerAddress".ToScriptHash();
        internal static readonly byte[] Master = "MasterAddress".ToScriptHash();

        public static byte[] V = { 7 };
        public static string Env() => "dev_w_migrate";

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return false;
            }
            if (Runtime.Trigger == TriggerType.Application)
            {
                FunctionCall(operation);
                //in case of migration
                if (operation == "migrate")
                {
                    if (args.Length < 9)
                    {
                        Error("error_missing_migrate_params");
                        return false;
                    }
                    byte[] script = (byte[])args[0];
                    byte[] plist = (byte[])args[1];
                    byte rtype = (byte)args[2];
                    ContractPropertyState cps = (ContractPropertyState)args[3];
                    string name = (string)args[4];
                    string version = (string)args[5];
                    string author = (string)args[6];
                    string email = (string)args[7];
                    string description = (string)args[8];
                    return Migrate(script,
                                   plist,
                                   rtype,
                                   cps,
                                   name,
                                   version,
                                   author,
                                   email,
                                   description);
                }
                if (operation == "getKYCLevel")
                {
                    return Whitelist.getKYCLevel((byte[])args[0]);
                }
                if (operation == "isActiveUser")
                {
                    return Whitelist.isActive((byte[])args[0]);
                }                
                if (operation == "getRegisteredAddress")
                {
                    return RegisterAddresses.getRegisteredAddr((byte[])args[0], (string) args[1]);
                }              
                if(operation == "isSupported")
                {
                    return SupportedTokens.isSupported((string) args[0]);
                }
                //function called only by admin
                if (operation == "addSupportToken")
                {
                    return SupportedTokens.addToken((string)args[0]);
                }
                if (operation == "deactive")
                {
                    return Whitelist.deactiveUser((byte[])args[0]);
                }
                if (operation == "setKYCLevel")
                {
                    return Whitelist.setKYCLevel((byte[])args[0], (uint)args[1]);
                }
                if (operation == "register")
                {
                    return Whitelist.registerUser((byte[])args[0], (byte[])args[1],(string)args[2]);
                }

                //function called by admin/user 
                if (operation == "addDeposit")
                {
                    return RegisterAddresses.addDepositAddr((byte[])args[0], (byte[])args[1], (string)args[2]);
                }
                if (operation == "removeDeposit")
                {
                    return RegisterAddresses.removeDepositAddr((byte[])args[0], (string)args[1]);
                }

                //function not found
                Error("error_unsupported_token");
                return false;
            }

            //If there is no opteration matched
            Error("error_unsupported_trigger");
            return false;
        }

        /// <summary>
        /// Migrate smart contract to a new one, called by admin only
        /// </summary>
        /// <param name="script"></param>
        /// <param name="plist"></param> '0710'
        /// <param name="rtype"></param> '05'
        /// <param name="cps"></param> 1
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="author"></param>
        /// <param name="email"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        private static Boolean Migrate(byte[] script,
            byte[] plist,
            byte rtype,
            ContractPropertyState cps,
            string name,
            string version,
            string author,
            string email,
            string description)
        {
            if (!Runtime.CheckWitness(Master))
            {
                Error("401");
                return false;
            }
            var contract = Contract.Migrate(script,
                plist,
                rtype,
                cps,
                name,
                version,
                author,
                email,
                description);
            return true;
        }
    }
}
