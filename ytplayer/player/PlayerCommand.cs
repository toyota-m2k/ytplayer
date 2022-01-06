using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.player {
    public class PlayerCommand : KeyCommandManager {
        public enum ID {
            PLAY = 1,
            PAUSE,
            TOGGLE_PLAY,
            CLOSE,

            TOGGLE_STRECH_MODE,
            TOGGLE_FULLSCREEN,

            MOVIE_NEXT,
            MOVIE_PREV,
            CHAPTER_NEXT,
            CHAPTER_PREV,

            SEEK_FORWARD_1,
            SEEK_FORWARD_5,
            SEEK_FORWARD_10,
            SEEK_BACK_1,
            SEEK_BACK_5,
            SEEK_BACK_10,

            RATING_EXCELLENT,
            RATING_GOOD,
            RATING_NORMAL,
            RATING_BAD,
            RATING_DREADFUL,
            RATING_BAD_AND_NEXT,
            RATING_DREADFUL_AND_NEXT,

            TRIM_SET_START,
            TRIM_SET_END,
            TRIM_RESET_START,
            TRIM_RESET_END,

            HELP,
        }
        public PlayerCommand(PlayerViewModel viewModel) {
            RegisterCommand(
                  CMD(ID.PLAY, "Play", viewModel.PlayCommand)
                , CMD(ID.PAUSE, "Pause", viewModel.PauseCommand)
                , CMD(ID.TOGGLE_PLAY,"TogglePlay", () => (viewModel.IsPlaying.Value ? viewModel.PauseCommand : viewModel.PlayCommand).Execute())
                , CMD(ID.CLOSE, "Close", viewModel.ClosePlayerCommand)
                , CMD(ID.TOGGLE_STRECH_MODE, "ToggleStrechMode", () => viewModel.FitMode.Value = !viewModel.FitMode.Value, "Change fitting mode")
                , CMD(ID.TOGGLE_FULLSCREEN, "ToggleFullscreen", () => viewModel.Fullscreen.Value = !viewModel.Fullscreen.Value, "Fullscreen Mode")
                , CMD(ID.MOVIE_NEXT, "MovieNext", viewModel.PlayList.Next, "Next movie")
                , CMD(ID.MOVIE_PREV, "MovieNext", viewModel.PlayList.Prev, "Previous movie")
                , CMD(ID.CHAPTER_NEXT, "ChapterNext", viewModel.NextChapterCommand, "Next chapter")
                , CMD(ID.CHAPTER_PREV, "ChapterPrev", viewModel.PrevChapterCommand, "Previous chapter")
                , REP_CMD(ID.SEEK_FORWARD_1, "SeekForward1", () => viewModel.SeekRelative(1000), null, "Seek forward in 1 sec.")
                , REP_CMD(ID.SEEK_FORWARD_5, "SeekForward5", () => viewModel.SeekRelative(5000), null, "Seek forward in 5 sec.")
                , REP_CMD(ID.SEEK_FORWARD_10, "SeekForward10", () => viewModel.SeekRelative(10000), null, "Seek forward in 10 sec.")
                , REP_CMD(ID.SEEK_BACK_1, "SeekBack1", () => viewModel.SeekRelative(-1000), null, "Seek backward in 1 sec.")
                , REP_CMD(ID.SEEK_BACK_5, "SeekBack5", () => viewModel.SeekRelative(-5000), null, "Seek backward in 5 sec.")
                , REP_CMD(ID.SEEK_BACK_10, "SeekBack10", () => viewModel.SeekRelative(-10000), null, "Seek backward in 10 sec.")
                , CMD(ID.RATING_EXCELLENT, "RatingExcellent", () => viewModel.SetRating(Rating.EXCELLENT), "Rating:Excellent")
                , CMD(ID.RATING_GOOD, "RatingGood", () => viewModel.SetRating(Rating.GOOD), "Rating:Good")
                , CMD(ID.RATING_NORMAL, "RatingNormal", () => viewModel.SetRating(Rating.NORMAL), "Rating:Normal")
                , CMD(ID.RATING_BAD, "RatingBad", () => viewModel.SetRating(Rating.BAD), "Rating:Bad")
                , CMD(ID.RATING_DREADFUL, "RatingDreadful", () => viewModel.SetRating(Rating.DREADFUL), "Rating:Dreadful")
                , CMD(ID.RATING_BAD_AND_NEXT, "RatingBadAndNext", () => { viewModel.SetRating(Rating.BAD); viewModel.PlayList.Next(); }, "Rating:Bad and next movie")
                , CMD(ID.RATING_DREADFUL_AND_NEXT, "RatingDreadfulAndNext", () => { viewModel.SetRating(Rating.DREADFUL); ; viewModel.PlayList.Next(); }, "Rating:Dreadful and naxt movie.")
                , CMD(ID.TRIM_SET_START, "SetTrimStart", viewModel.SetTrimmingStartAtCurrentPos, "Trimming to current position from head")
                , CMD(ID.TRIM_SET_END, "SetTrimEnd", viewModel.SetTrimmingEndAtCurrentPos, "Trimming from current position to tail")
                , CMD(ID.TRIM_RESET_START, "ResetTrimStart", viewModel.ResetTrimmingStart, "Reset head trimming.")
                , CMD(ID.TRIM_RESET_END, "ResetTrimEnd", viewModel.ResetTrimmingEnd, "Reset tail trimming.")
                , CMD(ID.HELP, "Help", viewModel.HelpCommand)
            );
            AssignSingleKeyCommand(ID.SEEK_FORWARD_1, Key.Right);
            AssignSingleKeyCommand(ID.SEEK_FORWARD_1, Key.G);
            AssignControlKeyCommand(ID.SEEK_FORWARD_10, Key.Right);
            AssignControlKeyCommand(ID.SEEK_FORWARD_10, Key.G);

            AssignSingleKeyCommand(ID.MOVIE_NEXT, Key.PageDown);
            AssignSingleKeyCommand(ID.MOVIE_PREV, Key.PageUp);
            AssignSingleKeyCommand(ID.MOVIE_NEXT, Key.MediaNextTrack);
            AssignSingleKeyCommand(ID.MOVIE_PREV, Key.MediaPreviousTrack);

            AssignSingleKeyCommand(ID.SEEK_BACK_1, Key.D);
            AssignSingleKeyCommand(ID.SEEK_BACK_1, Key.Left);
            AssignControlKeyCommand(ID.SEEK_BACK_5, Key.Left);
            AssignControlKeyCommand(ID.SEEK_BACK_5, Key.D);

            AssignSingleKeyCommand(ID.CLOSE, Key.Escape);

            AssignSingleKeyCommand(ID.TOGGLE_PLAY, Key.S);
            AssignSingleKeyCommand(ID.TOGGLE_PLAY, Key.MediaPlayPause);

            AssignSingleKeyCommand(ID.RATING_EXCELLENT, Key.D5);
            AssignSingleKeyCommand(ID.RATING_GOOD, Key.D4);
            AssignSingleKeyCommand(ID.RATING_NORMAL, Key.D3);
            AssignSingleKeyCommand(ID.RATING_BAD, Key.D2);
            AssignSingleKeyCommand(ID.RATING_DREADFUL, Key.D1);

            AssignSingleKeyCommand(ID.TRIM_SET_START, Key.J);
            AssignSingleKeyCommand(ID.TRIM_SET_END, Key.K);
            AssignControlKeyCommand(ID.TRIM_RESET_START, Key.J);
            AssignControlKeyCommand(ID.TRIM_RESET_END, Key.K);

            AssignSingleKeyCommand(ID.HELP, Key.F1);
        }

        public Command this[ID id] {
            get => CommandOf((int)id);
        }

        #region Private

        // ボタン押下毎に呼び出される普通のコマンド
        private static Command CMD(ID id, string name, ReactiveCommand fn, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc);
        }
        private static Command CMD(ID id, string name, Action fn, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc);
        }

        /**
         * ボタン押下中に有効化される継続性コマンド。
         * ボタンを放したときに、brkアクションが呼ばれる。
         */
        private static Command CNT_CMD(ID id, string name, ReactiveCommand fn, Action brk, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetBreakAction(brk).SetBreakAction(brk);
        }
        private static Command CNT_CMD(ID id, string name, Action fn, Action brk, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetBreakAction(brk);
        }


        /**
         * ボタン押下中に繰り返して呼ばれるコマンド。
         * 繰り返し回数が必要なら、Action<int>, ReactiveCommand<int> を使用。
         * ボタンを放したときに、brkアクションが呼ばれる。
         */
        private static Command REP_CMD(ID id, string name, ReactiveCommand fn, Action brk = null, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetRepeatable(true);
        }
        private static Command REP_CMD(ID id, string name, ReactiveCommand<int> fn, Action brk = null, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetRepeatable(true);
        }
        private static Command REP_CMD(ID id, string name, Action fn, Action brk = null, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetRepeatable(true).SetBreakAction(brk);
        }
        private static Command REP_CMD(ID id, string name, Action<int> fn, Action brk = null, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetRepeatable(true).SetBreakAction(brk);
        }

        private void AssignSingleKeyCommand(ID id, Key key) {
            AssignSingleKeyCommand((int)id, key);
        }
        private void AssignControlKeyCommand(ID id, Key key) {
            AssignControlKeyCommand((int)id, key);
        }
        private void AssignShiftKeyCommand(ID id, Key key) {
            AssignShiftKeyCommand((int)id, key);
        }
        private void AssignControlShiftKeyCommand(ID id, Key key) {
            AssignControlShiftKeyCommand((int)id, key);
        }

        #endregion

    }
}
