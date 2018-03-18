using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using JetBrains.Annotations;
using Livet;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Settings;
using StarryEyes.Settings.KeyAssigns;
using StarryEyes.Views.Controls;

namespace StarryEyes.ViewModels.WindowParts.Flips.SettingFlips
{
    public class KeyAssignEditorViewModel : ViewModel
    {
        private KeyAssignProfile _profile;

        private AssignViewModel _currentAssignViewModel;

        public KeyAssignEditorViewModel()
        {
            RefreshRegisteredActions();
        }

        public void RefreshRegisteredActions()
        {
            Actions.Clear();
            KeyAssignManager.RegisteredActions
                            .ForEach(a => Actions.Add(new AssignActionViewModel(a)));
        }

        public KeyAssignProfile Profile
        {
            get => _profile;
            set
            {
                _profile = value;
                RaisePropertyChanged();
                Assigns.Clear();
                var groups = new[]
                {
                    KeyAssignGroup.Global,
                    KeyAssignGroup.Input,
                    KeyAssignGroup.Search,
                    KeyAssignGroup.Timeline
                };
                groups.SelectMany(g =>
                          value.GetDictionary(g).Values
                               .SelectMany(a => a)
                               .SelectMany(a =>
                                   a.Actions
                                    .Select(action => new AssignViewModel(this,
                                        a.Key, a.Modifiers, g,
                                        action.ActionName, action.Argument))))
                      .ForEach(v => Assigns.Add(v));
                CurrentAssignViewModel = null;
            }
        }

        public void ClearCurrentProfile()
        {
            _profile = null;
        }

        public ObservableCollection<AssignViewModel> Assigns { get; } = new ObservableCollection<AssignViewModel>();

        public ObservableCollection<AssignActionViewModel> Actions { get; } =
            new ObservableCollection<AssignActionViewModel>();

        public AssignViewModel CurrentAssignViewModel
        {
            get => _currentAssignViewModel;
            set
            {
                _currentAssignViewModel = value;
                RaisePropertyChanged();
            }
        }

        public void Commit()
        {
            if (_profile == null) return;
            _profile.ClearAssigns();
            Assigns.Where(a => !String.IsNullOrEmpty(a.Action))
                   .GroupBy(a => a.Group)
                   .ForEach(g =>
                       g.GroupBy(a => Tuple.Create(a.Key, a.Modifier))
                        .Select(a => new KeyAssign(a.Key.Item1, a.Key.Item2,
                            a.Select(m => new KeyAssignActionDescription
                            {
                                ActionName = m.Action,
                                Argument = String.IsNullOrEmpty(m.Argument) ? null : m.Argument
                            })))
                        .ForEach(a => _profile.SetAssign(g.Key, a)));
            _profile.Save(KeyAssignManager.KeyAssignsProfileDirectoryPath);
        }

        [UsedImplicitly]
        public void AddNewAssign()
        {
            var nvm = new AssignViewModel(this,
                Key.Enter, ModifierKeys.None, KeyAssignGroup.Global,
                null, null);
            Assigns.Add(nvm);
            CurrentAssignViewModel = nvm;
            Commit();
        }

        public void Remove(AssignViewModel assignViewModel)
        {
            Assigns.Remove(assignViewModel);
            Commit();
        }
    }

    public class AssignViewModel : ViewModel
    {
        private readonly KeyAssignEditorViewModel _parent;
        private Key _key;
        private ModifierKeys _modifier;
        private KeyAssignGroup _group;
        private string _action;
        private string _argument;

        public AssignViewModel(KeyAssignEditorViewModel parent,
            Key key, ModifierKeys modifier, KeyAssignGroup group,
            string action, string argument)
        {
            _parent = parent;
            _key = key;
            _modifier = modifier;
            _group = group;
            _action = action;
            _argument = argument;
        }

        public Key Key
        {
            get => _key;
            set
            {
                _key = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => KeyAndModifier);
                _parent.Commit();
            }
        }

        public ModifierKeys Modifier
        {
            get => _modifier;
            set
            {
                _modifier = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => KeyAndModifier);
                _parent.Commit();
            }
        }

        public string KeyAndModifier => HotKeyTextBox.StringifyShortcutKeys(Key, Modifier);

        public KeyAssignGroup Group
        {
            get => _group;
            set
            {
                _group = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => GroupString);
                RaisePropertyChanged(() => GroupIndex);
                _parent.Commit();
            }
        }

        public string GroupString => _group.ToString();

        public int GroupIndex
        {
            get => (int)Group;
            set => Group = (KeyAssignGroup)value;
        }

        public ObservableCollection<AssignActionViewModel> Actions => _parent.Actions;

        public string Action => _action;

        public AssignActionViewModel CurrentActionViewModel
        {
            get { return _parent.Actions.FirstOrDefault(a => a.Name == _action); }
            set
            {
                _action = value.Name;
                if (!value.IsArgumentRequired)
                {
                    Argument = null;
                }
                _parent.Commit();
                RaisePropertyChanged(() => Action);
                RaisePropertyChanged(() => CurrentActionViewModel);
                RaisePropertyChanged(() => IsArgumentEnabled);
            }
        }

        public bool IsArgumentEnabled
        {
            get
            {
                var cavm = CurrentActionViewModel;
                return cavm != null && cavm.IsArgumentRequired;
            }
        }

        public string Argument
        {
            get => _argument;
            set
            {
                _argument = value;
                RaisePropertyChanged();
                _parent.Commit();
            }
        }

        [UsedImplicitly]
        public void Remove()
        {
            _parent.Remove(this);
        }
    }

    public class AssignActionViewModel : ViewModel
    {
        private readonly KeyAssignAction _action;

        public AssignActionViewModel(KeyAssignAction action)
        {
            _action = action;
        }

        public string Name => _action.Name;

        public string ArgumentType => _action.ArgumentRequired == null
            ? SettingFlipResources.KeyAssignArgumentOptional
            : (_action.ArgumentRequired.Value
                ? SettingFlipResources.KeyAssignArgumentRequired
                : SettingFlipResources.KeyAssignArgumentNone);

        public bool IsArgumentRequired => _action.ArgumentRequired.GetValueOrDefault(true);
    }
}