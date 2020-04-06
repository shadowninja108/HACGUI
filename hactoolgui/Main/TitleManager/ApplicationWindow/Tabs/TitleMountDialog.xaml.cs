﻿using HACGUI.Extensions;
using HACGUI.Main.TaskManager;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Services;
using LibHac;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs
{
    /// <summary>
    /// Interaction logic for TitleMountDialog.xaml
    /// </summary>
    public partial class TitleMountDialog : Window
    {
        private readonly Dictionary<NcaFormatType, List<Tuple<SwitchFsNca, int>>> Indexed;

        public TitleMountDialog(Dictionary<NcaFormatType, List<Tuple<SwitchFsNca, int>>> indexed)
        {
            InitializeComponent();
            Indexed = indexed;

            bool hasPatch = false;
            foreach (Tuple<SwitchFsNca, int> t in Indexed.Values.SelectMany(i => i))
            {
                NcaFsHeader section = t.Item1.Nca.Header.GetFsHeader(t.Item2);
                if(section.IsPatchSection())
                {
                    hasPatch = true;
                    break;
                }
            }

            if (Indexed.ContainsKey(NcaFormatType.Romfs) || hasPatch)
                ComboBox.Items.Add(MountType.Romfs);
            if (Indexed.ContainsKey(NcaFormatType.Pfs0))
                ComboBox.Items.Add(MountType.Exefs);
        }


        private void MountClicked(object sender, RoutedEventArgs e)
        {
            if (ComboBox.SelectedItem == null)
                return;

            MountType mountType = (MountType) ComboBox.SelectedItem;
            NcaFormatType sectionType = NcaFormatType.Romfs;
            switch (mountType)
            {
                case MountType.Exefs:
                    sectionType = NcaFormatType.Pfs0;
                    break;
                case MountType.Romfs:
                    sectionType = NcaFormatType.Romfs;
                    break;
            }
            List<IFileSystem> filesystems = new List<IFileSystem>();
            IEnumerable<Tuple<SwitchFsNca, int>> list = Indexed[sectionType];
            TaskManagerPage.Current.Queue.Submit(new RunTask("Opening filesystems to mount...", new Task(() => 
            {
                foreach (Tuple<SwitchFsNca, int> t in list)
                {
                    SwitchFsNca nca = t.Item1;
                    NcaFsHeader section = t.Item1.Nca.Header.GetFsHeader(t.Item2);
                    int index = t.Item2;

                    /*IStorage inStorage = nca.OpenStorage(index, IntegrityCheckLevel.ErrorOnInvalid);
                    IFile outFile = new LocalFile("./tmp.bin", OpenMode.Write | OpenMode.AllowAppend);
                    IStorage outStorage = outFile.AsStorage();
                    inStorage.GetSize(out long size);
                    long buffLen = 0x10000;
                    for (int i = 0; i < (int)Math.Min(Math.Ceiling((double)size / buffLen), 5); i++)
                    {
                        long off = i * buffLen;
                        long left = size - off;
                        long toRead = Math.Min(buffLen, left);

                        byte[] buff = new byte[toRead];

                        inStorage.Read(off, buff);
                        outStorage.Write(off, buff);
                    }
                    //inStorage.Dispose();
                    outFile.Dispose();*/

                    filesystems.Add(nca.OpenFileSystem(index, IntegrityCheckLevel.ErrorOnInvalid));
                }
                filesystems.Reverse();
                LayeredFileSystem fs = new LayeredFileSystem(filesystems);
                string typeString = sectionType.ToString();
                MountService.Mount(new MountableFileSystem(fs, $"Mounted {mountType.ToString().ToLower()}", typeString, OpenMode.Read));
            })));
        }

        public enum MountType
        {
            Romfs, Exefs
        }
    }
}
