using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace swagger_fetch {
  public class Parser {

    private string filePath;

    public Parser(string relativeFilePath) {
      this.filePath = Path.GetFullPath(relativeFilePath);
    }

    public async Task Process() {
      try {
        string text = await File.ReadAllTextAsync(this.filePath);
        var swaggerJson = JsonSerializer.Deserialize<JsonElement>(text);

        var outputPath = Path.Join(Path.GetDirectoryName(this.filePath), "types.ts");

        var outputFileText = new StringBuilder();
        outputFileText.AppendLine("/* eslint-disable */");

        foreach (var definition in swaggerJson.GetProperty("definitions").EnumerateObject()) {
          var name = definition.Name;

          foreach (var prop in definition.Value.EnumerateObject()) {
            if (prop.NameEquals("type")) {
              var value = prop.Value.GetString();

              if (value == "string") {
                HandleEnum(name, definition.Value, outputFileText);
              }

              else if (value == "object") {
                HandleType(name, definition.Value, outputFileText);
              }
              break;
            }
          }
        }

        var file = File.CreateText(outputPath);
        await file.WriteAsync(outputFileText.ToString());
        file.Close();
      }

      catch (Exception e) {
        Console.WriteLine("Something is wrong - \n\n" + e.ToString());
      }
    }

    private void HandleEnum(string name, JsonElement definitionValue, StringBuilder outputFileText) {

      outputFileText.AppendLine($"export enum {name} {{");

      foreach (var propMember in definitionValue.GetProperty("enum").EnumerateArray()) {
        outputFileText.AppendLine($"  {propMember} = '{propMember}',");
      }

      outputFileText.AppendLine($"}}");
    }

    private void HandleType(string name, JsonElement definition, StringBuilder outputFileText) {
      string result = String.Empty;

      var typeDescriptionExists = definition.TryGetProperty("description", out JsonElement typeDescription);
      if (typeDescriptionExists) {
        outputFileText.AppendLine($"/**");
        outputFileText.AppendLine($" * {typeDescription.GetString()?.Trim()}");
        outputFileText.AppendLine($" */");
      }

      outputFileText.AppendLine($"export type {name} = {{");

      var requiredProps = new List<string>();
      var requiredExists = definition.TryGetProperty("required", out JsonElement required);
      if (requiredExists) {
        foreach (var propName in required.EnumerateArray()) {
          var propNameString = propName.GetString();
          if (!String.IsNullOrEmpty(propNameString)) {
            requiredProps.Add(propNameString);
          }
        }
      }

      foreach (var propMember in definition.GetProperty("properties").EnumerateObject()) {
        var questionMark = requiredProps.Contains(propMember.Name.Trim()) ? "" : "?";

        var descriptionExists = propMember.Value.TryGetProperty("description", out JsonElement description);
        if (descriptionExists) {
          outputFileText.AppendLine($"  /** {description.GetString()?.Trim()} */");
        }

        // Handle type references and move on if they do
        var typeReferenceExists = propMember.Value.TryGetProperty("$ref", out JsonElement typeReference);
        if (typeReferenceExists) {
          outputFileText.AppendLine($"  {propMember.Name}{questionMark}: {typeReference.GetString()?.Replace("#/definitions/", "").Trim()};");
          continue;
        }

        // Handle if type property exists
        var propType = propMember.Value.GetProperty("type").GetString();

        if (propType == "string") {
          outputFileText.AppendLine($"  {propMember.Name}{questionMark}: string;");
        }

        else if (propType == "array") {
          var reference = propMember.Value.GetProperty("items").GetProperty("$ref").GetString();
          var propMemberType = reference?.Replace("#/definitions/", "").Trim();

          if (!String.IsNullOrEmpty(propMemberType)) {
            result += $"  {propMember.Name}{questionMark}: {propMemberType}[];\n";
            outputFileText.AppendLine($"  {propMember.Name}{questionMark}: {propMemberType}[];");
          }
        }

        else if (propType == "integer") {
          result += $"  {propMember.Name}{questionMark}: number;\n";
          outputFileText.AppendLine($"  {propMember.Name}{questionMark}: number;");
        }
      }

      outputFileText.AppendLine($"}}");
    }
  }
}
