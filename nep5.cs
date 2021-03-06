using Neo.SmartContract.Framework;

using Neo.SmartContract.Framework.Services.Neo;

using System;

using System.Numerics;

using System.ComponentModel;

namespace nepCoin {

   public class NepCoin : SmartContract
   {
       public static readonly byte[] Owner = 
"AGgg3odmiU287C4KKReSy6ntgRxXoU34LQ".ToScriptHash();
       public static event Action<byte[], byte[], BigInteger> tranfer;
       public static object Main(string operation, object[] args)
       {
           if (operation == "name")
           {
               return "Token";
           }
           if (operation == "symbol")
           {
               return "TKN";
           }
           if (operation == "decimals")
           {
               return 2;
           }
           if (operation == "totalSupply")
           {
               return 1000_00;
           }
           if (operation == "balanceOf")
           {
               return BalanceOf((byte[])args[0]);
           }
           if (operation == "transfer")
           {
               var from = (byte[])args[0];
               if (!Runtime.CheckWitness(from))
               {
                   Runtime.Notify("Not Authorized");
                   return false;
               }
               return PerfromTransfer((byte[])args[0], (byte[])args[1], 
(BigInteger)args[2]);
           }
           if(operation == "approve")
           {
               return Approve((byte[])args[0], (byte[])args[1], new 
BigInteger((byte[]) args[2]));
           }
           if(operation == "allowance")
           {
               return Allowance((byte[])args[0], (byte[])args[1]);
           }
           if(operation == "transferFrom")
           {
               return TransferFrom((byte[])args[0], (byte[])args[1], 
(byte[])args[2], new BigInteger((byte[])args[3]));
           }
           if (operation == "deploy")
           {
               return Deploy();
           }
           Runtime.Notify("Could not find action",operation);
           return false;
       }
       public static bool PerfromTransfer(byte[] from, byte[] to, 
BigInteger amount)
       {
           if(from.Length != 20)
           {
               Runtime.Notify("Invalid from");
           }
           if(to.Length != 20)
           {
               Runtime.Notify("Invalid to");
           }
           if (Runtime.CheckWitness(from))
           {
               Runtime.Notify("Not Authorized");
               return false;
           }
           if (from == to)
           {
               Runtime.Notify("Can not seed to self");
               return false;
           }
           if(amount <= 0)
           {
               Runtime.Notify("Invalid Amount");
               return false;
           }
           var fromBalance = BalanceOf(from);
           if (fromBalance - amount < 0)
           {
               Runtime.Notify("Insufficient Fund!");
               return false;
           }
           var toBalance = BalanceOf(to);
           fromBalance = fromBalance - amount;
           toBalance = toBalance + amount;
           Storage.Put(Storage.CurrentContext, from, fromBalance);
           Storage.Put(Storage.CurrentContext, to, toBalance);
           tranfer(from, to, amount);
           return true;
       }
       public static BigInteger BalanceOf(byte[] account)
       {
           var balanceFromStorage = Storage.Get(Storage.CurrentContext, 
account);
           Runtime.Notify("Retrieved Balance", account, 
balanceFromStorage);
           return new BigInteger(balanceFromStorage);
       }
       public static bool Deploy()
       {
           if (!Runtime.CheckWitness(Owner))
           {
               Runtime.Log("You are not authorized!");
               return false;
           }
           var deployComplete = Storage.Get(Storage.CurrentContext, 
"DeploymentComplete");
           if (deployComplete != null)
           {
               Runtime.Log("Can not deploy. Deployment already ran");
               return false;
           }
           Storage.Put(Storage.CurrentContext, "DeploymentComplete", 1);
           Storage.Put(Storage.CurrentContext, Owner, 100_00);
           Runtime.Log("Deployment Complete");
           return true;
       }
       public static bool Approve(byte[] approver, byte[] 
trustee,BigInteger amount)
       {
           if(approver.Length != 20)
           {
               Runtime.Notify("Invalid Approver");
               return false;
           }
           if(trustee.Length != 20)
           {
               Runtime.Notify("Invalid Trustee");
               return false;
           }
           if(amount < 0)
           {
               Runtime.Notify("Approver failure");
               return false;
           }
           if (!Runtime.CheckWitness(approver))
           {
               Runtime.Notify("Approver failure");
               return false;
           }
           Storage.Put(Storage.CurrentContext, approver.Concat(trustee), 
amount);
           return true;
       }
       public static BigInteger Allowance(byte[] approver,byte[] 
trustee)
       {
           return new BigInteger(Storage.Get(Storage.CurrentContext, 
approver.Concat(trustee)));
       }
       public static bool TransferFrom(byte[] trustee,byte[] 
approver,byte[] to,BigInteger amount)
       {
           if(trustee.Length != 20)
           {
               Runtime.Notify("Invalid trustee");
               return false;
           }
           if (approver.Length != 20)
           {
               Runtime.Notify("Invalid approver");
               return false;
           }
           if(to.Length != 20)
           {
               Runtime.Notify("Invalid to");
               return false;
           }
           if (amount <= 0)
           {
               Runtime.Notify("Invalid amount");
               return false;
           }
           if (!Runtime.CheckWitness(trustee)) {
               Runtime.Notify("Trustee Failure");
               return false;
           }
           var approvedAmount = Allowance(approver, trustee);
           if(amount > approvedAmount)
           {
               Runtime.Notify("Allowance Failure");
               return false;
           }
           var transferSuccess = PerfromTransfer(approver, to, amount);
           if (transferSuccess)
           {
               Storage.Put(Storage.CurrentContext, 
approver.Concat(trustee), approvedAmount - amount);
               return true;
           }
           return false;
       }
   }
}
