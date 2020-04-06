using HACGUI.Extensions;
using HACGUI.Main.TaskManager;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Main.TitleManager.ApplicationWindow.Tabs;
using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using LibHac.Ncm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HACGUI.Main.TitleManager
{
    public class ApplicationElement
    {
        public static readonly ImageSource UnknownIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Img/UnknownTitle.jpg", UriKind.RelativeOrAbsolute));

        static readonly List<ContentMetaType> Priority = new List<ContentMetaType>() { ContentMetaType.Application, ContentMetaType.Patch, ContentMetaType.AddOnContent };

        static ApplicationElement()
        {
            UnknownIcon.Freeze(); // make it thread safe
        }

        public List<Title> Titles { get; set; } = new List<Title>();
        public List<TitleElement> TitleElements
        {
            get
            {
                List<TitleElement> titles = new List<TitleElement>();
                foreach (Title title in Titles)
                    titles.Add(new TitleElement() { Title = title });
                return titles;
            }
        }

        public ImageSource Icon { get; set; } = UnknownIcon;

        public string Name
        {
            get {
                foreach (Title title in Titles)
                {
                    switch (title.Metadata.Type)
                    {
                        case ContentMetaType.Patch:
                        case ContentMetaType.Application:
                            return title.Name;
                    }
                }
                List<Title> titles = OrderTitlesByBest();
                titles.Reverse();
                return SystemTitleNames.GetNameFromTitle(titles.First());
            }
        }
        public ulong TitleId => FindBestTitle().Id;
        public ulong BaseTitleId => FindBestTitle().GetBaseTitleID();
        public string FriendlyVersion
        {
            get
            {
                List<Title> list = OrderTitlesByBest();
                list.Reverse();
                foreach (Title title in list)
                {
                    if (title.Metadata.Type != ContentMetaType.AddOnContent)
                    {
                        if (title.Control != null)
                            return title.Control.DisplayVersion;
                        else
                            return title.Version.ToString();
                    }
                }
                return new TitleVersion(0).ToString();
            }
        }
        public TitleVersion Version
        {
            get
            {
                List<Title> list = OrderTitlesByBest();
                list.Reverse();
                foreach (Title title in list)
                {
                    if (title.Metadata.Type != ContentMetaType.AddOnContent)
                        return title.Version;
                }
                return new TitleVersion(0);
            }
        }
        public List<ContentMetaType> Types
        {
            get
            {
                HashSet<ContentMetaType> set = new HashSet<ContentMetaType>(); // ensure that there are no duplicates
                foreach (Title title in OrderTitlesByBest())
                    set.Add(title.Metadata.Type);
                List<ContentMetaType> list = new List<ContentMetaType>();
                list.AddRange(set);
                return list;
            }
        }
        public string TypesAsString => string.Join(Environment.NewLine, Types);

        public string BcatPassphrase
        {
            get
            {
                List<Title> titles = OrderTitlesByBest();
                Title title = titles.LastOrDefault((x) => x.Metadata.Type == ContentMetaType.Patch);
                if (title == null)
                    title = titles.FirstOrDefault((x) => x.Metadata.Type == ContentMetaType.Application);
                if (title == null || title.Control == null)
                    return "";
                else
                    return title.Control.BcatPassphrase;
            }
        }

        public long Size
        {
            get
            {
                long size = 0;
                foreach (Title title in Titles)
                    size += title.GetSize();
                return size;
            }
        }

        public void Load(ListView view)
        {
            Task<ImageSource> task = GetIconAync();
            task.ContinueWith((source) => 
            {
                Icon = source.Result;
                if (view.GetContainerByItem(this) is ListViewItem container) // find visual container for this element
                    container.Dispatcher.Invoke(() =>
                    {
                        Image image = container.FindVisualChildren<Image>().First(); // find Image object for container
                        BindingExpression binding = image.GetBindingExpression(Image.SourceProperty); // get binding
                        binding.UpdateTarget(); // force refresh
                    });
            });
            TaskManagerPage.Current.Queue.Submit(new RunTask($"Decoding icon for {Name}...", task));
        }

        public Title FindBestTitle()
        {
            foreach(ContentMetaType type in Priority)
            {
                Title title = Titles.FirstOrDefault(x => x?.Metadata.Type == type);
                if (title != null)
                    return title;
            }
            return Titles.First();
        }

        public List<Title> OrderTitlesByBest()
        {
            List<Title> list = new List<Title>();
            foreach (ContentMetaType type in Priority) // first find the types that need to be in a specifc order
            {
                Title title = Titles.FirstOrDefault(x => x?.Metadata.Type == type);
                if (title != null)
                    list.Add(title);
            }
            IEnumerable<Title> unorganized = Titles.Except(list); // then add the types that don't need to be organized afterwards
            list.AddRange(unorganized);
            return list;
        }

        public Task<ImageSource> GetIconAync()
        {
            List<Title> list = OrderTitlesByBest();
            list.Reverse();
            foreach (Title title in list)
            {
                Task<ImageSource> source = FindTitleIcon(title);
                if (source != null)
                    return source;
            }

            return new Task<ImageSource>(() => UnknownIcon);
        }

        public Task<ImageSource> FindTitleIcon(Title title)
        {
            if (title.ControlNca != null)
            {
                IFileSystem controlFS = title.ControlNca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
                DirectoryEntryEx file = controlFS.EnumerateEntries("/", "icon_*.dat").FirstOrDefault();

                if (file != null)
                    return new Task<ImageSource>(() =>
                    {
                        try
                        {
                            controlFS.OpenFile(out IFile jpegFile, file.FullPath.ToU8Span(), OpenMode.Read);
                            JpegBitmapDecoder decoder = new JpegBitmapDecoder(jpegFile.AsStream(), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                            decoder.Frames[0].Freeze();
                            return decoder.Frames[0];
                        } catch(Exception)
                        {
                            return null;
                        }
                    });
            }
            return null;
        }
    }
}
