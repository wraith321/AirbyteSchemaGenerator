using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
	/// Interaction logic for TaskContainer.xaml
	/// </summary>
	public partial class TaskContainer : UserControl
	{
		private Action<object>? MessageHandler = null;

		public static readonly DependencyProperty ConsoleMessagesProperty =
		DependencyProperty.Register(
		   "ConsoleMessages",
		   typeof(ObservableCollection<ConsoleMessage>),
		   typeof(TaskContainer),
		   new PropertyMetadata(null));

		public ObservableCollection<ConsoleMessage> ConsoleMessages
		{
			get { return (ObservableCollection<ConsoleMessage>)GetValue(ConsoleMessagesProperty); }
			set { SetValue(ConsoleMessagesProperty, value); }
		}

		public static readonly DependencyProperty LatestConsoleMessageProperty =
		DependencyProperty.Register(
		   "LatestConsoleMessage",
		   typeof(ConsoleMessage),
		   typeof(TaskContainer),
		   new PropertyMetadata(null));

		public ConsoleMessage? LatestConsoleMessage
		{
			get { return (ConsoleMessage)GetValue(LatestConsoleMessageProperty); }
			set { SetValue(LatestConsoleMessageProperty, value); }
		}

		public TaskContainer()
		{
			InitializeComponent();
			ConsoleMessages = new ObservableCollection<ConsoleMessage>();
		}

		public static readonly DependencyProperty ProgressProperty =
		DependencyProperty.Register(
		"Progress",
		   typeof(double),
		   typeof(TaskContainer),
		   new PropertyMetadata(0.0));

		public class ConsoleMessage
		{
			public string Message { get; set; } = string.Empty;
			public Color Color { get; set; } = Colors.White;
			public object? Payload { get; set; } = null;
			public bool HasPayload => Payload != null;
		}

		public double Progress
		{
			get { return (double)GetValue(ProgressProperty); }
			set { SetValue(ProgressProperty, value); }
		}

		public static readonly DependencyProperty TaskNameProperty =
		DependencyProperty.Register(
		"TaskName",
		   typeof(string),
		   typeof(TaskContainer),
		   new PropertyMetadata(string.Empty));

		public string TaskName
		{
			get { return (string)GetValue(TaskNameProperty); }
			set { SetValue(TaskNameProperty, value); }
		}

		public void AddMessage(string? message, Color? Color = null, object? Payload = null)
		{
			if (message == null)
			{
				return;
			}

			Dispatcher.Invoke(() =>
			{
				LatestConsoleMessage = new ConsoleMessage { Message = message, Color = Color ?? Colors.White, Payload = Payload };
				ConsoleMessages.Insert(0, LatestConsoleMessage);
			});
		}

		public void SetMessageHandler(Action<object>? handler)
		{
			MessageHandler = handler;
		}

		public void ClearMessages()
		{
			Dispatcher.Invoke(() =>
			{
				ConsoleMessages.Clear();
				LatestConsoleMessage = null;
			});
		}

		public void SetProgress(double progress)
		{
			Dispatcher.Invoke(() =>
			{
				Progress = progress;
			});
		}

		private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//if the new selected item has a consolemessage as datacontext and its payload is not null, call the message handler
			if (sender is ListBox listBox && listBox.SelectedItem is ConsoleMessage message)
			{
				if (message.Payload != null && MessageHandler != null)
				{
					MessageHandler(message.Payload);
				}
			}
		}
	}
}
