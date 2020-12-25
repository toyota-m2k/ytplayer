using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ytplayer.common {
    public abstract class FileDialogBuilder<TBuilderClass> where TBuilderClass : class {
        protected CommonFileDialog mDialog;
        protected FileDialogBuilder(CommonFileDialog dlg) {
            mDialog = dlg;
        }
        public TBuilderClass title(string title) {
            mDialog.Title = title;
            return this as TBuilderClass;
        }
        /**
         * "txt" など（.なし）
         */
        public TBuilderClass defaultExtension(string ext) {
            mDialog.DefaultExtension = ext;
            return this as TBuilderClass;
        }
        public TBuilderClass defaultFilename(string ext) {
            mDialog.DefaultFileName = ext;
            return this as TBuilderClass;
        }
        public TBuilderClass initialDirectory(string path) {
            mDialog.InitialDirectory = path;
            return this as TBuilderClass;
        }
        public TBuilderClass addFileType(string label, string wildcards) {
            mDialog.Filters.Add(new CommonFileDialogFilter(label, wildcards));
            return this as TBuilderClass;
        }
        public TBuilderClass addFileTypes(IEnumerable<(string label, string wildcards)> types) {
            foreach (var type in types) {
                mDialog.Filters.Add(new CommonFileDialogFilter(type.label, type.wildcards));
            }
            return this as TBuilderClass;
        }
        public TBuilderClass ensureFileExists(bool exists = true) {
            mDialog.EnsureFileExists = exists;
            return this as TBuilderClass;
        }

        public string GetFilePath(Window owner) {
            using (mDialog) {
                var r = mDialog.ShowDialog(owner);
                if (r == CommonFileDialogResult.Ok) {
                    return mDialog.FileName;
                }
                return null;
            }
        }
    }
    
    public class OpenFileDialogBuilder : FileDialogBuilder<OpenFileDialogBuilder> {
        private CommonOpenFileDialog Dialog => mDialog as CommonOpenFileDialog;

        public OpenFileDialogBuilder() : base(new CommonOpenFileDialog()) {
            Dialog.Multiselect = false;
            Dialog.RestoreDirectory = true;
        }
        public OpenFileDialogBuilder multiSelection(bool multi = true) {
            Dialog.Multiselect = multi;
            return this;
        }
        public OpenFileDialogBuilder directorySelection(bool dir = true) {
            Dialog.IsFolderPicker = dir;
            return this;
        }

        public OpenFileDialogBuilder customize(Action<CommonOpenFileDialog> custom) {
            custom(Dialog);
            return this;
        }

        public IEnumerable<string> GetFilePaths(Window owner) {
            using (Dialog) {
                Dialog.Multiselect = true;
                var r = Dialog.ShowDialog(owner);
                if (r == CommonFileDialogResult.Ok) {
                    return Dialog.FileNames;
                }
                return null;
            }
        }
        public static OpenFileDialogBuilder Create() {
            return new OpenFileDialogBuilder();
        }
    }

    public class FolderDialogBuilder : FileDialogBuilder<FolderDialogBuilder> {
        private CommonOpenFileDialog Dialog => mDialog as CommonOpenFileDialog;
        public FolderDialogBuilder() : base(new CommonOpenFileDialog()) {
            Dialog.Multiselect = false;
            Dialog.RestoreDirectory = true;
            Dialog.IsFolderPicker = true;
        }
        public FolderDialogBuilder customize(Action<CommonOpenFileDialog> custom) {
            custom(Dialog);
            return this;
        }
        public static FolderDialogBuilder Create() {
            return new FolderDialogBuilder();
        }
    }

    public class SaveFileDialogBuilder : FileDialogBuilder<SaveFileDialogBuilder> {
        private CommonSaveFileDialog Dialog => mDialog as CommonSaveFileDialog;
        public SaveFileDialogBuilder() : base(new CommonSaveFileDialog()) {
        }

        public SaveFileDialogBuilder overwritePrompt(bool prompt=true) {
            Dialog.OverwritePrompt = prompt;
            return this;
        }
        public SaveFileDialogBuilder createPrompt(bool prompt = true) {
            Dialog.CreatePrompt = prompt;
            return this;
        }
        public SaveFileDialogBuilder showFolders(bool expandMode = true) {
            Dialog.IsExpandedMode = expandMode;
            return this;
        }
        public SaveFileDialogBuilder customize(Action<CommonSaveFileDialog> custom) {
            custom(Dialog);
            return this;
        }

        public static SaveFileDialogBuilder Create() {
            return new SaveFileDialogBuilder();
        }
    }
}
