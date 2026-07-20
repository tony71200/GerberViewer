using GerberViewer.Views;
using GerberViewer.Workflow.Models;

namespace GerberViewer
{
    public partial class MainForm : System.Windows.Forms.Form
    {
        private readonly WorkflowContext _workflowContext = new WorkflowContext();

        public MainForm()
        {
            InitializeComponent();
            readGerberControl.WorkflowContext = _workflowContext;
            createGerberSampleControl.WorkflowContext = _workflowContext;
            alignStitchingControl.WorkflowContext = _workflowContext;
        }
    }
}
