using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;

namespace fipcoin
{
    //UAV

    internal class Utils
    {
        internal static object ApplicationMethods(string operation, object[] args)
        {
            if (operation.Equals("UAV_Migrate"))
            {
                return UpgradeSmartContract((byte[])args[0], (byte[])args[1], (byte)args[2], (string)args[3], (string)args[4], (string)args[5], (string)args[6], (string)args[7]);
            } 
            if (operation.Equals("UAV_Destroy"))
            {
                return DestroySmartContract();
            }
            return null;
        }

        internal static bool ValidateNEOAddress(byte[] address)
        {
            if (address.Length != 20)
                return false;
            if (address.AsBigInteger() == 0)
                return false;
            return true;
        }
        /// <summary>
        /// Migrate all the data to a new smart contract
        /// </summary>
        /// <param name="newScript"></param>
        /// <param name="newName"></param>
        /// <param name="newVersion"></param>
        /// <param name="newAuthor"></param>
        /// <param name="newEmail"></param>
        /// <param name="newDescription"></param>
        /// <returns></returns>
        private static bool UpgradeSmartContract(byte[] newScript, byte[]inputparams, byte outputparam, string newName, string newVersion, string newAuthor, string newEmail, string newDescription)
        {
            if (!Runtime.CheckWitness(ModuleToken.Owner))
            {
                return false;
            }
            Contract.Migrate(newScript, inputparams, outputparam, ContractPropertyState.HasStorage, newName, newVersion, newAuthor, newEmail, newDescription);
            return true;
        }
        /// <summary>
        /// Destroy a smart contract
        /// </summary>
        /// <returns></returns>
        private static bool DestroySmartContract()
        {
            if (!Runtime.CheckWitness(ModuleToken.Owner))
            {
                return false;
            }
            Contract.Destroy();
            return true;
        }

        /*
         * DateTime Library
         */

        private static int[] DaysToMonth365()
        {
            var ret = new int[13];
            ret[0]  = 0;
            ret[1]  = 31;
            ret[2]  = 59;
            ret[3]  = 90;
            ret[4]  = 120;
            ret[5]  = 151;
            ret[6]  = 181;
            ret[7]  = 212;
            ret[8]  = 243;
            ret[9]  = 273;
            ret[10] = 304;
            ret[11] = 334;
            ret[12] = 365;
            return ret;
        }

        private static int[] DaysToMonth366()
        {
            var ret = new int[13];
            ret[0]  = 0;
            ret[1]  = 31;
            ret[2]  = 60;
            ret[3]  = 91;
            ret[4]  = 121;
            ret[5]  = 152;
            ret[6]  = 182;
            ret[7]  = 213;
            ret[8]  = 244;
            ret[9]  = 274;
            ret[10] = 305;
            ret[11] = 335;
            ret[12] = 366;
            return ret;
        }

        internal static long UnixTimeStamp2WindowsTimeStamps(uint timeStamp)
        {
            return timeStamp + 62135596800L;
        }

        internal static long GetCurrentTime()
        {
            return UnixTimeStamp2WindowsTimeStamps(Runtime.Time);
        }

        private static bool IsLeapYear(int year)
        {
            if (year % 4 != 0)
                return false;
            if (year % 100 == 0)
                return year % 400 == 0;
            return true;
        }

        private static int DaysInMonth(int year, int month)
        {
            int[] numArray = IsLeapYear(year) ? DaysToMonth366() : DaysToMonth365();
            return numArray[month] - numArray[month - 1];
        }

        private static int[] GetDatePart(long ticksInSeconds)
        {
            int year, month, day;
            int num1 = (int) (ticksInSeconds / 86400L);
//            int num1 = (int) (seconds / 864000000000L);
            int num2 = num1 / 146097;
            int num3 = num1 - num2 * 146097;
            int num4 = num3 / 36524;
            if (num4 == 4)
                num4 = 3;
            int num5 = num3 - num4 * 36524;
            int num6 = num5 / 1461;
            int num7 = num5 - num6 * 1461;
            int num8 = num7 / 365;
            if (num8 == 4)
                num8 = 3;
            year = num2 * 400 + num4 * 100 + num6 * 4 + num8 + 1;
            int   num9     = num7 - num8 * 365;
            int[] numArray = num8 == 3 && (num6 != 24 || num4 == 3) ? DaysToMonth366() : DaysToMonth365();
            int   index    = (num9 >> 5) + 1;
            while (num9 >= numArray[index])
                ++index;
            month = index;
            day   = num9 - numArray[index - 1] + 1;

            int[] ret = new int[3];
            ret[0] = year;
            ret[1] = month;
            ret[2] = day;
            return ret;
        }

        private static long DateToSeconds(int year, int month, int day)
        {
            if (year >= 1 && year <= 9999 && (month >= 1 && month <= 12))
            {
                int[] numArray = IsLeapYear(year) ? DaysToMonth366() : DaysToMonth365();
                if (day >= 1 && day <= numArray[month] - numArray[month - 1])
                {
                    int num = year                                                                         - 1;
                    return (long) (num * 365 + num / 4 - num / 100 + num / 400 + numArray[month - 1] + day - 1) * 86400L;
                }
            }

            return 0;
        }

        internal static long AddMonths(long ticksInSeconds, int months)
        {
            var r = GetDatePart(ticksInSeconds);

            int year1 = r[0];
            int month = r[1];
            int day   = r[2];

            int num1 = month - 1 + months;
            int year2;
            if (num1 >= 0)
            {
                month = num1 % 12 + 1;
                year2 = year1     + num1 / 12;
            }
            else
            {
                month = 12    + (num1 + 1)  % 12;
                year2 = year1 + (num1 - 11) / 12;
            }

            int num2 = DaysInMonth(year2, month);
            if (day > num2)
                day = num2;

            long ret = DateToSeconds(year2, month, day) + (ticksInSeconds % 86400L);

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ticksInSeconds">Windows ticks in seconds, not Unix timestamps</param>
        /// <param name="days"></param>
        /// <returns></returns>
        internal static long AddDays(long ticksInSeconds, int days)
        {
            return ticksInSeconds + 86400 * days; //Add(value, 86400000);
        }
    }
}