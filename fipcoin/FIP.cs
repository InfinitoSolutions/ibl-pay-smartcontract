using System;
using System.ComponentModel;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using PRO = fipcoin.ModuleProtocol;
using UAV = fipcoin.Utils;
using TOK = fipcoin.ModuleToken;
using NEP = fipcoin.ModuleNEP5;

namespace fipcoin
{
    public class FIP : SmartContract
    {
        [DisplayName("function_call")]
        public static event Action<string> Called;
        [DisplayName("migrate_error")]
        public static event Action<string> MigrateError;

        //External smart contract
        [Appcall("")]
        public static extern object KYC(string arg, object[] param);

        internal static readonly byte[] Master = "MasterAddress".ToScriptHash();
        internal static readonly byte[] Admin = "AdminAddress".ToScriptHash();


        // params: 0710
        // return : 05
        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                // param Owner must be script hash
                if (Runtime.CheckWitness(TOK.Owner))
                {
                    return true;
                }
                // Check if attached assets are accepted
                return false;
            }
            
            if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "migrate")
                {
                    if (args.Length < 9)
                    {
                        MigrateError("error_missing_migrate_params");
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

                //if not migrate
                var ret = ApplicationMethods(operation, args);

                if (ret != null)
                {
                    return ret;
                }
            }

            return false;
        }

        private static object ApplicationMethods(string operation, object[] args)
        {
            Called(operation);
            string prefix = operation.Substring(0, 3);
            
            if (prefix.Equals("PRO"))
            {
                return PRO.ApplicationMethods(operation, args);
            }
            if (prefix.Equals("TOK"))
            {
                return TOK.ApplicationMethods(operation, args);
            }
            if (prefix.Equals("UAV"))
            {
                return UAV.ApplicationMethods(operation, args);
            }
            if (prefix.Equals("NEP"))
            {
                return NEP.ApplicationMethods(operation, args);
            }
            if (prefix.Equals("COM"))
            {
                return ModuleCommission.ApplicationMethods(operation, args);
            }
            return ModuleMisc.ApplicationMethods(operation, args);
        }
        /// <summary>
        /// MIgrate Fip
        /// </summary>
        /// <param name="script"></param> 
        /// <param name="plist"></param>'0710'
        /// <param name="rtype"></param>'05'
        /// <param name="cps"></param>1
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
                MigrateError("401");
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
