﻿using HACGUI.Extensions;
using HACGUI.Services;
using LibHac.IO.Save;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HACGUI.Main.SaveManager
{
    /// <summary>
    /// Interaction logic for SaveManagerPage.xaml
    /// </summary>
    public partial class SaveManagerPage : UserControl
    {
        private ulong TitleID;

        public SaveManagerPage(ulong titleId)
        {
            TitleID = titleId;
            InitializeComponent();

            DeviceService.TitlesChanged += RefreshSavesView;

            DeviceService.Start();

            GridView grid = ListView.View as GridView;
            if (titleId == 0)
            {
                grid.Columns.Insert(0, new GridViewColumn()
                {
                    DisplayMemberBinding = new Binding("Owner"),
                    Header = "Owner",
                    Width = double.NaN
                });
            }
            else
            {
                GridViewColumn column = grid.Columns.Where((c) => c.Header as string == "Name/ID").FirstOrDefault();
                if (column != null)
                    grid.Columns.Remove(column);
                else
                {
                    ; // for breakpoint (this should never happen)
                }
            }

            Refresh();
        }

        private void SaveDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            SaveElement element = ListView.SelectedItem as SaveElement;
            SaveInfoWindow window = new SaveInfoWindow(element)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }

        private void RefreshSavesView(Dictionary<ulong, LibHac.Application> apps, Dictionary<ulong, LibHac.Title> titles, Dictionary<string, SaveDataFileSystem> saves)
        {
            Refresh();
        }

        private void Refresh()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ListView.Items.Clear();

                foreach (KeyValuePair<string, SaveDataFileSystem> kv in DeviceService.Saves)
                {
                    SaveElement element = new SaveElement(kv);
                    if (TitleID == 0)
                    {
                        if ((element.SaveId & 0x8000000000000000) != 0)
                            ListView.Items.Add(element);
                    }
                    else
                    {
                        if (element.SaveId == TitleID)
                            ListView.Items.Add(element);
                    }
                }

            }));
        }
    }
}
