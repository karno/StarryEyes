using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Livet;
using StarryEyes.Annotations;
using StarryEyes.Settings;
using StarryEyes.Settings.KeyAssigns;
using StarryEyes.Views.Controls;

namespace StarryEyes.ViewModels.WindowParts.Flips.SettingFlips
{
    public class KeyAssignEditorViewModel : ViewModel
    {
        private KeyAssignProfile _profile;

        private readonly ObservableCollection<AssignViewModel> _assigns =
            new ObservableCollection<AssignViewModel>();

        private readonly ObservableCollection<AssignActionViewModel> _actions =
            new ObservableCollection<AssignActionViewModel>();

        private AssignViewModel _currentAssignViewModel;

        public KeyAssignEditorViewModel()
        {
            KeyAssignManager.RegisteredActions
                            .ForEach(a => this.Actions.Add(new AssignActionViewModel(a)));
        }

        public KeyAssignProfile Profile
        {
            get { return _profile; }
            set
            {
                _profile = value;
                RaisePropertyChanged();
                this.Assigns.Clear();
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
                      .ForEach(v => this.Assigns.Add(v));
                CurrentAssignViewModel = null;
            }
        }

        public void ClearCurrentProfile()
        {
            _profile = null;
        }

        public ObservableCollection<AssignViewModel> Assigns
        {
            get { return this._assigns; }
        }

        public ObservableCollection<AssignActionViewModel> Actions
        {
            get { return this._actions; }
        }

        public AssignViewModel CurrentAssignViewModel
        {
            get { return this._currentAssignViewModel; }
            set
            {
                this._currentAssignViewModel = value;
                RaisePropertyChanged();
            }
        }

        public void Commit()
        {
            if (_profile == null) return;
            _profile.ClearAssigns();
            this.Assigns.Where(a => !String.IsNullOrEmpty(a.Action))
                .GroupBy(a => a.Group)
                .ForEach(g =>
                    g.GroupBy(a => Tuple.Create(a.Key, a.Modifier))
                     .Select(a => new KeyAssign(a.Key.Item1, a.Key.Item2,
                         a.Select(m => new KeyAssignActionDescription
                         {
                             ActionName = m.Action,
                             Argument = String.IsNullOrEmpty(m.Argument) ? null : m.Argument
                         })))
                     .ForEach(a => this._profile.SetAssign(g.Key, a)));
            _profile.Save(KeyAssignManager.KeyAssignsProfileDirectoryPath);
        }

        [UsedImplicitly]
        public void AddNewAssign()
        {
            this.Assigns.Add(new AssignViewModel(this,
                Key.Enter, ModifierKeys.None, KeyAssignGroup.Global,
                null, null));
            Commit();
        }

        public void Remove(AssignViewModel assignViewModel)
        {
            this.Assigns.Remove(assignViewModel);
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
            this._parent = parent;
            this._key = key;
            this._modifier = modifier;
            this._group = group;
            this._action = action;
            this._argument = argument;
        }

        public Key Key
        {
            get { return this._key; }
            set
            {
                this._key = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => KeyAndModifier);
                _parent.Commit();
            }
        }

        public ModifierKeys Modifier
        {
            get { return this._modifier; }
            set
            {
                this._modifier = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => KeyAndModifier);
                _parent.Commit();
            }
        }

        public string KeyAndModifier
        {
            get { return HotKeyTextBox.StringifyShortcutKeys(Key, Modifier); }
        }

        public KeyAssignGroup Group
        {
            get { return this._group; }
            set
            {
                this._group = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => GroupString);
                RaisePropertyChanged(() => GroupIndex);
                _parent.Commit();
            }
        }

        public string GroupString
        {
            get { return this._group.ToString(); }
        }

        public int GroupIndex
        {
            get { return (int)Group; }
            set { Group = (KeyAssignGroup)value; }
        }

        public ObservableCollection<AssignActionViewModel> Actions
        {
            get { return this._parent.Actions; }
        }

        public string Action
        {
            get { return this._action; }
        }

        public AssignActionViewModel CurrentActionViewModel
        {
            get { return _parent.Actions.FirstOrDefault(a => a.Name == this._action); }
            set
            {
                this._action = value.Name;
                if (!value.IsArgumentEnabled)
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
                return cavm != null && cavm.IsArgumentEnabled;
            }
        }

        public string Argument
        {
            get { return this._argument; }
            set
            {
                this._argument = value;
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
            this._action = action;
        }

        public string Name
        {
            get { return _action.Name; }
        }

        public string ArgumentType
        {
            get
            {
                return this._action.HasArgument == null
                    ? "引数あり(省略可能)"
                    : (this._action.HasArgument == false
                        ? "引数なし"
                        : "引数あり");
            }
        }

        public bool IsArgumentEnabled
        {
            get { return _action.HasArgument.GetValueOrDefault(true); }
        }
    }
}
