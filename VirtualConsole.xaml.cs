using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AirbyteSchemaGeneratorWPF
{
	/// <summary>
	/// Interaction logic for VirtualConsole.xaml
	/// </summary>
	public partial class VirtualConsole : UserControl
	{
		//dependency property for the task list
		public static readonly DependencyProperty TaskListProperty =
			DependencyProperty.Register(
				"TaskList",
				typeof(ObservableCollection<TaskContainer>),
				typeof(VirtualConsole),
				new PropertyMetadata(null));

		public ObservableCollection<TaskContainer> TaskList
		{
			get { return (ObservableCollection<TaskContainer>)GetValue(TaskListProperty); }
			set { SetValue(TaskListProperty, value); }
		}

		public VirtualConsole()
		{
			InitializeComponent();
			TaskList = new ObservableCollection<TaskContainer>();
		}

		//Add a task with an optional message click handler which can handle an optional payload supplied with a message
		public TaskContainer AddTask(string TaskName, Action<object> MessageClickHandler = null)
		{
			return Dispatcher.Invoke(() =>
			{
				TaskContainer task = new TaskContainer { TaskName = TaskName };
				TaskList.Insert(0, task);
				return task;
			});
		}		
	}
}
