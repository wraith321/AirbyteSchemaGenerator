using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;

namespace AirbyteSchemaGeneratorWPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private TaskContainer MainTask;
		public MainWindow()
		{
			InitializeComponent();
			MainTask = VC.AddTask("General");
		}

		private string JSonTypeToSchemaType(JsonNode Node)
		{
			var type = Node.GetValueKind();
			switch (type)
			{
				case JsonValueKind.String:
					return "string";
				case JsonValueKind.Number:
					//check if the number is an integer
					if (Node.AsValue().ToString().Contains('.'))
					{
						return "number";
					}
					else
					{
						return "integer";
					}
				case JsonValueKind.True:
				case JsonValueKind.False:
					return "boolean";
				case JsonValueKind.Object:
					return "object";
				case JsonValueKind.Array:
					return "array";
				default:
					return "null";
			}			
		}

		private void CleanupSchema(JsonObject Schema, float occurrenceFactorToMarkAsAdditionalProperty = 0.1f)
		{
			CleanupProperty(Schema, occurrenceFactorToMarkAsAdditionalProperty, null, null);
		}

		private void CleanupProperty(JsonObject Obj, float occurrenceFactorToMarkAsAdditionalProperty, string? KeyInParent, JsonObject? ParentObject)
		{			
			if(ParentObject != null)
			{
				//If the property appeared in less than 10% of the parent object, remove it
				if (Obj["occurrences"]!.GetValue<int>() <= occurrenceFactorToMarkAsAdditionalProperty * ParentObject["occurrences"]!.GetValue<int>())
				{
					ParentObject["additionalProperties"] = true;
					ParentObject["properties"]!.AsObject().Remove(KeyInParent!);

					//if this property is a required property, remove it from the required list of the parent
					JsonNode? found = ParentObject["required"]!.AsArray().FirstOrDefault(key => key!.GetValue<string>() == KeyInParent);
					if (found != null)
					{
						ParentObject["required"]!.AsArray().Remove(found);						
					}

					return;
				}
			}

			//this is an object and it has properties
			if (Obj.ContainsKey("properties"))
			{
				//cleanup all properties of the object if it has any
				var propObject = Obj["properties"]!.AsObject();
				if (propObject.Count == 0)
				{
					Obj.Remove("properties");
				}
				else
				{
					for (int i = propObject.Count - 1; i >= 0; --i)
					{
						var prop = propObject.ElementAt(i);
						CleanupProperty(prop.Value!.AsObject(), occurrenceFactorToMarkAsAdditionalProperty, prop.Key, Obj);
					}
				}

				//remove the required property if it's empty
				if (Obj["required"]!.AsArray().Count == 0)
				{
					Obj.Remove("required");
				}

				//remove the additionalProperties property if it's false
				if (Obj["additionalProperties"]!.GetValue<bool>() == false)
				{
					Obj.Remove("additionalProperties");
				}
			}

			Obj.Remove("occurrences");

			//Make the type field a string if it only has one type
			var types = Obj!["type"]!.AsArray();
			if (types.Count == 1)
			{
				Obj["type"] = types[0]!.GetValue<string>();
			}
		}

		private JsonObject CreateSchemaFromRecords(IEnumerable<JsonObject> Records, float occurrenceFactorToMarkAsAdditionalProperty = 0.1f)
		{
			//Create a fresh, empty json object as a start
			var newSchema = new JsonObject();
			newSchema["occurrences"] = Records.Count();
			newSchema["$schema"] = "http://json-schema.org/draft-07/schema#";
			newSchema["additionalProperties"] = false;
			newSchema["required"] = new JsonArray();
			newSchema["properties"] = new JsonObject();
			newSchema["type"] = new JsonArray("object");

			foreach (var record in Records)
			{
				ApplyJsonObjectToSchema(newSchema, record);
			}

			CleanupSchema(newSchema, occurrenceFactorToMarkAsAdditionalProperty);
			
			return newSchema;
		}

		private void ApplyJsonObjectToSchema(JsonObject Schema, JsonObject Record)
		{
			var currentSchemaProps = Schema["properties"]!.AsObject();
			foreach (var kv in Record)
			{
				//get the value kind of the current value in the record
				string valType = kv.Value == null ? "null" : JSonTypeToSchemaType(kv.Value);

				if (currentSchemaProps.ContainsKey(kv.Key))
				{
					JsonArray currentTypes = currentSchemaProps[kv.Key]!["type"]!.AsArray();
					//check if the new type is already in the list of types
					JsonNode? found = currentTypes.FirstOrDefault(t => t.GetValue<string>() == valType);
					if (found == null)
					{
						if (valType == "number")
						{
							JsonNode? foundInt = currentTypes.FirstOrDefault(t => t.GetValue<string>() == "integer");
							if (foundInt != null)
							{
								currentTypes.Remove(foundInt);
							}
						}

						if (valType == "object")
						{
							//remove all other types except null
							for (int i = currentTypes.Count - 1; i >= 0; --i)
							{
								var t = currentTypes[i];
								if (t!.GetValue<string>() != "null")
								{
									currentTypes.RemoveAt(i);
								}
							}

							//if it was previously a different type, setup the properties for the object
							if (!currentSchemaProps[kv.Key]!.AsObject().ContainsKey("required"))
							{
								currentSchemaProps[kv.Key]!["required"] = new JsonArray();
							}

							if (!currentSchemaProps[kv.Key]!.AsObject().ContainsKey("properties"))
							{
								currentSchemaProps[kv.Key]!["properties"] = new JsonObject();
							}

							if (!currentSchemaProps[kv.Key]!.AsObject().ContainsKey("additionalProperties"))
							{
								currentSchemaProps[kv.Key]!["additionalProperties"] = false;
							}
						}
						
						//an object type can never become a different type might needs conflict checking
						if(valType == "null" || !currentTypes.Any(t => t.GetValue<string>() == "object"))
						{
							currentTypes.Add(valType);
						}
					}
					currentSchemaProps[kv.Key]!["occurrences"] = currentSchemaProps[kv.Key]!["occurrences"]!.GetValue<int>() + 1;

					if (valType == "object")
					{
						//if the value is an object, apply the object to the schema recursively
						ApplyJsonObjectToSchema(currentSchemaProps[kv.Key]!.AsObject(), kv.Value!.AsObject());
					}
				}
				else
				{
					//create a new property in the schema
					var newProp = new JsonObject();
					currentSchemaProps[kv.Key] = newProp;
					newProp["type"] = new JsonArray(valType);
					newProp["occurrences"] = 1;

					if (valType == "object")
					{
						//if the value is an object, create a new schema for it
						newProp["required"] = new JsonArray();
						newProp["properties"] = new JsonObject();
						newProp["additionalProperties"] = false;
						ApplyJsonObjectToSchema(newProp, kv.Value!.AsObject());
					}
				}
			}
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			//open a folder dialog
			OpenFolderDialog openFolderDialog = new OpenFolderDialog();
			if (openFolderDialog.ShowDialog() == true)
			{
				var folderPath = openFolderDialog.FolderName;
				MainTask.AddMessage($"Selected Folder: {folderPath}");
				//check if a dockerfile exists in the current directory
				if (File.Exists(System.IO.Path.Combine(folderPath, "Dockerfile")))
				{
					//if it exists, build the docker image
					var dockerBuild = VC.AddTask("Docker Build");
					dockerBuild.AddMessage("Building Docker Image...");
					using var process = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = "docker",
							//build the image with the name based on the current directory name
							Arguments = $"build -t airbyte/{openFolderDialog.SafeFolderName}:dev .",
							RedirectStandardOutput = true,
							RedirectStandardError = true,
							UseShellExecute = false,
							CreateNoWindow = true,
							WorkingDirectory = folderPath
						}
					};


					process.EnableRaisingEvents = true;
					process.Start();
					process.OutputDataReceived += (sender, args) => dockerBuild.AddMessage(args.Data);
					process.ErrorDataReceived += (sender, args) => dockerBuild.AddMessage(args.Data);
					process.BeginOutputReadLine();
					process.BeginErrorReadLine();
					await process.WaitForExitAsync();
					dockerBuild.AddMessage("Docker Image Built Successfully");

					var dockerRun = VC.AddTask("Docker Run");
					//run the docker image
					dockerRun.AddMessage("Running Docker Image...");
					using var runProcess = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = "docker",
							Arguments = $"run --rm -v {folderPath}/secrets:/secrets -v {folderPath}/integration_tests:/integration_tests airbyte/{openFolderDialog.SafeFolderName}:dev read --config /secrets/config.json --catalog /integration_tests/configured_catalog.json",
							RedirectStandardOutput = true,
							RedirectStandardError = true,
							UseShellExecute = false,
							CreateNoWindow = true,
							WorkingDirectory = folderPath
						}
					};

					Dictionary<string, List<JsonObject>> records = [];

					runProcess.EnableRaisingEvents = true;
					runProcess.Start();
					runProcess.OutputDataReceived += (sender, args) =>
					{
						if (args.Data == null)
						{
							return;
						}

						//try to parse the output as json
						try
						{
							var json = JsonNode.Parse(args.Data);
							//check the type key to determine the type of message
							if (json?["type"]?.ToString() == "RECORD")
							{
								var stream = json?["record"]?["stream"]?.ToString();
								if (stream != null)
								{
									if (!records.ContainsKey(stream))
									{
										records[stream] = new List<JsonObject>();
									}
									var obj = json?["record"]?["data"]?.AsObject();
									if (obj != null)
									{
										records[stream].Add(obj);
										//print one summary line with all records for each stream
										List<string> summaries = [];
										foreach (var key in records.Keys)
										{
											summaries.Add($"{key}: {records[key].Count} records");
										}
										dockerRun.AddMessage(string.Join(", ", summaries));
									}
								}
							}
						}
						catch (Exception)
						{
							//ignore
						}

					};
					runProcess.ErrorDataReceived += (sender, args) => dockerRun.AddMessage(args.Data);
					runProcess.BeginOutputReadLine();
					runProcess.BeginErrorReadLine();
					await runProcess.WaitForExitAsync();
					dockerRun.AddMessage("Docker Image Ran Successfully");

					var schemaGeneration = VC.AddTask("Schema Generation");
					schemaGeneration.SetMessageHandler((payload) =>
					{
						//open the payload, which is a json object, in a new window, visualizing it in a tree view
						var schemaViewer = new SchemaViewer();
						schemaViewer.Owner = this;
						schemaViewer.LoadSchema((JsonObject)payload);
						this.IsEnabled = false;
						schemaViewer.ShowDialog();
						this.IsEnabled = true;
					});
					//create one schema for each stream based on the extracted records
					foreach(var kv in records)
					{
						//Create a fresh, empty json object as a start
						var newSchema = CreateSchemaFromRecords(kv.Value);						
						schemaGeneration.AddMessage($"Schema for {kv.Key} generated!", Payload: newSchema);
					}
				}
				else
				{
					MainTask.AddMessage("Dockerfile not found in the selected directory");
				}
			}
		}
	}
}