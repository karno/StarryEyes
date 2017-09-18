using System.Windows;
using TaskDialogInterop;

namespace StarryEyes.Nightmare.Windows
{
    public struct TaskDialogOptions
    {
        /// <summary>
        /// The default <see cref="T:TaskDialogOptions"/> to be used
        ///             by all new <see cref="T:TaskDialog"/>s.
        /// 
        /// </summary>
        /// 
        /// <remarks>
        /// Use this to make application-wide defaults, such as for
        ///             the caption.
        /// 
        /// </remarks>
        public static TaskDialogOptions Default;

        /// <summary>
        /// The owner window of the task dialog box.
        /// 
        /// </summary>
        public Window Owner;

        /// <summary>
        /// Caption of the window.
        /// 
        /// </summary>
        public string Title;

        /// <summary>
        /// A large 32x32 icon that signifies the purpose of the dialog, using
        ///             one of the built-in system icons.
        /// 
        /// </summary>
        public VistaTaskDialogIcon MainIcon;

        /// <summary>
        /// Principal text.
        /// 
        /// </summary>
        public string MainInstruction;

        /// <summary>
        /// Supplemental text that expands on the principal text.
        /// 
        /// </summary>
        public string Content;

        /// <summary>
        /// Extra text that will be hidden by default.
        /// 
        /// </summary>
        public string ExpandedInfo;

        /// <summary>
        /// Indicates that the expanded info should be displayed when the
        ///             dialog is initially displayed.
        /// 
        /// </summary>
        public bool ExpandedByDefault;

        /// <summary>
        /// Indicates that the expanded info should be displayed at the bottom
        ///             of the dialog's footer area instead of immediately after the
        ///             dialog's content.
        /// 
        /// </summary>
        public bool ExpandToFooter;

        /// <summary>
        /// Standard buttons.
        /// 
        /// </summary>
        public TaskDialogCommonButtons CommonButtons;

        /// <summary>
        /// Application-defined options for the user.
        /// 
        /// </summary>
        public string[] RadioButtons;

        /// <summary>
        /// Buttons that are not from the set of standard buttons. Use an
        ///             ampersand to denote an access key.
        /// 
        /// </summary>
        public string[] CustomButtons;

        /// <summary>
        /// Command link buttons.
        /// 
        /// </summary>
        public string[] CommandButtons;

        /// <summary>
        /// Zero-based index of the button to have focus by default.
        /// 
        /// </summary>
        public int? DefaultButtonIndex;

        /// <summary>
        /// Text accompanied by a checkbox, typically for user feedback such as
        ///             Do-not-show-this-dialog-again options.
        /// 
        /// </summary>
        public string VerificationText;

        /// <summary>
        /// Indicates that the verification checkbox in the dialog is checked
        ///             when the dialog is initially displayed.
        /// 
        /// </summary>
        public bool VerificationByDefault;

        /// <summary>
        /// A small 16x16 icon that signifies the purpose of the footer text,
        ///             using one of the built-in system icons.
        /// 
        /// </summary>
        public VistaTaskDialogIcon FooterIcon;

        /// <summary>
        /// Additional footer text.
        /// 
        /// </summary>
        public string FooterText;

        /// <summary>
        /// Indicates that the dialog should be able to be closed using Alt-F4,
        ///             Escape, and the title bar's close button even if no cancel button
        ///             is specified the CommonButtons.
        /// 
        /// </summary>
        /// 
        /// <remarks>
        /// You'll want to set this to true if you use CustomButtons and have
        ///             a Cancel-like button in it.
        /// 
        /// </remarks>
        public bool AllowDialogCancellation;

        /// <summary>
        /// Indicates that a Progress Bar is to be displayed.
        /// 
        /// </summary>
        /// 
        /// <remarks>
        /// You can set the state, whether paused, in error, etc., as well as
        ///             the range and current value by setting a callback and timer to
        ///             control the dialog at custom intervals.
        /// 
        /// </remarks>
        public bool ShowProgressBar;

        /// <summary>
        /// Indicates that an Marquee Progress Bar is to be displayed.
        /// 
        /// </summary>
        /// 
        /// <remarks>
        /// You can set start and stop the animation by setting a callback and
        ///             timer to control the dialog at custom intervals.
        /// 
        /// </remarks>
        public bool ShowMarqueeProgressBar;

        /// <summary>
        /// A callback that receives messages from the Task Dialog when
        ///             various events occur.
        /// 
        /// </summary>
        public TaskDialogCallback Callback;

        /// <summary>
        /// Reference object that is passed to the callback.
        /// 
        /// </summary>
        public object CallbackData;

        /// <summary>
        /// Indicates that the task dialog's callback is to be called
        ///             approximately every 200 milliseconds.
        /// 
        /// </summary>
        /// 
        /// <remarks>
        /// Enable this in order to do updates on the task dialog periodically,
        ///             such as for a progress bar, current download speed, or estimated
        ///             time to complete, etc.
        /// 
        /// </remarks>
        public bool EnableCallbackTimer;

        public TaskDialogInterop.TaskDialogOptions ConvertToNative()
        {
            return new TaskDialogInterop.TaskDialogOptions
            {
                AllowDialogCancellation = AllowDialogCancellation,
                Callback = Callback == null ? null : new TaskDialogInterop.TaskDialogCallback(Callback),
                CallbackData = CallbackData,
                CommandButtons = CommandButtons,
                CommonButtons = (TaskDialogInterop.TaskDialogCommonButtons)CommonButtons,
                Content = Content,
                CustomButtons = CustomButtons,
                DefaultButtonIndex = DefaultButtonIndex,
                EnableCallbackTimer = EnableCallbackTimer,
                ExpandToFooter = ExpandToFooter,
                ExpandedByDefault = ExpandedByDefault,
                ExpandedInfo = ExpandedInfo,
                FooterIcon = (TaskDialogInterop.VistaTaskDialogIcon)FooterIcon,
                FooterText = FooterText,
                MainIcon = (TaskDialogInterop.VistaTaskDialogIcon)MainIcon,
                MainInstruction = MainInstruction,
                Owner = Owner,
                RadioButtons = RadioButtons,
                ShowMarqueeProgressBar = ShowMarqueeProgressBar,
                ShowProgressBar = ShowProgressBar,
                Title = Title,
                VerificationByDefault = VerificationByDefault,
                VerificationText = VerificationText,
            };
        }
    }

    public delegate bool TaskDialogCallback(IActiveTaskDialog dialog, VistaTaskDialogNotificationArgs args,
        object callbackData);

    public enum TaskDialogCommonButtons
    {
        None,
        Close,
        YesNo,
        YesNoCancel,
        OKCancel,
        RetryCancel,
    }

    public enum VistaTaskDialogIcon : uint
    {
        None = 0U,
        Shield = 65532U,
        Information = 65533U,
        Error = 65534U,
        Warning = 65535U,
    }
}