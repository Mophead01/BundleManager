using AutoBundleManager.DependencyDetector;
using Frosty.Controls;
using Frosty.Core;
using FrostySdk.Ebx;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace AutoBundleManagerPlugin
{
    [TemplatePart(Name = PART_BundlesSelected, Type = typeof(TextBox))]
    [TemplatePart(Name = PART_BundlesList, Type = typeof(TextBox))]
    public class DependencyTabItem : FrostyTabItem
    {
        private const string PART_BundlesSelected = "PART_BundlesSelected";
        private const string PART_BundlesList = "PART_BundlesList";

        private TextBlock bundlesSelectedTextBlock;
        private TextBox dependenciesListTextBox;

        private TabControl miscTabControl;

        static DependencyTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DependencyTabItem), new FrameworkPropertyMetadata(typeof(DependencyTabItem)));
        }

        public DependencyTabItem()
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            bundlesSelectedTextBlock = GetTemplateChild(PART_BundlesSelected) as TextBlock;
            dependenciesListTextBox = GetTemplateChild(PART_BundlesList) as TextBox;

            App.EditorWindow.DataExplorer.SelectionChanged += dataExplorer_SelectionChanged;
            App.EditorWindow.MiscTabControl.SelectionChanged += miscTabControl_SelectionChanged;

            miscTabControl = App.EditorWindow.MiscTabControl;
        }

        private void dataExplorer_SelectionChanged(object sender, RoutedEventArgs e)
        {
            EbxAssetEntry selectedEntry = App.SelectedAsset;

            RefreshBundles(selectedEntry);
        }

        private void miscTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EbxAssetEntry selectedEntry = App.SelectedAsset;

            RefreshBundles(selectedEntry);
        }

        public void RefreshBundles(EbxAssetEntry entry)
        {
            // TODO: Figure out how to get tab item ref
            //if (!bundleTabItem.IsSelected)
            //    return;

            if (entry == null)
            {
                bundlesSelectedTextBlock.Text = "No asset selected";
                dependenciesListTextBox.Text = "";
                return;
            }
            Dependencies parDependecies = new Dependencies(entry, App.AssetManager.GetEbx(entry));

            StringBuilder sbEbxPr = new StringBuilder();
            StringBuilder sbResId = new StringBuilder();
            StringBuilder sbChunk = new StringBuilder();
            StringBuilder sbTextRef = new StringBuilder();

            foreach (Guid guid in parDependecies.ebxPointerGuids)
                sbEbxPr.AppendLine($"Guid:\t{guid} \tName:\t{(App.AssetManager.GetEbxEntry(guid) != null ? App.AssetManager.GetEbxEntry(guid).Name : "ERROR MISSING EBX")}");

            foreach (ulong resRid in parDependecies.resRids)
                sbResId.AppendLine($"ResRid:\t{resRid}\tName:\t{(App.AssetManager.GetResEntry(resRid) != null ? App.AssetManager.GetResEntry(resRid).Name : "ERROR MISSING RES")}");

            foreach (Guid chkGuid in parDependecies.chunkGuids)
                sbChunk.AppendLine($"Chunk:\t{chkGuid}\t{(App.AssetManager.GetChunkEntry(chkGuid) != null ? "" : "ERROR CHUNK DOES NOT EXIST")}");

            foreach (string txtRef in parDependecies.refNames)
                sbTextRef.AppendLine($"Text Reference:\t\"{txtRef}\"");

            bundlesSelectedTextBlock.Text = "Bundle Manager Detected Dependencies Of " + entry.Filename;

            dependenciesListTextBox.Text = "";

            if (sbEbxPr.Length > 0)
                dependenciesListTextBox.Text += "\r\nEbx PointerRefs:\r\n" + sbEbxPr.ToString();

            if (sbResId.Length > 0)
                dependenciesListTextBox.Text += "\r\nResource References:\r\n" + sbResId.ToString();

            if (sbChunk.Length > 0)
                dependenciesListTextBox.Text += "\r\nChunk Guids:\r\n" + sbChunk.ToString();

            if (sbTextRef.Length > 0)
                dependenciesListTextBox.Text += "\r\nText References:\r\n" + sbTextRef.ToString();

            if (dependenciesListTextBox.Text.Length > 0)
                dependenciesListTextBox.Text = string.Join("\n", dependenciesListTextBox.Text.Split('\n').Skip(1));
        }
    }
    public class DependencyViewer : TabExtension
    {
        public override string TabItemName => "Dependencies";

        public override FrostyTabItem TabContent => new DependencyTabItem();
    }
}
