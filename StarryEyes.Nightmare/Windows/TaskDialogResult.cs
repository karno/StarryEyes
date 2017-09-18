namespace StarryEyes.Nightmare.Windows
{
    public class TaskDialogResult
    {
        /// <summary>
        /// Represents a result with no data.
        /// 
        /// </summary>
        public static readonly TaskDialogResult Empty = new TaskDialogResult();

        /// <summary>
        /// Gets the <see cref="TaskDialogSimpleResult"/> of the TaskDialog.
        /// 
        /// </summary>
        public TaskDialogSimpleResult Result { get; }

        /// <summary>
        /// Gets a value indicating whether or not the verification checkbox
        ///             was checked. A null value indicates that the checkbox wasn't shown.
        /// 
        /// </summary>
        public bool? VerificationChecked { get; }

        /// <summary>
        /// Gets the zero-based index of the radio button that was clicked.
        ///             A null value indicates that no radio button was clicked.
        /// 
        /// </summary>
        public int? RadioButtonResult { get; }

        /// <summary>
        /// Gets the zero-based index of the command button that was clicked.
        ///             A null value indicates that no command button was clicked.
        /// 
        /// </summary>
        public int? CommandButtonResult { get; }

        /// <summary>
        /// Gets the zero-based index of the custom button that was clicked.
        ///             A null value indicates that no custom button was clicked.
        /// 
        /// </summary>
        public int? CustomButtonResult { get; }

        static TaskDialogResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDialogResult"/> class.
        /// 
        /// </summary>
        private TaskDialogResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDialogResult"/> class.
        /// 
        /// </summary>
        /// <param name="result">The simple TaskDialog result.</param><param name="verificationChecked">Wether the verification checkbox was checked.</param><param name="radioButtonResult">The radio button result, if any.</param><param name="commandButtonResult">The command button result, if any.</param><param name="customButtonResult">The custom button result, if any.</param>
        internal TaskDialogResult(TaskDialogInterop.TaskDialogSimpleResult result, bool? verificationChecked = null,
            int? radioButtonResult = null, int? commandButtonResult = null, int? customButtonResult = null)
            : this()
        {
            Result = (TaskDialogSimpleResult)result;
            VerificationChecked = verificationChecked;
            RadioButtonResult = radioButtonResult;
            CommandButtonResult = commandButtonResult;
            CustomButtonResult = customButtonResult;
        }
    }

    public enum TaskDialogSimpleResult
    {
        None = 0,
        Ok = 1,
        Cancel = 2,
        Retry = 4,
        Yes = 6,
        No = 7,
        Close = 8,
        Command = 20,
        Custom = 21,
    }
}