﻿using MultiMiner.Engine.Data;
using MultiMiner.Utility.Forms;
using MultiMiner.Utility.OS;
using MultiMiner.Utility.Serialization;
using MultiMiner.Xgminer.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MultiMiner.Win.Forms.Configuration
{
    public partial class CoinsForm : MessageBoxFontForm
    {
        private readonly List<Engine.Data.Configuration.Coin> configurations = new List<Engine.Data.Configuration.Coin>();
        private readonly List<CryptoCoin> knownCoins;

        public CoinsForm(List<Engine.Data.Configuration.Coin> configurations, List<CryptoCoin> knownCoins)
        {
            this.configurations = configurations;
            this.knownCoins = knownCoins;
            InitializeComponent();
        }

        private void CoinsForm_Load(object sender, EventArgs e)
        {
            //not supported on mono
            if (OSVersionPlatform.GetGenericPlatform() != PlatformID.Unix)
                coinListBox.AllowDrop = true;
            PopulateConfigurations();
            UpdateButtonStates();
        }
        
        private void removeCoinButton_Click(object sender, EventArgs e)
        {
            DialogResult promptResult = MessageBox.Show("Remove the selected coin configuration?", "Confirm", MessageBoxButtons.YesNo);
            if (promptResult == System.Windows.Forms.DialogResult.Yes)
            {
                //required to clear bindings if this was the last coin in the list
                coinConfigurationBindingSource.DataSource = typeof(Engine.Data.Configuration.Coin);
                miningPoolBindingSource.DataSource = typeof(MiningPool);

                Engine.Data.Configuration.Coin configuration = configurations[coinListBox.SelectedIndex];
                configurations.Remove(configuration);
                coinListBox.Items.RemoveAt(coinListBox.SelectedIndex);

                //select a coin - otherwise nothing will be selected
                if (configurations.Count > 0)
                    coinListBox.SelectedIndex = 0;
            }
        }

        private void PopulateConfigurations()
        {
            coinListBox.Items.Clear();

            foreach (Engine.Data.Configuration.Coin configuration in configurations)
                coinListBox.Items.Add(configuration.CryptoCoin.Name);

            if (configurations.Count > 0)
                coinListBox.SelectedIndex = 0;
        }
        
        private void AddCoinConfiguration(CryptoCoin cryptoCoin)
        {
            //don't allow two configurations for the same coin symbol
            Engine.Data.Configuration.Coin configuration = configurations.SingleOrDefault(c => c.CryptoCoin.Symbol.Equals(cryptoCoin.Symbol, StringComparison.OrdinalIgnoreCase));
            if (configuration == null)
                //don't allow two configurations for the same coin name
                configuration = configurations.SingleOrDefault(c => c.CryptoCoin.Name.Equals(cryptoCoin.Name, StringComparison.OrdinalIgnoreCase));

            if (configuration != null)
            {
                coinListBox.SelectedIndex = configurations.IndexOf(configuration);
            }
            else
            {
                configuration = new Engine.Data.Configuration.Coin();

                configuration.CryptoCoin = knownCoins.SingleOrDefault(c => c.Symbol.Equals(cryptoCoin.Symbol, StringComparison.OrdinalIgnoreCase));

                //user may have manually entered a coin
                if (configuration.CryptoCoin == null)
                {
                    configuration.CryptoCoin = new CryptoCoin();
                    configuration.CryptoCoin.Name = cryptoCoin.Name;
                    configuration.CryptoCoin.Symbol = cryptoCoin.Symbol;
                    configuration.CryptoCoin.Algorithm = cryptoCoin.Algorithm;
                }

                configuration.Pools.Add(new MiningPool());

                configurations.Add(configuration);

                coinListBox.Items.Add(configuration.CryptoCoin.Name);
                coinListBox.SelectedIndex = configurations.IndexOf(configuration);
            }

            hostEdit.Focus();
        }

        private void coinListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (coinListBox.SelectedIndex >= 0)
            {
                Engine.Data.Configuration.Coin configuration = configurations[coinListBox.SelectedIndex];

                coinConfigurationBindingSource.DataSource = configuration;
                miningPoolBindingSource.DataSource = configuration.Pools;
                poolListBox.DataSource = miningPoolBindingSource;
                poolListBox.DisplayMember = "Host";
            }

            UpdateButtonStates();
        }

        private void addPoolButton_Click(object sender, EventArgs e)
        {
            Engine.Data.Configuration.Coin configuration = configurations[coinListBox.SelectedIndex];
            miningPoolBindingSource.Add(new MiningPool());
            poolListBox.SelectedIndex = configuration.Pools.Count - 1;
            hostEdit.Focus();
        }

        private void removePoolButton_Click(object sender, EventArgs e)
        {
            DialogResult promptResult = MessageBox.Show("Remove the selected pool configuration?", "Confirm", MessageBoxButtons.YesNo);
            if (promptResult == System.Windows.Forms.DialogResult.Yes)
            {
                miningPoolBindingSource.RemoveAt(poolListBox.SelectedIndex);
                hostEdit.Focus();
            }
        }

        private void UpdateButtonStates()
        {
            addPoolButton.Enabled = coinListBox.SelectedIndex >= 0;
            removePoolButton.Enabled = (coinListBox.SelectedIndex >= 0) && (poolListBox.SelectedIndex >= 0);
            removeCoinButton.Enabled = (coinListBox.SelectedIndex >= 0) && (coinListBox.SelectedIndex >= 0);
            poolUpButton.Enabled = (poolListBox.SelectedIndex >= 1);
            poolDownButton.Enabled = (poolListBox.SelectedIndex < poolListBox.Items.Count - 1);
        }
        
        private void poolListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void adjustProfitCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (coinConfigurationBindingSource.Current == null)
                return;

            Engine.Data.Configuration.Coin currentConfiguration = (Engine.Data.Configuration.Coin)coinConfigurationBindingSource.Current;
            currentConfiguration.ProfitabilityAdjustmentType = (Engine.Data.Configuration.Coin.AdjustmentType)((ComboBox)sender).SelectedIndex;
        }

        private void coinConfigurationBindingSource_CurrentChanged(object sender, EventArgs e)
        {
            if (coinConfigurationBindingSource.Current == null)
                return;

            Engine.Data.Configuration.Coin currentConfiguration = (Engine.Data.Configuration.Coin)coinConfigurationBindingSource.Current;
            adjustProfitCombo.SelectedIndex = (int)currentConfiguration.ProfitabilityAdjustmentType;
        }

        private void poolUpButton_Click(object sender, EventArgs e)
        {
            MoveSelectedPool(-1);
        }

        private void poolDownButton_Click(object sender, EventArgs e)
        {
            MoveSelectedPool(1);
        }

        private void MoveSelectedPool(int offset)
        {
            Object currentObject = miningPoolBindingSource.Current;
            int currentIndex = miningPoolBindingSource.IndexOf(currentObject);
            int newIndex = currentIndex + offset;
            miningPoolBindingSource.RemoveAt(currentIndex);
            miningPoolBindingSource.Insert(newIndex, currentObject);
            miningPoolBindingSource.Position = newIndex;
            poolListBox.Focus();
        }

        private void addCoinButton_Click(object sender, EventArgs e)
        {
            CoinChooseForm coinChooseForm = new CoinChooseForm(knownCoins);
            DialogResult dialogResult = coinChooseForm.ShowDialog();
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
                AddCoinConfiguration(coinChooseForm.SelectedCoin);
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (this.ValidateChildren())
            {
                DialogResult = System.Windows.Forms.DialogResult.OK;
            }
            else
            {
                userNameEdit.Focus();
            }
        }

        private void toolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "CoinConfigurations.xml";
            openFileDialog1.Title = "Import CoinConfigurations.xml";
            openFileDialog1.Filter = "XML files|*.xml";
            openFileDialog1.DefaultExt = ".xml";

            DialogResult dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                string sourceFileName = openFileDialog1.FileName;

                MergeConfigurationsFromFile(sourceFileName);

                PopulateConfigurations();
            }
        }

        private void MergeConfigurationsFromFile(string configurationsFileName)
        {
            List<Engine.Data.Configuration.Coin> sourceConfigurations = ConfigurationReaderWriter.ReadConfiguration<List<Engine.Data.Configuration.Coin>>(configurationsFileName);
            List<Engine.Data.Configuration.Coin> destinationConfigurations = configurations;

            foreach (Engine.Data.Configuration.Coin sourceConfiguration in sourceConfigurations)
            {
                int existingIndex = destinationConfigurations.FindIndex(c => c.CryptoCoin.Symbol.Equals(sourceConfiguration.CryptoCoin.Symbol));
                if (existingIndex == -1)
                    destinationConfigurations.Add(sourceConfiguration);
                else
                    destinationConfigurations[existingIndex] = sourceConfiguration;
            }
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "CoinConfigurations.xml";
            saveFileDialog1.Title = "Export CoinConfigurations.xml";
            saveFileDialog1.Filter = "XML files|*.xml";
            saveFileDialog1.DefaultExt = ".xml";

            DialogResult dialogResult = saveFileDialog1.ShowDialog();
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                //string sourceFileName = coinConfigurationsFileName;
                string destinationFileName = saveFileDialog1.FileName;
                if (File.Exists(destinationFileName))
                    File.Delete(destinationFileName);
                ConfigurationReaderWriter.WriteConfiguration(configurations, destinationFileName);
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            SortConfigurations();
        }

        private void SortConfigurations()
        {
            configurations.Sort((config1, config2) => config1.CryptoCoin.Name.CompareTo(config2.CryptoCoin.Name));
            PopulateConfigurations();
        }

        private void coinListBox_MouseMove(object sender, MouseEventArgs e)
        {
            //not supported on mono
            if (OSVersionPlatform.GetGenericPlatform() == PlatformID.Unix)
                return;

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (coinListBox.SelectedItem == null) return;
                coinListBox.DoDragDrop(coinListBox.SelectedItem, DragDropEffects.Move);
            }
        }

        private void coinListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void coinListBox_DragDrop(object sender, DragEventArgs e)
        {
            Point point = coinListBox.PointToClient(new Point(e.X, e.Y));
            int index = coinListBox.IndexFromPoint(point);
            if (index < 0) index = coinListBox.Items.Count - 1;

            string coinName = (string)e.Data.GetData(typeof(string));

            MoveCoinToIndex(coinName, index);
        }

        private void MoveCoinToIndex(string coinName, int index)
        {
            Engine.Data.Configuration.Coin configuration = configurations.Single(
                config => config.CryptoCoin.Name.Equals(coinName, StringComparison.OrdinalIgnoreCase));

            configurations.Remove(configuration);
            configurations.Insert(index, configuration);

            coinListBox.Items.Remove(coinName);
            coinListBox.Items.Insert(index, coinName);

            coinListBox.SelectedIndex = index;
        }
    }
}
