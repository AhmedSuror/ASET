using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ASET.Core.Authentication
{
    /// <summary>
    /// The vault that holds user data and secrets for ETA Authentication.
    /// </summary>
    public interface ISecretVault
    {
        Task<string> GetClientId();
        Task<string> GetClientSecret1();
        Task<string> GetClientSecret2();
    }
}
