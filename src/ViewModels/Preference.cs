using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class Preference : ObservableObject
    {
        [JsonIgnore]
        public static Preference Instance
        {
            get
            {
                if (_instance == null)
                {
                    _isLoading = true;
                    if (!File.Exists(_savePath))
                    {
                        _instance = new Preference();
                    }
                    else
                    {
                        try
                        {
                            _instance = JsonSerializer.Deserialize(File.ReadAllText(_savePath), JsonCodeGen.Default.Preference);
                        }
                        catch
                        {
                            _instance = new Preference();
                        }
                    }
                    _isLoading = false;
                }

                if (!_instance.IsGitConfigured())
                    _instance.GitInstallPath = Native.OS.FindGitExecutable();

                return _instance;
            }
        }

        public string Locale
        {
            get => _locale;
            set
            {
                if (SetProperty(ref _locale, value) && !_isLoading)
                    App.SetLocale(value);
            }
        }

        public string Theme
        {
            get => _theme;
            set
            {
                if (SetProperty(ref _theme, value) && !_isLoading)
                    App.SetTheme(_theme, _themeOverrides);
            }
        }

        public string ThemeOverrides
        {
            get => _themeOverrides;
            set
            {
                if (SetProperty(ref _themeOverrides, value) && !_isLoading)
                    App.SetTheme(_theme, value);
            }
        }

        public string DefaultFontFamily
        {
            get => _defaultFontFamily;
            set
            {
                if (SetProperty(ref _defaultFontFamily, value) && !_isLoading)
                    App.SetFonts(_defaultFontFamily, _monospaceFontFamily, _onlyUseMonoFontInEditor);
            }
        }

        public string MonospaceFontFamily
        {
            get => _monospaceFontFamily;
            set
            {
                if (SetProperty(ref _monospaceFontFamily, value) && !_isLoading)
                    App.SetFonts(_defaultFontFamily, _monospaceFontFamily, _onlyUseMonoFontInEditor);
            }
        }

        public bool OnlyUseMonoFontInEditor
        {
            get => _onlyUseMonoFontInEditor;
            set
            {
                if (SetProperty(ref _onlyUseMonoFontInEditor, value) && !_isLoading)
                    App.SetFonts(_defaultFontFamily, _monospaceFontFamily, _onlyUseMonoFontInEditor);
            }
        }

        public bool UseSystemWindowFrame
        {
            get => _useSystemWindowFrame;
            set => SetProperty(ref _useSystemWindowFrame, value);
        }

        public double DefaultFontSize
        {
            get => _defaultFontSize;
            set => SetProperty(ref _defaultFontSize, value);
        }

        public LayoutInfo Layout
        {
            get => _layout;
            set => SetProperty(ref _layout, value);
        }

        public int MaxHistoryCommits
        {
            get => _maxHistoryCommits;
            set => SetProperty(ref _maxHistoryCommits, value);
        }

        public int SubjectGuideLength
        {
            get => _subjectGuideLength;
            set => SetProperty(ref _subjectGuideLength, value);
        }

        public bool RestoreTabs
        {
            get => _restoreTabs;
            set => SetProperty(ref _restoreTabs, value);
        }

        public bool UseFixedTabWidth
        {
            get => _useFixedTabWidth;
            set => SetProperty(ref _useFixedTabWidth, value);
        }

        public bool Check4UpdatesOnStartup
        {
            get => _check4UpdatesOnStartup;
            set => SetProperty(ref _check4UpdatesOnStartup, value);
        }

        public string IgnoreUpdateTag
        {
            get;
            set;
        } = string.Empty;

        public bool ShowTagsAsTree
        {
            get => _showTagsAsTree;
            set => SetProperty(ref _showTagsAsTree, value);
        }

        public bool UseTwoColumnsLayoutInHistories
        {
            get => _useTwoColumnsLayoutInHistories;
            set => SetProperty(ref _useTwoColumnsLayoutInHistories, value);
        }

        public bool DisplayTimeAsPeriodInHistories
        {
            get => _displayTimeAsPeriodInHistories;
            set => SetProperty(ref _displayTimeAsPeriodInHistories, value);
        }

        public bool UseSideBySideDiff
        {
            get => _useSideBySideDiff;
            set => SetProperty(ref _useSideBySideDiff, value);
        }

        public bool UseSyntaxHighlighting
        {
            get => _useSyntaxHighlighting;
            set => SetProperty(ref _useSyntaxHighlighting, value);
        }

        public bool EnableDiffViewWordWrap
        {
            get => _enableDiffViewWordWrap;
            set => SetProperty(ref _enableDiffViewWordWrap, value);
        }

        public bool ShowHiddenSymbolsInDiffView
        {
            get => _showHiddenSymbolsInDiffView;
            set => SetProperty(ref _showHiddenSymbolsInDiffView, value);
        }

        public Models.ChangeViewMode UnstagedChangeViewMode
        {
            get => _unstagedChangeViewMode;
            set => SetProperty(ref _unstagedChangeViewMode, value);
        }

        public Models.ChangeViewMode StagedChangeViewMode
        {
            get => _stagedChangeViewMode;
            set => SetProperty(ref _stagedChangeViewMode, value);
        }

        public Models.ChangeViewMode CommitChangeViewMode
        {
            get => _commitChangeViewMode;
            set => SetProperty(ref _commitChangeViewMode, value);
        }

        public string GitInstallPath
        {
            get => Native.OS.GitExecutable;
            set
            {
                if (Native.OS.GitExecutable != value)
                {
                    Native.OS.GitExecutable = value;
                    OnPropertyChanged();
                }
            }
        }

        public Models.Shell GitShell
        {
            get => Native.OS.GetShell();
            set
            {
                if (Native.OS.SetShell(value))
                    OnPropertyChanged();
            }
        }

        public string GitDefaultCloneDir
        {
            get => _gitDefaultCloneDir;
            set => SetProperty(ref _gitDefaultCloneDir, value);
        }

        public bool GitAutoFetch
        {
            get => Models.AutoFetchManager.Instance.IsEnabled;
            set
            {
                if (Models.AutoFetchManager.Instance.IsEnabled != value)
                {
                    Models.AutoFetchManager.Instance.IsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public int? GitAutoFetchInterval
        {
            get => Models.AutoFetchManager.Instance.Interval;
            set
            {
                if (value is null || value < 1)
                    return;

                if (Models.AutoFetchManager.Instance.Interval != value)
                {
                    Models.AutoFetchManager.Instance.Interval = (int)value;
                    OnPropertyChanged();
                }
            }
        }

        public int ExternalMergeToolType
        {
            get => _externalMergeToolType;
            set
            {
                var changed = SetProperty(ref _externalMergeToolType, value);
                if (changed && !OperatingSystem.IsWindows() && value > 0 && value < Models.ExternalMerger.Supported.Count)
                {
                    var tool = Models.ExternalMerger.Supported[value];
                    if (File.Exists(tool.Exec))
                        ExternalMergeToolPath = tool.Exec;
                    else
                        ExternalMergeToolPath = string.Empty;
                }
            }
        }

        public string ExternalMergeToolPath
        {
            get => _externalMergeToolPath;
            set => SetProperty(ref _externalMergeToolPath, value);
        }

        public List<RepositoryNode> RepositoryNodes
        {
            get;
            set;
        } = [];

        public List<string> OpenedTabs
        {
            get;
            set;
        } = [];

        public int LastActiveTabIdx
        {
            get;
            set;
        } = 0;

        public double LastCheckUpdateTime
        {
            get;
            set;
        } = 0;

        public bool IsGitConfigured()
        {
            var path = GitInstallPath;
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        public bool ShouldCheck4UpdateOnStartup()
        {
            if (!_check4UpdatesOnStartup)
                return false;

            var lastCheck = DateTime.UnixEpoch.AddSeconds(LastCheckUpdateTime).ToLocalTime();
            var now = DateTime.Now;

            if (lastCheck.Year == now.Year && lastCheck.Month == now.Month && lastCheck.Day == now.Day)
                return false;

            LastCheckUpdateTime = now.Subtract(DateTime.UnixEpoch.ToLocalTime()).TotalSeconds;
            return true;
        }

        public void AddNode(RepositoryNode node, RepositoryNode to = null)
        {
            var collection = to == null ? RepositoryNodes : to.SubNodes;
            collection.Add(node);
            collection.Sort((l, r) =>
            {
                if (l.IsRepository != r.IsRepository)
                    return l.IsRepository ? 1 : -1;

                return string.Compare(l.Name, r.Name, StringComparison.Ordinal);
            });
        }

        public RepositoryNode FindNode(string id)
        {
            return FindNodeRecursive(id, RepositoryNodes);
        }

        public RepositoryNode FindOrAddNodeByRepositoryPath(string repo, RepositoryNode parent, bool shouldMoveNode)
        {
            var node = FindNodeRecursive(repo, RepositoryNodes);
            if (node == null)
            {
                node = new RepositoryNode()
                {
                    Id = repo,
                    Name = Path.GetFileName(repo),
                    Bookmark = 0,
                    IsRepository = true,
                };

                AddNode(node, parent);
            }
            else if (shouldMoveNode)
            {
                MoveNode(node, parent);
            }

            return node;
        }

        public void MoveNode(RepositoryNode node, RepositoryNode to = null)
        {
            if (to == null && RepositoryNodes.Contains(node))
                return;
            if (to != null && to.SubNodes.Contains(node))
                return;

            RemoveNode(node);
            AddNode(node, to);
        }

        public void RemoveNode(RepositoryNode node)
        {
            RemoveNodeRecursive(node, RepositoryNodes);
        }

        public void SortByRenamedNode(RepositoryNode node)
        {
            var container = FindNodeContainer(node, RepositoryNodes);
            container?.Sort((l, r) =>
            {
                if (l.IsRepository != r.IsRepository)
                    return l.IsRepository ? 1 : -1;

                return string.Compare(l.Name, r.Name, StringComparison.Ordinal);
            });
        }

        public void Save()
        {
            var data = JsonSerializer.Serialize(this, JsonCodeGen.Default.Preference);
            File.WriteAllText(_savePath, data);
        }

        private RepositoryNode FindNodeRecursive(string id, List<RepositoryNode> collection)
        {
            foreach (var node in collection)
            {
                if (node.Id == id)
                    return node;

                var sub = FindNodeRecursive(id, node.SubNodes);
                if (sub != null)
                    return sub;
            }

            return null;
        }

        private List<RepositoryNode> FindNodeContainer(RepositoryNode node, List<RepositoryNode> collection)
        {
            foreach (var sub in collection)
            {
                if (node == sub)
                    return collection;

                var subCollection = FindNodeContainer(node, sub.SubNodes);
                if (subCollection != null)
                    return subCollection;
            }

            return null;
        }

        private bool RemoveNodeRecursive(RepositoryNode node, List<RepositoryNode> collection)
        {
            if (collection.Contains(node))
            {
                collection.Remove(node);
                return true;
            }

            foreach (RepositoryNode one in collection)
            {
                if (RemoveNodeRecursive(node, one.SubNodes))
                    return true;
            }

            return false;
        }

        private static Preference _instance = null;
        private static bool _isLoading = false;
        private static readonly string _savePath = Path.Combine(Native.OS.DataDir, "preference.json");

        private string _locale = "en_US";
        private string _theme = "Default";
        private string _themeOverrides = string.Empty;
        private string _defaultFontFamily = string.Empty;
        private string _monospaceFontFamily = string.Empty;
        private bool _onlyUseMonoFontInEditor = false;
        private bool _useSystemWindowFrame = false;
        private double _defaultFontSize = 13;
        private LayoutInfo _layout = new LayoutInfo();

        private int _maxHistoryCommits = 20000;
        private int _subjectGuideLength = 50;
        private bool _restoreTabs = false;
        private bool _useFixedTabWidth = true;
        private bool _check4UpdatesOnStartup = true;

        private bool _showTagsAsTree = false;
        private bool _useTwoColumnsLayoutInHistories = false;
        private bool _displayTimeAsPeriodInHistories = false;
        private bool _useSideBySideDiff = false;
        private bool _useSyntaxHighlighting = false;
        private bool _enableDiffViewWordWrap = false;
        private bool _showHiddenSymbolsInDiffView = false;

        private Models.ChangeViewMode _unstagedChangeViewMode = Models.ChangeViewMode.List;
        private Models.ChangeViewMode _stagedChangeViewMode = Models.ChangeViewMode.List;
        private Models.ChangeViewMode _commitChangeViewMode = Models.ChangeViewMode.List;

        private string _gitDefaultCloneDir = string.Empty;

        private int _externalMergeToolType = 0;
        private string _externalMergeToolPath = string.Empty;
    }
}
