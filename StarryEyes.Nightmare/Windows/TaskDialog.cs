namespace StarryEyes.Nightmare.Windows
{
    public static class TaskDialog
    {
        public static TaskDialogResult Show(TaskDialogOptions options)
        {
            var ir = TaskDialogInterop.TaskDialog.Show(options.ConvertToNative());
            return new TaskDialogResult(ir.Result, ir.VerificationChecked,
                ir.RadioButtonResult, ir.CommandButtonResult, ir.CustomButtonResult);
        }
    }
}