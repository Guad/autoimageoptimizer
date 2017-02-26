using System;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Configuration;
using System.Diagnostics;
using System.IO;


namespace AutoImageOptimizer
{
    public partial class Optimizer : ServiceBase
    {
        private IOptimizer[] _optimizers;
        private FileSystemWatcher[] _watchers;
        private EventLog _eventLog;

        public Optimizer()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _eventLog = new EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("AutoImageOptimizer"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "AutoImageOptimizer", "Application");
            }
            _eventLog.Source = "AutoImageOptimizer";
            _eventLog.Log = "Application";

            _eventLog.WriteEntry("Starting up @ " + DateTime.Now);

            _optimizers =
                Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => !t.IsInterface && Array.IndexOf(t.GetInterfaces(), typeof(IOptimizer)) != -1)
                    .Select(t => Activator.CreateInstance(t) as IOptimizer)
                    .Where(i => i != null)
                    .ToArray();

            _eventLog.WriteEntry("Found " + _optimizers.Length + " optimizers.");

            string[] directories = ReadLookDirectories();

            if (directories != null)
            {
                _eventLog.WriteEntry("Directories found: " + directories.Length);
                _watchers = new FileSystemWatcher[directories.Length];

                for (int i = 0; i < directories.Length; i++)
                {
                    _eventLog.WriteEntry("Starting watch for " + directories[i]);

                    FileSystemWatcher watcher = new FileSystemWatcher(directories[i]);
                    watcher.Filter = "*.*";
                    watcher.IncludeSubdirectories = true;
                    watcher.EnableRaisingEvents = true;
                    watcher.Created += OnFileCreated;

                    _watchers[i] = watcher;
                }
            }

            
        }

        protected override void OnStop()
        {
            if (_watchers != null)
                for (int i = 0; i < _watchers.Length; i++)
                {
                    _watchers[i].EnableRaisingEvents = false;
                    _watchers[i].Dispose();
                }

            _eventLog.Close();

            _watchers = null;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            string extension = Path.GetExtension(e.FullPath);

            foreach (IOptimizer optimizer in _optimizers)
                if (optimizer.Optimizable(extension))
                {
                    _eventLog.WriteEntry("Optimizing file " + e.FullPath);

                    optimizer.Optimize(e.FullPath, AcquireStream(e.FullPath));
                    break;
                }
        }

        private Stream AcquireStream(string path)
        {
            int numTries = 0;

            while (numTries <= 10)
            {
                ++numTries;
                try
                {
                    FileStream fs = new FileStream(path,
                        FileMode.Open, FileAccess.ReadWrite,
                        FileShare.None);
                    fs.ReadByte();

                    // If we got this far the file is ready
                    fs.Seek(0, SeekOrigin.Begin);
                    return fs;
                }
                catch (Exception)
                {
                    System.Threading.Thread.Sleep(500);
                }
            }

            return null;
        }

        private string[] ReadLookDirectories()
        {
            const string filename = "optimizer_folders.txt";
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), filename);

            if (!File.Exists(path)) return null;

            return File.ReadAllLines(path).ToArray();
        }
    }
}
