using System;
using System.ComponentModel;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using NEP = fipcoin.ModuleNEP5;
using TOK = fipcoin.ModuleToken;
using STO = fipcoin.ModuleStorage;
using UAV = fipcoin.Utils;
using Helper = Neo.SmartContract.Framework.Helper;

namespace fipcoin
{
  //PRO
  internal class ModuleProtocol
  {
    internal static object ApplicationMethods(string operation, object[] args)
    {
      if (operation == "PRO_MaxFundAdjust")
      {
        return solveMaxFund((bool)args[0], (string)args[1], (BigInteger)args[2]);
      }

      /******
       * 
       * sec 1: only one participants
       * ****/
      if (!(bool)FIP.KYC("isActiveUser", new object[] { args[0] }))
      {
        Error("In_active_user1" + ((byte[])args[0]).AsString());
        return false;
      }
      if (operation == "PRO_PullPayment")
      {
        return PullPayment((byte[])args[0], (BigInteger)args[1]);
      }

      /******
      * 
      * sec 2: 2 participants required
      * ****/
      switch (operation)
      {

      }
      if (!(bool)FIP.KYC("isActiveUser", new object[] { args[1] }))
      {
        Error("In_active_user2" + ((byte[])args[0]).AsString());
        return false;
      }
      if (operation == "PRO_InstantPay")
      {
        return InstantPay((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);
      }
      if (operation == "PRO_SinglePay")
      {
        return SinglePay((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);
      }
      if (operation == "PRO_Agreement")
      {
        return MakeAgreement((byte[])args[0], (byte[])args[1], (BigInteger)args[2], (BigInteger)args[3], (BigInteger)args[4], (BigInteger)args[5], (uint)args[6]);
      }
      if (operation == "PRO_PullSchedule")
      {
        return PullSchedulePayment((byte[])args[0], (byte[])args[1], (BigInteger)args[2], (byte[])args[3]);
      }
      if (operation == "PRO_CancelSchedulePay")
      {
        return CancelSchedulePayment((byte[])args[0], (byte[])args[1], (byte[])args[2]);
      }
      Error("unsupported_protocol_function");
      return null;
    }

    /***********************************************
     * Begin event definition
     * ***************************************/
    [DisplayName("error")]
    public static event Action<String> Error;
    [DisplayName("agreement_creation")]
    public static event Action<byte[], byte[], BigInteger, BigInteger, BigInteger, BigInteger, uint, byte[]> AgreementCreated;
    [DisplayName("delete_agreement")]
    public static event Action<byte[]> AgreementDeleted;
    [DisplayName("max_fund")]
    public static event Action<string, BigInteger, byte[], byte[]> MaxFund;
    [DisplayName("update_holding")]
    public static event Action<byte[], BigInteger, BigInteger> Holdings;
    private static readonly uint timeCode = 86400;
    /***********************************************
     * Event event definition
     * ***************************************/
    /// <summary>
    /// This function is to directly transfer tokens from one user (from) to another user (to)
    /// if the calling is not from "from", throw error events "Authorization failed"
    /// success processing will create event "transferred"
    /// 
    /// </summary>
    /// <param name="from"></param>  address of paying user, hash 160 
    /// <param name="to"></param> address of benefit, hash 160, different from "to"
    /// <param name="amount"></param> number of tokens to be transferred, > 0 is required
    /// <returns></returns>
    private static bool InstantPay(byte[] from, byte[] to, BigInteger amount)
    {
      if (!Runtime.CheckWitness(from) || !Utils.ValidateNEOAddress(to))
      {
        Error("Authorization failed");
        return false;
      }
      return NEP.Transfer(from, to, amount, (uint)ModuleCommission.getCommission("directP"));
    }

    /// <summary>
    /// This function will temporary withdraw tokesn from one user (from)
    /// -if the calling is not from "from", throw error events "Authorization failed"
    /// -successing processing will create event "transferred" && account balances will be updated on "from" address and admin address 
    /// this balance will be holded for 24 hours. And beside, this will also updated admin balance if there is any balance is avaiable for
    /// benefit account to be withdrawn. If yes, update admin balace and create "update_holding" event
    /// </summary>
    /// <param name="from"></param> address of paying user, hash 160 
    /// <param name="to"></param> address of benefit, hash 160, different from "to"
    /// <param name="value"></param> number of tokens to be handled, >0 is required
    /// <returns></returns>
    private static bool SinglePay(byte[] from, byte[] to, BigInteger value)
    {
      if (!Runtime.CheckWitness(from) || !Utils.ValidateNEOAddress(to))
      {
        Error("Authorization failed");
        return false;
      }
      return DelegateTransfer(from, to, value);
    }

    /// <summary>
    /// Delegate transfer to another address
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private static bool DelegateTransfer(byte[] from, byte[] to, BigInteger value)
    {
      if (NEP.Transfer(from, TOK.Owner, value, 0))
      {
        UpdateHoldings(to, value);
        return true;
      }

      return false;
    }

    //PullPayment payment - except schedule
    /// <summary>
    /// To pull avaiable tokens from admin account to benefit account (to)
    /// -if the calling is not from "to", throw error events "Authorization failed"
    /// -if successfully process, there is a "update_holding" event to be created,
    /// if there is balance to be withdrawn, "transferred" event when be fired - otherwise, there will be error event 
    /// </summary>
    /// <param name="to"></param> address of benefit, hash 160, different from "to"
    /// <param name="value"></param>  number of tokens to be handled, >0 is required
    /// <returns></returns>
    private static bool PullPayment(byte[] to, BigInteger value)
    {
      if (!Runtime.CheckWitness(to))
      {
        Error("Authorization failed");
        return false;
      }
      UpdateHoldings(to, 0);
      bool success = NEP.Transfer(TOK.Owner, to, Storage.Get(Storage.CurrentContext, to + "_holding").AsBigInteger(), (uint)ModuleCommission.getCommission("singleP"));
      if (success)
      {
        Storage.Put(Storage.CurrentContext, to + "_holding", 0); //return value
        return true;
      }
      return false;
    }

    //Create agreeement of schedule paymnet
    /// <summary>
    /// To create a schedule payment with buyer "from" and merchant "to"
    /// -if the calling is not from "from", throw error events "Authorization failed"
    /// -if there all parameters are qualified, create an agreement with ID is the txid of the transaction and fire event "agreement_creation"
    /// </summary>
    /// <param name="from"></param> address of paying user, hash 160 
    /// <param name="to"></param> address of merchant, hash 160
    /// <param name="schedule"></param> type of schedule, 1-2-3 (daily - weekly - monthly) - otherwise error
    /// <param name="startTime"></param> timestamp of the time when 1st payment can take affect, based on Windows timestamp       
    /// <param name="value"></param> tokens of a payment, if value if not fixed -> passed 0
    /// <param name="max"></param> maximum number of tokens of a payment, passed value here if paymnet amount not fixed
    /// <param name="duration"></param> number of schedulling times 
    /// <returns></returns>
    private static bool MakeAgreement(byte[] from, byte[] to, BigInteger schedule, BigInteger startTime, BigInteger value, BigInteger max, uint duration)
    {
      if (!Runtime.CheckWitness(from) || !Utils.ValidateNEOAddress(to))
      {
        Error("Authorization failed");
        return false;
      }
      return Agreement(from, to, schedule, startTime, value, max, duration);
    }

    /******************************************************************
    * PullPayment out money as merchant request
    * Skip all the time if merchant miss the default delay
    * @param
    * from: merchant address in hash160
    * to: buyer address
    * value: amount to poll
    * txid: agreement number
    * **************************************************************/
    private static bool PullSchedulePayment(byte[] from, byte[] to, BigInteger value, byte[] txid)
    {
      if (!Runtime.CheckWitness(to) || !Utils.ValidateNEOAddress(from))
      {
        Error("Authorization failed");
        return false;
      }
      StorageMap contractAgreement = STO.getStorage(txid);

      if (!CheckBeforePullSchedulePayment(from, to, value, txid, contractAgreement)) return false;
      //if value > max
      BigInteger max = contractAgreement.Get("max").AsBigInteger();
      bool maxfund = false;
      if (max != 0 && value > max)
      {
        maxfund = true;
      }

      return DoSchedulePayment(from, to, value, txid, contractAgreement, maxfund);
    }

    private static bool CancelSchedulePayment(byte[] from, byte[] to, byte[] agreementId)
    {
      if (!Runtime.CheckWitness(from) || !Runtime.CheckWitness(to))
      {
        Error("Authorization_failed");
        return false;
      }

      StorageMap agreement = STO.getStorage(agreementId);
      if (agreement.Get("duration").AsBigInteger() == 0)
      {
        Error("Invalid_agreement");
        return false;
      }
      DeleteAgreement(agreement, agreementId);
      return true;
    }

    private static bool DoSchedulePayment(byte[] from, byte[] to, BigInteger value, byte[] txid, StorageMap agreement, bool isToHandleMaxFund)
    //long scheduleTime, BigInteger scheduleTypeFromStorage)
    {
      //Evaluate time 
      long currentTime = UAV.GetCurrentTime();
      uint scheduleType = (uint)agreement.Get("schedule").AsBigInteger();
      long scheduleTime = (long)agreement.Get("nextTime").AsBigInteger();


      if (currentTime < scheduleTime)
      {
        Error("Pull schedule payment: not in time yet");
        return false;
      }
      uint[] paymentInfo = new uint[3];
      //get payment possibilities
      // 0 - nexTime
      // 1 - count 
      // 2 - iswithdraw
      if (scheduleType < 3)
      {
        paymentInfo = ProcessDWPayment(scheduleType, currentTime, scheduleTime);
      } else
      {
        paymentInfo = ProcessMPayment(currentTime, scheduleTime);
      }

      //update
      uint duration = (uint)agreement.Get("duration").AsBigInteger();
      duration = duration - paymentInfo[1];

      if (duration < 0)
      {
        Error("Contract is no longer valid");
        DeleteAgreement(agreement, txid);
        return false;
      } else if (duration == 0)
      {
        DeleteAgreement(agreement, txid);
      }
      else
      {
        agreement.Put("duration", duration);
        agreement.Put("nextTime", scheduleTime + paymentInfo[0]);
      }

      //---if over maxfund
      if (isToHandleMaxFund)
      {
        string maxfundId = Helper.Concat(txid, Helper.AsByteArray(duration)).AsString();
        StorageMap maxFundMap = Storage.CurrentContext.CreateMap(maxfundId);
        maxFundMap.Put("value", value);
        maxFundMap.Put("from", from);
        maxFundMap.Put("to", to);
        //Throw event
        MaxFund(maxfundId, value, from, to);
        return false;
      }
      //-----End of handle max fund

      if (paymentInfo[2] == 1 && duration >= 0) //case of withdrwal
      {
        return NEP.Transfer(from, to, value, (uint)ModuleCommission.getCommission("scheduleP"));
      }

      //if withdrawal
      Error("Pull scheduled payment: not duly");
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="scheduleType"></param>
    /// <returns></returns>
    private static uint GetTimeRelay(uint scheduleType)
    {
      if (scheduleType == 1) // days
      {
        return 43200;
      }

      if (scheduleType == 2) //weeks
      {
        return 259200;
      }

      if (scheduleType == 3) //months
      {
        return 604800;
      }

      return 0;
    }

    /// <summary>
    /// Paymnet info incase of schedule = 1 | 2
    /// </summary>
    /// <param name="scheduleType"></param>
    /// <param name="currentDateTime"></param>
    /// <param name="scheduleDateTime"></param>
    /// <returns></returns>
    private static uint[] ProcessDWPayment(uint scheduleType, long currentDateTime, long scheduleDateTime)
    {
      uint[] paymentInfo = new uint[3];
      uint count = 0; uint isWithraw = 1;
      uint timeRelay = GetTimeRelay(scheduleType);
      uint timeCode = 0;
      if (scheduleType == 1) timeCode = 86400;
      if (scheduleType == 2) timeCode = 604800;

      uint interval = (uint)(currentDateTime - scheduleDateTime);
      uint tmp = interval % timeCode;
      count = interval / timeCode;
      paymentInfo[0] = (count + 1) * timeCode;
      paymentInfo[1] = count;

      //Time to schedule
      if (tmp > timeRelay)
      {
        isWithraw = 0;
      }

      //retun iswithdraw, timeRelay, nextschedule
      paymentInfo[2] = isWithraw;
      return paymentInfo;
    }


    /// <summary>
    /// Process monthly scheduled payment
    /// </summary>
    /// <param name="currentDateTime"></param>
    /// <param name="scheduleDateTime"></param>
    /// <returns></returns>
    private static uint[] ProcessMPayment(long currentDateTime, long scheduleDateTime)
    {
      uint[] paymentInfo = new uint[3];
      uint count = 0; uint isWithraw = 0;
      uint timeRelay = GetTimeRelay(3);

      while (scheduleDateTime <= currentDateTime)
      {
        count = count + 1;
        long afterSchedule = UAV.AddMonths(scheduleDateTime, 1); //get the next time of possible pays
                                                                 // |-------scheduleTime---------xrelay-------------afterSchedule-------|
        if (currentDateTime <= scheduleDateTime + timeRelay)
        {
          isWithraw = 1;
        }
        scheduleDateTime = afterSchedule;
      }

      paymentInfo[2] = isWithraw;
      paymentInfo[1] = count;
      paymentInfo[0] = (uint)(scheduleDateTime - currentDateTime);

      return paymentInfo;
    }

    /// <summary>
    /// Delete an agreement when contract ends
    /// </summary>
    /// <param name="agreement"></param>
    /// <param name="txid"></param>
    internal static void DeleteAgreement(StorageMap agreement, byte[] txid)
    {
      agreement.Delete("from");
      agreement.Delete("to");
      agreement.Delete("nextTime");
      agreement.Delete("schedule");
      agreement.Delete("max");
      agreement.Delete("value");
      agreement.Delete("duration");
      //Throw event
      AgreementDeleted(txid);

    }


    /// <summary>
    /// Check payment
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="value"></param>
    /// <param name="txid"></param>
    /// <param name="agreement"></param>
    /// <returns></returns>
    private static bool CheckBeforePullSchedulePayment(byte[] from, byte[] to, BigInteger value, byte[] txid, StorageMap agreement)
    {
      //Check agreement: existing agreement?
      BigInteger nextTime = agreement.Get("nextTime").AsBigInteger();
      if (nextTime <= 0)
      {
        Error("Pull payment:no agreement");
        return false;
      }

      //Check agreement: envolved parties
      byte[] merchant = agreement.Get("from");
      byte[] buyer = agreement.Get("to");
      if (from != merchant || to != buyer)
      {
        Error("Pull payment:different parties");
        return false;
      }

      //Check agreement: valid amount requested
      BigInteger oldValue = agreement.Get("value").AsBigInteger();
      if (oldValue != 0 && value != oldValue)
      {
        Error("Pull payment: amount not matched");
        return false;
      }

      return true;
    }

    /********************************************
     * Create agreemnet smart contract
     * @parameter
     * from: buyer address (hash 160)
     * to: merchant address (hash 160)
     * schedule: 1- daily 2-monthly 3-weekly;
     * startTime: timestamp
     * value: fixed value -> fix for every time
     * max: -> max number of a pull
     * duration: number of repeat
     * *******************************************/

    private static bool Agreement(byte[] from, byte[] to, BigInteger schedule, BigInteger startTime, BigInteger value, BigInteger max, uint duration)
    {
      Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
      byte[] txid = tx.Hash;
      string[] keyprams = new string[] { txid.AsString() };
      StorageMap txidMap = STO.getStorage(keyprams);
      BigInteger recordTime = txidMap.Get("nextTime").AsBigInteger();
      long currentTime = Utils.GetCurrentTime();

      //Check input parameters
      if (recordTime > 0)
      {
        Error("Agreement: already existed");
        return false;
      }

      if (currentTime > startTime || duration <= 0)
      {
        Error("Agreement: Invalid time value");
        return false;
      }

      if ((value == 0 && max == 0) || (value != 0 && max != 0))
      {
        Error("Agreement: Invalid amount of payment");
        return false;
      }

      if (schedule < 1 || schedule > 3)
      {
        Error("Agreement: Invalid schedule type");
        return false;
      }

      //Record transaction
      txidMap.Put("from", from);
      txidMap.Put("to", to);
      txidMap.Put("nextTime", startTime);
      txidMap.Put("schedule", schedule);
      if (value == 0) txidMap.Put("max", max);
      if (max == 0) txidMap.Put("value", value);
      txidMap.Put("duration", duration);

      //event emitting
      AgreementCreated(@from, to, startTime, schedule, value, max, duration, txid);
      return true;
    }

    //----Begin  3155 max fund handle
    [DisplayName("max_fund_delete")]
    public static event Action<string> DeleteMaxFund;
    [DisplayName("max_fund_reject")]
    public static event Action<string> RejectMaxFund;
    /// <summary>
    /// Handle decision from buyer to withdrawl value > max
    /// </summary>
    /// <param name="decision"></param>
    /// <param name="id"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private static bool solveMaxFund(bool decision, string id, BigInteger value)
    {
      StorageMap maxfund = Storage.CurrentContext.CreateMap(id);
      //Check value
      BigInteger storedValue = maxfund.Get("value").AsBigInteger();
      if (value <= 0 || storedValue != value)
      {
        Error("No record found or value not matched");
        return false;
      }
      byte[] buyer = maxfund.Get("from");
      //validate buyer
      if (!Runtime.CheckWitness(buyer))
      {
        Error("Authorization failed");
        return false;
      }
      byte[] merchant = maxfund.Get("to");


      //Delete the case which has maxfund
      // and do transfer or reject it
      maxfund.Delete("value");
      maxfund.Delete("from");
      maxfund.Delete("to");
      DeleteMaxFund(id);
      if (decision) //grant the request
      {
        return NEP.Transfer(buyer, merchant, value, (uint)ModuleCommission.getCommission("scheduleP"));
      }

      RejectMaxFund(id);
      return false;
    }
    /// <summary>
    /// Update current holdings 
    /// </summary>
    /// <param name="account"></param>
    /// <param name="payment"></param>
    private static void UpdateHoldings(byte[] account, BigInteger payment)
    {
      BigInteger prevTimeStamp = Storage.Get(Storage.CurrentContext, account + "_timestamp").AsBigInteger();
      uint currentTime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
      if (prevTimeStamp == 0)
      {
        //store
        uint startTime = currentTime - (currentTime % timeCode);
        Storage.Put(Storage.CurrentContext, account + "_timestamp", startTime);
        Storage.Put(Storage.CurrentContext, account + "_holding", 0);
        Storage.Put(Storage.CurrentContext, account + "_pending", payment);
        Holdings(account, 0, payment);
      }

      if (prevTimeStamp > 0)
      {
        BigInteger pending = Storage.Get(Storage.CurrentContext, account + "_pending").AsBigInteger();
        if ((currentTime - prevTimeStamp) <= timeCode)
          Storage.Put(Storage.CurrentContext, account + "_pending", pending + payment);
        else
        {
          BigInteger holdings = Storage.Get(Storage.CurrentContext, account + "_holding").AsBigInteger();
          Storage.Put(Storage.CurrentContext, account + "_holding", holdings + pending);
          Storage.Put(Storage.CurrentContext, account + "_pending", payment);
          Storage.Put(Storage.CurrentContext, account + "_timestamp", prevTimeStamp + timeCode);
          Holdings(account, holdings + pending, payment);
        }
      }
    }


  }
}