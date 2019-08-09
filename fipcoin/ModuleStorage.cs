using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;

namespace fipcoin
{
    //STO
    internal static class ModuleStorage
    {
        /// <summary>
        /// Get storage context
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static StorageMap getStorage(params string[] keys)
        {
            string finalKey = combineKeys(keys);
            return Storage.CurrentContext.CreateMap(finalKey);
        }
        public static StorageMap getStorage(byte[] key)
        {
            return Storage.CurrentContext.CreateMap(key.AsString());
        }
        /// <summary>
        /// build up private keys from individual keywords
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        private static string combineKeys(params string[] keys)
        {
            string finalKey = "";

            foreach (string key in keys)
            {
                finalKey = finalKey + key;
            }
            return finalKey;
        }   
    }
}