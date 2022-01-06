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
        public int ID { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; } = null;
        public Action BreakAction { get; private set; } = null;
        public bool Repeatable { get; private set; } = false;

        private object Executable;

        private Command() {
            Executable = null;
            ID = 0;
            Name = "NOP";
        }

        public Command(int id, string name, Action fn) {
            Executable = fn;
            Name = name;
            ID = id;
        }
        public Command(int id, string name, Action<int> fn) {
            Executable = fn;
            Name = name;
            ID = id;
        }
        public Command(int id, string name, ReactiveCommand fn) {
            Executable = fn;
            Name = name;
            ID = id;
        }
        public Command(int id, string name, ReactiveCommand<int> fn) {
            Executable = fn;
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
            if (Executable == null) return;
            var action = Executable as Action;
            if(action!=null) {
                action();
                return;
            }
            var action1 = Executable as Action<int>;
            if(action!=null) {
                action1(count);
            }
            var command = Executable as ReactiveCommand;
            if (command != null) {
                command.Execute();
                return;
            }
            var command1 = Executable as ReactiveCommand<int>;
            if (command != null) {
                command.Execute(count);
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
        private Dictionary<int, Command> CommandMap = new Dictionary<int, Command>();
        private Dictionary<Key, Command> SingleKeyCommands = new Dictionary<Key, Command>();
        private Dictionary<Key, Command> ControlKeyCommands = new Dictionary<Key, Command>();
        private Dictionary<Key, Command> ShiftKeyCommands = new Dictionary<Key, Command>();
        private Dictionary<Key, Command> ControlShiftKeyCommands = new Dictionary<Key, Command>();

        private ReactiveProperty<Key> ActiveKey { get; } = new ReactiveProperty<Key>(Key.None, ReactivePropertyMode.None);
        private ReactiveProperty<bool> Ctrl { get; } = new ReactiveProperty<bool>(false, ReactivePropertyMode.None);
        private ReactiveProperty<bool> Shift { get; } = new ReactiveProperty<bool>(false, ReactivePropertyMode.None);
        private IDisposable enabled { get; set; } = null;
        private int RepeatCount = 0;

        private ReadOnlyReactiveProperty<Command> CommandFlow { get; }
        private Command CurrentCommand = Command.NOP;

        public KeyCommandManager() : base(disposeNonPublic: true) {
            CommandFlow = ActiveKey.CombineLatest(Ctrl, Shift, (k, c, s) => {
                //LoggerEx.debug($"key changed:{k}");
                if (c && s) {
                    return ControlShiftKeyCommands.GetValue(k, Command.NOP);
                }
                else if (c) {
                    return ControlKeyCommands.GetValue(k, Command.NOP);
                }
                else if (s) {
                    return ShiftKeyCommands.GetValue(k, Command.NOP);
                }
                else {
                    return SingleKeyCommands.GetValue(k, Command.NOP);
                }
            }).ToReadOnlyReactiveProperty(Command.NOP, ReactivePropertyMode.None);
        }

        public bool Enabled {
            get => enabled != null;
        }

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
                if (null == enabled) {
                    Cancel();
                    owner.AddHandler(Keyboard.PreviewKeyDownEvent, (KeyEventHandler)OnKeyDown);
                    owner.AddHandler(Keyboard.PreviewKeyUpEvent, (KeyEventHandler)OnKeyUp);
                    enabled = CommandFlow.Subscribe(c => {
                        Execute(c);
                    });
                }
            } else {
                if (null != enabled) {
                    owner.RemoveHandler(Keyboard.PreviewKeyDownEvent, (KeyEventHandler)OnKeyDown);
                    owner.RemoveHandler(Keyboard.PreviewKeyUpEvent, (KeyEventHandler)OnKeyUp);
                    enabled.Dispose();
                    enabled = null;
                    Cancel();
                }
            }
        }

        private void Execute(Command nextCommand) {
            if(CurrentCommand.ID != nextCommand.ID) {
                RepeatCount = 0;
                CurrentCommand?.BreakAction?.Invoke();
                CurrentCommand = nextCommand;
            }
            else if(!CurrentCommand.Repeatable) {
                return;
            }
            //LoggerEx.debug($"{CurrentCommand.Name}");
            CurrentCommand.Invoke(RepeatCount++);

            // Key.Media* の場合、Upイベントが来ないので、ここでリセットしないといけない。
            var key = ActiveKey.Value;
            if(key.ToString().StartsWith("Media")) {
                ActiveKey.Value = Key.None;
            }
        }

        public void AssignSingleKeyCommand(int id, Key key) {
            SingleKeyCommands.Add(key, this[id]);
        }
        public void AssignControlKeyCommand(int id, Key key) {
            ControlKeyCommands.Add(key, this[id]);
        }
        public void AssignShiftKeyCommand(int id, Key key) {
            ShiftKeyCommands.Add(key, this[id]);
        }
        public void AssignControlShiftKeyCommand(int id, Key key) {
            ControlShiftKeyCommands.Add(key, this[id]);
        }

        public Command CommandOf(string name) {
            return CommandMap.Values.Where(c => c.Name == name).FirstOrDefault();
        }
        public Command CommandOf(int id) {
            return CommandMap.GetValue(id);
        }
        public Command this[int id] {
            get => CommandOf(id);
        }
        public Command this[string name] {
            get => CommandOf(name);
        }
        public void RegisterCommand(Command command) {
            CommandMap[command.ID] = command;
        }
        public void RegisterCommand(params Command[] commands) {
            foreach(var cmd in commands) {
                CommandMap[cmd.ID] = cmd;
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
            return SingleKeyCommands.Select(p => new KeyMapHelpItem(p.Key, p.Value))
                    .Concat(ControlKeyCommands.Select(p => new KeyMapHelpItem(p.Key, p.Value, "Ctrl+")))
                    .Concat(ShiftKeyCommands.Select(p => new KeyMapHelpItem(p.Key, p.Value, "Shift+")))
                    .Concat(ControlShiftKeyCommands.Select(p => new KeyMapHelpItem(p.Key, p.Value, "Ctrl+Shift+")))
                    .OrderBy(t => t.ID);
        }

        public override void Dispose() {
            base.Dispose();
        }
    }

}
