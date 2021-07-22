using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace VisualStudioEvalExtender
{
    public partial class Form1 : Form
    {
        private readonly Dictionary<string, string> _versionRegSubKeys =
            new Dictionary<string, string>()
            {
                {"2015", @"Licenses\4D8CFBCB-2F6A-4AD2-BABF-10E28F6F2C8F\07078" },
                {"2017", @"Licenses\5C505A59-E312-4B89-9508-E162F8150517\08878" },
                {"2019", @"Licenses\41717607-F34E-432C-A138-A3CFD7E25CDA\09278" }
            };

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            TryPopulateInstalledVisualStudioVersions();            
        }

        private void TryPopulateInstalledVisualStudioVersions()
        {
            cmbVersions.Items.Clear();
            foreach (var item in _versionRegSubKeys)
            {
                RegistryKey subKey = Registry.ClassesRoot.OpenSubKey(item.Value, false);
                if (subKey != null)
                {
                    cmbVersions.Items.Add(item.Key);
                    subKey.Close();
                }
            }

            if (cmbVersions.Items.Count < 0)
            {
                MessageBox.Show("Visual Studio not installed!!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void BtnExtend_Click(object sender, EventArgs e)
        {

            if(cmbVersions.SelectedIndex < 0 || !int.TryParse(txtMonths.Text, out var result))
            {
                MessageBox.Show("Please select Visual Studio version OR enter proper month!!!");
                return;
            }

            try
            {
                var subKey = _versionRegSubKeys[cmbVersions.SelectedItem.ToString()];
                SetDate(subKey, result);
                MessageBox.Show("License extended successfully.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                GetDate(subKey);
            }
            catch
            {
                MessageBox.Show("The license couldn't be extended.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CmbVersions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cmbVersions.SelectedIndex > -1)
            {
                GetDate(_versionRegSubKeys[cmbVersions.SelectedItem.ToString()]);
            }
        }

        private void GetDate(string regSubKey)
        {
            byte[] decryptedData = OpenHKCRSubKey(regSubKey);
            string expirationDate = ConvertFromBinaryDate(decryptedData);

            dtpExpirationDate.Text = expirationDate;            
        }

        private void SetDate(string regSubKey, int months)
        {
            byte[] decryptedData = OpenHKCRSubKey(regSubKey);
            DateTime expirationDate = dtpExpirationDate.Value.AddMonths(months);
            byte[] newData = ConvertToBinaryDate(decryptedData, expirationDate.Year, expirationDate.Month, expirationDate.Day);

            WriteHKCRSubKey(newData, regSubKey);
        }

        private byte[] OpenHKCRSubKey(string subKey)
        {
            RegistryKey regHKCR = Registry.ClassesRoot.OpenSubKey(subKey, true);

            byte[] encryptedData = (byte[])regHKCR.GetValue("", true);
            byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.LocalMachine);

            regHKCR.Close();

            return decryptedData;
        }

        private void WriteHKCRSubKey(byte[] decryptedData, string subKey)
        {
            RegistryKey regHKCR = Registry.ClassesRoot.OpenSubKey(subKey, true);

            byte[] encryptedData = ProtectedData.Protect(decryptedData, null, DataProtectionScope.LocalMachine);

            regHKCR.SetValue("", encryptedData);
            regHKCR.Close();
        }

        private string ConvertFromBinaryDate(byte[] decryptedData)
        {
            // Year

            byte[] YearB = new byte[2];
            YearB[0] = decryptedData[decryptedData.Length - 15];
            YearB[1] = decryptedData[decryptedData.Length - 16];

            string YearS = "";
            YearS += BitConverter.ToString(YearB, 0, 1);
            YearS += BitConverter.ToString(YearB, 1, 1);

            int YearI = int.Parse(YearS, System.Globalization.NumberStyles.HexNumber);

            // Month

            byte[] MonthB = new byte[2];
            MonthB[0] = decryptedData[decryptedData.Length - 13];
            MonthB[1] = decryptedData[decryptedData.Length - 14];

            string MonthS = "";
            MonthS += BitConverter.ToString(MonthB, 0, 1);
            MonthS += BitConverter.ToString(MonthB, 1, 1);

            int MonthI = int.Parse(MonthS, System.Globalization.NumberStyles.HexNumber);

            // Day

            byte[] DayB = new byte[2];
            DayB[0] = decryptedData[decryptedData.Length - 11];
            DayB[1] = decryptedData[decryptedData.Length - 12];

            string DayS = "";
            DayS += BitConverter.ToString(DayB, 0, 1);
            DayS += BitConverter.ToString(DayB, 1, 1);

            int DayI = int.Parse(DayS, System.Globalization.NumberStyles.HexNumber);

            string ExpirationDate = String.Format("{0:00}", DayI) + "/" + String.Format("{0:00}", MonthI) + "/" + YearI.ToString();

            return ExpirationDate;
        }

        private byte[] ConvertToBinaryDate(byte[] DecryptedData, int Year, int Month, int Day)
        {
            // Year

            byte[] YearB = BitConverter.GetBytes(Year);
            DecryptedData[DecryptedData.Length - 16] = YearB[0];
            DecryptedData[DecryptedData.Length - 15] = YearB[1];

            // Month

            byte[] MonthB = BitConverter.GetBytes(Month);
            DecryptedData[DecryptedData.Length - 14] = MonthB[0];
            DecryptedData[DecryptedData.Length - 13] = MonthB[1];

            // Day

            byte[] DayB = BitConverter.GetBytes(Day);
            DecryptedData[DecryptedData.Length - 12] = DayB[0];
            DecryptedData[DecryptedData.Length - 11] = DayB[1];

            return DecryptedData;
        }
    }
}
