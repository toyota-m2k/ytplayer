using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace ytplayer.common
{
    public class Command {
        public int ID { get; }
        public string Name { get; }
        public string Description { get; private set; } = null;
        public Action BreakAction { get; private set; } = null;
        public bool Repeatable { get; private set; } = false;

        private readonly object mExecutable;

        private Command() {
            mExecutable = null;
            ID = 0;
            Name = "NOP";
        }

        public Command(int id, string name, Action fn) {
            mExecutable = fn;
            Name = name;
            ID = id;
        }
        public Command(int id, string name, Action<int> fn) {
            mExecutable = fn;
            Name = name;
            ID = id;
        }
        public Command(int id, string name, ReactiveCommand fn) {
            mExecutable = fn;
            Name = name;
            ID = id;
        }
        public Command(int id, string name, ReactiveCommand<int> fn) {
            mExecutable = fn;
            Name = name;
            ID = id;
        }

        public Command SetBreakAction(Action breakAction) {
            BreakAction = breakAction;
            return this;
        }
        public Command SetDescription(string description) {
            Description = description;
            return this;
        }
        public Command SetRepeatable(bool repeatable) {
            Repeatable = repeatable;
            return this;
        }

        public void Invoke(int count) {
            if (mExecutable == null) return;
            if(mExecutable is Action action) {
                action();
                return;
            }

            if(mExecutable is Action<int> action1) {
                action1(count);
            }

            if (mExecutable is ReactiveCommand command) {
                command.Execute();
                return;
            }

            if (mExecutable is ReactiveCommand<int> command1) {
                command1.Execute(count);
                return;
            }
            Logger.error("invalid executable");
        }

        public static Command NOP = new Command();

        public override string ToString() {
            return string.IsNullOrWhiteSpace(Description) ? Name : Description;
        }
    }

    public class KeyMapHelpItem {
        public string Key { get; }
        public string Description { get; }
        public int ID { get; }
        public KeyMapHelpItem(Key key, Command command, string optionKey = "") {
            Key = $"{optionKey}{key}";
            ID = command.ID;
            Description = command.ToString();
        }
    }


    public class KeyCommandManager : ViewModelBase {
        private readonly Dictionary<int, Command> mCommandMap = new Dictionary<int, Command>();
        private readonly Dictionary<Key, Command> mSingleKeyCommands = new Dictionary<Key, Command>();
        private readonly Dictionary<Key, Command> mControlKeyCommands = new Dictionary<Key, Command>();
        private readonly Dictionary<Key, Command> mShiftKeyCommands = new Dictionary<Key, Command>();
        private readonly Dictionary<Key, Command> mControlShiftKeyCommands = new Dictionary<Key, Command>();

        private ReactiveProperty<Key> ActiveKey { get; } = new ReactiveProperty<Key>(Key.None, ReactivePropertyMode.None);
        private ReactiveProperty<bool> Ctrl { get; } = new ReactiveProperty<bool>(false, ReactivePropertyMode.None);
        private ReactiveProperty<bool> Shift { get; } = new ReactiveProperty<bool>(false, ReactivePropertyMode.None);
        private IDisposable Enabled { get; set; } = null;
        private int mRepeatCount = 0;

        private ReadOnlyReactiveProperty<Command> CommandFlow { get; }
        private Command mCurrentCommand = Command.NOP;

        public KeyCommandManager() : base(disposeNonPublic: true) {
            CommandFlow = ActiveKey.CombineLatest(Ctrl, Shift, (k, c, s) => {
                //LoggerEx.debug($"key changed:{k}");
                if (c && s) {
                    return mControlShiftKeyCommands.GetValue(k, Command.NOP);
                }
                else if (c) {
                    return mControlKeyCommands.GetValue(k, Command.NOP);
                }
                else if (s) {
                    return mShiftKeyCommands.GetValue(k, Command.NOP);
                }
                else {
                    return mSingleKeyCommands.GetValue(k, Command.NOP);
                }
            }).ToReadOnlyReactiveProperty(Command.NOP, ReactivePropertyMode.None);
        }

        public bool IsEnabled => Enabled != null;

        private bool PausedTemporary { get; set; } = false;
        public void Pause(bool pause) {
            PausedTemporary = pause;
            Cancel();
        }

        private void OnKeyDown(object sender, KeyEventArgs e) {
            //LoggerEx.debug($"Key={e.Key}, Sys={e.SystemKey}, State={e.KeyStates}, Rep={e.IsRepeat}, Down={e.IsDown}, Up={e.IsUp}, Toggled={e.IsToggled}");
            Down(e.Key);
        }
        private void OnKeyUp(object sender, KeyEventArgs e) {
            //LoggerEx.debug($"Key={e.Key}, Sys={e.SystemKey}, State={e.KeyStates}, Rep={e.IsRepeat}, Down={e.IsDown}, Up={e.IsUp}, Toggled={e.IsToggled}");
            Up(e.Key);
        }

        public void Enable(Window owner, bool enable) {
            if (enable) {
                if (null == Enabled) {
                    Cancel();
                    owner.AddHandler(Keyboard.PreviewKeyDownEvent, (KeyEventHandler)OnKeyDown);
                    owner.AddHandler(Keyboard.PreviewKeyUpEvent, (KeyEventHandler)OnKeyUp);
                    Enabled = CommandFlow.Subscribe(Execute);
                }
            } else {
                if (null != Enabled) {
                    owner.RemoveHandler(Keyboard.PreviewKeyDownEvent, (KeyEventHandler)OnKeyDown);
                    owner.RemoveHandler(Keyboard.PreviewKeyUpEvent, (KeyEventHandler)OnKeyUp);
                    Enabled.Dispose();
                    Enabled = null;
                    Cancel();
                }
            }
        }

        private void Execute(Command nextCommand) {
            if(mCurrentCommand.ID != nextCommand.ID) {
                mRepeatCount = 0;
                mCurrentCommand?.BreakAction?.Invoke();
                mCurrentCommand = nextCommand;
            }
            else if(!mCurrentCommand.Repeatable) {
                return;
            }
            //LoggerEx.debug($"{CurrentCommand.Name}");
            mCurrentCommand.Invoke(mRepeatCount++);

            // Key.Media* の場合、Upイベントが来ないので、ここでリセットしないといけない。
            var key = ActiveKey.Value;
            if(key.ToString().StartsWith("Media")) {
                ActiveKey.Value = Key.None;
            }
        }

        public void AssignSingleKeyCommand(int id, Key key) {
            mSingleKeyCommands.Add(key, this[id]);
        }
        public void AssignControlKeyCommand(int id, Key key) {
            mControlKeyCommands.Add(key, this[id]);
        }
        public void AssignShiftKeyCommand(int id, Key key) {
            mShiftKeyCommands.Add(key, this[id]);
        }
        public void AssignControlShiftKeyCommand(int id, Key key) {
            mControlShiftKeyCommands.Add(key, this[id]);
        }

        public Command CommandOf(string name) {
            return mCommandMap.Values.FirstOrDefault(c => c.Name == name);
        }
        public Command CommandOf(int id) {
            return mCommandMap.GetValue(id);
        }
        public Command this[int id] => CommandOf(id);

        public Command this[string name] => CommandOf(name);

        public void RegisterCommand(Command command) {
            mCommandMap[command.ID] = command;
        }
        public void RegisterCommand(params Command[] commands) {
            foreach(var cmd in commands) {
                mCommandMap[cmd.ID] = cmd;
            }
        }

        public void Down(Key key) {
            if (PausedTemporary) {
                return;
            }

            //LoggerEx.debug($"{key}");
            if (key == Key.LeftCtrl || key == Key.RightCtrl) {
                Ctrl.Value = true;
                //LoggerEx.debug($"Ctrl=true");
            }
            else if (key == Key.LeftShift || key == Key.RightShift) {
                Shift.Value = true;
                //LoggerEx.debug($"Shift=true");
            }
            else {
                //LoggerEx.debug($"{ActiveKey.Value} --> {key}");
                ActiveKey.Value = key;
            }
            LoggerEx.debug($"Ctrl={Ctrl.Value}, Shift={Shift.Value}, {ActiveKey.Value}");
        }

        public void Up(Key key) {
            if (key == Key.LeftCtrl || key == Key.RightCtrl) {
                Ctrl.Value = false;
                //LoggerEx.debug($"Ctrl=false");
            }
            if (key == Key.LeftShift || key==Key.RightShift) {
                Shift.Value = false;
                //LoggerEx.debug($"Shift=false");
            }
            else if (key == ActiveKey.Value) {
                ActiveKey.Value = Key.None;
                //LoggerEx.debug($"{key} --> None");
            }
            //LoggerEx.debug($"Ctrl={Ctrl.Value}, Shift={Shift.Value}, {ActiveKey.Value}");
        }

        public void Cancel() {
            ActiveKey.Value = Key.None;
            Ctrl.Value = false;
            Shift.Value = false;
        }

        public IEnumerable<KeyMapHelpItem> MakeHelpMessage() {
            return mSingleKeyCommands.Select(p => new KeyMapHelpItem(p.Key, p.Value))
                    .Concat(mControlKeyCommands.Select(p => new KeyMapHelpItem(p.Key, p.Value, "Ctrl+")))
                    .Concat(mShiftKeyCommands.Select(p => new KeyMapHelpItem(p.Key, p.Value, "Shift+")))
                    .Concat(mControlShiftKeyCommands.Select(p => new KeyMapHelpItem(p.Key, p.Value, "Ctrl+Shift+")))
                    .OrderBy(t => t.ID);
        }

        // public override void Dispose() {
        //     base.Dispose();
        // }
    }

}
