using ASET.Core.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASET.Demo
{
    internal class SecretVault : ISecretVault
    {
        private FrmMain fm = Program.fm;

        public Task<string> GetClientId()
        {
            var c = fm.txtClientId.Text.Trim();
            return Task.FromResult(c);
        }

        public Task<string> GetClientSecret1()
        {
            var c = fm.txtClientSecret1.Text.Trim();
            return Task.FromResult(c);
        }

        public Task<string> GetClientSecret2()
        {
            throw new NotImplementedException();
        }
    }
}
