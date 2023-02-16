using ASET.Core;
using ASET.Core.Authentication;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASET.Demo
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();

            Token.TokenGenerating += Token_TokenGenerating;
            Token.TokenGenerated += Token_TokenGenerated;
            Token.LifeSpanChanged += Token_LifeSpanChanged;
            Token.TokenExpired += Token_TokenExpired;
            Token.TokenDestroyed += Token_TokenDestroyed;

            lblEnvironment.TextChanged += (s, e) =>
            {
                if (lblEnvironment.Text.Equals("N/A"))
                {
                    lblEnvironment.ForeColor = Color.Black;
                }
                else if (lblEnvironment.Text.Equals("Production"))
                {
                    lblEnvironment.ForeColor = Color.Green;
                }
                else
                {
                    lblEnvironment.ForeColor = Color.Red;
                }

            };

            lblTokenStatus.TextChanged += (s, e) =>
            {
                if (lblTokenStatus.Text.Equals("N/A") || lblTokenStatus.Text.Equals("Generating..."))
                {
                    lblTokenStatus.ForeColor = Color.Black;
                }
                else if (lblTokenStatus.Text.Equals("Active"))
                {
                    lblTokenStatus.ForeColor = Color.Green;
                }
                else
                {
                    lblTokenStatus.ForeColor = Color.Red;
                }
            };
        }

        private void GetTokenData()
        {
            lblEnvironment.Text = Token.Environment.ToString();
            txtToken.Text = Token.Value ?? string.Empty;
            lblTokenTimestamp.Text = Token.GenerationTime?.ToString() ?? "N/A";
            lblTokenLiveTime.Text = Token.ExpireIn?.ToString() ?? "N/A";
            lblTokenExpiry.Text = Token.ExpiryTime?.ToString() ?? "N/A";
            lblRemainingLife.Text = "0";
        }

        private void btnSaveConfig_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtClientId.Text.Trim())
                || string.IsNullOrWhiteSpace(txtClientSecret1.Text.Trim())
                || string.IsNullOrWhiteSpace(txtClientSecret2.Text.Trim())
                || string.IsNullOrWhiteSpace(cboEnviron.Text))
            {
                MessageBox.Show("Please supply all fields first.",
                                $"{Text} | Application settings",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
            else
            {
                Properties.Settings.Default.Save();

                MessageBox.Show("Settings saved.",
                                    $"{Text} | Application settings",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
            }

        }

        private void btnClearConfig_Click(object sender, EventArgs e)
        {
            cboEnviron.Text = null;
            Properties.Settings.Default.Reset();
        }

        private async void btnGenerateToken_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtClientId.Text.Trim())
                || string.IsNullOrWhiteSpace(txtClientSecret1.Text.Trim())
                || string.IsNullOrWhiteSpace(txtClientSecret2.Text.Trim())
                || string.IsNullOrWhiteSpace(cboEnviron.Text))
            {
                MessageBox.Show("Please initialize application configuration first.",
                                $"{Text} | Generate a token",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
            else
            {
                SecretVault s = new SecretVault();
                Token.Vault = s;

                //PreProduction / UAT = 0
                //Production          = 1
                //SIT                 = 2
                switch (cboEnviron.SelectedIndex)
                {
                    case 0:
                        Token.Environment = EtEnvironment.PreProduction;
                        break;
                    case 1:
                        Token.Environment = EtEnvironment.Production;
                        break;
                    case 2:
                        Token.Environment = EtEnvironment.SIT;
                        break;
                }

                try
                {
                    await Token.GenerateAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message,
                               $"{Text} | Generate a token",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
                }
            }
        }

        private void Token_TokenGenerating(object sender, EventArgs e)
        {
            lblTokenStatus.Text = "Generating...";
        }

        private void Token_TokenGenerated(object sender, EventArgs e)
        {
            lblTokenStatus.Text = "Active";

            GetTokenData();
        }

        private void Token_LifeSpanChanged(object sender, LifeSpanEventArgs e)
        {
            pBar.Maximum = e.LifeSpan;
            pBar.Value = e.LifeSpanRemaining;

            if (TimeSpan.FromSeconds(e.LifeSpanRemaining).Minutes < 1)
            {
                lblRemainingLife.Text = $"{e.LifeSpanRemaining} second(s)";
            }

            else
            {
                lblRemainingLife.Text = $"{TimeSpan.FromSeconds(e.LifeSpanRemaining).Minutes} minute(s) : " +
                       $"{TimeSpan.FromSeconds(e.LifeSpanRemaining).Seconds} second(s)";
            }
        }

        private async void Token_TokenExpired(object sender, EventArgs e)
        {
            lblTokenStatus.Text = "Expired";

            GetTokenData();
            lblEnvironment.Text = "N/A";

            await Task.Run(() => { Console.Beep(1200, 1000); });
        }

        private void btnDestroyToken_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Token.Value))
            {
                MessageBox.Show("No token to destroy!",
                                $"{Text} | Destroy token",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
            else
            {

                var r = MessageBox.Show("Are you sure?",
                                        $"{Text} | Destroy token",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question);
                if (r == DialogResult.Yes)
                {
                    Token.Destroy();

                    GetTokenData();
                    lblEnvironment.Text = "N/A";
                }
            }
        }

        private void Token_TokenDestroyed(object sender, EventArgs e)
        {
            lblTokenStatus.Text = "N/A";
            pBar.Value = 0;
        }
    }
}